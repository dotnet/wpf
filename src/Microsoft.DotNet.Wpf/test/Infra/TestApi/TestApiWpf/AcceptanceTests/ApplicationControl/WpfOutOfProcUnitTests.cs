// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Test.ApplicationControl;
using Microsoft.Test.Input;
using Xunit;

namespace Microsoft.Test.AcceptanceTests.ApplicationControl
{
    public class WpfOutOfProcUnitTests : AutomatedApplicationTestBase
    {
        public WpfOutOfProcUnitTests()
        {
        }

        protected override void MyTestInitialize()
        {
            TestAutomatedApp = new OutOfProcessApplication(new OutOfProcessApplicationSettings
            {
                ProcessStartInfo = new ProcessStartInfo(TestApplicationPath),
                ApplicationImplementationFactory = new UIAutomationOutOfProcessApplicationFactory()
            });
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
            Assert.True(typeof(AutomationElement).IsAssignableFrom(TestAutomatedApp.MainWindow.GetType()));
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

        [Fact]
        public override void WaitForWindowTest()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Debug.WriteLine("get the dialog launch button from the main window");
            var mainWindow = TestAutomatedApp.MainWindow as AutomationElement;

            var launchButton = mainWindow.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.AutomationIdProperty,
                    "OpenButton",
                    PropertyConditionFlags.IgnoreCase))[0];

            Debug.WriteLine("click the button to launch the dialog");
            var invokePattern = launchButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            invokePattern.Invoke();

            Debug.WriteLine("wait for the window");
            TestAutomatedApp.WaitForWindow("TestDialog", DefaultTimeoutTimeSpan);

            var testDialogWindows = Microsoft.Test.AutomationUtilities.FindElementsById(AutomationElement.RootElement, "TestDialog");
            Assert.NotNull(testDialogWindows);
            Assert.Equal<int>(1, testDialogWindows.Count);
        }

        [Fact]
        public override void WaitForInputIdleTest()
        {
            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            var mainWindow = TestAutomatedApp.MainWindow as AutomationElement;

            var winPattern = mainWindow.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;

            winPattern.SetWindowVisualState(WindowVisualState.Maximized);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);
            Assert.Equal<WindowVisualState>(WindowVisualState.Maximized, winPattern.Current.WindowVisualState);

            winPattern.SetWindowVisualState(WindowVisualState.Minimized);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);
            Assert.Equal<WindowVisualState>(WindowVisualState.Minimized, winPattern.Current.WindowVisualState);

            winPattern.SetWindowVisualState(WindowVisualState.Normal);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);
            Assert.Equal<WindowVisualState>(WindowVisualState.Normal, winPattern.Current.WindowVisualState);
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
                    Debug.WriteLine(string.Format(
                        CultureInfo.CurrentCulture,
                        "Focus changed event fired. NewFocusedElement: {0}",
                        (e.NewFocusedElement as AutomationElement).Current.Name));
                    focusChangedList.Add((e.NewFocusedElement as AutomationElement).Current.Name);
                }));

            Debug.WriteLine("starting automated app...");
            TestAutomatedApp.Start();
            TestAutomatedApp.WaitForMainWindow(DefaultTimeoutTimeSpan);

            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            Debug.WriteLine("press tab");
            Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            // allow time for the events to fire
            Thread.Sleep(1500);

            Debug.WriteLine("press tab again");
            Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            // allow time for the events to fire
            Thread.Sleep(1500);

            Debug.WriteLine("press tab again");
            Keyboard.Press(Key.Tab);
            TestAutomatedApp.WaitForInputIdle(DefaultTimeoutTimeSpan);

            // allow time for the events to fire
            Thread.Sleep(1500);

            Assert.Equal<int>(4, focusChangedList.Count);
            Assert.Equal<string>("Window1", focusChangedList[0]);
            Assert.Equal<string>("Open", focusChangedList[1]);
            Assert.Equal<string>("Start Animation", focusChangedList[2]);
            Assert.Equal<string>("Debug", focusChangedList[3]);
        }

        [Fact]
        public override void ApplicationSettingsValidationTest()
        {
            // create AutomatedApplication with no path set
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var testApp = new OutOfProcessApplication(new OutOfProcessApplicationSettings());
                });
        }

        #endregion Base Tests
    }
}
