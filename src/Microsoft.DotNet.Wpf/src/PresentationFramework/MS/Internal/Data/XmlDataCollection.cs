// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Data collection produced by an XmlDataProvider
//
// Specs:       IDataCollection.mht
//

using System.Xml;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Windows.Markup;

namespace MS.Internal.Data
{
    /// <summary>
    /// Implementation of a data collection based on ArrayList,
    /// implementing INotifyCollectionChanged to notify listeners
    /// when items get added, removed or the whole list is refreshed.
    /// </summary>
    internal class XmlDataCollection : ReadOnlyObservableCollection<XmlNode>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of XmlDataCollection that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="xmlDataProvider">Parent Xml Data Source</param>
        internal XmlDataCollection(XmlDataProvider xmlDataProvider) : base(new ObservableCollection<XmlNode>())
        {
            _xds = xmlDataProvider;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// XmlNamespaceManager property, XmlNamespaceManager used for executing XPath queries.
        /// </summary>
        internal XmlNamespaceManager XmlNamespaceManager
        {
            get
            {
                if (_nsMgr == null && _xds != null)
                    _nsMgr = _xds.XmlNamespaceManager;
                return _nsMgr;
            }
            set { _nsMgr = value; }
        }

        internal XmlDataProvider ParentXmlDataProvider
        {
            get { return _xds; }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        // return true if the counts are different or the identity of the nodes have changed
        internal bool CollectionHasChanged(XmlNodeList nodes)
        {
            int count = this.Count;
            if (count != nodes.Count)
                return true;
            for (int i = 0; i < count; ++i)
            {
                if (this[i] != nodes[i])
                    return true;
            }
            return false;
        }

        // Update the collection using new query results
        internal void SynchronizeCollection(XmlNodeList nodes)
        {
            if (nodes == null)
            {
                Items.Clear();
                return;
            }

            int i = 0, j;
            while (i < this.Count && i < nodes.Count)
            {
                if (this[i] != nodes[i])
                {
                    // starting after current node, see if the old node is still in the new list.
                    for (j = i + 1; j < nodes.Count; ++j)
                    {
                        if (this[i] == nodes[j])
                        {
                            break;
                        }
                    }
                    if (j < nodes.Count)
                    {
                        // the node from existing collection is found at [j] in the new collection;
                        // this means the node(s) [i ~ j-1] in new collection should be inserted.
                        while (i < j)
                        {
                            Items.Insert(i, nodes[i]);
                            ++i;
                        }
                        ++i; // advance to next node
                    }
                    else
                    {
                        // the node from existing collection is no longer in
                        // the new collection, delete it.
                        Items.RemoveAt(i);

                        // do not advance to the next node
                    }
                }
                else
                {
                    // nodes are the same; advance to the next node.
                    ++i;
                }
            }
            // Remove any extra nodes left over in the old collection
            while (i < this.Count)
            {
                Items.RemoveAt(i);
            }
            // Add any extra new nodes from the new collection
            while (i < nodes.Count)
            {
                Items.Insert(i, nodes[i]);
                ++i;
            }
        }

        private XmlDataProvider _xds;
        private XmlNamespaceManager _nsMgr;
    }
}



