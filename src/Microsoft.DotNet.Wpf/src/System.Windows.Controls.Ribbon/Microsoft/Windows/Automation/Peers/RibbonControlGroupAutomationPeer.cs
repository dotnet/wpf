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
    /// AutomationPeer for a RibbonControlGroup
    /// </summary>
    public class RibbonControlGroupAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonControlGroupAutomationPeer(RibbonControlGroup owner)
            : base(owner)
        {
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonControlDataAutomationPeer(item, this);
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }
    }
}
