// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows;

public class TextDecorationCollectionConverterTests
{
    [Theory]
    // Only valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(TextDecorationCollection))]
    [InlineData(false, typeof(IEnumerable<TextDecoration>))]
    [InlineData(false, typeof(InstanceDescriptor))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        TextDecorationCollectionConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    // Only valid type
    [InlineData(true, typeof(InstanceDescriptor))]
    // Invalid types
    [InlineData(false, typeof(TextDecorationCollection))]
    [InlineData(false, typeof(IEnumerable<TextDecoration>))]
    [InlineData(false, typeof(string))]
    public void CanConvertTo_ReturnsExpected(bool expected, Type destinationType)
    {
        TextDecorationCollectionConverter converter = new();

        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ReturnsExpected_Data))]
    public void ConvertFrom_ReturnsExpected(TextDecorationCollection expected, CultureInfo cultureInfo, string text)
    {
        TextDecorationCollectionConverter converter = new();

        TextDecorationCollection? converted = (TextDecorationCollection?)converter.ConvertFrom(null, cultureInfo, text);

        // Check count
        Assert.NotNull(converted);
        Assert.Equal(expected.Count, converted.Count);

        // We require the order to be exact as well
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], converted[i]);
        }
    }

    public static IEnumerable<object?[]> ConvertFrom_ReturnsExpected_Data
    {
        get
        {
            // "None" returns no items
            yield return new object[] { new TextDecorationCollection(), CultureInfo.InvariantCulture, string.Empty };
            yield return new object[] { new TextDecorationCollection(), CultureInfo.InvariantCulture, "          " };
            yield return new object[] { new TextDecorationCollection(), CultureInfo.InvariantCulture, "None" };
            yield return new object[] { new TextDecorationCollection(), CultureInfo.InvariantCulture, "      None     " };

            yield return new object[] { new TextDecorationCollection(), new CultureInfo("ru-RU"), string.Empty };
            yield return new object[] { new TextDecorationCollection(), new CultureInfo("no-NO"), "          " };
            yield return new object[] { new TextDecorationCollection(), new CultureInfo("no-NO"), "None" };
            yield return new object[] { new TextDecorationCollection(), new CultureInfo("ru-RU"), "      None     " };

            // Order matters here
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0]]), new CultureInfo("no-NO"), "Strikethrough" };
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0]]), new CultureInfo("ru-RU"), "Strikethrough           " };

            yield return new object[] { new TextDecorationCollection([TextDecorations.Underline[0], TextDecorations.Baseline[0]]),
                                        new CultureInfo("no-NO"),
                                        "Underline, Baseline" };
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0], TextDecorations.Underline[0], TextDecorations.Baseline[0]]),
                                        new CultureInfo("ru-RU"),
                                        "  Strikethrough   ,Underline, Baseline " };

            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0], TextDecorations.Underline[0],
                                                                      TextDecorations.Baseline[0], TextDecorations.OverLine[0]]),
                                                                      new CultureInfo("fr-FR"), "  Strikethrough   ,Underline, Baseline        , Overline             " };

        }
    }

    [Theory]
    // Starts with a separator
    [InlineData(",  Strikethrough   ,Underline, Baseline ")]
    // Ends with a separator
    [InlineData(" Strikethrough   ,Underline, Baseline, Overline, ")]
    // Duplicate item (must be unique)
    [InlineData("  Strikethrough  , Strikethrough, ,Underline, Baseline ")]
    [InlineData(" Underline, Underline ")]
    // None must be specified alone
    [InlineData("None,  Strikethrough   ,Underline, Baseline ")]
    [InlineData("None,  Strikethrough   ,Underline, Baseline, Overline ")]
    // Invalid decoration at the end
    [InlineData(" Strikethrough   ,Underline, Baseline, Overline, x ")]
    // Invalid decoration
    [InlineData(" Noneee ")]
    // Invalid data type
    [InlineData(double.PositiveInfinity)]
    [InlineData(1554554)]
    [InlineData(125.4d)]
    public void ConvertFrom_ThrowsArgumentException(object? source)
    {
        TextDecorationCollectionConverter converter = new();

        Assert.Throws<ArgumentException>(() => converter.ConvertFrom(null, null, source));
    }

    [Theory]
    [MemberData(nameof(ConvertFromString_ReturnsExpected_Data))]
    public void ConvertFromString_ReturnsExpected(TextDecorationCollection expected, string text)
    {
        TextDecorationCollection converted = TextDecorationCollectionConverter.ConvertFromString(text);

        // Check count
        Assert.Equal(expected.Count, converted.Count);

        // We require the order to be exact as well
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], converted[i]);
        }
    }

    public static IEnumerable<object?[]> ConvertFromString_ReturnsExpected_Data
    {
        get
        {
            // "None" returns no items
            yield return new object[] { new TextDecorationCollection(), string.Empty };
            yield return new object[] { new TextDecorationCollection(), "          " };
            yield return new object[] { new TextDecorationCollection(), "None" };
            yield return new object[] { new TextDecorationCollection(), "      None     " };

            // Order matters here
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0]]), "Strikethrough" };
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0]]), "Strikethrough           " };

            yield return new object[] { new TextDecorationCollection([TextDecorations.Underline[0], TextDecorations.Baseline[0]]), "Underline, Baseline" };
            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0], TextDecorations.Underline[0], TextDecorations.Baseline[0]]),
                                                                     "  Strikethrough   ,Underline, Baseline " };

            yield return new object[] { new TextDecorationCollection([TextDecorations.Strikethrough[0], TextDecorations.Underline[0],
                                                                      TextDecorations.Baseline[0], TextDecorations.OverLine[0]]),
                                                                     "  Strikethrough   ,Underline, Baseline        , Overline             " };

        }
    }

    [Fact]
    public void ConvertFromString_NullValue_ReturnsNull()
    {
        // null is simply null (NOTE: This differs from instance method ConvertFrom, that will throw on null value)
        TextDecorationCollection? converted = TextDecorationCollectionConverter.ConvertFromString(null);

        Assert.Null(converted);
    }

    [Theory]
    // Starts with a separator
    [InlineData(",  Strikethrough   ,Underline, Baseline ")]
    // Ends with a separator
    [InlineData(" Strikethrough   ,Underline, Baseline, Overline, ")]
    // Duplicate item (must be unique)
    [InlineData("  Strikethrough  , Strikethrough, ,Underline, Baseline ")]
    [InlineData(" Underline, Underline ")]
    // None must be specified alone
    [InlineData("None,  Strikethrough   ,Underline, Baseline ")]
    [InlineData("None,  Strikethrough   ,Underline, Baseline, Overline ")]
    // Invalid decoration at the end
    [InlineData(" Strikethrough   ,Underline, Baseline, Overline, x ")]
    // Invalid decoration
    [InlineData(" Noneee ")]
    public void ConvertFromString_ThrowsArgumentException(string text)
    {
        Assert.Throws<ArgumentException>(() => TextDecorationCollectionConverter.ConvertFromString(text));
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    public void ConvertTo_ReturnsExpected(TextDecorationCollection expected, object? value, Type destinationType)
    {
        TextDecorationCollectionConverter converter = new();

        InstanceDescriptor? result = (InstanceDescriptor?)converter.ConvertTo(null, null, value, destinationType);

        Assert.NotNull(result);

        // Create instance using the InstanceDescriptor
        TextDecorationCollection? actual = (TextDecorationCollection?)result.Invoke();

        // Check instance
        Assert.NotNull(actual);
        Assert.Equal(expected.Count, actual.Count);

        // We require the order to be exact as well
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    public static IEnumerable<object[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            // Single decoration
            yield return new object[] { new TextDecorationCollection(new TextDecoration[1] { TextDecorations.Underline[0] }),
                                        new TextDecorationCollection(new TextDecoration[1] { TextDecorations.Underline[0] }),
                                        typeof(InstanceDescriptor) };

            // Multiple decorations
            yield return new object[] { new TextDecorationCollection(new TextDecoration[3] { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }),
                                        new TextDecorationCollection(new TextDecoration[3] { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }),
                                        typeof(InstanceDescriptor) };

            // Source value just needs to be IEnumerable<TextDecoration>

            // T[]
            yield return new object[] { new TextDecorationCollection(new TextDecoration[3] { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }),
                                        new TextDecoration[3] { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }, typeof(InstanceDescriptor) };

            // List<T>
            yield return new object[] { new TextDecorationCollection(new TextDecoration[3] { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }),
                                        new List<TextDecoration> { TextDecorations.Underline[0], TextDecorations.OverLine[0], TextDecorations.Baseline[0] }, typeof(InstanceDescriptor) };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ThrowsArgumentNullException_Data))]
    public void ConvertTo_ThrowsArgumentNullException(object value, Type destinationType)
    {
        TextDecorationCollectionConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, null, value, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_ThrowsArgumentNullException_Data
    {
        get
        {
            // null value and destinationType
            yield return new object?[] { null, null };
            // supported value, null destinationType
            yield return new object?[] { new TextDecorationCollection(), null };
        }
    }

    [Fact]
    public void ConvertTo_ThrowsNotSupportedException()
    {
        TextDecorationCollectionConverter converter = new();

        // supported value, bad destinationType
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, null, new TextDecorationCollection(), typeof(TextDecorationCollection)));
    }
}
