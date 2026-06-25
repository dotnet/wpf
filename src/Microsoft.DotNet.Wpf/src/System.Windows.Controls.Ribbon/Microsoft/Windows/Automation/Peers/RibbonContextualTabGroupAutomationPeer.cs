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
    /// AutomationPeer for a RibbonContextualTabGroup
    /// </summary>
    public class RibbonContextualTabGroupAutomationPeer : FrameworkElementAutomationPeer
    {
        public RibbonContextualTabGroupAutomationPeer(RibbonContextualTabGroup owner)
            : base(owner)
        {
        }

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (String.IsNullOrEmpty(name))
            {
                RibbonContextualTabGroup tabGroup = Owner as RibbonContextualTabGroup;
                if (tabGroup != null && tabGroup.Header != null)
                {
                    UIElement headerElement = tabGroup.Header as UIElement;
                    if (headerElement != null)
                    {
                        AutomationPeer peer = CreatePeerForElement(headerElement);
                        if (peer != null)
                        {
                            name = peer.GetName();
                        }
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        name = tabGroup.Header.ToString();
                    }
                }
            }

            return name;
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
