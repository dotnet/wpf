// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Tests;

public class ItemPropertyInfoTests
{
    public static IEnumerable<object?[]> Ctor_String_Type_Object_TestData()
    {
        yield return new object?[] { null, null, null };
        yield return new object?[] { string.Empty, typeof(int), new object() };
        yield return new object?[] { "name", typeof(object), 1 };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_Type_Object_TestData))]
    public void Ctor_String_Type_Object(string name, Type type, object descriptor)
    {
        var propertyInfo = new ItemPropertyInfo(name, type, descriptor);
        Assert.Equal(descriptor, propertyInfo.Descriptor);
        Assert.Equal(name, propertyInfo.Name);
        Assert.Equal(type, propertyInfo.PropertyType);
    }
}
