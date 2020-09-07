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
    using System.Windows.Automation;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;
    using System.Windows.Controls;
    using Microsoft.Windows.Controls;
#if RIBBON_IN_FRAMEWORK
    using System.Windows.Controls.Ribbon;
#else
    using Microsoft.Windows.Controls.Ribbon;
#endif

    #endregion

    /// <summary>
    ///   An automation peer class which automates RibbonMenuItem control.
    /// </summary>
    public class RibbonMenuItemDataAutomationPeer : ItemAutomationPeer, IExpandCollapseProvider, IInvokeProvider, IToggleProvider, ITransformProvider
    {

        #region Constructors

        public RibbonMenuItemDataAutomationPeer(object item, ItemsControlAutomationPeer itemsControlPeer)
            : base(item, itemsControlPeer)
        {
        }

        #endregion

        #region AutomationPeer overrides

        /// <summary>
        ///   Return class name for automation clients to display
        /// </summary> 
        protected override string GetClassNameCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetClassName();
            }
            
            return "RibbonMenuItem";
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            AutomationPeer wrapperPeer = GetWrapperPeer();
            if (wrapperPeer != null)
            {
                return wrapperPeer.GetAutomationControlType();
            }

            return AutomationControlType.MenuItem;
        }

        public override object GetPattern(PatternInterface patternInterface)
        {
            object result = null;
            UIElement owner = GetWrapper();
            if (owner != null)
            {
                RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
                if (menuItemOwner == null)
                {
                    AutomationPeer wrapperPeer = GetWrapperPeer();
                    if (wrapperPeer != null)
                    {
                        result = wrapperPeer.GetPattern(patternInterface);
                    }
                }
                else
                {
                    MenuItemRole role = menuItemOwner.Role;
                    if (patternInterface == PatternInterface.ExpandCollapse)
                    {
                        if ((role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
                            && menuItemOwner.HasItems)
                        {
                            result = this;
                        }
                    }
                    else if (patternInterface == PatternInterface.Toggle)
                    {
                        if (menuItemOwner.IsCheckable)
                        {
                            result = this;
                        }
                    }
                    else if (patternInterface == PatternInterface.Invoke)
                    {
                        if ((role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
                            && !menuItemOwner.HasItems)
                        {
                            result = this;
                        }
                    }
                    else if (patternInterface == PatternInterface.Transform)
                    {
                        if (menuItemOwner.IsSubmenuOpen && (menuItemOwner.CanUserResizeHorizontally || menuItemOwner.CanUserResizeVertically))
                        {
                            result = this;
                        }
                    }
                    else
                    {
                        AutomationPeer wrapperPeer = GetWrapperPeer();
                        if (wrapperPeer != null)
                        {
                            result = wrapperPeer.GetPattern(patternInterface);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region IExpandCollapseProvider Members

        void IExpandCollapseProvider.Expand()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            UIElement owner = GetWrapper();
            if (owner == null)
            {
                throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
            }

            RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
            if (menuItemOwner != null)
            {
                MenuItemRole role = menuItemOwner.Role;

                if ((role != MenuItemRole.TopLevelHeader && role != MenuItemRole.SubmenuHeader)
                    || !menuItemOwner.HasItems)
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
                }

                menuItemOwner.IsSubmenuOpen = true;
            }
            else
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        ///
        void IExpandCollapseProvider.Collapse()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            UIElement owner = GetWrapper();
            if (owner == null)
            {
                throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
            }

            RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
            if (menuItemOwner != null)
            {
                MenuItemRole role = menuItemOwner.Role;

                if ((role != MenuItemRole.TopLevelHeader && role != MenuItemRole.SubmenuHeader)
                    || !menuItemOwner.HasItems)
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
                }

                menuItemOwner.IsSubmenuOpen = false;
            }
            else
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }
        }

        ///
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                UIElement owner = GetWrapper();
                if (owner == null)
                {
                    throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
                }

                ExpandCollapseState result = ExpandCollapseState.Collapsed;

                RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
                if (menuItemOwner != null)
                {
                    MenuItemRole role = menuItemOwner.Role;

                    if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem || !menuItemOwner.HasItems)
                    {
                        result = ExpandCollapseState.LeafNode;
                    }
                    else if (menuItemOwner.IsSubmenuOpen)
                    {
                        result = ExpandCollapseState.Expanded;
                    }
                }
                else
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
                }

                return result;
            }
        }

        #endregion

        #region IInvokeProvider Members

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            UIElement owner = GetWrapper();
            if (owner == null)
            {
                throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
            }

            RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
            if (menuItemOwner == null)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            menuItemOwner.ClickItemInternal();
        }

        #endregion

        #region IToggleProvider Members

        void IToggleProvider.Toggle()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            UIElement owner = GetWrapper();
            if (owner == null)
            {
                throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
            }

            RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
            if (menuItemOwner == null || !menuItemOwner.IsCheckable)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            menuItemOwner.IsChecked = !menuItemOwner.IsChecked;
        }

        ///
        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                UIElement owner = GetWrapper();
                if (owner == null)
                {
                    throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
                }

                RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
                if (menuItemOwner == null)
                {
                    throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
                }

                return menuItemOwner.IsChecked ? ToggleState.On : ToggleState.Off;
            }
        }

        #endregion

        #region ITransformProvider Members

        bool ITransformProvider.CanMove
        {
            get { return false; }
        }

        bool ITransformProvider.CanResize
        {
            get 
            {
                RibbonMenuItem owner = GetWrapper() as RibbonMenuItem;
                if (owner != null)
                {
                    return IsEnabled() && (owner.CanUserResizeVertically || owner.CanUserResizeHorizontally);
                }

                return false;
            }
        }

        bool ITransformProvider.CanRotate
        {
            get { return false; }
        }

        void ITransformProvider.Move(double x, double y)
        {
            throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
        }

        void ITransformProvider.Resize(double width, double height)
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();

            UIElement owner = GetWrapper();
            if (owner == null)
            {
                throw new ElementNotAvailableException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.VirtualizedElement));
            }

            RibbonMenuItem menuItemOwner = owner as RibbonMenuItem;
            if (menuItemOwner == null)
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
            }

            if (!((ITransformProvider)this).CanResize || width <= 0 || height <= 0)
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));

            if (!menuItemOwner.ResizePopupInternal(width, height))
            {
                throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.ResizeParametersNotValid));
            }
        }

        void ITransformProvider.Rotate(double degrees)
        {
            throw new InvalidOperationException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.UIA_OperationCannotBePerformed));
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
