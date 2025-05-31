// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IItemContainerProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
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

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IItemContainerProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new ItemContainerProviderWrapper(peer, (IItemContainerProvider)iface);
    }
}
