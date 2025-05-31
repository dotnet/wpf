// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class SelectionItemProviderWrapper : MarshalByRefObject, ISelectionItemProvider
{
    private readonly AutomationPeer _peer;
    private readonly ISelectionItemProvider _iface;

    private SelectionItemProviderWrapper(AutomationPeer peer, ISelectionItemProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void Select()
    {
        ElementUtil.Invoke(_peer, static (state) => state.Select(), _iface);
    }

    public void AddToSelection()
    {
        ElementUtil.Invoke(_peer, static (state) => state.AddToSelection(), _iface);
    }

    public void RemoveFromSelection()
    {
        ElementUtil.Invoke(_peer, static (state) => state.RemoveFromSelection(), _iface);
    }

    public bool IsSelected
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsSelected, _iface);
    }

    public IRawElementProviderSimple SelectionContainer
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.SelectionContainer, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ISelectionItemProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new SelectionItemProviderWrapper(peer, (ISelectionItemProvider)iface);
    }
}
