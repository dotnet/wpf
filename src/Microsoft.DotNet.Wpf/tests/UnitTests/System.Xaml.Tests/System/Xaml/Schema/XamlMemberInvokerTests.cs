// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Xaml.Schema.Tests;

public class XamlMemberInvokerTests
{
    [Fact]
    public void Ctor_Default()
    {
        var invoker = new SubMemberInvoker();
        Assert.Null(invoker.UnderlyingGetter);
        Assert.Null(invoker.UnderlyingSetter);
    }

    [Fact]
    public void Ctor_XamlMemberUnknown()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var member = new XamlMember("name", type, false);
        var invoker = new XamlMemberInvoker(member);
        Assert.Null(invoker.UnderlyingGetter);
        Assert.Null(invoker.UnderlyingSetter);
    }

    [Fact]
    public void Ctor_XamlMemberWithGetterAndSetter()
    {
        MethodInfo getter = typeof(TestClass).GetMethod(nameof(TestClass.StaticGetter))!;
        MethodInfo setter = typeof(TestClass).GetMethod(nameof(TestClass.StaticSetter))!;
        var member = new XamlMember("StaticProperty", getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(getter, invoker.UnderlyingGetter);
        Assert.Equal(setter, invoker.UnderlyingSetter);
    }

    [Fact]
    public void Ctor_NullMember_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("member", () => new XamlMemberInvoker(null));
    }

    [Fact]
    public void UnknownInvoker_Get_ReturnsExpected()
    {
        XamlMemberInvoker invoker = XamlMemberInvoker.UnknownInvoker;
        Assert.Same(invoker, XamlMemberInvoker.UnknownInvoker);
        Assert.Null(invoker.UnderlyingGetter);
        Assert.Null(invoker.UnderlyingSetter);
    }

    public static IEnumerable<object[]> UnknownInvoker_TestData()
    {
        yield return new object[] { XamlMemberInvoker.UnknownInvoker };
        yield return new object[] { new SubMemberInvoker() };

        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var member = new XamlMember("name", type, false);
        yield return new object[] { new XamlMemberInvoker(member) };
    }

    [Fact]
    public void GetValue_InvokeNonStatic_ReturnsExpected()
    {
        var instance = new TestClass { Property = 1 };
        PropertyInfo property = typeof(TestClass).GetProperty(nameof(TestClass.Property))!;
        var member = new XamlMember(property, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(1, invoker.GetValue(instance));
    }

    [Fact]
    public void GetValue_InvokeStatic_ReturnsExpected()
    {
        var instance = new TestClass();
        MethodInfo getter = typeof(TestClass).GetMethod(nameof(TestClass.StaticGetter))!;
        MethodInfo setter = typeof(TestClass).GetMethod(nameof(TestClass.StaticSetter))!;
        var member = new XamlMember("StaticProperty", getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(2, invoker.GetValue(instance));
    }
    
    [Fact]
    public void GetValue_NullInstance_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("instance", () => XamlMemberInvoker.UnknownInvoker.GetValue(null));
    }
    
    [Fact]
    public void GetValue_NoUnderlyingGetter_ThrowsNotSupportedException()
    {
        var instance = new TestClass();
        MethodInfo setter = typeof(TestClass).GetMethod(nameof(TestClass.StaticSetter))!;
        var member = new XamlMember("StaticProperty", null, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Throws<NotSupportedException>(() => invoker.GetValue(instance));
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void GetValue_UnknownInvoker_ThrowsNotSupportedException(XamlMemberInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.GetValue("value"));
    }

    [Fact]
    public void SetValue_InvokeNonStatic_Success()
    {
        var instance = new TestClass();
        PropertyInfo property = typeof(TestClass).GetProperty(nameof(TestClass.Property))!;
        var member = new XamlMember(property, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        invoker.SetValue(instance, 1);
        Assert.Equal(1, instance.Property);
    }

    [Fact]
    public void SetValue_InvokeStatic_Success()
    {
        var instance = new TestClass();
        MethodInfo getter = typeof(TestClass).GetMethod(nameof(TestClass.StaticGetter))!;
        MethodInfo setter = typeof(TestClass).GetMethod(nameof(TestClass.StaticSetter))!;
        var member = new XamlMember("StaticProperty", getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        invoker.SetValue(instance, 1);
        Assert.Equal(1, TestClass.SetStaticProperty);
    }
    
    [Fact]
    public void SetValue_NullInstance_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("instance", () => XamlMemberInvoker.UnknownInvoker.SetValue(null, "value"));
    }
    
    [Fact]
    public void SetValue_NoUnderlyingSetter_ThrowsNotSupportedException()
    {
        var instance = new TestClass();
        MethodInfo getter = typeof(TestClass).GetMethod(nameof(TestClass.StaticGetter))!;
        var member = new XamlMember("StaticProperty", getter, null, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Throws<NotSupportedException>(() => invoker.SetValue(instance, 1));
    }
    
    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void SetValue_UnknownInvoker_ThrowsNotSupportedException(XamlMemberInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.SetValue(new object(), "value"));
    }

    [Theory]
    [InlineData(nameof(SerializeClass.PublicProperty), true)]
    [InlineData(nameof(SerializeClass.PublicProperty), false)]
    [InlineData(nameof(SerializeClass.PrivateProperty), true)]
    [InlineData(nameof(SerializeClass.PrivateProperty), false)]
    public void ShouldSerializeValue_NonStatic_Success(string name, bool result)
    {
        var instance = new SerializeClass { ShouldSerializeResult = result };
        PropertyInfo property = typeof(SerializeClass).GetProperty(name)!;
        var member = new XamlMember(property, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(result ? ShouldSerializeResult.True : ShouldSerializeResult.False, invoker.ShouldSerializeValue(instance));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSerializeValue_StaticPublic_Success(bool result)
    {
        SerializeClass.ShouldSerializeAttachablePublicPropertyResult = result;
        
        var instance = new SerializeClass();
        MethodInfo getter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticGetter))!;
        MethodInfo setter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticSetter))!;
        var member = new XamlMember(nameof(SerializeClass.AttachablePublicProperty), getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(result ? ShouldSerializeResult.True : ShouldSerializeResult.False, invoker.ShouldSerializeValue(instance));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSerializeValue_StaticPrivate_Success(bool result)
    {
        SerializeClass.ShouldSerializeAttachablePrivatePropertyResult = result;
        
        var instance = new SerializeClass();
        MethodInfo getter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticGetter))!;
        MethodInfo setter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticSetter))!;
        var member = new XamlMember(nameof(SerializeClass.AttachablePrivateProperty), getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(result ? ShouldSerializeResult.True : ShouldSerializeResult.False, invoker.ShouldSerializeValue(instance));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldSerializeValue_StaticNoUnderlyingType_Success(bool result)
    {
        SerializeClass.ShouldSerializeAttachableCustomPropertyResult = result;
        
        var instance = new SerializeClass();
        MethodInfo getter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticGetter))!;
        MethodInfo setter = typeof(SerializeClass).GetMethod(nameof(SerializeClass.StaticSetter))!;
        var member = new CustomTargetType(nameof(SerializeClass.AttachableCustomProperty), getter, setter, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(result ? ShouldSerializeResult.True : ShouldSerializeResult.False, invoker.ShouldSerializeValue(instance));
    }

    [Fact]
    public void ShouldSerializeValue_NoSuchMethod_ReturnsDefault()
    {
        var instance = new SerializeClass();
        PropertyInfo property = typeof(SerializeClass).GetProperty(nameof(SerializeClass.NoSuchMethodProperty))!;
        var member = new XamlMember(property, new XamlSchemaContext());
        var invoker = new XamlMemberInvoker(member);
        Assert.Equal(ShouldSerializeResult.Default, invoker.ShouldSerializeValue(instance));
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void ShouldSerializeValue_Unknown_ReturnsDefault(XamlMemberInvoker invoker)
    {
        Assert.Equal(ShouldSerializeResult.Default, invoker.ShouldSerializeValue(null));
    }

#pragma warning disable IDE0060, IDE0051 // Remove unused parameter, Remove unused private members
    private class SubMemberInvoker : XamlMemberInvoker
    {
        public SubMemberInvoker() : base() { }
    }

    private class TestClass
    {
        public int Property { get; set; }
        public static int GetStaticProperty { get; set; } = 2;
        public static int SetStaticProperty { get; set; }

        public static int StaticGetter(TestClass instance) => GetStaticProperty;
        public static int StaticSetter(TestClass instance, int value) => SetStaticProperty = value;
    }

    private class SerializeClass
    {
        public int PublicProperty { get; set; }
        public int PrivateProperty { get; set; }
        public int NoSuchMethodProperty { get; set; }

        public static int AttachablePublicProperty { get; set; }
        public static int AttachablePrivateProperty { get; set; }
        public static int AttachableCustomProperty { get; set; }

        public bool ShouldSerializeResult { get; set; }
        public bool ShouldSerializePublicProperty() => ShouldSerializeResult;
        private bool ShouldSerializePrivateProperty() => ShouldSerializeResult;

        public static bool ShouldSerializeAttachablePrivatePropertyResult { get; set; }
        public static bool ShouldSerializeAttachablePublicProperty(SerializeClass instance) => ShouldSerializeAttachablePublicPropertyResult;

        public static bool ShouldSerializeAttachablePublicPropertyResult { get; set; }
        private static bool ShouldSerializeAttachablePrivateProperty(SerializeClass instance) => ShouldSerializeAttachablePrivatePropertyResult;

        public static int StaticGetter(SerializeClass instance) => 0;
        public static void StaticSetter(SerializeClass instance, int value) { }
    
        public static bool ShouldSerializeAttachableCustomPropertyResult { get; set; }
        private static bool ShouldSerializeAttachableCustomProperty(object instance) => ShouldSerializeAttachableCustomPropertyResult;
    }
    
    private class CustomTargetType : XamlMember
    {
        public CustomTargetType(string attachablePropertyName, MethodInfo getter, MethodInfo setter, XamlSchemaContext schemaContext) : base(attachablePropertyName, getter, setter, schemaContext)
        {
        }

        protected override XamlType LookupTargetType()
        {
            return new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        }
    }
#pragma warning restore IDE0060, IDE0051 // Remove unused parameter, Remove unused private members
}
