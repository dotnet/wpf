// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Windows.Threading;

namespace System.Windows.Tests;

public class DependencyPropertyTests
{
    public static IEnumerable<object?[]> Register_String_Type_Type_TestData()
    {
        yield return new object?[] { "Register_String_Type_Type_TestData1", typeof(string), typeof(DependencyObjectTests), null };
        yield return new object?[] { "Register_String_Type_Type_TestData2", typeof(int), typeof(DependencyObjectTests), 0 };
        yield return new object?[] { "Register_String_Type_Type_TestData3", typeof(int?), typeof(DependencyObjectTests), null };
        yield return new object?[] { "Register_String_Type_Type_TestData4", typeof(List<int>), typeof(DependencyObjectTests), null };
        yield return new object?[] { "Register_String_Type_Type_TestData5", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null };
        yield return new object?[] { "Register_String_Type_Type_TestData6", typeof(object), typeof(DependencyObjectTests), null };
        yield return new object?[] { " ", typeof(int), typeof(DependencyObjectTests), 0 };
        yield return new object?[] { " Register_String_Type_Type_TestData7 ", typeof(int), typeof(DependencyObjectTests), 0 };
    }

    [Theory]
    [MemberData(nameof(Register_String_Type_Type_TestData))]
    public void Register_InvokeStringTypeType_Success(string name, Type propertyType, Type ownerType, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.Register(name, propertyType, ownerType);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }
    
    public static IEnumerable<object?[]> Register_String_Type_Type_PropertyMetadata_TestData()
    {
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "  ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " Register_String_Type_Type_PropertyMetadata_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(Register_String_Type_Type_PropertyMetadata_TestData))]
    public void Register_InvokeStringTypeTypePropertyMetadata_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.Register(name, propertyType, ownerType, typeMetadata);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    public static IEnumerable<object?[]> Register_String_Type_Type_Validate_TestData()
    {
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>(), 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>(), 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "Register_String_Type_Type_Validate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "   ", typeof(int), typeof(DependencyObjectTests), null, 0, 1 };
        yield return new object?[] { " Register_String_Type_Type_Validate_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
    }

    [Theory]
    [MemberData(nameof(Register_String_Type_Type_Validate_TestData))]
    public void Register_InvokeStringTypeTypePropertyMetadataValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue, int expectedValidateValueCallbackCallCount)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return true;
        };
        DependencyProperty property = DependencyProperty.Register(name, propertyType, ownerType, typeMetadata, validateValueCallback);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Same(validateValueCallback, property.ValidateValueCallback);
        Assert.Equal(expectedValidateValueCallbackCallCount, callCount);
    }

    public static IEnumerable<object?[]> Register_String_Type_Type_PropertyMetadata_NullValidate_TestData()
    {
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "Register_String_Type_Type_PropertyMetadata_NullValidate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "    ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " Register_String_Type_Type_PropertyMetadata_NullValidate_TestData8 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(Register_String_Type_Type_PropertyMetadata_NullValidate_TestData))]
    public void Register_InvokeStringTypeTypePropertyMetadataNullValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.Register(name, propertyType, ownerType, typeMetadata, null);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    [Fact]
    public void Register_Multiple_GlobalIndexDifferent()
    {
        DependencyProperty property1 = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name + "1", typeof(int), typeof(DependencyObjectTests));
        DependencyProperty property2 = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name + "2", typeof(int), typeof(DependencyObjectTests));
        Assert.NotEqual(property1.GlobalIndex, property2.GlobalIndex);
    }

    [Fact]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.Register(null, typeof(int), typeof(DependencyPropertyTests)));
    }

    [Fact]
    public void Register_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.Register(string.Empty, typeof(int), typeof(DependencyPropertyTests)));
    }

    [Fact]
    public void Register_NullPropertyType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void Register_InvalidPropertyType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests)));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(void), "DefaultValue")]
    [InlineData(typeof(int), "DefaultValue")]
    [InlineData(typeof(string), 1)]
    [InlineData(typeof(int), null)]
    public void Register_PropertyTypeDoesntMatchMetadata_ThrowsArgumentException(Type propertyType, object? defaultValue)
    {
        var metadata = new PropertyMetadata(defaultValue);
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata, value => true));
    }

    [Fact]
    public void Register_NullOwnerType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(DependencyPropertyTests))]
    public void Register_OwnerTypeNotDependencyObjectWithMetadata_ThrowsArgumentException(Type ownerType)
    {
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata { DefaultValue = 1 }));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata(), value => true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata { DefaultValue = 1 }, value => true));
    }

    [Fact]
    public void Register_ExpressionDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(Activator.CreateInstance(typeof(Expression), nonPublic: true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void Register_DispatcherObjectDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(new SubDispatcherObject());
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void Register_AlreadyRegistered_ThrowsArgumentException()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyPropertyTests));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(property.Name, typeof(int), typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    public static IEnumerable<object?[]> Register_String_Type_Type_ValidateFail_TestData()
    {
        yield return new object?[] { " ", typeof(int), typeof(DependencyObjectTest1), null, 0 };
        yield return new object?[] { " ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "Register_String_Type_Type_ValidateFail_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "Register_String_Type_Type_ValidateFail_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { " Register_String_Type_Type_ValidateFail_TestData3 ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " Register_String_Type_Type_ValidateFail_TestData4 ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
    }

    [Theory]
    [MemberData(nameof(Register_String_Type_Type_ValidateFail_TestData))]
    public void Register_InvokeStringTypeTypeValidateFail_Throws(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return false;
        };
        Assert.Throws<ArgumentException>(() => DependencyProperty.Register(name, propertyType, ownerType, typeMetadata, validateValueCallback));
        Assert.Equal(1, callCount);
    }

    public static IEnumerable<object?[]> RegisterAttached_String_Type_Type_TestData()
    {
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData1", typeof(string), typeof(DependencyObjectTests), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData2", typeof(int), typeof(DependencyObjectTests), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData3", typeof(int?), typeof(DependencyObjectTests), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData4", typeof(List<int>), typeof(DependencyObjectTests), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData5", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_TestData6", typeof(object), typeof(DependencyObjectTests), null };
        yield return new object?[] { "     ", typeof(int), typeof(DependencyObjectTests), 0 };
        yield return new object?[] { " RegisterAttached_String_Type_Type_TestData7 ", typeof(int), typeof(DependencyObjectTests), 0 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttached_String_Type_Type_TestData))]
    public void RegisterAttached_InvokeStringTypeType_Success(string name, Type propertyType, Type ownerType, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(name, propertyType, ownerType);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }
    
    public static IEnumerable<object?[]> RegisterAttached_String_Type_Type_PropertyMetadata_TestData()
    {
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "      ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttached_String_Type_Type_PropertyMetadata_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData30", typeof(int), typeof(int), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_TestData31", typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttached_String_Type_Type_PropertyMetadata_TestData))]
    public void RegisterAttached_InvokeStringTypeTypePropertyMetadata_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    public static IEnumerable<object?[]> RegisterAttached_String_Type_Type_Validate_TestData()
    {
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_Validate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "       ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttached_String_Type_Type_Validate_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttached_String_Type_Type_Validate_TestData))]
    public void RegisterAttached_InvokeStringTypeTypePropertyMetadataValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return true;
        };
        DependencyProperty property = DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata, validateValueCallback);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Same(validateValueCallback, property.ValidateValueCallback);
        Assert.Equal(1, callCount);
    }

    public static IEnumerable<object?[]> RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData()
    {
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "        ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData8 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttached_String_Type_Type_PropertyMetadata_NullValidate_TestData))]
    public void RegisterAttached_InvokeStringTypeTypePropertyMetadataNullValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata, null);
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    [Fact]
    public void RegisterAttached_Multiple_GlobalIndexDifferent()
    {
        DependencyProperty property1 = DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name + "1", typeof(int), typeof(DependencyObjectTests));
        DependencyProperty property2 = DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name + "2", typeof(int), typeof(DependencyObjectTests));
        Assert.NotEqual(property1.GlobalIndex, property2.GlobalIndex);
    }

    [Fact]
    public void RegisterAttached_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterAttached(null, typeof(int), typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterAttached(null, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterAttached(null, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttached_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterAttached(string.Empty, typeof(int), typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterAttached(string.Empty, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterAttached(string.Empty, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttached_NullPropertyType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttached_InvalidPropertyType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests)));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(void), "DefaultValue")]
    [InlineData(typeof(int), "DefaultValue")]
    [InlineData(typeof(string), 1)]
    [InlineData(typeof(int), null)]
    public void RegisterAttached_PropertyTypeDoesntMatchMetadata_ThrowsArgumentException(Type propertyType, object? defaultValue)
    {
        var metadata = new PropertyMetadata(defaultValue);
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata, value => true));
    }

    [Fact]
    public void RegisterAttached_NullOwnerType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttached_ExpressionDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(Activator.CreateInstance(typeof(Expression), nonPublic: true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterAttached_DispatcherObjectDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(new SubDispatcherObject());
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterAttached_AlreadyRegisterAttacheded_ThrowsArgumentException()
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyPropertyTests));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(property.Name, typeof(int), typeof(DependencyPropertyTests)));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    public static IEnumerable<object?[]> RegisterAttached_String_Type_Type_ValidateFail_TestData()
    {
        yield return new object?[] { "         ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "          ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttached_String_Type_Type_ValidateFail_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttached_String_Type_Type_ValidateFail_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { " RegisterAttached_String_Type_Type_ValidateFail_TestData3 ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttached_String_Type_Type_ValidateFail_TestData4 ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttached_String_Type_Type_ValidateFail_TestData))]
    public void RegisterAttached_InvokeStringTypeTypeValidateFail_Throws(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return false;
        };
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttached(name, propertyType, ownerType, typeMetadata, validateValueCallback));
        Assert.Equal(1, callCount);
    }
    
    public static IEnumerable<object?[]> RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData()
    {
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData1", typeof(string), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData6", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData10", typeof(int?), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData15", typeof(List<int>), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData23", typeof(object), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "           ", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { " RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterReadOnly_String_Type_Type_PropertyMetadata_TestData))]
    public void RegisterReadOnly_InvokeStringTypeTypePropertyMetadata_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(name, propertyType, ownerType, typeMetadata);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    public static IEnumerable<object?[]> RegisterReadOnly_String_Type_Type_Validate_TestData()
    {
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData1", typeof(string), typeof(DependencyObject), null, null, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData6", typeof(int), typeof(DependencyObject), null, 0, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData10", typeof(int?), typeof(DependencyObject), null, null, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData15", typeof(List<int>), typeof(DependencyObject), null, null, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>(), 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObject), null, null, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>(), 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData23", typeof(object), typeof(DependencyObject), null, null, 4 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_Validate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 2 };
        yield return new object?[] { "            ", typeof(int), typeof(DependencyObject), null, 0, 4 };
        yield return new object?[] { " RegisterReadOnly_String_Type_Type_Validate_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 2 };
    }

    [Theory]
    [MemberData(nameof(RegisterReadOnly_String_Type_Type_Validate_TestData))]
    public void RegisterReadOnly_InvokeStringTypeTypePropertyMetadataValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue, int expectedValidateValueCallbackCallCount)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return true;
        };
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(name, propertyType, ownerType, typeMetadata, validateValueCallback);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Same(validateValueCallback, property.ValidateValueCallback);
        Assert.Equal(expectedValidateValueCallbackCallCount, callCount);
    }

    public static IEnumerable<object?[]> RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData()
    {
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData1", typeof(string), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData6", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData10", typeof(int?), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData15", typeof(List<int>), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData23", typeof(object), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "             ", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { " RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData8 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData))]
    public void RegisterReadOnly_InvokeStringTypeTypePropertyMetadataNullValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(name, propertyType, ownerType, typeMetadata, null);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    [Fact]
    public void RegisterReadOnly_Multiple_GlobalIndexDifferent()
    {
        DependencyProperty property1 = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name + "1", typeof(int), typeof(DependencyObject), new PropertyMetadata()).DependencyProperty;
        DependencyProperty property2 = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name + "2", typeof(int), typeof(DependencyObject), new PropertyMetadata()).DependencyProperty;
        Assert.NotEqual(property1.GlobalIndex, property2.GlobalIndex);
    }

    [Fact]
    public void RegisterReadOnly_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterReadOnly(null, typeof(int), typeof(DependencyObject), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterReadOnly(null, typeof(int), typeof(DependencyObject), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterReadOnly_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterReadOnly(string.Empty, typeof(int), typeof(DependencyObject), new PropertyMetadata()));
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterReadOnly(string.Empty, typeof(int), typeof(DependencyObject), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterReadOnly_NullPropertyType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyProperty), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyProperty), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterReadOnly_InvalidPropertyType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyObject), new PropertyMetadata()));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyObject), new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(void), "DefaultValue")]
    [InlineData(typeof(int), "DefaultValue")]
    [InlineData(typeof(string), 1)]
    [InlineData(typeof(int), null)]
    public void RegisterReadOnly_PropertyTypeDoesntMatchMetadata_ThrowsArgumentException(Type propertyType, object? defaultValue)
    {
        var metadata = new PropertyMetadata(defaultValue);
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyProperty), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyProperty), metadata, value => true));
    }

    [Fact]
    public void RegisterReadOnly_NullOwnerType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(DependencyPropertyTests))]
    public void RegisterReadOnly_OwnerTypeNotDependencyObjectWithMetadata_ThrowsArgumentException(Type ownerType)
    {
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, null));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata { DefaultValue = 1 }));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, null, value => true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata(), value => true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), ownerType, new PropertyMetadata { DefaultValue = 1 }, value => true));
    }

    [Fact]
    public void RegisterReadOnly_ExpressionDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(Activator.CreateInstance(typeof(Expression), nonPublic: true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterReadOnly_DispatcherObjectDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(new SubDispatcherObject());
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterReadOnly_AlreadyRegistered_ThrowsArgumentException()
    {
        DependencyProperty property = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyObject), new PropertyMetadata()).DependencyProperty;
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(property.Name, typeof(int), typeof(DependencyObject), new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(property.Name, typeof(int), typeof(DependencyObject), new PropertyMetadata(), value => true));
    }

    public static IEnumerable<object?[]> RegisterReadOnly_String_Type_Type_ValidateFail_TestData()
    {
        yield return new object?[] { " ", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { " ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_ValidateFail_TestData1", typeof(string), typeof(DependencyObject), null, null };
        yield return new object?[] { "RegisterReadOnly_String_Type_Type_ValidateFail_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { " RegisterReadOnly_String_Type_Type_ValidateFail_TestData3 ", typeof(int), typeof(DependencyObject), null, 0 };
        yield return new object?[] { " RegisterReadOnly_String_Type_Type_ValidateFail_TestData4 ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
    }

    [Theory]
    [MemberData(nameof(RegisterReadOnly_String_Type_Type_ValidateFail_TestData))]
    public void RegisterReadOnly_InvokeStringTypeTypeValidateFail_Throws(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return false;
        };
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterReadOnly(name, propertyType, ownerType, typeMetadata, validateValueCallback));
        Assert.Equal(1, callCount);
    }

    public static IEnumerable<object?[]> RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData()
    {
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "              ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData30", typeof(int), typeof(int), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData31", typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_TestData))]
    public void RegisterAttachedReadOnly_InvokeStringTypeTypePropertyMetadata_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyPropertyKey key = DependencyProperty.RegisterAttachedReadOnly(name, propertyType, ownerType, typeMetadata);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    public static IEnumerable<object?[]> RegisterAttachedReadOnly_String_Type_Type_Validate_TestData()
    {
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>(), 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>(), 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null, 2 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value", 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_Validate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null, 1 };
        yield return new object?[] { "               ", typeof(int), typeof(DependencyObjectTests), null, 0, 2 };
        yield return new object?[] { " RegisterAttachedReadOnly_String_Type_Type_Validate_TestData29 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttachedReadOnly_String_Type_Type_Validate_TestData))]
    public void RegisterAttachedReadOnly_InvokeStringTypeTypePropertyMetadataValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue, int expectedValidateValueCallbackCallCount)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return true;
        };
        DependencyPropertyKey key = DependencyProperty.RegisterAttachedReadOnly(name, propertyType, ownerType, typeMetadata, validateValueCallback);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Same(validateValueCallback, property.ValidateValueCallback);
        Assert.Equal(expectedValidateValueCallbackCallCount, callCount);
    }

    public static IEnumerable<object?[]> RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData()
    {
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData3", typeof(string), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData4", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData5", typeof(string), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData6", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData7", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData8", typeof(int), typeof(SubDependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData9", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData10", typeof(int?), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData11", typeof(int?), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData12", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData13", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData14", typeof(int?), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData15", typeof(List<int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData16", typeof(List<int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData17", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData18", typeof(List<int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new List<int>() }, new List<int>() };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData19", typeof(Dictionary<string, int>), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData20", typeof(Dictionary<string, int>), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData21", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData22", typeof(Dictionary<string, int>), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = new Dictionary<string, int>() }, new Dictionary<string, int>() };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData23", typeof(object), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData24", typeof(object), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData25", typeof(object), typeof(SubDependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData26", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = "value" }, "value" };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData27", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData28", typeof(object), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = null }, null };
        yield return new object?[] { "                ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData8 ", typeof(int), typeof(SubDependencyObject), new PropertyMetadata { DefaultValue = 1 }, 1 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttachedReadOnly_String_Type_Type_PropertyMetadata_NullValidate_TestData))]
    public void RegisterAttachedReadOnly_InvokeStringTypeTypePropertyMetadataNullValidate_Success(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        DependencyPropertyKey key = DependencyProperty.RegisterAttachedReadOnly(name, propertyType, ownerType, typeMetadata, null);
        Assert.NotNull(key.DependencyProperty);
        Assert.Same(key.DependencyProperty, key.DependencyProperty);

        DependencyProperty property = key.DependencyProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(expectedDefaultValue, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.Equal(ownerType, property.OwnerType);
        Assert.True(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
    }

    [Fact]
    public void RegisterAttachedReadOnly_Multiple_GlobalIndexDifferent()
    {
        DependencyProperty property1 = DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name + "1", typeof(int), typeof(DependencyObjectTests), new PropertyMetadata()).DependencyProperty;
        DependencyProperty property2 = DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name + "2", typeof(int), typeof(DependencyObjectTests), new PropertyMetadata()).DependencyProperty;
        Assert.NotEqual(property1.GlobalIndex, property2.GlobalIndex);
    }

    [Fact]
    public void RegisterAttachedReadOnly_NullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterAttachedReadOnly(null, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("name", () => DependencyProperty.RegisterAttachedReadOnly(null, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_EmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterAttachedReadOnly(string.Empty, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentException>("name", () => DependencyProperty.RegisterAttachedReadOnly(string.Empty, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_NullPropertyType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("propertyType", () => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, null!, typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_InvalidPropertyType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<NotSupportedException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(void), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    [Theory]
    [InlineData(typeof(void), "DefaultValue")]
    [InlineData(typeof(int), "DefaultValue")]
    [InlineData(typeof(string), 1)]
    [InlineData(typeof(int), null)]
    public void RegisterAttachedReadOnly_PropertyTypeDoesntMatchMetadata_ThrowsArgumentException(Type propertyType, object? defaultValue)
    {
        var metadata = new PropertyMetadata(defaultValue);
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, propertyType, typeof(DependencyPropertyTests), metadata, value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_NullOwnerType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata()));
        Assert.Throws<ArgumentNullException>("ownerType", () => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), null!, new PropertyMetadata(), value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_ExpressionDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(Activator.CreateInstance(typeof(Expression), nonPublic: true));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_DispatcherObjectDefaultValue_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata(new SubDispatcherObject());
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), metadata, value => true));
    }

    [Fact]
    public void RegisterAttachedReadOnly_AlreadyRegisterAttacheded_ThrowsArgumentException()
    {
        DependencyProperty property = DependencyProperty.RegisterAttachedReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()).DependencyProperty;
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata()));
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(property.Name, typeof(int), typeof(DependencyPropertyTests), new PropertyMetadata(), value => true));
    }

    public static IEnumerable<object?[]> RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData()
    {
        yield return new object?[] { "         ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { "          ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData1", typeof(string), typeof(DependencyObjectTests), null, null };
        yield return new object?[] { "RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData2", typeof(string), typeof(DependencyObject), new PropertyMetadata(), null };
        yield return new object?[] { " RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData3 ", typeof(int), typeof(DependencyObjectTests), null, 0 };
        yield return new object?[] { " RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData4 ", typeof(int), typeof(DependencyObject), new PropertyMetadata(), 0 };
    }

    [Theory]
    [MemberData(nameof(RegisterAttachedReadOnly_String_Type_Type_ValidateFail_TestData))]
    public void RegisterAttachedReadOnly_InvokeStringTypeTypeValidateFail_Throws(string name, Type propertyType, Type ownerType, PropertyMetadata typeMetadata, object? expectedDefaultValue)
    {
        int callCount = 0;
        ValidateValueCallback validateValueCallback = value =>
        {
            Assert.Equal(expectedDefaultValue, value);
            callCount++;
            return false;
        };
        Assert.Throws<ArgumentException>(() => DependencyProperty.RegisterAttachedReadOnly(name, propertyType, ownerType, typeMetadata, validateValueCallback));
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsGlobalIndex()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyPropertyTests));
        Assert.Equal(property.GlobalIndex, property.GetHashCode());
    }

    [Fact]
    public void ToString_Invoke_ReturnsName()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyPropertyTests));
        Assert.Equal(property.Name, property.ToString());
    }

    private class SubDependencyObject : DependencyObject
    {
    }

    private class SubDispatcherObject : DispatcherObject
    {
    }

    private sealed class DependencyObjectTest1
    {
    }
}
