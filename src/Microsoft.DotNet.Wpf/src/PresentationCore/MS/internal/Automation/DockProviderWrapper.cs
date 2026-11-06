// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IDockProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class DockProviderWrapper : MarshalByRefObject, IDockProvider
{
    private readonly AutomationPeer _peer;
    private readonly IDockProvider _iface;

    private DockProviderWrapper(AutomationPeer peer, IDockProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void SetDockPosition(DockPosition dockPosition)
    {
        ElementUtil.Invoke(_peer, static (state, dockPosition) => state.SetDockPosition(dockPosition), _iface, dockPosition);
    }

    public DockPosition DockPosition
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.DockPosition, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IDockProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new DockProviderWrapper(peer, (IDockProvider)iface);
    }
}
