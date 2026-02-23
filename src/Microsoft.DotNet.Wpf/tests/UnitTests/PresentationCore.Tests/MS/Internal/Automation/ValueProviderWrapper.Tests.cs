// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace MS.Internal.Automation;

public sealed class ValueProviderWrapperTests
{
    [WpfFact]
    public void TextBoxAutomationPeer_Wrap_Properties_Methods_ReturnsExpected()
    {
        TextBox textBox = new();
        TextBoxAutomationPeer? peer = textBox.CreateAutomationPeer() as TextBoxAutomationPeer;

        Assert.NotNull(peer);

        ValueProviderWrapper? wrapper = peer.GetWrappedPattern(ValuePatternIdentifiers.Pattern.Id) as ValueProviderWrapper;
        Assert.NotNull(wrapper);

        // Default TextProperty value for TextBox is string.Empty
        Assert.Equal(string.Empty, wrapper.Value);
        Assert.False(wrapper.IsReadOnly);

        // We currently do not require TextBox to be a part of visual tree for ValuePattern to be available, so we can interact with it without any issues
        wrapper.SetValue("New Value");
        Assert.Equal("New Value", wrapper.Value);

        // Set the TextBox to be read-only and verify the wrapper reflects that change
        textBox.IsReadOnly = true;
        Assert.True(wrapper.IsReadOnly);
    }
}
