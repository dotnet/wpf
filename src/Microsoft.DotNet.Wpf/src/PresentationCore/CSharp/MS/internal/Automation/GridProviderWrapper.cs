// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Grid pattern provider wrapper for WCP
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
            return (IRawElementProviderSimple) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetItem ), new int [ ] { row, column } );
        } 

        public int RowCount
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetRowCount ), null );
            }
        }

        public int ColumnCount
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetColumnCount ), null );
            }
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
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object GetItem( object arg )
        {
            int [ ] coords = (int [ ]) arg;
            return _iface.GetItem( coords[ 0 ], coords[ 1 ] );
        } 

        private object GetRowCount( object unused )
        {
            return _iface.RowCount;
        }

        private object GetColumnCount( object unused )
        {
            return _iface.ColumnCount;
        }

        #endregion Private Methods


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
