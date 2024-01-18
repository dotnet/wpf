// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Windows.Markup.Tests;

public class TypeExtensionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var extension = new TypeExtension();
        Assert.Null(extension.Type);
        Assert.Null(extension.TypeName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("typeName")]
    public void Ctor_String(string typeName)
    {
        var extension = new TypeExtension(typeName);
        Assert.Null(extension.Type);
        Assert.Equal(typeName, extension.TypeName);
    }

    [Fact]
    public void Ctor_NullTypeName_ThrowsArgumemtNullException()
    {
        Assert.Throws<ArgumentNullException>("typeName", () => new TypeExtension((string)null!));
    }

    [Theory]
    [InlineData(typeof(int))]
    public void Ctor_Type(Type type)
    {
        var extension = new TypeExtension(type);
        Assert.Equal(type, extension.Type);
        Assert.Null(extension.TypeName);
    }

    [Fact]
    public void Ctor_NullType_ThrowsArgumemtNullException()
    {
        Assert.Throws<ArgumentNullException>("type", () => new TypeExtension((Type)null!));
    }

    [Fact]
    public void ProvideValue_HasType_ReturnsExpected()
    {
        var extension = new TypeExtension(typeof(int));
        Assert.Equal(typeof(int), extension.ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_HasTypeName_ReturnsExpected()
    {
        var extension = new TypeExtension("Type");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName =>
                {
                    Assert.Equal("Type", qualifiedTypeName);
                    return typeof(int);
                }
            }
        };
        Assert.Equal(typeof(int), extension.ProvideValue(provider));
        Assert.Equal(typeof(int), extension.Type);
    }

    [Fact]
    public void ProvideValue_NoTypeOrTypeName_ThrowsInvalidOperationException()
    {
        var extension = new TypeExtension();
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(null));
    }

    [Fact]
    public void ProvideValue_NullServiceProvider_ThrowsArgumentNullException()
    {
        var extension = new TypeExtension("typeName");
        Assert.Throws<ArgumentNullException>("serviceProvider", () => extension.ProvideValue(null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void ProvideValue_InvalidTypeResolver_ThrowsInvalidOperationException(object service)
    {
        var extension = new TypeExtension("typeName");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => service
        };
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(provider));
    }

    [Fact]
    public void ProvideValue_NullResolvedType_ThrowsInvalidOperationException()
    {
        var extension = new TypeExtension("typeName");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlTypeResolver
            {
                ResolveAction = qualifiedTypeName =>
                {
                    Assert.Equal("typeName", qualifiedTypeName);
                    return null!;
                }
            }
        };
        Assert.Throws<InvalidOperationException>(() => extension.ProvideValue(provider));
    }

    [Theory]
    [InlineData("")]
    [InlineData("typeName")]
    public void TypeName_Set_GetReturnsExpected(string value)
    {
        var extension = new TypeExtension(typeof(int))
        {
            TypeName = value
        };
        Assert.Null(extension.Type);
        Assert.Equal(value, extension.TypeName);
    }

    [Fact]
    public void TypeName_SetNull_ThrowsArgumemtNullException()
    {
        var extension = new TypeExtension();
        Assert.Throws<ArgumentNullException>("value", () => extension.TypeName = null);
    }

    [Theory]
    [InlineData(typeof(int))]
    public void Type_Set_GetReturnsExpected(Type value)
    {
        var extension = new TypeExtension("typeName") { Type = value };
        Assert.Equal(value, extension.Type);
        Assert.Null(extension.TypeName);
    }

    [Fact]
    public void Type_SetNull_ThrowsArgumemtNullException()
    {
        var extension = new TypeExtension();
        Assert.Throws<ArgumentNullException>("value", () => extension.Type = null);
    }

    [Theory]
    [InlineData(typeof(InstanceDescriptor), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(null, false)]
    public void TypeExtensionConverter_CanConvertTo_ReturnsExpected(Type type, bool expected)
    {
        var extension = new TypeExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Equal(expected, converter.CanConvertTo(type));
    }

    [Fact]
    public void TypeExtensionConverter_ConvertToInstanceDescriptor_ReturnsExpected()
    {
        var extension = new TypeExtension(typeof(int));
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(extension, typeof(InstanceDescriptor)));
        Assert.Equal(new Type[] { typeof(Type) }, Assert.IsAssignableFrom<ConstructorInfo>(descriptor.MemberInfo).GetParameters().Select(p => p.ParameterType));
        Assert.Equal(new Type[] { typeof(int) }, descriptor.Arguments);
    }

    [Fact]
    public void TypeExtensionConverter_ConvertToString_ReturnsExpected()
    {
        var extension = new TypeExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        InstanceDescriptor descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(extension, typeof(InstanceDescriptor)));
        Assert.Equal(extension.ToString(), converter.ConvertTo(extension, typeof(string)));
    }

    [Fact]
    public void TypeExtensionConverter_ConvertToNotTypeExtension_ThrowsArgumentException()
    {
        var extension = new TypeExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Throws<ArgumentException>(() => converter.ConvertTo(1, typeof(InstanceDescriptor)));
    }

    [Fact]
    public void TypeExtensionConverter_ConvertToInvalidType_ThrowsNotSupportedException()
    {
        var extension = new StaticExtension("member");
        TypeConverter converter = TypeDescriptor.GetConverter(extension);
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(extension, typeof(int)));
    }

    private class CustomServiceProvider : IServiceProvider
    {
        public Func<Type, object>? ServiceAction { get; set; }

        public object GetService(Type serviceType)
        {
            if (ServiceAction is null)
            {
                throw new NotImplementedException();
            }

            return ServiceAction(serviceType);
        }
    }

    private class CustomXamlTypeResolver : IXamlTypeResolver
    {
        public Func<string, Type>? ResolveAction { get; set; }

        public Type Resolve(string qualifiedTypeName)
        {
            if (ResolveAction is null)
            {
                throw new NotImplementedException();
            }

            return ResolveAction(qualifiedTypeName);
        }
    }
}
