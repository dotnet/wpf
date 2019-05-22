// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Transform pattern provider wrapper for WCP
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
    internal class TransformProviderWrapper: MarshalByRefObject, ITransformProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TransformProviderWrapper( AutomationPeer peer, ITransformProvider iface )
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
 
        #region Interface ITransformProvider


        public void Move( double x, double y )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Move ), new double [ ] { x, y } );
        }

        public void Resize( double width, double height )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Resize ), new double [ ] { width, height } );
        }

        public void Rotate( double degrees )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Rotate ), degrees );
        }

        public bool CanMove
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCanMove ), null );
            }
        }
        
        public bool CanResize
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCanResize ), null );
            }
        }
        
        public bool CanRotate
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCanRotate ), null );
            }
        }
        
        #endregion Interface ITransformProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new TransformProviderWrapper( peer, (ITransformProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object Move( object arg )
        {
            double [ ] args = (double [ ]) arg;
            _iface.Move( args[ 0 ], args[ 1 ] );
            return null;
        }

        private object Resize( object arg )
        {
            double [ ] args = (double [ ]) arg;
            _iface.Resize( args[ 0 ], args[ 1 ] );
            return null;
        }

        private object Rotate( object arg )
        {
            _iface.Rotate( (double)arg );
            return null;
        }

        private object GetCanMove( object unused )
        {
            return _iface.CanMove;
        }
        
        private object GetCanResize( object unused )
        {
            return _iface.CanResize;
        }
        
        private object GetCanRotate( object unused )
        {
            return _iface.CanRotate;
        }
        
        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private ITransformProvider _iface;

        #endregion Private Fields
    }
}
