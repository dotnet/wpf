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
using System.Windows.Automation;
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
    public class GridViewCellAutomationPeer : FrameworkElementAutomationPeer, ITableItemProvider
    {
        ///
        internal GridViewCellAutomationPeer(ContentPresenter owner, ListViewAutomationPeer parent)
            : base(owner)
        {
            Invariant.Assert(parent != null);
            _listviewAP = parent;
        }

        ///
        internal GridViewCellAutomationPeer(TextBlock owner, ListViewAutomationPeer parent)
            : base(owner)
        {
            Invariant.Assert(parent != null);
            _listviewAP = parent;
        }

        ///
        protected override string GetClassNameCore()
        {
            return Owner.GetType().Name;
        }

        ///
        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            if (Owner is TextBlock)
            {
                return AutomationControlType.Text;
            }
            else
            {
                return AutomationControlType.Custom;
            }
        }

        /// 
        override public object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.GridItem || patternInterface == PatternInterface.TableItem)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        ///
        protected override bool IsControlElementCore()
        {
            bool includeInvisibleItems = IncludeInvisibleElementsInControlView;
            if (Owner is TextBlock)
            {
                // We only want this peer to show up in the Control view if it is visible
                // For compat we allow falling back to legacy behavior (returning true always)
                // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
                return includeInvisibleItems || Owner.IsVisible;
            }
            else
            {
                List<AutomationPeer> children = GetChildrenAutomationPeer(Owner, includeInvisibleItems);
                return children != null && children.Count >= 1;
            }
        }

        internal int Column
        {
            get { return _column; }
            set { _column = value; }
        }

        internal int Row
        {
            get { return _row; }
            set { _row = value; }
        }

        #region ITableItem

        IRawElementProviderSimple[] ITableItemProvider.GetRowHeaderItems()
        {
            //If there are no row headers, return an empty array 
            return Array.Empty<IRawElementProviderSimple>();
        }

        IRawElementProviderSimple[] ITableItemProvider.GetColumnHeaderItems()
        {
            ListView listview = _listviewAP.Owner as ListView;
            if (listview != null && listview.View is GridView)
            {
                GridView gridview = listview.View as GridView;
                if (gridview.HeaderRowPresenter != null && gridview.HeaderRowPresenter.ActualColumnHeaders.Count > Column)
                {
                    GridViewColumnHeader header = gridview.HeaderRowPresenter.ActualColumnHeaders[Column];
                    AutomationPeer peer = UIElementAutomationPeer.FromElement(header);
                    if (peer != null)
                    {
                        return new IRawElementProviderSimple[] { ProviderFromPeer(peer) };
                    }
                }
            }
            return Array.Empty<IRawElementProviderSimple>();
        }

        #endregion

        #region IGridItem

        int IGridItemProvider.Row { get { return Row; } }
        int IGridItemProvider.Column { get { return Column; } }
        int IGridItemProvider.RowSpan { get { return 1; } }
        int IGridItemProvider.ColumnSpan { get { return 1; } }
        IRawElementProviderSimple IGridItemProvider.ContainingGrid { get { return ProviderFromPeer(_listviewAP); } }

        #endregion


        #region Private Methods

        /// <summary>
        /// Get the children of the parent which has automation peer
        /// </summary>
        private List<AutomationPeer> GetChildrenAutomationPeer(Visual parent, bool includeInvisibleItems)
        {
            Invariant.Assert(parent != null);

            List<AutomationPeer> children = null;

            iterate(parent, includeInvisibleItems,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        if (children == null)
                            children = new List<AutomationPeer>();

                        children.Add(peer);
                        return (false);
                    });

            return children;
        }

        private delegate bool IteratorCallback(AutomationPeer peer);

        //
        private static bool iterate(Visual parent, bool includeInvisibleItems, IteratorCallback callback)
        {
            bool done = false;

            AutomationPeer peer = null;

            int count = parent.InternalVisualChildrenCount;
            for (int i = 0; i < count && !done; i++)
            {
                Visual child = parent.InternalGetVisualChild(i);
                if (child != null
                    && child.CheckFlagsAnd(VisualFlags.IsUIElement)
                    && (includeInvisibleItems || ((UIElement)child).IsVisible)
                    && (peer = CreatePeerForElement((UIElement)child)) != null)
                {
                    done = callback(peer);
                }
                else
                {
                    done = iterate(child, includeInvisibleItems, callback);
                }
            }

            return (done);
        }

        #endregion

        #region Private Fields

        private ListViewAutomationPeer _listviewAP;
        private int _column;
        private int _row;

        #endregion
    }
}
