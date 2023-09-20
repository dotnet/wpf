// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 0618

namespace System.Windows.Markup.Tests;

public class MarkupExtensionReturnTypeAttributeTests
{
    [Fact]
    public void Ctor_Default()
    {
        var attribute = new MarkupExtensionReturnTypeAttribute();
        Assert.Null(attribute.ReturnType);
        Assert.Null(attribute.ExpressionType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(int))]
    public void Ctor_Type(Type returnType)
    {
        var attribute = new MarkupExtensionReturnTypeAttribute(returnType);
        Assert.Equal(returnType, attribute.ReturnType);
        Assert.Null(attribute.ExpressionType);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(typeof(int), typeof(string))]
    public void Ctor_Type_Type(Type returnType, Type expressionType)
    {
        var attribute = new MarkupExtensionReturnTypeAttribute(returnType, expressionType);
        Assert.Equal(returnType, attribute.ReturnType);
        Assert.Equal(expressionType, attribute.ExpressionType);
    }
}

#pragma warning restore 0618
