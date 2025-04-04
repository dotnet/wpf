// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Input;

namespace System.Windows.Interop;

internal sealed class HwndSourceKeyboardInputSite : IKeyboardInputSite
{
    private HwndSource _source;
    private IKeyboardInputSink _sink;
    private readonly UIElement _sinkElement;

    internal HwndSourceKeyboardInputSite(HwndSource source, IKeyboardInputSink sink)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sink);

        if (sink is not UIElement uiElement)
        {
            throw new ArgumentException(SR.KeyboardSinkMustBeAnElement, nameof(sink));
        }

        _source = source;

        _sink = sink;
        _sink.KeyboardInputSite = this;

        _sinkElement = uiElement;
    }

    /// <summary>
    ///     Unregisters a child KeyboardInputSink from this sink.
    /// </summary>
    void IKeyboardInputSite.Unregister()
    {
        if (_source is not null && _sink is not null)
        {
            _source.CriticalUnregisterKeyboardInputSink(this);
            _sink.KeyboardInputSite = null;
        }

        _source = null;
        _sink = null;
    }

    /// <summary>
    ///     Returns the sink associated with this site (the "child", not
    ///     the "parent" sink that owns the site).  There's no way of
    ///     getting from the site to the parent sink.
    /// </summary> 
    IKeyboardInputSink IKeyboardInputSite.Sink
    {
        get
        {
            return _sink;
        }
    }

    /// <summary>
    ///     Components call this when they want to move focus ("tab") but
    ///     have nowhere further to tab within their own component.  Return
    ///     value is true if the site moved focus, false if the calling
    ///     component still has focus and should wrap around.
    /// </summary> 
    bool IKeyboardInputSite.OnNoMoreTabStops(TraversalRequest request)
    {
        return _sinkElement is not null && _sinkElement.MoveFocus(request);
    }
}