// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Windows.Markup.Tests;

public class StaticExtensionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var extension = new StaticExtension();
        Assert.Null(extension.Member);
        Assert.Null(extension.MemberType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("member")]
    public void Ctor_String(string member)
    {
        var extension = new StaticExtension(member);
        Assert.Equal(member, extension.Member);
        Assert.Null(extension.MemberType);
    }

    [Fact]
    public void Ctor_NullMember_ThrowsArgumemtNullException()
    {
        Assert.Throws<ArgumentNullException>("member", () => new StaticExtension(null));
    }

    [Theory]
    [InlineData("s_inheritedField", 1)]
    [InlineData("InheritedProperty", 2)]
    [InlineData("s_field", 3)]
    [InlineData("StaticProperty", 4)]
    public void ProvideValue_ValidMemberType_ReturnsExpected(string member, object expected)
    {
        var extension = new StaticExtension(member) { MemberType = typeof(CustomType) };
        Assert.Equal(expected, extension.ProvideValue(null));
    }

    [Theory]
    [InlineData("s_inheritedField", 1)]
    [InlineData("InheritedProperty", 2)]
    [InlineData("s_field", 3)]
    [InlineData("StaticProperty", 4)]
    public void ProvideValue_ValidResolvedType_ReturnsExpected(string member, object expected)
    {
        var extension = new StaticExtension("Type." + member);
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName => typeof(CustomType)
            }
        };
        Assert.Equal(expected, extension.ProvideValue(provider));
    }

    [Fact]
    public void ProvideValue_EnumMemberType_ReturnsExpected()
    {
        var extension = new StaticExtension("Red")
        {
            MemberType = typeof(ConsoleColor)
        };
        Assert.Equal(ConsoleColor.Red, extension.ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_EnumResolvedType_ReturnsExpected()
    {
        var extension = new StaticExtension("Type.Red");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName => typeof(ConsoleColor)
            }
        };
        Assert.Equal(ConsoleColor.Red, extension.ProvideValue(provider));
    }

    [Fact]
    public void ProvideValue_NullMember_ThrowsInvalidOperationException()
    {
        var extension = new StaticExtension();
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(null));
    }

    [Theory]
    [InlineData("member")]
    [InlineData(".member")]
    [InlineData("type.")]
    public void ProvideValue_InvalidMember_ThrowsArgumentException(string member)
    {
        var extension = new StaticExtension(member);
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName => typeof(CustomType)
            }
        };
        Assert.Throws<ArgumentException>(() => extension.ProvideValue(provider));
    }

    [Fact]
    public void ProvideValue_NullServiceProvider_ThrowsArgumentNullException()
    {
        var extension = new StaticExtension("type.member");
        Assert.Throws<ArgumentNullException>("serviceProvider", () => extension.ProvideValue(null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void ProvideValue_InvalidTypeResolver_ThrowsArgumentException(object? service)
    {
        var extension = new StaticExtension("type.member");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => service!
        };
        Assert.Throws<ArgumentException>(() => extension.ProvideValue(provider));
    }

    [Theory]
    [InlineData("s_privateField")]
    [InlineData("_field")]
    [InlineData("PrivateStaticProperty")]
    [InlineData("Property")]
    public void ProvideValue_NoSuchFieldOrPropertyOnMemberType_ThrowsArgumentException(string member)
    {
        var extension = new StaticExtension(member) { MemberType = typeof(CustomType) };
        Assert.Throws<ArgumentException>(() => extension.ProvideValue(null));
    }

    [Theory]
    [InlineData("s_privateField")]
    [InlineData("_field")]
    [InlineData("PrivateStaticProperty")]
    [InlineData("Property")]
    public void ProvideValue_NoSuchFieldOrPropertyOnResolvedType_ThrowsArgumentException(string member)
    {
        var extension = new StaticExtension(member);
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName =>
                {
                    Assert.Equal(member, qualifiedTypeName);
                    return typeof(CustomType);
                }
            }
        };
        Assert.Throws<ArgumentException>(() => extension.ProvideValue(provider));
    }

    [Theory]
    [InlineData("")]
    [InlineData("member")]
    public void Member_Set_GetReturnsExpected(string value)
    {
        var extension = new StaticExtension { Member = value };
        Assert.Equal(value, extension.Member);
    }

    [Fact]
    public void Member_SetNull_ThrowsArgumemtNullException()
    {
        var extension = new StaticExtension();
        Assert.Throws<ArgumentNullException>("value", () => extension.Member = null);
    }

    [Theory]
    [InlineData(typeof(int))]
    public void MemberType_Set_GetReturnsExpected(Type value)
    {
        var extension = new StaticExtension { MemberType = value };
        Assert.Equal(value, extension.MemberType);
    }

    [Fact]
    public void MemberType_SetNull_ThrowsArgumemtNullException()
    {
        var extension = new StaticExtension();
        Assert.Throws<ArgumentNullException>("value", () => extension.MemberType = null);
    }

    [Theory]
    [InlineData(typeof(InstanceDescriptor), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void StaticExtensionConverter_CanConvertTo_ReturnsExpected(Type? type, bool expected)
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Equal(expected, converter.CanConvertTo(type));
    }

    [Fact]
    public void StaticExtensionConverter_ConvertToInstanceDescriptor_ReturnsExpected()
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(extension, typeof(InstanceDescriptor)));
        Assert.Equal(new Type[] { typeof(string) }, Assert.IsAssignableFrom<ConstructorInfo>(descriptor.MemberInfo).GetParameters().Select(p => p.ParameterType));
        Assert.Equal(new string[] { "member" }, descriptor.Arguments);
    }

    [Fact]
    public void StaticExtensionConverter_ConvertToString_ReturnsExpected()
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(extension, typeof(InstanceDescriptor)));
        Assert.Equal(extension.ToString(), converter.ConvertTo(extension, typeof(string)));
    }

    [Fact]
    public void StaticExtensionConverter_ConvertToNotStaticExtension_ThrowsArgumentException()
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Throws<ArgumentException>(() => converter.ConvertTo(1, typeof(InstanceDescriptor)));
    }

    [Fact]
    public void StaticExtensionConverter_ConvertToInvalidType_ThrowsNotSupportedException()
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(extension, typeof(int)));
    }

    private class CustomServiceProvider : IServiceProvider
    {
        public Func<Type, object>? ServiceAction { get; set; }

        public object GetService(Type serviceType) =>
            ServiceAction is null ? throw new NotImplementedException() : ServiceAction(serviceType);
    }

    private class CustomXamlTypeResolver : IXamlTypeResolver
    {
        public Func<string, Type>? ResolveAction { get; set; }

        public Type Resolve(string qualifiedTypeName) =>
            ResolveAction is null ? throw new NotImplementedException() : ResolveAction(qualifiedTypeName);
    }

    public class BaseType
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static int s_inheritedField = 1;
#pragma warning disable IDE1006 // Naming Styles
        public static int InheritedProperty = 2;
#pragma warning restore IDE1006
#pragma warning restore CA2211
    }

#pragma warning disable 0169
#pragma warning disable CA1051 // Do not declare visible instance fields
    public class CustomType : BaseType
    {
#pragma warning disable CA2211 // Non-constant fields should not be visible
        public static int s_field = 3;
#pragma warning restore CA2211
        public static int StaticProperty { get; set; } = 4;

#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
        private static int s_privateField;
#pragma warning restore IDE0051
#pragma warning restore IDE0044
#pragma warning restore CA1823
        public int _field;

#pragma warning disable IDE0051 // Remove unused private members
        private static int PrivateStaticProperty { get; set; }
#pragma warning restore IDE0051
        public int Property { get; set; }
    }
#pragma warning restore CA1051
#pragma warning restore 0169
}
