// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Selection pattern provider wrapper for WCP
//
//

using System;
using System.Windows.Threading;

using System.Windows.Media;
using System.Collections;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using System.ComponentModel;

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
    internal class SelectionProviderWrapper: MarshalByRefObject, ISelectionProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SelectionProviderWrapper( AutomationPeer peer, ISelectionProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ISelectionProvider
        //
        //------------------------------------------------------
 
        #region Interface ISelectionProvider

        public IRawElementProviderSimple [] GetSelection()
        {
            return (IRawElementProviderSimple []) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetSelection ), null );
        }

        public bool CanSelectMultiple
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCanSelectMultiple ), null );
            }
        }

        public bool IsSelectionRequired 
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetIsSelectionRequired ), null );
            } 
        }

        #endregion Interface ISelectionProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new SelectionProviderWrapper( peer, (ISelectionProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object GetSelection( object unused )
        {
            return _iface.GetSelection();
        }

        private object GetCanSelectMultiple( object unused )
        {
            return _iface.CanSelectMultiple;
        }

        private object GetIsSelectionRequired( object unused )
        {
            return _iface.IsSelectionRequired;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ISelectionProvider _iface;

        #endregion Private Fields
    }
}
