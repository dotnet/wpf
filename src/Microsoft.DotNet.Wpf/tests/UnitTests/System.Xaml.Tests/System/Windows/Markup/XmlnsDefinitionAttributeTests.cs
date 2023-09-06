// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class XmlnsDefinitionAttributeTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("xmlNamespace", "clrNamespace")]
    public void Ctor_String_String(string xmlNamespace, string clrNamespace)
    {
        var attribute = new XmlnsDefinitionAttribute(xmlNamespace, clrNamespace);
        Assert.Equal(xmlNamespace, attribute.XmlNamespace);
        Assert.Equal(clrNamespace, attribute.ClrNamespace);
        Assert.Null(attribute.AssemblyName);
    }

    [Fact]
    public void Ctor_NullXmlNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xmlNamespace", () => new XmlnsDefinitionAttribute(null, "clrNamespace"));
    }

    [Fact]
    public void Ctor_NullClrNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("clrNamespace", () => new XmlnsDefinitionAttribute("xmlNamespace", null));
    }
}
