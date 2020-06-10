// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
#if RIBBON_IN_FRAMEWORK
using System.Windows.Controls.Ribbon;
#else
using Microsoft.Windows.Controls.Ribbon;
#endif
using System.Windows;
using System.Windows.Controls;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Automation.Peers
#else
namespace Microsoft.Windows.Automation.Peers
#endif
{
    /// <summary>
    /// AutomationPeer that is a wrapper around Ribbon.Title object
    /// </summary>
    public class RibbonTitleAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonTitleAutomationPeer(FrameworkElement owner)
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
            ContentPresenter cp = Owner as ContentPresenter;
            if (cp != null && cp.Content != null)
            {
                return cp.Content.ToString();
            }
            return base.GetNameCore();
        }
    }
}
