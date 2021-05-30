// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Windows.Markup.Tests
{
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
                Service = new CustomXamlNameResolver
                {
                    ResolveResult = "resolve",
                    GetFixupTokenResult = "fixup"
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
                Service = new CustomXamlNameResolver
                {
                    ResolveResult = null,
                    GetFixupTokenResult = fixup
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
            var provider = new CustomServiceProvider { Service = value };
            Assert.Throws<InvalidOperationException>(() => reference.ProvideValue(provider));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ProvideValue_NullOrEmptyName_ThrowsInvalidOperationException(string name)
        {
            var reference = new Reference(name);
            var provider = new CustomServiceProvider { Service = new CustomXamlNameResolver() };
            Assert.Throws<InvalidOperationException>(() => reference.ProvideValue(provider));
        }
    }
}
