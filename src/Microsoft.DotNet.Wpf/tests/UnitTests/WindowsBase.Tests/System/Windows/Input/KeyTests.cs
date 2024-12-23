// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input.Tests;

public class KeyTests
{
    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<KeyConverter>(TypeDescriptor.GetConverter(typeof(Key)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<KeyValueSerializer>(ValueSerializer.GetSerializerFor(typeof(Key)));
    }
}
