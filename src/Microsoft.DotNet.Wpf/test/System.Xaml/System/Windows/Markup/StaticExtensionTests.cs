// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using System.Xaml.Tests.Common;
using Xunit;

namespace System.Windows.Markup.Tests
{
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
                Service = new CustomXamlTypeResolver
                {
                    ResolveResult = typeof(CustomType)
                }
            };
            Assert.Equal(expected, extension.ProvideValue(provider));
        }

        [Fact]
        public void ProvideValue_EnumMemberType_ReturnsExpected()
        {
            var extension = new StaticExtension("Red") { MemberType = typeof(ConsoleColor) };
            Assert.Equal(ConsoleColor.Red, extension.ProvideValue(null));
        }

        [Fact]
        public void ProvideValue_EnumResolvedType_ReturnsExpected()
        {
            var extension = new StaticExtension("Type.Red");
            var provider = new CustomServiceProvider
            {
                Service = new CustomXamlTypeResolver
                {
                    ResolveResult = typeof(ConsoleColor)
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
                Service = new CustomXamlTypeResolver
                {
                    ResolveResult = typeof(int)
                }
            };
            Assert.Throws<ArgumentException>(null, () => extension.ProvideValue(provider));
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
        public void ProvideValue_InvalidTypeResolver_ThrowsArgumentException(object service)
        {
            var extension = new StaticExtension("type.member");
            var provider = new CustomServiceProvider { Service = service };
            Assert.Throws<ArgumentException>(null, () => extension.ProvideValue(provider));
        }

        [Theory]
        [InlineData("s_privateField")]
        [InlineData("_field")]
        [InlineData("PrivateStaticProperty")]
        [InlineData("Property")]
        public void ProvideValue_NoSuchFieldOrPropertyOnMemberType_ThrowsArgumentException(string member)
        {
            var extension = new StaticExtension(member) { MemberType = typeof(CustomType) };
            Assert.Throws<ArgumentException>(null, () => extension.ProvideValue(null));
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
                Service = new CustomXamlTypeResolver
                {
                    ExpectedQualifiedTypeName = member,
                    ResolveResult = typeof(CustomType)
                }
            };
            Assert.Throws<ArgumentException>(null, () => extension.ProvideValue(provider));
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
        public void StaticExtensionConverter_CanConvertTo_ReturnsExpected(Type type, bool expected)
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
            Assert.Throws<ArgumentException>(null, () => converter.ConvertTo(1, typeof(InstanceDescriptor)));
        }

        [Fact]
        public void StaticExtensionConverter_ConvertToInvalidType_ThrowsNotSupportedException()
        {
            var extension = new StaticExtension("member");
            TypeConverter converter = TypeDescriptor.GetConverter(extension);
            Assert.Throws<NotSupportedException>(() => converter.ConvertTo(extension, typeof(int)));
        }

        public class BaseType
        {
            public static int s_inheritedField = 1;
            public static int InheritedProperty = 2;
        }

#pragma warning disable 0169
        public class CustomType : BaseType
        {
            public static int s_field = 3;
            public static int StaticProperty { get; set; } = 4;

            private static int s_privateField;
            public int _field;

            private static int PrivateStaticProperty { get; set; }
            public int Property { get; set; }
        }
#pragma warning restore 0169
    }
}
