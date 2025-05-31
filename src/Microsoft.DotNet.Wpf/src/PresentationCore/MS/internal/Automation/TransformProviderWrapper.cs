// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

namespace MS.Internal.Automation;

internal sealed class TransformProviderWrapper : MarshalByRefObject, ITransformProvider
{
    private readonly AutomationPeer _peer;
    private readonly ITransformProvider _iface;

    private TransformProviderWrapper(AutomationPeer peer, ITransformProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public void Move(double x, double y)
    {
        ElementUtil.Invoke(_peer, static (state, coordinates) => state.Move(coordinates[0], coordinates[1]), _iface, new double[] { x, y });
    }

    public void Resize(double width, double height)
    {
        ElementUtil.Invoke(_peer, static (state, dimensions) => state.Resize(dimensions[0], dimensions[1]), _iface, new double[] { width, height });
    }

    public void Rotate(double degrees)
    {
        ElementUtil.Invoke(_peer, static (state, degrees) => state.Rotate(degrees), _iface, degrees);
    }

    public bool CanMove
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.CanMove, _iface);
    }

    public bool CanResize
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.CanResize, _iface);
    }

    public bool CanRotate
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.CanRotate, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ITransformProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new TransformProviderWrapper(peer, (ITransformProvider)iface);
    }
}
