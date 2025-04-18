// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



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
    /// AutomationPeer for a RibbonTabHeaderItemsControl
    /// </summary>
    public class RibbonTabHeaderItemsControlAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonTabHeaderItemsControlAutomationPeer(RibbonTabHeaderItemsControl owner)
            : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonTabHeaderDataAutomationPeer(item, this);
        }
    }
}
