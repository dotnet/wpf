// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using MS.Internal;
using MS.Internal.Automation;
using MS.Win32;

namespace System.Windows.Automation.Peers
{
    /// <summary>
    /// GridView automation peer
    /// </summary>
    /// <remarks>
    /// Basically, the idea is to add a virtual method called CreateAutomationPeer on ViewBase
    /// Any view can override this method to create its own automation peer.
    /// ListView will use this method to get an automation peer for a given view and default to
    /// the properties/methods/patterns implemented by the view before going to default fall-backs on it
    /// These view automation peer must implement IViewAutomationPeer interface
    /// </remarks>
    public class GridViewAutomationPeer : IViewAutomationPeer, ITableProvider
    {
        ///
        public GridViewAutomationPeer(GridView owner, ListView listview)
            : base()
        {
            Invariant.Assert(owner != null);
            Invariant.Assert(listview != null);
            _owner = owner;
            _listview = listview;

            //Remember the items/columns count when GVAP is created, this is used for firing RowCount/ColumnCount changed event
            _oldItemsCount = _listview.Items.Count;
            _oldColumnsCount = _owner.Columns.Count;

            ((INotifyCollectionChanged)_owner.Columns).CollectionChanged += new NotifyCollectionChangedEventHandler(OnColumnCollectionChanged);
        }

        ///
        AutomationControlType IViewAutomationPeer.GetAutomationControlType()
        {
            return AutomationControlType.DataGrid;
        }

        ///
        object IViewAutomationPeer.GetPattern(PatternInterface patternInterface)
        {
            object ret = null;
            switch (patternInterface)
            {
                case PatternInterface.Grid:
                case PatternInterface.Table:
                    ret = this;
                    break;
            }

            return ret;
        }

        ///
        List<AutomationPeer> IViewAutomationPeer.GetChildren(List<AutomationPeer> children)
        {
            //Add GridViewHeaderRowPresenter as the first child of ListView
            if (_owner.HeaderRowPresenter != null)
            {
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(_owner.HeaderRowPresenter);
                if (peer != null)
                {
                    //If children is null, we still need to create an empty list to insert HeaderRowPresenter
                    if (children == null)
                    {
                        children = new List<AutomationPeer>();
                    }

                    children.Insert(0, peer);
                }
            }

            return children;
        }

        ItemAutomationPeer IViewAutomationPeer.CreateItemAutomationPeer(object item)
        {
            ListViewAutomationPeer lvAP = UIElementAutomationPeer.FromElement(_listview) as ListViewAutomationPeer;
            return new GridViewItemAutomationPeer(item, lvAP);
        }

        ///
        void IViewAutomationPeer.ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            ListViewAutomationPeer peer = UIElementAutomationPeer.FromElement(_listview) as ListViewAutomationPeer;
            if (peer != null)
            {
                if (_oldItemsCount != _listview.Items.Count)
                {
                    peer.RaisePropertyChangedEvent(GridPatternIdentifiers.RowCountProperty, _oldItemsCount, _listview.Items.Count);
                }
                _oldItemsCount = _listview.Items.Count;
            }
        }

        //Called when the view is detached from the listview
        // Note: see bug 1555137 for details.
        // Never inline, as we don't want to unnecessarily link the
        // automation DLL via the ITableProvider, IGridProvider interface type initialization.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        void IViewAutomationPeer.ViewDetached()
        {
            ((INotifyCollectionChanged)_owner.Columns).CollectionChanged -= new NotifyCollectionChangedEventHandler(OnColumnCollectionChanged);
        }

        #region ITableProvider

        /// <summary>
        /// Indicates if the data is best presented by row or column
        /// </summary>
        RowOrColumnMajor ITableProvider.RowOrColumnMajor
        {
            get { return RowOrColumnMajor.RowMajor; }
        }

        /// <summary>
        /// Collection of column headers
        /// </summary>
        IRawElementProviderSimple[] ITableProvider.GetColumnHeaders()
        {
            if (_owner.HeaderRowPresenter != null)
            {
                List<IRawElementProviderSimple> array = new List<IRawElementProviderSimple>(_owner.HeaderRowPresenter.ActualColumnHeaders.Count);
                ListViewAutomationPeer lvpeer = UIElementAutomationPeer.FromElement(_listview) as ListViewAutomationPeer;

                if(lvpeer != null)
                {
                    foreach (UIElement e in _owner.HeaderRowPresenter.ActualColumnHeaders)
                    {
                        AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(e);
                        if (peer != null)
                        {
                            array.Add(ElementProxy.StaticWrap(peer, lvpeer));
                        }
                    }
                }
                return array.ToArray();
            }

            return new IRawElementProviderSimple[0] ;
        }

        /// <summary>
        /// Collection of row headers
        /// </summary>
        IRawElementProviderSimple[] ITableProvider.GetRowHeaders()
        {
            //If there are no row headers, return an empty array
            return new IRawElementProviderSimple[0];
        }

        #endregion

        #region IGridProvider

        /// <summary>
        /// number of columns in the grid
        /// </summary>
        int IGridProvider.ColumnCount
        {
            get
            {
                if (_owner.HeaderRowPresenter != null)
                {
                    return _owner.HeaderRowPresenter.ActualColumnHeaders.Count;
                }
                return _owner.Columns.Count;
            }
        }

        /// <summary>
        /// number of rows in the grid
        /// </summary>
        int IGridProvider.RowCount
        {
            get { return _listview.Items.Count; }
        }

        /// <summary>
        /// Obtain the IRawElementProviderSimple at an absolute position
        /// </summary>
        IRawElementProviderSimple IGridProvider.GetItem(int row, int column)
        {
            if (row < 0 || row >= ((IGridProvider)this).RowCount)
            {
                throw new ArgumentOutOfRangeException("row");
            }
            if (column < 0 || column >= ((IGridProvider)this).ColumnCount)
            {
                throw new ArgumentOutOfRangeException("column");
            }

            ListViewItem lvi = _listview.ItemContainerGenerator.ContainerFromIndex(row) as ListViewItem;
            //If item is virtualized, try to de-virtualize it
            if (lvi == null)
            {
                VirtualizingPanel itemsHost = _listview.ItemsHost as VirtualizingPanel;
                if (itemsHost != null)
                {
                    itemsHost.BringIndexIntoView(row);
                }

                lvi = _listview.ItemContainerGenerator.ContainerFromIndex(row) as ListViewItem;

                if (lvi != null)
                {
                    //Must call Invoke here to force run the render process
                    _listview.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Loaded,
                        (System.Windows.Threading.DispatcherOperationCallback)delegate(object arg)
                        {
                            return null;
                        },
                        null);
                }
            }

            //lvi is null, it is virtualized, so we can't return its cell
            if (lvi != null)
            {
                AutomationPeer lvpeer = UIElementAutomationPeer.FromElement(_listview);
                if(lvpeer != null)
                {
                    AutomationPeer peer = UIElementAutomationPeer.FromElement(lvi);
                    if (peer != null)
                    {
                        // use the GridViewItemAutomationPeer, if available
                        AutomationPeer eventSource = peer.EventsSource;
                        if (eventSource != null)
                        {
                            peer = eventSource;
                        }

                        List<AutomationPeer> columns = peer.GetChildren();
                        if (columns.Count > column)
                        {
                            return ElementProxy.StaticWrap(columns[column], lvpeer);
                        }
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Fire ColumnCount changed event for GridPattern
        /// </summary>
        private void OnColumnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_oldColumnsCount != _owner.Columns.Count)
            {
                ListViewAutomationPeer peer = UIElementAutomationPeer.FromElement(_listview) as ListViewAutomationPeer;
                Invariant.Assert(peer != null);
                if (peer != null)
                {
                    peer.RaisePropertyChangedEvent(GridPatternIdentifiers.ColumnCountProperty, _oldColumnsCount, _owner.Columns.Count);
                }
            }

            _oldColumnsCount = _owner.Columns.Count;

            AutomationPeer lvPeer = UIElementAutomationPeer.FromElement(_listview);
            if (lvPeer != null)
            {
                List<AutomationPeer> list = lvPeer.GetChildren();
                if (list != null)
                {
                    foreach (AutomationPeer peer in list)
                    {
                        peer.InvalidatePeer();
                    }
                }
            }
        }

        #endregion

        #region Helper

        internal static Visual FindVisualByType(Visual parent, Type type)
        {
            if (parent != null)
            {
                int count = parent.InternalVisualChildrenCount;
                for (int i = 0; i < count; i++)
                {
                    Visual visual = parent.InternalGetVisualChild(i);
                    if (!type.IsInstanceOfType(visual))
                    {
                        visual = FindVisualByType(visual, type);
                    }

                    if (visual != null)
                    {
                        return visual;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Private Fields

        private GridView _owner;

        private ListView _listview;
        //Store the old items/columns count, this is used for firing RowCount changed event in IGridProvider
        private int _oldItemsCount = 0;
        private int _oldColumnsCount = 0;

        #endregion
    }
}


