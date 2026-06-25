// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Markup;
using System.Xaml.Schema;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Xaml.Tests;

public partial class XamlMemberTests
{
    [Theory]
    [InlineData("name", true)]
    [InlineData("", false)]
    public void Ctor_String_XamlType_Bool(string name, bool isAttachable)
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var member = new XamlMember(name, type, isAttachable);
        Assert.Equal(name, member.Name);
        Assert.Equal(type, member.DeclaringType);
        Assert.Equal(isAttachable, member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.False(member.IsEvent);
        Assert.Null(member.UnderlyingMember);
    }

    [Fact]
    public void Ctor_NullName_ThrowsArgumentNullException()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("name", () => new XamlMember(null, type, false));
    }

    [Fact]
    public void Ctor_NullDeclaringType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("declaringType", () => new XamlMember("name", null, false));
    }

    public static IEnumerable<object[]> Ctor_PropertyInfo_XamlSchemaContext_TestData()
    {
        yield return new object[] { typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_PropertyInfo_XamlSchemaContext_TestData))]
    public void Ctor_PropertyInfo_XamlSchemaContext(PropertyInfo propertyInfo, XamlSchemaContext schemaContext)
    {
        var member = new XamlMember(propertyInfo, schemaContext);
        Assert.Equal(propertyInfo.Name, member.Name);
        Assert.Equal(new XamlType(propertyInfo.DeclaringType, schemaContext), member.DeclaringType);
        Assert.False(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.False(member.IsEvent);
        Assert.Equal(propertyInfo, member.UnderlyingMember);
        Assert.NotNull(member.Invoker);
        Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    public static IEnumerable<object?[]> Ctor_PropertyInfo_XamlSchemaContext_XamlMemberInvoker_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext(), null };
        yield return new object?[] { typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext(), new XamlMemberInvoker(new XamlMember("name", type, false)) };
    }

    [Theory]
    [MemberData(nameof(Ctor_PropertyInfo_XamlSchemaContext_XamlMemberInvoker_TestData))]
    public void Ctor_PropertyInfo_XamlSchemaContext_XamlMemberInvoker(PropertyInfo propertyInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
    {
        var member = new XamlMember(propertyInfo, schemaContext, invoker);
        Assert.Equal(propertyInfo.Name, member.Name);
        Assert.Equal(new XamlType(propertyInfo.DeclaringType, schemaContext), member.DeclaringType);
        Assert.False(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.False(member.IsEvent);
        Assert.Equal(propertyInfo, member.UnderlyingMember);
        if (invoker == null)
        {
            Assert.NotNull(member.Invoker);
            Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
        }
        else
        {
            Assert.Equal(invoker, member.Invoker);
        }
    }

    [Fact]
    public void Ctor_NullPropertyInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("propertyInfo", () => new XamlMember((PropertyInfo)null!, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("propertyInfo", () => new XamlMember((PropertyInfo)null!, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    public static IEnumerable<object[]> Ctor_EventInfo_XamlSchemaContext_TestData()
    {
        yield return new object[] { typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_EventInfo_XamlSchemaContext_TestData))]
    public void Ctor_EventInfo_XamlSchemaContext(EventInfo eventInfo, XamlSchemaContext schemaContext)
    {
        var member = new XamlMember(eventInfo, schemaContext);
        Assert.Equal(eventInfo.Name, member.Name);
        Assert.Equal(new XamlType(eventInfo.DeclaringType, schemaContext), member.DeclaringType);
        Assert.False(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.True(member.IsEvent);
        Assert.Equal(eventInfo, member.UnderlyingMember);
        Assert.NotNull(member.Invoker);
        Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    public static IEnumerable<object?[]> Ctor_EventInfo_XamlSchemaContext_XamlMemberInvoker_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext(), null };
        yield return new object?[] { typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext(), new XamlMemberInvoker(new XamlMember("name", type, false)) };
    }

    [Theory]
    [MemberData(nameof(Ctor_EventInfo_XamlSchemaContext_XamlMemberInvoker_TestData))]
    public void Ctor_EventInfo_XamlSchemaContext_XamlMemberInvoker(EventInfo eventInfo, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
    {
        var member = new XamlMember(eventInfo, schemaContext, invoker);
        Assert.Equal(eventInfo.Name, member.Name);
        Assert.Equal(new XamlType(eventInfo.DeclaringType, schemaContext), member.DeclaringType);
        Assert.False(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.True(member.IsEvent);
        Assert.Equal(eventInfo, member.UnderlyingMember);
        if (invoker == null)
        {
            Assert.NotNull(member.Invoker);
            Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
        }
        else
        {
            Assert.Equal(invoker, member.Invoker);
        }
    }

    [Fact]
    public void Ctor_NullEventInfo_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("eventInfo", () => new XamlMember((EventInfo)null!, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("eventInfo", () => new XamlMember((EventInfo)null!, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    public static IEnumerable<object?[]> Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext_TestData()
    {
        yield return new object?[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext() };
        yield return new object?[] { "name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext() };
        yield return new object?[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext() };
        yield return new object?[] { "", typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticGetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext_TestData))]
    public void Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext(string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext)
    {
        var member = new XamlMember(attachablePropertyName, getter, setter, schemaContext);
        Assert.Equal(attachablePropertyName, member.Name);
        Assert.Equal(new XamlType(typeof(AccessorClass), schemaContext), member.DeclaringType);
        Assert.True(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.False(member.IsEvent);
        Assert.Equal(getter ?? setter, member.UnderlyingMember);
        Assert.NotNull(member.Invoker);
        Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    public static IEnumerable<object?[]> Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext_XamlMemberInvoker_TestData()
    {
        yield return new object?[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext(), null };
        yield return new object?[] { "name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker };
        yield return new object?[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext(), null };
        yield return new object?[] { "", typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticGetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext(), null };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext_XamlMemberInvoker_TestData))]
    public void Ctor_String_MethodInfo_MethodInfo_XamlSchemaContext_XamlMemberInvoker(string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
    {
        var member = new XamlMember(attachablePropertyName, getter, setter, schemaContext, invoker);
        Assert.Equal(attachablePropertyName, member.Name);
        Assert.Equal(new XamlType(typeof(AccessorClass), schemaContext), member.DeclaringType);
        Assert.True(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.False(member.IsEvent);
        Assert.Equal(getter ?? setter, member.UnderlyingMember);
        if (invoker == null)
        {
            Assert.NotNull(member.Invoker);
            Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
        }
        else
        {
            Assert.Equal(invoker, member.Invoker);
        }
    }

    [Fact]
    public void Ctor_NullAttachablePropertyName_ThrowsArgumentNullException()
    {
        MethodInfo getter = typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!;
        Assert.Throws<ArgumentNullException>("attachablePropertyName", () => new XamlMember(null, getter, null, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("attachablePropertyName", () => new XamlMember(null, getter, null, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Fact]
    public void Ctor_NullGetterAndSetter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new XamlMember("name", null, null, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>(() => new XamlMember("name", null, null, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Theory]
    [InlineData(nameof(AccessorClass.VoidGetMethod))]
    [InlineData(nameof(AccessorClass.ParameterlessGetMethod))]
    [InlineData(nameof(AccessorClass.TooManyParametersGetMethod))]
    public void Ctor_InvalidGetter_ThrowsArgumentException(string getterName)
    {
        MethodInfo getter = typeof(AccessorClass).GetMethod(getterName)!;
        Assert.Throws<ArgumentException>("getter", () => new XamlMember("name", getter, null, new XamlSchemaContext()));
        Assert.Throws<ArgumentException>("getter", () => new XamlMember("name", getter, null, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Theory]
    [InlineData(nameof(AccessorClass.ParameterlessSetMethod))]
    [InlineData(nameof(AccessorClass.TooManyParamtersSetMethod))]
    public void Ctor_InvalidSetter_ThrowsArgumentException(string setterName)
    {
        MethodInfo setter = typeof(AccessorClass).GetMethod(setterName)!;
        Assert.Throws<ArgumentException>("setter", () => new XamlMember("name", null, setter, new XamlSchemaContext()));
        Assert.Throws<ArgumentException>("setter", () => new XamlMember("name", null, setter, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    public static IEnumerable<object[]> Ctor_String_MethodInfo_XamlSchemaContext_TestData()
    {
        yield return new object[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext() };
        yield return new object[] { "", typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_MethodInfo_XamlSchemaContext_TestData))]
    public void Ctor_String_MethodInfo_XamlSchemaContext(string attachablePropertyName, MethodInfo adder, XamlSchemaContext schemaContext)
    {
        var member = new XamlMember(attachablePropertyName, adder, schemaContext);
        Assert.Equal(attachablePropertyName, member.Name);
        Assert.Equal(new XamlType(typeof(AccessorClass), schemaContext), member.DeclaringType);
        Assert.True(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.True(member.IsEvent);
        Assert.Equal(adder, member.UnderlyingMember);
        Assert.NotNull(member.Invoker);
        Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    public static IEnumerable<object?[]> Ctor_String_MethodInfo_XamlSchemaContext_XamlMemberInvoker_TestData()
    {
        yield return new object?[] { "name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext(), null };
        yield return new object?[] { "", typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_MethodInfo_XamlSchemaContext_XamlMemberInvoker_TestData))]
    public void Ctor_String_MethodInfo_XamlSchemaContext_XamlMemberInvoker(string attachablePropertyName, MethodInfo adder, XamlSchemaContext schemaContext, XamlMemberInvoker invoker)
    {
        var member = new XamlMember(attachablePropertyName, adder, schemaContext, invoker);
        Assert.Equal(attachablePropertyName, member.Name);
        Assert.Equal(new XamlType(typeof(AccessorClass), schemaContext), member.DeclaringType);
        Assert.True(member.IsAttachable);
        Assert.False(member.IsDirective);
        Assert.True(member.IsEvent);
        Assert.Equal(adder, member.UnderlyingMember);
        if (invoker == null)
        {
            Assert.NotNull(member.Invoker);
            Assert.NotEqual(XamlMemberInvoker.UnknownInvoker, member.Invoker);
        }
        else
        {
            Assert.Equal(invoker, member.Invoker);
        }
    }

    [Fact]
    public void Ctor_NullAttachableEventName_ThrowsArgumentNullException()
    {
        MethodInfo adder = typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!;
        Assert.Throws<ArgumentNullException>("attachableEventName", () => new XamlMember(null, adder, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("attachableEventName", () => new XamlMember(null, adder, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Fact]
    public void Ctor_NullAdder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("adder", () => new XamlMember("name", null, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("adder", () => new XamlMember("name", null, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Theory]
    [InlineData(nameof(AccessorClass.ParameterlessSetMethod))]
    [InlineData(nameof(AccessorClass.TooManyParamtersSetMethod))]
    public void Ctor_InvalidAdder_ThrowsArgumentException(string adderName)
    {
        MethodInfo adder = typeof(AccessorClass).GetMethod(adderName)!;
        Assert.Throws<ArgumentException>("adder", () => new XamlMember("name", adder, new XamlSchemaContext()));
        Assert.Throws<ArgumentException>("adder", () => new XamlMember("name", adder, new XamlSchemaContext(), XamlMemberInvoker.UnknownInvoker));
    }

    [Fact]
    public void Ctor_NullSchemaContext_ThrowsArgumentNullException()
    {
        PropertyInfo propertyInfo = typeof(DataClass).GetProperty(nameof(DataClass.Property))!;
        EventInfo eventInfo = typeof(DataClass).GetEvent(nameof(DataClass.Event))!;
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(propertyInfo, null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(eventInfo, null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(propertyInfo, null, XamlMemberInvoker.UnknownInvoker));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(eventInfo, null, XamlMemberInvoker.UnknownInvoker));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(propertyInfo.Name, propertyInfo.GetGetMethod(), propertyInfo.GetSetMethod(), null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(propertyInfo.Name, propertyInfo.GetGetMethod(), propertyInfo.GetSetMethod(), null, XamlMemberInvoker.UnknownInvoker));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(eventInfo.Name, eventInfo.GetAddMethod(), null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlMember(eventInfo.Name, eventInfo.GetAddMethod(), null, XamlMemberInvoker.UnknownInvoker));
    }


    [Theory]
    [InlineData("name", true)]
    [InlineData("_aA1e\u0300\u0903", true)]
    [InlineData("", false)]
    [InlineData(" name  ", false)]
    [InlineData("1name", false)]
    [InlineData("n#me", false)]
    [InlineData("n.me", false)]
    public void IsNameValid_Invoke_ReturnsExpected(string name, bool expected)
    {
        var member = new XamlMember(name, new XamlType(typeof(int), new XamlSchemaContext()), false);
        Assert.Equal(expected, member.IsNameValid);
        Assert.Equal(member.IsNameValid, member.IsNameValid);
    }

    [Fact]
    public void PreferredXamlNamespace_Unknown_ReturnsExpected()
    {
        var member = new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false);
        Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml", member.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_UnderlyingMember_ReturnsExpected()
    {
        var member = new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext());
        Assert.Equal("clr-namespace:System.Xaml.Tests;assembly=System.Xaml.Tests", member.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsNonEmpty_ReturnsFirstElement()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()) { GetXamlNamespacesResult = new string[] { "namespace1", "namespace2" } };
        Assert.Equal("namespace1", member.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsEmpty_ReturnsNull()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()) { GetXamlNamespacesResult = Array.Empty<string>() };
        Assert.Null(member.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsNull_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()) { GetXamlNamespacesResult = null };
        Assert.Throws<NullReferenceException>(() => member.PreferredXamlNamespace);
    }

    [Fact]
    public void LookupCustomAttributeProvider_Invoke_ReturnsNull()
    {
        var member = new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false);
        Assert.Null(member.LookupCustomAttributeProviderEntry());
    }

    public static IEnumerable<object?[]> LookupDeferringLoader_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };
        yield return new object?[] { new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlDeferLoadAttribute(typeof(int), typeof(string)) }
                }
            },
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlDeferLoadAttribute(typeof(int), typeof(string)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.TypeXamlDeferLoadAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.StringXamlDeferLoadAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.ClassWithTypeXamlDeferLoadAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.ClassWithStringXamlDeferLoadAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.TypeXamlDeferLoadAttribute))!, new XamlSchemaContext())
            {
                LookupDeferringLoaderResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupDeferringLoader_TestData))]
    public void LookupDeferringLoader_Invoke_ReturnsExpected(SubXamlMember member, XamlValueConverter<XamlDeferringLoader> expected)
    {
        Assert.Equal(expected, member.LookupDeferringLoaderEntry());
        Assert.Equal(expected, member.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.TypeXamlDeferLoadAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupDeferringLoaderEntry());
        Assert.Throws<NullReferenceException>(() => member.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.TypeXamlDeferLoadAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupDeferringLoaderEntry());
        Assert.Throws<NullReferenceException>(() => member.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.TypeXamlDeferLoadAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.LookupDeferringLoaderEntry());
        Assert.Throws<InvalidCastException>(() => member.DeferringLoader);
    }

    [Theory]
    [InlineData(nameof(XamlDeferLoadData.NullLoaderTypeXamlDeferLoadAttribute))]
    [InlineData(nameof(XamlDeferLoadData.NullContentTypeXamlDeferLoadAttribute))]
    [InlineData(nameof(XamlDeferLoadData.NoSuchLoaderTypeNameXamlDeferLoadAttribute))]
    [InlineData(nameof(XamlDeferLoadData.NoSuchContentTypeNameXamlDeferLoadAttribute))]
    public void LookupDeferringLoaderEntry_InvalidParametersType_ThrowsXamlSchemaException(string propertyName)
    {
        var member = new SubXamlMember(typeof(XamlDeferLoadData).GetProperty(propertyName)!, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => member.LookupDeferringLoaderEntry());
        Assert.Throws<XamlSchemaException>(() => member.DeferringLoader);
    }

    [Theory]
    [InlineData(nameof(XamlDeferLoadData.NullLoaderTypeNameXamlDeferLoadAttribute))]
    [InlineData(nameof(XamlDeferLoadData.NullContentTypeNameXamlDeferLoadAttribute))]
    public void LookupDeferringLoaderEntry_NullParametersTypeNames_ThrowsXamlSchemaException(string propertyName)
    {
        var member = new SubXamlMember(typeof(XamlDeferLoadData).GetProperty(propertyName)!, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => member.LookupDeferringLoaderEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => member.DeferringLoader);
    }

    private class XamlDeferLoadData
    {
        [XamlDeferLoad(typeof(int), typeof(string))]
        public int TypeXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad("System.Int32", "System.String")]
        public int StringXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad(null!, typeof(string))]
        public int NullLoaderTypeXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad(typeof(int), null!)]
        public int NullContentTypeXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad(null!, "System.String")]
        public int NullLoaderTypeNameXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad("System.Int32", null!)]
        public int NullContentTypeNameXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad("NoSuchType", "System.String")]
        public int NoSuchLoaderTypeNameXamlDeferLoadAttribute { get; set; }

        [XamlDeferLoad("System.Int32", "NoSuchType")]
        public int NoSuchContentTypeNameXamlDeferLoadAttribute { get; set; }

        public XamlTypeTests.ClassWithTypeXamlDeferLoadAttribute? ClassWithTypeXamlDeferLoadAttribute { get; set; }
        public XamlTypeTests.ClassWithStringXamlDeferLoadAttribute? ClassWithStringXamlDeferLoadAttribute { get; set; }
    }

    public static IEnumerable<object?[]> LookupDependsOn_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };
        yield return new object?[] { new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DependsOnAttribute(nameof(DataClass.Property))!, new DependsOnAttribute(""), new DependsOnAttribute(nameof(DataClass.Event)) }
                }
            },
            new XamlMember[] { new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DependsOnAttribute(nameof(DataClass.Property))!, new DependsOnAttribute(""), new DependsOnAttribute(nameof(DataClass.Event)) }
                }
            },
            null
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
            {
                LookupDependsOnResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupDependsOn_TestData))]
    public void LookupDependsOn_Invoke_ReturnsExpected(SubXamlMember member, IList<XamlMember> expected)
    {
        Assert.Equal(expected, member.LookupDependsOnEntry());
        Assert.Equal(expected ?? Array.Empty<XamlMember>(), member.DependsOn);
    }

    [Fact]
    public void LookupDependsOn_XamlDirective_ReturnsEmpty()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Empty(directive.DependsOn);
    }

    [Fact]
    public void LookupDependsOn_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupDependsOnEntry());
        Assert.Throws<NullReferenceException>(() => member.DependsOn);
    }

    [Fact]
    public void LookupDependsOn_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupDependsOnEntry());
        Assert.Throws<NullReferenceException>(() => member.DependsOn);
    }

    [Fact]
    public void LookupDependsOn_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.LookupDependsOnEntry());
        Assert.Throws<InvalidCastException>(() => member.DependsOn);
    }

    [Fact]
    public void LookupDependsOn_NullMemberName_ThrowsArgumentNullException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DependsOnAttribute(null!) }
            }
        };
        Assert.Throws<ArgumentNullException>("key", () => member.LookupDependsOnEntry());
        Assert.Throws<ArgumentNullException>("key", () => member.DependsOn);
    }

    [Fact]
    public void LookupInvoker_Unknown_ReturnsExpected()
    {
        var member = new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false);
        Assert.Null(member.LookupInvokerEntry());
        Assert.Equal(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    [Fact]
    public void LookupInvoker_UnderlyingMember_ReturnsExpected()
    {
        var member = new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext());
        Assert.NotNull(member.LookupInvokerEntry());
        Assert.NotNull(member.Invoker);
    }

    [Fact]
    public void LookupInvoker_UnderlyingMemberWithInvoker_ReturnsExpected()
    {
        var invoker = new XamlMemberInvoker(new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()));
        var member = new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext(), invoker);
        Assert.NotNull(member.LookupInvokerEntry());
        Assert.NotEqual(invoker, member.LookupInvokerEntry());
        Assert.Equal(invoker, member.Invoker);
    }

    [Fact]
    public void LookupInvoker_NullResult_ReturnsExpected()
    {
        var member = new CustomXamlMember(typeof(XamlDeferLoadData).GetProperty(nameof(XamlDeferLoadData.ClassWithTypeXamlDeferLoadAttribute))!, new XamlSchemaContext())
        {
            LookupInvokerResult = null
        };
        Assert.Null(member.LookupInvokerEntry());
        Assert.Equal(XamlMemberInvoker.UnknownInvoker, member.Invoker);
    }

    [Fact]
    public void LookupInvoker_XamlDirective_ReturnsUnknown()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Equal(XamlMemberInvoker.UnknownInvoker, directive.Invoker);
    }

    public static IEnumerable<object[]> LookupIsAmbient_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), false };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), false };
        yield return new object[] { new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), false };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            true
        };
        yield return new object[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            false
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlMember(typeof(AmbientData).GetProperty(nameof(AmbientData.Property))!, new XamlSchemaContext()),
            true
        };

        yield return new object[]
        {
            new CustomXamlMember(typeof(AmbientData).GetProperty(nameof(AmbientData.Property))!, new XamlSchemaContext())
            {
                LookupIsAmbientResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsAmbient_TestData))]
    public void LookupIsAmbient_Invoke_ReturnsExpected(SubXamlMember member, bool expected)
    {
        Assert.Equal(expected, member.LookupIsAmbientEntry());
        Assert.Equal(expected, member.IsAmbient);
    }

    [Fact]
    public void LookupIsAmbient_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.False(directive.IsAmbient);
    }

    private class AmbientData
    {
        [AmbientAttribute]
        public bool Property { get; set; }
    }

    public static IEnumerable<object[]> LookupIsEvent_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), false, false };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), false, false };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, true
        };

        yield return new object[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupIsEventResult = true
            },
            true, false
        };
        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext())
            {
                LookupIsEventResult = false
            },
            false, true
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsEvent_TestData))]
    public void LookupIsEvent_Invoke_ReturnsExpected(SubXamlMember member, bool expectedLookup, bool expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupIsEventEntry());
        Assert.Equal(expectedGet, member.IsEvent);
    }

    public static IEnumerable<object[]> LookupIsReadPublic_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), true, true };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), true, true };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(PrivateDeclaringType).GetProperty(nameof(PrivateDeclaringType.Property))!, new XamlSchemaContext()),
            true, false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext())
            {
                LookupIsReadPublicResult = false
            },
            false, false
        };
        yield return new object[]
        {
            new CustomXamlMember(typeof(PrivateDeclaringType).GetProperty(nameof(PrivateDeclaringType.Property))!, new XamlSchemaContext())
            {
                LookupIsReadPublicResult = true
            },
            true, false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsReadPublic_TestData))]
    public void LookupIsReadPublic_Invoke_ReturnsExpected(SubXamlMember member, bool expectedLookup, bool expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupIsReadPublicEntry());
        Assert.Equal(expectedGet, member.IsReadPublic);
    }

    [Fact]
    public void LookupIsReadPublic_XamlDirective_ReturnsTrue()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.True(directive.IsReadPublic);
    }

    public static IEnumerable<object[]> LookupIsReadOnly_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), false };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), false };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };

        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext())
            {
                LookupIsReadOnlyResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsReadOnly_TestData))]
    public void LookupIsReadOnly_Invoke_ReturnsExpected(SubXamlMember member, bool expected)
    {
        Assert.Equal(expected, member.LookupIsReadOnlyEntry());
        Assert.Equal(expected, member.IsReadOnly);
    }

    [Fact]
    public void LookupIsReadOnly_XamlDirective_ReturnsFalse()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.False(directive.IsReadOnly);
    }

    public static IEnumerable<object[]> LookupIsUnknown_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), true, true };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), true, true };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupIsUnknownResult = true
            },
            true, true
        };
        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupIsUnknownResult = true
            },
            true, false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsUnknown_TestData))]
    public void LookupIsUnknown_Invoke_ReturnsExpected(SubXamlMember member, bool expectedLookup, bool expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupIsUnknownEntry());
        Assert.Equal(expectedGet, member.IsUnknown);
    }

    [Fact]
    public void LookupIsUnknown_XamlDirective_ReturnsTrue()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.True(directive.IsUnknown);
    }

    public static IEnumerable<object?[]> LookupIsWritePublic_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), true, true };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), true, true };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateSetProperty))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("InternalProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(PrivateDeclaringType).GetProperty(nameof(PrivateDeclaringType.Property))!, new XamlSchemaContext()),
            true, false
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext()),
            false, false
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true, true
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            true, true
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true, true
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext())
            {
                LookupIsWritePublicResult = false
            },
            false, false
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(PrivateDeclaringType).GetProperty(nameof(PrivateDeclaringType.Property))!, new XamlSchemaContext())
            {
                LookupIsWritePublicResult = true
            },
            true, false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsWritePublic_TestData))]
    public void LookupIsWritePublic_Invoke_ReturnsExpected(SubXamlMember member, bool expectedLookup, bool expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupIsWritePublicEntry());
        Assert.Equal(expectedGet, member.IsWritePublic);
    }

    [Fact]
    public void LookupIsWritePublic_XamlDirective_ReturnsTrue()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.True(directive.IsWritePublic);
    }

    public static IEnumerable<object[]> LookupIsWriteOnly_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), false };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), false };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateSetProperty))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, null, new XamlSchemaContext()),
            false
        };
        yield return new object[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true
        };

        yield return new object[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            true
        };

        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext())
            {
                LookupIsWriteOnlyResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsWriteOnly_TestData))]
    public void LookupIsWriteOnly_Invoke_ReturnsExpected(SubXamlMember member, bool expected)
    {
        Assert.Equal(expected, member.LookupIsWriteOnlyEntry());
        Assert.Equal(expected, member.IsWriteOnly);
    }

    [Fact]
    public void LookupIsWriteOnly_XamlDirective_ReturnsFalse()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.False(directive.IsWriteOnly);
    }

    public static IEnumerable<object?[]> LookupMarkupExtensionBracketCharacters_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };
        yield return new object?[] { new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), new Dictionary<char, char>() };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new MarkupExtensionBracketCharactersAttribute('a', 'b') }
                }
            },
            new Dictionary<char, char> { { 'a', 'b' } }
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new MarkupExtensionBracketCharactersAttribute('a', 'b') }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext()),
            new Dictionary<char, char> { { 'a', 'b' }, { 'c', 'd' } }
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
            {
                LookupMarkupExtensionBracketCharactersResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupMarkupExtensionBracketCharacters_TestData))]
    public void LookupMarkupExtensionBracketCharacters_Invoke_ReturnsExpected(SubXamlMember member, IReadOnlyDictionary<char, char> expected)
    {
        Assert.Equal(expected, member.LookupMarkupExtensionBracketCharactersEntry());
        Assert.Equal(expected, member.MarkupExtensionBracketCharacters);
    }

    [Fact]
    public void LookupMarkupExtensionBracketCharacters_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Null(directive.MarkupExtensionBracketCharacters);
    }

    [Fact]
    public void LookupMarkupExtensionBracketCharacters_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupMarkupExtensionBracketCharactersEntry());
        Assert.Throws<NullReferenceException>(() => member.MarkupExtensionBracketCharacters);
    }

    [Fact]
    public void LookupMarkupExtensionBracketCharacters_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupMarkupExtensionBracketCharactersEntry());
        Assert.Throws<NullReferenceException>(() => member.MarkupExtensionBracketCharacters);
    }

    [Fact]
    public void LookupMarkupExtensionBracketCharacters_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.LookupMarkupExtensionBracketCharactersEntry());
        Assert.Throws<InvalidCastException>(() => member.MarkupExtensionBracketCharacters);
    }

    [Fact]
    public void LookupMarkupExtensionBracketCharacters_DuplicateAttribute_ThrowsArgumentException()
    {
        var member = new SubXamlMember(typeof(MarkupExtensionBracketCharactersData).GetProperty(nameof(MarkupExtensionBracketCharactersData.DuplicateProperty))!, new XamlSchemaContext());
        Assert.Throws<ArgumentException>(() => member.LookupMarkupExtensionBracketCharactersEntry());
        Assert.Throws<ArgumentException>(() => member.MarkupExtensionBracketCharacters);
    }

    private class MarkupExtensionBracketCharactersData
    {
        [MarkupExtensionBracketCharacters('a', 'b')]
        [MarkupExtensionBracketCharacters('c', 'd')]
        public int Property { get; set; }

        [MarkupExtensionBracketCharacters('a', 'b')]
        [MarkupExtensionBracketCharacters('a', 'b')]
        public int DuplicateProperty { get; set; }
    }

    public static IEnumerable<object[]> LookupSerializationVisibility_TestData()
    {
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), DesignerSerializationVisibility.Visible };
        yield return new object[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), DesignerSerializationVisibility.Visible };
        yield return new object[] { new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), DesignerSerializationVisibility.Visible };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content) }
                }
            },
            DesignerSerializationVisibility.Content
        };
        yield return new object[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            DesignerSerializationVisibility.Visible
        };
        yield return new object[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content) }
                }
            },
            DesignerSerializationVisibility.Visible
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlMember(typeof(DesignerSerializationVisibilityData).GetProperty(nameof(DesignerSerializationVisibilityData.Property))!, new XamlSchemaContext()),
            DesignerSerializationVisibility.Content
        };
    }

    [Theory]
    [MemberData(nameof(LookupSerializationVisibility_TestData))]
    public void LookupSerializationVisibility_Invoke_ReturnsExpected(SubXamlMember member, DesignerSerializationVisibility expected)
    {
        Assert.Equal(expected, member.SerializationVisibility);
    }

    [Fact]
    public void LookupSerializationVisibility_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Equal(DesignerSerializationVisibility.Visible, directive.SerializationVisibility);
    }

    [Fact]
    public void LookupSerializationVisibility_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(DesignerSerializationVisibilityData).GetProperty(nameof(DesignerSerializationVisibilityData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.SerializationVisibility);
    }

    [Fact]
    public void LookupSerializationVisibility_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(DesignerSerializationVisibilityData).GetProperty(nameof(DesignerSerializationVisibilityData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.SerializationVisibility);
    }

    [Fact]
    public void LookupSerializationVisibility_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(DesignerSerializationVisibilityData).GetProperty(nameof(DesignerSerializationVisibilityData.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.SerializationVisibility);
    }

    private class DesignerSerializationVisibilityData
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public int Property { get; set; }
    }

    public static IEnumerable<object?[]> LookupTargetType_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), XamlLanguage.Object, XamlLanguage.Object };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            new XamlType(typeof(DataClass), new XamlSchemaContext()), new XamlType(typeof(DataClass), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(string), new XamlSchemaContext()), new XamlType(typeof(string), new XamlSchemaContext())
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            new XamlType(typeof(DataClass), new XamlSchemaContext()), new XamlType(typeof(DataClass), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext())
        };

        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true)
            {
                LookupUnderlyingMemberResult = typeof(AccessorClass).GetMethod(nameof(AccessorClass.ParameterlessSetMethod))!
            },
            XamlLanguage.Object, XamlLanguage.Object
        };

        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true)
            {
                LookupTargetTypeResult = null
            },
            null, XamlLanguage.Object
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext())
            {
                LookupTargetTypeResult = null
            },
            null, XamlLanguage.Object
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupTargetTypeResult = null
            },
            null, new XamlType(typeof(DataClass), new XamlSchemaContext())
        };
    }

    [Theory]
    [MemberData(nameof(LookupTargetType_TestData))]
    public void LookupTargetType_Invoke_ReturnsExpected(SubXamlMember member, XamlType expectedLookup, XamlType expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupTargetTypeEntry());
        Assert.Equal(expectedGet, member.TargetType);
    }

    [Fact]
    public void LookupTargetType_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Null(directive.TargetType);
    }

    public static IEnumerable<object?[]> LookupType_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(string), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            new XamlType(typeof(EventHandler), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(string), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!, new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };

        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupUnderlyingMemberResult = new CustomMethodInfo(typeof(AccessorClass).GetMethod(nameof(AccessorClass.StaticSetMethod))!)
                {
                    ReturnTypeResult = null
                }
            },
            new XamlType(typeof(string), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupUnderlyingMemberResult = typeof(AccessorClass).GetMethod(nameof(AccessorClass.VoidGetMethod))!
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext())
            {
                LookupTypeResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupType_TestData))]
    public void LookupType_Invoke_ReturnsExpected(SubXamlMember member, XamlType expected)
    {
        Assert.Equal(expected, member.LookupTypeEntry());
        Assert.Equal(expected ?? XamlLanguage.Object, member.Type);
    }

    [Fact]
    public void LookupType_XamlDirective_ReturnsObject()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Equal(XamlLanguage.Object, directive.Type);
    }

    public static IEnumerable<object?[]> LookupTypeConverter_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), new XamlValueConverter<TypeConverter>(null, XamlLanguage.Object), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), new XamlValueConverter<TypeConverter>(null, XamlLanguage.Object), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new TypeConverterAttribute(typeof(string)) }
                }
            },
            new XamlValueConverter<TypeConverter>(typeof(string), null), new XamlValueConverter<TypeConverter>(typeof(string), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            new XamlValueConverter<TypeConverter>(typeof(Int32Converter), null), new XamlValueConverter<TypeConverter>(typeof(Int32Converter), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new TypeConverterAttribute(typeof(int)) }
                }
            },
            new XamlValueConverter<TypeConverter>(null, XamlLanguage.Object), null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithTypeConverterAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null), new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.StructWithTypeConverterAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null), new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.InheritedClassWithTypeConverterAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null), new XamlValueConverter<TypeConverter>(typeof(int), null)
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithTypeConverterAttribute))!, new XamlSchemaContext())
            {
                LookupTypeConverterResult = null
            },
            null, null
        };
    }

    [Theory]
    [MemberData(nameof(LookupTypeConverter_TestData))]
    public void LookupTypeConverter_Invoke_ReturnsExpected(SubXamlMember member, XamlValueConverter<TypeConverter> expectedLookup, XamlValueConverter<TypeConverter> expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupTypeConverterEntry());
        Assert.Equal(expectedGet, member.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Null(directive.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithTypeConverterAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupTypeConverterEntry());
        Assert.Throws<NullReferenceException>(() => member.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithTypeConverterAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupTypeConverterEntry());
        Assert.Throws<NullReferenceException>(() => member.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithTypeConverterAttribute))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.LookupTypeConverterEntry());
        Assert.Throws<InvalidCastException>(() => member.TypeConverter);
    }

    [Theory]
    [InlineData(nameof(TypeConverterData.ClassWithNullTypeConverterAttribute))]
    [InlineData(nameof(TypeConverterData.ClassWithDefaultTypeConverterAttribute))]
    public void LookupTypeConverter_InvalidParametersType_ThrowsXamlSchemaException(string propertyName)
    {
        var member = new SubXamlMember(typeof(TypeConverterData).GetProperty(propertyName)!, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => member.LookupTypeConverterEntry());
        Assert.Throws<XamlSchemaException>(() => member.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullStringType_ThrowsArgumentNullException()
    {
        var member = new SubXamlMember(typeof(TypeConverterData).GetProperty(nameof(TypeConverterData.ClassWithNullStringTypeConverterAttribute))!, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => member.LookupTypeConverterEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => member.TypeConverter);
    }

    private class TypeConverterData
    {
        public XamlTypeTests.ClassWithTypeConverterAttribute? ClassWithTypeConverterAttribute { get; set; }
        public XamlTypeTests.StructWithTypeConverterAttribute? StructWithTypeConverterAttribute { get; set; }
        public XamlTypeTests.ClassWithStringTypeConverterAttribute? ClassWithStringTypeConverterAttribute { get; set; }
        public XamlTypeTests.InheritedClassWithTypeConverterAttribute? InheritedClassWithTypeConverterAttribute { get; set; }
        public XamlTypeTests.ClassWithNullStringTypeConverterAttribute? ClassWithNullStringTypeConverterAttribute { get; set; }
        public XamlTypeTests.ClassWithNullTypeConverterAttribute? ClassWithNullTypeConverterAttribute { get; set; }
        public XamlTypeTests.ClassWithDefaultTypeConverterAttribute? ClassWithDefaultTypeConverterAttribute { get; set; }
    }

    public static IEnumerable<object?[]> LookupUnderlyingGetter_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty(nameof(DataClass.Property))!.GetGetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!.GetGetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.SetOnlyProperty))!, new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupUnderlyingGetterResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupUnderlyingGetter_TestData))]
    public void LookupUnderlyingGetter_Invoke_ReturnsExpected(SubXamlMember member, MethodInfo expected)
    {
        Assert.Equal(expected, member.LookupUnderlyingGetterEntry());
    }

    public static IEnumerable<object?[]> LookupUnderlyingMember_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null, null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null, null };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty(nameof(DataClass.Property))!, typeof(DataClass).GetProperty(nameof(DataClass.Property))!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            typeof(DataClass).GetEvent(nameof(DataClass.Event)), typeof(DataClass).GetEvent(nameof(DataClass.Event))!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext())
            {
                LookupUnderlyingMemberResult = null
            },
            null, typeof(DataClass).GetEvent(nameof(DataClass.Event))!
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupUnderlyingMemberResult = null
            },
            null, null
        };
    }

    [Theory]
    [MemberData(nameof(LookupUnderlyingMember_TestData))]
    public void LookupUnderlyingMember_Invoke_ReturnsExpected(SubXamlMember member, MemberInfo expectedLookup, MemberInfo expectedGet)
    {
        Assert.Equal(expectedLookup, member.LookupUnderlyingMemberEntry());
        Assert.Equal(expectedGet, member.UnderlyingMember);
    }

    [Fact]
    public void UnderlyingMember_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Null(directive.UnderlyingMember);
    }

    public static IEnumerable<object?[]> LookupUnderlyingSetter_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty(nameof(DataClass.Property))!.GetSetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty(nameof(DataClass.PrivateGetProperty))!.GetSetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            typeof(DataClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic)!.GetSetMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", null, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!
        };

        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event))!, new XamlSchemaContext()),
            typeof(DataClass).GetEvent(nameof(DataClass.Event))!.GetAddMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(DataClass).GetEvent("PrivateEvent", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
            typeof(DataClass).GetEvent("PrivateEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetAddMethod(true)!
        };
        yield return new object?[]
        {
            new SubXamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupUnderlyingSetterResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupUnderlyingSetter_TestData))]
    public void LookupUnderlyingSetter_Invoke_ReturnsExpected(SubXamlMember member, MethodInfo expected)
    {
        Assert.Equal(expected, member.LookupUnderlyingSetterEntry());
    }

    public static IEnumerable<object?[]> LookupValueSerializer_TestData()
    {
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false), null };
        yield return new object?[] { new SubXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ValueSerializerAttribute(typeof(string)) }
                }
            },
            new XamlValueConverter<ValueSerializer>(typeof(string), null)
        };
        yield return new object?[]
        {
            new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ValueSerializerAttribute(typeof(int)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.ClassWithValueSerializerAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.StructWithValueSerializerAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.InheritedClassWithValueSerializerAttribute))!, new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };

        yield return new object?[]
        {
            new CustomXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.ClassWithValueSerializerAttribute))!, new XamlSchemaContext())
            {
                LookupValueSerializerResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupValueSerializer_TestData))]
    public void LookupValueSerializer_Invoke_ReturnsExpected(SubXamlMember member, XamlValueConverter<ValueSerializer> expected)
    {
        Assert.Equal(expected, member.LookupValueSerializerEntry());
        Assert.Equal(expected, member.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_XamlDirective_ReturnsNull()
    {
        var directive = new XamlDirective("namespace", "name");
        Assert.Null(directive.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupValueSerializerEntry());
        Assert.Throws<NullReferenceException>(() => member.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => member.LookupValueSerializerEntry());
        Assert.Throws<NullReferenceException>(() => member.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var member = new CustomXamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => member.LookupValueSerializerEntry());
        Assert.Throws<InvalidCastException>(() => member.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_InvalidParametersType_ThrowsXamlSchemaException()
    {
        var member = new SubXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.ClassWithNullValueSerializerAttribute))!, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => member.LookupValueSerializerEntry());
        Assert.Throws<XamlSchemaException>(() => member.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullStringType_ThrowsArgumentNullException()
    {
        var member = new SubXamlMember(typeof(ValueSerializerData).GetProperty(nameof(ValueSerializerData.ClassWithNullStringValueSerializerAttribute))!, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => member.LookupValueSerializerEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => member.ValueSerializer);
    }

    private class ValueSerializerData
    {
        public XamlTypeTests.ClassWithValueSerializerAttribute? ClassWithValueSerializerAttribute { get; set; }
        public XamlTypeTests.StructWithValueSerializerAttribute? StructWithValueSerializerAttribute { get; set; }
        public XamlTypeTests.ClassWithStringValueSerializerAttribute? ClassWithStringValueSerializerAttribute { get; set; }
        public XamlTypeTests.InheritedClassWithValueSerializerAttribute? InheritedClassWithValueSerializerAttribute { get; set; }
        public XamlTypeTests.ClassWithNullStringValueSerializerAttribute? ClassWithNullStringValueSerializerAttribute { get; set; }
        public XamlTypeTests.ClassWithNullValueSerializerAttribute? ClassWithNullValueSerializerAttribute { get; set; }
    }

    [Fact]
    public void GetXamlNamespaces_Invoke_ReturnsExpected()
    {
        var member = new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext());
        Assert.Equal(member.DeclaringType.GetXamlNamespaces(), member.GetXamlNamespaces());
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var member = new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext());
        yield return new object?[] { member, member, true };
        yield return new object?[] { member, new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()), true };
        yield return new object?[] { member, new XamlMember(typeof(DesignerSerializationVisibilityData).GetProperty(nameof(DesignerSerializationVisibilityData.Property))!, new XamlSchemaContext()), false };
        yield return new object?[] { member, new XamlMember(typeof(AmbientData).GetProperty(nameof(AmbientData.Property))!, new XamlSchemaContext()), false };
        yield return new object?[] { member, new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()), false };
        yield return new object?[] { member, new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false), false };
        yield return new object?[] { member, new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()), false };
        yield return new object?[] { member, new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()), false };

        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember(typeof(DataClass).GetEvent("PrivateEvent", BindingFlags.Instance | BindingFlags.NonPublic), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            false
        };
        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };

        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            true
        };
        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("other", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            false
        };
        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false),
            false
        };
        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), true),
            false
        };
        yield return new object?[]
        {
            new XamlMember("Property", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember("Event", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.GetMethod))!, typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlMember("name", new XamlType(typeof(DataClass), new XamlSchemaContext()), false),
            new XamlMember("name", typeof(AccessorClass).GetMethod(nameof(AccessorClass.SetMethod))!, new XamlSchemaContext()),
            false
        };

        yield return new object?[] { member, null, false };
        yield return new object?[] { member, new object(), false };
        yield return new object?[] { null, member, false };
        yield return new object?[] { null, null, true };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(XamlMember type, object obj, bool expected)
    {
        XamlMember? other = obj as XamlMember;
        if (type != null)
        {
            Assert.Equal(expected, type.Equals(obj));
            Assert.Equal(expected, type.Equals(other));
        }

        Assert.Equal(expected, type == other);
        Assert.Equal(!expected, type != other);
    }

    public static IEnumerable<object[]> GetHashCode_TestData()
    {
        yield return new object[] { new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false) };
        yield return new object[] { new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), true) };

        yield return new object[] { new XamlMember(typeof(DataClass).GetProperty(nameof(DataClass.Property))!, new XamlSchemaContext()) };
        yield return new object[]
        {
            new XamlMember(new CustomPropertyInfo(typeof(DataClass).GetProperty(nameof(DataClass.Property))!)
            {
                NameResult = null
            }, new XamlSchemaContext())
        };
        yield return new object[] { new XamlMember(typeof(DataClass).GetEvent(nameof(DataClass.Event)), new XamlSchemaContext()) };
    }

    [Theory]
    [MemberData(nameof(GetHashCode_TestData))]
    public void GetHashCode_Invoke_ReturnsExpected(XamlMember member)
    {
        Assert.Equal(member.GetHashCode(), member.GetHashCode());
    }

    private class CustomAttributeProvider : ICustomAttributeProvider
    {
        public Func<Type, bool, object[]>? GetCustomAttributesAction { get; set; }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (GetCustomAttributesAction is null)
            {
                throw new NotImplementedException();
            }

            return GetCustomAttributesAction(attributeType, inherit);
        }

        public object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

        public Func<Type, bool, bool>? IsDefinedAction { get; set; }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            if (IsDefinedAction is null)
            {
                throw new NotImplementedException();
            }

            return IsDefinedAction(attributeType, inherit);
        }
    }

    private class CustomPropertyInfo : PropertyInfo
    {
        protected PropertyInfo DelegatingProperty { get; }

        public CustomPropertyInfo(PropertyInfo delegatingProperty)
        {
            DelegatingProperty = delegatingProperty;
        }

        public override PropertyAttributes Attributes => DelegatingProperty.Attributes;

        public override bool CanWrite => DelegatingProperty.CanWrite;

        public override bool CanRead => DelegatingProperty.CanRead;

        public override Type DeclaringType => DelegatingProperty.DeclaringType!;

        public override MemberTypes MemberType => DelegatingProperty.MemberType;

        public Optional<string?> NameResult { get; set; }
        public override string Name => NameResult.Or(DelegatingProperty.Name)!;

        public override Type PropertyType => DelegatingProperty.PropertyType;

        public override Type ReflectedType => DelegatingProperty.ReflectedType!;

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return DelegatingProperty.GetAccessors(nonPublic);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return DelegatingProperty.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return DelegatingProperty.GetCustomAttributes(attributeType, inherit);
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            return DelegatingProperty.GetGetMethod(nonPublic);
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return DelegatingProperty.GetIndexParameters();
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            return DelegatingProperty.GetSetMethod(nonPublic);
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            return DelegatingProperty.GetValue(obj, invokeAttr, binder, index, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return DelegatingProperty.IsDefined(attributeType, inherit);
        }
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            DelegatingProperty.SetValue(obj, value, invokeAttr, binder, index, culture);
        }
    }

#pragma warning disable IDE0051 // Remove unused private members
    public class DataClass
    {
        public int Property { get; set; }

        public int GetOnlyProperty { get { return 1; } }
        public int SetOnlyProperty { set { } }

        internal int InternalProperty { get; set; }

        protected int ProtectedProperty { get; set; }

        public int PrivateGetProperty { private get; set; }
        public int PrivateSetProperty { get; private set; }
        private int PrivateProperty { get; set; }

        public event EventHandler Event
        {
            add { }
            remove { }
        }

        private event EventHandler PrivateEvent
        {
            add { }
            remove { }
        }
    }

    private class PrivateDeclaringType
    {
        public int Property { get; set; }
    }

    public class AccessorClass
    {
        public int GetMethod(string sender) => 0;
        public static int StaticGetMethod(object sender) => 0;
        public void VoidGetMethod(object sender) { }
        public int ParameterlessGetMethod() => 0;
        public int TooManyParametersGetMethod(object sender, object extra) => 0;

        public void SetMethod(int sender, string value) { }
        public static int StaticSetMethod(object sender, string value) => 0;
        public void ParameterlessSetMethod() { }
        public void TooManyParamtersSetMethod(object sender, object value, object extra) { }
    }
#pragma warning restore IDE0051 // Remove unused private members
}
