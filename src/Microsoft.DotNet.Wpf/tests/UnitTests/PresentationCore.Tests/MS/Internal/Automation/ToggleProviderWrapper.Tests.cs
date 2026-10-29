// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MS.Internal.Automation;

public sealed class ToggleProviderWrapperTests
{
    [WpfFact]
    public void CheckBoxAutomationPeer_Wrap_Properties_Methods_ReturnsExpected()
    {
        CheckBox checkBox = new();
        CheckBoxAutomationPeer? peer = checkBox.CreateAutomationPeer() as CheckBoxAutomationPeer;

        Assert.NotNull(peer);

        ToggleProviderWrapper? wrapper = peer.GetWrappedPattern(TogglePatternIdentifiers.Pattern.Id) as ToggleProviderWrapper;
        Assert.NotNull(wrapper);

        Assert.Equal(ToggleState.Off, wrapper.ToggleState);

        // We currently do not require CheckBox to be a part of visual tree for TogglePattern to be available, so we can toggle it without any issues
        wrapper.Toggle();
        Assert.Equal(ToggleState.On, wrapper.ToggleState);
    }
}
