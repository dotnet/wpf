// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//          Enum describing the NavigationMode - {New, Back, Forward, Refresh} where New means a new navigation,
//          Forward, Back, and Refresh mean the navigation was initiated from the GoForward, GoBack, or
//          Refresh method (or corresponding UI button).
//

using System;

namespace System.Windows.Navigation
{
    #region NavigationMode Enum
    /// <summary>
    /// Enum describing the NavigationMode - {New, Back, Forward, Refresh} where New means a new navigation,
    /// Forward, Back, and Refresh mean the navigation was initiated from the GoForward, GoBack, or
    /// Refresh method (or corresponding UI button).
    /// </summary>
    public enum NavigationMode : byte
    {
        /// <summary>
        /// New navigation
        /// </summary>
        New,
        /// <summary>
        /// Navigating back in history
        /// </summary>
        Back,
        /// <summary>
        /// Navigating back in history
        /// </summary>
        Forward,
        /// <summary>
        /// Refreshing the current page. We could be refreshing a page in history
        /// </summary>
        Refresh,
    }

    #endregion NavigationMode Enum
}

