// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="FontMetrics"/> value struct.
/// </summary>
public class FontMetricsTests
{
    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        var metrics = new FontMetrics();

        metrics.DesignUnitsPerEm.Should().Be(0);
        metrics.Ascent.Should().Be(0);
        metrics.Descent.Should().Be(0);
        metrics.LineGap.Should().Be(0);
        metrics.CapHeight.Should().Be(0);
        metrics.XHeight.Should().Be(0);
        metrics.UnderlinePosition.Should().Be(0);
        metrics.UnderlineThickness.Should().Be(0);
        metrics.StrikethroughPosition.Should().Be(0);
        metrics.StrikethroughThickness.Should().Be(0);
    }

    [Fact]
    public void Baseline_ShouldCalculateCorrectly()
    {
        // Baseline = (Ascent + LineGap * 0.5) / DesignUnitsPerEm
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 1000,
            Ascent = 800,
            LineGap = 100
        };

        // Expected: (800 + 100 * 0.5) / 1000 = 850 / 1000 = 0.85
        metrics.Baseline.Should().BeApproximately(0.85, 0.0001);
    }

    [Fact]
    public void Baseline_WithNegativeLineGap_ShouldCalculateCorrectly()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 2048,
            Ascent = 1900,
            LineGap = -100
        };

        // Expected: (1900 + (-100) * 0.5) / 2048 = 1850 / 2048
        double expected = 1850.0 / 2048.0;
        metrics.Baseline.Should().BeApproximately(expected, 0.0001);
    }

    [Fact]
    public void LineSpacing_ShouldCalculateCorrectly()
    {
        // LineSpacing = (Ascent + Descent + LineGap) / DesignUnitsPerEm
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 1000,
            Ascent = 800,
            Descent = 200,
            LineGap = 100
        };

        // Expected: (800 + 200 + 100) / 1000 = 1100 / 1000 = 1.1
        metrics.LineSpacing.Should().BeApproximately(1.1, 0.0001);
    }

    [Fact]
    public void LineSpacing_WithZeroLineGap_ShouldCalculateCorrectly()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 2048,
            Ascent = 1900,
            Descent = 500,
            LineGap = 0
        };

        // Expected: (1900 + 500 + 0) / 2048 = 2400 / 2048
        double expected = 2400.0 / 2048.0;
        metrics.LineSpacing.Should().BeApproximately(expected, 0.0001);
    }

    [Fact]
    public void LineSpacing_WithNegativeLineGap_ShouldCalculateCorrectly()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 1000,
            Ascent = 800,
            Descent = 200,
            LineGap = -50
        };

        // Expected: (800 + 200 + (-50)) / 1000 = 950 / 1000 = 0.95
        metrics.LineSpacing.Should().BeApproximately(0.95, 0.0001);
    }

    [Theory]
    [InlineData((ushort)1000, (ushort)750, (ushort)250, (short)0)]
    [InlineData((ushort)2048, (ushort)1900, (ushort)500, (short)100)]
    [InlineData((ushort)1024, (ushort)800, (ushort)200, (short)-50)]
    public void FontMetrics_VariousValues_CalculationsAreConsistent(
        ushort designUnitsPerEm,
        ushort ascent,
        ushort descent,
        short lineGap)
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = designUnitsPerEm,
            Ascent = ascent,
            Descent = descent,
            LineGap = lineGap
        };

        double expectedBaseline = (ascent + lineGap * 0.5) / designUnitsPerEm;
        double expectedLineSpacing = (double)(ascent + descent + lineGap) / designUnitsPerEm;

        metrics.Baseline.Should().BeApproximately(expectedBaseline, 0.0001);
        metrics.LineSpacing.Should().BeApproximately(expectedLineSpacing, 0.0001);
    }

    [Fact]
    public void AllFields_CanBeSetAndRetrieved()
    {
        var metrics = new FontMetrics
        {
            DesignUnitsPerEm = 2048,
            Ascent = 1900,
            Descent = 500,
            LineGap = 100,
            CapHeight = 1400,
            XHeight = 1000,
            UnderlinePosition = -200,
            UnderlineThickness = 100,
            StrikethroughPosition = 600,
            StrikethroughThickness = 80
        };

        metrics.DesignUnitsPerEm.Should().Be(2048);
        metrics.Ascent.Should().Be(1900);
        metrics.Descent.Should().Be(500);
        metrics.LineGap.Should().Be(100);
        metrics.CapHeight.Should().Be(1400);
        metrics.XHeight.Should().Be(1000);
        metrics.UnderlinePosition.Should().Be(-200);
        metrics.UnderlineThickness.Should().Be(100);
        metrics.StrikethroughPosition.Should().Be(600);
        metrics.StrikethroughThickness.Should().Be(80);
    }
}
