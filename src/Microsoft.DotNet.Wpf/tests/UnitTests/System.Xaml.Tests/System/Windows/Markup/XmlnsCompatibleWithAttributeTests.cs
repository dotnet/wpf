// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class XmlnsCompatibleWithAttributeTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("oldNamespace", "newNamespace")]
    public void Ctor_String_String(string oldNamespace, string newNamespace)
    {
        var attribute = new XmlnsCompatibleWithAttribute(oldNamespace, newNamespace);
        Assert.Equal(oldNamespace, attribute.OldNamespace);
        Assert.Equal(newNamespace, attribute.NewNamespace);
    }

    [Fact]
    public void Ctor_NullOldNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("oldNamespace", () => new XmlnsCompatibleWithAttribute(null, "newNamespace"));
    }

    [Fact]
    public void Ctor_NullNewNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("newNamespace", () => new XmlnsCompatibleWithAttribute("oldNamespace", null));
    }
}
