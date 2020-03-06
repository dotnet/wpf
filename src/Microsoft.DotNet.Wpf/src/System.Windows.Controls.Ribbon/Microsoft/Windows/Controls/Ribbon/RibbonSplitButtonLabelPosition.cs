// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Data;
using System;
using System.Globalization;
#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    #region Using declarations
    #endregion

    /// <summary>
    ///   An enum that describes whether the RibbonSplitButton's label should be positioned
    ///   with the the 'Header' part or the 'DropDown' part 
    ///   when the RibbonSplitButton is in Medium variant.
    /// </summary>
    public enum RibbonSplitButtonLabelPosition
    {
        /// <summary>
        ///   Indicates that the label should be positioned with the
        ///   'Header' part of the RibbonSplitButton.
        /// </summary>
        Header,

        /// <summary>
        ///   Indicates that the label should be positioned with the
        ///   'DropDown' part of the RibbonSplitButton.
        /// </summary>
        DropDown,
    }
}
