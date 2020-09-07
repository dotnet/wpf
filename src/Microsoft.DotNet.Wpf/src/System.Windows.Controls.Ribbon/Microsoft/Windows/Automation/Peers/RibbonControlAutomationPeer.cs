// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{
    #region Using declarations

    using System;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for RibbonControl
    /// </summary>
    public class RibbonControlAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonControlAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

#if !RIBBON_IN_FRAMEWORK
        ///
        override protected bool IsOffscreenCore()
        {
            if (!Owner.IsVisible)
                return true;

            Rect boundingRect = RibbonHelper.CalculateVisibleBoundingRect(Owner);
            return (boundingRect == Rect.Empty || boundingRect.Height == 0 || boundingRect.Width == 0);
        }
#endif
    }
}
