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

    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Controls.Primitives;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
    using System.Windows.Automation.Peers;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates InRibbonGallery control.
    /// </summary>
    class InRibbonGalleryAutomationPeer : RibbonMenuButtonAutomationPeer
    {
        #region Constructors

        /// <summary>
        ///   Initialize Automation Peer for InRibbonGallery
        /// </summary>
        public InRibbonGalleryAutomationPeer(InRibbonGallery owner)
            : base(owner)
        {
        }

        #endregion

        #region Protected Methods

        // In In RibbonMode only e first gallery if exist is accessible.
        protected override List<AutomationPeer> GetChildrenCore()
        {
            InRibbonGallery irg = (InRibbonGallery)Owner;
            if (irg.IsInInRibbonMode)
            {
                List<AutomationPeer> children = new List<AutomationPeer>();

                RibbonGallery firstGallery = irg.FirstGallery;
                if (firstGallery != null)
                {
                    AutomationPeer galleryPeer = UIElementAutomationPeer.CreatePeerForElement(firstGallery);
                    if (galleryPeer != null)
                    {
                        children.Add(galleryPeer);
                    }
                }

                RepeatButton scrollUpButton = irg.ScrollUpButton;
                if (scrollUpButton != null)
                {
                    AutomationPeer scrollUpButtonPeer = UIElementAutomationPeer.CreatePeerForElement(scrollUpButton);
                    if (scrollUpButtonPeer != null)
                    {
                        children.Add(scrollUpButtonPeer);
                    }
                }

                RepeatButton scrollDownButton = irg.ScrollDownButton;
                if (scrollDownButton != null)
                {
                    AutomationPeer scrollDownButtonPeer = UIElementAutomationPeer.CreatePeerForElement(scrollDownButton);
                    if (scrollDownButtonPeer != null)
                    {
                        children.Add(scrollDownButtonPeer);
                    }
                }

                ToggleButton partToggleButton = irg.PartToggleButton;
                if (partToggleButton != null)
                {
                    AutomationPeer partToggleButtonPeer = UIElementAutomationPeer.CreatePeerForElement(partToggleButton);
                    if (partToggleButtonPeer != null)
                    {
                        children.Add(partToggleButtonPeer);
                    }
                }

                return children;
            }

            return base.GetChildrenCore();
        }
        #endregion
    }
}
