// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Tests;

public class PropertyFilterAttributeTests
{
    [Theory]
    [InlineData(PropertyFilterOptions.All)]
    [InlineData(PropertyFilterOptions.SetValues)]
    [InlineData(PropertyFilterOptions.Invalid)]
    [InlineData(PropertyFilterOptions.None)]
    [InlineData(PropertyFilterOptions.None - 1)]
    public void Ctor_PropertyFilterOptions(PropertyFilterOptions filter)
    {
        var attribute = new PropertyFilterAttribute(filter);
        Assert.Equal(filter, attribute.Filter);
    }

    [Fact]
    public void Default_Get_ReturnsExpected()
    {
        PropertyFilterAttribute attribute = PropertyFilterAttribute.Default;
        Assert.Equal(PropertyFilterOptions.All, attribute.Filter);
        Assert.Same(attribute, PropertyFilterAttribute.Default);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.None), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.All), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.All), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.None), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.All), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.None), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new object(), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(PropertyFilterAttribute attribute, object other, bool expected)
    {
        Assert.Equal(expected, attribute.Equals(other));
        if (other is PropertyFilterAttribute)
        {
            Assert.Equal(expected, attribute.GetHashCode().Equals(other.GetHashCode()));
        }
    }

    public static IEnumerable<object?[]> Match_TestData()
    {
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.None), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.All), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.All), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.None), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.All), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.SetValues), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.All), true };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.SetValues), new PropertyFilterAttribute(PropertyFilterOptions.None), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), new object(), false };
        yield return new object?[] { new PropertyFilterAttribute(PropertyFilterOptions.None), null, false };
    }

    [Theory]
    [MemberData(nameof(Match_TestData))]
    public void Match_Object_ReturnsExpected(PropertyFilterAttribute attribute, object value, bool expected)
    {
        Assert.Equal(expected, attribute.Match(value));
    }
}