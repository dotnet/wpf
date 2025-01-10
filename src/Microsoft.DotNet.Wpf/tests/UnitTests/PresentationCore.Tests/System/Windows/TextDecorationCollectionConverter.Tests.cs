// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;

namespace System.Windows
{
    public class TextDecorationCollectionConverterTests
    {
        [Theory]
        [MemberData(nameof(ConvertFromString_TestData))]
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

        [Fact]
        public void ConvertFromString_NullValue_ReturnsNull()
        {
            // null is simply null (NOTE: This differs from instance method ConvertFrom, that will throw on null value)
            TextDecorationCollection? converted = TextDecorationCollectionConverter.ConvertFromString(null);

            Assert.Null(converted);
        }

        public static IEnumerable<object?[]> ConvertFromString_TestData
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
        [MemberData(nameof(ConvertTo_ThrowsArgumentNullException_TestData))]
        public void ConvertTo_ThrowsArgumentNullException(object value, Type destinationType)
        {
            TextDecorationCollectionConverter converter = new();

            Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, null, value, destinationType));
        }

        public static IEnumerable<object?[]> ConvertTo_ThrowsArgumentNullException_TestData
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
}
