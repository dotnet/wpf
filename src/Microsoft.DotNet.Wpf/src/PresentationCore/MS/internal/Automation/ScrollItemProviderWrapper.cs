// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class ScrollItemProviderWrapper : MarshalByRefObject, IScrollItemProvider
{
    private readonly AutomationPeer _peer;
    private readonly IScrollItemProvider _iface;

    private ScrollItemProviderWrapper(AutomationPeer peer, IScrollItemProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void ScrollIntoView()
    {
        ElementUtil.Invoke(_peer, static (state) => state.ScrollIntoView(), _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="IScrollItemProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new ScrollItemProviderWrapper(peer, (IScrollItemProvider)iface);
    }
}
