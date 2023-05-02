// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xaml;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Windows.Markup.Tests
{
    public class NameReferenceConverterTests
    {
        [Theory]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(int), false)]
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameResolver) },
                Services = new object[]
                {
                    new CustomXamlNameResolver
                    {
                        ResolveResult = "resolve",
                        GetFixupTokenResult = "fixup"
                    }
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameResolver) },
                Services = new object[]
                {
                    new CustomXamlNameResolver
                    {
                        ResolveResult = null,
                        GetFixupTokenResult = fixup
                    }
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameResolver) },
                Services = new object[] { null }
            };
            Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(context, null, "name"));
        }

        [Fact]
        public void ConvertFrom_NonIXamlNameResolverService_ThrowsInvalidCastException()
        {
            var converter = new NameReferenceConverter();
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameResolver) },
                Services = new object[] { new object() }
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameResolver) },
                Services = new object[] { new CustomXamlNameResolver() }
            };
            Assert.Throws<InvalidOperationException>(() => converter.ConvertFrom(context, null, value));
        }

        public static IEnumerable<object[]> CanConvertTo_TestData()
        {
            yield return new object[] { null, null, false };
            yield return new object[] { new CustomTypeDescriptorContext { ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) }, Services = new object[] { null } }, typeof(string), false };
            yield return new object[] { new CustomTypeDescriptorContext { ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) }, Services = new object[] { new object() } }, typeof(string), false };
            yield return new object[] { new CustomTypeDescriptorContext { ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) }, Services = new object[] { new CustomXamlNameProvider() } }, typeof(int), false };
            yield return new object[] { new CustomTypeDescriptorContext { ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) }, Services = new object[] { new CustomXamlNameProvider() } }, typeof(string), true };
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) },
                Services = new object[]
                {
                    new CustomXamlNameProvider
                    {
                        NameResult = name
                    }
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
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) },
                Services = new object[] { null }
            };
            Assert.Throws<InvalidOperationException>(() => converter.ConvertTo(context, null, "value", null));
        }

        [Fact]
        public void ConvertTo_NonIXamlNameProviderService_ThrowsInvalidCastException()
        {
            var converter = new NameReferenceConverter();
            var context = new CustomTypeDescriptorContext
            {
                ExpectedServiceTypes = new Type[] { typeof(IXamlNameProvider) },
                Services = new object[] { new object() }
            };
            Assert.Throws<InvalidCastException>(() => converter.ConvertTo(context, null, "value", null));
        }
    }
}
