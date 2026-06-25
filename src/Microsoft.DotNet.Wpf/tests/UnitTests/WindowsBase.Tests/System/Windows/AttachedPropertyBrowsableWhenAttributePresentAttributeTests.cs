// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Tests;

public class AttachedPropertyBrowsableWhenAttributePresentAttributeTests
{
    // TODO:
    // - IsBrowsable
    
    [Fact]
    public void Ctor_Type()
    {
        var attribute = new AttachedPropertyBrowsableWhenAttributePresentAttribute(typeof(string));
        Assert.Equal(typeof(string), attribute.AttributeType);
        Assert.Equal(typeof(AttachedPropertyBrowsableWhenAttributePresentAttribute), attribute.TypeId);
    }

    [Fact]
    public void Ctor_NullAttributeType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("attributeType", () => new AttachedPropertyBrowsableWhenAttributePresentAttribute(null!));
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var attribute = new AttachedPropertyBrowsableWhenAttributePresentAttribute(typeof(string));
        yield return new object?[] { attribute, attribute, true };
        yield return new object?[] { attribute, new AttachedPropertyBrowsableWhenAttributePresentAttribute(typeof(string)), true };
        yield return new object?[] { attribute, new AttachedPropertyBrowsableWhenAttributePresentAttribute(typeof(int)), false };
        yield return new object?[] { attribute, new object(), false };
        yield return new object?[] { attribute, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(AttachedPropertyBrowsableWhenAttributePresentAttribute attribute, object obj, bool expected)
    {
        Assert.Equal(expected, attribute.Equals(obj));
        if (obj is AttachedPropertyBrowsableWhenAttributePresentAttribute otherAttribute)
        {
            Assert.Equal(expected, otherAttribute.Equals(attribute));
            Assert.Equal(expected, attribute.GetHashCode().Equals(obj.GetHashCode()));
        }
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsEqual()
    {
        var attribute = new AttachedPropertyBrowsableWhenAttributePresentAttribute(typeof(string));
        Assert.NotEqual(0, attribute.GetHashCode());
        Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
    }
}
