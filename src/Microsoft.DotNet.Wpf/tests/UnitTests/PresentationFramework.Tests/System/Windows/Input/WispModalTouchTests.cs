// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace System.Windows.Input;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DesktopTouchCollection
{
    public const string Name = "Desktop touch injection";
}

[Collection(DesktopTouchCollection.Name)]
public sealed class WispModalTouchTests
{
    public static bool DesktopTouchEnabled =>
        Environment.GetEnvironmentVariable("WPF_TEST_DESKTOP_TOUCH") == "1";

    [WpfTheory(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    [InlineData(PanningMode.Both, true, 0)]
    [InlineData(PanningMode.Both, false, 0)]
    [InlineData(PanningMode.None, false, 0)]
    [InlineData(PanningMode.Both, true, 1)]
    [InlineData(PanningMode.Both, true, 2)]
    public async Task ShowDialog_TouchPromotion_FirstDialogTouchClicks(
        PanningMode panningMode, bool previewDown, int handledStylusUp)
    {
        AssertLocalAssembly(typeof(Window).Assembly.Location);
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);

        Button opener = new() { Content = "Open dialog", Height = 72 };
        StackPanel panel = new() { Height = 600 };
        panel.Children.Add(opener);
        Window owner = CreateWindow(new ScrollViewer
        {
            PanningMode = panningMode,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel
        });
        Dispatcher dispatcher = owner.Dispatcher;
        Window? dialog = null;
        TaskCompletionSource<DialogState> ready = NewCompletionSource<DialogState>();
        TaskCompletionSource<bool> returned = NewCompletionSource<bool>();
        string? touchDeviceType = null;
        int openingClicks = 0;
        int openingDowns = 0;
        int openingTouchUps = 0;
        int handledStylusUps = 0;
        TouchDevice? openingTouchDevice = null;
        owner.PreviewTouchDown += (_, e) =>
        {
            openingTouchDevice = e.TouchDevice;
            touchDeviceType = e.TouchDevice.GetType().FullName;
        };
        owner.PreviewTouchUp += (_, _) => openingTouchUps++;
        opener.PreviewMouseLeftButtonDown += (_, _) => openingDowns++;
        opener.Click += (_, _) => openingClicks++;
        if (handledStylusUp != 0)
        {
            owner.AddHandler(handledStylusUp == 1 ? Stylus.PreviewStylusUpEvent : Stylus.StylusUpEvent,
                new StylusEventHandler((_, e) =>
                {
                    handledStylusUps++;
                    e.Handled = true;
                }));
        }

        void OpenDialog(object sender, RoutedEventArgs e)
        {
            Assert.NotNull(openingTouchDevice);
            Assert.False(openingTouchDevice.IsActive, "The opening touch must deactivate before replay.");
            Button probe = new() { Content = "Touch target", Height = 72 };
            dialog = CreateWindow(probe);
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            DialogState state = new(dialog, probe);
            dialog.PreviewTouchUp += (_, _) => state.TouchUps++;
            probe.PreviewMouseLeftButtonDown += (_, _) => state.MouseDowns++;
            probe.Click += (_, _) => state.Clicks++;
            dialog.ContentRendered += (_, _) => ready.TrySetResult(state);
            dialog.ShowDialog();
            returned.TrySetResult(true);
        }

        if (previewDown)
        {
            opener.PreviewMouseLeftButtonDown += OpenDialog;
        }
        else
        {
            opener.Click += OpenDialog;
        }

        try
        {
            owner.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    Target openingTarget = dispatcher.Invoke(() => GetTarget(owner, opener));
                    NativeTouch.Tap(openingTarget);
                    Task completed = Task.WhenAny(ready.Task, Task.Delay(15000)).GetAwaiter().GetResult();
                    Assert.True(completed == ready.Task,
                        $"Cycle {cycle + 1}: dialog did not render. " +
                        dispatcher.Invoke(() =>
                            $"Opening touch ups={openingTouchUps}, mouse downs={openingDowns}, clicks={openingClicks}, " +
                            $"device={touchDeviceType}, dialog visible={dialog?.IsVisible}."));
                    DialogState state = ready.Task.GetAwaiter().GetResult();
                    Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice",
                        dispatcher.Invoke(() => touchDeviceType));

                    int firstSuccessfulTouch = 0;
                    string? firstFailure = null;
                    for (int tap = 1; tap <= (previewDown ? 12 : 3); tap++)
                    {
                        Target target = dispatcher.Invoke(() => GetTarget(state.Window, state.Button));
                        NativeTouch.Tap(target);
                        WaitForTouchUp(dispatcher, state, tap);
                        dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                        (int clicks, int downs, int touchUps) =
                            dispatcher.Invoke(() => (state.Clicks, state.MouseDowns, state.TouchUps));
                        if (firstSuccessfulTouch == 0 && clicks > 0)
                        {
                            firstSuccessfulTouch = tap;
                        }
                        if (firstFailure is null && (clicks != tap || downs != tap))
                        {
                            firstFailure = $"Cycle {cycle + 1}, tap {tap}: {touchUps} touch ups, " +
                                $"{downs} mouse downs, {clicks} clicks.";
                        }
                    }
                    Assert.True(firstFailure is null,
                        $"{firstFailure} First successful dialog touch: {firstSuccessfulTouch}. " +
                        "Every dialog touch must click.");

                    dispatcher.Invoke(state.Window.Close);
                    returned.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                    dispatcher.Invoke(() =>
                    {
                        if (handledStylusUp != 0)
                        {
                            Assert.Equal(cycle + 1, handledStylusUps);
                            Assert.Equal(0, openingTouchUps);
                        }
                        Assert.Null(Mouse.Captured);
                        Assert.Null(Stylus.Captured);
                        ready = NewCompletionSource<DialogState>();
                        returned = NewCompletionSource<bool>();
                    }, DispatcherPriority.ApplicationIdle);
                    Thread.Sleep(300);
                }
            });
        }
        finally
        {
            dialog?.Close();
            owner.Close();
        }
    }

    [WpfTheory(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CancelledTap_WithoutModalDialog_PreservesPromotion(bool handleDown, bool handleUp)
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button button = new() { Content = "Touch target", Height = 72 };
        StackPanel panel = new() { Height = 600 };
        panel.Children.Add(button);
        Window window = CreateWindow(new ScrollViewer
        {
            PanningMode = PanningMode.Both,
            Content = panel
        });
        DialogState state = new(window, button);
        List<string> events = new();
        string? touchDeviceType = null;
        button.PreviewTouchDown += (_, e) =>
        {
            touchDeviceType = e.TouchDevice.GetType().FullName;
            events.Add("TouchDown");
            e.Handled = handleDown;
        };
        window.PreviewTouchUp += (_, e) =>
        {
            state.TouchUps++;
            events.Add("TouchUp");
            e.Handled = handleUp;
        };
        button.PreviewMouseLeftButtonDown += (_, _) => events.Add("MouseDown");
        button.PreviewMouseLeftButtonUp += (_, _) => events.Add("MouseUp");
        button.Click += (_, _) => state.Clicks++;

        try
        {
            window.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                Target target = window.Dispatcher.Invoke(() => GetTarget(window, button));
                NativeTouch.Tap(target);
                WaitForTouchUp(window.Dispatcher, state, 1);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            });
            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice", touchDeviceType);
            if (handleDown)
            {
                Assert.DoesNotContain("MouseDown", events);
                Assert.Equal(0, state.Clicks);
            }
            else
            {
                Assert.Equal(new[] { "TouchDown", "TouchUp", "MouseDown", "MouseUp" }, events);
                Assert.Equal(1, state.Clicks);
            }
            Assert.Null(Mouse.Captured);
            Assert.Null(Stylus.Captured);
        }
        finally
        {
            window.Close();
        }
    }

    [WpfTheory(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Manipulation_WhileContactHeld_PreservesPromotion(bool cancel)
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button button = new() { Content = "Drag target", Height = 72, Margin = new Thickness(0, 150, 0, 0) };
        StackPanel panel = new() { Height = 800 };
        panel.Children.Add(button);
        ScrollViewer viewer = new() { PanningMode = PanningMode.Both, Content = panel };
        Window window = CreateWindow(viewer);
        window.Height = 420;
        DialogState state = new(window, button);
        int deltas = 0;
        int inertiaStarts = 0;
        TaskCompletionSource<bool> completed = NewCompletionSource<bool>();
        bool downBeforeUp = false;
        string? touchDeviceType = null;
        window.PreviewTouchDown += (_, e) => touchDeviceType = e.TouchDevice.GetType().FullName;
        window.PreviewTouchUp += (_, _) => state.TouchUps++;
        window.PreviewMouseLeftButtonDown += (_, _) =>
        {
            state.MouseDowns++;
            downBeforeUp |= state.TouchUps == 0;
        };
        viewer.AddHandler(UIElement.ManipulationDeltaEvent,
            new EventHandler<ManipulationDeltaEventArgs>((_, e) =>
            {
                deltas++;
                if (cancel)
                {
                    e.Cancel();
                }
            }), handledEventsToo: true);
        viewer.AddHandler(UIElement.ManipulationInertiaStartingEvent,
            new EventHandler<ManipulationInertiaStartingEventArgs>((_, _) => inertiaStarts++),
            handledEventsToo: true);
        viewer.AddHandler(UIElement.ManipulationCompletedEvent,
            new EventHandler<ManipulationCompletedEventArgs>((_, _) => completed.TrySetResult(true)),
            handledEventsToo: true);

        try
        {
            window.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                Target target = window.Dispatcher.Invoke(() => GetTarget(window, button));
                NativeTouch.Stroke(target, -100);
                WaitForTouchUp(window.Dispatcher, state, 1);
                if (!cancel)
                {
                    completed.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                }
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            });

            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice", touchDeviceType);
            Assert.True(deltas > 0, "The injected stroke did not enter manipulation.");
            if (cancel)
            {
                Assert.Equal(1, state.MouseDowns);
                Assert.True(downBeforeUp, "Mid-contact cancellation must promote before TouchUp.");
            }
            else
            {
                Assert.True(viewer.VerticalOffset > 0, "The injected stroke did not pan the ScrollViewer.");
                Assert.True(inertiaStarts > 0, "Successful panning did not enter inertia.");
                Assert.Equal(0, state.MouseDowns);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    public async Task SourceClosedDuringReplay_NextWindowFirstTouchClicks()
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button opener = new() { Content = "Replace source", Height = 72 };
        StackPanel panel = new() { Height = 600 };
        panel.Children.Add(opener);
        Window owner = CreateWindow(new ScrollViewer { PanningMode = PanningMode.Both, Content = panel });
        Window? next = null;
        TaskCompletionSource<DialogState> ready = NewCompletionSource<DialogState>();
        string? touchDeviceType = null;
        int oldMouseUps = 0;
        owner.PreviewTouchDown += (_, e) => touchDeviceType = e.TouchDevice.GetType().FullName;
        owner.PreviewMouseLeftButtonUp += (_, _) => oldMouseUps++;
        opener.PreviewMouseLeftButtonDown += (_, _) =>
        {
            Button probe = new() { Content = "New source", Height = 72 };
            next = CreateWindow(probe);
            DialogState state = new(next, probe);
            next.PreviewTouchUp += (_, _) => state.TouchUps++;
            probe.Click += (_, _) => state.Clicks++;
            next.ContentRendered += (_, _) => ready.TrySetResult(state);
            owner.Close();
            next.Show();
        };

        try
        {
            owner.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                NativeTouch.Tap(owner.Dispatcher.Invoke(() => GetTarget(owner, opener)));
                DialogState state = ready.Task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
                NativeTouch.Tap(owner.Dispatcher.Invoke(() => GetTarget(state.Window, state.Button)));
                WaitForTouchUp(owner.Dispatcher, state, 1);
                owner.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.Equal(1, owner.Dispatcher.Invoke(() => state.Clicks));
            });
            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice", touchDeviceType);
            Assert.Equal(0, oldMouseUps);
        }
        finally
        {
            next?.Close();
            owner.Close();
        }
    }

    [WpfFact(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    public async Task ReplayHandlerThrows_NextTouchStillClicks()
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button button = new() { Content = "Throw once", Height = 72 };
        StackPanel panel = new() { Height = 600 };
        panel.Children.Add(button);
        Window window = CreateWindow(new ScrollViewer { PanningMode = PanningMode.Both, Content = panel });
        DialogState state = new(window, button);
        InvalidOperationException expected = new("Replay handler exception");
        bool raised = false;
        bool observed = false;
        string? touchDeviceType = null;
        window.PreviewTouchDown += (_, e) => touchDeviceType = e.TouchDevice.GetType().FullName;
        window.PreviewTouchUp += (_, _) => state.TouchUps++;
        button.PreviewMouseLeftButtonDown += (_, _) =>
        {
            if (!raised)
            {
                raised = true;
                throw expected;
            }
        };
        button.Click += (_, _) => state.Clicks++;
        DispatcherUnhandledExceptionEventHandler handler = (_, e) =>
        {
            if (ReferenceEquals(e.Exception, expected))
            {
                observed = true;
                e.Handled = true;
            }
        };
        window.Dispatcher.UnhandledException += handler;
        try
        {
            window.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                Target target = window.Dispatcher.Invoke(() => GetTarget(window, button));
                NativeTouch.Tap(target);
                WaitForTouchUp(window.Dispatcher, state, 1);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.True(window.Dispatcher.Invoke(() => observed), "The handler exception was not propagated.");
                NativeTouch.Tap(target);
                WaitForTouchUp(window.Dispatcher, state, 2);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            });
            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice", touchDeviceType);
            Assert.Equal(1, state.Clicks);
            Assert.Null(Mouse.Captured);
            Assert.Null(Stylus.Captured);
        }
        finally
        {
            window.Dispatcher.UnhandledException -= handler;
            window.Close();
        }
    }

    [WpfFact(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    public async Task PrimaryUp_WhileSecondContactHeld_DoesNotTransferMousePromotion()
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button button = new() { Content = "Two contacts", Height = 100 };
        Window window = CreateWindow(button);
        DialogState state = new(window, button);
        HashSet<int> contacts = new();
        int mouseUps = 0;
        window.PreviewTouchDown += (_, e) =>
        {
            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice",
                e.TouchDevice.GetType().FullName);
            contacts.Add(e.TouchDevice.Id);
        };
        window.PreviewTouchUp += (_, _) => state.TouchUps++;
        button.PreviewMouseLeftButtonDown += (_, _) => state.MouseDowns++;
        button.PreviewMouseLeftButtonUp += (_, _) => mouseUps++;
        button.Click += (_, _) => state.Clicks++;

        try
        {
            window.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                Target target = window.Dispatcher.Invoke(() => GetTarget(window, button));
                NativeTouch.TwoContacts(target, () =>
                {
                    WaitForTouchUp(window.Dispatcher, state, 1);
                    window.Dispatcher.Invoke(() =>
                    {
                        Assert.Equal(2, contacts.Count);
                        Assert.Equal(1, state.MouseDowns);
                        Assert.Equal(1, mouseUps);
                    }, DispatcherPriority.ApplicationIdle);
                });
                WaitForTouchUp(window.Dispatcher, state, 2);
                window.Dispatcher.Invoke(() =>
                {
                    Assert.Equal(1, state.MouseDowns);
                    Assert.Equal(1, mouseUps);
                    Assert.Equal(1, state.Clicks);
                }, DispatcherPriority.ApplicationIdle);
                NativeTouch.Tap(target);
                WaitForTouchUp(window.Dispatcher, state, 3);
                window.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            });
            Assert.Equal(2, state.MouseDowns);
            Assert.Equal(2, mouseUps);
            Assert.Equal(2, state.Clicks);
            Assert.Null(Mouse.Captured);
            Assert.Null(Stylus.Captured);
        }
        finally
        {
            window.Close();
        }
    }

    [WpfFact(Skip = "Requires an idle, interactive desktop and WPF_TEST_DESKTOP_TOUCH=1.",
        SkipUnless = nameof(DesktopTouchEnabled))]
    public async Task ReplayMouseCaptureTransferred_TerminalUpUsesCapturedSource()
    {
        AssertLocalAssembly(typeof(Mouse).Assembly.Location);
        Button opener = new() { Content = "Transfer capture", Height = 72 };
        StackPanel panel = new() { Height = 600 };
        panel.Children.Add(opener);
        Window owner = CreateWindow(new ScrollViewer { PanningMode = PanningMode.Both, Content = panel });
        Button captured = new() { Content = "Captured source", Height = 72 };
        Window next = CreateWindow(captured);
        next.ShowActivated = false;
        DialogState state = new(owner, opener);
        int oldMouseUps = 0;
        int capturedMouseUps = 0;
        string? touchDeviceType = null;
        owner.PreviewTouchDown += (_, e) => touchDeviceType = e.TouchDevice.GetType().FullName;
        owner.PreviewTouchUp += (_, _) => state.TouchUps++;
        owner.PreviewMouseLeftButtonUp += (_, _) => oldMouseUps++;
        opener.PreviewMouseLeftButtonDown += (_, e) =>
        {
            state.MouseDowns++;
            next.Show();
            Assert.True(Mouse.Capture(captured));
            e.Handled = true;
        };
        captured.PreviewMouseLeftButtonUp += (_, _) =>
        {
            capturedMouseUps++;
            Mouse.Capture(null);
        };

        try
        {
            owner.Show();
            await RunInjection(() =>
            {
                NativeTouch.Initialize();
                Thread.Sleep(1000);
                NativeTouch.Tap(owner.Dispatcher.Invoke(() => GetTarget(owner, opener)));
                WaitForTouchUp(owner.Dispatcher, state, 1);
                owner.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            });
            Assert.Equal("System.Windows.Input.StylusWisp.WispStylusTouchDevice", touchDeviceType);
            Assert.Equal(1, state.MouseDowns);
            Assert.Equal(0, oldMouseUps);
            Assert.Equal(1, capturedMouseUps);
            Assert.Null(Mouse.Captured);
            Assert.Null(Stylus.Captured);
        }
        finally
        {
            Mouse.Capture(null);
            next.Close();
            owner.Close();
        }
    }

    private static Task RunInjection(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static void WaitForTouchUp(Dispatcher dispatcher, DialogState state, int expected)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (dispatcher.Invoke(() => state.TouchUps) < expected)
        {
            Assert.True(DateTime.UtcNow < deadline, "Injected touch up was not delivered to the target.");
            Thread.Sleep(20);
        }
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AssertLocalAssembly(string location) =>
        Assert.Equal(Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetDirectoryName(Path.GetFullPath(location)), StringComparer.OrdinalIgnoreCase);

    private static Window CreateWindow(object content) => new()
    {
        Title = "WPF desktop touch regression",
        Width = 420,
        Height = 300,
        WindowStartupLocation = WindowStartupLocation.CenterScreen,
        Content = content
    };

    private static Target GetTarget(Window window, FrameworkElement element)
    {
        Point point = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
        return new(new WindowInteropHelper(window).Handle, new NativePoint
        {
            X = (int)Math.Round(point.X),
            Y = (int)Math.Round(point.Y)
        });
    }

    private sealed class DialogState(Window window, Button button)
    {
        public Window Window { get; } = window;
        public Button Button { get; } = button;
        public int TouchUps;
        public int MouseDowns;
        public int Clicks;
    }

    private readonly record struct Target(IntPtr Hwnd, NativePoint Point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerInfo
    {
        public uint Type, Id, FrameId, Flags;
        public IntPtr SourceDevice, HwndTarget;
        public NativePoint Pixel, Himetric, PixelRaw, HimetricRaw;
        public uint Time, HistoryCount;
        public int InputData;
        public uint KeyStates;
        public ulong PerformanceCount;
        public uint ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TouchInfo
    {
        public PointerInfo Pointer;
        public uint Flags, Mask;
        public NativeRect Contact, ContactRaw;
        public uint Orientation, Pressure;
    }

    private static class NativeTouch
    {
        public static void Initialize()
        {
            if (!InitializeTouchInjection(10, 3))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeTouchInjection failed.");
            }
        }

        public static void Tap(Target target) => Stroke(target, 0);

        public static void Stroke(Target target, int deltaY)
        {
            IntPtr previousDpiContext = SetThreadDpiAwarenessContext(new IntPtr(-4));
            if (previousDpiContext == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot use physical screen coordinates.");
            }
            TouchInfo contact = CreateContact(target, 1);
            bool held = false;
            try
            {
                GuardTarget(target);
                Inject(contact);
                held = true;
                Thread.Sleep(100);
                for (int step = 1; deltaY != 0 && step <= 8; step++)
                {
                    contact.Pointer.Pixel.Y = target.Point.Y + deltaY * step / 8;
                    contact.Contact.Top = contact.Pointer.Pixel.Y - 2;
                    contact.Contact.Bottom = contact.Pointer.Pixel.Y + 2;
                    contact.Pointer.Flags = 0x20006;
                    GuardTarget(target with { Point = contact.Pointer.Pixel });
                    Inject(contact);
                    Thread.Sleep(50);
                }
                GuardTarget(target with { Point = contact.Pointer.Pixel });
                contact.Pointer.Flags = 0x40000;
                Inject(contact);
                held = false;
            }
            finally
            {
                try
                {
                    if (held)
                    {
                        contact.Pointer.Flags = 0x48000; // Cancel only the contact injected by this test.
                        Inject(contact);
                    }
                }
                finally
                {
                    SetThreadDpiAwarenessContext(previousDpiContext);
                }
            }
        }

        public static void TwoContacts(Target target, Action primaryReleased)
        {
            IntPtr previousDpiContext = SetThreadDpiAwarenessContext(new IntPtr(-4));
            if (previousDpiContext == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot use physical screen coordinates.");
            }
            Target secondTarget = target with
            {
                Point = new NativePoint { X = target.Point.X + 50, Y = target.Point.Y }
            };
            TouchInfo first = CreateContact(target, 1);
            TouchInfo second = CreateContact(secondTarget, 2);
            bool firstHeld = false;
            bool secondHeld = false;
            try
            {
                GuardTarget(target);
                Inject(first);
                firstHeld = true;
                Thread.Sleep(100);
                first.Pointer.Flags = 0x20006;
                GuardTarget(target);
                GuardTarget(secondTarget);
                Inject(first, second);
                secondHeld = true;
                Thread.Sleep(100);
                first.Pointer.Flags = 0x40000;
                second.Pointer.Flags = 0x20006;
                GuardTarget(target);
                GuardTarget(secondTarget);
                Inject(first, second);
                firstHeld = false;
                primaryReleased();
                GuardTarget(secondTarget);
                Inject(second);
                Thread.Sleep(100);
                second.Pointer.Flags = 0x40000;
                GuardTarget(secondTarget);
                Inject(second);
                secondHeld = false;
            }
            finally
            {
                try
                {
                    List<TouchInfo> held = new();
                    if (firstHeld)
                    {
                        first.Pointer.Flags = 0x48000;
                        held.Add(first);
                    }
                    if (secondHeld)
                    {
                        second.Pointer.Flags = 0x48000;
                        held.Add(second);
                    }
                    if (held.Count > 0)
                    {
                        Inject(held.ToArray());
                    }
                }
                finally
                {
                    SetThreadDpiAwarenessContext(previousDpiContext);
                }
            }
        }

        private static TouchInfo CreateContact(Target target, uint id) => new()
        {
            Pointer = new PointerInfo { Type = 2, Id = id, Pixel = target.Point, Flags = 0x10006 },
            Mask = 7,
            Contact = new NativeRect
            {
                Left = target.Point.X - 2, Top = target.Point.Y - 2,
                Right = target.Point.X + 2, Bottom = target.Point.Y + 2
            },
            Orientation = 90,
            Pressure = 512
        };

        private static void GuardTarget(Target target)
        {
            GetWindowThreadProcessId(target.Hwnd, out uint owner);
            IntPtr foreground = GetForegroundWindow();
            uint foregroundOwner = 0;
            if (foreground != IntPtr.Zero)
            {
                GetWindowThreadProcessId(foreground, out foregroundOwner);
            }
            if (owner != (uint)Environment.ProcessId ||
                GetAncestor(WindowFromPoint(target.Point), 2) != target.Hwnd ||
                (foreground != IntPtr.Zero && foregroundOwner != owner))
            {
                throw new InvalidOperationException(
                    "Touch target is foreign, obscured, or not foreground. No input injected.");
            }
        }

        private static void Inject(params TouchInfo[] contacts)
        {
            if (!InjectTouchInput((uint)contacts.Length, contacts))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error,
                    $"InjectTouchInput failed (error {error}, flags {string.Join(", ", contacts.Select(c => $"0x{c.Pointer.Flags:X}"))}).");
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InitializeTouchInjection(uint count, uint mode);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InjectTouchInput(uint count, TouchInfo[] contacts);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    }
}
