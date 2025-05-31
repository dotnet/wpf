// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class ScrollProviderWrapper : MarshalByRefObject, IScrollProvider
{
    private readonly AutomationPeer _peer;
    private readonly IScrollProvider _iface;

    private ScrollProviderWrapper(AutomationPeer peer, IScrollProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
    {
        ElementUtil.Invoke(_peer, static (state, values) => state.Scroll(values[0], values[1]), _iface, new ScrollAmount[] { horizontalAmount, verticalAmount });
    }

    public void SetScrollPercent(double horizontalPercent, double verticalPercent)
    {
        ElementUtil.Invoke(_peer, static (state, values) => state.SetScrollPercent(values[0], values[1]), _iface, new double[] { horizontalPercent, verticalPercent });
    }

    public double HorizontalScrollPercent
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.HorizontalScrollPercent, _iface);
    }

    public double VerticalScrollPercent
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.VerticalScrollPercent, _iface);
    }

    public double HorizontalViewSize
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.HorizontalViewSize, _iface);
    }

    public double VerticalViewSize
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.VerticalViewSize, _iface);
    }

    public bool HorizontallyScrollable
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.HorizontallyScrollable, _iface);
    }

    public bool VerticallyScrollable
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.VerticallyScrollable, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IScrollProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new ScrollProviderWrapper(peer, (IScrollProvider)iface);
    }
}
