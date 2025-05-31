// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Peers;

#nullable enable

namespace MS.Internal.Automation;

internal sealed class TextProviderWrapper : MarshalByRefObject, ITextProvider
{
    private readonly AutomationPeer _peer;
    private readonly ITextProvider _iface;

    private TextProviderWrapper(AutomationPeer peer, ITextProvider iface)
    {
        Debug.Assert(peer is not null);
        Debug.Assert(iface is not null);

        _peer = peer;
        _iface = iface;
    }

    public ITextRangeProvider[] GetSelection()
    {
        return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetSelection(), peer), _iface, _peer);
    }

    public ITextRangeProvider[] GetVisibleRanges()
    {
        return ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.GetVisibleRanges(), peer), _iface, _peer);
    }

    public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
    {
        if (childElement is not ElementProxy)
            throw new ArgumentException(SR.Format(SR.TextProvider_InvalidChild, nameof(childElement)));

        // The actual invocation method that gets called on the peer's context.
        static ITextRangeProvider RangeFromChild(TextProviderWrapper state, IRawElementProviderSimple childElement)
        {
            return TextRangeProviderWrapper.WrapArgument(state._iface.RangeFromChild(childElement), state._peer);
        }

        return ElementUtil.Invoke(_peer, RangeFromChild, this, childElement);
    }

    public ITextRangeProvider RangeFromPoint(Point screenLocation)
    {
        // The actual invocation method that gets called on the peer's context.
        static ITextRangeProvider RangeFromPoint(TextProviderWrapper state, Point screenLocation)
        {
            return TextRangeProviderWrapper.WrapArgument(state._iface.RangeFromPoint(screenLocation), state._peer);
        }

        return ElementUtil.Invoke(_peer, RangeFromPoint, this, screenLocation);
    }

    public ITextRangeProvider DocumentRange
    {
        get => ElementUtil.Invoke(_peer, static (state, peer) => TextRangeProviderWrapper.WrapArgument(state.DocumentRange, peer), _iface, _peer);
    }

    public SupportedTextSelection SupportedTextSelection
    {
        get => ElementUtil.Invoke(_peer, static (state) => state.SupportedTextSelection, _iface);
    }

    /// <summary>
    /// Creates a wrapper for the given <see cref="AutomationPeer"/> and <see cref="ITextProvider"/> interface.
    /// </summary>
    internal static object Wrap(AutomationPeer peer, object iface)
    {
        return new TextProviderWrapper(peer, (ITextProvider)iface);
    }
}

