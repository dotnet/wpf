// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents: Internal class that exposes the culture the platform is localized to.
//
//

using System;
using System.Globalization;
using System.Windows;
using MS.Internal.WindowsBase;      

namespace MS.Internal
{
    /// <summary>
    /// Exposes the CultureInfo for the culture the platform is localized to.
    /// </summary>    
    [FriendAccessAllowed]
    internal static class PlatformCulture
    {
        /// <summary>
        /// Culture the platform is localized to.
        /// </summary>    
        public static CultureInfo Value
        {
            get 
            {
                // Get the UI Language from the string table
                string uiLanguage = SR.Get(SRID.WPF_UILanguage);
                Invariant.Assert(!string.IsNullOrEmpty(uiLanguage), "No UILanguage was specified in stringtable.");
    
                // Return the CultureInfo for this UI language.
                return new CultureInfo(uiLanguage);
            }
        }
}
}
