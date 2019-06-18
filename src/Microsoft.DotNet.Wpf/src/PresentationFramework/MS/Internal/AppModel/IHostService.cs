// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// Description:
//      Internal interface used for setting up the browser hosting
//      environment
//
//
//
//
// 
//---------------------------------------------------------------------------

using System;

using System.Windows;
using System.Windows.Controls;
using System.Security;
using System.Security.Permissions;
namespace MS.Internal.AppModel
{
    // <summary>
    // Internal interface used to set up the hosting environment for browser hosting
    // </summary>
    internal interface IHostService
    {        
        // <summary>
        // The client window passed in to host 
        // Needed for non-Avalon host scenarios
        // when the winow needs re-positioning within the host's UI
        // </summary>
        RootBrowserWindowProxy RootBrowserWindowProxy
        {
            get;
        }

        // <summary>
        // get the HWND of the host
        // We use this to parent our first Window to this window.
        // </summary>

        IntPtr HostWindowHandle
        {
            get;
        }
    }
}
