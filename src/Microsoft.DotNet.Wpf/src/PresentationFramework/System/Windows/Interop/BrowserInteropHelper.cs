// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements Avalon BrowserInteropHelper class, which helps
//              interop with the browser
//

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using MS.Internal;
using MS.Win32;
using System.Windows.Input;
using MS.Internal.AppModel;
using MS.Internal.Interop;
using System.Threading;

using SafeSecurityHelper=MS.Internal.PresentationFramework.SafeSecurityHelper;

namespace System.Windows.Interop
{
    /// <summary>
    /// Implements Avalon BrowserInteropHelper, which helps interop with the browser
    /// </summary>
    public static class BrowserInteropHelper
    {
        /// <SecurityNote>
        /// Critical because it sets critical data.
        /// Safe because it is the static ctor, and the data doesn't go anywhere.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        static BrowserInteropHelper()
        {
            IsInitialViewerNavigation = true;
        }

        /// <summary>
        /// Returns the IOleClientSite interface
        /// </summary>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        /// </remarks>
        /// <SecurityNote>
        ///   Critical: Exposes a COM interface pointer to the IOleClientSite where the app is hosted
        ///   PublicOK: It is public, but there is a demand
        /// </SecurityNote>
        public static object ClientSite
        {
            [SecurityCritical]
            get
            {
                SecurityHelper.DemandUnmanagedCode();

                object oleClientSite = null;

#if NETFX
                if (IsBrowserHosted)
                {
                    Application.Current.BrowserCallbackServices.GetOleClientSite(out oleClientSite);
                }
#endif

                return oleClientSite;
            }
        }

        /// <SecurityNote>
        /// Critical: Calls a COM interface method.
        ///           Calls the constructor of DynamicScriptObject.
        ///           Calls InitializeHostHtmlDocumentServiceProvider.
        /// Safe: The browser's object model is safe for scripting.
        ///       The implementation of IHostBrowser.GetHostScriptObject() is responsible to block cross-domain access.
        /// </SecurityNote>
        public static dynamic HostScript
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                // Allows to disable script interop through the registry in partial trust code.
                EnsureScriptInteropAllowed();

                // IE's marshaling proxy for the HTMLWindow object doesn't work in MTA.
                Verify.IsApartmentState(ApartmentState.STA);

                IHostBrowser2 hb2 = HostBrowser as IHostBrowser2;
                if (hb2 == null)
                    return null; // One possibility is we are running under NPWPF v3.5.
                try
                {
                    var hostScriptObject = (UnsafeNativeMethods.IDispatch)hb2.HostScriptObject;
                    if (hostScriptObject == null)
                        return null;

                    var scriptObject = new DynamicScriptObject(hostScriptObject);

                    InitializeHostHtmlDocumentServiceProvider(scriptObject);

                    return scriptObject;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns true if the app is a browser hosted app.
        /// </summary>
        /// <remarks>
        /// Note that HostingFlags may not be set at the time this property is queried first. 
        /// That's why they are still separate. Also, this one is public.
        /// </remarks>
        public static bool IsBrowserHosted => false;

        /// <SecurityNote>
        /// Critical: These flags are a critical resource because they are used in security decisions.
        /// </SecurityNote>
        internal static HostingFlags HostingFlags 
        { 
            get { return _hostingFlags.Value; }
            [SecurityCritical]
            set { _hostingFlags.Value = value; }
        }
        
        /// <summary>
        /// Returns the Uri used to launch the application.
        /// </summary>
        public static Uri Source
        {
            get
            {
                return SiteOfOriginContainer.BrowserSource;
            }
        }

        /// <summary>
        /// Returns true if we are running the XAML viewer pseudo-application (what used to be XamlViewer.xbap).
        /// This explicitly does not cover the case of XPS documents (MimeType.Document).
        /// </summary>
        internal static bool IsViewer
        {
            get
            {
                Application app = Application.Current;
                return app != null && app.MimeType == MimeType.Markup;
            }
        }


        /// <summary>
        /// Returns true if the host browser is IE or the WebOC hosted in a standalone application.
        /// Also returns false if not browser-hosted.
        /// </summary>
        internal static bool IsHostedInIEorWebOC
        {
            get
            {
                return (HostingFlags & HostingFlags.hfHostedInIEorWebOC) != 0;
            }
        }

        /// <summary>
        /// Returns true if we are in viewer mode AND this is the first time that a viewer has been navigated.
        /// Including IsViewer is defense-in-depth in case somebody forgets to check IsViewer. There are other
        /// reasons why both IsViewer and IsViewerNavigation are necessary, however.
        /// </summary>
        /// <SecurityNote>
        /// Critical: setting this information is a critical resource.
        /// </SecurityNote>
        internal static bool IsInitialViewerNavigation
        {
            get
            {
                return IsViewer && _isInitialViewerNavigation.Value;
            }
            [SecurityCritical]
            set
            {
                _isInitialViewerNavigation.Value = value;
            }
        }

        ///<SecurityNote> 
        ///     Critical : Field for critical type IHostBrowser
        ///</SecurityNote> 
        [SecurityCritical]
        internal static IHostBrowser HostBrowser;

        /// <SecurityNote>
        /// Critical: Calls Marshal.ReleaseComObject().
        ///           Drops the security critical service provider stored in _hostHtmlDocumentProvider.
        /// </SecurityNote>
        [SecurityCritical]
        internal static void ReleaseBrowserInterfaces()
        {
            if (HostBrowser != null)
            {
                Marshal.ReleaseComObject(HostBrowser);
                HostBrowser = null;
            }

            if (_hostHtmlDocumentServiceProvider.Value != null)
            {
                Marshal.ReleaseComObject(_hostHtmlDocumentServiceProvider.Value);
                _hostHtmlDocumentServiceProvider.Value = null;
            }
        }

        /// <summary>
        /// Retrieves the IServiceProvider object for the browser we're hosted in.
        /// This is used for IDispatchEx operations in the script interop feature.
        /// Critical: Returns critical type UnsafeNativeMethods.IServiceProvider
        /// </summary>
        /// <SecurityNote>
        /// Critical: Returns critical type UnsafeNativeMethods.IServiceProvider
        /// </SecurityNote>
        internal static UnsafeNativeMethods.IServiceProvider HostHtmlDocumentServiceProvider
        {
            [SecurityCritical]
            get
            {
                // HostHtmlDocumentServiceProvider is used by DynamicScriptObject for IDispatchEx
                // operations in case of IE. During initialization we get the document node from
                // the DOM to obtain the required service provider. During this short timeframe
                // it's valid to have a null-value returned here. In all other cases, we make sure
                // not to run with a null service provider for sake of security.
                // See InitializeHostHtmlDocumentServiceProvider as well.
                /* TODO: straighten this with browser flags to check only when hosted in IE */
                Invariant.Assert(!(_initializedHostScript.Value && _hostHtmlDocumentServiceProvider.Value == null));

                return _hostHtmlDocumentServiceProvider.Value;
            }
        }

        /// <SecurityNote>
        /// Critical: Sets the critical _hostHtmlDocumentServiceProvider field, which is used during script
        ///           interop to let Internet Explorer make security decisions with regards to zones. By
        ///           passing in another service provider it may be possible to interfere with this logic.
        /// </SecurityNote>
        [SecurityCritical]
        private static void InitializeHostHtmlDocumentServiceProvider(DynamicScriptObject scriptObject)
        {
            // The service provider is used for Internet Explorer IDispatchEx use.
            if (   IsHostedInIEorWebOC
                && scriptObject.ScriptObject is UnsafeNativeMethods.IHTMLWindow4
                && _hostHtmlDocumentServiceProvider.Value == null)
            {
                // We use the IDispatch infrastructure to gain access to the document DOM node where
                // the IServiceProvider lives that was recommended to us by IE people. Notice during
                // this call the HostHtmlDocumentServiceProvider property here is still null, so this
                // first request is made (and succeeds properly) without the service provider in place.
                object document;
                bool foundDoc = scriptObject.TryFindMemberAndInvokeNonWrapped("document",
                                                                              NativeMethods.DISPATCH_PROPERTYGET,
                                                                              true  /* cache DISPID, why not? */,
                                                                              null, /* arguments */
                                                                              out document);

                // The fact the host script is required to be a IHTMLWindow4 here ensures there is
                // document property and because we're dealing with IE, we know it has a service
                // provider on it.
                Invariant.Assert(foundDoc);
                _hostHtmlDocumentServiceProvider.Value = (UnsafeNativeMethods.IServiceProvider)document;

                // See HostHtmlDocumentServiceProvider property get accessor for more information on the use of
                // this field to ensure we got a valid service provider.
                _initializedHostScript.Value = true;
            }
        }

        /// <SecurityNote>
        ///     Critical: this calls ForwardTranslateAccelerator, which is SUC'ed.
        /// </SecurityNote>
        [SecurityCritical]
        private static void HostFilterInput(ref MSG msg, ref bool handled)
        {
            WindowMessage message = (WindowMessage)msg.message;
            // The host gets to see input, keyboard and mouse messages.
            if (message == WindowMessage.WM_INPUT ||
                (message >= WindowMessage.WM_KEYFIRST && message <= WindowMessage.WM_IME_KEYLAST) ||
                (message >= WindowMessage.WM_MOUSEFIRST && message <= WindowMessage.WM_MOUSELAST))
            {
                if (MS.Win32.NativeMethods.S_OK == ForwardTranslateAccelerator(ref msg, false))
                {
                    handled = true;
                }
            }
        }

        /// <summary> This hook gets a "last chance" to handle a key. Such applicaton-unhandled
        /// keys are forwarded to the browser frame.
        /// </summary>
        /// <SecurityNote>
        ///     Critical: this calls ForwardTranslateAccelerator, which is SUC'ed.
        /// </SecurityNote>
        [SecurityCritical]
        internal static IntPtr PostFilterInput(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (!handled)
            {
                if ((WindowMessage)msg >= WindowMessage.WM_KEYFIRST && (WindowMessage)msg <= WindowMessage.WM_IME_KEYLAST)
                {
                    MSG m = new MSG(hwnd, msg, wParam, lParam, SafeNativeMethods.GetMessageTime(), 0, 0);
                    if (MS.Win32.NativeMethods.S_OK == ForwardTranslateAccelerator(ref m, true))
                    {
                        handled = true;
                    }
                }
            }
            return IntPtr.Zero;
        }

        /// <SecurityNote>
        ///     Critical: this attaches an event to ThreadFilterMessage, which requires an assert
        ///     Safe: doesn't expose anything, just does some internal plumbing stuff
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static void InitializeHostFilterInput()
        {
            (new UIPermission(PermissionState.Unrestricted)).Assert(); // Blessed assert
            try
            {
                ComponentDispatcher.ThreadFilterMessage +=
                                    new ThreadMessageEventHandler(HostFilterInput);
            }
            finally
            {
                UIPermission.RevertAssert();
            }
        }

        /// <SecurityNote>
        ///     Critical: setting critical _isScriptInteropDisabled flag.
        ///     Safe: _isScriptInteropDisabled is set from a trusted source.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        private static void EnsureScriptInteropAllowed()
        {
            if (_isScriptInteropDisabled.Value == null)
            {
                _isScriptInteropDisabled.Value = SafeSecurityHelper.IsFeatureDisabled(SafeSecurityHelper.KeyToRead.ScriptInteropDisable);
            }

            // Similar approach as with WebBrowser.cs.
            if (_isScriptInteropDisabled.Value.Value)
            {
                // Feature is disabled - demand unrestricted WebBrowserPermission to hand out the script object.
                MS.Internal.PresentationFramework.SecurityHelper.DemandWebBrowserPermission();
            }
            else
            {
                // Feature is enabled - demand Safe level to hand out the script object, granted in Partial Trust by default.
                (new WebBrowserPermission(WebBrowserPermissionLevel.Safe)).Demand();
            }
        }

        private static SecurityCriticalDataForSet<HostingFlags> _hostingFlags;
        private static SecurityCriticalDataForSet<bool> _isInitialViewerNavigation;
        private static SecurityCriticalDataForSet<bool?> _isScriptInteropDisabled;
        
        ///<SecurityNote> 
        ///     Critical : Field for critical type UnsafeNativeMethods.IServiceProvider
        ///</SecurityNote> 
        [SecurityCritical]
        private static SecurityCriticalDataForSet<UnsafeNativeMethods.IServiceProvider> _hostHtmlDocumentServiceProvider;
        private static SecurityCriticalDataForSet<bool> _initializedHostScript;

        ///<SecurityNote> 
        ///     Critical - call is SUC'ed
        ///</SecurityNote> 
        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [DllImport(ExternDll.PresentationHostDll, EntryPoint="ForwardTranslateAccelerator")]
        private static extern int ForwardTranslateAccelerator(ref MSG pMsg, bool appUnhandled);
    }
}

