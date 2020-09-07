// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Selection Item pattern provider wrapper for WCP
//
//

using System;
using System.Windows.Threading;

using System.Windows.Media;
using System.Collections;
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

    internal class SelectionItemProviderWrapper: MarshalByRefObject, ISelectionItemProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private SelectionItemProviderWrapper( AutomationPeer peer, ISelectionItemProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface ISelectionItemProvider
        //
        //------------------------------------------------------
 
        #region Interface ISelectionItemProvider

        public void Select()
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Select ), null );
        }

        public void AddToSelection()
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( AddToSelection ), null );
        }
        
        public void RemoveFromSelection()
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( RemoveFromSelection ), null );
        }

        public bool IsSelected
        { 
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetIsSelected ), null );
            }
        }

        public IRawElementProviderSimple SelectionContainer
        {
            get
            {
                return (IRawElementProviderSimple) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetSelectionContainer ), null );
            }
        }

        #endregion Interface ISelectionItemProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new SelectionItemProviderWrapper( peer, (ISelectionItemProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object Select( object unused )
        {
            _iface.Select();
            return null;
        }

        private object AddToSelection( object unused )
        {
            _iface.AddToSelection();
            return null;
        }
        
        private object RemoveFromSelection( object unused )
        {
            _iface.RemoveFromSelection();
            return null;
        }

        private object GetIsSelected( object unused )
        { 
            return _iface.IsSelected;
        }

        private object GetSelectionContainer( object unused )
        {
            return _iface.SelectionContainer;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ISelectionItemProvider _iface;

        #endregion Private Fields
    }
}
