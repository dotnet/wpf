// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: Item Container pattern provider wrapper for WPF
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
    internal sealed class ItemContainerProviderWrapper : MarshalByRefObject, IItemContainerProvider
    {
        private readonly AutomationPeer _peer;
        private readonly IItemContainerProvider _iface;

        private ItemContainerProviderWrapper(AutomationPeer peer, IItemContainerProvider iface)
        {
            Debug.Assert(peer is not null);
            Debug.Assert(iface is not null);

            _peer = peer;
            _iface = iface;
        }

        public IRawElementProviderSimple FindItemByProperty(IRawElementProviderSimple startAfter, int propertyId, object value)
        {
            object[] args = [startAfter, propertyId, value];

            // The actual invocation method that gets called on the peer's context.
            static IRawElementProviderSimple FindItemByProperty(IItemContainerProvider state, object[] args)
            {
                IRawElementProviderSimple startAfter = (IRawElementProviderSimple)args[0];
                int propertyId = (int)args[1];
                object value = args[2];

                return state.FindItemByProperty(startAfter, propertyId, value);
            }

            return ElementUtil.Invoke(_peer, FindItemByProperty, _iface, args);
        }

        internal static object Wrap(AutomationPeer peer, object iface)
        {
            return new ItemContainerProviderWrapper(peer, (IItemContainerProvider)iface);
        }
    }
}
