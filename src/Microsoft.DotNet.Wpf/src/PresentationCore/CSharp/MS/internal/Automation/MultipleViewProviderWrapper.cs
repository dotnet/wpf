// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Multiple View pattern provider wrapper for WCP
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
    internal class MultipleViewProviderWrapper: MarshalByRefObject, IMultipleViewProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private MultipleViewProviderWrapper( AutomationPeer peer, IMultipleViewProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IMultipleViewProvider
        //
        //------------------------------------------------------
 
        #region Interface IMultipleViewProvider

        public string GetViewName( int viewID )
        {
            return (string) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetViewName ), viewID );
        }

        public void SetCurrentView( int viewID )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( SetCurrentView ), viewID );
        }    

        public int CurrentView
        {
            get
            {
                return (int) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCurrentView ), null );
            }
        }

        public int [] GetSupportedViews()
        {
            return (int []) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetSupportedViews ), null );
        }

        #endregion Interface IMultipleViewProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new MultipleViewProviderWrapper( peer, (IMultipleViewProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object GetViewName( object arg )
        {
            return _iface.GetViewName( (int) arg );
        }

        private object SetCurrentView( object arg )
        {
            _iface.SetCurrentView( (int) arg );
            return null;
        }    

        private object GetCurrentView( object unused )
        {
            return _iface.CurrentView;
        }

        private object GetSupportedViews( object unused )
        {
            return _iface.GetSupportedViews();
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IMultipleViewProvider _iface;

        #endregion Private Fields
    }
}
