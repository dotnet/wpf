// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description:
//      Interop services between windows client applications
//      and the browser host. This interface is implemented on
//      the client application end to support services to the 
//      unmanaged docobj hosted in the browser 
//
//  ***********************IMPORTANT**************************
//
//      If you change any of the interface definitions here
//      make sure you also change the interface definitions
//      in the unmanaged side. (wcp\host\inc\hostservices.idl)
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

namespace MS.Internal.AppModel
{
    //********************************************************************************************//
    //  IMPORTANT:  IMPORTANT:  IMPORTANT:  IMPORTANT:                                            //
    //********************************************************************************************//
    //  If you change or update this interface, make sure you update the definitions in 
    //  wcp\host\inc\hostservices.idl

    [Flags]
    enum HostingFlags
    {
        hfHostedInIE = 1,   // Not mutually exclusive! See master definition in the IDL file.
        hfHostedInWebOC = 2,//
        hfHostedInIEorWebOC = 3,
        hfHostedInMozilla = 4,
        hfHostedInFrame = 8, // hosted in an HTML frame or iframe element or WebOC in the IE process
        hfIsBrowserLowIntegrityProcess = 0x10,
        hfInDebugMode = 0x20
    };

    // <summary>
    // This interface is used to host Windows Client Applications in the browser
    // The unmanaged docobj server communicates with the application through this
    // interface using COM interop.
    // </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("a0aa9153-65b4-3b57-9f2b-126f9c76c9f5")]
    internal interface IBrowserHostServices
    {
        // <summary> This method inits and runs the server </summary> 
        // <returns>Int indicating whether the exit code of the application, failure could also 
        // mean that the app was not launched successfully</returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int Run([In, MarshalAs(UnmanagedType.LPWStr)]  string strUrl,
                 [In, MarshalAs(UnmanagedType.LPWStr)]  string strFragment,
                                                        MimeType mime,
                 [In, MarshalAs(UnmanagedType.LPWStr)]  string strDebugSecurityZoneURL,
                 [In, MarshalAs(UnmanagedType.LPWStr)]  string strApplicationId,
                 [In, MarshalAs(UnmanagedType.Interface)] object storageIUnknown,
                 [In, MarshalAs(UnmanagedType.Interface)] object loadByteArray,
                                                        HostingFlags hostingFlags,
                                                        INativeProgressPage nativeProgressPage,
                 [In, MarshalAs(UnmanagedType.BStr)]    string bstrProgressAssemblyName,
                 [In, MarshalAs(UnmanagedType.BStr)]    string bstrProgressClassName,
                 [In, MarshalAs(UnmanagedType.BStr)]    string bstrErrorAssemblyName,
                 [In, MarshalAs(UnmanagedType.BStr)]    string bstrErrorClassName,
                                                        IHostBrowser hostBrowser
            );

        // <summary> Reparent the viewport </summary>
        void SetParent(IntPtr parentHandle);

        // <summary> Show the viewport     </summary> 
        void Show([MarshalAs(UnmanagedType.Bool)]bool showView);

        // <summary> Move the viewport     </summary>
        void Move(int x, int y, int width, int height);

        /// <summary>
        /// Used by C# hosting code to get back to the native browser hosting code.
        /// (The interface type is IBrowserCallbackServices.)
        /// </summary>
        void SetBrowserCallback([In, MarshalAs(UnmanagedType.Interface)]object browserCallback);

        // <summary>If the Application is loaded we use LoadHistory else create a new app object </summary>
        // <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsAppLoaded();

        // <summary>Returns the Environment.ExitCode set by the application object when it Shutdown</summary>
        // <returns></returns>
        [PreserveSig]
        int GetApplicationExitCode();

        // <summary>Returns whether a journalEntry at that index is invokable. If the entry is a frame
        // and we are not in the context of its host page, we return failure</summary>
        // <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool CanInvokeJournalEntry([In, MarshalAs(UnmanagedType.I4)] int entryId);

        //<summary> IPersistHistory::SaveHistory implementation called
        //when hosted in the browser </summary>
        void SaveHistory([MarshalAs(UnmanagedType.Interface)]object ucomIStream,
                         [MarshalAs(UnmanagedType.Bool)]bool persistEntireJournal,
                         [Out, MarshalAs(UnmanagedType.I4)] out int entryIndex,
                         [Out, MarshalAs(UnmanagedType.LPWStr)]out string url,
                         [Out, MarshalAs(UnmanagedType.LPWStr)]out string title);

        //<summary> IPersistHistory::LoadHistory implementation called
        //when hosted in the browser </summary>
        void LoadHistory([MarshalAs(UnmanagedType.Interface)]object ucomIStream);

        //<summary> 
        // IOleCommandTarget::QueryStatus called when hosted in the browser
        //</summary>
        [PreserveSig]
        int QueryStatus([MarshalAs(UnmanagedType.LPStruct)]Guid guidCmdGroup, [In] uint command, [Out] out uint flags);

        //<summary> 
        // IOleCommandTarget::Exec called when hosted in the browser
        //</summary>
        [PreserveSig]
        int ExecCommand([MarshalAs(UnmanagedType.LPStruct)]Guid guidCmdGroup, uint command, object arg);

        /// <summary> Shuts down the application. </summary>
        /// <remarks>
        /// The "post" in the method name is legacy. Now all of Application's shutdown work is complete 
        /// when this method returns. In particular, the managed Dispatcher is shut down.
        /// </remarks>
        void PostShutdown();

        // <summary> Activate or deactivate RootBrowswerWindow  </summary>
        void Activate([MarshalAs(UnmanagedType.Bool)]bool fActivated);

        void TabInto(bool forward);

        /// <summary>
        /// Returns true is the focused element wants the backspace key
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool FocusedElementWantsBackspace();
    }

    //********************************************************************************************//
    //  IMPORTANT:  IMPORTANT:  IMPORTANT:  IMPORTANT:                                            //
    //********************************************************************************************//
    //  If you change or update this interface, make sure you update the definitions in 
    //  wcp\host\inc\hostservices.idl
    //  In addition, make sure the enum is updated in wcp\host\startup\shellhandler.cxx
    internal enum MimeType
    {
        Unknown = 0,
        Document = 1,
        Application = 2,
        Markup = 3
    }

    //********************************************************************************************//
    //  IMPORTANT:  IMPORTANT:  IMPORTANT:  IMPORTANT:                                            //
    //********************************************************************************************//
    //Start with 8001 , the enum defined on the managed world starts with 8001  as well
    //KEEP THESE IN SYNC
    //The ApplicationCommands enums in wcp\host\inc\hostservices.idl and IBrowserHostServices.cs 
    //and the menuIDs wcp\host\docobj\resource.hxx and resources.rc
    //
    internal enum AppCommands
    {
        Edit_Cut = 8001,
        Edit_Copy,
        Edit_Paste,
        Edit_SelectAll,
        Edit_Find,

        Edit_Digitalsignatures,
        Edit_Digitalsignatures_SignDocument,
        Edit_Digitalsignatures_RequestSignature,
        Edit_Digitalsignatures_ViewSignature,

        Edit_Permission,
        Edit_Permission_Set,
        Edit_Permission_View,
        Edit_Permission_Restrict,

        View_StatusBar,
        View_Stop,
        View_Refresh,
        View_FullScreen,

        View_Zoom,
        View_Zoom_In,
        View_Zoom_Out,
        View_Zoom_400,
        View_Zoom_250,
        View_Zoom_150,
        View_Zoom_100,
        View_Zoom_75,
        View_Zoom_50,
        View_Zoom_25,
        View_Zoom_PageWidth,
        View_Zoom_WholePage,
        View_Zoom_TwoPages,
        View_Zoom_Thumbnails,
    }

    internal enum AppMenus
    {
        EditMenu = 0x3020,
        ViewMenu = 0x3040
    }

    //***Keep in sync with host\Inc\HostServices.idl.
    internal enum EditingCommandIds : uint
    {
        Backspace = 1,
        Delete = 2
    };
}
