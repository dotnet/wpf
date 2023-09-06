// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class StringValueSerializerTests
{
    [Fact]
    public void GetSerializerFor_StringConvertToString_ReturnsTrue()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(string));
        Assert.True(serializer.CanConvertToString(null, null));
    }

    [Fact]
    public void GetSerializerFor_StringConvertFromString_ReturnsTrue()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(string));
        Assert.True(serializer.CanConvertFromString(null, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void GetSerializerFor_StringConvertToString_ReturnsValue(object value)
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(string));
        Assert.Equal(value, serializer.ConvertToString(value, null));
    }

    [Fact]
    public void GetSerializerFor_StringConvertToStringValueNotString_ThrowsInvalidCastException()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(string));
        Assert.Throws<InvalidCastException>(() => serializer.ConvertToString(1, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void GetSerializerFor_StringConvertFromString_ReturnsValue(string value)
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(string));
        Assert.Equal(value, serializer.ConvertFromString(value, null));
    }
}
