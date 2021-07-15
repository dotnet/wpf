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
    public abstract class SelectorItemAutomationPeer : ItemAutomationPeer, ISelectionItemProvider
    {
        ///
        protected SelectorItemAutomationPeer(object owner, SelectorAutomationPeer selectorAutomationPeer)
            : base(owner, selectorAutomationPeer)
        {
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if(patternInterface == PatternInterface.SelectionItem)
            {
                return this;
            }
            return base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Sets the current element as the selection
        /// This clears the selection from other elements in the container
        /// </summary>
        void ISelectionItemProvider.Select()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);
            if (parentSelector == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            parentSelector.SelectionChange.SelectJustThisItem(parentSelector.NewItemInfo(Item), true /* assumeInItemsCollection */);
        }


        /// <summary>
        /// Adds current element to selection
        /// </summary>
        void ISelectionItemProvider.AddToSelection()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);
            if ((parentSelector == null) || (!parentSelector.CanSelectMultiple && parentSelector.SelectedItem != null && parentSelector.SelectedItem != Item))
            {
                // Parent must exist and be multi-select
                // in single-select mode the selected item should be null or Owner
                throw new InvalidOperationException(SR.Get(SRID.UIA_OperationCannotBePerformed));
            }

            parentSelector.SelectionChange.Begin();
            parentSelector.SelectionChange.Select(parentSelector.NewItemInfo(Item), true);
            parentSelector.SelectionChange.End();
        }


        /// <summary>
        /// Removes current element from selection
        /// </summary>
        void ISelectionItemProvider.RemoveFromSelection()
        {
            if(!IsEnabled())
                throw new ElementNotEnabledException();

            Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);

            parentSelector.SelectionChange.Begin();
            parentSelector.SelectionChange.Unselect(parentSelector.NewItemInfo(Item));
            parentSelector.SelectionChange.End();
        }


        /// <summary>
        /// Check whether an element is selected
        /// </summary>
        /// <value>returns true if the element is selected</value>
        bool ISelectionItemProvider.IsSelected
        {
            get
            {
                Selector parentSelector = (Selector)(ItemsControlAutomationPeer.Owner);
                return parentSelector._selectedItems.Contains(parentSelector.NewItemInfo(Item));
            }
        }


        /// <summary>
        /// The logical element that supports the SelectionPattern for this Item
        /// </summary>
        /// <value>returns an IRawElementProviderSimple</value>
        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                return ProviderFromPeer(ItemsControlAutomationPeer);
            }
        }

        // BUG 1555137: Never inline, as we don't want to unnecessarily link the automation DLL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseAutomationIsSelectedChanged(bool isSelected)
        {
            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                !isSelected,
                isSelected);
        }
    }
}



