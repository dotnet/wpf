// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows.Media.Animation;

public sealed class KeySplineConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(InstanceDescriptor))]
    [InlineData(false, typeof(KeySpline))]
    [InlineData(false, typeof(int))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        KeySplineConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    // Valid types
    [InlineData(true, typeof(string))]
    [InlineData(true, typeof(InstanceDescriptor))]
    // Invalid types
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(KeySpline))]
    [InlineData(false, typeof(int))]
    [InlineData(false, typeof(long))]
    public void CanConvertTo_ReturnsExpected(bool expected, Type destinationType)
    {
        KeySplineConverter converter = new();

        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ValidValues_ReturnsExpected_Data))]
    public void ConvertFrom_ValidValues_ReturnsExpected(string input, double x1, double y1, double x2, double y2, CultureInfo culture)
    {
        KeySplineConverter converter = new();

        KeySpline? result = (KeySpline?)converter.ConvertFrom(null, culture, input);
        Assert.NotNull(result);

        Assert.Equal(x1, result.ControlPoint1.X);
        Assert.Equal(y1, result.ControlPoint1.Y);
        Assert.Equal(x2, result.ControlPoint2.X);
        Assert.Equal(y2, result.ControlPoint2.Y);
    }

    public static IEnumerable<object[]> ConvertFrom_ValidValues_ReturnsExpected_Data
    {
        get
        {
            yield return new object[] { "0.25,0.1,0.25,1", 0.25, 0.1, 0.25, 1.0, CultureInfo.InvariantCulture };
            yield return new object[] { "0,25    ;0,1;0,25;1", 0.25, 0.1, 0.25, 1.0, new CultureInfo("fr-FR") };
            yield return new object[] { "     0,25;0,1     ;0,25;1", 0.25, 0.1, 0.25, 1.0, new CultureInfo("de-DE") };
            yield return new object[] { "0.25,0.1,0.25,1", 0.25, 0.1, 0.25, 1.0, new CultureInfo("en-US") };
            yield return new object[] { "0,25;      0,1;0,25;1    ", 0.25, 0.1, 0.25, 1.0, new CultureInfo("es-ES") };
            yield return new object[] { "0.5,0.75,0.25,0.9", 0.5, 0.75, 0.25, 0.9, CultureInfo.InvariantCulture };
            yield return new object[] { "0,5;0,75;0,25;        0,9", 0.5, 0.75, 0.25, 0.9, new CultureInfo("fr-FR") };
            yield return new object[] { "1,0,0,1", 1.0, 0.0, 0.0, 1.0, CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_KeySpline_ThrowsArgumentException_Data))]
    public void ConvertFrom_KeySpline_ThrowsArgumentException(string input, CultureInfo cultureInfo)
    {
        KeySplineConverter converter = new();

        // This throws in KeySpline actually as X values cannot be over 1.0
        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(null, cultureInfo, input));
    }

    public static IEnumerable<object[]> ConvertFrom_KeySpline_ThrowsArgumentException_Data
    {
        get
        {
            yield return new object[] { "0.3, 0.4, 77771.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "          1.1, 0.4, 0.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "040.3, 881.2, 0.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "8,3; 0,4; 0,6; 1,8", new CultureInfo("fr-FR") };
            yield return new object[] { "1.1, 1.2, 0.6, 0.7             ", CultureInfo.InvariantCulture };
            yield return new object[] { "1110.3, 1.4, 1.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "8888.9, 0.4, 0.6, 1.2", CultureInfo.InvariantCulture };
            yield return new object[] { "0.3,         0.4, 1.7776, 1.2", CultureInfo.InvariantCulture };
            yield return new object[] { "1.3, 0.4, 0.66666, 1.2", CultureInfo.InvariantCulture };
            yield return new object[] { "0.3, 1.8, 40.6, 1.2", CultureInfo.InvariantCulture };
            yield return new object[] { "1.3, 1.8, 0.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "90.3, 1.8, 0.6, 72.2", CultureInfo.InvariantCulture };
            yield return new object[] { "1.3,             0.4, 1.6, 25", CultureInfo.InvariantCulture };
            yield return new object[] { "2221.3, 1.4, 1.6, 2.2", CultureInfo.InvariantCulture };
            yield return new object[] { "0.3, 1.4, 110.6, 2.2", CultureInfo.InvariantCulture };
            yield return new object[] { "1.1, 0.4, 1.6, 2.2", CultureInfo.InvariantCulture };
            yield return new object[] { "        0.3, 1.8,       1.6,     0.7", new CultureInfo("de-DE") };
            yield return new object[] { "1.3, 1.4, 0.6, 0.7", CultureInfo.InvariantCulture };
            yield return new object[] { "0.3, 0.4, 1.6, 2.7", CultureInfo.InvariantCulture };
            yield return new object[] { "1.3, 0.4, 1.6,             2.7", CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0.1, invalid")]
    [InlineData("0.1,invalid,0.3")]
    [InlineData("0.1,    0.2,       0.3, invalid")]
    [InlineData("0.1,    0.2,   invalid,    0.3, 0.7 ")]
    public void ConvertFrom_DoubleParse_ThrowsFormatException(string input)
    {
        KeySplineConverter converter = new();

        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0.1")]
    [InlineData("0.1,0.2")]
    [InlineData("0.1,0.2,0.3")]
    [InlineData("0.7, 0.5, 0.3")]
    [InlineData("     0.1,      0.2")]
    [InlineData("0.1,    0.2,       0.3")]
    public void ConvertFrom_Tokenizer_ThrowsInvalidOperationException(string input)
    {
        KeySplineConverter converter = new();

        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, input));
    }

    [Fact]
    public void ConvertFrom_NULL_ThrowsNotSupportedException()
    {
        KeySplineConverter converter = new();

        // TODO: Remove suppression once nullable annotations are done
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, null!));
    }

    [MemberData(nameof(ConvertTo_String_ReturnsExpected_Data))]
    [Theory]
    public void ConvertTo_String_ReturnsExpected(KeySpline keySpline, CultureInfo culture, string expected)
    {
        KeySplineConverter converter = new();

        Assert.Equal(expected, converter.ConvertTo(null, culture, keySpline, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_String_ReturnsExpected_Data
    {
        get
        {
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.25, 0.1), ControlPoint2 = new Point(0.25, 1.0) },
                CultureInfo.InvariantCulture, "0.25,0.1,0.25,1"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.5, 0.75), ControlPoint2 = new Point(0.25, 0.9) },
                CultureInfo.InvariantCulture, "0.5,0.75,0.25,0.9"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(1.0, 0.0), ControlPoint2 = new Point(0.0, 1.0) },
                new CultureInfo("en-US"), "1,0,0,1"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(1.0, 0.0), ControlPoint2 = new Point(0.0, 1.0) },
                new CultureInfo("fr-FR"), "1;0;0;1"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.3, 0.2), ControlPoint2 = new Point(0.4, 0.8) },
                new CultureInfo("de-DE"), "0,3;0,2;0,4;0,8"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.12, 0.34), ControlPoint2 = new Point(0.56, 0.78) },
                CultureInfo.InvariantCulture, "0.12,0.34,0.56,0.78"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.9, 0.1), ControlPoint2 = new Point(0.3, 0.7) },
                CultureInfo.InvariantCulture,  "0.9,0.1,0.3,0.7"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.0, 0.5), ControlPoint2 = new Point(1.0, 0.5) },
                CultureInfo.InvariantCulture, "0,0.5,1,0.5"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.15, 0.35), ControlPoint2 = new Point(0.85, 0.95) },
                CultureInfo.InvariantCulture, "0.15,0.35,0.85,0.95"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.6, 0.4), ControlPoint2 = new Point(0.2, 0.8) },
                new CultureInfo("en-US"), "0.6,0.4,0.2,0.8"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.33, 0.67), ControlPoint2 = new Point(0.25, 0.75) },
                CultureInfo.InvariantCulture, "0.33,0.67,0.25,0.75"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.2, 0.8), ControlPoint2 = new Point(0.4, 0.6) },
                new CultureInfo("fr-FR"), "0,2;0,8;0,4;0,6"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.75, 0.25), ControlPoint2 = new Point(0.5, 0.5) },
                new CultureInfo("de-DE"), "0,75;0,25;0,5;0,5"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.1, 0.9), ControlPoint2 = new Point(0.9, 0.1) },
                CultureInfo.InvariantCulture, "0.1,0.9,0.9,0.1"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.05, 0.95), ControlPoint2 = new Point(0.95, 100.05) },
                CultureInfo.InvariantCulture, "0.05,0.95,0.95,100.05"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.4, 0.4), ControlPoint2 = new Point(0.6, 0.6) },
                new CultureInfo("en-US"), "0.4,0.4,0.6,0.6"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.8, 0.2), ControlPoint2 = new Point(0.3, 0.7) },
                new CultureInfo("fr-FR"), "0,8;0,2;0,3;0,7"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.55, 0.45), ControlPoint2 = new Point(0.35, 0.65) },
                new CultureInfo("de-DE"), "0,55;0,45;0,35;0,65"
            };
            yield return new object[]
            {
                new KeySpline { ControlPoint1 = new Point(0.99, 0.01), ControlPoint2 = new Point(0.5, 0.5) },
                CultureInfo.InvariantCulture, "0.99,0.01,0.5,0.5"
            };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_StringInput_ReturnsString_Data))]
    public void ConvertTo_ObjectInput_ReturnsStringRepresentation(object input, Type destinationType, CultureInfo culture)
    {
        KeySplineConverter converter = new();

        Assert.Equal(input.ToString(), converter.ConvertTo(null, culture, input, destinationType));
    }

    public static IEnumerable<object[]> ConvertTo_StringInput_ReturnsString_Data
    {
        get
        {
            yield return new object[] { string.Empty, typeof(string), CultureInfo.InvariantCulture };

            // This is how base calls work, fun
            yield return new object[] { Colors.Red, typeof(string), CultureInfo.InvariantCulture };
            yield return new object[] { Brushes.Purple, typeof(string), CultureInfo.CurrentCulture };
            yield return new object[] { "This is given back", typeof(string), CultureInfo.InvariantCulture };
            yield return new object[] { "   This too ", typeof(string), CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidData_ThrowsNotSupportedException_Data))]
    public void ConvertTo_InvalidData_ThrowsNotSupportedException(object? input, Type? destinationType, CultureInfo? culture)
    {
        KeySplineConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, culture, input, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_InvalidData_ThrowsNotSupportedException_Data
    {
        get
        {
            yield return new object[] { new KeySpline(0.1, 0.2, 0.3, 0.4), typeof(int), CultureInfo.CurrentCulture };
            yield return new object[] { new KeySpline(0.5, 0.6, 0.7, 0.8), typeof(double), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.25, 0.25, 0.75, 0.75), typeof(object), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(1.0, 0.0, 0.0, 1.0), typeof(bool), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.33, 0.66, 0.66, 0.33), typeof(DateTime), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.5, 0.5, 0.5, 0.5), typeof(Guid), CultureInfo.CurrentCulture };
            yield return new object[] { new KeySpline(0.1, 0.2, 0.3, 0.4), typeof(Uri), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.6, 0.7, 0.8, 0.9), typeof(Array), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.2, 0.3, 0.4, 0.5), typeof(TimeSpan), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.7, 0.8, 0.9, 1.0), typeof(Enum), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.1, 0.2, 0.3, 0.4), typeof(Point), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.1, 0.1, 0.9, 0.9), typeof(Color), CultureInfo.CurrentCulture };
            yield return new object[] { new KeySpline(0.2, 0.4, 0.6, 0.8), typeof(KeySpline), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.2, 0.3, 0.4, 0.5), typeof(byte[]), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.0, 0.1, 0.2, 0.3), typeof(Dictionary<int, string>), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.1, 0.2, 0.3, 0.4), typeof(List<int>), CultureInfo.InvariantCulture };
            yield return new object[] { new KeySpline(0.4, 0.3, 0.2, 0.1), typeof(Stack<int>), CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidDestinationType_ThrowsArgumentNullException_Data))]
    public void ConvertTo_InvalidDestinationType_ThrowsArgumentNullException(KeySpline? input, Type? destinationType, CultureInfo? culture)
    {
        KeySplineConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, culture, input, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_InvalidDestinationType_ThrowsArgumentNullException_Data
    {
        get
        {
            yield return new object?[] { null, null, CultureInfo.InvariantCulture };
            yield return new object?[] { new KeySpline(0.0, 0.0, 1.0, 1.0), null, CultureInfo.InvariantCulture };
        }
    }
}
