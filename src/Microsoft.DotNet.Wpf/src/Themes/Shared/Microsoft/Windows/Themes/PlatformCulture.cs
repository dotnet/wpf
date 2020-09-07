// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Diagnostics;
using System.Globalization;
using System.Windows;

using System;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Public class used to expose some properties of the culture
    /// the platform is localized to.
    /// </summary>
    public static class PlatformCulture
    {
        /// <summary>
        /// FlowDirection of the culture the platform is localized to.
        /// </summary>
        public static FlowDirection FlowDirection
        {
            get
            {
                if (_platformCulture == null)
                {
                    _platformCulture = MS.Internal.PlatformCulture.Value;
                }
                Debug.Assert(_platformCulture != null);

                return _platformCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }
        }

        private static CultureInfo _platformCulture;
    }
}
