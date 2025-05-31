// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class GridItemProviderWrapper : MarshalByRefObject, IGridItemProvider
{
    private readonly AutomationPeer _peer;
    private readonly IGridItemProvider _iface;

    private GridItemProviderWrapper(AutomationPeer peer, IGridItemProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public int Row
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Row, _iface);
    }

    public int Column
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Column, _iface);
    }

    public int RowSpan
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.RowSpan, _iface);
    }

    public int ColumnSpan
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.ColumnSpan, _iface);
    }

    public IRawElementProviderSimple ContainingGrid
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.ContainingGrid, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IGridItemProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new GridItemProviderWrapper(peer, (IGridItemProvider)iface);
    }
}
