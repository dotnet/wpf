// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Xaml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class NameReferenceConverterTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(InstanceDescriptor), true)]
    public void CanConvertFrom_Invoke_ReturnsExpected(Type type, bool expected)
    {
        var converter = new NameReferenceConverter();
        Assert.Equal(expected, converter.CanConvertFrom(type));
    }

    [Fact]
    public void ConvertFrom_ResolveSuccessful_ReturnsExpected()
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameResolver), serviceType);
                return new CustomXamlNameResolver
                {
                    ResolveAction = name => "resolve",
                    GetFixupTokenAction = (names, canAssignDirectly) => "fixup"
                };
            }
        };
        Assert.Equal("resolve", converter.ConvertFrom(context, null, "name"));
    }

    [Theory]
    [InlineData("fixup")]
    [InlineData(null)]
    public void ConvertFrom_ResolveUnsuccessful_ReturnsExpected(string fixup)
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameResolver), serviceType);
                return new CustomXamlNameResolver
                {
                    ResolveAction = name => null!,
                    GetFixupTokenAction = (names, canAssignDirectly) => fixup
                };
            }
        };
        Assert.Equal(fixup, converter.ConvertFrom(context, null, "name"));
    }

    [Fact]
    public void ConvertFrom_NullContext_ThrowsArgumentNullException()
    {
        var converter = new NameReferenceConverter();
        Assert.Throws<ArgumentNullException>("context", () => converter.ConvertFrom(null, CultureInfo.CurrentCulture, "name"));
    }

    [Fact]
    public void ConvertFrom_NullService_ThrowsInvalidOperationException()
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameResolver), serviceType);
                return null!;
            }
        };
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(context, null, "name"));
    }

    [Fact]
    public void ConvertFrom_NonIXamlNameResolverService_ThrowsInvalidCastException()
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameResolver), serviceType);
                return new object();
            }
        };
        Assert.Throws<InvalidCastException>(() => converter.ConvertFrom(context, null, "name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData("")]
    public void ConvertFrom_InvalidValue_ThrowsInvalidOperationException(object value)
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameResolver), serviceType);
                return new CustomXamlNameResolver();
            }
        };
        Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(context, null, value));
    }

    public static IEnumerable<object?[]> CanConvertTo_TestData()
    {
        yield return new object?[] { null, null, false };
        yield return new object?[] { new CustomTypeDescriptorContext { GetServiceAction = serviceType => null! }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { GetServiceAction = serviceType => new object() }, typeof(string), false };
        yield return new object?[] { new CustomTypeDescriptorContext { GetServiceAction = serviceType => new CustomXamlNameProvider() }, typeof(int), false };
        yield return new object?[] { new CustomTypeDescriptorContext { GetServiceAction = serviceType => new CustomXamlNameProvider() }, typeof(string), true };
    }

    [Theory]
    [MemberData(nameof(CanConvertTo_TestData))]
    public void CanConvertTo_Invoke_ReturnsExpected(ITypeDescriptorContext context, Type destinationType, bool expected)
    {
        var converter = new NameReferenceConverter();
        Assert.Equal(expected, converter.CanConvertTo(context, destinationType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("name")]
    public void ConvertTo_ValidService_ReturnsExpected(string name)
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameProvider), serviceType);
                return new CustomXamlNameProvider
                {
                    GetNameAction = value => name
                };
            }
        };
        Assert.Equal(name, converter.ConvertTo(context, null, "value", null));
    }

    [Fact]
    public void ConvertTo_NullContext_ThrowsArgumentNullException()
    {
        var converter = new NameReferenceConverter();
        Assert.Throws<ArgumentNullException>("context", () => converter.ConvertTo(null, CultureInfo.CurrentCulture, "value", typeof(string)));
    }

    [Fact]
    public void ConvertTo_NullService_ThrowsInvalidOperationException()
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameProvider), serviceType);
                return null!;
            }
        };
        Assert.Throws<InvalidOperationException>(() => converter.ConvertTo(context, null, "value", null));
    }

    [Fact]
    public void ConvertTo_NonIXamlNameProviderService_ThrowsInvalidCastException()
    {
        var converter = new NameReferenceConverter();
        var context = new CustomTypeDescriptorContext
        {
            GetServiceAction = serviceType =>
            {
                Assert.Equal(typeof(IXamlNameProvider), serviceType);
                return new object();
            }
        };
        Assert.Throws<InvalidCastException>(() => converter.ConvertTo(context, null, "value", null));
    }

    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public Func<Type, object>? GetServiceAction { get; set; }

        public object GetService(Type serviceType)
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

    private class CustomXamlNameResolver : IXamlNameResolver
    {
        public bool IsFixupTokenAvailable => throw new NotImplementedException();

        public Func<string, object>? ResolveAction { get; set; }

        public object Resolve(string name)
        {
            if (ResolveAction is null)
            {
                throw new NotImplementedException();
            }

            return ResolveAction(name);
        }

        public object Resolve(string name, out bool isFullyInitialized) => throw new NotImplementedException();

        public object GetFixupToken(IEnumerable<string> names) => throw new NotImplementedException();

        public Func<IEnumerable<string>, bool, object>? GetFixupTokenAction { get; set; }
        
        public object GetFixupToken(IEnumerable<string> names, bool canAssignDirectly)
        {
            if (GetFixupTokenAction is null)
            {
                throw new NotImplementedException();
            }

            return GetFixupTokenAction(names, canAssignDirectly);
        }

        public IEnumerable<KeyValuePair<string, object>> GetAllNamesAndValuesInScope()
            => throw new NotImplementedException();

        public event EventHandler OnNameScopeInitializationComplete
        {
            add => throw new NotImplementedException();
            remove => throw new NotImplementedException();
        }
    }

    private class CustomXamlNameProvider : IXamlNameProvider
    {
        public Func<object, string>? GetNameAction { get; set; }
        
        public string GetName(object value)
        {
            if (GetNameAction is null)
            {
                throw new NotImplementedException();
            }

            return GetNameAction(value);
        }
    }
}
