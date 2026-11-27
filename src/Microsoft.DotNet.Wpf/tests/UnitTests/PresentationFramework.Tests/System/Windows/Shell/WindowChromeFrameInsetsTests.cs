// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Shell;

public sealed class WindowChromeFrameInsetsTests
{
    [Fact]
    public void ComputeDwmToWindowScale_SameDpi_ReturnsOne()
    {
        WindowChromeWorker.ComputeDwmToWindowScale(814, 800, 96, 96).Should().Be(1.0);
    }

    [Fact]
    public void ComputeDwmToWindowScale_VirtualizedWindow_ReturnsDpiRatio()
    {
        // Window 814 wide in virtualized pixels, DWM reports 1200 physical pixels: measured ratio 0.678 agrees
        // with the DPI ratio 96/144 = 0.667.
        WindowChromeWorker.ComputeDwmToWindowScale(814, 1200, 96, 144).Should().BeApproximately(96.0 / 144.0, 0.0001);
    }

    [Fact]
    public void ComputeDwmToWindowScale_MeasuredRatioDisagrees_ReturnsOne()
    {
        // Per-monitor aware window: both rects are in the same space (ratio ~1) although the DPIs differ.
        WindowChromeWorker.ComputeDwmToWindowScale(814, 800, 96, 144).Should().Be(1.0);
    }

    [Theory]
    [InlineData(0, 800, 96, 96)]
    [InlineData(800, 0, 96, 96)]
    [InlineData(800, 800, 0, 96)]
    [InlineData(800, 800, 96, 0)]
    public void ComputeDwmToWindowScale_DegenerateInput_ReturnsOne(int windowWidth, int visibleWidth, double windowDpi, double monitorDpi)
    {
        WindowChromeWorker.ComputeDwmToWindowScale(windowWidth, visibleWidth, windowDpi, monitorDpi).Should().Be(1.0);
    }
}
