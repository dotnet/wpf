// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines XmlBindingWorker object, workhorse for XML bindings
//

using System;
using System.Xml;
using System.Xml.XPath;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;      // IGeneratorHost
using System.Windows.Markup;
using MS.Internal.Data;

namespace MS.Internal.Data
{
    internal class XmlBindingWorker : BindingWorker, IWeakEventListener
    {
        private enum XPathType : byte { Default, SimpleName, SimpleAttribute }

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal XmlBindingWorker(ClrBindingWorker worker, bool collectionMode) : base(worker.ParentBindingExpression)
        {
            _hostWorker = worker;
            _xpath = ParentBinding.XPath;
            Debug.Assert(_xpath != null);

            // when collectionMode is true, we update the XmlDataCollection for XmlNodeChanges,
            // otherwise, any XmlNodeChange counts as a disastrous change that requires reset.
            _collectionMode = collectionMode;

            _xpathType = GetXPathType(_xpath);

            // PERF: it is possible to add one more optimization "mode" for the case when
            // we know the host wants to use the CurrentItem (i.e. DrillIn == Always).
            // We could be using SelectSingleNode() instead of SelectNodes(),
            // and then only watch for changes to one node instead of comparing collections.
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        internal override void AttachDataItem()
        {
            // If there is an XPath, we get a context node for running queries by
            // creating a view from DataItem and using its CurrentItem.
            // If DataItem isn't a valid collection, it's probably an XmlNode,
            // in which case we will try using DataItem directly as the ContextNode

            if (XPath.Length > 0)
            {
                CollectionView = DataItem as CollectionView;

                if (CollectionView == null && DataItem is ICollection)
                {
                    CollectionView = CollectionViewSource.GetDefaultCollectionView(DataItem, TargetElement);
                }
            }

            if (CollectionView != null)
            {
                CurrentChangedEventManager.AddHandler(CollectionView, ParentBindingExpression.OnCurrentChanged);

                if (IsReflective)
                {
                    CurrentChangingEventManager.AddHandler(CollectionView, ParentBindingExpression.OnCurrentChanging);
                }
            }

            // Set ContextNode and hook events
            UpdateContextNode(true);
        }

        internal override void DetachDataItem()
        {
            //UnHook Collection Manager Currency notifications
            if (CollectionView != null)
            {
                CurrentChangedEventManager.RemoveHandler(CollectionView, ParentBindingExpression.OnCurrentChanged);

                if (IsReflective)
                {
                    CurrentChangingEventManager.RemoveHandler(CollectionView, ParentBindingExpression.OnCurrentChanging);
                }
            }

            // Set ContextNode (this unhooks events first)
            UpdateContextNode(false);

            CollectionView = null;
        }

        internal override void OnCurrentChanged(ICollectionView collectionView, EventArgs args)
        {
            // There are two possible CurrentChanged events that comes through this event handler.
            // 1. CurrentChanged from DataItem as CollectionView
            // 2. CurrentChanged from QueriedCollection

            // only handle changed event from DataItem as CollectionView
            if (collectionView == CollectionView)
            {
                using (ParentBindingExpression.ChangingValue())
                {
                    // This will unhook and hook notifications
                    UpdateContextNode(true);

                    // tell host worker to use a new item
                    _hostWorker.UseNewXmlItem(this.RawValue());
                }
            }
        }

        internal override object RawValue()
        {
            if (XPath.Length == 0)
            {
                return DataItem;
            }

            if (ContextNode == null)    // possibly because currentItem moved off collection
            {
                QueriedCollection = null;
                return null;
            }

            XmlNodeList nodes = SelectNodes();

            if (nodes == null)
            {
                QueriedCollection = null;
                return DependencyProperty.UnsetValue;
            }

            return BuildQueriedCollection(nodes);
        }

        internal void ReportBadXPath(TraceEventType traceType)
        {
            if (TraceData.IsEnabled)
            {
                TraceData.Trace(traceType,
                                    TraceData.BadXPath(
                                        XPath,
                                        IdentifyNode(ContextNode)));
            }
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        private XmlDataCollection QueriedCollection
        {
            get { return _queriedCollection; }
            set { _queriedCollection = value; }
        }

        private ICollectionView CollectionView
        {
            get { return _collectionView; }
            set { _collectionView = value; }
        }

        private XmlNode ContextNode
        {
            get { return _contextNode; }
            set
            {
                if (_contextNode != value && TraceData.IsExtendedTraceEnabled(ParentBindingExpression, TraceDataLevel.ReplaceItem))
                {
                    TraceData.Trace(TraceEventType.Warning,
                                        TraceData.XmlContextNode(
                                            TraceData.Identify(ParentBindingExpression),
                                            IdentifyNode(value)));
                }

                _contextNode = value;
            }
        }

        private string XPath
        {
            get { return _xpath; }
        }

        private XmlNamespaceManager NamespaceManager
        {
            get
            {
                DependencyObject target = TargetElement;
                if (target == null)
                    return null;

                XmlNamespaceManager nsMgr = Binding.GetXmlNamespaceManager(target);

                if (nsMgr == null)
                {
                    if (XmlDataProvider != null)
                    {
                        nsMgr = XmlDataProvider.XmlNamespaceManager;
                    }
                }

                return nsMgr;
            }
        }

        // lazy computation of the XmlDataProvider that begat our data.
        // This is used primarily to get the right XmlNamespaceManager for
        // subqueries, sorting, etc.
        private XmlDataProvider XmlDataProvider
        {
            get
            {
                if (_xmlDataProvider == null)
                {
                    XmlDataCollection xdc;
                    ItemsControl ic;

                    // if the binding knows its data source and it's the right kind, use it
                    if ((_xmlDataProvider = ParentBindingExpression.DataSource as XmlDataProvider) != null)
                    {
                        // nothing more to do
                    }

                    // if the data is an XmlDataCollection, use its provider
                    else if ((xdc = DataItem as XmlDataCollection) != null)
                    {
                        _xmlDataProvider = xdc.ParentXmlDataProvider;
                    }

                    // if the data is a view over an XmlDataCollection, use its provider
                    else if (CollectionView != null &&
                            (xdc = CollectionView.SourceCollection as XmlDataCollection) != null)
                    {
                        _xmlDataProvider = xdc.ParentXmlDataProvider;
                    }

                    // if the binding is a transient one attached to an ItemsControl,
                    // use the provider for the ItemsSource.  This arises for the "Primary
                    // Text" binding for a ComboBox.
                    else if (TargetProperty == BindingExpressionBase.NoTargetProperty &&
                            (ic = TargetElement as ItemsControl) != null)
                    {
                        object itemsSource = ic.ItemsSource;
                        if ((xdc = itemsSource as XmlDataCollection) == null)
                        {
                            ICollectionView icv = itemsSource as ICollectionView;
                            xdc = ((icv != null) ? icv.SourceCollection : null) as XmlDataCollection;
                        }

                        if (xdc != null)
                        {
                            _xmlDataProvider = xdc.ParentXmlDataProvider;
                        }
                    }

                    // bindings in DataTemplates are typically bound to a single XmlNode.
                    // Find the governing XmlDataProvider.
                    else
                    {
                        _xmlDataProvider = Helper.XmlDataProviderForElement(TargetElement);
                    }
                }

                return _xmlDataProvider;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // Recalculate the node to be used as XPath query context;
        // only call this when CurrentItem changes and for Attach/Detach DataItem.
        // If worker was hooked up for notifications, this will UnhookNotifications()
        // before changing ContextNode, and then optionally HookNotifications() after.
        private void UpdateContextNode(bool hookNotifications)
        {
            UnHookNotifications();

            if (DataItem == BindingExpressionBase.DisconnectedItem)
            {
                ContextNode = null;
                return;
            }

            if (CollectionView != null)
            {
                ContextNode = CollectionView.CurrentItem as XmlNode;

                if (ContextNode != CollectionView.CurrentItem && TraceData.IsEnabled)
                {
                    TraceData.Trace(TraceEventType.Error, TraceData.XmlBindingToNonXmlCollection, XPath,
                            ParentBindingExpression, DataItem);
                }
            }
            else
            {
                ContextNode = DataItem as XmlNode;

                if (ContextNode != DataItem && TraceData.IsEnabled)
                {
                    TraceData.Trace(TraceEventType.Error, TraceData.XmlBindingToNonXml, XPath,
                            ParentBindingExpression, DataItem);
                }
            }

            if (hookNotifications)
                HookNotifications();
        }

        // We hook up only one set of event listeners per document and propagate
        // events to our worker instances.  This is a perf savings because we use
        // a doubly-linked to add/remove workers, whereas delegate add/remove
        // is linear and doesn't scale well for high number of binding workers.
        private void HookNotifications()
        {
            //Hook Xml Node Change Notifications for one way
            //and two way binding
            if (IsDynamic)
            {
                // Check the node on which we would run XPath queries.
                // We can only hook if there is a node.
                if (ContextNode != null)
                {
                    XmlDocument doc = DocumentFor(ContextNode);
                    if (doc != null)
                    {
                        XmlNodeChangedEventManager.AddHandler(doc, OnXmlNodeChanged);
                    }
                }
            }
        }

        // see comment on HookNotifications()
        private void UnHookNotifications()
        {
            //Hook Xml Node Change Notifications for one way
            //and two way binding
            if (IsDynamic)
            {
                //this worker might not be hooked, either because
                //the query is empty or because of an invalid query.
                //Only unhook if we were hooked in the first place.
                if (ContextNode != null)
                {
                    XmlDocument doc = DocumentFor(ContextNode);
                    if (doc != null)
                    {
                        XmlNodeChangedEventManager.RemoveHandler(doc, OnXmlNodeChanged);
                    }
                }
            }
        }

        private XmlDocument DocumentFor(XmlNode node)
        {
            XmlDocument doc = node.OwnerDocument;
            if (doc == null)
            {
                // this may be a document itself
                doc = node as XmlDocument;
            }

            return doc;
        }

        XmlDataCollection BuildQueriedCollection(XmlNodeList nodes)
        {
            if (TraceData.IsExtendedTraceEnabled(ParentBindingExpression, TraceDataLevel.GetValue))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.XmlNewCollection(
                                        TraceData.Identify(ParentBindingExpression),
                                        IdentifyNodeList(nodes)));
            }

            QueriedCollection = new XmlDataCollection(XmlDataProvider);
            QueriedCollection.XmlNamespaceManager = NamespaceManager;
            QueriedCollection.SynchronizeCollection(nodes);
            return QueriedCollection;
        }

        bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs args)
        {
            return false;   // this method is no longer used (but must remain, for compat)
        }

        void OnXmlNodeChanged(object sender, XmlNodeChangedEventArgs e)
        {
            if (TraceData.IsExtendedTraceEnabled(ParentBindingExpression, TraceDataLevel.Events))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.GotEvent(
                                        TraceData.Identify(ParentBindingExpression),
                                        "XmlNodeChanged",
                                        TraceData.Identify(sender)));
            }

            ProcessXmlNodeChanged(e);
        }

        void ProcessXmlNodeChanged(EventArgs args)
        {
            // By the time this worker is notified, its binding's TargetElement may already be gone.
            // We should first check TargetElement to see if this worker still matters. (Fix 1494812)
            DependencyObject target = ParentBindingExpression.TargetElement;
            if (target == null)
                return;

            if (IgnoreSourcePropertyChange)
                return;

            if (DataItem == BindingExpressionBase.DisconnectedItem)
                return;

            // There should never be a change notification when there's no ContextNode.
            // If this Assert ever hits, something is wrong with the logic or ordering of
            // UpdateContextNode(), HookNotifications() and UnHookNotifications().
            Debug.Assert(ContextNode != null);

            // ignore changes that cannot possibly affect the value of this XPath
            if (!IsChangeRelevant(args))
                return;

            if (XPath.Length == 0)
            {
                // DataItem is being used directly; no need to check queries at all.
                _hostWorker.OnXmlValueChanged();
            }
            else if (QueriedCollection == null)
            {
                // If there was no previous QueryCollection, it's probably because
                // the previous xpath query failed.  Try again now.
                _hostWorker.UseNewXmlItem(this.RawValue());
            }
            else
            {
                // We have a previous query result; run a new query for comparison:

                XmlNodeList nodes = SelectNodes();

                if (nodes == null)
                {
                    // Node change has caused the new query to fail.
                    QueriedCollection = null;
                    _hostWorker.UseNewXmlItem(DependencyProperty.UnsetValue);
                }
                else if (_collectionMode)
                {
                    if (TraceData.IsExtendedTraceEnabled(ParentBindingExpression, TraceDataLevel.GetValue))
                    {
                        TraceData.Trace(TraceEventType.Warning,
                                            TraceData.XmlSynchronizeCollection(
                                                TraceData.Identify(ParentBindingExpression),
                                                IdentifyNodeList(nodes)));
                    }

                    // Any xml change action, doesn't matter if it's an insert,
                    // remove, or change, can result in any number of changes
                    // to the content of the queried collection, so we have to
                    // update the old collection with the new results.
                    QueriedCollection.SynchronizeCollection(nodes);
                }
                // PERF: it is possible to add one more optimization "mode" here for singleMode.
                else if (QueriedCollection.CollectionHasChanged(nodes))
                {
                    // RawValue itself has changed, and we don't know
                    // if the hostWorker is consuming information on the
                    // collection itself or on its CurrentItem, so we just reset.
                    _hostWorker.UseNewXmlItem(BuildQueriedCollection(nodes));
                }
                else
                {
                    // RawValue itself hasn't changed, but its children's content may have.
                    _hostWorker.OnXmlValueChanged();
                }
            }
            GC.KeepAlive(target);   // keep target alive during process xml change (bug 1494812)
        }

        private XmlNodeList SelectNodes()
        {
            XmlNamespaceManager nsMgr = NamespaceManager;
            XmlNodeList nodes = null;
            try
            {
                if (nsMgr != null)
                {
                    nodes = ContextNode.SelectNodes(XPath, nsMgr);
                }
                else
                {
                    nodes = ContextNode.SelectNodes(XPath);
                }
            }
            catch (XPathException xe)
            {
                Status = BindingStatusInternal.PathError;
                if (TraceData.IsEnabled)
                {
                    TraceData.Trace(TraceEventType.Error, TraceData.CannotGetXmlNodeCollection,
                            (ContextNode != null) ? ContextNode.Name : null, XPath,
                            ParentBindingExpression, xe);
                }
            }

            if (TraceData.IsExtendedTraceEnabled(ParentBindingExpression, TraceDataLevel.GetValue))
            {
                TraceData.Trace(TraceEventType.Warning,
                                    TraceData.SelectNodes(
                                        TraceData.Identify(ParentBindingExpression),
                                        IdentifyNode(ContextNode),
                                        TraceData.Identify(XPath),
                                        IdentifyNodeList(nodes)));
            }

            return nodes;
        }

        private string IdentifyNode(XmlNode node)
        {
            if (node == null)
                return "<null>";

            return String.Format(TypeConverterHelper.InvariantEnglishUS, "{0} ({1})",
                                    node.GetType().Name, node.Name);
        }

        private string IdentifyNodeList(XmlNodeList nodeList)
        {
            if (nodeList == null)
                return "<null>";

            return String.Format(TypeConverterHelper.InvariantEnglishUS, "{0} (hash={1} Count={2})",
                                    nodeList.GetType().Name, AvTrace.GetHashCodeHelper(nodeList), nodeList.Count);
        }

        // 90% of the XPaths used in practice are very simple - consisting of a
        // single child or attribute.  For these we can streamline the process of
        // handling change events, since we can ignore many events that cannot
        // possibly affect the value of the XPath.
        private static XPathType GetXPathType(string xpath)
        {
            int n = xpath.Length;
            if (n == 0)
                return XPathType.SimpleName;

            // attributes start with '@', followed by a Name
            bool isAttribute = (xpath[0] == '@');
            int index = isAttribute ? 1 : 0;
            if (index >= n)
                return XPathType.Default;

            // [XML spec]  Name ::= (Letter | '_' | ':') (NameChar)*
            char c = xpath[index];
            if (!(Char.IsLetter(c) || c == '_' || c == ':'))
                return XPathType.Default;

            // [XML spec]  NameChar ::=  Letter | Digit | '.' | '-' | '_' | ':' | CombiningChar | Extender
            // We ignore the last two possibilities to keep the code simple.  They
            // don't arise often in practice.
            for (++index; index < n; ++index)
            {
                c = xpath[index];
                if (!(Char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' || c == ':'))
                    return XPathType.Default;
            }

            return isAttribute ? XPathType.SimpleAttribute : XPathType.SimpleName;
        }

        // determine if a change can possibly affect the value of the XPath
        private bool IsChangeRelevant(EventArgs rawArgs)
        {
            // if the XPath isn't "simple", any change in the XML tree might
            // affect its value
            if (_xpathType == XPathType.Default)
                return true;

            XmlNodeChangedEventArgs args = (XmlNodeChangedEventArgs)rawArgs;
            XmlNode parent = null;
            XmlNode valueNode = null;

            switch (args.Action)
            {
                case XmlNodeChangedAction.Insert:
                    parent = args.NewParent;
                    break;

                case XmlNodeChangedAction.Remove:
                    parent = args.OldParent;
                    break;

                case XmlNodeChangedAction.Change:
                    valueNode = args.Node;
                    break;
            }

            if (_collectionMode)
            {
                // only insertions/deletions to the context node are relevant
                return (parent == ContextNode);
            }
            else
            {
                // insertions/deletions to the context node are relevant -
                // the inserted/deleted node might match the XPath
                if (parent == ContextNode)
                    return true;

                // also relevant are changes that affect the value of the result
                // node.  This includes value changes directly on the result node,
                // as well as any change to the descendants of the result node.
                XmlNode resultNode = _hostWorker.GetResultNode() as XmlNode;
                if (resultNode == null)
                    return false;

                if (valueNode != null)
                    parent = valueNode;

                while (parent != null)
                {
                    if (parent == resultNode)
                        return true;

                    parent = parent.ParentNode;
                }

                return false;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private bool _collectionMode;
        private XPathType _xpathType;
        private XmlNode _contextNode;
        private XmlDataCollection _queriedCollection; // new DataCollection
        private ICollectionView _collectionView;
        private XmlDataProvider _xmlDataProvider;
        private ClrBindingWorker _hostWorker;
        private string _xpath;
    }
}

