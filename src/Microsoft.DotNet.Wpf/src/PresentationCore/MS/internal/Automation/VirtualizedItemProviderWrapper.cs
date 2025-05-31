// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: Virtualized Item pattern provider wrapper for WPF
//
//

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation
{
    // Automation/WPF Wrapper class: Implements that UIAutomation I...Provider
    // interface, and calls through to a WPF AutomationPeer which implements the corresponding
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
    internal sealed class VirtualizedItemProviderWrapper : MarshalByRefObject, IVirtualizedItemProvider
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        private VirtualizedItemProviderWrapper(AutomationPeer peer, IVirtualizedItemProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

            _peer = peer;
            _iface = iface;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Interface IVirtualizedItemProvider
        //
        //------------------------------------------------------

        #region Interface IVirtualizedItemProvider

        public void Realize()
        {
            ElementUtil.Invoke(_peer, static (state) => state.Realize(), _iface);
        }

        #endregion Interface IVirtualizedItemProvider


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static object Wrap(AutomationPeer peer, object iface)
        {
            return new VirtualizedItemProviderWrapper(peer, (IVirtualizedItemProvider)iface);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly AutomationPeer _peer;
        private readonly IVirtualizedItemProvider _iface;

        #endregion Private Fields
    }
}
