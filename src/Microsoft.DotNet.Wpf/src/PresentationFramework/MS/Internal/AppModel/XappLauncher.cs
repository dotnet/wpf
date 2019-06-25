// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Deployment.Application;
using System.Runtime.Remoting;
using System.Security;
using System.Security.Policy;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Internal.Utility;
using Microsoft.Internal.DeploymentUI;
using Microsoft.Win32;
using System.Reflection;
using MS.Utility;
using System.Windows.Input;
using System.Security.Permissions;
using System.Threading;

namespace MS.Internal.AppModel
{
    internal class XappLauncherApp : Application
    {
        internal XappLauncherApp(Uri deploymentManifest, string applicationId,
            IBrowserCallbackServices browser, DocObjHost.ApplicationRunnerCallback applicationRunner,
            INativeProgressPage nativeProgressPage,
            string progressPageAssembly, string progressPageClass, string errorPageAssembly, string errorPageClass)
        {
            _deploymentManifest = deploymentManifest;
            _applicationId = applicationId;
            _browser = browser;
            _applicationRunnerCallback = applicationRunner;
            _fwlinkUri = null;
            _requiredCLRVersion = null;
            this.Startup += new StartupEventHandler(XappLauncherApp_Startup);
            this.Exit += new ExitEventHandler(XappLauncherApp_Exit);
            this.Navigated += new NavigatedEventHandler(XappLauncherApp_Navigated);

            _nativeProgressPage = nativeProgressPage;
            _progressPageAssembly = progressPageAssembly;
            _progressPageClass = progressPageClass;
            _errorPageAssembly = errorPageAssembly;
            _errorPageClass = errorPageClass;
        }

        void OnCommandRefresh(object sender, RoutedEventArgs e)
        {
            HandleRefresh();
        }

        void OnCommandStop(object sender, RoutedEventArgs e)
        {
            UserStop();
        }

        void XappLauncherApp_Startup(object sender, StartupEventArgs e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_XappLauncherAppStartup);

            CreateApplicationIdentity();
            if (_identity != null)
            {
                TryApplicationIdActivation();
            }
            else
            {
                TryUriActivation();
            }
        }

        void XappLauncherApp_Exit(object sender, ExitEventArgs e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_XappLauncherAppExit, _runApplication);

            Invariant.Assert(!_isInAsynchronousOperation,
                "Async downloading should have been canceled before XappLauncherApp exits.");

            RunApplicationAsyncCallback(null);
            
            _browser = null;
            _applicationRunner = null;
            _applicationRunnerCallback = null;
        }


        void XappLauncherApp_Navigated(object sender, NavigationEventArgs e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_XappLauncherAppNavigated);

            if (IsShuttingDown)
                return;

            if (!_commandBindingsRegistered)
            {
                _commandBindingsRegistered = true;

                // These bindings handle the commands sent by the browser when the stop/refresh buttons are pressed. If nothing in the
                // page has focus, they will be sent directly to the window.
                // SP2 Update: DocObjHost.ExecCommand() now also handles these commands, either directly from
                // the browser or from the native progress page.
                MainWindow.CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseStop, new ExecutedRoutedEventHandler(OnCommandStop)));
                MainWindow.CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, new ExecutedRoutedEventHandler(OnCommandRefresh)));
            }

            NavigationWindow navWin = GetAppWindow();
            Invariant.Assert(navWin != null, "A RootBrowserWindow should have been created.");
            while (navWin.CanGoBack)
            {
                navWin.RemoveBackEntry();
            }
        }

        void StartAsynchronousOperation()
        {
            Debug.Assert(!_isInAsynchronousOperation && !IsCanceledOrShuttingDown);
            _isInAsynchronousOperation = true;
            ChangeBrowserDownloadState(_isInAsynchronousOperation);
        }

        void ClearAsynchronousOperationStatus()
        {
            _isInAsynchronousOperation = false;
            ChangeBrowserDownloadState(_isInAsynchronousOperation);
        }


        private object UserRefresh(object unused)
        {
            HandleRefresh();
            return null;
        }

        internal override void PerformNavigationStateChangeTasks(
            bool isNavigationInitiator, bool playNavigatingSound, NavigationStateChange state)
        {
            // Do not play sounds or start and stop the globe on when navigations
            // occur because the progress page is merely an enhanced visual experience
            // during the actual navigation to the application.  Conceptually it  should
            // appear as something that happens during navigation and not a series of
            // discrete navigations.

            // We do need to ensure that the Stop and Refresh buttons are in the correct state
            // while downloading the app.
            if (isNavigationInitiator && state == NavigationStateChange.Completed)
            {
                UpdateBrowserCommands();
            }
        }

        // This function gets called when the browser refresh button in clicked.
        // This'll cause the browser to navigate the address bar
        internal void HandleRefresh()
        {
            lock (_lockObject) // we do this in case the refresh button is getting clicked rapidly, before the navigation happens
            {
                if (!_refreshing)
                {
                    _refreshing = true;
                    BrowserCallbackServices.DelegateNavigation(_deploymentManifest.ToString(), null, null);
                }
            }
        }

        private void ChangeBrowserDownloadState(bool newState)
        {
            // start or stop waving the flag
            // When shutting down, which may happen during deployment, IBrowserCallbackServices may become
            // unavailable. Calling ChangeBrowserDownloadState(false) should not trigger an exception then.
            try
            {
                _browser.ChangeDownloadState(newState);
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidComObjectException || ex is COMException || ex is InvalidOperationException) ||
                    newState || !IsShuttingDown)
                {
                    throw;
                }
            }
        }

        private void TryApplicationIdActivation()
        {
            Dispatcher.Invoke(
                DispatcherPriority.Input,
                new DispatcherOperationCallback(DoDirectActivation),
                null);
        }

        private void TryUriActivation()
        {
            if(_hasTriedUriActivation)
            {
                Shutdown();
            }
            else
            {
                _hasTriedUriActivation = true;
                
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.WpfHost_FirstTimeActivation);

                // IPHM created on a worker thread. See threading comment on _hostingManager.
                ThreadStart d = delegate
                {
                    _hostingManager = new InPlaceHostingManager(_deploymentManifest);

                    // Continue on the UI thread.
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new SendOrPostCallback(delegate 
                    {
                        // Ordering is important here - downloading the manifest is done asynchronously
                        // so can be started before we spend time setting up the UI.  This saves us some
                        // time - especially during cold-start scenarios.

                        DoGetManifestAsync();

                        DoDownloadUI();
                    }), null);
                };
                d.BeginInvoke(null, null);
            }
        }

        private object DoDirectActivation(object unused)
        {
            if (IsCanceledOrShuttingDown)
                return null;

            try
            {
                // Verify that this app is actually cached. This is because the call to
                // CreatePartialActivationContext can succeed when we don't want it to;
                // it appears to be insensitive to some parts of the ApplicationIdentity,
                // or it tries to make things work when they really should fail. Looking
                // at the UserApplicationTrusts does a better comparison.
                if (ApplicationSecurityManager.UserApplicationTrusts[_identity.ToString()] != null)
                {
                    _context = System.ActivationContext.CreatePartialActivationContext(_identity);
                    RunApplicationAsync(ExecuteDirectApplication);
                }
                else
                {
                    TryUriActivation();
                }
            }
            catch(Exception exception)
            {
                // Delete the cached trust decision to force going down the full ClickOnce path next time
                DeleteCachedApplicationTrust(_identity);

                // Fatal error like NullReferenceException and SEHException should not be ignored.
                if (exception is NullReferenceException || exception is SEHException)
                {
                    throw;
                }
                else
                {
                    TryUriActivation();
                }
            }

            return null;
        }
        
        private bool ExecuteDirectApplication()
        {
            _runApplication = false;
            
            try
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_ClickOnceActivationStart, KnownBoxes.BooleanBoxes.TrueBox);
                ObjectHandle oh = Activator.CreateInstance(_context);
                GotNewAppDomain(oh);

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_ClickOnceActivationEnd);

                Shutdown();

                return true;
            }
            catch (Exception exception)
            {
                // Delete the cached trust decision to force going down the full ClickOnce path next time
                DeleteCachedApplicationTrust(_identity);

                // Fatal error like NullReferenceException and SEHException should not be ignored.
                if (exception is NullReferenceException || exception is SEHException)
                {
                    throw;
                }
                else
                {
                    TryUriActivation();
                }
            }

            return false;
        }

        void GotNewAppDomain(ObjectHandle oh)
        {
            Invariant.Assert(PresentationAppDomainManager.SaveAppDomain);

            // In stress situations or with a corrupt application store, ClickOnce may fail to return us
            // an AppDomain, without throwing an exception. This might also happen if something goes wrong
            // with ApplicationActivator's use of our custom AppDomainManager
            if (oh != null)
            {
                AppDomain newDomain = (AppDomain)oh.Unwrap();
                if (newDomain != null)
                {
                    PresentationAppDomainManager.NewAppDomain = newDomain;
                    return;
                }
            }
            // Note that DocObjHost enables the unhandled exception page just before trying to run the
            // application, so this exception message should be displayed to the user (except if we are
            // trying direct activation, in which case the exception is caught and we fall back to the full
            // IPHM activation).
            throw new ApplicationException(SR.Get(SRID.AppActivationException));
        }

        private void DoGetManifestAsync()
        {
            if (IsCanceledOrShuttingDown)
                return;

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_DownloadDeplManifestStart);

            StartAsynchronousOperation();
            SetStatusText(SR.Get(SRID.HostingStatusDownloadAppInfo));

            _hostingManager.GetManifestCompleted += new EventHandler<GetManifestCompletedEventArgs>(GetManifestCompleted);
            // Possible reentrancy! When making the outgoing calls to the browser to update its
            // status (above), a pending incoming call can be dispatched. This may be OLECMDID_STOP,
            // which would lead to calling UserStop(), which makes IPHM unusable.
            if (!IsCanceledOrShuttingDown)
            {
                Debug.Assert(_isInAsynchronousOperation);
                _hostingManager.GetManifestAsync();
            }
        }

        private object GetCustomPage(string pageAssemblyName, string pageClassName)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_GetDownloadPageStart, pageClassName);

            object customPage;
            try
            {
                // Uses custom progress page
                // If the assembly is not specified, use PresentationUI.
                Assembly customPageAssembly = string.IsNullOrEmpty(pageAssemblyName) ? typeof(TenFeetInstallationProgress).Assembly : Assembly.Load(pageAssemblyName);
                customPage = customPageAssembly.CreateInstance(pageClassName);
            }
            catch
            {
                customPage = null;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_GetDownloadPageEnd);

            return customPage;
        }

        void GetManifestCompleted(object sender, GetManifestCompletedEventArgs e)
        {
            // Continue through a Background priority work item to ensure that the progress page has loaded--
            // we need at least the javascript functions to be available to call. Trident uses a sequence of 
            // message-based callbacks to do its work. Once we ensure the first callback is enqueued before 
            // the main thread starts pumping messages again, the sequence will complete before the Background
            // priority operation is dispatched. DoDownloadUI() is called right after DoGetManifestAsync(), so
            // the HTML loading will start before we return to the message loop.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(DoGetManifestCompleted), e);
        }

        private object DoGetManifestCompleted(object e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_DownloadDeplManifestEnd);

            GetManifestCompletedEventArgs args = (GetManifestCompletedEventArgs)e;
            _getManifestCompletedEventArgs = args;

            // Race condition: UserStop() can be called after InPlaceHostingManager has completed
            // the manifest downloading but before this callback is called.
            bool canceled = _canceled || args.Cancelled;

            ClearAsynchronousOperationStatus();

            if (IsShuttingDown)
                return null;
            if (args.Error != null)
            {
                // If the async operation failed, it is invalid to request the
                // SupportUri so we simply pass in null.
                HandleError(args.Error, args.LogFilePath, null, null);
                return null;
            }
            if (canceled)
            {
                HandleCancel();
                return null;
            }

            // args.ApplicationIdentity throws if the operation has been canceled. That's why the above check has
            // to come first.
            _identity = args.ApplicationIdentity;

            if (_progressPage != null)
            {
                _progressPage.ApplicationName = args.ProductName;

                // GetManifestCompletedEventArgs.PublisherName doesn't exist.
                // DevDiv bug 166088 tracks this.
                // The retrieval below takes surprisingly little time: < 1 ms.
                XmlReader rdr = args.DeploymentManifest;
                rdr.MoveToContent();
                if (rdr.LocalName == "assembly")
                {
                    rdr.ReadStartElement();
                    while(rdr.NodeType != XmlNodeType.EndElement)
                    {
                        if(rdr.LocalName == "description")
                        {
                            string publisher = rdr.GetAttribute("publisher", "urn:schemas-microsoft-com:asm.v2");
                            if (!string.IsNullOrEmpty(publisher))
                            {
                                _progressPage.PublisherName = publisher;
                            }
                            break;
                        }
                        rdr.Skip();
                    }
                }
            }

            //
            // Start asynchronous application downloading
            //
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_DownloadApplicationStart);
            ShowDownloadingStatusMessage();
            StartAsynchronousOperation();
            // Possible reentrancy! When making the outgoing calls to the browser to update its
            // status (above), a pending incoming call can be dispatched. This may be OLECMDID_STOP,
            // which would lead to calling UserStop(), which makes IPHM unusable.
            if (IsCanceledOrShuttingDown)
                return null;
            _hostingManager.DownloadProgressChanged += new EventHandler<DownloadProgressChangedEventArgs>(DownloadProgressChanged);
            _hostingManager.DownloadApplicationCompleted += new EventHandler<DownloadApplicationCompletedEventArgs>(DownloadApplicationCompleted);
            _hostingManager.DownloadApplicationAsync();

            // Cold-start optimization: While async downloading is underway, do AssertApplicationRequirements.
            // AAR takes 0.8-1.5+ seconds on cold start because it loads all assemblies from which permission
            // types are referenced.
            Dispatcher.BeginInvoke(DispatcherPriority.Input,
                new DispatcherOperationCallback(AssertApplicationRequirementsAsync), null);
            _assertAppRequirementsEvent = new ManualResetEvent(false);

            return null;
        }

        void ShowDownloadingStatusMessage()
        {
            SetStatusText(SR.Get(SRID.HostingStatusDownloadApp));
        }

        object AssertApplicationRequirementsAsync(object unused)
        {
            if (IsCanceledOrShuttingDown)
                return null;

            if (CheckAccess()) // initial call by Dispatcher?
            { // Do a callback to a worker thread 
                // Status bar is set on the main thread (here) to avoid switching.
                SetStatusText(SR.Get(SRID.HostingStatusVerifying));

                // Creating an STA thread to get CLR's 'selectively permeable' managed locks. This is needed to
                // enable the dispatching of the window event hook calls needed to intercept the showing of the
                // ClickOnce elevation prompt. See PresentationAppDomainManager.DetermineApplicationTrust().
                Thread thread = new Thread(() => AssertApplicationRequirementsAsync(null));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Name = "IPHM.AAR thread";
                thread.Start();
            }
            else // on a worker thread
            {
                try
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AssertAppRequirementsStart);

                    _hostingManager.AssertApplicationRequirements();

                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_AssertAppRequirementsEnd);

                    // Switch to the main thread to update the status message. This is necessary because the
                    // native IBCS and INativeProgressPage interfaces are not marshalable.
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(
                        delegate(object unused3)
                        {
                            if (_isInAsynchronousOperation)
                            {
                                ShowDownloadingStatusMessage();
                            }
                            return null;
                        }), null);
                }
                catch (Exception exception0)
                {
                    if (exception0.GetType() == typeof(InvalidOperationException) &&
                        exception0.Source == "System.Deployment")
                    { // "No further operations are possible with this instance":
                        // This condition can occur when IPHM.AssertApplicationRequirements() is called after application
                        // downloading has failed. That error may or may not have been reported to the main thread yet.
                        // Presumably, it is the "real" error that failed deployment and should be reported to the user.
                        // That's why no further handling is done here, and _assertAppRequirementsFailed is not set.
                    }
                    else
                    {
                        _assertAppRequirementsFailed = true;

                        // Note that an exception allowed to escape from here will be caught by the thread-pool manager.
                        // That's why all further processing is moved to the main thread.
                        Dispatcher.BeginInvoke(DispatcherPriority.Send, new DispatcherOperationCallback(
                            delegate(object exceptionObj)
                            {
                                Exception exception = (Exception)exceptionObj;
                                if (CriticalExceptions.IsCriticalException(exception))
                                { // Wrap the exception object in order to preserve the original callstack.
                                    throw new DeploymentException(SR.Get(SRID.UnknownErrorText), exception);
                                }

                                GetManifestCompletedEventArgs args = _getManifestCompletedEventArgs;
                                string version = null;
                                if (exception is TrustNotGrantedException)
                                {
                                    version = GetMissingCustomPermissionVersion(args.ApplicationManifest);
                                    if (!string.IsNullOrEmpty(version))
                                    {
                                        exception = new DependentPlatformMissingException();
                                    }
                                }

                                HandleError(exception, args.LogFilePath, args.SupportUri, version);
                                return null;
                            }), exception0);
                    }
                }
                finally
                {
                    // Synchronize with DoDownloadApplicationCompleted(). [See explanation there.]
                    _assertAppRequirementsEvent.Set();
                }
            }
            return null;
        }

        /// <remarks> Nested message pumping should not be allowed within this method. See the synchronization
        /// issue with deployment manifest downloading explained in GetManifestCompleted().
        /// </remarks>
        private void DoDownloadUI()
        {
            // ASSUMES ALREADY IN CORRECT CONTEXT

            // Note: The custom progress page support was provided for Media Center. Since MC has
            // deprecated XBAP support, we'll likely counter-deprecate.

            bool usingCustomPage = true;
            if (_progressPageClass != null)
            {
                _progressPage = GetCustomPage(_progressPageAssembly, _progressPageClass) as IProgressPage;
            }
            // If we failed to get a custom page, or didn't even try, use our default.
            if (_progressPage == null)
            {
                usingCustomPage = false;
                Invariant.Assert(_nativeProgressPage != null);
                _progressPage = new NativeProgressPageProxy(_nativeProgressPage);
            }

            _progressPage.DeploymentPath = _deploymentManifest;
            _progressPage.StopCallback = new DispatcherOperationCallback(UserStop);
            _progressPage.RefreshCallback = new DispatcherOperationCallback(UserRefresh);

            // A "custom" page is a managed one. It needs to be loaded in the RootBrowserWindow.
            // The RBW is not created in the default AppDomain otherwise. This is what saves significantly
            // from the startup time, especially on cold start.
            if (usingCustomPage)
            {
                BrowserWindow.Navigate(_progressPage);
            }
            else
            {
                // Note: The native progress page may have been shown, due to the cold start heuristic.
                _nativeProgressPage.Show();
                //Q: What ever hides the native progress page?
                //A: IBrowserCallbackServices.OnBeforeShowNavigationWindow() (in CColeDocument).
                //  This arrangement covers the RBW being shown in either AppDomain. (In the default AppDomain
                //  we may have to show the deployment failed/canceled page or a custom progress page.)
            }
        }

        private void HandleError(Exception exception, string logFilePath, Uri supportUri, string requiredWpfVersion)
        {

            ClearAsynchronousOperationStatus();

            // Delete the cached trust decision to force going down the full ClickOnce path next time
            DeleteCachedApplicationTrust(_identity);

            // If we are being shut down by the browser, don't do anything else.
            if (IsShuttingDown)
            {
                AbortActivation();
                return;
            }

            // ASSUMES ALREADY IN CORRECT CONTEXT
            SetStatusText(SR.Get(SRID.HostingStatusFailed));
            string version = String.Empty;
            MissingDependencyType getWinFXReq = MissingDependencyType.Others;

            if (exception is DependentPlatformMissingException)
            {
                if (requiredWpfVersion != null)
                {
                    getWinFXReq = MissingDependencyType.WinFX;
                    version = requiredWpfVersion;
                    DeploymentExceptionMapper.ConstructFwlinkUrl(version, out _fwlinkUri);
                    _requiredCLRVersion = ClrVersionFromWinFXVersion(version);
                }
                else
                {
                    getWinFXReq = DeploymentExceptionMapper.GetWinFXRequirement(exception, _hostingManager, out version, out _fwlinkUri);
                    switch(getWinFXReq)
                    {
                        case MissingDependencyType.WinFX:
                            _requiredCLRVersion = ClrVersionFromWinFXVersion(version);
                            break;
                        case MissingDependencyType.CLR:
                            _requiredCLRVersion = version;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(_requiredCLRVersion))
            {
	            // This scenario is possible of we have a valid WPF version that we could not map to known CLR version
                // We have no scheme that can reliably predict how to install future versions of the framework.
                // This code prevents us from offering an FWLink or activation button for future versions of the Framework                
                getWinFXReq = MissingDependencyType.Others;
            }
            
            string errorTitle, errorMessage;

            switch(getWinFXReq)
            {
                case MissingDependencyType.WinFX:
                    // Wrong version of Avalon is installed.
                    errorTitle = SR.Get(SRID.PlatformRequirementTitle);
                    errorMessage = SR.Get(SRID.IncompatibleWinFXText, version);
                    break;
                case MissingDependencyType.CLR:
                    // Missing CLR dependency
                    errorTitle = SR.Get(SRID.PlatformRequirementTitle);
                    errorMessage = SR.Get(SRID.IncompatibleCLRText, version);
                    break;
                default:
                    // All other deployment exceptions
                    DeploymentExceptionMapper.GetErrorTextFromException(exception, out errorTitle, out errorMessage);
                    break;
            }

            IErrorPage errorpage = null;

            if (_errorPageClass != null)
            {
                errorpage = GetCustomPage(_errorPageAssembly, _errorPageClass) as IErrorPage;
            }
            // If we failed to get a custom page, or didn't even try, use our default.
            if (errorpage == null)
            {
                //use default class
                errorpage = new InstallationErrorPage() as IErrorPage;
            }

            errorpage.DeploymentPath = _deploymentManifest;
            errorpage.ErrorTitle = errorTitle;
            errorpage.ErrorText = errorMessage;
            errorpage.SupportUri = supportUri;
            errorpage.LogFilePath = logFilePath;
            errorpage.RefreshCallback = new DispatcherOperationCallback(UserRefresh);
            errorpage.GetWinFxCallback = (getWinFXReq != MissingDependencyType.Others)? new DispatcherOperationCallback(GetWinFX) : null;
            errorpage.ErrorFlag = true;

            BrowserWindow.Navigate(errorpage);
        }

        private void HandleCancel()
        {

            // After _runApplication is set to true, we no longer allow canceling deployment.
            if (_cancelHandled || _runApplication)
                return;
            _cancelHandled = _canceled = true;

            // Delete the cached trust decision to force going down the full ClickOnce path next time
            DeleteCachedApplicationTrust(_identity);

            // If we are being shut down by the browser, don't do anything else.
            if (IsShuttingDown)
            {
                AbortActivation();
                return;
            }

            CancelAsynchronousOperation();

            // ASSUMES ALREADY IN CORRECT CONTEXT
            SetStatusText(SR.Get(SRID.HostingStatusCancelled));
            string errorTitle, errorMessage;
            DeploymentExceptionMapper.GetErrorTextFromException(null, out errorTitle, out errorMessage);
            IErrorPage errorpage = null;
            //dont even try to use reflection if assembly name is null
            if (_errorPageAssembly != null || _errorPageClass != null)
            {
                errorpage = GetCustomPage(_errorPageAssembly, _errorPageClass) as IErrorPage;
            }
            //if this is null then there is no custom page so fall back to default ui
            if (errorpage == null)
            {
                //use default class
                errorpage = new InstallationErrorPage() as IErrorPage;
            }

            errorpage.DeploymentPath = _deploymentManifest;
            errorpage.ErrorTitle = errorTitle;
            errorpage.ErrorText = errorMessage;
            errorpage.SupportUri = null;
            errorpage.LogFilePath = null;
            errorpage.ErrorFlag = false;
            errorpage.RefreshCallback = new DispatcherOperationCallback(UserRefresh);
            errorpage.GetWinFxCallback = null;

            BrowserWindow.Navigate(errorpage);
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            _bytesDownloaded = e.BytesDownloaded;
            _bytesTotal = e.TotalBytesToDownload;

            if (!_updatePending)
            {
                _updatePending = true;
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new DispatcherOperationCallback(DoDownloadProgressChanged),
                    null);
            }
        }

        private object DoDownloadProgressChanged(object unused)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_DownloadProgressUpdate, _bytesDownloaded, _bytesTotal);

            // !_isInAsynchronousOperation implies activation failed or was canceled.
            if (!_isInAsynchronousOperation || IsShuttingDown)
                return null;
            Debug.Assert(!_canceled);


            if (_progressPage != null)
            {
                _progressPage.UpdateProgress(_bytesDownloaded, _bytesTotal);
            }
            _updatePending = false;

            return null;
        }

        void DownloadApplicationCompleted(object sender, DownloadApplicationCompletedEventArgs e)
        {
            _hostingManager.DownloadProgressChanged -= new EventHandler<DownloadProgressChangedEventArgs>(DownloadProgressChanged);

            // UPDATE: The explanation below is from the time when IPHM was created on the GUI (main) thread.
            // Now BeginInvoke() is needed merely to switch to that thread from whatever worker thread IPHM is 
            // raising this event on.
            // 
            // Using BeginInvoke() to avoid a deadlock. InPlaceHostingManager allows only one thread to run
            // any one of its methods at a time. The DownloadApplicationCompleted event is raised on the main
            // thread but under IPHM's internal lock. Our handler (DoDownloadApplicationCompleted) needs to
            // synchronize with the thread that calls IPHM.AssertApplicationRequirements(). That thread may
            // be trying to call AAR(), which needs to take IPHM's internal lock. But it's already taken by the
            // code that raises the DownloadApplicationCompleted event, and in our handler we wait on AAR() to
            // complete. => deadlock
            //
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(DoDownloadApplicationCompleted), e);
        }

        private object DoDownloadApplicationCompleted(object e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_DownloadApplicationEnd);

            DownloadApplicationCompletedEventArgs args = (DownloadApplicationCompletedEventArgs)e;

            ClearAsynchronousOperationStatus();
            // Shutdown may have started and IBrowserCallbackServices may have become unavailable.
            // In particular, making calls to the browser, just like above, could cause a pending shutdown
            // call to be dispatched.
            if (IsShuttingDown)
                return null;

            if (args.Error != null)
            {
                // Synchronizing with AssertApplicationRequirementsAsync(). (If no error here, AAR must have
                // succeeded too.) IPHM could throw exceptions on both threads, in any order. We should
                // handle only one, and we prefer the one from AAR as it tends to be more meaningful, except
                // in the case when it's InvalidOperationException due to an error already encountered during
                // application download and IPHM getting set to a dead state internally.
                _assertAppRequirementsEvent.WaitOne();
                if (!_assertAppRequirementsFailed)
                {
                    HandleError(args.Error, args.LogFilePath, _getManifestCompletedEventArgs.SupportUri, null);
                }
                return null;
            }

            // Race condition: UserStop() can be called after InPlaceHostingManager has completed
            // the downloading but before our callback is called.
            bool canceled = _canceled || args.Cancelled;
            if (canceled)
            {
                HandleCancel();
                return null;
            }

            SetStatusText(SR.Get(SRID.HostingStatusPreparingToRun));
            // This is the last progress message set. It is cleared by
            // IBrowserCallbackServices.OnBeforeShowNavigationWindow() (in CColeDocument).

            // Go through the Dispatcher at a lower priority to allow the above status message to render.
            // There can be a signficant delay between the end of downloading and the rendering of the first
            // application page, especially on cold start.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(
                delegate(object unused)
                {
                    if (!IsCanceledOrShuttingDown)
                    {
                        RunApplicationAsync(ExecuteDownloadedApplication);
                    }
                    return null;
                }), null);

            return null;
        }

        private bool ExecuteDownloadedApplication()
        {
            _runApplication = false;
            
            try 
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_ClickOnceActivationStart, KnownBoxes.BooleanBoxes.FalseBox);

                ObjectHandle oh = _hostingManager.Execute();
                GotNewAppDomain(oh);

                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_ClickOnceActivationEnd);

                return true;
            }
            finally 
            {
                Shutdown();
            }
        }
        
        private object UserStop(object unused)
        {
            UserStop();
            return null;
        }
        
        internal void UserStop()
        {
            if (_isInAsynchronousOperation)
            {
                CancelAsynchronousOperation();
            }
            else
            {
                HandleCancel();
            }

        }

        internal void AbortActivation()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting, EventTrace.Event.WpfHost_AbortingActivation);

            CancelAsynchronousOperation();
            _runApplication = false;
            Shutdown(ERROR_ACTIVATION_ABORTED);
        }

        private void CancelAsynchronousOperation()
        {
            lock (_lockObject)
            {
                if (_isInAsynchronousOperation)
                {
                    Debug.Assert(!_canceled);
                    _canceled = true;
                    Invariant.Assert(_hostingManager != null, "_hostingManager should not be null if _isInAsynchronousOperation is true");
                    _hostingManager.CancelAsync();
                    ClearAsynchronousOperationStatus();
                }
            }
        }

        private bool IsCanceledOrShuttingDown
        {
            get
            {
                Debug.Assert(!(_canceled && _isInAsynchronousOperation));
                return _canceled || IsShuttingDown;
            }
        }

    #if DEBUG
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    #endif
        private RootBrowserWindow BrowserWindow
        {
            get
            {
                RootBrowserWindow rbw = (RootBrowserWindow)GetAppWindow();
                Invariant.Assert(rbw != null, "Should have instantiated RBW if it wasn't already there");
                rbw.ShowsNavigationUI = false; // not needed and not RightToLeft-enabled in this context
                return rbw;
            }
        }

        private void CreateApplicationIdentity()
        {
            _identity = null;
            if (_applicationId != null)
            {
                try
                {
                    _identity = new ApplicationIdentity(_applicationId);
                }
                catch(NullReferenceException)
                {
                    throw;
                }
                catch(SEHException)
                {
                    throw;
                }
                catch(Exception)
                {
                }
            }
        }

        /// <summary>
        /// This is necessary as a workaround for various ClickOnce issues and the interaction
        /// between cached trust decisions and our direct activation shortcut. If there is a
        /// cached trust decision for a given ApplicationIdentity, we will try the direct
        /// activation shortcut. If something goes wrong, we want to delete that cached trust
        /// decision, so that next time we will be forced to go down the offical ClickOnce
        /// deployment pathway.
        /// </summary>
        private void DeleteCachedApplicationTrust(ApplicationIdentity identity)
        {
            if (identity != null)
            {
                ApplicationTrust trust = new ApplicationTrust(identity);
                // This does not throw if the trust isn't there.
                ApplicationSecurityManager.UserApplicationTrusts.Remove(trust);
            }
        }

        ///<summary>
        /// This function is private to xapplauncher. It invokes the default browser with Uri
        /// to WinFXSetup.exe.  Its called from the "Install WinFX" button on the error page
        /// shown when an app requests a different version of WinFX than the one installed.
        ///</summary>
        private object GetWinFX(object unused)
        {
            bool frameworkActivated = false;
            

            // Order matters newer OS versions should be tested before older OS versions
            if (OperatingSystemVersionCheck.IsVersionOrLater(OperatingSystemVersion.Windows8))
            {
                // Use FOD to activate the required framework version
                Invariant.Assert(!String.IsNullOrEmpty(_requiredCLRVersion));
                UnsafeNativeMethods.TryGetRequestedCLRRuntime(_requiredCLRVersion);
                frameworkActivated = true;
            }
            else if (OperatingSystemVersionCheck.IsVersionOrLater(OperatingSystemVersion.WindowsVista))
            {
                // Vista and Win7 
                // v3 of the framework come with the OS, use OCSetup to activate it
                // all other versions of the framework, use fwLinks to activate the required framework version
                switch(_requiredCLRVersion)
                {                    
                    case "v2.0.50727":
                        string full_ocSetupPath = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.System), 
                            "ocsetup.exe");
                    
                        ProcessStartInfo startInfo = new ProcessStartInfo(full_ocSetupPath, "NetFx3") {
                            Verb = "runas" // required to prompt for elevation
                        };

                        System.Diagnostics.Process.Start(startInfo);
                        frameworkActivated = true;
                        break;
                    
                    // Use fwLinks to activate the required framework version
                    default:
                        Invariant.Assert(_fwlinkUri != null);
                        AppSecurityManager.ShellExecuteDefaultBrowser(_fwlinkUri);
                        frameworkActivated = true;
                        break;
                }
            }
            
            if (!frameworkActivated && _fwlinkUri != null)
            {
                // Use fwLinks to activate the required framework version
                Invariant.Assert(_fwlinkUri != null);
                AppSecurityManager.ShellExecuteDefaultBrowser(_fwlinkUri);
            }

            return null;
        }

        ///<summary>
        /// Given a WinFx version number (usually) obtained from WindowsBase.dll 
        /// returns the CLR version that WinFx is layered on top of.
        /// returns null for unknown WinFx versions.
        ///</summary>
        private static string ClrVersionFromWinFXVersion(string winfxVersion)
        {
            string clrVersion = null;
            
            switch(winfxVersion)
            {
                case "3.0":
                case "3.5":
                    clrVersion =  "v2.0.50727";
                    break;
                
                case "4.0":
                    clrVersion = "v4.0.30319";
                    break;
                
                default:
                    break;
            }
            
            return clrVersion;
        }
        
        private void SetStatusText(string newStatusText)
        {
            IProgressPage2 pp2 = _progressPage as IProgressPage2;
            if (pp2 != null)
            {
                pp2.ShowProgressMessage(newStatusText);
            }
            BrowserInteropHelper.HostBrowser.SetStatusText(newStatusText);
        }

        /// <summary>
        /// If trust was not granted, it may have been because the assembly of a custom permission
        /// could not be loaded. In this case, we are interested in custom permissions that are
        /// from WindowsBase.
        /// </summary>
        /// <param name="reader">The XmlReader for the application manifest</param>
        /// <returns>If the specified version of WindowsBase could not be loaded, the version, else null.</returns>
        private string GetMissingCustomPermissionVersion(XmlReader reader)
        {
            string requiredVersion = null;

            while (reader.ReadToFollowing("IPermission", "urn:schemas-microsoft-com:asm.v2"))
            {
                string attr = reader.GetAttribute("class");

                // Strip the first item in the comma-delimited list, which is the permission class
                AssemblyName assyName = new AssemblyName(attr.Substring(attr.IndexOf(",",StringComparison.OrdinalIgnoreCase) + 1));
                if (assyName.Name.Equals("WindowsBase", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Assembly assy = Assembly.Load(assyName);
                    }
                    catch (Exception e)
                    {
                        // This will give a FileLoadException under the debugger, but a FileNotFoundException otherwise
                        if (e is System.IO.FileNotFoundException || e is System.IO.FileLoadException)
                        {
                            requiredVersion = assyName.Version.ToString();
                            break;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            reader.Close();

            return requiredVersion;
        }

        // Called by the laucher in preparation for launching an XBAP and shutting down.
        // We used to just call Shutdown on XAppLauncher and launch the XBAP in a Shutdown handler
        //     However sometimes the initial launch of a cached XBAP fails 
        //     so we refresh the cache by downloading the XBAP and retrying the launch.
        // This retry cannot succeed while shutdown is in progress so we now
        //     Launch the XBAP in a dispatcher callback
        //     If the XBAP launch does not immediately fail we shutdown XappLauncher
        private void RunApplicationAsync(DocObjHost.ApplicationRunner applicationRunner)
        {
                Invariant.Assert(applicationRunner != null);
                _applicationRunner = applicationRunner;
                _runApplication = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(RunApplicationAsyncCallback), null);                        
        }
        
        private object RunApplicationAsyncCallback(object unused)
        {
            if (_runApplication)
            {
                _applicationRunnerCallback(new DocObjHost.ApplicationRunner(_applicationRunner));
            }
            
            return null;
        }


        /* Threading issues with IPHM *
        IPHM has weird threading behavior. This makes controlling it challenging.
          1. It has partial thread affinity. Some events get raised on the thread on which the object is 
            created, others straight on some worker thread. Either way, we want to do all our processing
            on the UI thread, so we always go through the Dispatcher. This also ensures that the right
            ExecutionContext will be applied and the ExceptionWrapper will be in scope.
          2. It allows some concurreny. In particular, we rely much on application downloading and 
            AssertApplicationRequirements() to be done in parallel for better cold-start performance.
            (Optimization done in v3.5 SP1.)
          3. However, IPHM uses an internal lock to allow only one method at a time to run. This leads to
            unnecessary blocking of the threads using IPHM and thus potential deadlocks. 
              * When we started allowing the ClickOnce elevation prompt, this became a bigger issue. 
                The prompt is shown under IPHM's internal lock. This made download progress updates pushed
                to the main thread block that thread. This in turn prevented the progress page from 
                repainting when the elevation prompt is dragged over it. The solution was to create IPHM
                on a worker thread so that it never tries to call back on the main thread (see issue 1). 
                Now we are in charge of synchronizing with the main (UI) thread as necessary. For the most 
                part, Dispatcher.BeginInvoke() solves all problems.
        */
        InPlaceHostingManager _hostingManager;

        IBrowserCallbackServices _browser;
        ApplicationIdentity _identity;
        Uri _deploymentManifest;
        Uri _fwlinkUri;
        string _requiredCLRVersion;
        string _applicationId;
        System.ActivationContext _context;
        DocObjHost.ApplicationRunnerCallback _applicationRunnerCallback;
        DocObjHost.ApplicationRunner _applicationRunner;
        
        INativeProgressPage _nativeProgressPage;
        IProgressPage _progressPage;
        bool _runApplication;
        GetManifestCompletedEventArgs _getManifestCompletedEventArgs;

        const int ERROR_ACTIVATION_ABORTED = 30; // defined in host\inc\Definitions.hxx

        object _lockObject = new object();
        ManualResetEvent _assertAppRequirementsEvent;

        long _bytesDownloaded;
        long _bytesTotal;
        bool _updatePending;

        string _progressPageAssembly = null;
        string _progressPageClass = null;
        string _errorPageAssembly = null;
        string _errorPageClass = null;

        bool _commandBindingsRegistered;

        bool _isInAsynchronousOperation;
        volatile bool _canceled;
        bool _cancelHandled;
        volatile bool _assertAppRequirementsFailed;
        bool _refreshing;
        bool _hasTriedUriActivation;
        
        static class UnsafeNativeMethods
        {
            [DllImport(DllImport.PresentationNative, CharSet = CharSet.Unicode)]
            public static extern 
            int /* HRESULT */
            TryGetRequestedCLRRuntime(string versionString);        
        }
    }
}
