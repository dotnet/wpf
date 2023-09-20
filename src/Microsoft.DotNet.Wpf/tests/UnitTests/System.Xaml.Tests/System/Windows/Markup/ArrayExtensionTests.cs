// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Windows.Markup.Tests;

public class ArrayExtensionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var extension = new ArrayExtension();
        Assert.Null(extension.Type);
        Assert.IsType<ArrayList>(extension.Items);
        Assert.Same(extension.Items, extension.Items);
        Assert.Empty(extension.Items);
    }

    [Theory]
    [InlineData(typeof(int))]
    public void Ctor_Type(Type type)
    {
        var extension = new ArrayExtension(type);
        Assert.Equal(type, extension.Type);
        Assert.IsType<ArrayList>(extension.Items);
        Assert.Same(extension.Items, extension.Items);
        Assert.Empty(extension.Items);
    }

    [Fact]
    public void Ctor_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("arrayType", () => new ArrayExtension((Type)null!));
    }

    public static IEnumerable<object[]> Ctor_Array_TestData()
    {
        yield return new object[] { new object[0], typeof(object) };
        yield return new object[] { new object[1], typeof(object) };
        yield return new object[] { new int[0], typeof(int) };
        yield return new object[] { new int[] { 1, 2, 3, 4, 5, 6 }, typeof(int) };
        yield return new object[] { new int[0, 0], typeof(int) };
        yield return new object[] { new int[0, 1], typeof(int) };
        yield return new object[] { new int[1, 0], typeof(int) };
    }

    [Theory]
    [MemberData(nameof(Ctor_Array_TestData))]
    public void Ctor_Array(Array elements, Type expectedType)
    {
        var extension = new ArrayExtension(elements);
        Assert.Equal(expectedType, extension.Type);
        Assert.IsType<ArrayList>(extension.Items);
        Assert.Same(extension.Items, extension.Items);
        Assert.Equal(elements, extension.Items);
    }

    [Fact]
    public void Ctor_NullElements_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("elements", () => new ArrayExtension((Array)null!));
    }

    [Fact]
    public void Ctor_MultdimensionalArray_ThrowsRankException()
    {
        Assert.Throws<RankException>(() => new ArrayExtension(new int[1, 1]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("string")]
    [InlineData(1)]
    public void AddChild_Invoke_AddsToItems(object value)
    {
        var extension = new ArrayExtension();
        extension.AddChild(value);
        Assert.Equal(value, Assert.Single(extension.Items));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("string")]
    public void AddText_Invoke_AddsToItems(string text)
    {
        var extension = new ArrayExtension();
        extension.AddText(text);
        Assert.Equal(text, Assert.Single(extension.Items));
    }

    [Fact]
    public void ProvideValue_ValidArrayType_ReturnsExpected()
    {
        var extension = new ArrayExtension(typeof(int));
        extension.AddChild(1);
        extension.AddChild(2);
        Assert.Equal(new object[] { 1, 2 }, extension.ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_NullArrayType_ThrowsInvalidOperationException()
    {
        var extension = new ArrayExtension();
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_InvalidArrayType_ThrowsInvalidOperationException()
    {
        var extension = new ArrayExtension(typeof(int));
        extension.AddChild("string");
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(int))]
    public void Type_Set_GetReturnsExpected(Type value)
    {
        var extension = new ArrayExtension();

        // Set.
        extension.Type = value;
        Assert.Equal(value, extension.Type);

        // Set same.
        extension.Type = value;
        Assert.Equal(value, extension.Type);
    }
}
