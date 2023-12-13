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
