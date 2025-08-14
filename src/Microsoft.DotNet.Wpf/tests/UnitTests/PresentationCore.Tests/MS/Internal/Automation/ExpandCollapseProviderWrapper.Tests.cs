// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MS.Internal.Automation;

public sealed class ExpandCollapseProviderWrapperTests
{
    [WpfFact]
    public void ComboBoxAutomationPeer_Wrap_Properties_Methods_ReturnsExpected()
    {
        ComboBox comboBox = new();
        ComboBoxAutomationPeer? peer = comboBox.CreateAutomationPeer() as ComboBoxAutomationPeer;

        Assert.NotNull(peer);

        ExpandCollapseProviderWrapper? wrapper = peer.GetWrappedPattern(ExpandCollapsePatternIdentifiers.Pattern.Id) as ExpandCollapseProviderWrapper;
        Assert.NotNull(wrapper);

        Assert.Equal(ExpandCollapseState.Collapsed, wrapper.ExpandCollapseState);

        // TODO: To check the state after Expand and Collapse methods, we'd require valid ComboBox in the visual tree.
        wrapper.Expand();
        wrapper.Collapse();
    }
}
