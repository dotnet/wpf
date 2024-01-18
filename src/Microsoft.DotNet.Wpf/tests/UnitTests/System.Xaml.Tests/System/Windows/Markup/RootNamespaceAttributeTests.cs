// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class RootNamespaceAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nameSpace")]
    public void Ctor_String(string nameSpace)
    {
        var attribute = new RootNamespaceAttribute(nameSpace);
        Assert.Equal(nameSpace, attribute.Namespace);
    }
}
