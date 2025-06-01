// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IGridProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class GridProviderWrapper : MarshalByRefObject, IGridProvider
{
    private readonly AutomationPeer _peer;
    private readonly IGridProvider _iface;

    private GridProviderWrapper(AutomationPeer peer, IGridProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public IRawElementProviderSimple? GetItem(int row, int column)
    {
        return ElementUtil.Invoke(_peer, static (state, rowColumn) => state.GetItem(rowColumn[0], rowColumn[1]), _iface, new int[] { row, column });
    }

    public int RowCount
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.RowCount, _iface);
    }

    public int ColumnCount
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.ColumnCount, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IGridProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new GridProviderWrapper(peer, (IGridProvider)iface);
    }
}
