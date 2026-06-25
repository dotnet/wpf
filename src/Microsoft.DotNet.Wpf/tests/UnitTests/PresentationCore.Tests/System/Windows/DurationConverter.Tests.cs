// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace System.Windows;

public sealed class DurationConverterTests
{
    [Theory]
    // Valid type
    [InlineData(true, typeof(string))]
    // Invalid types
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(InstanceDescriptor))]
    public void CanConvertFrom_ReturnsExpected(bool expected, Type sourceType)
    {
        DurationConverter converter = new();

        Assert.Equal(expected, converter.CanConvertFrom(sourceType));
    }

    [Theory]
    // Valid types
    [InlineData(true, typeof(string))]
    [InlineData(true, typeof(InstanceDescriptor))]
    // Invalid types
    [InlineData(false, typeof(Duration))]
    [InlineData(false, typeof(TimeSpan))]
    [InlineData(false, typeof(int))]
    [InlineData(false, typeof(long))]
    public void CanConvertTo_ReturnsExpected(bool expected, Type destinationType)
    {
        DurationConverter converter = new();

        Assert.Equal(expected, converter.CanConvertTo(destinationType));
    }

    [Theory]
    [MemberData(nameof(ConvertTo_ReturnsExpected_Data))]
    public void ConvertTo_ReturnsExpected(string expected, CultureInfo? culture, object value, bool checkTimeSpan)
    {
        DurationConverter converter = new();

        if (checkTimeSpan) // Framework sanity check to keep the Invariant assumption
            Assert.Equal(expected, TimeSpan.Parse(expected).ToString());

        Assert.Equal(expected, (string)converter.ConvertTo(null, culture, value, typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_ReturnsExpected_Data
    {
        get
        {
            yield return new object?[] { "Automatic", null, Duration.Automatic, false };
            yield return new object?[] { "Forever", null, Duration.Forever, false };
            // Because Duration uses TimeSpan.ToString() under the covers, which was Invariant up to NetFX 4, the Duration's ToString will always be Invariant
            yield return new object?[] { "17.22:10:15.4571230", CultureInfo.InvariantCulture, new Duration(new TimeSpan(17, 22, 10, 15, 457, 123)), true };
            yield return new object?[] { "17.22:10:15.4571230", new CultureInfo("ru-RU"), new Duration(new TimeSpan(17, 22, 10, 15, 457, 123)), true };
            // This is a special case, because original call was a base call, it does not throw for NULL but returns string.Empty
            yield return new object?[] { string.Empty, null, null, false };
        }
    }

    [Fact]
    public void ConvertTo_ThrowsArgumentNullException()
    {
        DurationConverter converter = new();

        // destinationType was NULL
        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(new Duration(new TimeSpan(17, 22, 10, 15, 457, 123)), destinationType: null!));
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Duration))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(DateOnly))]
    public void ConvertTo_ThrowsNotSupportedException(Type destinationType)
    {
        DurationConverter converter = new();

        // Supply of invalid destinationTypes
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, null, new Duration(new TimeSpan(17, 22, 10, 15, 457, 123)), destinationType));
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ReturnsExpected_TestData))]
    public void ConvertFrom_ReturnsExpected(Duration expected, CultureInfo? culture, object value)
    {
        DurationConverter converter = new();

        Assert.Equal(expected, (Duration)converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ReturnsExpected_TestData
    {
        get
        {
            yield return new object?[] { Duration.Automatic, null, "Automatic" };
            yield return new object?[] { Duration.Forever, null, " Forever" };
            yield return new object?[] { new Duration(TimeSpan.FromTicks(6488853448000)), CultureInfo.InvariantCulture, "07:12:14:45.3448 " };
            yield return new object?[] { new Duration(TimeSpan.FromTicks(5624853448000)), new CultureInfo("ru-RU"), " 6:12:14:45,3448 " };
            // 6d
            yield return new object?[] { new Duration(TimeSpan.FromTicks(5184000000000)), CultureInfo.InvariantCulture, " 6 " };
            // 6h 12m
            yield return new object?[] { new Duration(TimeSpan.FromTicks(223200000000)), CultureInfo.InvariantCulture, " 6:12 " };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_ThrowsFormatException_TestData))]
    public void ConvertFrom_ThrowsFormatException(CultureInfo? culture, object value)
    {
        DurationConverter converter = new();

        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_ThrowsFormatException_TestData
    {
        get
        {
            // Invalid TimeSpan string specified
            yield return new object?[] { null, " á " };
            // Invalid TimeSpan string specified
            yield return new object?[] { new CultureInfo("ru-RU"), " á " };
            // Valid TimeSpan but wrong culture
            yield return new object?[] { CultureInfo.InvariantCulture, "       6:12:14:45,3448 " };
            // Valid TimeSpan but wrong culture
            yield return new object?[] { new CultureInfo("ru-RU"), "       6:12:14:45.3448 " };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_NotSupportedException_TestData))]
    public void ConvertFrom_NotSupportedException(CultureInfo? culture, object value)
    {
        DurationConverter converter = new();

        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null, culture, value));
    }

    public static IEnumerable<object?[]> ConvertFrom_NotSupportedException_TestData
    {
        get
        {
            // Input was null
            yield return new object?[] { null, null };
            // Wrong input type
            yield return new object?[] { null, 12345 };
            // Wrong input type with Culture
            yield return new object?[] { new CultureInfo("ru-RU"), 12345 };
        }
    }
}
