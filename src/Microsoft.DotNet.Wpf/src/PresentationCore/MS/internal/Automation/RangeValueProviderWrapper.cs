// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="IRangeValueProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class RangeValueProviderWrapper : MarshalByRefObject, IRangeValueProvider
{
    private readonly AutomationPeer _peer;
    private readonly IRangeValueProvider _iface;

    private RangeValueProviderWrapper(AutomationPeer peer, IRangeValueProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void SetValue(double val)
    {
        ElementUtil.Invoke(_peer, static (state, val) => state.SetValue(val), _iface, val);
    }

    public double Value
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Value, _iface);
    }

    public bool IsReadOnly
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.IsReadOnly, _iface);
    }

    public double Maximum
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Maximum, _iface);
    }

    public double Minimum
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.Minimum, _iface);
    }

    public double LargeChange
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.LargeChange, _iface);
    }

    public double SmallChange
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.SmallChange, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IRangeValueProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new RangeValueProviderWrapper(peer, (IRangeValueProvider)iface);
    }
}
