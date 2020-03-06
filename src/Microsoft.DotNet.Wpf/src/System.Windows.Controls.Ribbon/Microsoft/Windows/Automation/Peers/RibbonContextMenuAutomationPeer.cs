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
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    /// AutomationPeer for a RibbonContextMenu
    /// </summary>
    public class RibbonContextMenuAutomationPeer : ItemsControlAutomationPeer
    {
        public RibbonContextMenuAutomationPeer(RibbonContextMenu owner)
            : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonMenuItemDataAutomationPeer(item, this);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Menu;
        }

        // AutomationControlType.Menu must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms741841.aspx.
        protected override bool IsContentElementCore()
        {
            return false;
        }
    
    }
}
