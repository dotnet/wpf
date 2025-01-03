// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Xunit;

namespace System.Xaml.Replacements.Tests;

public class DateTimeConverter2Tests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), true)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
    }

    public static IEnumerable<object[]> ConvertFrom_TestData()
    {
        yield return new object[] { "", DateTime.MinValue };

        foreach (DateTime date in new DateTime[] { new DateTime(2018, 12, 9), new DateTime(2018, 12, 9, 1, 2, 0, 0), new DateTime(2018, 12, 9, 1, 2, 3, 0) })
        {
            yield return new object[] { date.ToString(CultureInfo.InvariantCulture), date };
        }
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_String_ReturnsExpected(string value, DateTime expected)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.ConvertFrom(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }

    public static IEnumerable<object[]> ConvertTo_TestData()
    {
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Local), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Utc), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), "yyyy-MM-ddK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 0, 0), "yyyy-MM-dd'T'HH':'mmK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 3, 0), "yyyy-MM-dd'T'HH':'mm':'ssK" };
        yield return new object[] { new DateTime(2018, 12, 9, 1, 2, 3, 4), "yyyy-MM-dd'T'HH':'mm':'ss'.'FFFFFFFK" };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_TestData))]
    public void ConvertTo_DateTime_ReturnsExpected(DateTime date, string format)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(date.ToString(format, CultureInfo.InvariantCulture), converter.ConvertTo(date, typeof(string)));
    }
    
    [Theory]
    [InlineData("notDateTime")]
    [InlineData(null)]
    public void ConvertTo_NotDateTime_ReturnsExpected(object? value)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(InstanceDescriptor))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(DateTime.MinValue, destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        var type = new XamlType(typeof(DateTime), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(DateTime.MinValue, null!));
    }
}
