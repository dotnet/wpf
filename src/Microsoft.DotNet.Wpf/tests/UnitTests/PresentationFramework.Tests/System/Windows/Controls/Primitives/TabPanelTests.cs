// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls.Primitives;

public class TabPanelTests
{
    [WpfFact]
    public void ArrangeOverride_WithMoreRowsThanMeasure_DoesNotThrow()
    {
        TestTabPanel panel = new();
        Border firstHeader = new() { Width = 65, Height = 20 };
        Border secondHeader = new() { Width = 35, Height = 20 };
        Border thirdHeader = new() { Width = 60, Height = 20 };
        panel.Children.Add(firstHeader);
        panel.Children.Add(secondHeader);
        panel.Children.Add(thirdHeader);

        panel.MeasureForTest(new Size(113, 100));

        panel.ArrangeForTest(new Size(76, 60));

        LayoutInformation.GetLayoutSlot(firstHeader).Y.Should().Be(0);
        LayoutInformation.GetLayoutSlot(secondHeader).Y.Should().Be(20);
        LayoutInformation.GetLayoutSlot(thirdHeader).Y.Should().Be(40);
    }

    private sealed class TestTabPanel : TabPanel
    {
        public Size MeasureForTest(Size constraint) => MeasureOverride(constraint);

        public Size ArrangeForTest(Size arrangeSize) => ArrangeOverride(arrangeSize);
    }
}
