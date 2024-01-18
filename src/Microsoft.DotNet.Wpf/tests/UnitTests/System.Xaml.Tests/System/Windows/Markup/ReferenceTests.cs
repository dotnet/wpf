// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class ReferenceTests
{
    [Fact]
    public void Ctor_Default()
    {
        var reference = new Reference();
        Assert.Null(reference.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("name")]
    public void Ctor_String(string name)
    {
        var reference = new Reference(name);
        Assert.Equal(name, reference.Name);
    }

    [Fact]
    public void ProvideValue_ResolveSuccessful_ReturnsExpected()
    {
        var reference = new Reference("name");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlNameResolver
            {
                ResolveAction = name => "resolve",
                GetFixupTokenAction = (names, canAssignDirectly) => "fixup"
            }
        };
        Assert.Equal("resolve", reference.ProvideValue(provider));
    }

    [Theory]
    [InlineData("fixup")]
    [InlineData(null)]
    public void ProvideValue_ResolveUnsuccessful_ReturnsExpected(string fixup)
    {
        var reference = new Reference("name");

        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlNameResolver
            {
                ResolveAction = name => null!,
                GetFixupTokenAction = (names, canAssignDirectly) => fixup
            }
        };
        Assert.Equal(fixup, reference.ProvideValue(provider));
    }

    [Fact]
    public void ProvideValue_NullServiceProvider_ThrowsArgumentNullException()
    {
        var reference = new Reference("name");
        Assert.Throws<ArgumentNullException>("serviceProvider", () => reference.ProvideValue(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("string")]
    public void ProvideValue_NonIXamlNameResolverProvider_ThrowsInvalidOperationException(object value)
    {
        var reference = new Reference("name");
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => value
        };
        Assert.Throws<InvalidOperationException>(() => reference.ProvideValue(provider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ProvideValue_NullOrEmptyName_ThrowsInvalidOperationException(string name)
    {
        var reference = new Reference(name);
        var provider = new CustomServiceProvider
        {
            ServiceAction = serviceType => new CustomXamlNameResolver()
        };
        Assert.Throws<InvalidOperationException>(() => reference.ProvideValue(provider));
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
}
