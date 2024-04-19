// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable
using Xunit;

namespace System.Windows.Markup.Tests;

public class XmlLangPropertyAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("name")]
    public void Ctor_String(string name)
    {
        var attribute = new XmlLangPropertyAttribute(name);
        Assert.Equal(name, attribute.Name);
    }
}
