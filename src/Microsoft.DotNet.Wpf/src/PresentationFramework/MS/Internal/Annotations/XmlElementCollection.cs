// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Subclass of ObservableCollection<T> which also registers for 
//              change notifications from the XmlElements it contains.  It fires
//              CollectionChanged event with action==Reset for any item that
//              changed.  This is sufficient to let owner objects know an item
//              has changed.  
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using MS.Internal;

namespace MS.Internal.Annotations
{
    /// <summary>
    /// </summary>
    internal sealed class XmlElementCollection : ObservableCollection<XmlElement>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Initializes a new instance of XmlElementCollection that is empty and has default initial capacity.
        /// </summary>
        public XmlElementCollection() : base()
        {
            _xmlDocsRefCounts = new Dictionary<XmlDocument, int>();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// called by base class Collection&lt;T&gt; when the list is being cleared;
        /// unregisters from all items
        /// </summary>
        protected override void ClearItems()
        {
            foreach (XmlElement item in this)
            {
                UnregisterForElement(item);
            }

            base.ClearItems();
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is removed from list;
        /// unregisters on item being removed
        /// </summary>
        protected override void RemoveItem(int index)
        {
            XmlElement removedItem = this[index];

            UnregisterForElement(removedItem);

            base.RemoveItem(index);
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is added to list;
        /// registers on new item
        /// </summary>
        protected override void InsertItem(int index, XmlElement item)
        {
            if (item != null && this.Contains(item))
            {
                throw new ArgumentException(SR.Get(SRID.XmlNodeAlreadyOwned, "change", "change"), "item");
            }

            base.InsertItem(index, item);

            RegisterForElement(item);
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is added to list;
        /// unregisters on previous item and registers for new item
        /// </summary>
        protected override void SetItem(int index, XmlElement item)
        {
            if (item != null && this.Contains(item))
            {
                throw new ArgumentException(SR.Get(SRID.XmlNodeAlreadyOwned, "change", "change"), "item");
            }

            XmlElement originalItem = this[index];

            UnregisterForElement(originalItem);

            Items[index] = item;    // directly set Collection<T> inner Items collection
            OnCollectionReset();

            RegisterForElement(item);
        }

        #endregion Protected Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Unregister for change notifications for this element.
        ///     We decrease the reference count and unregister if the count
        ///     has reached zero.  
        /// </summary>
        /// <param name="element">the element to unregister for</param>
        private void UnregisterForElement(XmlElement element)
        {
            // Nulls may exist in the collection in which case we don't need to unregister
            if (element == null)
                return;

            Invariant.Assert(_xmlDocsRefCounts.ContainsKey(element.OwnerDocument), "Not registered on XmlElement");

            // Decrease the reference count
            _xmlDocsRefCounts[element.OwnerDocument]--;

            // If the reference count is at zero, we can unregister for notifications
            // from the document and clear out its entry in the hashtable.
            if (_xmlDocsRefCounts[element.OwnerDocument] == 0)
            {
                element.OwnerDocument.NodeChanged -= OnNodeChanged;
                element.OwnerDocument.NodeInserted -= OnNodeChanged;
                element.OwnerDocument.NodeRemoved -= OnNodeChanged;
                _xmlDocsRefCounts.Remove(element.OwnerDocument);
            }
        }

        /// <summary>
        ///     Register for change notifications for this element.  In
        ///     reality we regiser on the OwnerDocument, so we keep a count
        ///     of all the elements from a particular docuemnt we are listening
        ///     for.  If that ref count gets to zero we unregister from the
        ///     document.
        /// </summary>
        /// <param name="element">the element to register for</param>
        private void RegisterForElement(XmlElement element)
        {
            // Nulls may exist in the collection in which case we don't need to register
            if (element == null)
                return;

            if (!_xmlDocsRefCounts.ContainsKey(element.OwnerDocument))
            {
                // If we aren't register on this document yet, register
                // and initialize the reference count to 1.
                _xmlDocsRefCounts[element.OwnerDocument] = 1;
                XmlNodeChangedEventHandler handler = new XmlNodeChangedEventHandler(OnNodeChanged);
                element.OwnerDocument.NodeChanged += handler;
                element.OwnerDocument.NodeInserted += handler;
                element.OwnerDocument.NodeRemoved += handler;
            }
            else
            {
                // Increase the reference count
                _xmlDocsRefCounts[element.OwnerDocument]++;
            }
        }

        /// <summary>
        ///     We register for node changes on the documents that own the contents
        ///     of this Resource.  Its the only way to know if the contents have
        ///     changed.
        /// </summary>
        /// <param name="sender">document whose node has changed</param>
        /// <param name="args">args describing the kind of change and specifying the node that changed</param>
        private void OnNodeChanged(object sender, XmlNodeChangedEventArgs args)
        {
            XmlAttribute attr = null;
            XmlElement element = null;

            // We should only be getting notifications from documents we have registered on
            Invariant.Assert(_xmlDocsRefCounts.ContainsKey(sender as XmlDocument), "Not expecting a notification from this sender");

            // The node that changed may not be a content but could be a part of a content
            // (such as an attribute node).  Therefore we must walk up from the node until
            // we either a) get to the root or b) find a content we contain.  In the case of 
            // (a) we do nothing.  In the case of (b) we must fire a change notification
            // for this Resource.
            XmlNode current = args.Node;
            while (current != null)
            {
                element = current as XmlElement;
                if (element != null && this.Contains(element))
                {
                    OnCollectionReset();
                    break;
                }

                // Get the parent of the current node
                attr = current as XmlAttribute;
                if (attr != null)
                {
                    // ParentNode isn't implemented for XmlAttributes, we must
                    // use its OwnerElement to continue our walk up the node tree.
                    current = attr.OwnerElement;
                }
                else
                {
                    current = current.ParentNode;
                }
            }
        }

        private void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        Dictionary<XmlDocument, int> _xmlDocsRefCounts;

        #endregion Private Fields
    }
}
