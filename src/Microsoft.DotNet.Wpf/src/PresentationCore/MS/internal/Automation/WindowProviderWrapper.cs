// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IWindowProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class WindowProviderWrapper : MarshalByRefObject, IWindowProvider
{
    private readonly AutomationPeer _peer;
    private readonly IWindowProvider _iface;

    private WindowProviderWrapper(AutomationPeer peer, IWindowProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void SetVisualState(WindowVisualState state)
    {
        ElementUtil.Invoke(_peer, static (state, visualState) => state.SetVisualState(visualState), _iface, state);
    }

    public void Close()
    {
        ElementUtil.Invoke(_peer, static (state) => state.Close(), _iface);
    }

    public bool WaitForInputIdle(int milliseconds)
    {
        return ElementUtil.Invoke(_peer, static (state, milliseconds) => state.WaitForInputIdle(milliseconds), _iface, milliseconds);
    }

    public bool Maximizable
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Maximizable, _iface);
    }

    public bool Minimizable
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Minimizable, _iface);
    }

    public bool IsModal
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsModal, _iface);
    }

    public WindowVisualState VisualState
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.VisualState, _iface);
    }

    public WindowInteractionState InteractionState
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.InteractionState, _iface);
    }

    public bool IsTopmost
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsTopmost, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IWindowProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new WindowProviderWrapper(peer, (IWindowProvider)iface);
    }
}
