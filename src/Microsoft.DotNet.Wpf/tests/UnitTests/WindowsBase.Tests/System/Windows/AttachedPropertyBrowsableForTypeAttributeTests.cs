// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Tests;

public class AttachedPropertyBrowsableForTypeAttributeTests
{
    // TODO:
    // - IsBrowsable
    // - UnionResults
    
    [Fact]
    public void Ctor_Type()
    {
        var attribute = new AttachedPropertyBrowsableForTypeAttribute(typeof(string));
        Assert.Equal(typeof(string), attribute.TargetType);
        Assert.Same(attribute, attribute.TypeId);
    }

    [Fact]
    public void Ctor_NullTargetType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("targetType", () => new AttachedPropertyBrowsableForTypeAttribute(null!));
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var attribute = new AttachedPropertyBrowsableForTypeAttribute(typeof(string));
        yield return new object?[] { attribute, attribute, true };
        yield return new object?[] { attribute, new AttachedPropertyBrowsableForTypeAttribute(typeof(string)), true };
        yield return new object?[] { attribute, new AttachedPropertyBrowsableForTypeAttribute(typeof(int)), false };
        yield return new object?[] { attribute, new object(), false };
        yield return new object?[] { attribute, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(AttachedPropertyBrowsableForTypeAttribute attribute, object obj, bool expected)
    {
        Assert.Equal(expected, attribute.Equals(obj));
        if (obj is AttachedPropertyBrowsableForTypeAttribute otherAttribute)
        {
            Assert.Equal(expected, otherAttribute.Equals(attribute));
            Assert.Equal(expected, attribute.GetHashCode().Equals(obj.GetHashCode()));
        }
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsEqual()
    {
        var attribute = new AttachedPropertyBrowsableForTypeAttribute(typeof(string));
        Assert.NotEqual(0, attribute.GetHashCode());
        Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
    }
}
