// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Collections.Generic;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Loads and starts a test application either in the current process or in a new, 
    /// separate process.
    /// </summary>
    ///
    /// <example>
    /// The following example shows in-process usage. The code runs the target
    /// application in a separate thread within the current process.
    /// <code>
    /// public void MyTest()
    /// {
    ///    var path = Path.Combine(executionDir, "WpfTestApplication.exe");
    ///    var a = new InProcessApplication(new WpfInProcessApplicationSettings
    ///    {
    ///         Path = path,
    ///         InProcessApplicationType = InProcessApplicationType.InProcessSeparateThread,
    ///         ApplicationImplementationFactory = new WpfInProcessApplicationFactory()
    ///    });
    /// 
    ///    a.Start();
    ///    a.WaitForMainWindow(TimeSpan.FromMilliseconds(5000));
    ///    
    ///    // Perform various tests...
    ///    
    ///    a.Close();
    /// }
    /// </code>
    /// </example>
    ///
    /// <example>
    /// The following example demonstrates out-of-process usage:
    /// <code>
    /// public void MyTest()
    /// {
    ///    var path = Path.Combine(executionDir, "WpfTestApplication.exe");
    ///    var a = new OutOfProcessApplication(new OutOfProcessApplicationSettings
    ///    {
    ///        ProcessStartInfo = new ProcessStartInfo(path),
    ///        ApplicationImplementationFactory = new UIAutomationOutOfProcessApplicationFactory()
    ///    });
    ///  
    ///    a.Start();
    ///    a.WaitForMainWindow(TimeSpan.FromMilliseconds(5000));
    ///    
    ///    // Perform various tests...
    ///    
    ///    a.Close();
    /// }
    /// </code>
    /// </example>
    public abstract class AutomatedApplication : MarshalByRefObject
    {
        #region Constructor

        /// <summary>
        /// AutomatedApplication objects are instantiated internally.
        /// </summary>
        protected AutomatedApplication()
        {
            IsApplicationRunning = false;
            _eventHandlers = new Dictionary<AutomatedApplicationEventType, List<Delegate>>();
        }

        #endregion Constructor

        #region Events

        /// <summary>
        /// Notifies listeners that the MainWindow of the test application has opened.
        /// </summary>
        public event EventHandler<AutomatedApplicationEventArgs> MainWindowOpened;

        /// <summary>
        /// Notifies listeners that the test application has exited.
        /// </summary>
        public event EventHandler<AutomatedApplicationEventArgs> Exited;

        #endregion Events

        #region Public Properties

        /// <summary>
        /// The main window of the test application. 
        /// </summary>
        /// <remarks>
        /// This is an AutomationElement for an OutOfProcessApplication and a System.Windows.Window
        /// for an InProcessApplication.
        /// </remarks>
        public object MainWindow
        {
            get
            {
                if (ApplicationImplementation != null)
                {
                    return ApplicationImplementation.MainWindow;
                }

                return null;
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Starts the test application after validating its settings.
        /// </summary>
        /// <remarks>
        /// Refined abstractions are expected to initialize AutomatedAppImp
        /// and call AutomatedAppImp.Start().
        /// </remarks>
        public abstract void Start();

        /// <summary>
        /// Waits for the test application to display its main window.
        /// </summary>
        /// <remarks>
        /// Blocks execution of the current thread until the window is displayed 
        /// or until the specified timeout interval elapses.
        /// </remarks>
        /// <param name="timeout">The timeout interval.</param>
        public void WaitForMainWindow(TimeSpan timeout)
        {
            if (!IsApplicationRunning)
            {
                return;
            }

            TimeSpan zero = TimeSpan.FromMilliseconds(0);
            TimeSpan delta = TimeSpan.FromMilliseconds(10);

            // must first wait for for AutomatedAppImp to be initialized
            while (ApplicationImplementation == null && timeout > zero)
            {
                Thread.Sleep(10);
                timeout -= delta;
            }

            if (ApplicationImplementation != null)
            {
                // then do the wait implementation
                ApplicationImplementation.WaitForMainWindow(timeout);
            }
        }

        /// <summary>
        /// Waits for the test application to display a window with a specified name.
        /// </summary>
        /// <remarks>
        /// Blocks execution of the current thread until the window is displayed 
        /// or until the specified timeout interval elapses.
        /// </remarks>
        /// <param name="windowName">The window to wait for.</param>
        /// <param name="timeout">The timeout interval.</param>
        public void WaitForWindow(string windowName, TimeSpan timeout)
        {
            if (!IsApplicationRunning)
            {
                return;
            }

            ApplicationImplementation.WaitForWindow(windowName, timeout);
        }

        /// <summary>
        /// Closes the automated application gracefully.
        /// </summary>
        public virtual void Close()
        {
            if (ApplicationImplementation != null)
            {
                ApplicationImplementation.MainWindowOpened -= this.OnMainWindowOpened;
                ApplicationImplementation.FocusChanged -= this.OnFocusChanged;
                ApplicationImplementation.Close();
                ApplicationImplementation = null;
            }

            IsApplicationRunning = false;
        }

        /// <summary>
        /// Waits for the test application to enter an idle state.
        /// </summary>
        /// <remarks>
        /// Blocks execution of the current thread until the window is displayed 
        /// or until the specified timeout interval elapses.
        /// </remarks>
        /// <param name="timeout">The timeout interval.</param>
        public void WaitForInputIdle(TimeSpan timeout)
        {
            if (!IsApplicationRunning)
            {
                return;
            }

            ApplicationImplementation.WaitForInputIdle(timeout);
        }

        /// <summary>
        /// Adds an event handler for the given AutomatedApplicationEventType.
        /// </summary>
        /// <param name="eventType">The type of event to listen for.</param>
        /// <param name="handler">The delegate to be called when the event occurs.</param>
        public void AddEventHandler(AutomatedApplicationEventType eventType, Delegate handler)
        {
            if (_eventHandlers.ContainsKey(eventType))
            {
                if (!_eventHandlers[eventType].Contains(handler))
                {
                    _eventHandlers[eventType].Add(handler);
                }
            }
            else
            {
                _eventHandlers.Add(eventType, new List<Delegate>());
                _eventHandlers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Removes an event handler for the given AutomatedApplicationEventType.
        /// </summary>
        /// <param name="eventType">The type of event to remove.</param>
        /// <param name="handler">The delegate to remove.</param>
        public void RemoveEventHandler(AutomatedApplicationEventType eventType, Delegate handler)
        {
            if (_eventHandlers.ContainsKey(eventType))
            {
                if (_eventHandlers[eventType].Contains(handler))
                {
                    _eventHandlers[eventType].Remove(handler);
                }
            }
        }

        #endregion Public Methods

        #region Internal and Protected Properties

        /// <summary>
        /// Gets or sets the automated application implementation.  
        /// </summary>
        /// <remarks>
        /// This is the 'implementation' following the bridge pattern.
        /// </remarks>
        protected IAutomatedApplicationImpl ApplicationImplementation
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the main window of the test application is open.
        /// </summary>
        protected bool IsMainWindowOpened
        {
            get
            {
                if (ApplicationImplementation != null)
                {
                    return ApplicationImplementation.IsMainWindowOpened;
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates whether the test application is running.
        /// </summary>
        protected bool IsApplicationRunning
        {
            get;
            set;
        }

        #endregion Internal and Protected Properties

        #region Internal and Protected Methods

        /// <summary>
        /// Wrapper to signal the MainWindowOpened event
        /// </summary>
        /// <param name="sender">The source of the event which is IAutomatedApplicationImpl.</param>
        /// <param name="e">An System.EventArgs that contains no event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected void OnMainWindowOpened(object sender, EventArgs e)
        {
            if (MainWindowOpened != null)
            {
                MainWindowOpened(this, new AutomatedApplicationEventArgs(this));
            }

            if (_eventHandlers.ContainsKey(AutomatedApplicationEventType.MainWindowOpenedEvent))
            {
                foreach (Delegate handler in _eventHandlers[AutomatedApplicationEventType.MainWindowOpenedEvent])
                {
                    var openHandler = handler as EventHandler<AutomatedApplicationEventArgs>;
                    if (openHandler != null)
                    {
                        openHandler(this, new AutomatedApplicationEventArgs(this));
                    }
                }
            }
        }

        /// <summary>
        /// Wrapper to signal the FocusChanged event
        /// </summary>
        /// <param name="sender">The source of the event which is IAutomatedApplicationImpl.</param>
        /// <param name="e">An System.EventArgs that contains no event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected void OnFocusChanged(object sender, EventArgs e)
        {
            if (_eventHandlers.ContainsKey(AutomatedApplicationEventType.FocusChangedEvent))
            {
                foreach (Delegate handler in _eventHandlers[AutomatedApplicationEventType.FocusChangedEvent])
                {
                    var focusHandler = handler as EventHandler<AutomatedApplicationFocusChangedEventArgs>;
                    if (focusHandler != null)
                    {
                        focusHandler(this, new AutomatedApplicationFocusChangedEventArgs(this, sender));
                    }
                }
            }
        }

        /// <summary>
        /// Wrapper to signal the Exited event
        /// </summary>
        /// <param name="sender">The source of the event which is IAutomatedApplicationImpl.</param>
        /// <param name="e">An System.EventArgs that contains no event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2109:ReviewVisibleEventHandlers")]
        protected void OnExit(object sender, EventArgs e)
        {
            if (Exited != null)
            {
                Exited(this, new AutomatedApplicationEventArgs(this));
            }

            if (_eventHandlers.ContainsKey(AutomatedApplicationEventType.ApplicationExitedEvent))
            {
                foreach (Delegate handler in _eventHandlers[AutomatedApplicationEventType.ApplicationExitedEvent])
                {
                    var exitHandler = handler as EventHandler<AutomatedApplicationEventArgs>;
                    if (exitHandler != null)
                    {
                        exitHandler(this, new AutomatedApplicationEventArgs(this));
                    }
                }
            }

            if (ApplicationImplementation != null)
            {
                ApplicationImplementation.Exited -= this.OnExit;
            }

            // remove all event handlers
            foreach (var kvp in _eventHandlers)
            {
                kvp.Value.Clear();
            }
        }

        #endregion Internal and Protected Methods

        #region Private Fields

        /// <summary>
        /// Holds a table of general event handlers for AutomatedApplication
        /// </summary>
        private Dictionary<AutomatedApplicationEventType, List<Delegate>> _eventHandlers;

        #endregion Private Fields
    }
}
