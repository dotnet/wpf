// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:  
//      WebBrowser is a wrapper for the webbrowser activex control     

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using MS.Win32;
using System.Security; 
using System.Security.Permissions; 
using System.Windows.Controls.Primitives; //PopupRoot
using MS.Internal.Utility ;
using MS.Internal.AppModel; //RootBrowserWindow
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Navigation;
using System.IO; //Stream
using System.Threading; // thread
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Interop;
using MS.Internal.Telemetry.PresentationFramework;
using System.IO.Packaging;
using System.Diagnostics.CodeAnalysis;

using HRESULT = MS.Internal.Interop.HRESULT;
using SafeSecurityHelper=MS.Internal.PresentationFramework.SafeSecurityHelper;
using SecurityHelperPF=MS.Internal.PresentationFramework.SecurityHelper;
using PackUriHelper = MS.Internal.IO.Packaging.PackUriHelper;

/* Overview of Keyboard Input Routing for the WebOC

The WebOC receives regular alphanumeric keyboard input via its WndProc. Whoever is running the message loop
calls DispatchMessage() after any preprocessing and special routing, and the message gets to the WndProc of 
the window with focus. 

"Accelerator" keys need to be passed to the WebOC via IOleInPlaceActiveObject::TranslateAccelerator().
For example, these include the navigation keys and clipboard keys. Depending on how the WebOC is hosted,
input messages flow differently:
 1) In standalone application or 
    in an XBAP when IE is not in protected mode => WebOC hosted in the PresentationHost process: 
     HwndSource.OnPreprocessMessage() -> InputManager ->(routed event) HwndHost.OnKeyDown() -> 
     WebBrowser.IKeyboardInputSink.TranslateAccelerator().
 2) In the browser process (IE 7+, when in protected mode): The WebOC is run on a dedicated thread. There is a
    message loop running for it. Before calling TranslateMessage & DispatchMessage, it calls WebOC's
    IOleInPlaceActiveObject::TranslateAccelerator().
    
Other accelerator handling APIs:
 - IDocHostUIHandler::TranslateAccelerator(): This appears to be called by the WebOC for everything passed to it
     via IOleInPlaceActiveObject::TranslateAccelerator(), so it's not interesting to us.
 - IOleControlSite::TranslateAccelerator(): The WebOC is passing MSGs it gets via IOleInPlaceActiveObject::
   TranslateAccelerator() and its WndProc but doesn't handle. Our WebBrowserSite takes advantage of this to
   handle tabbing out of the WebOC.
*/

namespace System.Windows.Controls
{
    ///<summary>
    /// This is a wrapper over the native WebBrowser control implemented in shdocvw.dll.
    ///</summary>
    /// <remarks>
    /// The WebBrowser class is currently not thread safe. Multi-threading could corrupt the class internal state, 
    /// which could lead to security exploits (see example in devdiv bug #196538). So we enforce thread affinity. 
    /// </remarks>
    public sealed class WebBrowser : ActiveXHost
    {
        //----------------------------------------------
        //
        // Constructors
        //
        //----------------------------------------------

        #region Constructor

        ///<SecurityNote> 
        /// Critical: 1. Implements security policy. 
        ///     2. Uses the critical RegistryKeys.ReadLocalMachineBool() to read a security configuration flag.
        /// Safe: This flag is not a secret.
        ///</SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        static WebBrowser()
        {
#if NETFX
            if (IsWebOCPermissionRestricted)
            {
                // Breaking change for v4, intended to be backported to v3.x: We block PT hosting of the WebOC:
                //   - In non-IE browser (intended for Firefox presently) for Internet-zone XBAPs, in order 
                //     not to expose IE's attack surface;
                //   - In PT standalone ClickOnce application. WPF does not support this in general, but a WebBrowser
                //     can be instantiated and navigated, and script in it will run, which could be harmful.
                if (BrowserInteropHelper.IsBrowserHosted) 
                {
                    if ((BrowserInteropHelper.HostingFlags & HostingFlags.hfHostedInIEorWebOC) == 0)
                    {
                        // Explicitly trust only these zones...
                        int sourceZone = AppSecurityManager.MapUrlToZone(BrowserInteropHelper.Source);
                        if (sourceZone != NativeMethods.URLZONE_INTRANET && sourceZone != NativeMethods.URLZONE_TRUSTED &&
                        sourceZone != NativeMethods.URLZONE_LOCAL_MACHINE)
                        {
                            // Enable explicit opt-out of this blocking.
                            if (RegistryKeys.ReadLocalMachineBool(RegistryKeys.WPF_Hosting, RegistryKeys.value_UnblockWebBrowserControl) != true)
                            {
                                //[Using a resource string for another issue that's close enough in meaning.]
                                throw new SecurityException(SR.Get(SRID.AffectedByMsCtfIssue, "http://go.microsoft.com/fwlink/?LinkID=168882"));
                            }
                        }
                    }
                }
                else
                {
                    // ClickOnce uses AppLaunch.exe to host partial-trust applications.
                    string hostProcessName = Path.GetFileName(UnsafeNativeMethods.GetModuleFileName(new HandleRef()));
                    if (string.Compare(hostProcessName, "AppLaunch.exe", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // No explanation message is warranted in this case since it's not supported anyway.
                        SecurityHelperPF.DemandWebBrowserPermission();
                    }
                }

                RegisterWithRBW();
            }
#endif

            TurnOnFeatureControlKeys();

            ControlsTraceLogger.AddControl(TelemetryControls.WebBrowser);
        }

        ///<SecurityNote> 
        ///     Critical - accesses critical Ctor 
        ///     PublicOK - creating web browser considered safe. 
        ///
        ///                   Known threats and the justification as to why they are mitigated : 
        ///
        ///                     Uri is validated at set time. An attempts to navigate outside site of origin will fail the demand for web-permission. 
        ///                     Attempts to navigate the HTML to another page are mitigated by "Site Lock" feature. 
        ///                     Attempts to show popup's above HTML are mitigated by popup work. 
        ///                     Running script inside WebOC - considered ok as script is Internet Zone. 
        ///                     Running activeX controls - considered safe. Equivalent functionality is enabled through web-pages.
        ///                     Cookies: The WebOC thinks it's a top-level browser. Thus cookies it sets will always
        ///                         have 1st party status, which wouldn't be right if the WebOC is in an XBAP that is
        ///                         third party to the containing HTML page. But now PresentationHost intercepts all
        ///                         calls to the WinInet cookie APIs and will add the 3rd party flag when necessary.
        ///                         V3 SP2 Update - WebOCHostedInBrowserProcess - The cookie shim does not apply 
        ///                             in this case. To prevent the cookie status elevation problem, the native code
        ///                             in PresentationHostDll fails creating the WebOC.
        ///                                                
        ///</SecurityNote> 
        [ SecurityCritical ] 
        public WebBrowser() 
            : base(new Guid(CLSID.WebBrowser), true )
        {
            // Check whether feature is disabled
            if (SafeSecurityHelper.IsFeatureDisabled(SafeSecurityHelper.KeyToRead.WebBrowserDisable))
            {
                // in case the registry key is '1' then demand unrestricted WebBrowserPermission to create it
                SecurityHelperPF.DemandWebBrowserPermission();
            }
            else
            {
                // Feature is enabled - demand Safe level to create this object, granted in Partial Trust by default
                (new WebBrowserPermission(WebBrowserPermissionLevel.Safe)).Demand();
            }

#if NETFX
            // If the webbrowser permission is restricted, we don't allow webbrowser to be inside Popup.   
            if (IsWebOCPermissionRestricted)
            {
                Loaded += new RoutedEventHandler(LoadedHandler);
            }
            _hostingAdaptor = IsWebOCHostedInBrowserProcess ? 
                new WebOCHostedInBrowserAdaptor(this) : new WebOCHostingAdaptor(this);
#else 
            _hostingAdaptor = new WebOCHostingAdaptor(this);
#endif
        }

        #endregion Constructor

        //----------------------------------------------
        //
        // Public Methods
        //
        //----------------------------------------------

        #region Public Methods

        /// <summary>
        /// Navigate to the WebBrowser control to the given URI.
        /// </summary>
        /// <param name="source">URI being navigated to.</param>
        public void Navigate(Uri source)
        {
            Navigate(source, null, null, null);
        }

        /// <summary>
        /// Navigate to the WebBrowser control to the given URI, allowing invalid UTF-8 sequences in the URI string.
        /// </summary>
        /// <param name="source">String representation of the URI being navigated to.</param>
        [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "See comment in DoNavigate for rationale of having this overload.")]
        public void Navigate(string source)
        {
            // See comment in DoNavigate for rationale of having this overload.
            Navigate(source, null, null, null);
        }

        /// <summary>
        /// Navigate to the WebBrowser control to the given URI. 
        /// </summary>
        /// <param name="source">URI being navigated to.</param>
        /// <param name="targetFrameName">The name of the frame in which to load the document.</param>
        /// <param name="postData">HTTP POST data such as form data.</param>
        /// <param name="additionalHeaders">HTTP headers to add to the default headers.</param>        
        public void Navigate(Uri source, string targetFrameName, byte[] postData, string additionalHeaders)
        {             
            object objTargetFrameName = (object)targetFrameName;
            object objPostData = (object)postData;
            object objHeaders = (object)additionalHeaders;

            DoNavigate(source, ref objTargetFrameName, ref objPostData, ref objHeaders);
        }

        /// <summary>
        /// Navigate to the WebBrowser control to the given URI, allowing invalid UTF-8 sequences in the URI string.
        /// </summary>
        /// <param name="source">String representation of the URI being navigated to.</param>
        /// <param name="targetFrameName">The name of the frame in which to load the document.</param>
        /// <param name="postData">HTTP POST data such as form data.</param>
        /// <param name="additionalHeaders">HTTP headers to add to the default headers.</param>
        [SuppressMessage("Microsoft.Design", "CA1057:StringUriOverloadsCallSystemUriOverloads", Justification = "See comment in DoNavigate for rationale of having this overload.")]
        public void Navigate(string source, string targetFrameName, byte[] postData, string additionalHeaders)
        {
            // See comment in DoNavigate for rationale of having this overload.             
            object objTargetFrameName = (object)targetFrameName;
            object objPostData = (object)postData;
            object objHeaders = (object)additionalHeaders;

            Uri uri = new Uri(source);
            DoNavigate(uri, ref objTargetFrameName, ref objPostData, ref objHeaders, true /* ignoreEscaping */);
        }

        /// <summary>
        /// Navigates the the stream of a html page.
        /// </summary>
        /// <param name="stream">The stream that contains the content of a html document</param>
        public void NavigateToStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            DocumentStream = stream;            
            // We navigate to "about:blank" when Source is set to null. 
            // When we get NavigateComplete event, we load the stream via the IPersistStreamInit interface.
            Source = null;
        }

        /// <summary>
        /// Navigates to the text of a html page.
        /// </summary>
        /// <param name="text">The string that contains the content of a html document</param>
        public void NavigateToString(String text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            MemoryStream ms = new MemoryStream(text.Length);
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(text);
            sw.Flush();
            ms.Position = 0;

            NavigateToStream(ms);
        }

        /// <summary>
        /// Navigates the WebBrowser control to the previous page if available.
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - accesses the critical unmanaged interface of the control.
        ///     Public OK - This code does not expose the interface.
        ///               - Going back to the previous page within the WebBrowser control is considered safe.
        ///</SecurityNote>
        [SecurityCritical]
        public void GoBack()
        {
            VerifyAccess();

            AxIWebBrowser2.GoBack();
        }

        /// <summary>
        /// Navigates the WebBrowser control to the next page if available.
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - accesses the critical unmanaged interface of the control.
        ///     Public OK - This does not expose the interface.
        ///               - Going forward to the next page within the WebBrowser is considered safe.
        ///</SecurityNote>
        [SecurityCritical]
        public void GoForward()
        {
            VerifyAccess();

            AxIWebBrowser2.GoForward();            
        }


        /// <summary>
        /// Refreshes the current page. 
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - accesses the critical unmanaged interface of the control.
        ///     Public OK - This does not expose the interface.
        ///               - Refreshing the WebBrowser is considered safe.
        ///</SecurityNote>
        [SecurityCritical]
        public void Refresh()
        {
            VerifyAccess();

            AxIWebBrowser2.Refresh();
        }

        /// <summary>
        /// Refreshes the current page. 
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - accesses the critical unmanaged interface of the control.
        ///     Public OK - This does not expose the interface.
        ///               - Refreshing the WebBrowser is considered safe.
        ///</SecurityNote>
        /// <param name="noCache">Whether to refresh without cache validation by sending "Pragma:no-cache" header to the server.</param>
        [SecurityCritical]
        public void Refresh(bool noCache)
        {
            VerifyAccess();

            // Out of the three options of RefreshConstants, we only expose two: REFRESH_NORMAL and REFRESH_COMPLETELY,
            // because the thrid option, RefreshConstants.REFRESH_IFEXPIRED, is not implemented by IWebBrowser2.Refresh.
            // And those two options cover the common scenarios.
            //
            // enum RefreshConstants{
            //      REFRESH_NORMAL = 0,
            //      REFRESH_IFEXPIRED = 1 /* Not supported by IWebBrowser2*/, 
            //      REFRESH_COMPLETELY = 3
            // }
            int refreshOption = noCache ? 3 : 0;
            object refreshOptionObject = (object) refreshOption;
            AxIWebBrowser2.Refresh2(ref refreshOptionObject);
        }

        /// <summary>
        /// Executes an Active Scripting function defined in the HTML document currently loaded in the WebBrowser control. 
        /// </summary>
        /// <param name="scriptName">The name of the script method to invoke.</param>
        /// <returns>The object returned by the Active Scripting call.</returns>
        public object InvokeScript(string scriptName)
        {
            return InvokeScript(scriptName, null);
        }

        /// <summary>
        /// Executes an Active Scripting function defined in the HTML document currently loaded in the WebBrowser control. 
        /// </summary>
        /// <param name="scriptName">The name of the script method to invoke.</param>
        /// <param name="args"></param>
        /// <returns>The object returned by the Active Scripting call.</returns>
        /// <SecurityNote>
        /// Critical: Calls critical property and method. 
        /// Public OK - Demand WebPermission to protect againt cross domain scripting attacks. 
        ///           - We count on WebOC to "sandbox" the script to the zone of the page.
        /// </SecurityNote>
        [SecurityCritical]
        public object InvokeScript(string scriptName, params object[] args)
        {
            VerifyAccess();

            if (string.IsNullOrEmpty(scriptName))
            {
                throw new ArgumentNullException("scriptName");
            }

            UnsafeNativeMethods.IDispatchEx scriptObjectEx = null;
            UnsafeNativeMethods.IHTMLDocument2 htmlDocument = NativeHTMLDocument;
            if (htmlDocument != null)
            {
                scriptObjectEx = htmlDocument.GetScript() as UnsafeNativeMethods.IDispatchEx;
            }

            // Protect against the cross domain scripting attacks. 
            // We rely on the site locking feature in gerneral. But in IE 6 server side redirect is not blocked.
            // (In IE 7 it is blocked by turning on the DOCHOSTUIFLAG.ENABLE_REDIRECT_NOTIFICATION flag so that  
            // the additional BeforeNavigate2 event is fired for server side redirect.)

            // If it is our internal navigation to blank for navigating to null or load stream, 
            // or before any navigation has happened, Source will be null. 
            Uri currentSource = Source;
            if (currentSource != null)
            {
                SecurityHelper.DemandWebPermission(currentSource);
            }
            // Unfortunately, IWebBrowser2::CurrentURL does not always give us the full truth. It is not updated
            // when the browser navigates to an error page. Then, supposedly, it points to the "intended 
            // destination". (But on canceled navigation the previous URL may stay.) We don't want to allow 
            // running script from the built-in error pages as they are privileged. 
            // See http://support.microsoft.com/kb/272095. Contrary to what the KB article 
            // suggests, document.location.href is not good either. But document.URL seems to be.
            if (htmlDocument != null)
            {
                string innerURL = htmlDocument.GetUrl();
                // Again, in the special case we've deliberately navigated to about:blank we don't want to 
                // demand. But NavigatingToAboutBlank may not be true anymore, so it's not used as a condition.
                // In that case the two URL properties should match. If not, we'll demand to be safe.
                if (string.CompareOrdinal(innerURL, AxIWebBrowser2.LocationURL) != 0)
                {
                    SecurityHelper.DemandWebPermission(new Uri(innerURL, UriKind.Absolute));
                }
            }

            object retVal = null;            
            if (scriptObjectEx != null)
            {
                NativeMethods.DISPPARAMS dp = new NativeMethods.DISPPARAMS();
                dp.rgvarg = IntPtr.Zero;
                try
                {
                    // If we use reflection to call script code, we need to Assert for the UnmanagedCode permission. 
                    // But it will be a security issue when the WPF app makes a framework object available to the 
                    // hosted script via ObjectForScripting or as paramter of InvokeScript, and calls the framework
                    // API that demands the UnmanagedCode permission. We do not want the demand to succeed. However, 
                    // The stack walk will igore the native frames and keeps going until it reaches the Assert. 
                    // That is why we switch to invoking the script via IDispatch with SUCS on the methods.
                    Guid guid = Guid.Empty;
                    string[] names = new string[] { scriptName };
                    int[] dispids = new int[] { NativeMethods.DISPID_UNKNOWN };

                    HRESULT hr = scriptObjectEx.GetIDsOfNames(ref guid, names, 1, Thread.CurrentThread.CurrentCulture.LCID, dispids);
                    hr.ThrowIfFailed();

                    if (args != null)
                    {
                        // Reverse the arg order so that parms read naturally after IDispatch. (WinForms bug 187662)
                        Array.Reverse(args);
                    }
                    dp.rgvarg = (args == null) ? IntPtr.Zero : UnsafeNativeMethods.ArrayToVARIANTHelper.ArrayToVARIANTVector(args);
                    dp.cArgs = (uint)((args == null) ? 0 : args.Length);
                    dp.rgdispidNamedArgs = IntPtr.Zero;
                    dp.cNamedArgs = 0;

                    // It's important to use IDispatchEx::InvokeEx here rather than the non-Ex versions for security reasons.
                    // This version allows us to pass the IServiceProvider for security context.
                    //
                    // Calling window.open from within WPF WebBrowser control results in Access Denied script error
                    // Providing a service provider to InvokeEx only makes sense when nesting occurs (for e.g., when WPF calls
                    // a script which calls back into WPF which in turn calls the script again) and there is a need to maintain 
                    // the service provider chain. When the execution is from a root occurance, then there is no valid service 
                    // provider that will have all of the information from the stack. 
                    // 
                    // Until recently, IE was ignoring bad service providers -so our passing (IServiceProvider)htmlDocument to InvokeEx
                    // worked. IE has recently taken a security fix to ensure that it doesn't fall back to the last IOleCommandTarget
                    // in the chain it found - so now we simply pass null to indicate that this is the root call site. 

                    hr = scriptObjectEx.InvokeEx(
                        dispids[0],
                        Thread.CurrentThread.CurrentCulture.LCID,
                        NativeMethods.DISPATCH_METHOD,
                        dp,
                        out retVal, 
                        new NativeMethods.EXCEPINFO(),
                        null);
                    hr.ThrowIfFailed();
                }
                finally
                {
                    if (dp.rgvarg != IntPtr.Zero)
                    {
                        UnsafeNativeMethods.ArrayToVARIANTHelper.FreeVARIANTVector(dp.rgvarg, args.Length);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotInvokeScript));
            }
            return retVal;
        }

        #endregion Public Methods

        //----------------------------------------------
        //
        // Public Properties
        //
        //----------------------------------------------

        #region Public Properties

        /// <summary>
        /// Gets or sets the current uri of the WebBrowser control.
        /// </summary>
        /// <SecurityNote>
        ///     Critical - Accesses the critical unmanaged interface of the control.
        ///     Public OK - This does not expose the interface.
        ///               - Return the current uri is considered as safe because we only allow navigation to 
        ///                 site-of-origin for top level navigation in parital trust.
        /// </SecurityNote>
        public Uri Source
        {            
            set
            {
                VerifyAccess();

                Navigate(value);
            }
            [SecurityCritical]
            get
            {
                VerifyAccess();

                // Current url, return IWebBrowser2.LocationURL.
                string urlString = AxIWebBrowser2.LocationURL;

                // When source set to null or navigating to stream/string, we navigate to "about:blank"
                // internally. Make sure we return null in those cases. 
                // Note that the current LocationURL may not be 'about:blank' yet.
                // Also, we'll (inconsistently) return 'about:blank' in some cases--see description of 
                // _navigatingToAboutBlank.
                if (NavigatingToAboutBlank)
                {
                    urlString = null;
                }
               
                return (string.IsNullOrEmpty(urlString) ? null : new Uri(urlString));
            }
        }

        /// <summary>
        /// Gets a value indicating whether a previous page in navigation history is available.
        /// </summary>
        public bool CanGoBack
        {
            get
            {
                VerifyAccess();

                return (!IsDisposed && _canGoBack);
            }
        }

        /// <summary>
        /// Gets a value indicating whether a subsequent page in navigation history is available.
        /// </summary>
        public bool CanGoForward
        {
            get
            {
                VerifyAccess();

                return (!IsDisposed && _canGoForward);
            }
        }

        /// <summary>
        /// Gets or sets an object that can be accessed by scripting code that is contained 
        /// within a Web page in the WebBrowser control. 
        /// </summary>
        /// <SecurityNote>
        ///     Critical: Calls Marshal.IsTypeVisibleFromCom(), which has a LinkDemand for UnmanagedCode.
        ///     PublicOK: We do not expose any security critical info. 
        /// </SecurityNote>
        public object ObjectForScripting
        {
            get
            {
                VerifyAccess();

                return _objectForScripting;
            }
            [SecurityCritical]
            set
            {
                VerifyAccess();

                if (value != null)
                {
                    Type t = value.GetType();
#if NETFX
                    if (!System.Runtime.InteropServices.Marshal.IsTypeVisibleFromCom(t))
#else
                    if (!System.Runtime.InteropServices.MarshalLocal.IsTypeVisibleFromCom(t))
#endif
                    {
                        throw new ArgumentException(SR.Get(SRID.NeedToBeComVisible));
                    }
                }

                _objectForScripting = value;
                _hostingAdaptor.ObjectForScripting = value;
            }
        }

        /// <summary>
        /// The HtmlDocument for page hosted in the html page.  If no page is loaded, it returns null.
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - exposes the critical unmanaged interface of the DOM object.
        ///     Public OK - Demands unrestricted WebPermission.
        ///</SecurityNote>        
        public object Document
        {
            [SecurityCritical]
            get
            {
                VerifyAccess();

                SecurityHelper.DemandUnmanagedCode();

                return AxIWebBrowser2.Document;
            }
        }
        
        #endregion Public Properties

        //----------------------------------------------
        //
        // Public Events
        //
        //----------------------------------------------

        #region Public Events

        /// <summary>
        /// Raised before a navigation takes place. This event is fired only for 
        /// top-level page navigations.        
        /// Canceling this event prevents the WebBrowser control from navigating.
        /// </summary>
        public event NavigatingCancelEventHandler Navigating;

        /// <summary>
        /// Raised after navigation the target has been found and the download has begun. This event
        /// is fired only for top-level page navigations.
        /// </summary>
        public event NavigatedEventHandler Navigated;

        /// <summary>
        /// Raised when the WebBrowser control finishes loading a document. This event
        /// is fired only for top-level page navigations.
        /// </summary>
        public event LoadCompletedEventHandler LoadCompleted;

        #endregion Public Events

        //----------------------------------------------
        //
        // Protected Methods
        //
        //----------------------------------------------

        #region Protected Methods        

        #endregion Protected Methods

        //----------------------------------------------
        //
        // Internal Methods
        //
        //----------------------------------------------

        #region Internal Methods

        internal void OnNavigating(NavigatingCancelEventArgs e)
        {
            VerifyAccess();

            if (Navigating != null)
            {
                Navigating(this, e);
            }
        }

        internal void OnNavigated(NavigationEventArgs e)
        {
            VerifyAccess();

            if (Navigated != null)
            {
                Navigated(this, e);
            }
        }

        internal void OnLoadCompleted(NavigationEventArgs e)
        {
            VerifyAccess();

            if (LoadCompleted != null)
            {
                LoadCompleted(this, e);
            }
        }


        /// <SecurityNote>
        /// Critical: As a native object, the WebOC should not be exposed directly to partial-trust code.
        /// </SecurityNote>
        [SecurityCritical]
        internal override object CreateActiveXObject(Guid clsid)
        {
            Debug.Assert(clsid.ToString("D") == CLSID.WebBrowser);
            return _hostingAdaptor.CreateWebOC();
        }

        /// This will be called when the native ActiveX control has just been created.
        /// Inheritors of this class can override this method to cast the nativeActiveXObject
        /// parameter to the appropriate interface. They can then cache this interface
        /// value in a member variable. However, they must release this value when
        /// DetachInterfaces is called (by setting the cached interface variable to null).
        /// <SecurityNote>
        ///     Critical: This code can be exploited to pass in a bogus Activex object
        /// </SecurityNote>
        [SecurityCritical]
        internal override void AttachInterfaces(object nativeActiveXObject)
        {
            //cache the interface
            this._axIWebBrowser2 = (UnsafeNativeMethods.IWebBrowser2)nativeActiveXObject;

            //
            //Initializations
            //
            //By default _axIWebBrowser2.RegisterAsDropTarget and _axIWebBrowser2.RegisterAsBrowser
            //are set to false. Since we control navigations through webbrowser events, we can set these
            //to true if needed to allow drag n drop of documents on to the control and 
            //allow frame targetting respectively. Think its better to lock it down until this is spec-ed out

            //Set Silent property to true to suppress error dialogs (or demand permission for it here and
            //set it accordingly)            
        }

        /// See AttachInterfaces for a description of when to override DetachInterfaces.
        /// <SecurityNote>
        ///     Critical: This code references the critical object _axIWebBrowser2
        ///     TreatAsSafe: It does not expose it
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal override void DetachInterfaces()
        {
            //clear the interface. Base will release the COMObject
            _axIWebBrowser2 = null;
        }

        /// <Summary> 
        ///     Attaches to the DWebBrowserEvents2 connection point.
        /// </Summary>
        ///<SecurityNote> 
        ///     Critical - uses the critical _axIWebBrowser2 and calls _hostingAdaptor.CreateEventSink(). 
        ///     TreatAsSafe - registering to handle events is ok. The event sink object is not exposed.
        ///</SecurityNote> 
        [ SecurityCritical, SecurityTreatAsSafe ]
        internal override void CreateSink()
        {
            Debug.Assert(_axIWebBrowser2 != null);
            _cookie = new ConnectionPointCookie(_axIWebBrowser2,
                    _hostingAdaptor.CreateEventSink(),
                    typeof(UnsafeNativeMethods.DWebBrowserEvents2));
        }

        ///<Summary> 
        ///     Releases the DWebBrowserEvents2 connection point.
        ///</Summary>   
        /// <SecurityNote>
        /// Critical: Disconnecting the event sink breaks the site-locking feature.
        /// </SecurityNote>
        [SecurityCritical]
        internal override void DetachSink()
        {
            //If we have a cookie get rid of it
            if ( _cookie != null)
            {
                _cookie.Disconnect();
                _cookie = null;
            }
        }
        
        ///<SecurityNote> 
        ///     Needs to link-demand as base method link-demanded. 
        ///
        ///     Critical - calls critical WebBrowserSite - ctor. 
        ///</SecurityNote>
        [SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
        [SecurityCritical]
        internal override ActiveXSite CreateActiveXSite()
        {
            return new WebBrowserSite(this);
        }

        /// <summary>
        ///     GetDrawing - Returns the drawing content of this Visual.
        /// </summary>
        /// <remarks>
        ///     This returns a bitmap obtained by calling the PrintWindow Win32 API.
        /// </remarks>
        /// <SecurityNote>
        ///     Critical:This code Asserts an elevated permission.
        ///     TreatAsSafe: only site of origin pages can be loaded in PT, so giving out a bitmap of this window is OK.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal override System.Windows.Media.DrawingGroup GetDrawing()
        {
            // SecurityHelper.DemandWebPermission(_source.Value); // _source is null by now...

            (new UIPermission(UIPermissionWindow.AllWindows)).Assert(); // Blessed assert
            try
            {
                return base.GetDrawing();
            }
            finally
            {
                UIPermission.RevertAssert();
            }
        }

        /// <summary>
        ///     Cleans the internal state used by NavigateToStream/NavigateToString.
        /// </summary>
        /// <SecurityNote>
        ///     Critical - Resets NavigatingToAboutBlank which is used in security decisions.
        /// </SecurityNote>
        [SecurityCritical]
        internal void CleanInternalState()
        {
            NavigatingToAboutBlank = false;
            DocumentStream = null;
        }

        #endregion Internal Methods

        //----------------------------------------------
        //
        // Internal Properties
        //
        //----------------------------------------------

        #region Internal Properties       

        /// <SecurityNote>
        ///     Critical: Exposes an HTML document object, which is generally 'safe for scripting', but only
        ///         if from the same site of origin as the host application.
        /// </SecurityNote>
        internal UnsafeNativeMethods.IHTMLDocument2 NativeHTMLDocument
        {
            [SecurityCritical]
            get
            {
                object objDoc = AxIWebBrowser2.Document;
                return objDoc as UnsafeNativeMethods.IHTMLDocument2;
                // Demand WebPermission NetworkAccess.Connect if we expose this publicly
            }
        }

        /// <SecurityNote>
        ///     Critical: This code exposes an unmanaged interface with SupressUnmanagedCodeSecurity 
        ///               attribute on some methods
        /// </SecurityNote>        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal UnsafeNativeMethods.IWebBrowser2 AxIWebBrowser2
        {
            [SecurityCritical]
            get
            {
                if (_axIWebBrowser2 == null)
                {
                    if (!IsDisposed)
                    {
                        //This should call AttachInterfaces which will set this member variable
                        //We don't want to force the state to InPlaceActive yet since we don't
                        //have the parent handle yet.
                        TransitionUpTo(ActiveXHelper.ActiveXState.Running);
                    }
                    else
                    {
                        throw new System.ObjectDisposedException(GetType().Name);
                    }
                }
                // We still don't have _axIWebBrowser2. Throw an exception.
                if (_axIWebBrowser2 == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.WebBrowserNoCastToIWebBrowser2));
                }
                return _axIWebBrowser2;
            }
        }

        internal WebOCHostingAdaptor HostingAdaptor { get { return _hostingAdaptor; } }

        internal Stream DocumentStream
        {
            get
            {
                return _documentStream;
            }
            set
            {
                _documentStream = value;
            }
        }

        // This property indicates whether we are navigating to "about:blank" internally
        // because Source is set to null or navigating to stream.
        /// <SecurityNote>
        ///    Critical: NavigatingToAboutBlank is involved in making security decisions. 
        ///              We navigate to about:blank internally when navigating to null or navigation to string/stream. 
        ///              This flag is used to avoid security check per navigation source in order to enable navigation 
        ///              to about:blank in partial trust.
        ///              Setting this property to true will by pass the site locking logic. Currently it is only true when
        ///              navigating to about:blank internally for the scenarios described above.
        ///              Note that the property may become false once we are at the about:blank page. This happens,
        ///              for example, when attempting a navigation that is canceled or fails. 
        /// </SecurityNote>
        internal bool NavigatingToAboutBlank
        {
            get
            {
                return _navigatingToAboutBlank.Value;
            }
            [SecurityCritical]
            set
            {
                _navigatingToAboutBlank.Value = value;
            }
        }

        /// <summary>
        /// Launching a navigation from the Navigating event handler causes reentrancy.
        /// We keep a counter identifying the last navigation that was carried out, used in reentrancy detection logic.
        /// </summary>
        /// <SecurityNote>
        ///    Critical: tampering with the sequencing can influence clean-up decisions made, causing
        ///              the critical _navigatingToAboutBlank not to get cleaned up where needed.
        /// </SecurityNote>
        internal Guid LastNavigation
        {
            get
            {
                return _lastNavigation.Value;
            }
            [SecurityCritical]
            set
            {
                _lastNavigation.Value = value;
            }
        }

        /// <SecurityNote>
        /// Starting from v3 SP2, we host the WebOC in the IE 7+ browser process when it's running at low 
        /// integrity level ('protected mode'). This is to prevent elevation of privilege via our process in
        /// case a bug in the WebOC is exploited. PresentationHost is on IE's silent elevation list; thus, 
        /// potentially bigger damange could be effected by running malicious code in our process.
        /// 
        /// Starting from v4, we always host the WebOC in IE. This addresses other security concerns:
        ///   - Mixing CLR code and JavaScript in the same process enables more attack vectors;
        ///   - We have to play constant catch-up with IE as new Feature Control Keys and other security 
        ///     mitigations are added. By hosting the WebOC in the IE process, new FCKs automatically apply 
        ///     (at the risk of being breaking changes, but this risk is justified for partial trust XBAPs).
        ///     
        /// Note that we must keep any WebOC in the IE process even in deeply nested situations like this,
        /// if we allow them:
        ///     IE / XBAP / WebOC / XBAP / WebOC [this is in terms of container/visual nesting]
        /// If the WebOC hosted via the inner XBAP is allowed to run outside the protected-mode IE, the whole
        /// feature is defeated! To ensure we are aware of this situation, the native hosting code sets both
        /// hfHostedInWebOC and hfHostedInIE. For v4, WebOC hosting is entirely blocked in this situation. 
        /// See further explanation in COleDocument::InitDocHost().
        /// </SecurityNote>
        internal static bool IsWebOCHostedInBrowserProcess
        {
            get
            {
#if NETFX
                if(!IsWebOCPermissionRestricted)
                    return false;
                HostingFlags hf = BrowserInteropHelper.HostingFlags;
                return (hf & HostingFlags.hfHostedInIE) != 0 
                        || // Backup condition in case the low-integrity IE process is compromised. 
                           // The hfHostedInIE flag cannot be trusted, because it's derived by talking to code 
                           // in the IE process. (Theretically, one could pretend to be our Mozilla plugin by 
                           // creating a similar hosting envrionment within IE! Then no IWebBrowser...)
                           // But hfIsBrowserLowIntegrityProcess is reliable because it's determined externally.
                       (hf & HostingFlags.hfIsBrowserLowIntegrityProcess) != 0;
#else
                return false;
#endif
            }
        }

        #endregion Internal Properties

        //----------------------------------------------
        //
        // Internal Fields
        //
        //----------------------------------------------

        #region Internal Fields

        internal bool _canGoBack;
        internal bool _canGoForward;
        internal const string AboutBlankUriString = "about:blank";

        #endregion Internal Fields

        //----------------------------------------------
        //
        // Private Methods
        //
        //----------------------------------------------

        #region Private Methods
        
        /// <SecurityNote>
        ///     Critical:This code gets critical data, PresentationSource
        ///     TreatAsSafe: The PresentationSource is not exposed.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private void LoadedHandler(object sender, RoutedEventArgs args)
        {
            PresentationSource pSource = PresentationSource.CriticalFromVisual(this);

            // Note that we cannot assert this condition here. The reason is that this element might have 
            // been disconnected from the tree through one of its parents even while it waited for the 
            // pending Loaded event to fire. More details for this scenario can be found in the 
            // Windows OS Bug#1981485.
            // Invariant.Assert(pSource != null, "Loaded has fired. PresentationSource shouldn't be null");
            
            if (pSource != null && pSource.RootVisual is PopupRoot)
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotBeInsidePopup));
            }
        }

        private static void RegisterWithRBW()
        {
#if NETFX
            // if we are browser hosted, rbw should have been created here.  
            // what if RootBrowserWindow is null.  
            if (RootBrowserWindow != null)
            {
                RootBrowserWindow.AddLayoutUpdatedHandler();
            }
#endif
        }

        // Turn on all the WebOC Feature Control Keys implementing various security mitigations. 
        // Whenever possible, we do it programmatically instead of adding reg-keys so that these are on on all WPF apps. 
        // Unfortunately, some FCKs, especially newer ones, work only through the registry.
        ///<SecurityNote> 
        ///     Critical - calls critical code. 
        ///     TreatAsSafe - turns on only FCKs that make the browser control more locked-down. 
        ///</SecurityNote> 
        [ SecurityCritical, SecurityTreatAsSafe ]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId="MS.Win32.UnsafeNativeMethods.CoInternetSetFeatureEnabled(System.Int32,System.Int32,System.Boolean)", 
            Justification="CoInternetSetFeatureEnabled() returns error for an unknown FCK. We expect this to happen with older versions of IE.")]
        private static void TurnOnFeatureControlKeys()
        {
            Version osver = Environment.OSVersion.Version;
            if (osver.Major == 5 && osver.Minor == 2 && osver.MajorRevision == 0) 
            {
                // XPSP2 mitigations - not available on Server 2003 before SP1. 
                return ; 
            }

            // NOTE: If the WebOC is hosted in the browser's process, the flags we set here will have no 
            // effect on it. This is somewhat unfortunate because we may get differences in behavior, but it
            // shouldn't be much of a security issue because we host the WebOC in the browser's process when
            // it is running at low integrity level (IE 'protected mode'), so the WebOC is in a stronger 
            // sandbox there.

            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_OBJECT_CACHING, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ; 
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_ZONE_ELEVATION, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_MIME_HANDLING, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_MIME_SNIFFING, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_WINDOW_RESTRICTIONS, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_WEBOC_POPUPMANAGEMENT, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_BEHAVIORS, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_DISABLE_MK_PROTOCOL, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_LOCALMACHINE_LOCKDOWN, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_SECURITYBAND, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_RESTRICT_ACTIVEXINSTALL, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_VALIDATE_NAVIGATE_URL, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_RESTRICT_FILEDOWNLOAD, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_ADDON_MANAGEMENT, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_PROTOCOL_LOCKDOWN, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_HTTP_USERNAME_PASSWORD_DISABLE, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_SAFE_BINDTOOBJECT, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_UNC_SAVEDFILECHECK, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_GET_URL_DOM_FILEPATH_UNENCODED, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;

            // Note: For PresentationHost.exe WebOC hosting scenarios, the FEATURE_LOCALMACHINE_LOCKDOWN
            //       flag is set through the registry, as there's a known limitation of not being able
            //       to set this one programmatically.

            // IE7 and higher. Those keys are both set programmatically and through the registry, so that
            // they also can take effect to downlevel configuration; i.e. when .NET 4.0 get uninstalled
            // but PresentationHost.exe is left behind for the 3.x layer, those mitagations still take
            // effect.
            //
            // Rationale for IE7 flags set here:
            // - FEATURE_SSLUX                      - Turns on the IE7 SSL user experience, producing a full-page
            //                                        experience (you know, with scary red background and such)
            //                                        rather than the WinInet certificate error dialog.
            // - FEATURE_DISABLE_LEGACY_COMPRESSION - This forces the use of WinInet's compression support which
            //                                        is better than the UrlMon's one. No reason not to have this
            //                                        according to IE people.
            // - FEATURE_DISABLE_TELNET_PROTOCOL    - Telnet should only be used by malicious sites; there should
            //                                        be no valid use for it that's justifiable in the light of
            //                                        security.
            // - FEATURE_FORCE_ADDR_AND_STATUS      - Displays address bar and status bar in windows spawn by the
            //                                        browser, so the user always sees the address being navigated
            //                                        to, preventing spoofing.
            //
            // Rationale for IE7 flags not set here:
            // - FEATURE_TABBED_BROWSING            - Our control doesn't support tabbed browsing, so no need to
            //                                        enable shortcuts or notifications for the feature.
            // - FEATURE_DISABLE_NAVIGATION_SOUNDS  - Not a security setting.
            // - FEATURE_XMLHTTP                    - Don't want to block AJAX-style of applications.
            // - FEATURE_FEEDS                      - Keeping the old RSS display behavior; we don't have a "feed"
            //                                        detected notification mechanism.
            // - FEATURE_BLOCK_INPUT_PROMPTS        - Strictly speaking a security setting, but the spoofing threat
            //                                        is rather minimal according to IE people; we don't want to
            //                                        break existing applications relying on a JS prompt call.
            //
            // Special case:
            // - FEATURE_RESTRICT_ABOUT_PROTOCOL_IE7: Important but supposedly works only when applied through the
            //                                      resistry. We have it set for PresentationHost ().
            //                                      Standalone WebBrowser hosts will have to apply it on their own.
            //                                      (Similar problem with FEATURE_LOCALMACHINE_LOCKDOWN.)

            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_SSLUX, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_DISABLE_LEGACY_COMPRESSION, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_DISABLE_TELNET_PROTOCOL, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;

#if NETFX
            // For use of the WebOC in stand-alone applications and full-trust XBAPs, we don't want to
            // impact existing behavior causing potential different layout due to the appearance of
            // address bar and status bar. We therefore only apply the following flag where the spoofing
            // threat is the most relevant, when restricted permissions apply.
            if (IsWebOCPermissionRestricted)
            {
                UnsafeNativeMethods.CoInternetSetFeatureEnabled( NativeMethods.FEATURE_FORCE_ADDR_AND_STATUS, NativeMethods.SET_FEATURE_ON_PROCESS, true ) ;
            }
#endif

            // The relevant IE 8 FCKs are applied only in the registry because they don't work through the API. 
            // We are trying to change this unfortunate trend. .
        }
        
        ///<SecurityNote> 
        ///     Critical - Can be used to spoof HTTP headers.  
        ///              - Can do cross-domain communication via HTTP POST data.        
        ///              - Can be used to enable navigation to about:blank in PT.
        ///              - Calling IOleCommandTarget.Exec, IWebBrowser2.get_Busy.
        ///    TreatAsSafe - We only allow site-of-origin navigation programmatically for both top level
        ///                - and sub frame navigations. Spoofing against the SOO is not considered dangerous.
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        private void DoNavigate(Uri source, ref object targetFrameName, ref object postData, ref object headers, bool ignoreEscaping = false)
        {
            VerifyAccess();

            // TFS  - Calling Navigate and NavigateToStream subsequently causes internal state
            // of the WebBrowser control to become invalid. By cancelling outstanding navigations in the
            // browser, we make sure not to get a NavigationCompleted2 event for the original request while
            // we've already overwritten internal state (more specifically the NavigatingToAboutBlank and
            // DocumentStream properties), therefore avoiding the Invariant.Assert over there to fail.
            // Notice we're passing showCancelPage = false as the input parameter. This keeps the browser
            // from navigating to res://ieframe.dll/navcancl.htm because of the cancellation (which would
            // by itself cause navigation events to occur). See inetcore/ieframe/shdocvw/shvocx.cpp.
            NativeMethods.IOleCommandTarget cmdTarget = (NativeMethods.IOleCommandTarget)AxIWebBrowser2;
            object showCancelPage = false;
            cmdTarget.Exec(
                null /* default command group */,
                (int)UnsafeNativeMethods.OLECMDID.OLECMDID_STOP,
                (int)UnsafeNativeMethods.OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT,
                new object[] { showCancelPage }, /* don't navigate to res://ieframe.dll/navcancl.htm */
                0
            );

            // TFS  - Launching a navigation from the Navigating event handler causes reentrancy.
            // For more info, see WebBrowser.LastNavigation. Here we generate a new navigation identifier which
            // is used to detect reentrant calls during handling of the Navigating event.
            LastNavigation = Guid.NewGuid();

            // When source set to null or navigating to stream/string, we navigate to "about:blank" internally.
            if (source == null)
            {
                NavigatingToAboutBlank = true;
                source = new Uri(AboutBlankUriString);
            }
            else
            {
                CleanInternalState();
            }

            if (!source.IsAbsoluteUri)
            {
                throw new ArgumentException(SR.Get(SRID.AbsoluteUriOnly), "source");
            }

            // Resolve Pack://siteoforigin.
            if (PackUriHelper.IsPackUri(source))
            {
                source = BaseUriHelper.ConvertPackUriToAbsoluteExternallyVisibleUri(source);
            }

            // Block popup window. We attempted to use the default popup Manager to block pup-up windows, 
            // by passing the BrowserNavConstants.NewWindowsManaged flag to WebBrowser
            // But it did not work. New browser windows still can be opened with "_blank" in Internet zone.
            // So demand unrestricted WebPermission until we figure out a better solution. 
            if (!string.IsNullOrEmpty((string)targetFrameName))
            {
                (new System.Net.WebPermission(PermissionState.Unrestricted)).Demand();
            }
            else
            {
                // site locking. 
                // Note: navigation to "about:blank" is not enabled in partial trust. If we are navigating to 
                // "about:blank" internally as a result of setting source to null or navigating to stream/string,
                // do not demand WebPermission.
                if (!NavigatingToAboutBlank)
                {
                    // we currently demand for both top level and subframe navigations.
                    // If we allow sub frames to navigate out of site of origin programmtically, we must block cross domain communication 
                    // of all kinds, so demand when additional headers and postData are set for sub frame navigation. 
                    // The headers can be used to spoof referer headers.   
                    SecurityHelper.DemandWebPermission(source);
                }
            }

            // figure out why BrowserNavConstants.NewWindowsManaged does not work.
            object flags = (object)null; // UnsafeNativeMethods.BrowserNavConstants.NewWindowsManaged;

            // Fix for inability to navigate to a URI containing invalid UTF-8 sequences
            // BindUriHelper.UriToString does use Uri.GetComponents with the UriFormat.SafeUnescaped flag passed in,
            // causing invalid UTF-8 sequences to get dropped, resulting in a strictly speaking valid URI but for
            // some websites this causes breakage. Therefore we allow ignoring this treatment by means of string-
            // based overloads for the public Navigate methods, creating a Uri internally and using AbsoluteUri
            // to get back the URI string to feed in to the WebOC in its original form. WinForms has a similar
            // set of overloads to enable this scenario.
            object sourceString = ignoreEscaping ? source.AbsoluteUri : BindUriHelper.UriToString(source);            

            try
            {
                AxIWebBrowser2.Navigate2(ref sourceString, ref flags, ref targetFrameName, ref postData, ref headers);                
            }
            catch (COMException ce)
            {   
                // Clear internal state if Navigation fails.
                CleanInternalState();

                // "the operation was canceled by the user" - navigation failed
                // ignore this error, IE has already alerted the user. 
                if ((uint)unchecked(ce.ErrorCode) != (uint)unchecked(0x800704c7))
                {
                    throw;
                }                             
            }
        }

        /// <summary>
        /// This method helps work around IE 6 WebOC bugs related to activation state change. The problem seems 
        /// to be fixed in IE 7.
        ///   * When the security 'goldbar' pops up, it acquires focus. When the user unblocks the control, 
        ///     focus is set to the main WebOC window, but it doesn't call us on IOleInPlaceSite.OnUIActivate()
        ///     like it normally does when it gets focus. This leaves the ActiveXState at InPlaceActive, but 
        ///     it should be UIActive. Because of this, the invariant assert in TranslateAccelerator() was 
        ///     failing on a key down
        ///   * Similar case from DevDiv bug 121501: Clicking on a combobox to get its drop down list. If the 
        ///     WebOC doesn't have focus before that, it acquires it, but doesn't call OnUIActivate(). This 
        ///     fails the Assert in OnPreprocessMessageThunk().
        /// </summary>
        private void SyncUIActiveState()
        {
            if (ActiveXState != ActiveXHelper.ActiveXState.UIActive && this.HasFocusWithinCore())
            {
                Invariant.Assert(ActiveXState == ActiveXHelper.ActiveXState.InPlaceActive);
                ActiveXState = ActiveXHelper.ActiveXState.UIActive;
            }
        }

        /// <summary>
        ///     Gives the component a chance to process keyboard input.
        ///     Return value is true if handled, false if not.  Components
        ///     will generally call a child component's TranslateAccelerator
        ///     if they can't handle the input themselves.  The message must
        ///     either be WM_KEYDOWN or WM_SYSKEYDOWN.  It is illegal to
        ///     modify the MSG structure, it's passed by reference only as
        ///     a performance optimization.
        /// </summary>
        ///<SecurityNote> 
        ///     Critical - access critical data ActiveXInPlaceActiveObject and can be used to spoof input
        ///     TreatAsSafe: The interface declaration for this method has a demand on it.
        ///</SecurityNote> 
        [SecurityCritical, SecurityTreatAsSafe]
        protected override bool TranslateAcceleratorCore(ref MSG msg, ModifierKeys modifiers)
        {
            SyncUIActiveState();
            Invariant.Assert(ActiveXState >= ActiveXHelper.ActiveXState.UIActive, "Should be at least UIActive when we are processing accelerator keys");

            return (ActiveXInPlaceActiveObject.TranslateAccelerator(ref msg) == 0);
        }

        /// <summary>
        ///     Set focus to the first or last tab stop. If it can't, because it has no tab stops,
        ///     the return value is false.
        /// </summary>
        ///<SecurityNote> 
        ///     Critical: calls the critical DoVerb(), which sets focus on the control.
        ///     PublicOK: Setting focus on the WebOC is okay. Script is allowed to do that.
        ///</SecurityNote> 
        [SecurityCritical]
        protected override bool TabIntoCore(TraversalRequest request)
        {
            Invariant.Assert(ActiveXState >= ActiveXHelper.ActiveXState.InPlaceActive, "Should be at least InPlaceActive when tabbed into");

            bool activated = DoVerb(NativeMethods.OLEIVERB_UIACTIVATE);
            
            if (activated)
            {
                this.ActiveXState = ActiveXHelper.ActiveXState.UIActive;
            }
            
            return activated;
        }

        #endregion Private Methods

        //----------------------------------------------
        //
        // Private Properties
        //
        //----------------------------------------------

        #region Private Properties

#if NETFX
        /// <SecurityNote>
        /// Critical - Retrieves RBW and sets it on _rbw.
        /// TreatAsSafe - The RBW is exposed via Application.MainWindow anyhow.
        /// </SecurityNote>        
        private static RootBrowserWindow RootBrowserWindow
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                if (_rbw.Value == null)
                {
                    if (Application.Current != null)
                    {
                        _rbw.Value = Application.Current.MainWindow as RootBrowserWindow;
                    }
                }

                return _rbw.Value;
            }
        }
#endif

        #endregion Private Properties

        //----------------------------------------------
        //
        // Private Fields
        //
        //----------------------------------------------

        #region Private Fields

#if NETFX
        private static readonly bool IsWebOCPermissionRestricted = 
            !SecurityHelperPF.CallerAndAppDomainHaveUnrestrictedWebBrowserPermission();
#endif

        // Reference to the native ActiveX control's IWebBrowser2
        // Do not reference this directly. Use the AxIWebBrowser2 property instead since that
        // will cause the object to be instantiated if it is not already created.
        /// <SecurityNote>
        /// Critical - This code can be exploited to call Navigate on a page and it holds a COM interface
        /// </SecurityNote>
        [SecurityCritical]
        private UnsafeNativeMethods.IWebBrowser2                 _axIWebBrowser2;

        WebOCHostingAdaptor                                     _hostingAdaptor;

        // To hook up events from the native WebBrowser
        private ConnectionPointCookie                           _cookie;

#if NETFX
        /// <SecurityNote> 
        ///     Critical for set - We want to protect the RBW from being set from non-trusted sources.
        ///                        There's no need to make it SecurityCritical because it's exposed anyhow via Application.MainWindow.
        /// </SecurityNote> 
        private static SecurityCriticalDataForSet<RootBrowserWindow> _rbw;
#endif

        private object                                           _objectForScripting;
        private Stream                                           _documentStream;

        /// <SecurityNote> 
        ///    Critical: _navigatingToAboutBlank is involved in making security decisions. 
        ///              We navigate to about:blank internally when navigating to null or navigation to string/stream. 
        ///              This flag is used to avoid security check per navigation source in order to enable navigation 
        ///              to about:blank in partial trust.
        ///              Setting this property to true will by pass the site locking logic. Currently it is only true when
        ///              navigating to about:blank internally for the scenarios described above.
        ///              Note that the property may become false once we are at the about:blank page. This happens,
        ///              for example, when attempting a navigation that is canceled or fails. 
        /// </SecurityNote> 
        private SecurityCriticalDataForSet<bool>                    _navigatingToAboutBlank;

        /// <summary>
        /// TFS  - Launching a navigation from the Navigating event handler causes reentrancy.
        /// We keep an identifier for the last navigation that was carried out. If, during raising of the
        /// event, another navigation is launched we detect the identifier has changed, which indicates
        /// we shouldn't clean up the shared state touched by the last navigation (see WebBrowserEvent's
        /// BeforeNavigate2 method), so that the newly started navigation can continue.
        /// </summary>
        /// <SecurityNote> 
        ///    Critical: tampering with the sequencing can influence clean-up decisions made, causing
        ///              the critical _navigatingToAboutBlank not to get cleaned up where needed.
        /// </SecurityNote> 
        private SecurityCriticalDataForSet<Guid>                    _lastNavigation;

        #endregion Private Fields

        //----------------------------------------------
        //
        // Private Classes
        //
        //----------------------------------------------

        #region Private Class

        /// <summary>
        /// Logical extension to class WebBrowser. Exposes a minimal polymorphic interface that lets us host
        /// the WebOC either in-process or in the browser process (when IsWebOCHostedInBrowserProcess==true).
        /// This base class handles the in-process hosting.
        /// </summary>
        internal class WebOCHostingAdaptor
        {
            internal WebOCHostingAdaptor(WebBrowser webBrowser)
            {
                _webBrowser = webBrowser;
            }

            internal virtual object ObjectForScripting
            {
                get { return _webBrowser.ObjectForScripting; }
                set { }
            }

            /// <SecurityNote>
            /// Critical: As a native object, the WebOC should not be exposed to partial-trust code.
            ///           Asserts UnmanagedCode SecurityPermissions for the CreateInstance call.
            /// </SecurityNote>
            [SecurityCritical]
            internal virtual object CreateWebOC()
            {
                (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).Assert();
                try
                {
                    return Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(CLSID.WebBrowser)));
                }
                finally
                {
                    SecurityPermission.RevertAssert();
                }
            }

            /// <SecurityNote>
            /// Critical: WebBrowserEvent instances should not be exposed to partial-trust code.
            /// </SecurityNote>
            [SecurityCritical]
            internal virtual object CreateEventSink()
            {
                return new WebBrowserEvent(_webBrowser);
            }

            protected WebBrowser _webBrowser;
        };

#if NETFX
        /// <summary>
        /// Used when WebBrowser.IsWebOCHostedInBrowserProcess.
        /// </summary>
        private class WebOCHostedInBrowserAdaptor : WebOCHostingAdaptor
        {
            internal WebOCHostedInBrowserAdaptor(WebBrowser webBrowser) : base(webBrowser) { }

            /// <SecurityNote>
            /// Critical: Calls the native CoRegisterPSClsid().
            /// TAS: Enabling a specific interface to be marshaled. The proxy-stub code is in our PHProxy DLL.
            /// </SecurityNote>
            [SecurityCritical, SecurityTreatAsSafe]
            static WebOCHostedInBrowserAdaptor()
            {
                // IDocHostUIHandler is not marshalable ... probably because no one has needed to use it
                // cross-thread. PresentationHostProxy.dll is compiled with a clone of it and contains
                // proxy-stub code for it. Registering the proxy-stub this way rather than in the registry
                // is cleaner because it applies only to our scenarios. 
                // The same thing is done in the browser process, by our in-proc handler.
                Guid iidDHUIH = typeof(UnsafeNativeMethods.IDocHostUIHandler).GUID;
                Guid clsidPresHostProxy = new Guid("e302cb55-5f9d-41a3-9ef3-61827fb8b46d");
                int hr = UnsafeNativeMethods.CoRegisterPSClsid(ref iidDHUIH, ref clsidPresHostProxy);
                if (hr != NativeMethods.S_OK)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }

            /// <SecurityNote>
            /// Critical: Calls the SUC'd CreateIDispatchSTAForwarder().
            /// TAS: Wrapping a managed object in a native one that trivially delegates IDispatch calls is safe.
            /// </SecurityNote>
            internal override object ObjectForScripting
            {
                [SecurityCritical, SecurityTreatAsSafe]
                get
                {
                    return _threadBoundObjectForScripting;
                }

                [SecurityCritical, SecurityTreatAsSafe]
                set
                {
                    _threadBoundObjectForScripting = 
                        value == null ? null : ActiveXHelper.CreateIDispatchSTAForwarder(value);
                }
            }

            /// <SecurityNote>
            /// Critical: As a native object, the WebOC should not be exposed to partial-trust code.
            /// </SecurityNote>
            [SecurityCritical]
            internal override object CreateWebOC()
            {
                IntPtr pWebOC = Application.Current.BrowserCallbackServices.CreateWebBrowserControlInBrowserProcess();
                object webOC = Marshal.GetTypedObjectForIUnknown(pWebOC, typeof(UnsafeNativeMethods.IWebBrowser2));
                Marshal.Release(pWebOC);
                return webOC;
            }

            /// <SecurityNote>
            /// Critical: WebBrowserEvent instances should not be exposed to partial-trust code.
            /// </SecurityNote>
            [SecurityCritical]
            internal override object CreateEventSink()
            {
                return ActiveXHelper.CreateIDispatchSTAForwarder(
                    (UnsafeNativeMethods.DWebBrowserEvents2)base.CreateEventSink());
            }

            // This is a native object that wraps the ObjectForScripting provided by the application
            // in order to ensure calls arrive on WebBrowser's thread. 
            object _threadBoundObjectForScripting;
        };
#endif
        #endregion Private Class
    }
}
