// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.Xaml.Schema.Tests;

public class XamlTypeConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var converter = new XamlTypeTypeConverter();
        Assert.Equal(expected, converter.CanConvertFrom(sourceType!));
    }
    
    public static IEnumerable<object[]> ConvertFrom_TestData()
    {
        var context = new XamlSchemaContext();
        yield return new object[]
        {
            "prefix:name",
            "namespace",
            context,
            new XamlType("namespace", "name", null, context)
        };
        yield return new object[]
        {
            "prefix:name(prefix:typeName)",
            "namespace",
            context,
            new XamlType("namespace", "name", new XamlType[]
            {
                new XamlType("namespace", "typeName", null, context)
            }, context)
        };

        yield return new object[]
        {
            "prefix:Int32",
            "http://schemas.microsoft.com/winfx/2006/xaml",
            context,
            new XamlType(typeof(int), context)
        };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_TestData))]
    public void ConvertFrom_ValidContextService_ReturnsExpected(string value, string namespaceResult, XamlSchemaContext schemaContext, XamlType expected)
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlNamespaceResolver))
                {
                    return  new CustomXamlNamespaceResolver
                    {
                        GetNamespaceAction = prefix => namespaceResult
                    };
                }
                else if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = schemaContext
                    };
                }

                throw new NotImplementedException();
            }
        };
        XamlType actual = Assert.IsType<XamlType>(converter.ConvertFrom(context, null, value));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1:name")]
    [InlineData("prefix:1")]
    public void ConvertFrom_InvalidStringValue_ThrowsFormatException(string value)
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlNamespaceResolver))
                {
                    return new CustomXamlNamespaceResolver
                    {
                        GetNamespaceAction = prefix => "namespace"
                    };
                }
                else if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return new CustomXamlSchemaContextProvider
                    {
                        SchemaContextResult = new XamlSchemaContext()
                    };
                }

                throw new NotImplementedException();
            }
        };
        Assert.Throws<FormatException>(() => converter.ConvertFrom(context, null, value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertFrom_InvalidIXamlNamespaceResolverService_ThrowsNotSupportedException(object? service)
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlNamespaceResolver))
                {
                    return service;
                }

                throw new NotImplementedException();
            }
        };
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "value"));
    }

    public static IEnumerable<object?[]> ConvertFrom_InvalidIXamlSchemaContextProvider_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { 1 };
        yield return new object?[] { new CustomXamlSchemaContextProvider() };
    }

    [Theory]
    [MemberData(nameof(ConvertFrom_InvalidIXamlSchemaContextProvider_TestData))]
    public void ConvertFrom_InvalidIXamlSchemaContextProvider_ThrowsNotSupportedException(object service)
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(IXamlNamespaceResolver))
                {
                    return new CustomXamlNamespaceResolver
                    {
                        GetNamespaceAction = prefix => "namespace"
                    };
                }
                else if (serviceType == typeof(IXamlSchemaContextProvider))
                {
                    return service;
                }

                throw new NotImplementedException();
            }
        };
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(context, null, "prefix:namespace"));
    }

    [Fact]
    public void ConvertFrom_NullContext_ThrowsNotSupportedException()
    {
        var converter = new XamlTypeTypeConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom("value"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void ConvertFrom_InvalidObject_ThrowsNotSupportedException(object? value)
    {
        var converter = new XamlTypeTypeConverter();
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(value!));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(new CustomTypeDescriptorContext(), null, value));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(InstanceDescriptor), false)]
    public void CanConvertTo_Invoke_ReturnsExpected(Type? sourceType, bool expected)
    {
        var converter = new XamlTypeTypeConverter();
        Assert.Equal(expected, converter.CanConvertTo(sourceType));
    }

    [Fact]
    public void ConvertTo_ValidContextService_ReturnsExpected()
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(INamespacePrefixLookup))
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

                throw new NotImplementedException();
            }
        };

        var type = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Equal("prefix:Int32", converter.ConvertTo(context, null, type, typeof(string)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public void ConvertTo_InvalidINamespacePrefixLookup_ReturnsExpected(object? service)
    {
        var converter = new XamlTypeTypeConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                if (serviceType == typeof(INamespacePrefixLookup))
                {
                    return service;
                }

                throw new NotImplementedException();
            }
        };

        var type = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Equal(type.ToString(), converter.ConvertTo(context, null, type, typeof(string)));
    }
    
    [Theory]
    [InlineData("notXamlType")]
    [InlineData(null)]
    public void ConvertTo_NotXamlType_ReturnsExpected(object? value)
    {
        var converter = new XamlTypeTypeConverter();
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(value, typeof(string)));
        Assert.Equal(value ?? string.Empty, converter.ConvertTo(new CustomTypeDescriptorContext(), null, value, typeof(string)));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    public void ConvertTo_InvalidType_ThrowsNotSupportedException(Type destinationType)
    {
        var converter = new XamlTypeTypeConverter();
        var type = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(type, destinationType));
        Assert.Throws<NotSupportedException>(() => converter.ConvertTo(new CustomTypeDescriptorContext(), null, type, destinationType));
    }

    [Fact]
    public void ConvertTo_NullDestinationType_ThrowsArgumentNullException()
    {
        var converter = new XamlTypeTypeConverter();
        var type = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("destinationType", () => converter.ConvertTo(type, null!));
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

    private class CustomXamlNamespaceResolver : IXamlNamespaceResolver
    {
        public Func<string, string>? GetNamespaceAction { get; set; }

        public string GetNamespace(string prefix)
        {
            if (GetNamespaceAction is null)
            {
                throw new NotImplementedException();
            }

            return GetNamespaceAction(prefix);
        }

        public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes() => throw new NotImplementedException();
    }
    
    private class CustomXamlSchemaContextProvider : IXamlSchemaContextProvider
    {
        public XamlSchemaContext? SchemaContextResult { get; set; }

        public XamlSchemaContext? SchemaContext => SchemaContextResult;
    }
}
