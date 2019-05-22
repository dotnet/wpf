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
    public abstract class SelectorAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
    {
        ///
        protected SelectorAutomationPeer(Selector owner): base(owner)
        {}

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }

        ///
        override public object GetPattern(PatternInterface patternInterface)
        {
            if(patternInterface == PatternInterface.Selection)
            {
                return this;
            }

            return base.GetPattern(patternInterface); // ItemsControlAutomationPeer support Scroll pattern
        }

        ///
        internal override bool IsPropertySupportedByControlForFindItem(int id)
        {
            return SelectorAutomationPeer.IsPropertySupportedByControlForFindItemInternal(id);
        }

        internal static new bool IsPropertySupportedByControlForFindItemInternal(int id)
        {
            if (ItemsControlAutomationPeer.IsPropertySupportedByControlForFindItemInternal(id))
                return true;
            else
            {
                if (SelectionItemPatternIdentifiers.IsSelectedProperty.Id == id)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Support for IsSelectedProperty should come from SelectorAutomationPeer only,
        /// </summary>
        internal override object GetSupportedPropertyValue(ItemAutomationPeer itemPeer, int propertyId)
        {
            return SelectorAutomationPeer.GetSupportedPropertyValueInternal(itemPeer, propertyId);
        }

        internal static new object GetSupportedPropertyValueInternal(AutomationPeer itemPeer, int propertyId)
        {
            if (SelectionItemPatternIdentifiers.IsSelectedProperty.Id == propertyId)
            {
                ISelectionItemProvider selectionItem = itemPeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider;
                if (selectionItem != null)
                    return selectionItem.IsSelected;
                else
                    return null;
            }
            return ItemsControlAutomationPeer.GetSupportedPropertyValueInternal(itemPeer, propertyId);
        }

        //-------------------------------------------------------------------
        //
        //  ISelectionProvider
        //
        //-------------------------------------------------------------------

        #region ISelectionProvider

        IRawElementProviderSimple [] ISelectionProvider.GetSelection()
        {
            Selector owner = (Selector)Owner;

            int count = owner._selectedItems.Count;
            int itemsCount = (owner as ItemsControl).Items.Count;

            if(count > 0 && itemsCount > 0)
            {
                List<IRawElementProviderSimple> selectedProviders = new List<IRawElementProviderSimple>(count);

                for(int i=0; i<count; i++)
                {
                    SelectorItemAutomationPeer peer = FindOrCreateItemAutomationPeer(owner._selectedItems[i].Item) as SelectorItemAutomationPeer;
                    if(peer != null)
                    {
                        selectedProviders.Add(ProviderFromPeer(peer));
                    }
                }
                return selectedProviders.ToArray();
            }
            return null;
        }

        bool ISelectionProvider.CanSelectMultiple
        {
            get
            {
                Selector owner = (Selector)Owner;
                return owner.CanSelectMultiple;
            }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get
            {
                return false;
            }
        }

        // Note: see bug 1555137 for details.
        // Never inline, as we don't want to unnecessarily link the
        // automation DLL via the ISelectionProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void RaiseSelectionEvents(SelectionChangedEventArgs e)
        {
            if (ItemPeers.Count == 0)
            {
                //  ItemPeers.Count == 0 if children were never fetched.
                //  in the case, when client probably is not interested in the details
                //  of selection changes. but we still want to notify client about it.
                this.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);

                return;
            }

            Selector owner = (Selector)Owner;

            // These counters are bound to selection only numAdded = number of items just added and included in the current selection,
            // numRemoved = number of items just removed from the selection, numSelected = total number of items currently selected after addition and removal.
            int numSelected = owner._selectedItems.Count;
            int numAdded = e.AddedItems.Count;
            int numRemoved = e.RemovedItems.Count;
            if (numSelected == 1 && numAdded == 1)
            {
                SelectorItemAutomationPeer peer = FindOrCreateItemAutomationPeer(owner._selectedItems[0].Item) as SelectorItemAutomationPeer;
                if(peer != null)
                {
                    peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
                }
            }
            else
            {
                // If more than InvalidateLimit element change their state then we invalidate the selection container
                // Otherwise we fire Add/Remove from selection events
                if (numAdded + numRemoved > AutomationInteropProvider.InvalidateLimit)
                {
                    this.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
                }
                else
                {
                    int i;

                    for (i = 0; i < numAdded; i++)
                    {
                        SelectorItemAutomationPeer peer = FindOrCreateItemAutomationPeer(e.AddedItems[i]) as SelectorItemAutomationPeer;

                        if (peer != null)
                        {
                            peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
                        }
                    }

                    for (i = 0; i < numRemoved; i++)
                    {
                        SelectorItemAutomationPeer peer = FindOrCreateItemAutomationPeer(e.RemovedItems[i]) as SelectorItemAutomationPeer;

                        if (peer != null)
                        {
                            peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
                        }
                    }
                }
            }
        }

        #endregion
    }
}



