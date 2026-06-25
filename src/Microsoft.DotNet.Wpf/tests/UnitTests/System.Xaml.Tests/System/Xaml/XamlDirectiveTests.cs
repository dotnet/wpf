// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xaml.Schema;
using Xunit;

namespace System.Xaml.Tests;

public class XamlDirectiveTests
{
    public static IEnumerable<object?[]> Ctor_Strings_String_XamlType_XamlValueConverter_AllowedMembersLocation_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());

        yield return new object?[] { new string[] { "xamlNamespace" }, "name", null, AllowedMemberLocations.Any };
        yield return new object?[] { new string[] { ""} , "", new XamlValueConverter<TypeConverter>(typeof(int), type), AllowedMemberLocations.None };
        yield return new object?[] { new string[] { ""} , "", new XamlValueConverter<TypeConverter>(typeof(int), type), AllowedMemberLocations.None };
        yield return new object?[] { Array.Empty<string>(), "name", null, AllowedMemberLocations.None - 1 };
    }

    [Theory]
    [MemberData(nameof(Ctor_Strings_String_XamlType_XamlValueConverter_AllowedMembersLocation_TestData))]
    public void Ctor_Strings_String_XamlType_XamlValueConverter_AllowedMembersLocation(IEnumerable<string> xamlNamespaces, string name, XamlValueConverter<TypeConverter> typeConverter, AllowedMemberLocations allowedLocation)
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var directive = new XamlDirective(xamlNamespaces, name, type, typeConverter, allowedLocation);
        Assert.Equal(xamlNamespaces, directive.GetXamlNamespaces());
        Assert.Equal(name, directive.Name);
        Assert.Equal(type, directive.Type);
        Assert.Equal(typeConverter, directive.TypeConverter);
        Assert.Equal(allowedLocation, directive.AllowedLocation);
        Assert.False(directive.IsUnknown);
    }

    [Fact]
    public void Ctor_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlType", () => new XamlDirective(new string[] { "namespace"}, "name", null, null, AllowedMemberLocations.Any));
    }

    [Fact]
    public void Ctor_NullNamespaces_ThrowsArgumentNullException()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("xamlNamespaces", () => new XamlDirective(null, "name", type, null, AllowedMemberLocations.Any));
    }

    [Fact]
    public void Ctor_NullValueInNamespaces_ThrowsArgumentException()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        Assert.Throws<ArgumentException>("xamlNamespaces", () => new XamlDirective(new string?[] { null }, "name", type, null, AllowedMemberLocations.Any));
    }

    [Theory]
    [InlineData("xamlNamespace", "name")]
    [InlineData("", "")]
    [InlineData("xamlNamespace", null)]
    public void Ctor_String_String(string xamlNamespace, string? name)
    {
        var directive = new XamlDirective(xamlNamespace, name);
        Assert.Equal(new string[] { xamlNamespace }, directive.GetXamlNamespaces());
        Assert.Equal(name, directive.Name);
        Assert.Equal(XamlLanguage.Object, directive.Type);
        Assert.Equal(AllowedMemberLocations.Any, directive.AllowedLocation);
        Assert.True(directive.IsUnknown);
    }

    [Fact]
    public void Ctor_NullNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlNamespace", () => new XamlDirective(null, "name"));
    }

    [Fact]
    public void Invoker_GetValue_ThrowsNotSupportedException()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var directive = new XamlDirective(new string[] { "namespace"}, "name", type, null, AllowedMemberLocations.Any);
        Assert.Throws<NotSupportedException>(() => directive.Invoker.GetValue(null));
    }

    [Fact]
    public void Invoker_SetValue_ThrowsNotSupportedException()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var directive = new XamlDirective(new string[] { "namespace"}, "name", type, null, AllowedMemberLocations.Any);
        Assert.Throws<NotSupportedException>(() => directive.Invoker.SetValue(null, null));
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var type1 = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var type2 = new XamlType("unknownTypeNamespace2", "unknownTypeName", null, new XamlSchemaContext());
        var directive = new XamlDirective(new string[] { "xamlNamespace" }, "name", type1, null, AllowedMemberLocations.Any);
        yield return new object?[] { directive, directive, true };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, "name", type1, null, AllowedMemberLocations.Any), true };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, "name", type1, null, AllowedMemberLocations.None), true };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, "name", type1, new XamlValueConverter<TypeConverter>(typeof(int), type1), AllowedMemberLocations.None), true };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, "name", type2, null, AllowedMemberLocations.Any), true };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespaces" }, "name", type1, null, AllowedMemberLocations.Any), false };
        yield return new object?[] { directive, new XamlDirective(Array.Empty<string>(), "name", type1, null, AllowedMemberLocations.Any), false };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace", "2" }, "name", type1, null, AllowedMemberLocations.Any), false };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, "name2", type1, null, AllowedMemberLocations.Any), false };
        yield return new object?[] { directive, new XamlDirective(new string[] { "xamlNamespace" }, null, type1, null, AllowedMemberLocations.Any), false };

        yield return new object?[] { new XamlDirective("xamlNamespace", null), new XamlDirective("xamlNamespace", null), true };
        yield return new object?[] { new XamlDirective("xamlNamespace", null), new XamlDirective("xamlNamespace", "name"), false };
        yield return new object?[] { directive, new object(), false };
        yield return new object?[] { directive, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(XamlDirective directive, object obj, bool expected)
    {
        XamlDirective? other = obj as XamlDirective;
        if (other != null || obj == null)
        {
            if (directive != null)
            {
                Assert.Equal(expected, directive.Equals(other));
            }
            Assert.Equal(expected, directive == other);
            Assert.Equal(!expected, directive != other);
        }

        if (directive != null)
        {
            Assert.Equal(expected, directive.Equals(obj));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("name")]
    public void GetHashCode_Invoke_ReturnsExpected(string? name)
    {
        var directive = new XamlDirective("xamlNamespace", name);
        Assert.Equal(directive.GetHashCode(), directive.GetHashCode());
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object[] { new XamlDirective(Array.Empty<string>(), "name", type, null, AllowedMemberLocations.Any), "name" };
        yield return new object[] { new XamlDirective(new string[] { "1", "2" }, "name", type, null, AllowedMemberLocations.Any), "{1}name" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(XamlDirective directive, string expected)
    {
        Assert.Equal(expected, directive.ToString());
    }

    [Fact]
    public void LookupInvoker_Get_ReturnsExpected()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Same(directive.LookupInvokerEntry(), directive.LookupInvokerEntry());
    }

    [Fact]
    public void LookupCustomAttributeProvider_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupCustomAttributeProviderEntry());
    }

    [Fact]
    public void LookupDependsOn_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupDependsOnEntry());
    }

    [Fact]
    public void LookupDeferringLoader_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupDeferringLoaderEntry());
    }

    [Fact]
    public void LookupIsAmbient_Get_ReturnsFalse()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.False(directive.LookupIsAmbientEntry());
    }

    [Fact]
    public void LookupIsEvent_Get_ReturnsFalse()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.False(directive.LookupIsEventEntry());
    }

    [Fact]
    public void LookupIsReadOnly_Get_ReturnsFalse()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.False(directive.LookupIsReadOnlyEntry());
    }

    [Fact]
    public void LookupIsReadPublic_Get_ReturnsTrue()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.True(directive.LookupIsReadPublicEntry());
    }

    [Fact]
    public void LookupIsUnknown_Get_ReturnsTrue()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.True(directive.LookupIsUnknownEntry());
    }

    [Fact]
    public void LookupIsWriteOnly_Get_ReturnsFalse()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.False(directive.LookupIsWriteOnlyEntry());
    }

    [Fact]
    public void LookupIsWritePublic_Get_ReturnsTrue()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.True(directive.LookupIsWritePublicEntry());
    }

    [Fact]
    public void LookupTargetType_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupTargetTypeEntry());
    }

    [Fact]
    public void LookupTypeConverter_Get_ReturnsExpected()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var typeConverter = new XamlValueConverter<TypeConverter>(typeof(int), type);
        var converter = new SubXamlDirective(Array.Empty<string>(), "name", type, typeConverter, AllowedMemberLocations.Any);
        Assert.Equal(typeConverter, converter.LookupTypeConverterEntry());
    }

    [Fact]
    public void LookupType_Get_ReturnsExpected()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var converter = new SubXamlDirective(Array.Empty<string>(), "name", type, null!, AllowedMemberLocations.Any);
        Assert.Equal(type, converter.LookupTypeEntry());
    }

    [Fact]
    public void LookupUnderlyingGetter_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupUnderlyingGetterEntry());
    }

    [Fact]
    public void LookupUnderlyingMember_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupUnderlyingMemberEntry());
    }

    [Fact]
    public void LookupUnderlyingSetter_Get_ReturnsNull()
    {
        var directive = new SubXamlDirective("xamlNamespace", "name");
        Assert.Null(directive.LookupUnderlyingSetterEntry());
    }

    public class SubXamlDirective : XamlDirective
    {
        public SubXamlDirective(string xamlNamespace, string name) : base(xamlNamespace, name) { }
        
        public SubXamlDirective(IEnumerable<string> xamlNamespaces, string name, XamlType xamlType, XamlValueConverter<TypeConverter> typeConverter, AllowedMemberLocations allowedLocation) : base(xamlNamespaces, name, xamlType, typeConverter, allowedLocation) { }

        public XamlMemberInvoker LookupInvokerEntry() => LookupInvoker();

        public ICustomAttributeProvider LookupCustomAttributeProviderEntry() => LookupCustomAttributeProvider();

        public IList<XamlMember> LookupDependsOnEntry() => LookupDependsOn();

        public XamlValueConverter<XamlDeferringLoader> LookupDeferringLoaderEntry() => LookupDeferringLoader();

        public bool LookupIsAmbientEntry() => LookupIsAmbient();

        public bool LookupIsEventEntry() => LookupIsEvent();

        public bool LookupIsReadOnlyEntry() => LookupIsReadOnly();

        public bool LookupIsReadPublicEntry() => LookupIsReadPublic();

        public bool LookupIsUnknownEntry() => LookupIsUnknown();

        public bool LookupIsWriteOnlyEntry() => LookupIsWriteOnly();

        public bool LookupIsWritePublicEntry() => LookupIsWritePublic();

        public XamlType LookupTargetTypeEntry() => LookupTargetType();

        public XamlValueConverter<TypeConverter> LookupTypeConverterEntry() => LookupTypeConverter();

        public XamlType LookupTypeEntry() => LookupType();

        public MethodInfo LookupUnderlyingGetterEntry() => LookupUnderlyingGetter();

        public MemberInfo LookupUnderlyingMemberEntry() => LookupUnderlyingMember();

        public MethodInfo LookupUnderlyingSetterEntry() => LookupUnderlyingSetter();
    }
}
