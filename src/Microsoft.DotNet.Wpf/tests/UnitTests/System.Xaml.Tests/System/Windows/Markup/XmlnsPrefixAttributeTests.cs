// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class XmlnsPrefixAttributeTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("xmlNamespace", "prefix")]
    public void Ctor_String_String(string xmlNamespace, string prefix)
    {
        var attribute = new XmlnsPrefixAttribute(xmlNamespace, prefix);
        Assert.Equal(xmlNamespace, attribute.XmlNamespace);
        Assert.Equal(prefix, attribute.Prefix);
    }

    [Fact]
    public void Ctor_NullXmlNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xmlNamespace", () => new XmlnsPrefixAttribute(null, "prefix"));
    }

    [Fact]
    public void Ctor_NullPrefix_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("prefix", () => new XmlnsPrefixAttribute("xmlNamespace", null));
    }
}
