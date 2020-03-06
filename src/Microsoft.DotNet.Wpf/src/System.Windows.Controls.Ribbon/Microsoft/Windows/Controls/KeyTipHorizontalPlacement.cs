// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls
#else
namespace Microsoft.Windows.Controls
#endif
{
    /// <summary>
    ///     Enumeration for horizontal placement of keytip
    ///     with respect to its placement target.
    /// </summary>
    public enum KeyTipHorizontalPlacement
    {
        KeyTipLeftAtTargetLeft = 0,
        KeyTipLeftAtTargetCenter,
        KeyTipLeftAtTargetRight,
        KeyTipCenterAtTargetLeft,
        KeyTipCenterAtTargetCenter,
        KeyTipCenterAtTargetRight,
        KeyTipRightAtTargetLeft,
        KeyTipRightAtTargetCenter,
        KeyTipRightAtTargetRight
    }
}