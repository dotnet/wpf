// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 0618

namespace System.Windows.Markup.Tests;

public class AcceptedMarkupExtensionExpressionTypeAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(typeof(int))]
    public void Ctor_Type(Type type)
    {
        var attribute = new AcceptedMarkupExtensionExpressionTypeAttribute(type);
        Assert.Equal(type, attribute.Type);
    }
}

#pragma warning restore 0618
