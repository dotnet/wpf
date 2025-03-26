// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;

namespace System.Windows.Controls;

public sealed class VirtualizationCacheLengthConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    [InlineData(true, typeof(decimal))]
    [InlineData(true, typeof(float))]
    [InlineData(true, typeof(double))]
    [InlineData(true, typeof(short))]
    [InlineData(true, typeof(int))]
    [InlineData(true, typeof(long))]
    [InlineData(true, typeof(ushort))]
    [InlineData(true, typeof(uint))]
    [InlineData(true, typeof(ulong))]
    // Invalid types
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(InstanceDescriptor))]
    [InlineData(false, typeof(VirtualizationCacheLength))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    // Valid types
    [InlineData(true, typeof(string))]
    [InlineData(true, typeof(InstanceDescriptor))]
    // Invalid types
    [InlineData(false, typeof(int))]
    [InlineData(false, typeof(long))]
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(VirtualizationCacheLength))]
    public void CanConvertTo_ReturnsExpected(bool expected, Type destinationType)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    [MemberData(nameof(ConvertFrom_StringValues_ReturnsExpected_Data))]
    [Theory]
    public void ConvertFrom_StringValues_ReturnsExpected(string input, double cacheBefore, double cacheAfter, CultureInfo cultureInfo)
    {
        VirtualizationCacheLengthConverter converter = new();

        object? result = converter.ConvertFrom(null, cultureInfo, input);

        Assert.IsType<VirtualizationCacheLength>(result);
        VirtualizationCacheLength cachedLength = (VirtualizationCacheLength)result;

        Assert.Equal(cachedLength.CacheBeforeViewport, cacheBefore);
        Assert.Equal(cachedLength.CacheAfterViewport, cacheAfter);
    }

    public static IEnumerable<object[]> ConvertFrom_StringValues_ReturnsExpected_Data
    {
        get
        {
            // Standard double value parse
            yield return new object[] { "25,50", 25.0, 50.0, CultureInfo.InvariantCulture };
            yield return new object[] { "100,200", 100.0, 200.0, CultureInfo.InvariantCulture };
            yield return new object[] { "12.34,56.78", 12.34, 56.78, CultureInfo.InvariantCulture };
            yield return new object[] { "0,0", 0.0, 0.0, CultureInfo.InvariantCulture, };
            yield return new object[] { "1,2", 1.0, 2.0, CultureInfo.InvariantCulture };
            yield return new object[] { "3.14,2.718", 3.14, 2.718, CultureInfo.InvariantCulture };
            yield return new object[] { "42,84", 42.0, 84.0, CultureInfo.InvariantCulture };
            yield return new object[] { "99.9,100.1", 99.9, 100.1, CultureInfo.InvariantCulture };
            yield return new object[] { "0.1,0.2", 0.1, 0.2, CultureInfo.InvariantCulture };
            yield return new object[] { "123,456", 123.0, 456.0, CultureInfo.InvariantCulture };

            yield return new object[] { "25;50", 25.0, 50.0, new CultureInfo("fr-FR") };
            yield return new object[] { "12,34;56,78", 12.34, 56.78, new CultureInfo("fr-FR") };
            yield return new object[] { "0;0", 0.0, 0.0, new CultureInfo("fr-FR") };
            yield return new object[] { "1;2", 1.0, 2.0, new CultureInfo("fr-FR") };
            yield return new object[] { "3,14;2,718", 3.14, 2.718, new CultureInfo("fr-FR") };
            yield return new object[] { "42;84", 42.0, 84.0, new CultureInfo("fr-FR") };
            yield return new object[] { "99,9;100,1", 99.9, 100.1, new CultureInfo("fr-FR") };
            yield return new object[] { "0,1;0,2", 0.1, 0.2, new CultureInfo("fr-FR") };
            yield return new object[] { "123;456", 123.0, 456.0, new CultureInfo("fr-FR") };

            yield return new object[] { "7,5;15,5", 7.5, 15.5, new CultureInfo("de-DE") };

            // Fuzzed
            yield return new object[] { "      1;2", 1.0, 2.0, new CultureInfo("fr-FR") };
            yield return new object[] { "3,14;        2,718", 3.14, 2.718, new CultureInfo("fr-FR") };
            yield return new object[] { "99.9    ,       100.1    ", 99.9, 100.1, CultureInfo.InvariantCulture };
            yield return new object[] { "    0.1   ,0.2   ", 0.1, 0.2, CultureInfo.InvariantCulture };

            // Single value parse
            yield return new object[] { "88,8", 88.8, 88.8, new CultureInfo("de-DE") };
            yield return new object[] { "66.66", 66.66, 66.66, new CultureInfo("en-US") };
            yield return new object[] { "15.25", 15.25, 15.25, CultureInfo.InvariantCulture };
            yield return new object[] { "39,95", 39.95, 39.95, new CultureInfo("es-ES") };
            yield return new object[] { "110,1", 110.1, 110.1, new CultureInfo("fr-FR") };

            yield return new object[] { "        3,14", 3.14, 3.14, new CultureInfo("fr-FR") };
            yield return new object[] { "42            ", 42.0, 42.0, new CultureInfo("fr-FR") };
            yield return new object[] { "    2.718       ", 2.718, 2.718, CultureInfo.InvariantCulture };
        }
    }

    [MemberData(nameof(ConvertFrom_NumericValues_ReturnsExpected_Data))]
    [Theory]
    public void ConvertFrom_NumericValues_ReturnsExpected<T>(T input, double expectedCache, CultureInfo cultureInfo)
    {
        VirtualizationCacheLengthConverter converter = new();

        object? result = converter.ConvertFrom(null, cultureInfo, input);

        Assert.IsType<VirtualizationCacheLength>(result);
        VirtualizationCacheLength cachedLength = (VirtualizationCacheLength)result;

        Assert.Equal(cachedLength.CacheBeforeViewport, expectedCache);
        Assert.Equal(cachedLength.CacheAfterViewport, expectedCache);
    }

    public static IEnumerable<object[]> ConvertFrom_NumericValues_ReturnsExpected_Data
    {
        get
        {
            yield return new object[] { (decimal)25.3, 25.3, CultureInfo.InvariantCulture };
            yield return new object[] { (float)12.75, 12.75, new CultureInfo("fr-FR") };
            yield return new object[] { (short)100, 100.0, new CultureInfo("de-DE") };
            yield return new object[] { (ushort)65535, 65535.0, CultureInfo.InvariantCulture };
            yield return new object[] { 33, 33.0, new CultureInfo("en-US") }; // int
            yield return new object[] { (long)50, 50.0, new CultureInfo("es-ES") };
            yield return new object[] { (ulong)856699, 856699.0, new CultureInfo("fr-FR") };
            yield return new object[] { (uint)6666, 6666.0, new CultureInfo("de-DE") };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsNotSupportedException_Data))]
    public void ConvertFrom_ThrowsNotSupportedException(CultureInfo? culture, object value)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsNotSupportedException_Data
    {
        get
        {
            // Input was null
            yield return new object?[] { null, null };
            yield return new object?[] { new CultureInfo("ru-RU"), null };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsInvalidCastException_Data))]
    public void ConvertFrom_ThrowsInvalidCastException(CultureInfo? culture, object value)
    {
        VirtualizationCacheLengthConverter converter = new();

        // Thrown via Convert.ToDouble
        Assert.Throws<InvalidCastException>(() => converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsInvalidCastException_Data
    {
        get
        {
            // Bad type
            yield return new object?[] { new CultureInfo("ru-RU"), typeof(Duration) };
            yield return new object?[] { new CultureInfo("ru-RU"), typeof(TimeSpan) };

            yield return new object?[] { CultureInfo.InvariantCulture, typeof(VirtualizationCacheLengthUnit) };
            yield return new object?[] { CultureInfo.InvariantCulture, typeof(VirtualizationCacheLengthConverter) };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsFormatException_Data))]
    public void ConvertFrom_ThrowsFormatException(CultureInfo? culture, object value)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsFormatException_Data
    {
        get
        {
            // Wrong decimals/list separators
            yield return new object?[] { new CultureInfo("ru-RU"), "10.5;10.5" };
            yield return new object?[] { new CultureInfo("ru-RU"), "10.5,10.5" };
            yield return new object?[] { CultureInfo.InvariantCulture, "10,5;10,5" };

            // Wrong format
            yield return new object?[] { new CultureInfo("ru-RU"), "10,5.10,5" };
            yield return new object?[] { new CultureInfo("ru-RU"), "0.1,0.2,0.3" };
            yield return new object?[] { CultureInfo.InvariantCulture, "10,5.10,5" };
            yield return new object?[] { CultureInfo.InvariantCulture, "0.7, 0.5, 0.3" };
            yield return new object?[] { CultureInfo.InvariantCulture, "0.1,    0.2,       0.3" };

            // Too few
            yield return new object?[] { new CultureInfo("ru-RU"), string.Empty };
            yield return new object?[] { CultureInfo.InvariantCulture, string.Empty };
        }
    }

    [Theory]
    [InlineData(",1")]
    [InlineData(",  1")]
    [InlineData(",0.1,,0.2")]
    [InlineData("     ,0.1,      0.2")]

    public void ConvertFrom_Tokenizer_ThrowsInvalidOperationException(string input)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(null, CultureInfo.InvariantCulture, input));
    }

    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    [Theory]
    public void ConvertTo_ReturnsExpected(string expected, VirtualizationCacheLength input, CultureInfo cultureInfo)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Equal(expected, converter.ConvertTo(null, cultureInfo, input, typeof(string)));
    }

    public static IEnumerable<object[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            // Test cases using the single constructor
            yield return new object[] { "42.7,42.7", new VirtualizationCacheLength(42.7), new CultureInfo("en-US") };
            yield return new object[] { "0,0", new VirtualizationCacheLength(0), CultureInfo.InvariantCulture };
            yield return new object[] { "-15,2;-15,2", new VirtualizationCacheLength(-15.2), new CultureInfo("fr-FR") };
            yield return new object[] { "3,14159;3,14159", new VirtualizationCacheLength(3.14159), new CultureInfo("de-DE") };
            yield return new object[] { "10000,10000", new VirtualizationCacheLength(10000), new CultureInfo("ja-JP") };
            yield return new object[] { "0.0005,0.0005", new VirtualizationCacheLength(0.0005), new CultureInfo("en-GB") };
            yield return new object[] { "-99,99;-99,99", new VirtualizationCacheLength(-99.99), new CultureInfo("es-ES") };
            yield return new object[] { "500,5;500,5", new VirtualizationCacheLength(500.5), new CultureInfo("it-IT") };
            yield return new object[] { "7;7", new VirtualizationCacheLength(7), new CultureInfo("pt-BR") };
            yield return new object[] { "1.23456,1.23456", new VirtualizationCacheLength(1.23456), new CultureInfo("en-US") };

            // Test cases using the double constructor
            yield return new object[] { "12.34,56.78", new VirtualizationCacheLength(12.34, 56.78), CultureInfo.InvariantCulture };
            yield return new object[] { "-8.9,10.1", new VirtualizationCacheLength(-8.9, 10.1), new CultureInfo("en-US") };
            yield return new object[] { "0;25,5", new VirtualizationCacheLength(0.0, 25.5), new CultureInfo("fr-FR") };
            yield return new object[] { "100,75;200,25", new VirtualizationCacheLength(100.75, 200.25), new CultureInfo("de-DE") };
            yield return new object[] { "-0.001,0.002", new VirtualizationCacheLength(-0.001, 0.002), new CultureInfo("ja-JP") };
            yield return new object[] { "987.654,321.098", new VirtualizationCacheLength(987.654, 321.098), new CultureInfo("en-GB") };
            yield return new object[] { "-50;50", new VirtualizationCacheLength(-50.0, 50.0), new CultureInfo("es-ES") };
            yield return new object[] { "0,123;456,789", new VirtualizationCacheLength(0.123, 456.789), new CultureInfo("it-IT") };
            yield return new object[] { "9999,9;10000,1", new VirtualizationCacheLength(9999.9, 10000.1), new CultureInfo("pt-BR") };
            yield return new object[] { "-7.89,0", new VirtualizationCacheLength(-7.89, 0), CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidData_ThrowsNotSupportedException_Data))]
    public void ConvertTo_InvalidData_ThrowsNotSupportedException(object? input, Type? destinationType, CultureInfo? culture)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, culture, input, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_InvalidData_ThrowsNotSupportedException_Data
    {
        get
        {
            yield return new object[] { new VirtualizationCacheLength(17.5), typeof(Guid), new CultureInfo("de-DE") };
            yield return new object[] { new VirtualizationCacheLength(666.666), typeof(Uri), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(255848), typeof(Array), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.3, 0.4), typeof(int), CultureInfo.CurrentCulture };
            yield return new object[] { new VirtualizationCacheLength(0.7, 0.8), typeof(double), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.75, 0.75), typeof(object), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.0, 1.0), typeof(bool), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.66, 0.33), typeof(DateTime), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.4, 0.5), typeof(TimeSpan), new CultureInfo("it-IT") };
            yield return new object[] { new VirtualizationCacheLength(0.9, 1.0), typeof(Enum), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.3, 0.4), typeof(Point), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.9, 0.9), typeof(VirtualizationCacheLength), CultureInfo.CurrentCulture };
            yield return new object[] { new VirtualizationCacheLength(0.6, 0.8), typeof(VirtualizationCacheLengthUnit), new CultureInfo("pt-BR") };
            yield return new object[] { new VirtualizationCacheLength(0.4, 0.5), typeof(byte[]), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.2, 0.3), typeof(Dictionary<int, string>), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.3, 0.4), typeof(List<int>), CultureInfo.InvariantCulture };
            yield return new object[] { new VirtualizationCacheLength(0.2, 0.1), typeof(Stack<int>), CultureInfo.InvariantCulture };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidDestinationType_ThrowsArgumentNullException_Data))]
    public void ConvertTo_InvalidDestinationType_ThrowsArgumentNullException(VirtualizationCacheLength? input, Type? destinationType, CultureInfo? culture)
    {
        VirtualizationCacheLengthConverter converter = new();

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, culture, input, destinationType));
    }

    public static IEnumerable<object?[]> ConvertTo_InvalidDestinationType_ThrowsArgumentNullException_Data
    {
        get
        {
            yield return new object?[] { null, null, CultureInfo.InvariantCulture };
            yield return new object?[] { new VirtualizationCacheLength(0.0, 0.0), null, CultureInfo.InvariantCulture };
        }
    }
}
