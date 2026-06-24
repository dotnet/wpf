// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Windows.Media;

public sealed class FormattedTextTests
{
    private static FormattedText CreateFormattedText(string text = "Test")
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            12.0,
            System.Windows.Media.Brushes.Black,
            96.0);
    }

    [Fact]
    public void LineHeight_SetNaN_DoesNotThrow()
    {
        FormattedText ft = CreateFormattedText();

        ft.LineHeight = double.NaN;
    }

    [Fact]
    public void LineHeight_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        FormattedText ft = CreateFormattedText();

        Assert.Throws<ArgumentOutOfRangeException>(() => ft.LineHeight = -1.0);
    }

    [Fact]
    public void LineHeight_SetZero_DoesNotThrow()
    {
        FormattedText ft = CreateFormattedText();

        ft.LineHeight = 0.0;
    }

    [Fact]
    public void MaxTextWidth_SetNaN_DoesNotThrow()
    {
        FormattedText ft = CreateFormattedText();

        ft.MaxTextWidth = double.NaN;
    }

    [Fact]
    public void MaxTextWidth_SetNegative_ThrowsArgumentOutOfRangeException()
    {
        FormattedText ft = CreateFormattedText();

        Assert.Throws<ArgumentOutOfRangeException>(() => ft.MaxTextWidth = -1.0);
    }

    [Fact]
    public void MaxTextWidth_SetZero_DoesNotThrow()
    {
        FormattedText ft = CreateFormattedText();

        ft.MaxTextWidth = 0.0;
    }
}
