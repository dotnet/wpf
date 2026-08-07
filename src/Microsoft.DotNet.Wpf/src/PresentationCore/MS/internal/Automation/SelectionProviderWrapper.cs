// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="ISelectionProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class SelectionProviderWrapper : MarshalByRefObject, ISelectionProvider
{
    private readonly AutomationPeer _peer;
    private readonly ISelectionProvider _iface;

    private SelectionProviderWrapper(AutomationPeer peer, ISelectionProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public IRawElementProviderSimple[] GetSelection()
    {
        return ElementUtil.Invoke(_peer, static (state) => state.GetSelection(), _iface);
    }

    public bool CanSelectMultiple
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.CanSelectMultiple, _iface);
    }

    public bool IsSelectionRequired
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsSelectionRequired, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ISelectionProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new SelectionProviderWrapper(peer, (ISelectionProvider)iface);
    }
}
