// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#region Using declarations

#if RIBBON_IN_FRAMEWORK
using System.Windows.Controls.Ribbon;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for a RibbonTabHeader
    /// </summary>
    public class RibbonTabHeaderAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonTabHeaderAutomationPeer(RibbonTabHeader owner)
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

            // Borrowed from fix OffScreen fix in 4.0
            Rect boundingRect = RibbonHelper.CalculateVisibleBoundingRect(Owner);
            return (boundingRect == Rect.Empty || boundingRect.Height == 0 || boundingRect.Width == 0);
        }
#endif

    }
}
