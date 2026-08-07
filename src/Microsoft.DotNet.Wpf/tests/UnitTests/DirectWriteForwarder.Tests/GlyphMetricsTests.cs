// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="GlyphMetrics"/> value struct.
/// </summary>
public class GlyphMetricsTests
{
    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        var metrics = new GlyphMetrics();

        metrics.LeftSideBearing.Should().Be(0);
        metrics.AdvanceWidth.Should().Be(0);
        metrics.RightSideBearing.Should().Be(0);
        metrics.TopSideBearing.Should().Be(0);
        metrics.AdvanceHeight.Should().Be(0);
        metrics.BottomSideBearing.Should().Be(0);
        metrics.VerticalOriginY.Should().Be(0);
    }

    [Fact]
    public void AllFields_CanBeSetAndRetrieved()
    {
        var metrics = new GlyphMetrics
        {
            LeftSideBearing = -10,
            AdvanceWidth = 500,
            RightSideBearing = 20,
            TopSideBearing = 50,
            AdvanceHeight = 1000,
            BottomSideBearing = -30,
            VerticalOriginY = 800
        };

        metrics.LeftSideBearing.Should().Be(-10);
        metrics.AdvanceWidth.Should().Be(500);
        metrics.RightSideBearing.Should().Be(20);
        metrics.TopSideBearing.Should().Be(50);
        metrics.AdvanceHeight.Should().Be(1000);
        metrics.BottomSideBearing.Should().Be(-30);
        metrics.VerticalOriginY.Should().Be(800);
    }

    [Fact]
    public void NegativeLeftSideBearing_IndicatesOverhangToLeft()
    {
        // A negative LeftSideBearing means the black box extends to the left of the origin
        // (often true for lowercase italic 'f')
        var metrics = new GlyphMetrics
        {
            LeftSideBearing = -50,
            AdvanceWidth = 400
        };

        metrics.LeftSideBearing.Should().BeNegative();
    }

    [Fact]
    public void NegativeRightSideBearing_IndicatesOverhangToRight()
    {
        // A negative RightSideBearing means the right edge of the black box 
        // overhangs the layout box
        var metrics = new GlyphMetrics
        {
            RightSideBearing = -25,
            AdvanceWidth = 500
        };

        metrics.RightSideBearing.Should().BeNegative();
    }

    [Theory]
    [InlineData(-100, 600, 50)]
    [InlineData(0, 500, 0)]
    [InlineData(25, 450, -20)]
    public void HorizontalMetrics_VariousValues(int leftBearing, uint advanceWidth, int rightBearing)
    {
        var metrics = new GlyphMetrics
        {
            LeftSideBearing = leftBearing,
            AdvanceWidth = advanceWidth,
            RightSideBearing = rightBearing
        };

        metrics.LeftSideBearing.Should().Be(leftBearing);
        metrics.AdvanceWidth.Should().Be(advanceWidth);
        metrics.RightSideBearing.Should().Be(rightBearing);
    }

    [Theory]
    [InlineData(100, 1200, -50, 900)]
    [InlineData(0, 1000, 0, 800)]
    [InlineData(-20, 1100, 30, 850)]
    public void VerticalMetrics_VariousValues(int topBearing, uint advanceHeight, int bottomBearing, int verticalOriginY)
    {
        var metrics = new GlyphMetrics
        {
            TopSideBearing = topBearing,
            AdvanceHeight = advanceHeight,
            BottomSideBearing = bottomBearing,
            VerticalOriginY = verticalOriginY
        };

        metrics.TopSideBearing.Should().Be(topBearing);
        metrics.AdvanceHeight.Should().Be(advanceHeight);
        metrics.BottomSideBearing.Should().Be(bottomBearing);
        metrics.VerticalOriginY.Should().Be(verticalOriginY);
    }
}
