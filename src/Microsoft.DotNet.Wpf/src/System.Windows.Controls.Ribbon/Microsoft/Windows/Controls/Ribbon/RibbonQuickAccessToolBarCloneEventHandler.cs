// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    /// <summary>
    ///     Event handler type for RibbonQuickAccessToolBarCloneEvent.
    /// </summary>
    public delegate void RibbonQuickAccessToolBarCloneEventHandler(object sender, RibbonQuickAccessToolBarCloneEventArgs e);
}