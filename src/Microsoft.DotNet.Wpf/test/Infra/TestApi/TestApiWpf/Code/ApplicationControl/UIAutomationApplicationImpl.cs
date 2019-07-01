// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Threading;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// UIAutomation application implementation for Out-of-Process scenario
    /// </summary>
    internal class UIAutomationApplicationImpl : IOutOfProcessAutomatedApplicationImpl
    {
        internal UIAutomationApplicationImpl(OutOfProcessApplicationSettings settings)
        {
            IsMainWindowOpened = false;
            this.settings = settings;
            this.windows = new List<AutomationElement>();

            Process = new Process();
            Process.StartInfo = settings.ProcessStartInfo;
            Process.EnableRaisingEvents = true;
            Process.Exited += this.OnApplicationExit;
                        
            Automation.AddAutomationEventHandler(
                WindowPatternIdentifiers.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Subtree,
                OnActivated);

            Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);            
        }
        
        #region Properties

        internal AutomationElement MainWindowAutomationElement
        {
            get
            {
                if (this.MainWindow != null)
                {
                    return this.MainWindow as AutomationElement;
                }
                return null;
            }
        }

        #endregion Properties

        #region IAutomatedApplicationImpl

        /// <summary>
        /// Gets the process associated with the application.
        /// </summary>
        public Process Process
        {
            get;
            private set;
        }

        /// <summary>
        /// Event fired when the main window AutomationEliement is opened
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
        /// Gets access to the MainWindow object which is an AutomationElement
        /// </summary>
        public object MainWindow
        {
            get;
            private set;
        }

        /// <summary>
        /// UIAutomation always returns null for this property
        /// </summary>
        public object ApplicationDriver
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Starts the application through System.Diagnostics.Process
        /// </summary>
        public void Start()
        {
            Process.Start();
        }

        /// <summary>
        /// Waits for the application to become idle
        /// </summary>
        /// <param name="timeSpan">the timeout interval</param>
        public void WaitForInputIdle(TimeSpan timeSpan)
        {
            if (Process != null && MainWindowAutomationElement != null)
            {             
                Process.WaitForInputIdle((int)timeSpan.TotalMilliseconds);
            }
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
                Thread.Sleep(10);
                timeout -= delta;
            }
        }

        /// <summary>
        /// Waits for the given window to open
        /// </summary>
        /// <param name="windowName">the window id of the window to wait for</param>
        /// <param name="timeout">the timeout interval</param>
        public void WaitForWindow(string windowName, TimeSpan timeout)
        {
            bool isWindowOpened = false;
            AutomationElement windowToWait = null;

            TimeSpan zero = TimeSpan.FromMilliseconds(0);
            TimeSpan delta = TimeSpan.FromMilliseconds(10);

            while (!isWindowOpened && timeout > zero)
            {
                Thread.Sleep(10);
                timeout -= delta;

                var elements = AutomationElement.RootElement.FindAll(
                   TreeScope.Children,
                   new PropertyCondition(
                       AutomationElement.AutomationIdProperty,
                       windowName,
                       PropertyConditionFlags.IgnoreCase));

                if (elements != null && elements.Count > 0)
                {
                    windowToWait = elements[0];
                }

                if (windowToWait != null)
                {
                    isWindowOpened = windowToWait.Current.IsEnabled;
                }
            }
        }

        /// <summary>
        /// Closes the application
        /// </summary>
        public void Close()
        {
            if (Process != null && !Process.HasExited)
            {
                // detach event handlers
                Automation.RemoveAllEventHandlers();

                var waitThread = new Thread(WaitForExit);
                waitThread.Start();

                // close process on new thread so exit event handlers can properly
                // be called and disposed
                var closeThread = new Thread(CloseProcessWorker);
                closeThread.Start();

                waitThread.Join(60000);    
            }
        }

        #endregion IAutomatedApplicationImpl

        #region Private Methods

        private void OnActivated(object sender, EventArgs e)
        {
            if (this.IsMainWindowOpened)
            {
                // keep track of windows that are launched so they can all be closed later
                var window = sender as AutomationElement;
                if (window != this.MainWindowAutomationElement && !windows.Contains(window))
                {
                    windows.Add(window);
                }

                return;
            }

            this.MainWindow = AutomationElement.FromHandle(Process.MainWindowHandle);

            if (MainWindowOpened != null)
            {
                MainWindowOpened(this, e);
            }

            this.IsMainWindowOpened = true;
        }

        private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs e)
        {
            if (FocusChanged != null)
            {
                FocusChanged(sender, e);
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            Process.Exited -= OnApplicationExit;
            if (Exited != null)
            {
                Exited(this, e);
            }
        }

        private void CloseProcessWorker()
        {
            // close any child windows
            foreach (AutomationElement elem in windows)
            {
                var winPattern = elem.GetCurrentPattern(WindowPattern.Pattern) as WindowPattern;
                winPattern.Close();
            }

            Process.CloseMainWindow();
            Process.Close();
        }

        private void WaitForExit()
        {
            Process.WaitForExit(60000);
        }

        #endregion Private Methods

        #region Private Fields

        private OutOfProcessApplicationSettings settings;
        private List<AutomationElement> windows;

        #endregion Private Fields
    }
}
