// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Managed definition for IBrowserCallbackServices & IHostBrowser, used for
//      communicating from managed code back to the native DocObject code and the 
//      in-proc handlers in the host browser.
//
//  ***********************IMPORTANT**************************
//
//      If you change any of the interface definitions here
//      make sure you also change the interface definitions
//      on the native side (src\host\inc\HostServices.idl & HostSupport.idl).
//      If you are not sure about how to define it 
//      here, TEMPORARILY mark the interface as 
//      ComVisible in the managed side, use tlbexp to generate
//      a typelibrary from the managed dll and copy the method
//      signatures from there. REMEMBER to remove the ComVisible
//      in the managed code when you are done. 
//      Defining the interfaces at both ends prevents us from
//      publicly exposing these interfaces to the outside world.
//      In order for marshaling to work correctly, the vtable
//      and data types should match EXACTLY in both the managed
//      and unmanaged worlds
//

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Interop;
using MS.Internal;
using MS.Utility;
using MS.Internal.Interop;

namespace MS.Internal.AppModel
{
    //********************************************************************************************//
    //  IMPORTANT:  IMPORTANT:  IMPORTANT:  IMPORTANT:                                            //
    //********************************************************************************************//
    //  If you change or update this interface, make sure you update the definitions in 
    //  wcp\host\inc\hostservices.idl

    /// <summary>
    /// Internal interface used for Interop in browser hosting scenarios. This 
    /// interface is passed in by the Docobj Server hosted in the browser and is
    /// used to communicate from the Windows Client application back to the browser
    /// The master definition is in HostServices.idl.
    /// </summary>
    /// <remarks>
    /// The original (v1) interface has been partly superseded by IHostBrowser.
    /// </remarks>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5FFAD804-61C8-445c-8C31-A2101C64C510")]
    internal interface IBrowserCallbackServices
    {
        void OnBeforeShowNavigationWindow();

        /// <summary>
        /// Causes the browser host to fire a ReadyState change to Complete to let
        /// shdocvw know that the navigation is complete
        /// </summary>
        /// <param name="readyState"></param>
        void PostReadyStateChange([In, MarshalAs(UnmanagedType.I4)] int readyState);

        /// <summary>
        /// Allows browser host to navigate to the url. This method is typically called 
        /// to delegate navigation back to the browser for mime types we don't handle eg: html
        /// </summary>
        /// <param name="url"></param>
        /// <param name="targetName"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        void DelegateNavigation([In, MarshalAs(UnmanagedType.BStr)] string url, [In, MarshalAs(UnmanagedType.BStr)] string targetName, [In, MarshalAs(UnmanagedType.BStr)] string headers);

        /// <summary>
        /// Notifies the avalon host to update the address bar with the current url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool UpdateAddressBar([In, MarshalAs(UnmanagedType.BStr)] string url);

        /// <summary>
        /// When the internal Journal state changes, we need to make sure it is reflected
        /// appropriately in the browser. Adding entries will make this happen automatically 
        /// since we explicitly add entries to the browser's TravelLog
        /// We need this callback for the following purposes
        /// 1.. Deleting visible entries will not reflect the change immediately unless
        /// we explicitly notify the browser (think the browser calls CanInvoke but its nice
        /// to update UI immediately)
        /// 2. Back/Forward state needs to be updated automatically when frames are 
        /// programmatically removed from the tree. Since frames don't have their own 
        /// journal, reparenting a frame to a new tree doesn't affect the new tree.
        /// Its only the tree state where it is being removed from that is affected.
        /// </summary>
        [PreserveSig]
        void UpdateBackForwardState();

        /// <summary>
        /// Add entry to shdocvw's TravelLog. Will fail on downlevel platform.
        /// </summary>
        /// <param name="topLevelNav"></param>
        /// <param name="addNewEntry"></param>
        /// <returns></returns>
        void UpdateTravelLog([In, MarshalAs(UnmanagedType.Bool)]bool addNewEntry);

        /// <summary>
        /// Change status of progress bar.
        /// </summary>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool UpdateProgress([In, MarshalAs(UnmanagedType.I8)]long cBytesCompleted, [In, MarshalAs(UnmanagedType.I8)]long cBytesTotal);

        /// <summary>
        /// Change the download state (spin the globe/wave the flag).
        /// </summary>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool ChangeDownloadState([In]bool fIsDownloading);

        /// <summary>
        /// Is this a downlevel platform that is not fully integrated
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsDownlevelPlatform();

        /// <summary>
        /// Check if browser is shutting us down
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsShuttingDown();

        /// <summary>
        /// Moves focus out of the application, to the browser frame.
        /// </summary>
        [PreserveSig]
        bool TabOut(bool forward);

        /// <summary>
        /// When an unhandled exception occurs in PresentationHost a stack trace is generated
        /// and passed to native code via this method.  Then an html error page is generated
        /// and the browser navigates to it.
        /// NOTE: There's also a DLL-exported function from PresentationHostDll for this purpose.
        ///   See DocObjHost.ProcessUnhandledException().
        /// </summary>
        [PreserveSig]
        void ProcessUnhandledException([In, MarshalAs(UnmanagedType.BStr)] string pErrorMsg);

        /// <summary>
        /// Returns the IOleClientSite interface
        /// </summary>
        [PreserveSig]
        int GetOleClientSite([Out, MarshalAs(UnmanagedType.IUnknown)] out object oleClientSite);

        /// <summary>
        /// Asks the browser to re-query for command status
        /// </summary>
        [PreserveSig]
        int UpdateCommands();

        /// <remarks>
        /// The return value is declared as an IntPtr, not as a typed IWebBrowser2 interface, to prevent CLR 
        /// Remoting from getting involved when the object is passed cross-AppDomain. When making calls on this
        /// interface, there is no point in switching to the default AppDomain, given that that object actually
        /// lives in another process.
        /// The caller must call Release() on the COM interface.
        /// </remarks>
        IntPtr CreateWebBrowserControlInBrowserProcess();
    }


    /// <summary>
    /// [See master definition in HostSupport.idl.]
    /// </summary>
    [ComImport, Guid("AD5D6F02-5F4E-4D77-9FC0-381981317144"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IHostBrowser
    {
        /// <summary>
        /// Returns the browser's top-level URL and whether the DocObject is top-level or in a frame.
        /// If querying from a frame in a less secure zone than the top window, NULL may be returned for top-level URL.
        /// </summary>
        string DetermineTopLevel(out bool pbIsTopLevel);

        /// <summary>
        /// Delegates a navigation to the browser. This may cause the Avalon application to be shut down.
        /// targetName is the name of a frame/window or one of the predefined targets: _parent, _blank, etc.
        /// Normally, IBCS.DelegateNavigation() should be used instead. It calls CoAllowSetForegroundWindow() 
        /// to let a new browser window become active.
        /// </summary>
        void Navigate(string url, string targetName = null, string headers = null);

        void GoBack();
        void GoForward();

        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
        [PreserveSig]
        long SetStatusText([MarshalAs(UnmanagedType.LPWStr)] string text);

        void SetWidth(uint width);
        void SetHeight(uint height);
        uint GetWidth();
        uint GetHeight();
        int GetLeft();
        int GetTop();

        // These methods should not be used directly. They are used in the implementation of the cookie shim in PHDLL.
        // Managed code can use Application.Get/SetCookie().
        string GetCookie_DoNotUse(string url, string cookieName, bool thirdParty);
        void SetCookie_NoNotUse(string url, string cookieName, string cookieData, bool thirdParty, string P3PHeader = null);

        [PreserveSig]
        MS.Internal.Interop.HRESULT GetUserAgentString(out string userAgent);

        // The implementation of IBCS.CreateWebBrowserControlInBrowserProcess() performs an important security
        // check before calling this method.
        void CreateWebBrowserControl_DoNotUse([Out] out IntPtr ppWebBrowser);

        void TabOut_v35SP1QFE(bool forward);
    };

    [ComImport, Guid("AD5D6F03-0002-4D77-9FC0-381981317144"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IHostBrowser2
    {
        // Use IBCS.TabOut() instead. The implementation of TabOut is not fully factored out yet.
        void TabOut_DoNotUse(bool forward);

        object HostScriptObject { [return: MarshalAs(UnmanagedType.IDispatch)] get; }

        string PluginName { get; }
        string PluginVersion { get; }
    };
}
