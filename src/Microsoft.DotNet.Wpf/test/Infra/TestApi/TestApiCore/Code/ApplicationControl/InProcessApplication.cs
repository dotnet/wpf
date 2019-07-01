// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Microsoft.Test.ApplicationControl
{
    /// <summary>
    /// Represents a test application running in the current process.
    /// </summary>
    ///
    /// <example>
    /// The following example demonstrates how to use this class. The code runs the target
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
    public class InProcessApplication : AutomatedApplication
    {
        /// <summary>
        /// Initializes a new instance of an InProcessApplication.
        /// </summary>
        /// <param name="settings">The settings used to start the test application.</param>
        public InProcessApplication(InProcessApplicationSettings settings)
        {
            ValidateApplicationSettings(settings);
            ApplicationSettings = settings;
        }

        #region Public Properties

        /// <summary>
        /// Access to the UI thread dispatcher.  
        /// </summary>
        /// <remarks>
        /// This is used only for the in-proc/separate thread scenario.
        /// </remarks>

        public object ApplicationDriver
        {
            get
            {
                if (ApplicationImplementation != null)
                {
                    return ApplicationImplementation.ApplicationDriver;
                }

                return null;
            }
        }

        /// <summary>
        /// The settings for the test application.
        /// </summary>
        public InProcessApplicationSettings ApplicationSettings
        {
            get;
            protected set;
        }

        #endregion Public Properties

        #region Override Members

        /// <summary>
        /// Creates and starts the test application. 
        /// </summary>        
        /// <remarks>
        /// Depending on the AutomatedApplicationType
        /// this can be on the same thread or on a separate thread.
        /// </remarks>        
        public override void Start()
        {
            if (IsApplicationRunning)
            {
                throw new InvalidOperationException("Cannot start an application instance if it is already running.");
            }

            IsApplicationRunning = true;

            if (ApplicationSettings.InProcessApplicationType == InProcessApplicationType.InProcessSeparateThread ||
                ApplicationSettings.InProcessApplicationType == InProcessApplicationType.InProcessSeparateThreadAndAppDomain)
            {
                var mainApplicationThread = new Thread(ApplicationStartWorker);
                mainApplicationThread.SetApartmentState(ApartmentState.STA);
                mainApplicationThread.Start();
            }
            else if (ApplicationSettings.InProcessApplicationType == InProcessApplicationType.InProcessSameThread)
            {
                ApplicationImplementation = ApplicationSettings.ApplicationImplementationFactory.Create(ApplicationSettings, null);
                ApplicationImplementation.MainWindowOpened += this.OnMainWindowOpened;
                ApplicationImplementation.Exited += this.OnExit;
                ApplicationImplementation.FocusChanged += this.OnFocusChanged;
                ApplicationImplementation.Start();
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    "Unable to start automated application with AutomatedApplicationType: {0}",
                    ApplicationSettings.InProcessApplicationType));
            }
        }

        /// <summary>
        /// Validates the settings for an InProcessApplication.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        private void ValidateApplicationSettings(InProcessApplicationSettings settings)
        {
            if (settings.ApplicationImplementationFactory == null)
            {
                throw new InvalidOperationException("ApplicationImplementationFactory must be specified.");
            }

            if (string.IsNullOrEmpty(settings.Path))
            {
                throw new InvalidOperationException("For InProc scenarios, Path cannot be null or empty.");
            }            
        }

        #endregion Override Members

        #region Private Methods       

        /// <summary>
        /// Thread worker that creates the AutomatedApplication implementation and 
        /// starts the application. If InProcessSeparateThreadAndAppDomain is specified
        /// then the AutomatedApplication implementation is created in a new AppDomain.
        /// </summary>        
        private void ApplicationStartWorker()
        {
            AppDomain testAppDomain = null;
            // TODO:  What do we do here with AppDomain?
            //if (ApplicationSettings.InProcessApplicationType == InProcessApplicationType.InProcessSeparateThreadAndAppDomain)
            //{
            //    var setup = new AppDomainSetup();
            //    setup.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //    testAppDomain = AppDomain.CreateDomain("Automated Application Domain", null, setup);
            //    ApplicationImplementation = ApplicationSettings.ApplicationImplementationFactory.Create(ApplicationSettings, testAppDomain);
            //}
            //else
            //{
                ApplicationImplementation = ApplicationSettings.ApplicationImplementationFactory.Create(ApplicationSettings, null);
            //}

            ApplicationImplementation.MainWindowOpened += this.OnMainWindowOpened;
            ApplicationImplementation.Exited += this.OnExit;
            ApplicationImplementation.FocusChanged += this.OnFocusChanged;
            ApplicationImplementation.Start();

            if (testAppDomain != null)
            {
                // run has completed, now unload this appdomain
                AppDomain.Unload(testAppDomain);
                testAppDomain = null;
                ApplicationImplementation = null;
            }
        }

        #endregion Private Methods
    }
}
