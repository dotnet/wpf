// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

namespace System.Windows.Shell;

public sealed class CaptionTextColorHelperTests
{
    [Theory]
    [InlineData(0x00, 0x00, 0x00, true)]      // black
    [InlineData(0xFF, 0xFF, 0xFF, false)]     // white
    [InlineData(0x2B, 0x2B, 0x2B, true)]      // default dark caption
    [InlineData(0x00, 0x78, 0xD7, true)]      // Windows 10 default accent
    [InlineData(0x80, 0x80, 0x80, true)]      // 2*128 + 5*128 + 128 = 1024, boundary is dark
    [InlineData(0x80, 0x80, 0x81, false)]     // 1025 is light
    public void IsDark_UsesWeightedLuminanceThreshold(byte r, byte g, byte b, bool expected)
    {
        CaptionTextColorHelper.IsDark(Color.FromRgb(r, g, b)).Should().Be(expected);
    }

    [Fact]
    public void GetActiveCaptionText_DarkBackground_ReturnsWhite()
    {
        CaptionTextColorHelper.GetActiveCaptionText(Color.FromRgb(0x2B, 0x2B, 0x2B), darkMode: false).Should().Be(Colors.White);
    }

    [Fact]
    public void GetActiveCaptionText_LightBackground_ReturnsBlack()
    {
        CaptionTextColorHelper.GetActiveCaptionText(Colors.White, darkMode: true).Should().Be(Colors.Black);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetActiveCaptionText_NoBackground_FollowsDarkMode(bool darkMode)
    {
        CaptionTextColorHelper.GetActiveCaptionText(null, darkMode).Should().Be(darkMode ? Colors.White : Colors.Black);
    }

    [Fact]
    public void GetInactiveCaptionText_DarkMode_BlendsWhiteTowardDefaultDarkBackground()
    {
        Color expected = CaptionTextColorHelper.Blend(Colors.White, Color.FromRgb(0x2B, 0x2B, 0x2B), 0.4);

        CaptionTextColorHelper.GetInactiveCaptionText(null, darkMode: true).Should().Be(expected);
    }

    [Fact]
    public void GetInactiveCaptionText_LightMode_BlendsBlackTowardWhite()
    {
        Color expected = CaptionTextColorHelper.Blend(Colors.Black, Colors.White, 0.6);

        CaptionTextColorHelper.GetInactiveCaptionText(null, darkMode: false).Should().Be(expected);
    }

    [Fact]
    public void Blend_Endpoints_ReturnInputs()
    {
        Color a = Color.FromArgb(0x80, 0x10, 0x20, 0x30);
        Color b = Color.FromArgb(0xFF, 0xF0, 0xE0, 0xD0);

        CaptionTextColorHelper.Blend(a, b, 0.0).Should().Be(a);
        CaptionTextColorHelper.Blend(a, b, 1.0).Should().Be(b);
    }

    [Fact]
    public void Blend_MidPoint_AveragesEveryChannelIncludingAlpha()
    {
        Color a = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
        Color b = Color.FromArgb(0xFE, 0xFE, 0xFE, 0xFE);

        CaptionTextColorHelper.Blend(a, b, 0.5).Should().Be(Color.FromArgb(0x7F, 0x7F, 0x7F, 0x7F));
    }
}
