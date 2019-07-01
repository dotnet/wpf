// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Test.ApplicationControl;
using Microsoft.Test.Input;
using Xunit;

namespace Microsoft.Test.AcceptanceTests.ApplicationControl
{
    /// <summary>
    /// This is a test class for AutomatedApplicationTest and is intended
    /// to contain all AutomatedApplicationTest Unit Tests
    /// </summary>
    public class WpfInProcSameThreadUnitTests : AutomatedApplicationTestBase
    {
        public WpfInProcSameThreadUnitTests()
        {
        }

        protected override void MyTestInitialize()
        {
            if (TestAutomatedApp == null)
            {
                TestAutomatedApp = new InProcessApplication(new WpfInProcessApplicationSettings
                {
                    Path = TestApplicationPath,
                    InProcessApplicationType = InProcessApplicationType.InProcessSameThread,
                    WindowClassName = TestWindowClassName,
                    ApplicationImplementationFactory = new WpfInProcessApplicationFactory()
                });
            }
        }

        #region Base Tests

        [Fact]
        public override void CreateTest()
        {
            CreateTestHelper(InProcessApplicationType.InProcessSameThread);
        }

        [Fact]
        public override void StartTest()
        {
            StartTestHelper();
        }

        [Fact]
        public override void MainWindowTest()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            Assert.NotNull(TestAutomatedApp.MainWindow);
            Assert.True(typeof(Window).IsAssignableFrom(TestAutomatedApp.MainWindow.GetType()));
        }

        [Fact]
        public override void WaitForMainWindowTest()
        {
            WaitForMainWindowHelper();
        }

        [Fact]
        public override void OnMainWindowOpenedTest()
        {
            OnMainWindowOpenedHelper();
        }

        [Fact]
        public override void OnMainWindowOpenedViaAddHandlerTest()
        {
            OnMainWindowOpenedViaAddHandlerHelper();
        }

        [Fact]
        public override void OnExitTest()
        {
            OnExitHelper();
        }

        [Fact]
        public override void OnExitViaAddHandlerTest()
        {
            OnExitViaAddHandlerHelper();
        }

        //[Fact]
        public override void WaitForWindowTest()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            Debug.WriteLine("get the dialog launch button from the main window");
            var mainWindow = TestAutomatedApp.MainWindow as Window;
            var launchButton = TestHelper.GetVisualChild<Button>(mainWindow);

            Debug.WriteLine("click the button to launch the dialog");
            var clickPointWPF = launchButton.PointToScreen(new Point(3, 3));
            var clickPoint = new System.Drawing.Point();
            clickPoint.X = (int)clickPointWPF.X;
            clickPoint.Y = (int)clickPointWPF.Y;

            Mouse.MoveTo(clickPoint);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);
            Mouse.Click(MouseButton.Left);

            Debug.WriteLine("wait for the window");
            TestAutomatedApp.WaitForWindow("TestDialog", DefaultTimeoutTimeSpan);

            Window windowToWait = null;
            foreach (Window window in Application.Current.Windows)
            {
                if (window.Title == "TestDialog")
                {
                    windowToWait = window;
                }
            }

            Assert.NotNull(windowToWait);
        }

        [Fact]
        public override void WaitForInputIdleTest()
        {
            bool isWaitForOperationCompleted = false;
            Queue<int> operationOrderQueue = new Queue<int>();

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            var dispatcher = ((TestAutomatedApp as InProcessApplication).ApplicationDriver as Application).Dispatcher;
            dispatcher.Hooks.OperationCompleted += (completedSender, completedArgs) =>
            {
                if (completedArgs.Operation.Priority == DispatcherPriority.ApplicationIdle)
                {
                    operationOrderQueue.Enqueue(2);

                    isWaitForOperationCompleted = true;
                    Debug.WriteLine(string.Format(
                        CultureInfo.CurrentCulture,
                        "operation completed: pri: {0}, status: {1}, result: {2}",
                        completedArgs.Operation.Priority,
                        completedArgs.Operation.Status,
                        completedArgs.Operation.Result));
                }
            };

            // add other operations to the queue that should run before ApplicationIdle
            dispatcher.BeginInvoke(new DispatcherOperationCallback((param) =>
                {
                    Debug.WriteLine("minimize the window");
                    (TestAutomatedApp.MainWindow as Window).WindowState = WindowState.Minimized;

                    Debug.WriteLine("maximize the window");
                    (TestAutomatedApp.MainWindow as Window).WindowState = WindowState.Maximized;

                    Debug.WriteLine("normalize the window");
                    (TestAutomatedApp.MainWindow as Window).WindowState = WindowState.Normal;

                    operationOrderQueue.Enqueue(1);

                    return null;
                }),
                DispatcherPriority.Normal,
                new object[] { null });

            Assert.False(isWaitForOperationCompleted);

            operationOrderQueue.Enqueue(0);

            Debug.WriteLine("wait for input idle");
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            operationOrderQueue.Enqueue(3);

            Assert.True(isWaitForOperationCompleted);

            // verify the operations took place in the correct order
            for (int i = 0; i < operationOrderQueue.Count; i++)
            {
                Assert.Equal<int>(i, operationOrderQueue.Dequeue());
            }
        }

        [Fact]
        public override void CloseTest()
        {
            CloseHelper();
        }

        [Fact]
        public override void OnFocusChangedTest()
        {
            var focusChangedList = new List<string>();

            TestAutomatedApp.AddEventHandler(
                AutomatedApplicationEventType.FocusChangedEvent,
                new EventHandler<AutomatedApplicationFocusChangedEventArgs>((s, e) =>
                {
                    Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Focus changed event fired. NewFocusedElement: {0}", (e.NewFocusedElement as ContentControl).Content.ToString()));
                    focusChangedList.Add((e.NewFocusedElement as ContentControl).Content.ToString());
                }));

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Debug.WriteLine("press tab");
            Microsoft.Test.Input.Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Debug.WriteLine("press tab again");
            Microsoft.Test.Input.Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Debug.WriteLine("press tab again");
            Microsoft.Test.Input.Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Assert.Equal<int>(4, focusChangedList.Count);
            Assert.Equal<string>("System.Windows.Controls.Grid", focusChangedList[0]);
            Assert.Equal<string>("Open", focusChangedList[1]);
            Assert.Equal<string>("Start Animation", focusChangedList[2]);
            Assert.Equal<string>("Debug", focusChangedList[3]);
        }

        [Fact]
        public override void ApplicationSettingsValidationTest()
        {
            // create AutomatedApplication with no path or WindowClassName set
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var testApp = new InProcessApplication(new WpfInProcessApplicationSettings
                    {
                        InProcessApplicationType = InProcessApplicationType.InProcessSameThread
                    });
                });

            // create AutomatedApplication with path set to emtpy string
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var testApp = new InProcessApplication(new WpfInProcessApplicationSettings
                    {
                        InProcessApplicationType = InProcessApplicationType.InProcessSameThread,
                        Path = string.Empty
                    });
                });

            // create AutomatedApplication with correct path but no WindowClassName 
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var testApp = new InProcessApplication(new WpfInProcessApplicationSettings
                    {
                        InProcessApplicationType = InProcessApplicationType.InProcessSameThread,
                        Path = TestApplicationPath
                    });
                });

            // create AutomatedApplication with correct path but empty WindowClassName 
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var testApp = new InProcessApplication(new WpfInProcessApplicationSettings
                    {
                        InProcessApplicationType = InProcessApplicationType.InProcessSameThread,
                        Path = TestApplicationPath,
                        WindowClassName = string.Empty
                    });
                });
        }

        #endregion Base Tests

        /// <summary>
        ///A test for UiThreadDispatcher
        ///</summary>
        [Fact]
        public void UIThreadDispatcherTest()
        {
            ApplicationDriverHelper();
        }
    }
}
