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
    /// AutomationPeer that is a wrapper around RibbonGroup.Header object
    /// </summary>
    public class RibbonGroupHeaderAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonGroupHeaderAutomationPeer(FrameworkElement owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Header;
        }

        // AutomationControlType.Header must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms753110.aspx
        protected override bool IsContentElementCore()
        {
            return false;
        }

        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        protected override string GetNameCore()
        {
            AutomationPeer ribbonGroupPeer = GetParent();
            if (ribbonGroupPeer != null)
            {
                return ribbonGroupPeer.GetName();
            }

            return string.Empty;
        }
    }
}
