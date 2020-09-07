// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Window pattern provider wrapper for WCP
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
    internal class WindowProviderWrapper: MarshalByRefObject, IWindowProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private WindowProviderWrapper( AutomationPeer peer, IWindowProvider iface)
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IWindowProvider
        //
        //------------------------------------------------------
 
        #region Interface IWindowProvider

        public void SetVisualState( WindowVisualState state )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( SetVisualState ), state );
        }

        public void Close()
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Close ), null );
        }

        public bool WaitForInputIdle( int milliseconds )
        {
            return (bool)ElementUtil.Invoke( _peer, new DispatcherOperationCallback( WaitForInputIdle ), milliseconds );
        }

        public bool Maximizable 
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetMaximizable ), null );
            }
        }
        
        public bool Minimizable
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetMinimizable ), null );
            }
        }
        
        public bool IsModal
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetIsModal ), null );
            }
        }
        
        public WindowVisualState VisualState
        {
            get
            {
                return (WindowVisualState) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetVisualState ), null );
            }
        }
        
        public WindowInteractionState InteractionState
        {
            get
            {
                return (WindowInteractionState) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetInteractionState ), null );
            }
        }
        
        public bool IsTopmost
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetIsTopmost ), null );
            }
        }

        #endregion Interface IWindowProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface)
        {
            return new WindowProviderWrapper( peer, (IWindowProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object SetVisualState( object arg )
        {
            _iface.SetVisualState( (WindowVisualState) arg );
            return null;
        }

        private object WaitForInputIdle( object arg )
        {
            return _iface.WaitForInputIdle( (int) arg );
        }

        private object Close( object unused )
        {
            _iface.Close();
            return null;
        }

        private object GetMaximizable( object unused )
        {
            return _iface.Maximizable;
        }
        
        private object GetMinimizable( object unused )
        {
            return _iface.Minimizable;
        }
        
        private object GetIsModal( object unused )
        {
            return _iface.IsModal;
        }
        
        private object GetVisualState( object unused )
        {
            return _iface.VisualState;
        }
        
        private object GetInteractionState( object unused )
        {
            return _iface.InteractionState;
        }
        
        private object GetIsTopmost( object unused )
        {
            return _iface.IsTopmost;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IWindowProvider _iface;

        #endregion Private Fields
    }
}
