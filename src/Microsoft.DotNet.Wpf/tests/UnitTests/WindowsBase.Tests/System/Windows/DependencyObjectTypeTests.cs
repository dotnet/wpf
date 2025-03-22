// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Tests;

public class DependencyObjectTypeTests
{
    [Fact]
    public void FromSystemType_DependencyObject_Success()
    {
        DependencyObjectType type = DependencyObjectType.FromSystemType(typeof(DependencyObject));
        Assert.NotNull(type);
        Assert.Same(type, DependencyObjectType.FromSystemType(typeof(DependencyObject)));
        Assert.Null(type.BaseType);
        Assert.Equal(nameof(DependencyObject), type.Name);
        Assert.True(type.Id >= 0);
        Assert.Equal(typeof(DependencyObject), type.SystemType);
    }
    
    [Fact]
    public void FromSystemType_SubDependencyObject_Success()
    {
        DependencyObjectType type = DependencyObjectType.FromSystemType(typeof(SubDependencyObject));
        Assert.NotNull(type);
        Assert.Same(type, DependencyObjectType.FromSystemType(typeof(SubDependencyObject)));
        Assert.NotNull(type.BaseType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(DependencyObject)), type.BaseType);
        Assert.Equal(nameof(SubDependencyObject), type.Name);
        Assert.True(type.Id >= 0);
        Assert.Equal(typeof(SubDependencyObject), type.SystemType);
    }

    [Fact]
    public void FromSystemType_MultipleTypes_IdDifferent()
    {
        DependencyObjectType type1 = DependencyObjectType.FromSystemType(typeof(DependencyObject));
        DependencyObjectType type2 = DependencyObjectType.FromSystemType(typeof(SubDependencyObject));
        Assert.NotEqual(type1.Id, type2.Id);
    }

    [Fact]
    public void FromSystemType_NullSystemType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("systemType", () => DependencyObjectType.FromSystemType(null!));
    }

    [Theory]
    [InlineData(typeof(int))]
    public void FromSystemType_InvalidSystemType_ThrowsArgumentException(Type systemType)
    {
        // TODO: add paramName to code.
        Assert.Throws<ArgumentException>(() => DependencyObjectType.FromSystemType(systemType));
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsExpected()
    {
        DependencyObjectType type = DependencyObjectType.FromSystemType(typeof(DependencyObject));
        Assert.Equal(type.Id, type.GetHashCode());
    }

    public static IEnumerable<object?[]> IsInstanceOfType_TestData()
    {
        yield return new object?[] { typeof(DependencyObject), new DependencyObject(), true };
        yield return new object?[] { typeof(DependencyObject), new SubDependencyObject(), true };
        yield return new object?[] { typeof(DependencyObject), new OtherDependencyObject(), true };
        yield return new object?[] { typeof(DependencyObject), null, false };
        yield return new object?[] { typeof(SubDependencyObject), new DependencyObject(), false };
        yield return new object?[] { typeof(SubDependencyObject), new SubDependencyObject(), true };
        yield return new object?[] { typeof(SubDependencyObject), new OtherDependencyObject(), false };
        yield return new object?[] { typeof(SubDependencyObject), new SubSubDependencyObject(), true };
        yield return new object?[] { typeof(SubDependencyObject), null, false };
        yield return new object?[] { typeof(SubSubDependencyObject), new DependencyObject(), false };
        yield return new object?[] { typeof(SubSubDependencyObject), new OtherDependencyObject(), false };
        yield return new object?[] { typeof(SubSubDependencyObject), new SubDependencyObject(), false };
        yield return new object?[] { typeof(SubSubDependencyObject), new SubSubDependencyObject(), true };
        yield return new object?[] { typeof(SubSubDependencyObject), null, false };
    }

    [Theory]
    [MemberData(nameof(IsInstanceOfType_TestData))]
    public void IsInstanceOfType_Invoke_ReturnsExpected(Type systemType, DependencyObject dependencyObject, bool expected)
    {
        DependencyObjectType type = DependencyObjectType.FromSystemType(systemType);
        Assert.Equal(expected, type.IsInstanceOfType(dependencyObject));
    }

    [Theory]
    [InlineData(typeof(DependencyObject), typeof(DependencyObject), false)]
    [InlineData(typeof(DependencyObject), typeof(SubDependencyObject), false)]
    [InlineData(typeof(DependencyObject), typeof(OtherDependencyObject), false)]
    [InlineData(typeof(DependencyObject), null, false)]
    [InlineData(typeof(SubDependencyObject), typeof(DependencyObject), true)]
    [InlineData(typeof(SubDependencyObject), typeof(SubDependencyObject), false)]
    [InlineData(typeof(SubDependencyObject), typeof(OtherDependencyObject), false)]
    [InlineData(typeof(SubDependencyObject), typeof(SubSubDependencyObject), false)]
    [InlineData(typeof(SubDependencyObject), null, false)]
    [InlineData(typeof(SubSubDependencyObject), typeof(DependencyObject), true)]
    [InlineData(typeof(SubSubDependencyObject), typeof(SubDependencyObject), true)]
    [InlineData(typeof(SubSubDependencyObject), typeof(OtherDependencyObject), false)]
    [InlineData(typeof(SubSubDependencyObject), typeof(SubSubDependencyObject), false)]
    [InlineData(typeof(SubSubDependencyObject), null, false)]
    public void IsSubclassOf_Invoke_ReturnsExpected(Type systemType, Type? dependencyObjectType, bool expected)
    {
        DependencyObjectType type = DependencyObjectType.FromSystemType(systemType);
        DependencyObjectType? dependencyType = dependencyObjectType != null ? DependencyObjectType.FromSystemType(dependencyObjectType) : null;
        Assert.Equal(expected, type.IsSubclassOf(dependencyType));
    }

    private class SubDependencyObject : DependencyObject
    {
    }

    private class OtherDependencyObject : DependencyObject
    {
    }

    private class SubSubDependencyObject : SubDependencyObject
    {
    }
}
