// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MS.Internal.Automation;

public sealed class RangleValueProviderWrapperTests
{
    [WpfFact]
    public void SliderAutomationPeer_Wrap_Properties_Methods_ReturnsExpected()
    {
        Slider slider = new() { Minimum = 10, Maximum = 100, Value = 50, LargeChange = 25, SmallChange = 10 };
        SliderAutomationPeer? peer = slider.CreateAutomationPeer() as SliderAutomationPeer;

        Assert.NotNull(peer);

        RangeValueProviderWrapper? wrapper = peer.GetWrappedPattern(RangeValuePatternIdentifiers.Pattern.Id) as RangeValueProviderWrapper;
        Assert.NotNull(wrapper);

        Assert.Equal(50, wrapper.Value);
        Assert.Equal(10, wrapper.Minimum);
        Assert.Equal(100, wrapper.Maximum);
        Assert.Equal(25, wrapper.LargeChange);
        Assert.Equal(10, wrapper.SmallChange);

        Assert.False(wrapper.IsReadOnly);

        // We currently do not require Slider to be a part of visual tree for RangeValuePattern to be available, so we can set value without any issues
        wrapper.SetValue(75);
        Assert.Equal(75, wrapper.Value);
    }
}
