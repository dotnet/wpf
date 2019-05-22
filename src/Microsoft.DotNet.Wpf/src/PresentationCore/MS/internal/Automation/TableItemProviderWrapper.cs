// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Table Item pattern provider wrapper for WCP
//
//

using System;
using System.Windows.Threading;

using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // Automation/WCP Wrapper class: Implements that UIAutomation I...Provider
    // interface, and calls through to a WCP AutomationPeer which implements the corresponding
    // I...Provider inteface. Marshalls the call from the RPC thread onto the
    // target AutomationPeer's context.
    //
    // Class has two major parts to it:
    // * Implementation of the I...Provider, which uses Dispatcher.Invoke
    //   to call a private method (lives in second half of the class) via a delegate,
    //   if necessary, packages any params into an object param. Return type of Invoke
    //   must be cast from object to appropriate type.
    // * private methods - one for each interface entry point - which get called back
    //   on the right context. These call through to the peer that's actually
    //   implenting the I...Provider version of the interface. 
    internal class TableItemProviderWrapper: MarshalByRefObject, ITableItemProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TableItemProviderWrapper( AutomationPeer peer, ITableItemProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITableItemProvider
        //
        //------------------------------------------------------
 
        #region Interface ITableItemProvider

        public int Row
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetRow ), null );
            }
        }

        public int Column
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetColumn ), null );
            }
        }

        public int RowSpan
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetRowSpan ), null );
            }
        }

        public int ColumnSpan
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetColumnSpan ), null );
            }
        }

        public IRawElementProviderSimple ContainingGrid
        {
            get
            {
                return (IRawElementProviderSimple) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetContainingGrid ), null );
            }
        }

        public IRawElementProviderSimple [] GetRowHeaderItems()
        {
            return (IRawElementProviderSimple []) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetRowHeaderItems ), null );
        }

        public IRawElementProviderSimple [] GetColumnHeaderItems()
        {
            return (IRawElementProviderSimple []) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetColumnHeaderItems ), null );
        }

        #endregion Interface ITableItemProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new TableItemProviderWrapper( peer, (ITableItemProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object GetRow( object unused )
        {
            return _iface.Row;
        }

        private object GetColumn( object unused )
        {
            return _iface.Column;
        }

        private object GetRowSpan( object unused )
        {
            return _iface.RowSpan;
        }

        private object GetColumnSpan( object unused )
        {
            return _iface.ColumnSpan;
        }

        private object GetContainingGrid( object unused )
        {
            return _iface.ContainingGrid;
        }

        private object GetRowHeaderItems( object unused )
        {
            return _iface.GetRowHeaderItems();
        }

        private object GetColumnHeaderItems( object unused )
        {
            return _iface.GetColumnHeaderItems();
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITableItemProvider _iface;

        #endregion Private Fields
    }
}
