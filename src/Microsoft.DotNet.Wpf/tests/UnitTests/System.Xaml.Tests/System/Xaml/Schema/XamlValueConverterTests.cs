// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace System.Xaml.Schema.Tests;

public class XamlValueConverterTests
{
    public static IEnumerable<object?[]> Ctor_Type_XamlType_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { typeof(int), type, "Int32(unknownTypeName)" };
        yield return new object?[] { typeof(int), null, "Int32" };
        yield return new object?[] { null, type, "unknownTypeName" };
    }

    [Theory]
    [MemberData(nameof(Ctor_Type_XamlType_TestData))]
    public void Ctor_Type_XamlType(Type converterType, XamlType targetType, string expectedName)
    {
        var converter = new XamlValueConverter<string>(converterType, targetType);
        Assert.Equal(converterType, converter.ConverterType);
        Assert.Equal(targetType, converter.TargetType);
        Assert.Equal(expectedName, converter.Name);
    }
    public static IEnumerable<object?[]> Ctor_Type_XamlType_String_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { typeof(int), type, "name", "name" };
        yield return new object?[] { typeof(int), null, "", "" };
        yield return new object?[] { null, type, null, "unknownTypeName" };
    }

    [Theory]
    [MemberData(nameof(Ctor_Type_XamlType_String_TestData))]
    public void Ctor_Type_XamlType_String(Type converterType, XamlType targetType, string name, string expectedName)
    {
        var converter = new XamlValueConverter<string>(converterType, targetType, name);
        Assert.Equal(converterType, converter.ConverterType);
        Assert.Equal(targetType, converter.TargetType);
        Assert.Equal(expectedName, converter.Name);
    }

    [Fact]
    public void Ctor_NullParameters_ThrowsArgumentException()
    {
        // TODO: paramName.
        Assert.Throws<ArgumentException>(() => new XamlValueConverter<string>(null, null));
        Assert.Throws<ArgumentException>(() => new XamlValueConverter<string>(null, null, null));
    }

    [Fact]
    public void ConverterInstance_CustomConverter_ReturnsExpected()
    {
        var type = new XamlType(typeof(string), new XamlSchemaContext());
        var converter = new XamlValueConverter<CustomConverter>(typeof(CustomConverter), type, "name");
        CustomConverter instance = Assert.IsType<CustomConverter>(converter.ConverterInstance);
        Assert.Same(instance, converter.ConverterInstance);
    }

    [Fact]
    public void ConverterInstance_EnumConverter_ReturnsExpected()
    {
        var type = new XamlType(typeof(ConsoleColor), new XamlSchemaContext());
        var converter = new XamlValueConverter<TypeConverter>(typeof(EnumConverter), type);
        EnumConverter instance = Assert.IsType<EnumConverter>(converter.ConverterInstance);
        Assert.Same(instance, converter.ConverterInstance);
    }

    [Fact]
    public void ConverterInstance_NullConverterType_ReturnsExpected()
    {
        var type = new XamlType(typeof(ConsoleColor), new XamlSchemaContext());
        var converter = new XamlValueConverter<TypeConverter>(null, type, "name");
        Assert.Null(converter.ConverterInstance);
    }

    [Fact]
    public void ConverterInstance_CustomConverterWithInvalidBaseType_ThrowsXamlSchemaException()
    {
        var type = new XamlType(typeof(string), new XamlSchemaContext());
        var converter = new XamlValueConverter<string>(typeof(TypeConverter), type, "name");
        Assert.Throws<XamlSchemaException>(() => converter.ConverterInstance);
    }

    [Fact]
    public void ConverterInstance_EnumConverterWithInvalidBaseType_ThrowsInvalidCastException()
    {
        var type = new XamlType(typeof(ConsoleColor), new XamlSchemaContext());
        var converter = new XamlValueConverter<string>(typeof(EnumConverter), type);
        Assert.Throws<InvalidCastException>(() => converter.ConverterInstance);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var type1 = new XamlType("unknownTypeNamespace", "unknownTypeName1", null, new XamlSchemaContext());
        var type2 = new XamlType("unknownTypeNamespace", "unknownTypeName2", null, new XamlSchemaContext());
        var converter = new XamlValueConverter<string>(typeof(int), type1, "name");

        yield return new object?[] { converter, converter, true };
        yield return new object?[] { converter, new XamlValueConverter<string>(typeof(string), type1, "name"), false };
        yield return new object?[] { converter, new XamlValueConverter<string>(null, type1, "name"), false };
        yield return new object?[] { converter, new XamlValueConverter<string>(typeof(string), type1, "name"), false };
        yield return new object?[] { converter, new XamlValueConverter<string>(typeof(string), null, "name"), false };
        yield return new object?[] { converter, new XamlValueConverter<string>(typeof(string), type1, "name2"), false };
        yield return new object?[] { converter, new XamlValueConverter<string>(typeof(string), type1, null), false };
        yield return new object?[] { new XamlValueConverter<string>(null, type1, null), new XamlValueConverter<string>(null, type1, null), true };
        yield return new object?[] { new XamlValueConverter<string>(null, type1, null), new XamlValueConverter<string>(typeof(string), type1, null), false };
        yield return new object?[] { new XamlValueConverter<string>(typeof(string), null, null), new XamlValueConverter<string>(typeof(string), null, null), true };
        yield return new object?[] { new XamlValueConverter<string>(typeof(string), null, null), new XamlValueConverter<string>(typeof(string), type1, null), false };

        yield return new object?[] { converter, new XamlValueConverter<object>(typeof(string), type1, "name"), false };
        yield return new object?[] { converter, new object(), false };
        yield return new object?[] { converter, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(XamlValueConverter<string> converter, object obj, bool expected)
    {
        XamlValueConverter<string>? other = obj as XamlValueConverter<string>;
        if (other != null || obj == null)
        {
            Assert.Equal(expected, converter.Equals(other));
            if (other != null)
            {
                Assert.Equal(expected, converter.GetHashCode().Equals(other.GetHashCode()));
            }

            Assert.Equal(expected, converter == other);
            Assert.Equal(!expected, converter != other);
        }
        
        Assert.Equal(expected, converter.Equals(obj));
    }

    private class CustomConverter { }
}