// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Grid pattern provider wrapper for WCP
//
//

using System.Windows.Threading;
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
    internal class GridProviderWrapper: MarshalByRefObject, IGridProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private GridProviderWrapper( AutomationPeer peer, IGridProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IGridProvider
        //
        //------------------------------------------------------
 
        #region Interface IGridProvider

        public IRawElementProviderSimple GetItem(int row, int column)
        {
            return ElementUtil.Invoke(_peer, static (state, rowColumn) => state.GetItem(rowColumn[0], rowColumn[1]), this, new int[] { row, column });
        }

        public int RowCount
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.RowCount, this);
        }

        public int ColumnCount
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.ColumnCount, this);
        }

        #endregion Interface IGridProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new GridProviderWrapper( peer, (IGridProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IGridProvider _iface;

        #endregion Private Fields
    }
}
