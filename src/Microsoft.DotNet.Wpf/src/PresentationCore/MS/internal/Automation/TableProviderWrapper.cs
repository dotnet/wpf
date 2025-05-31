// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class TableProviderWrapper : MarshalByRefObject, ITableProvider
{
    private readonly AutomationPeer _peer;
    private readonly ITableProvider _iface;

    private TableProviderWrapper(AutomationPeer peer, ITableProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public IRawElementProviderSimple GetItem(int row, int column)
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

    public IRawElementProviderSimple[] GetRowHeaders()
    {
        return ElementUtil.Invoke(_peer, static (state) => state.GetRowHeaders(), _iface);
    }

    public IRawElementProviderSimple[] GetColumnHeaders()
    {
        return ElementUtil.Invoke(_peer, static (state) => state.GetColumnHeaders(), _iface);
    }

    public RowOrColumnMajor RowOrColumnMajor
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.RowOrColumnMajor, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ITableProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new TableProviderWrapper(peer, (ITableProvider)iface);
    }
}
