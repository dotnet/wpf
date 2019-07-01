// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Test.ApplicationControl;
using Xunit;

namespace Microsoft.Test.AcceptanceTests.ApplicationControl
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>    
    public abstract class AutomatedApplicationTestBase : IDisposable
    {
        public const int DefaultTimeoutInMS = 60000;
        public static readonly TimeSpan DefaultTimeoutTimeSpan = TimeSpan.FromMilliseconds(60000);

        protected AutomatedApplicationTestBase()
        {
            var executionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            executionDir = executionDir.Replace("file:\\", "");
            TestApplicationPath = Path.Combine(executionDir, "WpfTestApplication.exe");

            TestWindowClassName = "Microsoft.Test.AcceptanceTests.WpfTestApplication.Window1";
            TestAutomatedApp = null;

            MyTestInitialize();
        }

        #region Setup and Cleanup

        protected virtual void MyTestInitialize()
        {
        }

        protected virtual void MyTestCleanup()
        {
            if (TestAutomatedApp != null)
            {
                // test closing down the app
                TestAutomatedApp.Close();
                TestAutomatedApp = null;
            }
        }

        public void Dispose()
        {
            MyTestCleanup();
            GC.SuppressFinalize(this);
        }

        #endregion Setup and Cleanup

        #region Protected Properties

        protected string TestApplicationPath { get; set; }

        protected string TestWindowClassName { get; set; }

        protected AutomatedApplication TestAutomatedApp { get; set; }

        #endregion Protected Properties

        #region Tests

        /// <summary>
        ///A test for Create
        ///</summary>
        public abstract void CreateTest();

        /// <summary>
        ///A test for MainWindow
        ///</summary>
        public abstract void MainWindowTest();

        /// <summary>
        ///A test for Start
        ///</summary>
        public abstract void StartTest();

        /// <summary>
        ///A test for WaitForMainWindow
        ///</summary>
        public abstract void WaitForMainWindowTest();

        /// <summary>
        ///A test for OnMainWindowOpened
        ///</summary>
        public abstract void OnMainWindowOpenedTest();

        /// <summary>
        ///A test for OnMainWindowOpened
        ///</summary>
        public abstract void OnMainWindowOpenedViaAddHandlerTest();

        /// <summary>
        ///A test for OnExit
        ///</summary>
        public abstract void OnExitTest();

        /// <summary>
        ///A test for OnExit
        ///</summary>
        public abstract void OnExitViaAddHandlerTest();

        /// <summary>
        ///A test for WaitForWindow
        ///</summary>
        public abstract void WaitForWindowTest();

        /// <summary>
        ///A test for WaitForInputIdle
        ///</summary>
        public abstract void WaitForInputIdleTest();

        /// <summary>
        ///A test for Close
        ///</summary>
        public abstract void CloseTest();

        /// <summary>
        ///A test for FocusChanged
        ///</summary>
        public abstract void OnFocusChangedTest();

        /// <summary>
        ///A test for FocusChanged
        ///</summary>
        public abstract void ApplicationSettingsValidationTest();

        #endregion Tests

        #region Test Helpers

        protected void CreateTestHelper(InProcessApplicationType expectedAppType)
        {
            Assert.NotNull(TestAutomatedApp);

            if (TestAutomatedApp is InProcessApplication)
            {
                Assert.NotNull((TestAutomatedApp as InProcessApplication).ApplicationSettings);

                Assert.Equal<InProcessApplicationType>(expectedAppType, (TestAutomatedApp as InProcessApplication).ApplicationSettings.InProcessApplicationType);

                if (expectedAppType == InProcessApplicationType.InProcessSeparateThread)
                {
                    Assert.Equal<string>(TestApplicationPath, (TestAutomatedApp as InProcessApplication).ApplicationSettings.Path);
                }
                else if (expectedAppType == InProcessApplicationType.InProcessSameThread)
                {
                    var app = TestAutomatedApp as InProcessApplication;
                    Assert.Equal<string>(TestApplicationPath, app.ApplicationSettings.Path);

                    if (app.ApplicationSettings is WpfInProcessApplicationSettings)
                    {
                        Assert.Equal<string>(TestWindowClassName, (app.ApplicationSettings as WpfInProcessApplicationSettings).WindowClassName);
                    }
                }
            }
            else
            {
                Assert.NotNull((TestAutomatedApp as OutOfProcessApplication).ApplicationSettings);

                Assert.NotNull((TestAutomatedApp as OutOfProcessApplication).ApplicationSettings.ProcessStartInfo);
            }
        }

        protected void StartTestHelper()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            var appImp = typeof(AutomatedApplication).InvokeMember(
                                  "ApplicationImplementation",
                                  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty,
                                  null,
                                  TestAutomatedApp,
                                  null,
                                  CultureInfo.CurrentCulture);
            Assert.NotNull(appImp);     
        }

        protected void WaitForMainWindowHelper()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);           
            Assert.NotNull(TestAutomatedApp.MainWindow);
        }

        protected void OnMainWindowOpenedHelper()
        {
            bool mainWindowOpenedFired = false;

            TestAutomatedApp.MainWindowOpened += (s, e) =>
            {
                Debug.WriteLine("MainWindowOpened fired.");
                mainWindowOpenedFired = true;
            };

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            Assert.True(mainWindowOpenedFired, "MainWindowOpened event was not called");
        }

        protected void OnMainWindowOpenedViaAddHandlerHelper()
        {
            bool mainWindowOpenedFired = false;

            TestAutomatedApp.AddEventHandler(
                AutomatedApplicationEventType.MainWindowOpenedEvent,
                new EventHandler<AutomatedApplicationEventArgs>((s, e) =>
                {
                    Debug.WriteLine("MainWindowOpened fired.");
                    mainWindowOpenedFired = true;
                }));

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            Assert.True(mainWindowOpenedFired, "MainWindowOpened event was not called");
        }

        protected void OnExitHelper()
        {
            bool exitedFired = false;

            TestAutomatedApp.Exited += (s2, e2) =>
            {
                Debug.WriteLine("Exited fired.");
                exitedFired = true;
            };

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            // test closing down the app
            Debug.WriteLine("close the main window");
            TestAutomatedApp.Close();
            TestAutomatedApp = null;

            Assert.True(exitedFired, "Exited event did not fire.");
        }

        protected void OnExitViaAddHandlerHelper()
        {
            bool exitedFired = false;

            TestAutomatedApp.AddEventHandler(
                AutomatedApplicationEventType.ApplicationExitedEvent,
                new EventHandler<AutomatedApplicationEventArgs>((s, e) =>
                {
                    Debug.WriteLine("Exited fired.");
                    exitedFired = true;
                }));

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            // test closing down the app
            Debug.WriteLine("close the main window");
            TestAutomatedApp.Close();
            TestAutomatedApp = null;

            Assert.True(exitedFired, "Exited event did not fire.");
        }

        protected void CloseHelper()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            // test closing down the app
            Debug.WriteLine("close the main window");
            TestAutomatedApp.Close();

            Assert.Null(TestAutomatedApp.MainWindow);

            if (TestAutomatedApp is InProcessApplication)
            {
                Assert.Null((TestAutomatedApp as InProcessApplication).ApplicationDriver);
            }

            TestAutomatedApp = null;
        }

        protected void ApplicationDriverHelper()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            Assert.NotNull((TestAutomatedApp as InProcessApplication).ApplicationDriver);
        }

        #endregion Test Helpers
    }
}
