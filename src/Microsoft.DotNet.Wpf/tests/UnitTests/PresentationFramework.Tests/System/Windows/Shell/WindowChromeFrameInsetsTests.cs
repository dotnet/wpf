// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Shell;

public sealed class WindowChromeFrameInsetsTests
{
    [Fact]
    public void ComputeNormalFrameInsets_96Dpi_SplitsHorizontalAndKeepsVerticalAtBottom()
    {
        // 7 px invisible border left/right/bottom, none on top.
        Int32Rect insets = WindowChromeWorker.ComputeNormalFrameInsets(814, 607, 800, 600, 1.0);

        insets.X.Should().Be(7);        // left
        insets.Y.Should().Be(0);        // top
        insets.Width.Should().Be(7);    // right
        insets.Height.Should().Be(7);   // bottom
    }

    [Fact]
    public void ComputeNormalFrameInsets_150Percent_UsesScaledVisibleSize()
    {
        Int32Rect insets = WindowChromeWorker.ComputeNormalFrameInsets(822, 611, 800, 600, 1.0);

        insets.X.Should().Be(11);
        insets.Y.Should().Be(0);
        insets.Width.Should().Be(11);
        insets.Height.Should().Be(11);
    }

    [Fact]
    public void ComputeNormalFrameInsets_VisibleLargerThanWindow_ClampsToZero()
    {
        Int32Rect insets = WindowChromeWorker.ComputeNormalFrameInsets(800, 600, 900, 700, 1.0);

        insets.Should().Be(new Int32Rect(0, 0, 0, 0));
    }

    [Fact]
    public void ComputeNormalFrameInsets_VirtualizedWindow_AppliesScale()
    {
        // System-DPI-aware process on a 150% monitor: DWM reports physical pixels (1200x900) while the window
        // rect is virtualized (814x607); the scale 96/144 brings the visible size back to 800x600.
        Int32Rect insets = WindowChromeWorker.ComputeNormalFrameInsets(814, 607, 1200, 900, 96.0 / 144.0);

        insets.X.Should().Be(7);
        insets.Height.Should().Be(7);
    }

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

    [Theory]
    [InlineData(8, 12, 11)]   // 100% -> 150%: frame 4+4 -> 6+6, the 1 px visible border does not scale
    [InlineData(8, 16, 15)]   // 100% -> 200%
    [InlineData(8, 10, 9)]    // 100% -> 125%
    [InlineData(12, 8, 3)]    // 150% -> 100%: the delta is subtracted
    public void RescaleFrameInsets_FollowsTheResizeFrameMetricDelta(int oldFrame, int newFrame, int expected)
    {
        Int32Rect insets = WindowChromeWorker.RescaleFrameInsets(new Int32Rect(7, 0, 7, 7), oldFrame, newFrame);

        insets.Should().Be(new Int32Rect(expected, 0, expected, expected));
    }

    [Fact]
    public void RescaleFrameInsets_KeepsZeroSidesAtZero()
    {
        // A window whose top has no invisible border must not grow one when the DPI increases.
        Int32Rect insets = WindowChromeWorker.RescaleFrameInsets(new Int32Rect(7, 0, 7, 7), 8, 16);

        insets.Y.Should().Be(0);
    }

    [Fact]
    public void RescaleFrameInsets_NeverGoesNegative()
    {
        Int32Rect insets = WindowChromeWorker.RescaleFrameInsets(new Int32Rect(3, 0, 3, 3), 16, 8);

        insets.Should().Be(new Int32Rect(0, 0, 0, 0));
    }
}
