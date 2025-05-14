// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Text;

namespace System.Windows.Media.Generated;

/// <summary>
/// TODO: Add ITypeDescriptorContext-related tests
/// </summary>
public sealed class BrushConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(KnownColor))]
    [InlineData(false, typeof(ColorContext))]
    [InlineData(false, typeof(Color))]
    [InlineData(false, typeof(Brush))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        BrushConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(InstanceDescriptor))]
    [InlineData(false, typeof(KnownColor))]
    [InlineData(false, typeof(ColorContext))]
    [InlineData(false, typeof(Color))]
    [InlineData(false, typeof(Brush))]
    public void CanConvertTo_ReturnsExpected(bool expected, Type destinationType)
    {
        BrushConverter converter = new();

        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    [MemberData(nameof(ConvertFrom_NamedBrush_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_NamedBrush_ReturnsExpectedBrush(SolidColorBrush expectedColor, string colorName)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(colorName);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_NamedBrush_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { Brushes.Red, "Red" };
            yield return new object[] { Brushes.Blue, "Blue" };
            yield return new object[] { Brushes.Green, "Green" };
            yield return new object[] { Brushes.Orange, "Orange" };
            yield return new object[] { Brushes.Yellow, "Yellow" };

            yield return new object[] { Brushes.Black, "Black" };
            yield return new object[] { Brushes.White, "White" };
            yield return new object[] { Brushes.Gray, "Gray" };
            yield return new object[] { Brushes.DarkGray, "DarkGray" };
            yield return new object[] { Brushes.LightGray, "LightGray" };

            yield return new object[] { Brushes.Purple, "Purple" };
            yield return new object[] { Brushes.Magenta, "Magenta" };
            yield return new object[] { Brushes.Pink, "Pink" };
            yield return new object[] { Brushes.Brown, "Brown" };
            yield return new object[] { Brushes.Cyan, "Cyan" };

            yield return new object[] { Brushes.Olive, "Olive" };
            yield return new object[] { Brushes.Navy, "Navy" };
            yield return new object[] { Brushes.Teal, "Teal" };
            yield return new object[] { Brushes.Maroon, "Maroon" };
            yield return new object[] { Brushes.Silver, "Silver" };

            yield return new object[] { Brushes.Gold, "Gold" };
            yield return new object[] { Brushes.Coral, "Coral" };
            yield return new object[] { Brushes.Indigo, "Indigo" };
            yield return new object[] { Brushes.Violet, "Violet" };
            yield return new object[] { Brushes.Crimson, "Crimson" };

            // Wrong casing            
            yield return new object[] { Brushes.Chartreuse, "chartreuse" };
            yield return new object[] { Brushes.Khaki, "khaki" };
            yield return new object[] { Brushes.Tomato, "tomato" };

            yield return new object[] { Brushes.LightBlue, "   LightBlue   " };
            yield return new object[] { Brushes.LightCoral, "  LightCoral" };
            yield return new object[] { Brushes.OldLace, "OldLace      " };
        }
    }

    [MemberData(nameof(ConvertFrom_ContextColor_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_ContextColor_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_ContextColor_ReturnsExpectedBrush_Data
    {
        get
        {
            // As we don't pack our color profiles, this is probably the only way
            string? homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");

            if (homeDrive is null)
                Assert.Fail("%HOMEDRIVE% environment variable not present");

            yield return new object[] { new SolidColorBrush(Color.FromAValues(1.0f, [0.0f, 0.5f, 1.0f, 1.0f], new Uri($"file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm"))),
                                        $"ContextColor file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm 1.0, 0.0, 0.5, 1.0, 1.0" };

            yield return new object[] { new SolidColorBrush(Color.FromAValues(1.0f, [0.5f, 0.5f, 1.0f, 1.0f], new Uri($"file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm"))),
                                        $"ContextColor file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm 1.0,0.5,0.5,1.0,1.0" };

            yield return new object[] { new SolidColorBrush(Color.FromAValues(1.0f, [0.5f, 0.5f, 1.0f, 0.7f], new Uri($"file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm"))),
                                        $"ContextColor file://{homeDrive}/Windows/system32/spool/drivers/color/RSWOP.icm 1.0,   0.5,    0.5,   1.0,    0.7" };
        }
    }

    [MemberData(nameof(ConvertFrom_ScRGB_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_ScRGB_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_ScRGB_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 1f, 0f, 0f)), "sc#1,1,0,0" }; // Fully opaque red
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 0f, 1f, 0f)), "sc#1,0,1,0" }; // Fully opaque green
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 0f, 0f, 1f)), "sc#1,0,0,1" }; // Fully opaque blue
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0.5f, 1f, 1f, 0f)), "sc#0.5,1,1,0" }; // Semi-transparent yellow
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0f, 0f, 0f, 0f)), "sc#0,0,0,0" }; // Fully transparent black
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 0.5f, 0.5f, 0.5f)), "sc#1,0.5,0.5,0.5" }; // Fully opaque gray
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0.75f, 0.5f, 0f, 0.5f)), "sc#0.75,0.5,0,0.5" }; // Semi-transparent purple

            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 1f, 0f, 0f)), "sc#1, 1, 0, 0" }; // Extra space after commas
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 1f, 0f, 0f)), " sc#1,1,0,0" }; // Leading space
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 1f, 0f, 0f)), "sc#1,1,0,0 " }; // Trailing space
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 1f, 0f, 0f)), "   sc#1,1,0,0   " }; // Excessive surrounding whitespace
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0.9f, 0.8f, 0.7f, 0.6f)), "sc#0.9,0.8,0.7,0.6" }; // Non-integer values
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(1f, 0.12345f, 0.6789f, 0.54321f)), "sc#1,0.12345,0.6789,0.54321" }; // Values with extended precision
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0f, 1f, 0f, 0f)), "sc#0 ,1 ,0 ,0" }; // Spaces directly around commas
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0.5f, 0.5f, 0.5f, 0.5f)), "sc# .5 , .5 , .5 , .5" }; // Leading decimals with spaces
            yield return new object[] { new SolidColorBrush(Color.FromScRgb(0.75f, 0.5f, 0f, 0.5f)), "sc#0.75,0.50,0.00,0.50" }; // Explicit zero padding

        }
    }

    [MemberData(nameof(ConvertFrom_RGB_Short_HexColor_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_RGB_Short_HexColor_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_RGB_Short_HexColor_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00)), "#000" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)), "#FfF" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x11, 0x22, 0x33)), "#123" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x66)), "#456" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x77, 0x88, 0x99)), "#789" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xAA, 0xBB, 0xCC)), "#ABC" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xDD, 0xEE, 0xFF)), "#DEF" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x11, 0x33, 0x55)), "#135" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x22, 0x44, 0x66)), "#246" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x33, 0x55, 0x77)), "#357" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x44, 0x66, 0x88)), "#468" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x55, 0x77, 0x99)), "#579" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x66, 0x88, 0xAA)), "#68A" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x77, 0x99, 0xBB)), "#79B" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x88, 0xAA, 0xCC)), "#8AC" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x99, 0xBB, 0xDD)), "#9BD" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xCC, 0xBB, 0xAA)), "#CBA" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xFF, 0xEE, 0xDD)), "   #FED" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xAA)), "#89A   " };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x00, 0x77, 0xFF)), "  #07F  " };
        }
    }

    [MemberData(nameof(ConvertFrom_ARGB_Short_HexColor_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_ARGB_Short_HexColor_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_ARGB_Short_HexColor_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0x22, 0x55)), "#AF25" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x11, 0x22, 0x33, 0x44)), "#1234" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF)), "#F00F" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xCC, 0x33, 0x66, 0xFF)), "#C36f" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x77, 0x88, 0x99, 0xAA)), "#789A" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x77, 0x33, 0x66, 0x22)), "   #7362" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x66, 0x88, 0x99, 0x33)), "#6893   " };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xEE, 0xDD, 0xCC, 0xBB)), "  #EDCB  " };
        }
    }

    [MemberData(nameof(ConvertFrom_RGB_Long_HexColor_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_RGB_Long_HexColor_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_RGB_Long_HexColor_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x12, 0x34, 0x56)), "#123456" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xAA, 0xBB, 0xCC)), "#AABBCC" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0x00)), "#FF0000" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00)), "#00FF00" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0xFF)), "#0000FF" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0x00)), "#FFFF00" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0xFF)), "#00FFFF" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xFF, 0x00, 0xFF)), "#FF00FF" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)), "#C0C0C0" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)), "#808080" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x12, 0x12, 0x12)), "#121212" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x34, 0x56, 0x78)), "#345678" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x90, 0xAB, 0xCD)), "#90ABCD" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xDE, 0xAD, 0xBE)), "#DEADBE" };

            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xEF, 0xBE, 0xAD)), "#eFBEAD" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x12, 0x34, 0xFF)), "#1234ff" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x56, 0x78, 0x9A)), "#56789a" };

            yield return new object[] { new SolidColorBrush(Color.FromRgb(0xBC, 0xDE, 0xF0)), "   #BCDEF0" };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x01, 0x23, 0x45)), "  #012345   " };
            yield return new object[] { new SolidColorBrush(Color.FromRgb(0x67, 0x89, 0xAB)), "#6789AB   " };
        }
    }

    [MemberData(nameof(ConvertFrom_ARGB_Long_HexColor_ReturnsExpectedBrush_Data))]
    [Theory]
    public void ConvertFrom_ARGB_Long_HexColor_ReturnsExpectedBrush(SolidColorBrush expectedColor, string hexColor)
    {
        BrushConverter converter = new();
        object? result = converter.ConvertFrom(hexColor);

        // We serialize here back to string as SolidColorBrush doesn't override Equals nor has IEquatable<T>
        Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expectedColor.ToString(), ((SolidColorBrush)result).ToString());
    }

    public static IEnumerable<object[]> ConvertFrom_ARGB_Long_HexColor_ReturnsExpectedBrush_Data
    {
        get
        {
            yield return new object[] { Brushes.Red, $"#{KnownColor.Red:X}" };
            yield return new object[] { Brushes.Blue, $"#{KnownColor.Blue:X}" };
            yield return new object[] { Brushes.Green, $"#{KnownColor.Green:X}" };
            yield return new object[] { Brushes.Orange, $"#{KnownColor.Orange:X}" };
            yield return new object[] { Brushes.Yellow, $"#{KnownColor.Yellow:X}" };

            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x25, 0x37, 0x64, 0x88)), "#25376488" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xAF, 0x25, 0x46, 0xCC)), "#AF2546CC" };

            // Malformed bu acceptable
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x44, 0x37, 0x64, 0x88)), "   #44376488" };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x35, 0x25, 0x46, 0xCC)), "   #352546CC    " };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0x25, 0x37, 0x14, 0x25)), "  #25371425   " };
            yield return new object[] { new SolidColorBrush(Color.FromArgb(0xAF, 0x25, 0x46, 0x22)), "#AF254622   " };
        }
    }

    // ConvertFrom checks for non-strings, strings are evaluated by Parsers
    [InlineData("#FF00000000")]
    [InlineData("Not A Color")]
    [InlineData("#FF00 00FF")]
    [InlineData("NotAColor")]
    [InlineData("#FF000FF")]
    // Wrong casing on ScRgb (sc#)
    [InlineData("SC#1,0,1,0")]
    [InlineData("Sc#1,1,0,1")]
    // RGB; ARGB; RRGGBB; AARRGGBB but not 5
    [InlineData("#F12F1")]
    [InlineData("  # ")]
    [InlineData("  ")]
    [InlineData("# ")]
    [InlineData("#")]
    [InlineData("")]
    [Theory]
    public void ConvertFrom_InvalidColor_ThrowsFormatException(string color)
    {
        BrushConverter converter = new();

        Assert.Throws<FormatException>(() => converter.ConvertFrom(color));
    }

    // ConvertFrom checks for non-strings, strings are evaluated by Parsers
    [InlineData(typeof(SolidColorBrush))]
    [InlineData(0xFF00_FF00)]
    [InlineData(96.0d)]
    [InlineData(72.0f)]
    [Theory]
    public void ConvertFrom_InvalidColor_ThrowsNotSupportedException(object notString)
    {
        BrushConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(notString));
    }

    [Fact]
    public void ConvertFrom_NULL_ThrowsNotSupportedException()
    {
        BrushConverter converter = new();

        // TODO: Remove suppression once nullable annotations are done
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null!));
    }
}
