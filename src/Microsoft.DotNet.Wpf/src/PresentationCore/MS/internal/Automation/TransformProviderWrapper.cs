// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Transform pattern provider wrapper for WCP
//
//

#nullable enable

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
    internal sealed class TransformProviderWrapper : MarshalByRefObject, ITransformProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private TransformProviderWrapper(AutomationPeer peer, ITransformProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

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


        public void Move(double x, double y)
        {
            ElementUtil.Invoke(_peer, static (state, coordinates) => state.Move(coordinates[0], coordinates[1]), _iface, new double[] { x, y });
        }

        public void Resize(double width, double height)
        {
            ElementUtil.Invoke(_peer, static (state, dimensions) => state.Resize(dimensions[0], dimensions[1]), _iface, new double[] { width, height });
        }

        public void Rotate(double degrees)
        {
            ElementUtil.Invoke(_peer, static (state, degrees) => state.Rotate(degrees), _iface, degrees);
        }

        public bool CanMove
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.CanMove, _iface);
        }

        public bool CanResize
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.CanResize, _iface);
        }

        public bool CanRotate
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.CanRotate, _iface);
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
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private readonly AutomationPeer _peer;
        private readonly ITransformProvider _iface;

        #endregion Private Fields
    }
}
