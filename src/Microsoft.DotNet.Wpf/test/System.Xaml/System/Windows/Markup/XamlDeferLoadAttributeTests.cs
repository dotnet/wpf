// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 0618

namespace System.Windows.Markup.Tests
{
    public class XamlDeferLoadAttributeTests
    {
        [Theory]
        [InlineData(typeof(int), typeof(string))]
        public void Ctor_Type_Type(Type loaderType, Type contentType)
        {
            var attribute = new XamlDeferLoadAttribute(loaderType, contentType);
            Assert.Equal(loaderType, attribute.LoaderType);
            Assert.Equal(loaderType.AssemblyQualifiedName, attribute.LoaderTypeName);
            Assert.Equal(contentType, attribute.ContentType);
            Assert.Equal(contentType.AssemblyQualifiedName, attribute.ContentTypeName);
        }

        [Fact]
        public void Ctor_NullLoaderType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("loaderType", () => new XamlDeferLoadAttribute(null, typeof(int)));
        }

        [Fact]
        public void Ctor_NullContentType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("contentType", () => new XamlDeferLoadAttribute(typeof(int), null));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("loaderType", "contentType")]
        public void Ctor_String_String(string loaderType, string contentType)
        {
            var attribute = new XamlDeferLoadAttribute(loaderType, contentType);
            Assert.Null(attribute.LoaderType);
            Assert.Equal(loaderType, attribute.LoaderTypeName);
            Assert.Null(attribute.ContentType);
            Assert.Equal(contentType, attribute.ContentTypeName);
        }

        [Fact]
        public void Ctor_NullLoaderTypeName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("loaderType", () => new XamlDeferLoadAttribute(null, "contentType"));
        }

        [Fact]
        public void Ctor_NullContentTypeName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("contentType", () => new XamlDeferLoadAttribute("loaderType", null));
        }
    }
}

#pragma warning restore 0618
