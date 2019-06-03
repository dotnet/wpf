// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    ///
    public class MenuItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider, IToggleProvider
    {
        ///
        public MenuItemAutomationPeer(MenuItem owner): base(owner)
        {
        }

        ///
        override protected string GetClassNameCore()
        {
            return "MenuItem";
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuItem;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            object result = null;
            MenuItem owner = (MenuItem)Owner;

            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                MenuItemRole role = owner.Role;
                if (    (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
                    &&  owner.HasItems)
                {
                    result = this;
                }
            }
            else if (patternInterface == PatternInterface.Toggle)
            {
                if (owner.IsCheckable)
                {
                    result = this;
                }
            }
            else if (patternInterface == PatternInterface.Invoke)
            {
                MenuItemRole role = owner.Role;
                if (    (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
                    &&  !owner.HasItems)
                {
                    result = this;
                }
            }
            else if (patternInterface == PatternInterface.SynchronizedInput)
            {
                result = base.GetPattern(patternInterface);
            }


            return result;
        }

        /// <summary>
        /// Gets the size of a set that contains this MenuItem.
        /// </summary>
        /// <remarks>
        /// If <see cref="AutomationProperties.PositionInSetProperty"/> hasn't been set
        /// this method will calculate the position of the MenuItem based on its parent ItemsControl,
        /// if the ItemsControl is grouping the position will be relative to the group containing this item.
        /// </remarks>
        /// <returns>
        /// The value of <see cref="AutomationProperties.PositionInSetProperty"/> if it has been set, or it's position relative to the parent ItemsControl or GroupItem.
        /// </returns>
        override protected int GetSizeOfSetCore()
        {
            int sizeOfSet = base.GetSizeOfSetCore();

            if (sizeOfSet == AutomationProperties.AutomationSizeOfSetDefault)
            {
                MenuItem owner = (MenuItem)Owner;
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(owner);

                sizeOfSet = ItemAutomationPeer.GetSizeOfSetFromItemsControl(parent, owner);
            }

            return sizeOfSet;
        }

        /// <summary>
        /// Gets the position of a MenuItem contained in a set.
        /// </summary>
        /// <remarks>
        /// If this value has already been set by the developer, that value will be used for this property. If it hasn't, we find the position ourselves.
        /// </remarks>
        /// <returns>
        /// The position of a MenuItem that is contained in a set.
        /// </returns>
        override protected int GetPositionInSetCore()
        {
            int positionInSet = base.GetPositionInSetCore();

            if (positionInSet == AutomationProperties.AutomationPositionInSetDefault)
            {
                MenuItem owner = (MenuItem)Owner;
                ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(owner);
                positionInSet = ItemAutomationPeer.GetPositionInSetFromItemsControl(parent, owner);
            }

            return positionInSet;
        }

        ///
        override protected string GetAccessKeyCore()
        {
            string accessKey = base.GetAccessKeyCore();
            if (!string.IsNullOrEmpty(accessKey))
            {
                MenuItem menuItem = (MenuItem)Owner;
                MenuItemRole role = menuItem.Role;
                if (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.TopLevelItem)
                {
                    accessKey = "Alt+" + accessKey;
                }
            }
            return accessKey;
        }

        // MenuItem cannot rely on the base which gets the visal children because submenu items are part of
        // other visual tree under a Popup.
        // We return the list of items containers if they are currently visible
        // In case MenuItem is not expanded we return null
        ///
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = base.GetChildrenCore();

            if (ExpandCollapseState.Expanded == ((IExpandCollapseProvider)this).ExpandCollapseState)
            {
                ItemsControl owner = (ItemsControl)Owner;
                ItemCollection items = owner.Items;

                if (items.Count > 0)
                {
                    children = new List<AutomationPeer>(items.Count);
                    for (int i = 0; i < items.Count; i++)
                    {
                        UIElement uiElement = owner.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                        if (uiElement != null)
                        {
                            AutomationPeer peer = UIElementAutomationPeer.FromElement(uiElement);
                            if (peer == null)
                                peer = UIElementAutomationPeer.CreatePeerForElement(uiElement);
                            if( peer!= null)
                                children.Add(peer);
                        }
                    }
                }
            }

            return children;
        }

        ///
        void IExpandCollapseProvider.Expand()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            MenuItem owner = (MenuItem)Owner;
            MenuItemRole role = owner.Role;

            if (    (role != MenuItemRole.TopLevelHeader && role != MenuItemRole.SubmenuHeader)
                ||  !owner.HasItems)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            owner.OpenMenu();
        }

        ///
        void IExpandCollapseProvider.Collapse()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            MenuItem owner = (MenuItem)Owner;
            MenuItemRole role = owner.Role;

            if (    (role != MenuItemRole.TopLevelHeader && role != MenuItemRole.SubmenuHeader)
                ||  !owner.HasItems)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            owner.SetCurrentValueInternal(MenuItem.IsSubmenuOpenProperty, MS.Internal.KnownBoxes.BooleanBoxes.FalseBox);
        }

        ///
        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                ExpandCollapseState result = ExpandCollapseState.Collapsed;
                MenuItem owner = (MenuItem)Owner;
                MenuItemRole role = owner.Role;

                if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem || !owner.HasItems)
                {
                    result = ExpandCollapseState.LeafNode;
                }
                else if (owner.IsSubmenuOpen)
                {
                    result = ExpandCollapseState.Expanded;
                }

                return result;
            }
        }

        ///
        void IInvokeProvider.Invoke()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            MenuItem owner = (MenuItem)Owner;

            MenuItemRole role = owner.Role;

            if (role == MenuItemRole.TopLevelItem || role == MenuItemRole.SubmenuItem)
            {
                owner.ClickItem();
            }
            else if (role == MenuItemRole.TopLevelHeader || role == MenuItemRole.SubmenuHeader)
            {
                owner.ClickHeader();
            }
        }

        ///
        void IToggleProvider.Toggle()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            MenuItem owner = (MenuItem)Owner;

            if (!owner.IsCheckable)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            owner.SetCurrentValueInternal(MenuItem.IsCheckedProperty, MS.Internal.KnownBoxes.BooleanBoxes.Box(!owner.IsChecked));
        }

        ///
        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                MenuItem owner = (MenuItem)Owner;
                return owner.IsChecked ? ToggleState.On : ToggleState.Off;
            }
        }

        ///
        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseExpandCollapseAutomationEvent(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        // Return the base without the AccessKey character
        ///
        override protected string GetNameCore()
        {
            string result = base.GetNameCore();
            if (!string.IsNullOrEmpty(result))
            {
                MenuItem menuItem = (MenuItem)Owner;
                if (menuItem.Header is string)
                {
                    return AccessText.RemoveAccessKeyMarker(result);
                }
            }

            return result;
        }
    }
}

