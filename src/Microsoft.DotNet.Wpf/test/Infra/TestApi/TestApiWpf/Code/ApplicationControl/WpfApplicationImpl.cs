// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Diagnostics;

namespace Microsoft.Test.ApplicationControl
{  
    internal class WpfApplicationImpl : MarshalByRefObject, IAutomatedApplicationImpl
    {
        internal WpfApplicationImpl(WpfInProcessApplicationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSeparateThreadAndAppDomain)
            {
                throw new InvalidOperationException("WpfApplicationImpl currently does not implement a version for InProcessSeparateThreadAndAppDomain.");
            }

            if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
            {
                if (string.IsNullOrEmpty(settings.WindowClassName))
                {
                    throw new InvalidOperationException("For InProcessSameThread scenarios, WindowClassName cannot be null or empty.");
                }
            }

            IsMainWindowOpened = false;
            this.settings = settings;

            InitializeApplication();
        }

        #region IAutomatedApplicationImpl

        /// <summary>
        /// Event fired when the main window is opened
        /// </summary>
        public event EventHandler MainWindowOpened;

        /// <summary>
        /// Event fired when the application exits
        /// </summary>
        public event EventHandler Exited;

        /// <summary>
        /// Event fired when focus is changed
        /// </summary>
        public event EventHandler FocusChanged;

        /// <summary>
        /// Gets the value indicating whether the main window is opened
        /// </summary>
        public bool IsMainWindowOpened 
        { 
            get; 
            private set; 
        }

        /// <summary>
        /// Gets access to the MainWindow object which is a System.Windows.Window
        /// </summary>
        public object MainWindow
        {
            get
            {
                if (app != null)
                {
                    return app.MainWindow;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets access to the System.Windows.Application
        /// </summary>
        public object ApplicationDriver
        {
            get
            {
                return app;
            }
        }

        /// <summary>
        /// Starts the application 
        /// </summary>
        public void Start()
        {
            if (window != null)
            {
                if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
                {
                    window.Show();
                    window.Activate();
                }
                else
                {
                    app.Run(window);
                }
            }
            else if (app != null)
            {
                MethodInfo methodInfo = app.GetType().GetMethod(InitializeComponentString);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(app, null);
                }

                app.Run();
            }

            isAppFirstInitialization = false;
        }

        /// <summary>
        /// Waits for the application to become idle
        /// </summary>
        /// <param name="timeSpan">the timeout interval</param>
        public void WaitForInputIdle(TimeSpan timeSpan)
        {
            // To keep this thread busy, we'll have to push a frame.
            DispatcherFrame frame = new DispatcherFrame();

            app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new DispatcherOperationCallback(
                delegate(object arg)
                {
                    frame.Continue = false;
                    return null;
                }), null);

            // Keep the thread busy processing events until the timeout has expired.
            Dispatcher.PushFrame(frame);
        }

        /// <summary>
        /// Blocks execution of the current thread until the main window of the 
        /// application is displayed or until the specified timeout interval elapses.
        /// </summary>
        /// <param name="timeout">The timeout interval.</param>
        public void WaitForMainWindow(TimeSpan timeout)
        {
            TimeSpan zero = TimeSpan.FromMilliseconds(0);
            TimeSpan delta = TimeSpan.FromMilliseconds(10);

            while (!this.IsMainWindowOpened && timeout > zero)
            {
                if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
                {
                    DispatcherOperations.WaitFor(DispatcherPriority.ApplicationIdle);
                }
                else
                {
                    Thread.Sleep(10);
                }

                timeout -= delta;
            }

            // set as active
            if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
            {
                app.MainWindow.Activate();
            }
            else
            {
                this.DispatcherInvoke(() => { app.MainWindow.Activate(); });
            }
        }

        /// <summary>
        /// Waits for the given window to activate.
        /// </summary>
        /// <remarks>
        /// Note that this will not work for dialogs that block the thread.
        /// </remarks>
        /// <param name="windowName">The AutomationProperties.AutomationIdProperty value of the window</param>
        /// <param name="timeout">The timeout interval.</param>
        public void WaitForWindow(string windowName, TimeSpan timeout)
        {
            bool isWindowOpened = false;

            TimeSpan zero = TimeSpan.FromMilliseconds(0);
            TimeSpan delta = TimeSpan.FromMilliseconds(10);

            while (!isWindowOpened && timeout > zero)
            {
                DispatcherOperations.WaitFor(TimeSpan.FromMilliseconds(100));
                timeout -= delta;

                if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
                {
                    isWindowOpened = WaitForWindowHelper(windowName);
                }
                else
                {
                    isWindowOpened = DispatcherInvoke<bool>(() => { return WaitForWindowHelper(windowName); });
                }
            }
        }

        /// <summary>
        /// Closes the application
        /// </summary>
        public void Close()
        {
            if (app != null)
            {
                if (settings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
                {
                    CloseHelper();

                    DispatcherOperations.WaitFor(DispatcherPriority.ApplicationIdle);
                    app.Shutdown();
                    app = null;
                }
                else
                {
                    DispatcherInvoke(() => { CloseHelper(); });

                    DispatcherOperations.WaitFor(DispatcherPriority.ApplicationIdle);
                    app.Dispatcher.InvokeShutdown();
                    app = null;
                }
            }
        }

        #endregion IAutomatedApplicationImpl

        #region Private Methods

        private void OnActivated(object sender, EventArgs e)
        {
            app.MainWindow.PreviewGotKeyboardFocus += OnFocusChanged;

            if (MainWindowOpened != null)
            {
                MainWindowOpened(this, e);
            }

            IsMainWindowOpened = true;
        }

        private void OnFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (FocusChanged != null)
            {
                FocusChanged(e.NewFocus, e);
            }
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            app.Exit -= OnExit;

            if (Exited != null)
            {
                // note: ExitEventArgs is not serializable so should not be passed
                //       to listeners for cross appdomain scenarios
                Exited(this, null);
            }
        }
        
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember", Scope = "member", Target = "Microsoft.Test.ApplicationControl.WpfApplicationImpl.#InitializeApplication()", Justification = "Private members must be reflected here to init/close Application settings for in-proc scenarios")]
        private void InitializeApplication()
        {
            // look for the Application class
            var assemblyName = AssemblyName.GetAssemblyName(settings.Path);
            var assembly = Assembly.Load(assemblyName);
            var applicationType = assembly.GetTypes().FirstOrDefault(
                (type) =>
                {
                    if (type.BaseType == typeof(Application))
                    {
                        return true;
                    }
                    return false;
                });

            if (applicationType == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Assembly: {0}, does not contain an Application class. This must exist in order to start the application under test.",
                        settings.Path));
            }

            app = (Application)Activator.CreateInstance(applicationType);
            app.Activated += OnActivated;
            app.Exit += OnExit;

            if (!isAppFirstInitialization)
            {
                // need to redo the static initialization so Application can run again
                // simulates calling Application.ApplicationInit()
                typeof(Application).InvokeMember(
                                  ApplicationInitString,
                                  BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                                  null,
                                  null,
                                  null,
                                  CultureInfo.CurrentCulture);
            }

            //
            // simulates implementation for: Application.ResourceAssembly = assembly;
            // This is needed for cases when the currently executing assembly needs to 
            // launch an application.
            // 
            typeof(Application).InvokeMember(
                               AppResourceAssemblyString,
                               BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetField,
                               null,
                               null,
                               new object[] { assembly },
                               CultureInfo.CurrentCulture);

            typeof(BaseUriHelper).InvokeMember(
                               BaseUriResourceAssemblyString,
                               BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetField,
                               null,
                               null,
                               new object[] { assembly },
                               CultureInfo.CurrentCulture);

            if (!string.IsNullOrEmpty(settings.WindowClassName))
            {
                // if WindowClassName is specified create the specified window 
                // and make that the main window           
                var windowType = assembly.GetType(settings.WindowClassName, true);
                window = (Window)Activator.CreateInstance(windowType);
                window.Activated += OnActivated;

                app.MainWindow = window;
            }
        }

        private bool WaitForWindowHelper(string windowId)
        {
            bool isWindowOpened = false;
            Window windowToWait = null;

            foreach (Window window in app.Windows)
            {
                var id = (string)window.GetValue(AutomationProperties.AutomationIdProperty);
                if (id != null && id == windowId)
                {
                    windowToWait = window;
                }
            }

            if (windowToWait != null)
            {
                isWindowOpened = windowToWait.IsActive;
            }

            return isWindowOpened;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Type.InvokeMember", Scope = "member", Target = "Microsoft.Test.ApplicationControl.WpfApplicationImpl.#CloseHelper()", Justification="Private members must be reflected here to init/close Application settings for in-proc scenarios")]
        private void CloseHelper()
        {
            if (app.MainWindow != null)
            {
                foreach (Window window in app.Windows)
                {
                    if (window != app.MainWindow)
                    {
                        window.Close();
                    }
                }

                app.Activated -= OnActivated;
                app.MainWindow.Activated -= OnActivated;
                app.MainWindow.PreviewGotKeyboardFocus -= OnFocusChanged;
                app.MainWindow.Close();

                // resets the Application instance so it can be created again
                // simulates Application._appCreatedInThisAppDomain = false;
                typeof(Application).InvokeMember(
                    AppCreatedInThisAppDomainString,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.SetField,
                    null,
                    null,
                    new object[] { false },
                    CultureInfo.CurrentCulture);

                // clears the internal PreloadedPackages so Application can be reinitialized
                // simulates MS.Internal.IO.Packaging.PreloadedPackages.Clear();
                var coreAsm = Assembly.GetAssembly(typeof(UIElement));
                var preloadedPackagesType = coreAsm.GetType(PreloadedPackagesClassNameString, true, true);
                preloadedPackagesType.InvokeMember(
                    PreloadedPackagesClearString,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                    null,
                    null,
                    null,
                    CultureInfo.CurrentCulture);
            }
        }

        private void DispatcherInvoke(Action action)
        {
            app.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback((arg) =>
                {
                    action();
                    return null;
                }),
                null);
        }

        private void DispatcherInvoke<T>(Action<T> action, T args)
        {
            app.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    new DispatcherOperationCallback((arg) =>
                    {
                        action((T)arg);
                        return null;
                    }),
                    args);
        }

        private R DispatcherInvoke<R>(Func<R> func)
        {
            R retVal = (R)app.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback((arg) =>
                {
                    return func();
                }),
                null);

            return retVal;
        }

        private R DispatcherInvoke<T, R>(Func<T, R> func, T args)
        {
            R retVal = (R)app.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback((arg) =>
                {
                    return func((T)arg);
                }),
                args);

            return retVal;
        }

        private R DispatcherInvoke<T1, T2, R>(Func<T1, T2, R> func, T1 arg1, T2 arg2)
        {
            R retVal = (R)app.Dispatcher.Invoke(
                DispatcherPriority.Normal,
                new DispatcherOperationCallback((arg) =>
                {
                    return func((T1)arg, (T2)arg);
                }),
                arg1,
                arg2);

            return retVal;
        }

        #endregion Private Methods

        #region Private Fields

        // System.Windows.Application => private void InitializeComponent()
        private const string InitializeComponentString = "InitializeComponent";

        // System.Windows.Application => private static void ApplicationInit()
        private const string ApplicationInitString = "ApplicationInit";

        // System.Windows.Application => private Assembly _resourceAssembly;
        private const string AppResourceAssemblyString = "_resourceAssembly";

        // BaseUriHelper => private Assembly _resourceAssembly;
        private const string BaseUriResourceAssemblyString = "_resourceAssembly";

        // System.Windows.Application => private static bool _appCreatedInThisAppDomain;
        private const string AppCreatedInThisAppDomainString = "_appCreatedInThisAppDomain";

        // MS.Internal.IO.Packaging.PreloadedPackages full class name
        private const string PreloadedPackagesClassNameString = "MS.Internal.IO.Packaging.PreloadedPackages";

        // MS.Internal.IO.Packaging.PreloadedPackages => internal static void Clear()
        private const string PreloadedPackagesClearString = "Clear";

        /// <summary>
        /// Flag used to track if System.Windows.Application has already been called
        /// </summary>
        private static bool isAppFirstInitialization = true;

        private WpfInProcessApplicationSettings settings;
        private Application app;
        private Window window;

        #endregion Private Fields
    }
}
