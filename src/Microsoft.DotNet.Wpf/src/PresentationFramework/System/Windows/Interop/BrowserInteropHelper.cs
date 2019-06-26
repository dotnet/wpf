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
        public static object ClientSite
        {
            get
            {

                object oleClientSite = null;


                return oleClientSite;
            }
        }

        /// <summary>
        /// Gets a script object that provides access to the HTML window object,
        /// custom script functions, and global variables for the HTML page, if the XAML browser application (XBAP)
        /// is hosted in a frame.
        /// </summary>
        /// <remarks>
        /// Starting .NET Core 3.0, XBAP's are not supported - <see cref="HostScript"/> will always return <code>null</code>
        /// </remarks>
        public static dynamic HostScript => null;

        /// <summary>
        /// Returns true if the app is a browser hosted app.
        /// </summary>
        /// <remarks>
        /// Note that HostingFlags may not be set at the time this property is queried first. 
        /// That's why they are still separate. Also, this one is public.
        /// </remarks>
        public static bool IsBrowserHosted => false;

        internal static HostingFlags HostingFlags 
        { 
            get { return _hostingFlags.Value; }
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
        internal static bool IsInitialViewerNavigation
        {
            get
            {
                return IsViewer && _isInitialViewerNavigation.Value;
            }
            set
            {
                _isInitialViewerNavigation.Value = value;
            }
        }

        /// <summary>
        /// Retrieves the IServiceProvider object for the browser we're hosted in.
        /// This is used for IDispatchEx operations in the script interop feature.
        /// Critical: Returns critical type UnsafeNativeMethods.IServiceProvider
        /// </summary>
        internal static UnsafeNativeMethods.IServiceProvider HostHtmlDocumentServiceProvider
        {
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

        internal static void InitializeHostFilterInput()
        {
            ComponentDispatcher.ThreadFilterMessage +=
                                new ThreadMessageEventHandler(HostFilterInput);
        }

        private static void EnsureScriptInteropAllowed()
        {
            if (_isScriptInteropDisabled.Value == null)
            {
                _isScriptInteropDisabled.Value = SafeSecurityHelper.IsFeatureDisabled(SafeSecurityHelper.KeyToRead.ScriptInteropDisable);
            }
        }

        private static SecurityCriticalDataForSet<HostingFlags> _hostingFlags;
        private static SecurityCriticalDataForSet<bool> _isInitialViewerNavigation;
        private static SecurityCriticalDataForSet<bool?> _isScriptInteropDisabled;
        
        private static SecurityCriticalDataForSet<UnsafeNativeMethods.IServiceProvider> _hostHtmlDocumentServiceProvider;
        private static SecurityCriticalDataForSet<bool> _initializedHostScript;

        [DllImport(ExternDll.PresentationHostDll, EntryPoint="ForwardTranslateAccelerator")]
        private static extern int ForwardTranslateAccelerator(ref MSG pMsg, bool appUnhandled);
    }
}

