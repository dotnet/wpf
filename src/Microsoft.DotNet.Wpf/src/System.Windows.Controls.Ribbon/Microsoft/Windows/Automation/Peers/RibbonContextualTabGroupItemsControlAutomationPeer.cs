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
    /// AutomationPeer for a RibbonContextualTabGroupItemsControl
    /// </summary>
    public class RibbonContextualTabGroupItemsControlAutomationPeer : ItemsControlAutomationPeer
    {

        public RibbonContextualTabGroupItemsControlAutomationPeer(RibbonContextualTabGroupItemsControl owner)
            : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonContextualTabGroupDataAutomationPeer(item, this);
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            // Does not allow scrolling
            if (patternInterface == PatternInterface.Scroll)
            {
                return null;
            }
            return base.GetPattern(patternInterface);
        }
    }
}
