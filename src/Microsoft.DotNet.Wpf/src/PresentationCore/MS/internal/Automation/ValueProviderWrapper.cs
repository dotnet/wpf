// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class ValueProviderWrapper : MarshalByRefObject, IValueProvider
{
    private readonly AutomationPeer _peer;
    private readonly IValueProvider _iface;

    private ValueProviderWrapper(AutomationPeer peer, IValueProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void SetValue(string val)
    {
        ElementUtil.Invoke(_peer, static (state, value) => state.SetValue(value), _iface, val);
    }

    public string Value
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Value, _iface);
    }

    public bool IsReadOnly
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsReadOnly, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IValueProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new ValueProviderWrapper(peer, (IValueProvider)iface);
    }
}
