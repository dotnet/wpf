// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    /// <summary>
    /// An enumeration of available image sizes.
    /// </summary>
    public enum RibbonImageSize
    {
        /// <summary>
        /// Indicates that the image should be collapsed
        /// </summary>
        Collapsed,

        /// <summary>
        /// Indicates that a small image should be used. (Usually 16x16 at 96dpi)
        /// </summary>
        Small,

        /// <summary>
        /// Indicates that a large image should be used. (Usually 32x32 at 96dpi)
        /// </summary>
        Large
    }
}