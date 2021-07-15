// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Scroll pattern provider wrapper for WCP
//
//

using System;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;
using MS.Internal.Automation;

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

    internal class ScrollProviderWrapper: MarshalByRefObject, IScrollProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private ScrollProviderWrapper( AutomationPeer peer, IScrollProvider iface )
        {
            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IScrollProvider
        //
        //------------------------------------------------------
 
        #region Interface IScrollProvider

        public void Scroll( ScrollAmount horizontalAmount, ScrollAmount verticalAmount )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Scroll ), new ScrollAmount [ ] { horizontalAmount, verticalAmount } );
        }

        public void SetScrollPercent( double horizontalPercent, double verticalPercent )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( SetScrollPercent ), new double [ ] { horizontalPercent, verticalPercent } );
        }
        
        public double HorizontalScrollPercent
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetHorizontalScrollPercent ), null );
            }
        }

        public double VerticalScrollPercent
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetVerticalScrollPercent ), null );
            }
        }

        public double HorizontalViewSize
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetHorizontalViewSize ), null );
            }
        }

        public double VerticalViewSize
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetVerticalViewSize ), null );
            }
        }
        
        public bool HorizontallyScrollable
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetHorizontallyScrollable ), null );
            }
        }
        
        public bool VerticallyScrollable
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetVerticallyScrollable ), null );
            }
        }

        #endregion Interface IScrollProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new ScrollProviderWrapper( peer, (IScrollProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object Scroll( object arg )
        {
            ScrollAmount [ ] args = (ScrollAmount [ ]) arg;
            _iface.Scroll( args[ 0 ], args[ 1 ] );
            return null;
        }

        private object SetScrollPercent( object arg )
        {
            double [ ] args = (double [ ]) arg;
            _iface.SetScrollPercent( args[ 0 ], args[ 1 ] );
            return null;
        }
        
        private object GetHorizontalScrollPercent( object unused )
        {
            return _iface.HorizontalScrollPercent;
        }

        private object GetVerticalScrollPercent( object unused )
        {
            return _iface.VerticalScrollPercent;
        }

        private object GetHorizontalViewSize( object unused )
        {
            return _iface.HorizontalViewSize;
        }

        private object GetVerticalViewSize( object unused )
        {
            return _iface.VerticalViewSize;
        }
        
        private object GetHorizontallyScrollable( object unused )
        {
            return _iface.HorizontallyScrollable;
        }
        
        private object GetVerticallyScrollable( object unused )
        {
            return _iface.VerticallyScrollable;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationPeer _peer;
        private IScrollProvider _iface;

        #endregion Private Fields
    }
}
