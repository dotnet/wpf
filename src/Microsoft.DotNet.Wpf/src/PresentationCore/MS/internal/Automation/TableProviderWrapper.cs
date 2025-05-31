// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Table pattern provider wrapper for WCP
//
//

using System.Windows.Threading;
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
    internal class TableProviderWrapper: MarshalByRefObject, ITableProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TableProviderWrapper( AutomationPeer peer, ITableProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ITableProvider
        //
        //------------------------------------------------------
 
        #region Interface ITableProvider

        public IRawElementProviderSimple GetItem(int row, int column)
        {
            return ElementUtil.Invoke(_peer, static (state, rowColumn) => state.GetItem(rowColumn[0], rowColumn[1]), _iface, new int[] { row, column });
        }

        public int RowCount
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.RowCount, _iface);
        }

        public int ColumnCount
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.ColumnCount, _iface);
        }

        public IRawElementProviderSimple[] GetRowHeaders()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetRowHeaders(), _iface);
        }

        public IRawElementProviderSimple[] GetColumnHeaders()
        {
            return ElementUtil.Invoke(_peer, static (state) => state.GetColumnHeaders(), _iface);
        }

        public RowOrColumnMajor RowOrColumnMajor
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.RowOrColumnMajor, _iface);
        }

        #endregion Interface ITableProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new TableProviderWrapper( peer, (ITableProvider) iface );
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITableProvider _iface;

        #endregion Private Fields
    }
}
