// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IExpandCollapseProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class ExpandCollapseProviderWrapper : MarshalByRefObject, IExpandCollapseProvider
{
    private readonly AutomationPeer _peer;
    private readonly IExpandCollapseProvider _iface;

    private ExpandCollapseProviderWrapper(AutomationPeer peer, IExpandCollapseProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void Expand()
    {
        ElementUtil.Invoke(_peer, static (state) => state.Expand(), _iface);
    }

    public void Collapse()
    {
        ElementUtil.Invoke(_peer, static (state) => state.Collapse(), _iface);
    }

    public ExpandCollapseState ExpandCollapseState
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.ExpandCollapseState, _iface);
    }

    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new ExpandCollapseProviderWrapper(peer, (IExpandCollapseProvider)iface);
    }
}
