// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

/// <summary>
/// Wrapper class for the <see cref="ISynchronizedInputProvider"/> interface, calls through to the managed <see cref="AutomationPeer"/>
/// that implements it. The calls are made on the peer's context to ensure that the correct synchronization context is used.
/// </summary>
internal sealed class SynchronizedInputProviderWrapper : MarshalByRefObject, ISynchronizedInputProvider
{
    private readonly AutomationPeer _peer;
    private readonly ISynchronizedInputProvider _iface;

    private SynchronizedInputProviderWrapper(AutomationPeer peer, ISynchronizedInputProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void StartListening(SynchronizedInputType inputType)
    {
        ElementUtil.Invoke(_peer, static (state, inputType) => state.StartListening(inputType), _iface, inputType);
    }

    public void Cancel()
    {
        ElementUtil.Invoke(_peer, static (state) => state.Cancel(), _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ISynchronizedInputProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new SynchronizedInputProviderWrapper(peer, (ISynchronizedInputProvider)iface);
    }
}
