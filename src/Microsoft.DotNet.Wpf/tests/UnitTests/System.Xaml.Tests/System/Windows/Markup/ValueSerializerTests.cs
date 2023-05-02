// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Xunit;

namespace System.Windows.Markup.Tests;
 
public class ValueSerializerTests
{
    public static IEnumerable<object?[]> CanConvertToString_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(CanConvertToString_TestData))]
    public void CanConvertToString_Invoke_ReturnsFalse(object? value)
    {
        var serializer = new CustomValueSerializer();
        Assert.False(serializer.CanConvertToString(value, null));
        Assert.False(serializer.CanConvertToString(value, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object?[]> ConvertToString_CantConvert_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(ConvertToString_CantConvert_TestData))]
    public void ConvertToString_CantConvert_ThrowsNotSupportedException(object? value)
    {
        var serializer = new CustomValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertToString(value, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object?[]> CanConvertFromString_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
    }

    [Theory]
    [MemberData(nameof(CanConvertFromString_TestData))]
    public void CanConvertFromString_Invoke_ReturnsFalse(string value)
    {
        var serializer = new CustomValueSerializer();
        Assert.False(serializer.CanConvertFromString(value, null));
        Assert.False(serializer.CanConvertFromString(value, new CustomValueSerializerContext()));
    }

    [Fact]
    public void ConvertFromString_NullValue_ThrowsNotSupportedException()
    {
        var serializer = new CustomValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(null, new CustomValueSerializerContext()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("value")]
    public void ConvertFromString_InvalidValue_ThrowsNotSupportedException(string value)
    {
        var serializer = new CustomValueSerializer();
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(value, null));
        Assert.Throws<NotSupportedException>(() => serializer.ConvertFromString(value, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object?[]> TypeReferences_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { string.Empty };
        yield return new object?[] { "value" };
        yield return new object?[] { new object() };
    }

    [Theory]
    [MemberData(nameof(TypeReferences_TestData))]
    public void TypeReferences_Invoke_ReturnsEmpty(object? value)
    {
        var serializer = new CustomValueSerializer();
        Assert.Empty(serializer.TypeReferences(value, null));
        Assert.Empty(serializer.TypeReferences(value, new CustomValueSerializerContext()));
    }

    public static IEnumerable<object[]> GetSerializerFor_WellKnownType_TestData()
    {
        yield return new object[] { typeof(sbyte) };
        yield return new object[] { typeof(byte) };
        yield return new object[] { typeof(short) };
        yield return new object[] { typeof(ushort) };
        yield return new object[] { typeof(int) };
        yield return new object[] { typeof(uint) };
        yield return new object[] { typeof(long) };
        yield return new object[] { typeof(ulong) };
        yield return new object[] { typeof(char) };
        yield return new object[] { typeof(bool) };
        yield return new object[] { typeof(float) };
        yield return new object[] { typeof(double) };
        yield return new object[] { typeof(decimal) };
        yield return new object[] { typeof(TimeSpan) };
        yield return new object[] { typeof(Guid) };
        yield return new object[] { typeof(CultureInfo) };
        yield return new object[] { typeof(CustomCultureInfo) };
        yield return new object[] { typeof(Type) };
        yield return new object[] { typeof(Uri) };
        yield return new object[] { typeof(CustomUri) };
        yield return new object[] { typeof(int?) };
        yield return new object[] { typeof(ConsoleColor) };
    }

    [Theory]
    [MemberData(nameof(GetSerializerFor_WellKnownType_TestData))]
    public void GetSerializerFor_WellKnownType_ReturnsExpected(Type type)
    {
        Assert.NotNull(ValueSerializer.GetSerializerFor(type));
        Assert.NotNull(ValueSerializer.GetSerializerFor(type, null));
    }

    [Fact]
    public void GetSerializerFor_DateTime_ReturnsDateTimeValueSerializer()
    {
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(typeof(DateTime)));
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(typeof(DateTime), null));
    }

    [Fact]
    public void GetSerializerFor_HasValueSerializerAttribute_ReturnsExpected()
    {
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(typeof(ClassWithValueSerializerAttribute)));
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(typeof(ClassWithValueSerializerAttribute), null));
    }

    [Fact]
    public void GetSerializerFor_NoSerializerForType_ReturnsNull()
    {
        Assert.Null(ValueSerializer.GetSerializerFor(typeof(ValueSerializer)));
        Assert.Null(ValueSerializer.GetSerializerFor(typeof(ValueSerializer), null));
    }

    [Fact]
    public void GetSerializerFor_TypeConverterAttributeCanConvertToString_ReturnsExpected()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(ClassWithPublicTypeConverterAttribute));
        Assert.True(serializer.CanConvertToString(null, null));
    }

    [Fact]
    public void GetSerializerFor_TypeConverterAttributeConvertToString_ReturnsExpected()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(ClassWithPublicTypeConverterAttribute));
        Assert.Equal("1", serializer.ConvertToString(1, null));
    }

    [Fact]
    public void GetSerializerFor_TypeConverterAttributeCanConvertFromString_ReturnsExpected()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(ClassWithPublicTypeConverterAttribute));
        Assert.True(serializer.CanConvertFromString(null, null));
    }

    [Fact]
    public void GetSerializerFor_TypeConverterAttributeConvertFromString_ReturnsExpected()
    {
        ValueSerializer serializer = ValueSerializer.GetSerializerFor(typeof(ClassWithPublicTypeConverterAttribute));
        Assert.Equal("1", serializer.ConvertFromString("1", null));
    }

    [Theory]
    [InlineData(typeof(ValueSerializer))]
    [InlineData(typeof(ClassWithNoSuchTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNoSuchEmptyTypeConverterAttribute))]
    [InlineData(typeof(ClassWithBadQualifiedTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNoSuchTypeInAssemblyTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNoSuchEmptyTypeInAssemblyTypeConverterAttribute))]
    [InlineData(typeof(ClassWithInternalTypeConverterAttribute))]
    [InlineData(typeof(ClassWithPrivateTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNestedInInternalTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNestedInPrivateTypeConverterAttribute))]
    [InlineData(typeof(ClassWithNonTypeConverterConverterAttribute))]
    [InlineData(typeof(ClassWithNonTypeConverterInAssemblyTypeConverterAttribute))]
    [InlineData(typeof(ClassWithCannotConvertToTypeConverterAttribute))]
    [InlineData(typeof(ClassWithCannotConvertFromTypeConverterAttribute))]
    [InlineData(typeof(ClassWithReferenceConverterTypeConverterAttribute))]
    public void GetSerializerFor_NoSuchTypeConverterAttribute_ReturnsNull(Type type)
    {
        Assert.Null(ValueSerializer.GetSerializerFor(type));
        Assert.Null(ValueSerializer.GetSerializerFor(type, null));
    }

    [Fact]
    public void GetSerializerFor_NullTypeConverterAttribute_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("typeName", () => ValueSerializer.GetSerializerFor(typeof(ClassWithNullStringTypeConverterAttribute)));
        Assert.Throws<ArgumentNullException>("typeName", () => ValueSerializer.GetSerializerFor(typeof(ClassWithNullStringTypeConverterAttribute), null!));
    }

    [Fact]
    public void GetSerializerFor_EmptyTypeConverterAttribute_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ValueSerializer.GetSerializerFor(typeof(ClassWithEmptyTypeConverterAttribute)));
        Assert.Throws<ArgumentException>(() => ValueSerializer.GetSerializerFor(typeof(ClassWithEmptyTypeConverterAttribute), null!));
    }

    [Fact]
    public void GetSerializer_AfterRefresh_Success()
    {
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(typeof(DateTime)));
        TypeDescriptor.Refresh(typeof(DateTime));
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(typeof(DateTime)));
    }

    [Fact]
    public void GetSerializerFor_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("type", () => ValueSerializer.GetSerializerFor((Type)null!));
        Assert.Throws<ArgumentNullException>("type", () => ValueSerializer.GetSerializerFor((Type)null!, null));
    }

    [Fact]
    public void GetSerializerFor_HasInvalidValueSerializerAttribute_ThrowsInvalidCastException()
    {
        Assert.Throws<InvalidCastException>(() => ValueSerializer.GetSerializerFor(typeof(ClassWithInvalidValueSerializerAttribute)));
        Assert.Throws<InvalidCastException>(() => ValueSerializer.GetSerializerFor(typeof(ClassWithInvalidValueSerializerAttribute), null));
    }

    [Fact]
    public void GetSerializerFor_TypeWithContext_ReturnsExpected()
    {
        var serializer = new CustomValueSerializer();
        var context = new CustomValueSerializerContext { SerializerResult = serializer };
        Assert.Same(serializer, ValueSerializer.GetSerializerFor(typeof(int), context));
    }

    [Fact]
    public void GetSerializerFor_TypeWithNullContextResult_ReturnsExpected()
    {
        var context = new CustomValueSerializerContext { SerializerResult = null };
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(typeof(ClassWithValueSerializerAttribute), context));
    }

    [Fact]
    public void GetSerializerFor_DescriptorDateTime_ReturnsExpected()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithDateTimeProperty))[0];
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(descriptor));
        Assert.IsType<DateTimeValueSerializer>(ValueSerializer.GetSerializerFor(descriptor, null));
    }

    [Fact]
    public void GetSerializerFor_DescriptorHasValueSerializerAttribute_ReturnsExpected()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithValueSerializerAttributeProperty))[0];
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(descriptor));
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(descriptor, null));
    }

    [Fact]
    public void GetSerializerFor_DescriptorHasConverter_ReturnsExpected()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithConvertibleProperty))[0];
        Assert.NotNull(ValueSerializer.GetSerializerFor(descriptor));
        Assert.NotNull(ValueSerializer.GetSerializerFor(descriptor, null));
    }

    [Theory]
    [InlineData(typeof(ClassWithUnconvertibleProperty))]
    [InlineData(typeof(ClassWithCannotConvertToConverterProperty))]
    [InlineData(typeof(ClassWithCannotConvertFromConverterProperty))]
    [InlineData(typeof(ClassWithReferenceConverterProperty))]
    public void GetSerializerFor_DescriptorHasNoConverter_ReturnsNull(Type type)
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(type)[0];
        Assert.Null(ValueSerializer.GetSerializerFor(descriptor));
        Assert.Null(ValueSerializer.GetSerializerFor(descriptor, null));
    }

    [Fact]
    public void GetSerializerFor_NullDescriptor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("descriptor", () => ValueSerializer.GetSerializerFor((PropertyDescriptor)null!));
        Assert.Throws<ArgumentNullException>("descriptor", () => ValueSerializer.GetSerializerFor((PropertyDescriptor)null!, null));
    }

    [Fact]
    public void GetSerializerFor_DescriptorHasInvalidValueSerializerAttribute_ThrowsInvalidCastException()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithInvalidValueSerializerAttributeProperty))[0];
        Assert.Throws<InvalidCastException>(() => ValueSerializer.GetSerializerFor(descriptor));
        Assert.Throws<InvalidCastException>(() => ValueSerializer.GetSerializerFor(descriptor, null));
    }

    [Fact]
    public void GetSerializerFor_DescriptorWithContext_ReturnsExpected()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithValueSerializerAttributeProperty))[0];
        var serializer = new CustomValueSerializer();
        var context = new CustomValueSerializerContext { SerializerResult = serializer };
        Assert.Same(serializer, ValueSerializer.GetSerializerFor(descriptor, context));
    }

    [Fact]
    public void GetSerializerFor_DescriptorWithNullContextResult_ReturnsExpected()
    {
        PropertyDescriptor descriptor = TypeDescriptor.GetProperties(typeof(ClassWithValueSerializerAttributeProperty))[0];
        var context = new CustomValueSerializerContext { SerializerResult = null };
        Assert.IsType<CustomValueSerializer>(ValueSerializer.GetSerializerFor(descriptor, context));
    }

    public class CustomValueSerializer : ValueSerializer { }

    [ValueSerializer(typeof(CustomValueSerializer))]
    public class ClassWithValueSerializerAttribute { }

    [ValueSerializer(typeof(int))]
    public class ClassWithInvalidValueSerializerAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.CustomTypeConverter,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithPublicTypeConverterAttribute { }

    [TypeConverter("NoSuchType")]
    public class ClassWithNoSuchTypeConverterAttribute { }

    [TypeConverter("")]
    public class ClassWithNoSuchEmptyTypeConverterAttribute { }

    [TypeConverter("NoSuchType1,NoSuchType2,NoSuchType3")]
    public class ClassWithBadQualifiedTypeConverterAttribute { }

    [TypeConverter("NoSuchType,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithNoSuchTypeInAssemblyTypeConverterAttribute { }

    [TypeConverter(",System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithNoSuchEmptyTypeInAssemblyTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.InternalClass,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithInternalTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.PrivateClass,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithPrivateTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.InternalClass+NestedPublicClass,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithNestedInInternalTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.PrivateClass+NestedPublicClass,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithNestedInPrivateTypeConverterAttribute { }

    [TypeConverter("System.Int32")]
    public class ClassWithNonTypeConverterConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.PublicClass,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithNonTypeConverterInAssemblyTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.CannotConvertToTypeConverter,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithCannotConvertToTypeConverterAttribute { }

    [TypeConverter("System.Windows.Markup.Tests.CannotConvertFromTypeConverter,System.Xaml.Tests, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null")]
    public class ClassWithCannotConvertFromTypeConverterAttribute { }

    [TypeConverter("System.ComponentModel.ReferenceConverter")]
    public class ClassWithReferenceConverterTypeConverterAttribute { }

    [TypeConverter]
    public class ClassWithEmptyTypeConverterAttribute { }

    [TypeConverter((string)null!)]
    public class ClassWithNullStringTypeConverterAttribute { }

    public class CustomCultureInfo : CultureInfo
    {
        public CustomCultureInfo(int culture) : base(culture) { }
    }

    public class CustomUri : Uri
    {
        public CustomUri(string uriString) : base(uriString) { }
    }

    public class ClassWithDateTimeProperty
    {
        public DateTime Property { get; set; }
    }

    public class ClassWithConvertibleProperty
    {
        [TypeConverter(typeof(CustomTypeConverter))]
        public CustomValueSerializer? Property { get; set; }
    }

    public class ClassWithUnconvertibleProperty
    {
        public CustomValueSerializer? Property { get; set; }
    }

    public class ClassWithCannotConvertToConverterProperty
    {
        [TypeConverter(typeof(CannotConvertToTypeConverter))]
        public CustomValueSerializer? Property { get; set; }
    }

    public class ClassWithCannotConvertFromConverterProperty
    {
        [TypeConverter(typeof(CannotConvertFromTypeConverter))]
        public CustomValueSerializer? Property { get; set; }
    }

    public class ClassWithReferenceConverterProperty
    {
        [TypeConverter(typeof(ReferenceConverter))]
        public CustomValueSerializer? Property { get; set; }
    }

    public class ClassWithValueSerializerAttributeProperty
    {
        [ValueSerializer(typeof(CustomValueSerializer))]
        public int Property { get; set; }
    }

    public class ClassWithInvalidValueSerializerAttributeProperty
    {
        [ValueSerializer(typeof(int))]
        public int Property { get; set; }
    }

    public class CustomValueSerializerContext : IValueSerializerContext
    {
        public ValueSerializer? SerializerResult { get; set; }

        public ValueSerializer? GetValueSerializerFor(Type type) => SerializerResult;
        
        public ValueSerializer? GetValueSerializerFor(PropertyDescriptor type) => SerializerResult;

        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}

public class CustomTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? sourceType)
    {
        Assert.Equal(typeof(string), sourceType);
        return true;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
    {
        Assert.Equal(typeof(string), sourceType);
        return true;
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        return value!.ToString();
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        return Convert.ChangeType(value, destinationType)!;
    }
}

public class CannotConvertToTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? sourceType)
    {
        return false;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
    {
        return true;
    }
}

public class CannotConvertFromTypeConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? sourceType)
    {
        return true;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
    {
        return false;
    }
}

public class PublicClass : TypeConverter
{
    internal class NestedInternalClass { }
    private class NestedPrivateClass { }
}

internal class InternalClass : TypeConverter
{
    public class NestedPublicClass { }
}

class PrivateClass : TypeConverter
{
    public class NestedPublicClass { }
}
