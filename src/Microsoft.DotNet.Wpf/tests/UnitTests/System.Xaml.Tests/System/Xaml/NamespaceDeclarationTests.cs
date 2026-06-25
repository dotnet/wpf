// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests;

public class NamespaceDeclarationTests
{
    [Theory]
    [InlineData("ns", "prefix")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void Ctor_String_String(string? ns, string? prefix)
    {
        var declaration = new NamespaceDeclaration(ns, prefix);
        Assert.Equal(ns, declaration.Namespace);
        Assert.Equal(prefix, declaration.Prefix);
    }
}
