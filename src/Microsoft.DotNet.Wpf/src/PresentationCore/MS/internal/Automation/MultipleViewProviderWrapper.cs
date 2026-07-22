// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IMultipleViewProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class MultipleViewProviderWrapper : MarshalByRefObject, IMultipleViewProvider
{
    private readonly AutomationPeer _peer;
    private readonly IMultipleViewProvider _iface;

    private MultipleViewProviderWrapper(AutomationPeer peer, IMultipleViewProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public string GetViewName(int viewID)
    {
        return ElementUtil.Invoke(_peer, static (state, viewID) => state.GetViewName(viewID), _iface, viewID);
    }

    public void SetCurrentView(int viewID)
    {
        ElementUtil.Invoke(_peer, static (state, viewID) => state.SetCurrentView(viewID), _iface, viewID);
    }

    public int CurrentView
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.CurrentView, _iface);
    }

    public int[] GetSupportedViews()
    {
        return ElementUtil.Invoke(_peer, static (state) => state.GetSupportedViews(), _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IMultipleViewProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new MultipleViewProviderWrapper(peer, (IMultipleViewProvider)iface);
    }
}
