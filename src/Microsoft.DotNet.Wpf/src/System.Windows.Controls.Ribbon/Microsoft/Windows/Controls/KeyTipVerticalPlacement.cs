// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     Enumeration for vertical placement of the keytip
    ///     with respect to its placement target.
    /// </summary>
    public enum KeyTipVerticalPlacement
    {
        KeyTipTopAtTargetTop = 0,
        KeyTipTopAtTargetCenter,
        KeyTipTopAtTargetBottom,
        KeyTipCenterAtTargetTop,
        KeyTipCenterAtTargetCenter,
        KeyTipCenterAtTargetBottom,
        KeyTipBottomAtTargetTop,
        KeyTipBottomAtTargetCenter,
        KeyTipBottomAtTargetBottom
    }
}