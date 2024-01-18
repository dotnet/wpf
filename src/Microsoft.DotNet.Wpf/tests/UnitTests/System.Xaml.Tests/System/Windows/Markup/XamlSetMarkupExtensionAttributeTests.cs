// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class XamlSetMarkupExtensionAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xamlSetMarkupExtensionHandler")]
    public void Ctor_String(string xamlSetMarkupExtensionHandler)
    {
        var attribute = new XamlSetMarkupExtensionAttribute(xamlSetMarkupExtensionHandler);
        Assert.Equal(xamlSetMarkupExtensionHandler, attribute.XamlSetMarkupExtensionHandler);
    }
}
