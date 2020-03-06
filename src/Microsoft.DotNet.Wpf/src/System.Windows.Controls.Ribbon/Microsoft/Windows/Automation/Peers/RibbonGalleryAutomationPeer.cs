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
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    public class RibbonGalleryAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
    {
        #region constructor

        public RibbonGalleryAutomationPeer(RibbonGallery owner)
            : base(owner)
        { }

        #endregion constructor

        #region AutomationPeer overrides

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Selection)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        ///
        override protected string GetClassNameCore()
        {
            return "RibbonGallery";
        }

        /// <summary>
        ///   Returns help text 
        /// </summary>
        protected override string GetHelpTextCore()
        {
            string helpText = base.GetHelpTextCore();
            if (String.IsNullOrEmpty(helpText))
            {
                RibbonToolTip toolTip = ((RibbonGallery)Owner).ToolTip as RibbonToolTip;
                if (toolTip != null)
                {
                    helpText = toolTip.Description;
                }
            }

            return helpText;
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }

        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            RibbonGallery owner = (RibbonGallery)Owner;
            List<AutomationPeer> children = null;

#if IN_RIBBON_GALLERY
            // If this is an InRibbonGallery, then we do not want the filter peer or the
            // RibbonGalleryCategory peers in the tree.  Add only the RibbonGalleryItem peers.
            if (owner.ParentInRibbonGallery != null &&
                owner.ParentInRibbonGallery.IsInInRibbonMode)
            {
                foreach (AutomationPeer categoryPeer in base.GetChildrenCore())
                {
                    foreach (AutomationPeer itemPeer in categoryPeer.GetChildren())
                    {
                        if (children == null)
                        {
                            children = new List<AutomationPeer>();
                        }

                        children.Add(itemPeer);
                    }
                }

                return children;
            }
#endif

            if (!owner.IsGrouping)
            {
                children = base.GetChildrenCore();
            }
            
            if (owner.CanUserFilter)
            {
                UIElement filterHost = null;
                if (owner.FilterPaneContent != null || owner.FilterPaneContentTemplate != null)
                {
                    filterHost = owner.FilterContentPane;
                }
                else
                {
                    filterHost = owner.FilterMenuButton;
                }

                if (filterHost != null)
                {
                    if (children == null)
                    {
                        children = new List<AutomationPeer>(1);
                    }

                    children.Insert(0, RibbonHelper.CreatePeer(filterHost));
                }
            }

            return children;
        }

        #endregion AutomationPeer overrides

        #region ItemsControlAutomationPeer override

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new RibbonGalleryCategoryDataAutomationPeer(item, this);
        }

        #endregion ItemsControlAutomationPeer override

        #region ISelectionProvider Members

        /// <summary>
        /// True
        /// </summary>
        bool ISelectionProvider.CanSelectMultiple
        {
            get { return false; }
        }

        /// <summary>
        /// return SelectedContainers for SelectedItem
        /// </summary>
        /// <returns></returns>
        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            List<IRawElementProviderSimple> selectedProviders = new List<IRawElementProviderSimple>();
            Collection<RibbonGalleryItem> selectedContainers = ((RibbonGallery)Owner).SelectedContainers;
            for (int index = 0; index < selectedContainers.Count; index++)
            {
                AutomationPeer peer = UIElementAutomationPeer.FromElement(selectedContainers[index]);

                // With alization in effect RibbonGalleryItemDataAP would be exposed to client not the Peer directly associated with UI
                // and Selection must return the relevant peer(RibbonGalleryItemDataAP) stored in EventSource.
                if (peer.EventsSource != null)
                    peer = peer.EventsSource;
                
                if (peer != null)
                {
                    selectedProviders.Add(ProviderFromPeer(peer)); 
                }
            }

            if (selectedProviders.Count != 0)
            {
                return selectedProviders.ToArray();
            }

            return null;
        }

        /// <summary>
        /// False
        /// </summary>
        bool ISelectionProvider.IsSelectionRequired
        {
            get { return false; }
        }

        #endregion
    }
}
