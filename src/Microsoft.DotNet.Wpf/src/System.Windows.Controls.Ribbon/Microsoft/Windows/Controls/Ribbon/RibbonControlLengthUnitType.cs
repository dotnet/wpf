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
    ///   Indicates what kind of value a RibbonControlLength is holding.
    /// </summary>
    /// <remarks>
    ///   Note: Keep the RibbonControlLengthUnitType enum in sync with the string representation 
    ///       of units (RibbonControlLengthConverter._unitStrings). 
    /// </remarks>
    public enum RibbonControlLengthUnitType
    {
        Auto = 0,
        Pixel,
        Item,
        Star,
    }
}
