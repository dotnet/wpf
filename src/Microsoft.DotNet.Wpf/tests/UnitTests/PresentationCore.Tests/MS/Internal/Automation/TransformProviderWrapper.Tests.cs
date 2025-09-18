// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MS.Internal.Automation;

public sealed class TransformProviderWrapperTests
{
    [WpfFact]
    public void GridSplitterAutomationPeer_Wrap_Properties_Methods_ReturnsExpected()
    {
        GridSplitter gridSplitter = new();
        GridSplitterAutomationPeer? peer = gridSplitter.CreateAutomationPeer() as GridSplitterAutomationPeer;

        Assert.NotNull(peer);

        TransformProviderWrapper? wrapper = peer.GetWrappedPattern(TransformPatternIdentifiers.Pattern.Id) as TransformProviderWrapper;
        Assert.NotNull(wrapper);

        Assert.True(wrapper.CanMove);

        // GridSplitter does not support resizing or rotating, so these properties should return false
        Assert.False(wrapper.CanResize);
        Assert.False(wrapper.CanRotate);

        // Attempting to call Move should not throw an exception
        wrapper.Move(10, 10);

        // Unsupported operations should throw exceptions
        Assert.Throws<InvalidOperationException>(() => wrapper.Resize(100, 100));
        Assert.Throws<InvalidOperationException>(() => wrapper.Rotate(45));
    }
}
