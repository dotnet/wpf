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

    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
    using System.Windows;
    using System.Windows.Controls;
#endif

    #endregion

    public class RibbonGalleryCategoryDataAutomationPeer : ItemAutomationPeer,IScrollItemProvider
    {
        #region constructor

        ///
        public RibbonGalleryCategoryDataAutomationPeer(object owner, ItemsControlAutomationPeer itemsControlAutomationPeer)
            : base(owner, itemsControlAutomationPeer)
        {
        }

        #endregion constructor

        #region Automation override

        ///
        override protected string GetClassNameCore()
        {
            return "RibbonGalleryCategory";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ScrollItem)
            {
                return this;
            }

            RibbonGalleryCategoryAutomationPeer wrapperPeer = GetWrapperPeer() as RibbonGalleryCategoryAutomationPeer;
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetPattern(patternInterface);
            }

            return null;
        }

        #endregion Automation override

        #region IScrollItemProvider Members

        /// <summary>
        /// call wrapper.BringIntoView
        /// </summary>
        void IScrollItemProvider.ScrollIntoView()
        {
            RibbonGalleryCategory category = GetWrapper() as RibbonGalleryCategory;
            if (category != null)
            {
                category.BringIntoView();
            }
        }

        #endregion

#if !RIBBON_IN_FRAMEWORK
        #region Private methods

        private UIElement GetWrapper()
        {
            UIElement wrapper = null;
            ItemsControlAutomationPeer itemsControlAutomationPeer = ItemsControlAutomationPeer;
            if (itemsControlAutomationPeer != null)
            {
                ItemsControl owner = (ItemsControl)(itemsControlAutomationPeer.Owner);
                if (owner != null)
                {
                    wrapper = owner.ItemContainerGenerator.ContainerFromItem(Item) as UIElement;
                }
            }
            return wrapper;
        }

        private AutomationPeer GetWrapperPeer()
        {
            AutomationPeer wrapperPeer = null;
            UIElement wrapper = GetWrapper();
            if (wrapper != null)
            {
                wrapperPeer = UIElementAutomationPeer.CreatePeerForElement(wrapper);
                if (wrapperPeer == null)
                {
                    if (wrapper is FrameworkElement)
                        wrapperPeer = new FrameworkElementAutomationPeer((FrameworkElement)wrapper);
                    else
                        wrapperPeer = new UIElementAutomationPeer(wrapper);
                }
            }

            return wrapperPeer;
        }

        #endregion
#endif

    }
}
