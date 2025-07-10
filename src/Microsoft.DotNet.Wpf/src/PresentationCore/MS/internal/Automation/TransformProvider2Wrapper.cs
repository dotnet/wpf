// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal class TransformProvider2Wrapper: TransformProviderWrapper, ITransformProvider2
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TransformProvider2Wrapper( AutomationPeer peer, ITransformProvider2 iface ) : base(peer, iface)
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
 
        #region Interface ITransformProvider2


        public void Zoom( double zoomAmount )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( Zoom ), zoomAmount );
        }

        public void ZoomByUnit( ZoomUnit zoomUnit )
        {
            ElementUtil.Invoke( _peer, new DispatcherOperationCallback( ZoomByUnit ), zoomUnit );
        }

        public bool CanZoom
        {
            get
            {
                return (bool) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetCanZoom ), null );
            }
        }
        
        public double ZoomLevel
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback( GetZoomLevel ), null );
            }
        }
        
        public double ZoomMinimum
        {
            get
            {
                return (double) ElementUtil.Invoke( _peer, new DispatcherOperationCallback(GetZoomMinimum ), null );
            }
        }


        public double ZoomMaximum
        {
            get
            {
                return (double)ElementUtil.Invoke(_peer, new DispatcherOperationCallback(GetZoomMaximum), null);
            }
        }

        #endregion Interface ITransformProvider2


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static new object Wrap( AutomationPeer peer, object iface )
        {
            return new TransformProvider2Wrapper( peer, (ITransformProvider2) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private object Zoom( object arg )
        {
            _iface.Zoom( (double)arg );
            return null;
        }

        private object ZoomByUnit( object arg )
        {
            _iface.ZoomByUnit( (ZoomUnit)arg );
            return null;
        }

        private object GetCanZoom( object unused )
        {
            return _iface.CanZoom;
        }
        
        private object GetZoomLevel( object unused )
        {
            return _iface.ZoomLevel;
        }
        
        private object GetZoomMinimum( object unused )
        {
            return _iface.ZoomMinimum;
        }

        private object GetZoomMaximum(object unused)
        {
            return _iface.ZoomMaximum;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private AutomationPeer _peer;
        private ITransformProvider2 _iface;

        #endregion Private Fields
    }
}
