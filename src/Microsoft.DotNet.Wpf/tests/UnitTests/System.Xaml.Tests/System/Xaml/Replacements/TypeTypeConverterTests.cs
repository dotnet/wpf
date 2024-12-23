// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Replacements.Tests;

public class TypeTypeConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
    }

    [Fact]
    public void ConvertFrom_ValidContextService_ReturnsExpected()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlTypeResolver), serviceType);
                return new CustomXamlTypeResolver
                {
                    ResolveAction = qualifiedTypeName =>
                    {
                        Assert.Equal("value", qualifiedTypeName);
                        return typeof(int);
                    }
                };
            }
        };
        Assert.Equal(typeof(int), converter.ConvertFrom(context, null, "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertFrom_InvalidIXamlTypeResolverService_ThrowsNotSupportedException(object? service)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlTypeResolver), serviceType);
                return service;
            }
        };
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
    }

    [Fact]
    public void ConvertFrom_NullContext_ThrowsNotSupportedException()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), null, value!));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }

    [Fact]
    public void ConvertTo_ValidContextService_ReturnsExpected()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new XamlSchemaContext()
                    };
                }
                else if (serviceType == typeof(INamespacePrefixLookup))
                {
                    return new CustomNamespacePrefixLookup
                    {
                        LookupPrefixAction = ns =>
                        {
                            Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml", ns);
                            return "prefix";
                        }
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        };
        Assert.Equal("prefix:Int32", converter.ConvertTo(context, null, typeof(int), typeof(string)));
    }

    [Fact]
    public void ConvertTo_CustomGetTypeContextService_ReturnsExpected()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new CustomXamlSchemaContext
                        {
                            GetXamlTypeAction = (type) => new XamlType(typeof(short), new XamlSchemaContext())
                        }
                    };
                }
                else if (serviceType == typeof(INamespacePrefixLookup))
                {
                    return new CustomNamespacePrefixLookup
                    {
                        LookupPrefixAction = ns =>
                        {
                            Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml", ns);
                            return "prefix";
                        }
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        };
        Assert.Equal("prefix:Int16", converter.ConvertTo(context, null, typeof(int), typeof(string)));
    }

    [Fact]
    public void ConvertTo_NullGetXamlTypeResult_ReturnsExpected()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlSchemaContextProvider), serviceType);
                return new CustomXamlSchemaContextProvider
                {
                    SchemaContextResult = new CustomXamlSchemaContext
                    {
                        GetXamlTypeAction = type => null!
                    }
                };
            }
        };
        Assert.Equal(typeof(int).ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
    }

    public static IEnumerable<object?[]> ConvertTo_InvalidIXamlSchemaContextProvider_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { 1 };
        yield return new object?[] { new CustomXamlSchemaContextProvider() };
    }

    [Theory]
    [MemberData(nameof(ConvertTo_InvalidIXamlSchemaContextProvider_TestData))]
    public void ConvertTo_InvalidIXamlSchemaContextProvider_ReturnsExpected(object service)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlSchemaContextProvider), serviceType);
                return service;
            }
        };
        Assert.Equal(typeof(int).ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertTo_InvalidINamespacePrefixLookup_ReturnsExpected(object? service)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new XamlSchemaContext()
                    };
                }
                else if (serviceType == typeof(INamespacePrefixLookup))
                {
                    return service;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        };

        var expectedType = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Equal(expectedType.ToString(), converter.ConvertTo(context, null, typeof(int), typeof(string)));
    }
    
    [Theory]
    [InlineData("notType")]
    [InlineData(null)]
    public void ConvertTo_NotType_ReturnsExpected(object? value)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(typeof(int), destinationType));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, typeof(int), destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        var type = new XamlType(typeof(Type), new XamlSchemaContext());
        TypeConverter converter = type.TypeConverter.ConverterInstance;
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(typeof(int), null!));
    }
    
    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public Func<Type, object?>? GetServiceAction { get; set; }

        public object? GetService(Type serviceType)
        {
            if (GetServiceAction is null)
            {
                throw new NotImplementedException();
            }

            return GetServiceAction(serviceType);
        }

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
    
    private class CustomXamlSchemaContextProvider : IXamlSchemaContextProvider
    {
        public XamlSchemaContext? SchemaContextResult { get; set; }

        public XamlSchemaContext? SchemaContext => SchemaContextResult;
    }

    private class CustomNamespacePrefixLookup : INamespacePrefixLookup
    {
        public Func<string, string>? LookupPrefixAction { get; set; }

        public string LookupPrefix(string ns)
        {
            if (LookupPrefixAction is null)
            {
                throw new NotImplementedException();
            }

            return LookupPrefixAction(ns);
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
    
    private class CustomXamlSchemaContext : XamlSchemaContext
    {
        public Func<Type, XamlType>? GetXamlTypeAction { get; set; }

        public override XamlType GetXamlType(Type type)
        {
            if (GetXamlTypeAction is null)
            {
                throw new NotImplementedException();
            }

            return GetXamlTypeAction(type);
        }
    }
}
