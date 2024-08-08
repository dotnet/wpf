// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Xaml.Replacements.Tests;

public class DateTimeOffsetConverter2Tests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), true)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
    }

    public static IEnumerable<object?[]> ConvertFrom_TestData()
    {
        DateTimeOffset dateTimeOffset = new DateTimeOffset(2018, 12, 9, 1, 2, 3, 4, TimeSpan.FromMinutes(10));
        yield return new object?[] { dateTimeOffset.ToString("O", CultureInfo.CurrentCulture), null, dateTimeOffset };
        yield return new object?[] { "  " + dateTimeOffset.ToString("O", CultureInfo.CurrentCulture) + "  ", null, dateTimeOffset };
        yield return new object?[] { dateTimeOffset.ToString("O", new CultureInfo("en-US")), new CultureInfo("en-US"), dateTimeOffset };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_String_ReturnsExpected(string value, CultureInfo culture, DateTimeOffset expected)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.ConvertFrom(null, culture, value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), true)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }

    public static IEnumerable<object?[]> ConvertTo_TestData()
    {
        yield return new object?[] { DateTimeOffset.MinValue, null };
        yield return new object?[] { new DateTimeOffset(2018, 12, 9, 1, 2, 3, 4, TimeSpan.FromMinutes(10)), new CultureInfo("en-US") };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_TestData))]
    public void ConvertTo_DateTimeOffset_ReturnsExpected(DateTimeOffset dateTimeOffset, CultureInfo culture)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(dateTimeOffset.ToString("O", culture ?? CultureInfo.CurrentCulture), converter.ConvertTo(null, culture, dateTimeOffset, typeof(string)));
    }

    [Fact]
    public void ConvertTo_InstanceDescriptor_ReturnsExpected()
    {
        var dateTimeOffset = new DateTimeOffset(2018, 12, 9, 1, 2, 3, 4, TimeSpan.FromMinutes(10));
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(dateTimeOffset, typeof(InstanceDescriptor)));
        ParameterInfo[] parameters = Assert.IsAssignableFrom<ConstructorInfo>(descriptor.MemberInfo).GetParameters();
        Assert.Equal(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(TimeSpan) }, parameters.Select(p => p.ParameterType));
        Assert.Equal(new object[] { 2018, 12, 9, 1, 2, 3, 4, TimeSpan.FromMinutes(10) }, descriptor.Arguments);
        Assert.True(descriptor.IsComplete);
    }
    
    [Theory]
    [InlineData("notDateTime")]
    [InlineData(null)]
    public void ConvertTo_NotDateTimeOffset_ReturnsExpected(object? value)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(DateTimeOffset.MinValue, destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        var type = new XamlType(typeof(DateTimeOffset), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(DateTimeOffset.MinValue, null!));
    }
}
