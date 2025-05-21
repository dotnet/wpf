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
    /// An enumeration of modes for DismissPopupEvent.
    /// </summary>
    public enum RibbonDismissPopupMode
    {
        Always,
        MousePhysicallyNotOver
    }
}