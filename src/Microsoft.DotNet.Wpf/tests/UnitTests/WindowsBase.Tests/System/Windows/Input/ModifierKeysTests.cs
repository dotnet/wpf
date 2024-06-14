// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input.Tests;

public class ModifierKeysTests
{
    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<ModifierKeysConverter>(TypeDescriptor.GetConverter(typeof(ModifierKeys)));
    }

    [Fact]
    public void ValueSerializer_Get_ReturnsExpected()
    {
        Assert.IsType<ModifierKeysValueSerializer>(ValueSerializer.GetSerializerFor(typeof(ModifierKeys)));
    }
}
