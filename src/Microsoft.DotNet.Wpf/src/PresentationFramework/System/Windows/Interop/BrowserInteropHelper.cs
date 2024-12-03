// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements Avalon BrowserInteropHelper class, which helps
//              interop with the browser. Deprecated as XBAP is not supported.
//

namespace System.Windows.Interop
{
    /// <summary>
    /// Implements Avalon BrowserInteropHelper, which helps interop with the browser
    /// </summary>
    public static class BrowserInteropHelper
    {
        /// <summary>
        /// Returns the IOleClientSite interface
        /// </summary>
        public static object ClientSite => null;

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
        public static bool IsBrowserHosted => false;
        
        /// <summary>
        /// Returns the Uri used to launch the application.
        /// </summary>
        public static Uri Source => null;

    }
}

