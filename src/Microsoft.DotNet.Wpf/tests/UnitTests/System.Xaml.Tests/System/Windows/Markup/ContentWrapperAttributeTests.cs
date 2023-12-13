// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Windows.Markup.Tests;

public class ContentWrapperAttributeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(typeof(int))]
    public void Ctor_Type(Type contentWrapper)
    {
        var attribute = new ContentWrapperAttribute(contentWrapper);
        Assert.Equal(contentWrapper, attribute.ContentWrapper);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var attribute = new ContentWrapperAttribute(typeof(int));
        yield return new object?[] { attribute, attribute, true };
        yield return new object?[] { attribute, new ContentWrapperAttribute(typeof(int)), true };
        yield return new object?[] { attribute, new ContentWrapperAttribute(typeof(string)), false };
        yield return new object?[] { attribute, new ContentWrapperAttribute(null), false };
        yield return new object?[] { new ContentWrapperAttribute(null), new ContentWrapperAttribute(null), true };
        yield return new object?[] { new ContentWrapperAttribute(null), new ContentWrapperAttribute(typeof(int)), false };

        yield return new object?[] { attribute, new object(), false };
        yield return new object?[] { attribute, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(ContentWrapperAttribute attribute, object other, bool expected)
    {
        Assert.Equal(expected, attribute.Equals(other));
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsExpected()
    {
        var attribute = new ContentWrapperAttribute(typeof(int));
        Assert.Equal(typeof(int).GetHashCode(), attribute.GetHashCode());   
    }

    [Fact]
    public void GetHashCode_InvokeNullContentWrapper_ReturnsZero()
    {
        var attribute = new ContentWrapperAttribute(null);
        Assert.Equal(0, attribute.GetHashCode());
    }

    [Fact]
    public void TypeId_Get_ReturnsInstance()
    {
        var attribute = new ContentWrapperAttribute(typeof(int));
        Assert.Same(attribute, attribute.TypeId);
    }
}
