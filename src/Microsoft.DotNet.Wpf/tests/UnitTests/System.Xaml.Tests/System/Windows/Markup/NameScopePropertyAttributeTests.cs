// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class NameScopePropertyAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("name")]
    public void Ctor_String(string name)
    {
        var attribute = new NameScopePropertyAttribute(name);
        Assert.Equal(name, attribute.Name);
        Assert.Null(attribute.Type);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", typeof(int))]
    [InlineData("name", typeof(string))]
    public void Ctor_String_Type(string name, Type type)
    {
        var attribute = new NameScopePropertyAttribute(name, type);
        Assert.Equal(name, attribute.Name);
        Assert.Equal(type, attribute.Type);
    }
}
