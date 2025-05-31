// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: Range Value pattern provider wrapper for WCP
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
    internal sealed class RangeValueProviderWrapper : MarshalByRefObject, IRangeValueProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private RangeValueProviderWrapper(AutomationPeer peer, IRangeValueProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IRangeValueProvider
        //
        //------------------------------------------------------
 
        #region Interface IRangeValueProvider

        public void SetValue(double val)
        {
            ElementUtil.Invoke(_peer, static (state, val) => state.SetValue(val), _iface, val);
        }

        public double Value
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.Value, _iface);
        }

        public bool IsReadOnly
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.IsReadOnly, _iface);
        }

        public double Maximum
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.Maximum, _iface);
        }

        public double Minimum
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.Minimum, _iface);
        }

        public double LargeChange
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.LargeChange, _iface);
        }

        public double SmallChange
        {
            get => ElementUtil.Invoke(_peer, static (state) => state.SmallChange, _iface);
        }

        #endregion Interface IRangeValueProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static object Wrap( AutomationPeer peer, object iface )
        {
            return new RangeValueProviderWrapper( peer, (IRangeValueProvider) iface );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private readonly AutomationPeer _peer;
        private readonly IRangeValueProvider _iface;

        #endregion Private Fields
    }
}
