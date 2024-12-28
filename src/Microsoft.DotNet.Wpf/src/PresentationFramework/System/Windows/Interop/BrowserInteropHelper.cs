// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements Avalon BrowserInteropHelper class, which helps
//              interop with the browser. Deprecated as XBAP is not supported.
//

using MS.Win32;
using MS.Internal;
using MS.Internal.AppModel;

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
        /// <remarks>
        /// Note that HostingFlags may not be set at the time this property is queried first. 
        /// That's why they are still separate. Also, this one is public.
        /// </remarks>
        public static bool IsBrowserHosted => false;
        
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
                return Application.Current?.MimeType == MimeType.Markup;
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
                return IsViewer && _isInitialViewerNavigation;
            }
            set
            {
                _isInitialViewerNavigation = value;
            }
        }

        private static bool _isInitialViewerNavigation;
    }
}

