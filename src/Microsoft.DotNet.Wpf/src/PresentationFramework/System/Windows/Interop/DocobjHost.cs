// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the type used for COM interop by the browser host
//

using System;

#if NETFX
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Xml;
using Microsoft.Win32.SafeHandles;
using MS.Internal.AppModel;
using MS.Internal.IO.Packaging;
using MS.Internal.Progressivity;
using MS.Internal.Utility;
using MS.Utility;
using MS.Win32;
using MS.Internal;
using System.Text;
using System.Windows.Input;
using Microsoft.Win32;
#endif

//In order to avoid generating warnings about unknown message numbers and
//unknown pragmas when compiling your C# source code with the actual C# compiler,
//you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace System.Windows.Interop
{
#if NETFX
    /// <summary>
    /// Interop class used for implementing the managed part of a DocObj Server for browser hosting
    /// </summary>
    public sealed class DocObjHost : MarshalByRefObject, IServiceProvider, IHostService,
                              IBrowserHostServices, IByteRangeDownloaderService
    {
        ///<summary>
        /// This is only exposed publically for interop with the browser.
        /// This is not secure for partial trust.
        ///</summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        ///<SecurityNote>
        ///     Critical - as we have a treat as safe
        ///     PublicOK - as we have a demand.
        ///</SecurityNote>
        [ SecurityCritical  ]
        public DocObjHost()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_DocObjHostCreated);

            SecurityHelper.DemandUnmanagedCode();

            _mainThread = Thread.CurrentThread;
            _initData.Value.ServiceProvider = this;

            // Thread.ApartmentState is [Obsolete]
            #pragma warning disable 0618

            Debug.Assert(_mainThread.ApartmentState == ApartmentState.STA);

            #pragma warning restore 0618
        }

        #region Private Data

        private Thread                              _mainThread;
        private ApplicationProxyInternal            _appProxyInternal;
        private SecurityCriticalDataForSet<ApplicationProxyInternal.InitData> _initData =
            new SecurityCriticalDataForSet<ApplicationProxyInternal.InitData>(new ApplicationProxyInternal.InitData());
        private IntPtr                              _parent;
        private IBrowserCallbackServices            _browserCallbackServices;
        private Object                              _downloader;        // byte range downloader

        #endregion Private Data

        //******************************************************************************
        //
        //  MarshalByRef override
        //
        //******************************************************************************

        /// <summary>
        /// Return the ILease object, specifying that the lease should never expire
        /// </summary>
        /// <returns>A new ILease object</returns>
        /// <SecurityNote>
        ///    Critical: Elevates via assert to set the InitialLeaseTime
        ///    PublicOk: Always initializes to a constant value (TimeSpan.Zero)
        /// </SecurityNote>
        [SecurityCritical]
        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            Debug.Assert(lease.CurrentState == LeaseState.Initial);

            (new SecurityPermission(PermissionState.Unrestricted)).Assert(); //BlessedAssert
            try
            {
                lease.InitialLeaseTime = TimeSpan.Zero; // infinite -- never expire
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            return lease;
        }

        #region IServiceProvider
        //******************************************************************************
        //
        //  IServiceProvider interface implementation
        //
        //******************************************************************************

        /// <summary>
        /// Provides IHostService, IBrowserHostService
        /// </summary>
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IHostService))
            {
                return this;
            }
            else if (serviceType == typeof(IBrowserCallbackServices))
            {
                return _browserCallbackServices;
            }
            return null;
        }
        #endregion IServiceProvider

        #region IHostService
        //******************************************************************************
        //
        //  IHostService interface implementation
        //
        //******************************************************************************

        // <summary>
        // Get the proxy for the RootBrowserWindow.
        // CAUTION: This forces the RBW to be created.
        // </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        RootBrowserWindowProxy IHostService.RootBrowserWindowProxy
        {
            get
            {
                return (_appProxyInternal == null) ? null : _appProxyInternal.RootBrowserWindowProxy;
            }
        }

        // <summary>
        // ParentHandle of the host window
        // </summary>
        /// <SecurityNote>
        ///    Critical: returns the browser window
        /// </SecurityNote>
        IntPtr IHostService.HostWindowHandle
        {
            [SecurityCritical]
            get { return _parent; }
        }
        #endregion IHostService

        #region IBrowserHostServices
        //******************************************************************************
        //
        //  IBrowserHostServices interface implementation
        //
        //******************************************************************************

        /// <summary>
        /// Loads and runs the application or document based on the parameters passed in
        /// </summary>
        /// <param name="path">Path to XBAP deployment manifest, XPS document, or loose XAML file</param>
        /// <param name="fragment"> any URL #fragment (passed separately by the browser) </param>
        /// <param name="mime">Mime type of the content we are trying to load</param>
        /// <param name="debugSecurityZoneURL"> "fake" site-of-origin URL for debugging </param>
        /// <param name="applicationId"> ClickOnce application id (for XBAPs) </param>
        /// <param name="streamContainer">Marshaled IStream representing the current bind that we want to reuse</param>
        /// <param name="ucomLoadIStream">PersistHistory load stream</param>
        /// <param name="nativeProgressPage"></param>
        /// <param name="progressAssemblyName"> assembly name from which to load a custom deployment progress page </param>
        /// <param name="progressClassName"> class implementing IProgressPage </param>
        /// <param name="errorAssemblyName"> assembly name from which to load a custom deployment error page </param>
        /// <param name="errorClassName"></param>
        /// <returns>Int indicating whether the exit code of the application, failure could also
        /// mean that the app was not lauched successfully</returns>
        ///<SecurityNote>
        /// Critical    1) Calls the critical CreateAppDomainFor* methods;
        ///             2) uses the path information (which refers to the actual container or xaml
        ///                file that will be loaded) to determine the activationUri, which is used
        ///                to set the critical InitData.ActivationUri and .MimeType.
        ///             3) Because it sets _isAvalonTopLevel
        /// TreatAsSafe 2) Since the path information is coming directly from IE, and is the
        ///                same path that will be used to load the container or xaml file then this
        ///                information should be considered safe (if it was not safe the container
        ///                should fail to load, which would make this data moot).
        ///</SecurityNote>
        [SecurityCritical]
        int IBrowserHostServices.Run(
            String path,
            String fragment,
            MimeType mime,
            String debugSecurityZoneURL,
            String applicationId,
            object streamContainer,
            object ucomLoadIStream,
            HostingFlags hostingFlags,
            INativeProgressPage nativeProgressPage,
            string progressAssemblyName,
            string progressClassName,
            string errorAssemblyName,
            string errorClassName,
            IHostBrowser hostBrowser
            )
        {
            Invariant.Assert(String.IsNullOrEmpty(path) == false,  "path string should not be null or empty when Run method is called.");
            Invariant.Assert(mime != MimeType.Unknown, "Unknown mime type");

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_IBHSRunStart, "\""+path+"\"", "\""+applicationId+"\"");

            int exitCode = 0;

            try
            {
                ApplicationProxyInternal.InitData initData = _initData.Value;
                initData.HostBrowser = hostBrowser;
                initData.Fragment = fragment;
                initData.UcomLoadIStream = ucomLoadIStream;
                initData.HandleHistoryLoad = true;
                initData.MimeType.Value = mime;

                //  Installing .NET 4.0 adds two parts to the user agent string, i.e.
                // .NET4.0C and .NET4.0E, potentially causing the user agent string to overflow its
                // documented maximum length of MAX_PATH. While the right place to fix this is in
                // HostBrowserIE::GetUserAgentString in PresentationHostProxy, shared components
                // turn out hard to patch after the fact, so we do a spot-fix here in case we're
                // running in IE.
                string userAgent = null;
                MS.Internal.Interop.HRESULT hr = hostBrowser.GetUserAgentString(out userAgent); // get it only once for both AppDomains
                if (hr == MS.Internal.Interop.HRESULT.E_OUTOFMEMORY && (hostingFlags & HostingFlags.hfHostedInIEorWebOC) != 0)
                {
                    userAgent = MS.Win32.UnsafeNativeMethods.ObtainUserAgentString();
                    hr = MS.Internal.Interop.HRESULT.S_OK;
                }
                hr.ThrowIfFailed();

                initData.UserAgentString = userAgent;
                initData.HostingFlags = hostingFlags;

                Uri activationUri = new UriBuilder(path).Uri;
                initData.ActivationUri.Value = activationUri;
                PresentationAppDomainManager.ActivationUri = activationUri;

                // We do this here so that it will be set correctly when our deployment application
                // launches. This matters because if it isn't set when the app ctor is run, then
                // we will call Dispatcher.Run synchronously, which will make the browser
                // unresponsive.
                BrowserInteropHelper.SetBrowserHosted(true);

                if ((hostingFlags & HostingFlags.hfInDebugMode) != 0)
                {
                    _browserCallbackServices.ChangeDownloadState(false); // stop waving the flag
                    _browserCallbackServices.UpdateProgress(-1, 0); // make the progress bar go away
                    EnableErrorPage();
                    _appProxyInternal = new ApplicationLauncherXappDebug(path, debugSecurityZoneURL).Initialize();
                }
                else
                {
                    switch (mime)
                    {
                        case MimeType.Document:

                            _appProxyInternal = CreateAppDomainForXpsDocument();
                            if (_appProxyInternal == null)
                            {
                                exitCode = -1;
                            }
                            else
                            {
                                if (streamContainer != null)
                                {
                                    IntPtr punk = Marshal.GetIUnknownForObject(streamContainer);
                                    _appProxyInternal.StreamContainer = punk;
                                    Marshal.Release(punk);
                                }
                            }
                            // Free objects (after the _appProxyInternal.Run(initData) call below).
                            // For the other MIME types, this is done in RunApplication().
                            _initData.Value = null;
                            break;

                        case MimeType.Markup:
                            _appProxyInternal = CreateAppDomainForLooseXaml(activationUri);
                            _initData.Value = null; // Not needed anymore.
                            break;

                        case MimeType.Application:
                            // This is a browser hosted express app scenario.
                            // Setup XappLauncherApp with default values, and instantiate
                            // ApplicationProxyInternal for this AppDomain.
                            XappLauncherApp application = new XappLauncherApp(activationUri, applicationId,
                                _browserCallbackServices, new ApplicationRunnerCallback(RunApplication),
                                nativeProgressPage,
                                progressAssemblyName, progressClassName, errorAssemblyName, errorClassName);

                            // No need to handle history for progress app.  Remember
                            // it for the real app.
                            initData.HandleHistoryLoad = false;

                            _appProxyInternal = new ApplicationProxyInternal();
                            break;

                        default:
                            exitCode = -1;
                            break;
                    }
                }
                if (exitCode != -1)
                {
                    if (mime == MimeType.Document || mime == MimeType.Markup)
                    {
                        //[ChangoV, 7/27/07]
                        // Unfortunately, XPSViewer relies on the unhandled exception page to report bad XAML.
                        // Ideally, only exceptions from the XamlReader should be caught and shown this way,
                        // in order not to hide platform bugs. But it's more than one place where XAML is
                        // loaded from the XPS package, and in more than one way. A little too much to change
                        // in SP1.
                        // For loose XAML viewing, most exceptions likely to occur from this point on should be
                        // due to bad XAML.
                        EnableErrorPage();
                    }

                    //
                    // This mitigates failures in XBAP deployment and XPSViewer caused by msctf.dll
                    // by showing an actionable message instead of crashing
                    // deep inside the Input code where MSCTF gets loaded and fails to do so. More info on the
                    // conditions leading to this bug and how we detect those can be found in the method used
                    // for the check below and in the TFS bug database.
                    // See KB article http://support.microsoft.com/kb/954494 (linked in the displayed message).
                    if (IsAffectedByCtfIssue())
                    {
                        exitCode = -1;
                        _browserCallbackServices.ProcessUnhandledException(
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.Get(SRID.AffectedByMsCtfIssue),
                                "http://support.microsoft.com/kb/954494"
                            )
                        );
                    }
                    else
                    {
                        exitCode = _appProxyInternal.Run(initData);
                    }
                }
            }
            catch (Exception ex)
            {
                exitCode = -1;
                // The exception is re-thrown here, but it will get translated to an HRESULT by
                // COM Interop. That's why ProcessUnhandledException() is called directly.
                // In most cases it runs a modal loop around the error page and never returns.
                _browserCallbackServices.ProcessUnhandledException(ex.ToString());
                throw;
            }
            catch
            {
                // This catches non-CLS compliant exceptions.
                // Not having this clause triggers an FxCop violation.
                exitCode = -1;
                _browserCallbackServices.ProcessUnhandledException(SR.Get(SRID.NonClsActivationException));
                throw;
            }
            finally
            {
                Cleanup(exitCode);
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WpfHost_IBHSRunEnd, exitCode);

            return exitCode;
        }

        /// <summary>
        /// Checks for the conditions in which bug 451830 affects browser-hosted applications.
        /// </summary>
        /// <returns>true if the configuration is affected; false if not.</returns>
        private bool IsAffectedByCtfIssue()
        {
            // There's an issue in MSCTF.dll (Language Bar supporting DLL) that affects Windows XP users
            // when the language bar is enabled and a security policy known as "Security objects: Default
            // owner for objects created by members of the Administrators group" is set to (the non-default)
            // value of "Administrators group". Because PresentationHost.exe emulates Vista's UAC feature
            // on XP it strips down the process token, removing the Administrators group from it. This
            // causes calls within MSCTF.dll to fail when acquiring access to shared memory and mutexes
            // required for the operation of the language bar (see windows\advcore\ctf\inc\cicmutex.h),
            // which causes DllMain to fail, turning XBAPs that use Input features useless.
            //
            // Note: We tried detouring the affected function CreateFileMapping but it turned out other
            //       calls to CreateMutex were involved to create mutexes to protect the shared memory.
            //       Unfortunately the resulting language bar was not in a usable state due to broken
            //       input switching. This would be even more obscure than the original behavior so we
            //       opt for detection logic instead, pointing at the KB article.

            OperatingSystem os = Environment.OSVersion;
            if (os.Version.Major == 5 && os.Version.Minor == 1)
            {
                //
                // MSCTF.dll gets loaded by the WPF Text stack in two cases (which reflect the presence
                // of the language bar):
                // * TextServicesLoader.ServicesInstalled (special IME present)
                // * InputLanguageManager.IsMultipleKeyboardLayout (multiple keyboard layouts available)
                //
                if (TextServicesLoader.ServicesInstalled ||
                    InputLanguageManager.IsMultipleKeyboardLayout)
                {
                    //
                    // The registry value queried below is the one set by the aformentioned policy setting.
                    // This value can be controlled through group policy and requires a reboot or logoff/
                    // logon cycle in order to become effective. Therefore just changing this value will
                    // make the following check pass while still causing the application to crash because
                    // the objects used by MSCTF.dll will have been created at the time the policy was in
                    // effect, causing the owner of the created objects to be set to the Administrators
                    // group.
                    //
                    using (RegistryKey lsaKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Lsa"))
                    {
                        if ((int)lsaKey.GetValue("NoDefaultAdminOwner", 1) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal delegate bool ApplicationRunner();
        internal delegate void ApplicationRunnerCallback(ApplicationRunner runner);

        //
        // This method is invoked by the callback that occurs when the download of the app is complete, so that
        // running the main application happens on the correct thread.
        //
        ///<SecurityNote>
        /// Critical as this code calls a critical method.
        /// Setting _initData.Value = null is also critical, but it's safe.
        ///</SecurityNote>
        [SecurityCritical]
        internal void RunApplication(ApplicationRunner runner)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_DocObjHostRunApplicationStart);

            // Run the App in the new AppDomain and ask the AppDomainManager
            // to save it.
            PresentationAppDomainManager.SaveAppDomain = true;

            EnableErrorPage();

            if (runner())
            {
                Invariant.Assert(PresentationAppDomainManager.NewAppDomain != null, "Failed to start the application in a new AppDomain");

                Invariant.Assert(ApplicationProxyInternal.Current != null, "Unexpected reentrant PostShutdown?");

                // Create an ApplicationProxyInternal in the new domain.
                PresentationAppDomainManager appDomainMgrProxy =
                            PresentationAppDomainManager.NewAppDomain.DomainManager as PresentationAppDomainManager;

                // And replace _appProxyInternal.
                Invariant.Assert(ApplicationProxyInternal.Current == _appProxyInternal,
                    "AppProxyInternal has shut down unexpectedly.");
                _appProxyInternal = appDomainMgrProxy.CreateApplicationProxyInternal();

                PresentationAppDomainManager.SaveAppDomain = false;

                // Run the app.
                ApplicationProxyInternal.InitData initData = _initData.Value;
                initData.HandleHistoryLoad = true;
                _appProxyInternal.Run(initData);
                _initData.Value = null; // free objects
            }
            else
            {
                // Cached application activation failed
                // we will give the app launcher a chance to retry with Uri activation
                PresentationAppDomainManager.SaveAppDomain = false;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Verbose, EventTrace.Event.WpfHost_DocObjHostRunApplicationEnd);
        }

        // <summary>
        // Sets up the interface that the Windows Client host can use to talk back to the browser
        // </summary>
        // <param name="browserCallbackServices"></param>
        /// <SecurityNote>
        /// Critical: The callback object needs to be trusted.
        /// </SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.SetBrowserCallback(object browserCallbackServices)
        {
            Invariant.Assert(browserCallbackServices != null, "Browser interop interface passed in should not be null");
            _browserCallbackServices = (IBrowserCallbackServices)browserCallbackServices;
        }

        // <summary>
        // Set the parent window of the host window
        // </summary>
        // <param name="hParent"></param>
        /// <SecurityNote>
        /// Critical because it has the ability to parent window to an arbitrary IntPtr
        /// This is called via COM interop from the unmanaged side.
        /// </SecurityNote>
        /// <param name="hParent"></param>
        [SecurityCritical]
        void IBrowserHostServices.SetParent(IntPtr hParent)
        {
            _parent = hParent;
            // This is not a top-level window to be a proper owner, but at this time it may not be attached yet,
            // so we'll look for the root window later on.
            PresentationHostSecurityManager.ElevationPromptOwnerWindow = hParent;
        }

        // <summary>
        // Show or hide view
        // </summary>
        // <param name="show"></param>
        //
        void IBrowserHostServices.Show(bool show)
        {
            if (_initData.Value != null)
            {
                _initData.Value.ShowWindow = show;
            }

            if (_appProxyInternal != null)
            {
                _appProxyInternal.Show(show);
            }
        }

        // <summary>
        // For shdocvw LoadHistory support
        // </summary>
        // <returns></returns>
        bool IBrowserHostServices.IsAppLoaded()
        {
            return (_appProxyInternal == null) ? false : _appProxyInternal.IsAppLoaded();
        }

        // <summary>
        // The application sets the Environment.ExitCode to the applications exit code set by
        // calling Shutdown(exitCode) or in the ShutdownEventArgs
        // </summary>
        // <returns>Environment.ExitCode set by the application object when it Shutdown</returns>
        int IBrowserHostServices.GetApplicationExitCode()
        {
            return Environment.ExitCode;
        }

        bool IBrowserHostServices.CanInvokeJournalEntry(int entryId)
        {
            bool canInvoke = false;

            if ((this as IBrowserHostServices).IsAppLoaded() == false)
            {
                canInvoke = false;
            }
            else
            {
                canInvoke = _appProxyInternal.CanInvokeJournalEntry(entryId);
            }

            return canInvoke;
        }

        // <summary>
        // IPersistHistory.SaveHistory implementation called
        // when hosted in the browser
        // </summary>
        // <param name="comIStream">The native stream to save the journal information</param>
        // <returns>GCHandle to the saved byte array</returns>

        ///<SecurityNote>
        /// Critical as this calls a critical method.
        ///</SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.SaveHistory(object comIStream,
                                              bool persistEntireJournal,
                                              out int entryIndex,
                                              out string uri,
                                              out string title)
        {
            if (_appProxyInternal != null)
            {
                SaveHistoryHelper(comIStream, persistEntireJournal, out entryIndex, out uri, out title);
            }
            else
            {
                entryIndex = -1;
                uri = null;
                title = null;
            }
        }

        // <summary>
        // IPersistHistory::LoadHistory implementation called
        // when hosted in the browser
        // </summary>
        // <param name="ucomIStream"></param>
        ///<SecurityNote>
        ///      Critical as this method calls critical methods.
        ///</SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.LoadHistory(object ucomIStream)
        {
            if (_appProxyInternal != null)
            {
                LoadHistoryHelper(ucomIStream, /*firstHistoryLoad=*/false);
            }
        }

        //<summary>
        // IOleCommandTarget::QueryStatus called when hosted in the browser
        //</summary>
        /// <SecurityNote>
        ///     Critical: This code calls into query status helper
        ///     that can be used to cause an elevation.More importantly the parameters
        ///     that come in here come from the browser and we should not call this from elsewhere
        /// </SecurityNote>
        /// <remarks>
        /// OleCmdHelper reports errors by throwing ComException. The interop layer takes the
        /// associated HRESULT and returns it to the native caller. However, to avoid throwing exceptions
        /// on startup, while _appProxyInternal is not set up yet, this method is marked as [PreserveSig]
        /// and directly returns an error HRESULT.
        /// </remarks>
        [SecurityCritical]
        int IBrowserHostServices.QueryStatus(Guid guidCmdGroup, uint command, out uint flags)
        {
            flags = 0;
            if (_appProxyInternal != null)
            {
                OleCmdHelper cmdHelper = _appProxyInternal.OleCmdHelper;
                if (cmdHelper != null)
                {
                    cmdHelper.QueryStatus(guidCmdGroup, command, ref flags);
                    return NativeMethods.S_OK;
                }
            }
            return OleCmdHelper.OLECMDERR_E_UNKNOWNGROUP;
        }

        //<summary>
        // IOleCommandTarget::Exec called when hosted in the browser
        //</summary>
        /// <SecurityNote>
        ///     Critical: This code calls into ExecCommandHelper helper which can be used to spoof paste.
        ///     More importantly the parameters that come in here come from the browser and we should not call this from elsewhere
        /// </SecurityNote>
        /// <remarks>
        /// OleCmdHelper reports errors by throwing ComException. The interop layer takes the
        /// associated HRESULT and returns it to the native caller. However, to avoid throwing exceptions
        /// on startup, while _appProxyInternal is not set up yet, this method is marked as [PreserveSig]
        /// and directly returns an error HRESULT.
        /// </remarks>
        [SecurityCritical]
        int IBrowserHostServices.ExecCommand(Guid guidCommandGroup, uint command, object arg)
        {
            // When the native progress page is active, there is no RootBrowserWindow to handle the Refresh
            // and Stop commands. That's why they are dispatched directly here. Of course, this will also
            // handle some cases when the RBW is shown, for example Refresh (coming from the browser) when the
            // deployment failed/canceled page is shown.
            XappLauncherApp launcherApp = Application.Current as XappLauncherApp;
            if (launcherApp != null && guidCommandGroup == Guid.Empty)
            {
                switch ((UnsafeNativeMethods.OLECMDID)command)
                {
                    case UnsafeNativeMethods.OLECMDID.OLECMDID_REFRESH:
                        launcherApp.HandleRefresh();
                        return NativeMethods.S_OK;
                    case UnsafeNativeMethods.OLECMDID.OLECMDID_STOP:
                        launcherApp.UserStop();
                        return NativeMethods.S_OK;
                }
            }

            if (_appProxyInternal != null)
            {
                OleCmdHelper cmdHelper = _appProxyInternal.OleCmdHelper;
                if (cmdHelper != null)
                {
                    cmdHelper.ExecCommand(guidCommandGroup, command, arg);
                    return NativeMethods.S_OK;
                }
            }
            return OleCmdHelper.OLECMDERR_E_UNKNOWNGROUP;
        }

        // <summary>
        // Move -- standard args
        // </summary>
        // <param name="x"></param>
        // <param name="y"></param>
        // <param name="width"></param>
        // <param name="height"></param>
        ///<SecurityNote>
        /// Critical as this method calls a critical method.
        ///</SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.Move(int x, int y, int width, int height)
        {
            Rect windowRect = new Rect(x, y, width, height);

            // Remember the size and position of the browser window.  We'll need
            // to use this in the case of .deploy app where we need to size it
            // after creating it.  Otherwise the window won't render till we resize.
            // i. _initData is null after the application is started.
            if (_initData.Value != null)
            {
                _initData.Value.WindowRect = windowRect;
            }

            if (_appProxyInternal != null)
            {
                _appProxyInternal.Move(windowRect);
            }
        }

        ///<SecurityNote>
        /// This code call into critical code which in turn elevates to
        /// UI Permissions. This method is not available in the SEE.
        ///</SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.PostShutdown()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordHosting, EventTrace.Event.WpfHost_PostShutdown);

            if (_appProxyInternal != null)
            {
                _appProxyInternal.PostShutdown();
            }

            // AppProxyInternal does this, but we need to make sure the one in the default AppDomain gets
            // released too.
            BrowserInteropHelper.ReleaseBrowserInterfaces();
        }

        // <summary>
        //    Tell the app to activate/deactivate root browser window
        // </summary>
        // <param name="fActivate">Activate or Deactivate browser window</param>
        ///<SecurityNote>
        ///Critical as this calls a critical method.
        ///</SecurityNote>
        [SecurityCritical]
        void IBrowserHostServices.Activate(bool fActivate)
        {
            if (_appProxyInternal != null)
            {
                _appProxyInternal.Activate(fActivate);
            }
        }

        void IBrowserHostServices.TabInto(bool forward)
        {
            // Don't let the RBW be created while the native progress page is shown. Doing so would shut down
            // the progress page prematurely.
            if (!_appProxyInternal.RootBrowserWindowCreated)
                return;

            _appProxyInternal.RootBrowserWindowProxy.TabInto(forward);
        }

        bool IBrowserHostServices.FocusedElementWantsBackspace()
        {
            return _appProxyInternal != null ? _appProxyInternal.FocusedElementWantsBackspace() : false;
        }
        #endregion IBrowserHostServices

        #region ByteRangeDownloader

        /// <summary>
        /// Initialize the downloader for byte range request
        /// </summary>
        /// <param name="url">url to be downloaded</param>
        /// <param name="tempFile">temporary file where the downloaded bytes should be saved</param>
        /// <param name="eventHandle">event handle to be raised when a byte range request is done</param>
        /// <SecurityNote>
        /// Critical
        ///  1) creates ByteRangeDownloader which is critical
        /// </SecurityNote>
        [SecurityCritical]
        void IByteRangeDownloaderService.InitializeByteRangeDownloader(
            String url,
            String tempFile,
            SafeWaitHandle eventHandle)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if(tempFile == null)
            {
                throw new ArgumentNullException("tempFile");
            }

            if(eventHandle == null)
            {
                throw new ArgumentNullException("eventHandle");
            }

            if (eventHandle.IsInvalid || eventHandle.IsClosed)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidEventHandle), "eventHandle");
            }

            Uri requestedUri = new Uri(url, UriKind.Absolute);

            if(tempFile.Length <= 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidTempFileName), "tempFile");
            }

            ByteRangeDownloader loader = new ByteRangeDownloader(requestedUri, tempFile, eventHandle);

            // We defined _downloader as Object for performance reasons. If we define it as ByteRangeDownloader the whole
            // class will be loaded although it might not be used at all. By declaring it as Object we can prevent it
            // from being loaded. This technique is used in other areas.
            _downloader = (Object) loader;
        }

        /// <summary>
        /// Make HTTP byte range web request
        /// </summary>
        /// <param name="byteRanges">byte ranges to be downloaded; byteRanges is one dimensional
        /// array consisting pairs of offset and length</param>
        /// <param name="size">number of elements in byteRanges</param>
        void IByteRangeDownloaderService.RequestDownloadByteRanges (int[] byteRanges, int size)
        {
            //
            // Because of COM Interop Marshalling, we use made byte ranges as one dimensional array
            // However, since they are pairs of offset and length, it makes more sense to convert
            // them into two dimensional array in Managed code
            //
            if (byteRanges == null)
            {
                throw new ArgumentNullException("byteRanges");
            }

            if (byteRanges.Length <= 0 || (byteRanges.Length % 2) != 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidByteRanges, "byteRanges"));
            }

            if (_downloader == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.ByteRangeDownloaderNotInitialized));
            }

            ((ByteRangeDownloader) _downloader).RequestByteRanges(ByteRangeDownloader.ConvertByteRanges(byteRanges));
        }

        /// <summary>
        /// Get the byte ranges that are downloaded
        /// </summary>
        /// <param name="byteRanges">byte ranges that are downloaded; byteRanges is one dimensional
        /// array consisting pairs of offset and length</param>
        /// <param name="size">size of byteRanges</param>
        void IByteRangeDownloaderService.GetDownloadedByteRanges (out int[] byteRanges, out int size)
        {
            //
            // Because of COM Interop Marshalling, we use made byte ranges as one dimensional array
            // However, since they are pairs of offset and length, it makes more sense to convert
            // them into two dimensional array in Managed code
            //

            size = 0;
            byteRanges = null;

            if (_downloader == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.ByteRangeDownloaderNotInitialized));
            }

            int[,] ranges = ((ByteRangeDownloader) _downloader).GetDownloadedByteRanges();
            byteRanges = ByteRangeDownloader.ConvertByteRanges(ranges);
            size = byteRanges.Length;
        }

        /// <summary>
        /// Release the byte range downloader
        /// </summary>
        void IByteRangeDownloaderService.ReleaseByteRangeDownloader ()
        {
            if (_downloader == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.ByteRangeDownloaderNotInitialized));
            }

            ((IDisposable) _downloader).Dispose();
            _downloader = null;
        }

        #endregion ByteRangeDownloader

        #region Private Helper Methods

        #region Security Helpers

        /// <SecurityNote>
        ///    Critical: Creates a partial-trust AppDomain; sets up permissions.
        /// </SecurityNote>
        [SecurityCritical]
        private ApplicationProxyInternal CreateAppDomainForXpsDocument()
        {
            // Create the app domain using a restricted set of permissions, which are a subset of the
            // typical "Internet Zone" permissions.
            // (Explicitly, we leave out Web Browser permissions, and we only support SafeImages)
            PermissionSet permissionSet = new PermissionSet(null);
            permissionSet.AddPermission(new FileDialogPermission(FileDialogPermissionAccess.Open));
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissionSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows));
            permissionSet.AddPermission(new UIPermission(UIPermissionClipboard.OwnClipboard));
            permissionSet.AddPermission(new MediaPermission(MediaPermissionImage.SafeImage));

            // Set up IsolatedStorage permissions:
            // We allow 20mb of storage space and we isloate the storage on a per domain basis, by user.
            IsolatedStorageFilePermission storagePermission = new IsolatedStorageFilePermission(PermissionState.Unrestricted);
            storagePermission.UsageAllowed = IsolatedStorageContainment.DomainIsolationByUser;
            storagePermission.UserQuota = GetXpsViewerIsolatedStorageUserQuota();
            permissionSet.AddPermission(storagePermission);

            return CreateAppDomainAndAppProxy("WCP_Hosted_Application", GetXPSViewerPath(), permissionSet);
        }

        /// <SecurityNote>
        ///    Critical: Creates a partial-trust AppDomain; sets up permissions.
        ///         Calls native functions to discover the location of PresentationHostDll.
        /// </SecurityNote>
        [SecurityCritical]
        private ApplicationProxyInternal CreateAppDomainForLooseXaml(Uri uri)
        {
            /*
            The permission set for the XamlViewer AppDomain is similar to the default for the "Internet"
            security zone. Differences:
              - No IsolatedStorageFilePermission.
              - A site-of-origin permission is added so that the document and any other resources referenced
                by it can be loaded.

            FileDialogPermission and UIPermissionWindow.SafeTopLevelWindows shouldn't be needed for loose XAML
            viewing, but they are included for compatibility with the old XamlViewer.xbap.
            */
            PermissionSet permissionSet = new PermissionSet(null);
            permissionSet.AddPermission(new FileDialogPermission(FileDialogPermissionAccess.Open));
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissionSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows));
            permissionSet.AddPermission(new UIPermission(UIPermissionClipboard.OwnClipboard));
            permissionSet.AddPermission(SystemDrawingHelper.NewSafePrintingPermission());
            permissionSet.AddPermission(new MediaPermission(MediaPermissionAudio.SafeAudio, MediaPermissionVideo.SafeVideo, MediaPermissionImage.SafeImage));
            permissionSet.AddPermission(new WebBrowserPermission(WebBrowserPermissionLevel.Safe));

            permissionSet = PresentationHostSecurityManager.AddPermissionForUri(permissionSet, uri);

            // Setting AppDomain.ApplicationBase to some safe location is important. That's where assembly
            // lookup is done. We don't want to allow loading arbitrary assemblies in partial trust.
            // XamlViewer.xbap is defunct, but we still construct a path to where it used to be, just because
            // we need some path.
            string xamlViewer = "XamlViewer";
            // This GetModuleFileName() throws on error.
            string PHDLLPath = UnsafeNativeMethods.GetModuleFileName(new HandleRef(null, UnsafeNativeMethods.GetModuleHandle(ExternDll.PresentationHostDll)));
            string appBase = Path.GetDirectoryName(PHDLLPath) + "\\" + xamlViewer;

            return CreateAppDomainAndAppProxy(xamlViewer, appBase, permissionSet);
        }

        /// <SecurityNote>
        ///    Critical: Creates a partial-trust AppDomain; sets up permissions.
        /// </SecurityNote>
        [SecurityCritical]
        private ApplicationProxyInternal CreateAppDomainAndAppProxy(string domainName, string appBasePath, PermissionSet grantSet)
        {
            AppDomainSetup domainSetup = new AppDomainSetup();
            Invariant.Assert(!string.IsNullOrEmpty(appBasePath));
            domainSetup.ApplicationBase = appBasePath;
            AppDomain appDomain = AppDomain.CreateDomain(domainName, null, domainSetup, grantSet, null);
            return ((PresentationAppDomainManager)appDomain.DomainManager).CreateApplicationProxyInternal();
        }


        #endregion Security Helpers

        ///<SecurityNote>
        ///     Critical: calls Marshal.ReleaseComObject which LinkDemands
        ///</SecurityNote>
        [SecurityCritical]
        private void Cleanup(int exitCode)
        {
            //No error => don't cleanup
            if (exitCode == 0)
                return;

            // You need to explicitly release the COM object.  The RCW doesn't
            // seem to get garbage collected automatically.
            //
            // We don't want to call release multiple times.  So call either
            // on the App or on the Docobj since the ServiceProvider setter
            // release the  _browserCallbackServices com object
            if (_appProxyInternal != null)
            {
                _appProxyInternal.Cleanup();
            }
            else if (_browserCallbackServices != null)
            {
                Marshal.ReleaseComObject(_browserCallbackServices);
                _browserCallbackServices = null;
            }
        }

        // IPersistHistory::SaveHistory implementation called
        // when hosted in the browser
        // <returns>GCHandle to the saved byte array</returns>

        ///<SecurityNote>
        /// Critical as this calls a critical method SecuritySuppressedIStream.Write().
        ///</SecurityNote>
        [SecurityCritical]
        private void SaveHistoryHelper(object comIStream,
                                       bool persistEntireJournal,
                                       out int entryIndex,
                                       out string uri,
                                       out string title)
        {
            //out params need to be initialized before control leaves this method (CS0177)
            uri = title = null;
            entryIndex  = -1;

            // If the ApplicationProxyInternal object is in the default AppDomain, we are not yet
            // running the real applicaiton, so there is no journal to persist.
            if (_appProxyInternal == null || !RemotingServices.IsTransparentProxy(_appProxyInternal))
            {
                return;
            }

            SecuritySuppressedIStream historyStream = comIStream as SecuritySuppressedIStream;
            //CONSIDER: Should throw an exception instead?
            if (historyStream == null)
                return;

            byte [] saveByteArray = _appProxyInternal.GetSaveHistoryBytes( persistEntireJournal,
                                                                             out entryIndex,
                                                                             out uri,
                                                                             out title);

            if (saveByteArray == null)
                return;

            int len = saveByteArray.Length;
            int bytesWritten = 0;
            historyStream.Write(saveByteArray, len, out bytesWritten);
            Invariant.Assert(bytesWritten == len, "Error saving journal stream to native IStream");
        }

        // IPersistHistory::LoadHistory implementation called
        // when hosted in the browser
        // <param name="comIStream"></param>
        ///<SecurityNote>
        ///      Critical as this method accesses other members that are critical.
        ///</SecurityNote>
        [SecurityCritical]
        private void LoadHistoryHelper(object comIStream, bool firstLoadFromHistory)
        {
            if (_appProxyInternal == null)
            {
                return;
            }

            _appProxyInternal.LoadHistoryStream(ExtractComStream(comIStream), firstLoadFromHistory);
        }

        ///<SecurityNote>
        /// Critical as this calls a critical method SecuritySuppressedIStream.Read().
        ///</SecurityNote>
        [SecurityCritical]
        internal static MemoryStream ExtractComStream(object comIStream)
        {
            SecuritySuppressedIStream historyStream = comIStream as SecuritySuppressedIStream;
            if (historyStream == null)
            {
                throw new ArgumentNullException("comIStream");
            }

            MemoryStream loadStream = new MemoryStream();
            byte[] loadByteArray = new byte[1024];
            int bytesRead = 0;

            do
            {
                bytesRead = 0;
                historyStream.Read(loadByteArray, 1024, out bytesRead);
                loadStream.Write(loadByteArray, 0, bytesRead);
            }
            while (bytesRead > 0);

            Invariant.Assert(loadStream.Length > 0, "Error reading journal stream from native IStream");

            return loadStream;
        }

        private bool IsXbapErrorPageDisabled()
        {
            object errorPageSetting = Microsoft.Win32.Registry.GetValue(
                    "HKEY_CURRENT_USER\\" + RegistryKeys.WPF_Hosting, RegistryKeys.value_DisableXbapErrorPage, null);
            if(errorPageSetting == null)
            {
                errorPageSetting = Microsoft.Win32.Registry.GetValue(
                    "HKEY_LOCAL_MACHINE\\" + RegistryKeys.WPF_Hosting, RegistryKeys.value_DisableXbapErrorPage, null);
            }
            return errorPageSetting is int && (int)errorPageSetting != 0;
        }

        /// <summary>
        /// Sets up an AppDomain.UnhandledException handler intended to show exceptions from the hosted
        /// application in the HTML error page. This handler should be activated as late as possible,
        /// just before the Main() method of the hosted application is called, in order to allow unhandled
        /// exceptions in the hosting code and ClickOnce to go to Watson.
        /// In a future release we'll add a Send Error Report button to the error page. Exceptions due to
        /// platform bugs are currently "swallowed" by it.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Registers for critical AppDomain..UnhandledExceptioncalls event
        /// TreatAsSafe - Handler is known and trusted 
        /// </SecurityNote>
        [SecuritySafeCritical]
        private void EnableErrorPage()
        {
            if (!IsXbapErrorPageDisabled())
            {
                AppDomain.CurrentDomain.UnhandledException += this.ProcessUnhandledException;
            }
            // Caveats:
            // -- Debuggers affect exception handling. In particular, doing mixed-mode or
            // native-only debugging seems to disable this event. Debugging the unmanaged
            // unhandled-exception handling code is easier when it is called before an exception
            // occurs. (Add a temporary call somewhere.)
            // -- AppDomain.add_UnhandledException() requires the given handler to follow the
            // CLR Constrained Execution Region rules. (It calls RuntimeHelpers.PrepareDelegate().)
            // See http://blogs.msdn.com/bclteam/archive/2005/06/14/429181.aspx for a discussion.
        }

        /// <SecurityNote>
        /// Critical - calls IBCS.ProcessUnhandledException(), which is Critical.
        /// TreatAsSafe - Showing the error page is not a privileged operation. An XBAP can always do that
        ///     by throwing some exception.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        private void ProcessUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // In CLR v2 unhandled exceptions on background threads also terminate the process. That's why
            // e.IsTerminating appears to be always 'true' here.

            try
            {
                // Exception.ToString() contains the exception type, stack trace, and information about
                // inner exceptions. If the exception object is not derived from Exception (the CLR
                // allows throwing anything), we should get at least its type.
                string errorMessage = e.ExceptionObject.ToString();
                errorMessage = errorMessage.Replace(" --->", "\n--->"); // Make nested exceptions more readable.

                Invariant.Assert(_browserCallbackServices != null);

                // Immediately returning from here makes sure the debugger always gets notified on
                // unhandled exception.
                if (Debugger.IsAttached)
                    return;

                // Issue: If the exception arrives on a thread other than the main one, we can't call
                // _browserCallbackServices.ProcessUnhandledException(), because COM marshaling for
                // COleDocument is not available (no typelib or proxy/stub CLSID registered).
                // The workaround is to make the call via a DLL-exported function.
                // (This is actually better in a way because proper marshaling would depend on the main
                // thread actively pumping messages [STA apartment], and we can't be sure where it is
                // and what it is doing at this point.)
                //
                // In the current implementation, the native ProcessUnhandledException() may or may not
                // return, depending on whether the browser is currently blocked on a call into our
                // DocObject.
                if (Thread.CurrentThread == _mainThread)
                {
                    _browserCallbackServices.ProcessUnhandledException(errorMessage);
                }
                else
                {
                    MS.Win32.UnsafeNativeMethods.ProcessUnhandledException_DLL(errorMessage);
                }
            }
            catch(Exception ex)
            {
                Debug.Fail(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Returns the path for the XPSViewer application.
        /// </summary>
        /// <returns></returns>
        private string GetXPSViewerPath()
        {
            // Get the path to the XPSViewer executable from the registry:
            string xpsViewerPath = Microsoft.Win32.Registry.GetValue(
                RegistryKeys.HKLM_XpsViewerLocalServer32,
                null,
                null) as string;

            // If the registry value is not found, we consider this a fatal error and will not continue.
            if( xpsViewerPath == null )
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture,
                        SR.Get(SRID.DocumentApplicationRegistryKeyNotFound),
                        RegistryKeys.HKLM_XpsViewerLocalServer32));
            }

            // The path we get back from the registry contains the name of the XPSViewer
            // executable -- we only want the path to the viewer, so we strip that off.
            xpsViewerPath = System.IO.Path.GetDirectoryName(xpsViewerPath);

            return xpsViewerPath;
        }

        /// <summary>
        /// Returns the amount of IsolatedStorage to allot the XPSViewer instance.
        /// This queries the registry for a DWORD value named "IsolatedStorageUserQuota" in
        /// HKCU\Software\Microsoft\XPSViewer.  If it exists then the value there is used as
        /// the isolated storage quota; otherwise the default value of 20mb is used.
        /// </summary>
        /// <returns></returns>
        private int GetXpsViewerIsolatedStorageUserQuota()
        {
            int isolatedStorageUserQuota = _defaultXpsIsolatedStorageUserQuota;

            object isolatedStorageRegistryValue = Microsoft.Win32.Registry.GetValue(
                RegistryKeys.HKCU_XpsViewer,
                RegistryKeys.value_IsolatedStorageUserQuota,
                null);

            if (isolatedStorageRegistryValue is int)
            {
                isolatedStorageUserQuota = (int)isolatedStorageRegistryValue;
            }

            return isolatedStorageUserQuota;
        }

        #endregion Private Helper Methods

        //------------------------------------------------------
        //
        //  Private Unmanaged Interfaces
        //
        //------------------------------------------------------
        #region Private Unmanaged Interface imports

        /// <SecurityNote>
        /// Supressing unmanaged code security on Read and Write only as they are the only methods
        /// currently in use on this interface.  The reason that we do not use ISequentialStream
        /// instead of the full IStream is because IStream is what is passed to the
        /// IPersistHistory interfaces in native code.
        /// </SecurityNote>
        [Guid("0000000c-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        private interface SecuritySuppressedIStream
        {
            // ISequentialStream portion
            /// <SecurityNote>
            /// Critical - Causes an elevation to read from the unmanaged stream
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void Read([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Byte[] pv, int cb, out int pcbRead);

            /// <SecurityNote>
            /// Critical - Causes an elevation to write to the unmanaged stream
            /// </SecurityNote>
            [SecurityCritical, SuppressUnmanagedCodeSecurity]
            void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Byte[] pv, int cb, out int pcbWritten);

            // IStream portion
            void Seek(long dlibMove, int dwOrigin, out long plibNewPosition);
            void SetSize(long libNewSize);
            void CopyTo(SecuritySuppressedIStream pstm, long cb, out long pcbRead, out long pcbWritten);
            void Commit(int grfCommitFlags);
            void Revert();
            void LockRegion(long libOffset, long cb, int dwLockType);
            void UnlockRegion(long libOffset, long cb, int dwLockType);
            void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
            void Clone(out SecuritySuppressedIStream ppstm);
        }
        #endregion Private Unmanaged Interface imports

        // The amount of User Quota to allow for XPSViewer's Isolated Storage (approx 512mb)
        private const int _defaultXpsIsolatedStorageUserQuota = 512000000;
    }

    internal class ApplicationLauncherXappDebug
    {
        ///<SecurityNote>
        /// Critical
        ///    Because it sets critical data _debugSecurityZoneURL
        ///</SecurityNote>
        [SecurityCritical]
        public ApplicationLauncherXappDebug(string path, string debugSecurityZoneURL)
        {
            _deploymentManifestPath = path; // assumed to be .xapp
            _deploymentManifest = new Uri(path);
            if (!string.IsNullOrEmpty(debugSecurityZoneURL))
            {
                _debugSecurityZoneURL.Value = new Uri(debugSecurityZoneURL);
            }
            _applicationManifestPath = Path.ChangeExtension(path, ".exe.manifest");
            _exePath = Path.ChangeExtension(path, ".exe");
        }

        /// <SecurityNote>
        /// This creates and manipulates protected resources, so it must be critical.
        /// It has a demand, so it is safe.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public ApplicationProxyInternal Initialize()
        {
            SecurityHelper.DemandUIWindowPermission();

            _context = ActivationContext.CreatePartialActivationContext(GetApplicationIdentity(), new string[] {_deploymentManifestPath, _applicationManifestPath});

            // remove cached trust decision
            ApplicationTrust at = new ApplicationTrust(GetApplicationIdentity());
            System.Security.Policy.ApplicationSecurityManager.UserApplicationTrusts.Remove(at);

            PresentationAppDomainManager.IsDebug = true;
            PresentationAppDomainManager.DebugSecurityZoneURL = _debugSecurityZoneURL.Value;
            PresentationAppDomainManager.SaveAppDomain = true;
            ObjectHandle oh = Activator.CreateInstance(_context);
            if (PresentationAppDomainManager.SaveAppDomain)
            {
                AppDomain newDomain = oh.Unwrap() as AppDomain;
                PresentationAppDomainManager.NewAppDomain = newDomain;
            }

            // Create an ApplicationProxyInternal in the new domain.
            PresentationAppDomainManager appDomainMgrProxy = PresentationAppDomainManager.NewAppDomain.DomainManager as PresentationAppDomainManager;
            ApplicationProxyInternal proxy = appDomainMgrProxy.CreateApplicationProxyInternal();

            proxy.SetDebugSecurityZoneURL(_debugSecurityZoneURL.Value);

            PresentationAppDomainManager.SaveAppDomain = false;

            return proxy;
        }

        private ApplicationIdentity GetApplicationIdentity()
        {
            return new ApplicationIdentity(
                _deploymentManifest.ToString() + "#"
                + GetIdFromManifest(_deploymentManifestPath) + "/"
                + GetIdFromManifest(_applicationManifestPath));
        }

        private string GetIdFromManifest(string manifestName)
        {
            FileStream fileStream = new FileStream(manifestName, FileMode.Open, FileAccess.Read);
            try
            {
                using (XmlTextReader reader = new XmlTextReader(fileStream))
                {
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.NamespaceURI != "urn:schemas-microsoft-com:asm.v1")
                            {
                                continue;
                            }
                            if (reader.LocalName == "assemblyIdentity")
                            {
                                string id = string.Empty;
                                while (reader.MoveToNextAttribute())
                                {
                                    if (reader.Name == "name")
                                    {
                                        id = reader.Value + id;
                                    }
                                    else if (reader.Name == "xmlns")
                                    {
                                        // do nothing
                                    }
                                    else
                                    {
                                        id = id + ", " + reader.Name + "=" + reader.Value;
                                    }
                                }
                                return id;
                            }
                        }
                    }
                }
            }
            finally
            {
                fileStream.Close();
            }

            return string.Empty;
        }

        string _deploymentManifestPath;
        Uri _deploymentManifest;
        string _applicationManifestPath;
        string _exePath;
        SecurityCriticalDataForSet<Uri> _debugSecurityZoneURL = new SecurityCriticalDataForSet<Uri>(null);
        ActivationContext _context;
    }
#else

    public sealed class DocObjHost : MarshalByRefObject, IServiceProvider
    {
        public DocObjHost()
        {
            throw new PlatformNotSupportedException(SR.Get(SRID.BrowserHostingNotSupported));
        }
        public override object InitializeLifetimeService()
        {
            throw new PlatformNotSupportedException(SR.Get(SRID.BrowserHostingNotSupported));
        }
        object System.IServiceProvider.GetService(Type serviceType)
        {
            throw new PlatformNotSupportedException(SR.Get(SRID.BrowserHostingNotSupported));
        }
    }
#endif
}
