// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Windows.Markup.Tests;

public class DateTimeValueSerializerTests
{
    public static IEnumerable<object?[]> CanConvertFrom_TestData()
    {
        yield return new object?[] { new DateTime(), true };
        yield return new object?[] { new object(), false };
        yield return new object?[] { null, false };
    }

    [Theory]
    [MemberData(nameof(CanConvertFrom_TestData))]
    public void CanConvertToString_Invoke_ReturnsFalse(object value, bool expected)
    {
        var serializer = new DateTimeValueSerializer();
        Assert.Equal(expected, serializer.CanConvertToString(value, null));
    }

    public static IEnumerable<object[]> ConvertToString_TestData()
    {
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Local), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Utc), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "yyyy-MM-ddK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 0, 0), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 3, 0), "yyyy-MM-dd'T'HH':'mm':'ssK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 3, 4), "yyyy-MM-dd'T'HH':'mm':'ss'.'FFFFFFFK" };
    }

    [Theory]
    [MemberData(nameof(ConvertToString_TestData))]
    public void ConvertToString_DateTime_ReturnsExpected(DateTime date, string format)
    {
        var serializer = new DateTimeValueSerializer();
        Assert.Equal(date.ToString(format, CultureInfo.InvariantCulture), serializer.ConvertToString(date, null));
    }
    
    [Theory]
    [InlineData("notDateTime")]
    [InlineData(null)]
    public void ConvertToString_NotDateTime_ThrowsNotSupportedException(object value)
    {
        var serializer = new DateTimeValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, null));
    }

    [Fact]
    public void CanConvertFromString_Invoke_ReturnsTrue()
    {
        var serializer = new DateTimeValueSerializer();
        Assert.True(serializer.CanConvertFromString(null, null));
    }

    public static IEnumerable<object[]> ConvertFromString_TestData()
    {
        yield return new object[] { "", DateTime.MinValue };

        foreach (DateTime date in new DateTime[] { new DateTime(2018, 12, 9), new DateTime(2018, 12, 9, 1, 2, 0, 0), new DateTime(2018, 12, 9, 1, 2, 3, 0) })
        {
            yield return new object[] { date.ToString(CultureInfo.InvariantCulture), date };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFromString_TestData))]
    public void ConvertFromString_String_ReturnsExpected(string value, DateTime expected)
    {
        var serializer = new DateTimeValueSerializer();
        Assert.Equal(expected, serializer.ConvertFromString(value, null));
    }

    [Fact]
    public void ConvertFromString_NullString_ThrowsNotSupportedException()
    {
        var serializer = new DateTimeValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, null));   
    }
}
