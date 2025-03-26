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

namespace MS.Internal.AppModel
{
    //********************************************************************************************//
    //  IMPORTANT:  IMPORTANT:  IMPORTANT:  IMPORTANT:                                            //
    //********************************************************************************************//
    //  If you change or update this interface, make sure you update the definitions in 
    //  wcp\host\inc\hostservices.idl

    [Flags]
    internal enum HostingFlags
    {
        hfHostedInIE = 1,   // Not mutually exclusive! See master definition in the IDL file.
        hfHostedInWebOC = 2,//
        hfHostedInIEorWebOC = 3,
        hfHostedInMozilla = 4,
        hfHostedInFrame = 8, // hosted in an HTML frame or iframe element or WebOC in the IE process
        hfIsBrowserLowIntegrityProcess = 0x10,
        hfInDebugMode = 0x20
    };


}
