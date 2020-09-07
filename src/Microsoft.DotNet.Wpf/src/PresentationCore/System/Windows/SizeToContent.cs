// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

namespace System.Windows
{
    /// <summary>
    /// SizeToContent
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum SizeToContent
    {
        /// <summary>
        /// Does not size to content
        /// </summary>
        Manual = 0,
        /// <summary>
        /// Sizes Width to content's Width
        /// </summary>        
        Width = 1,
        /// <summary>
        /// Sizes Height to content's Height
        /// </summary>
        Height = 2,
        /// <summary>
        /// Sizes both Width and Height to content's size
        /// </summary>
        WidthAndHeight = 3,
        // Please update IsValidSizeToContent if there are name changes.
    }
}
