// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Windows.Tests;

public class PropertyMetadataTests
{
    [Fact]
    public void Ctor_Default()
    {
        var metadata = new SubPropertyMetadata();
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void Ctor_Object(object? defaultValue)
    {
        var metadata = new SubPropertyMetadata(defaultValue!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Fact]
    public void Ctor_PropertyChangedCallback()
    {
        int propertyChangedCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        var metadata = new SubPropertyMetadata(propertyChangedCallback);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
    }

    [Fact]
    public void Ctor_NullPropertyChangedCallback()
    {
        var metadata = new SubPropertyMetadata((PropertyChangedCallback)null!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void Ctor_Object_PropertyChangedCallback(object? defaultValue)
    {
        int propertyChangedCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        var metadata = new SubPropertyMetadata(defaultValue!, propertyChangedCallback);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void Ctor_Object_NullPropertyChangedCallback(object? defaultValue)
    {
        var metadata = new SubPropertyMetadata(defaultValue!, (PropertyChangedCallback)null!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.False(metadata.IsSealed);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void Ctor_Object_PropertyChangedCallback_CoerceValueCallback(object? defaultValue)
    {
        int propertyChangedCallbackCallCount = 0;
        int coerceValueCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        CoerceValueCallback coerceValueCallback = (d, v) => coerceValueCallbackCallCount++;
        var metadata = new SubPropertyMetadata(defaultValue!, propertyChangedCallback, coerceValueCallback);
        Assert.Equal(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
        Assert.Equal(0, coerceValueCallbackCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void Ctor_Object_NullPropertyChangedCallback_NullCoerceValueCallback(object? defaultValue)
    {
        var metadata = new SubPropertyMetadata(defaultValue!, (PropertyChangedCallback)null!, (CoerceValueCallback)null!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Fact]
    public void Ctor_UnsetDefaultValue_ThrowsArgumentException()
    {
        // TODO: add paramName.
        Assert.Throws<ArgumentException>(() => new PropertyMetadata(DependencyProperty.UnsetValue));
        Assert.Throws<ArgumentException>(() => new PropertyMetadata(DependencyProperty.UnsetValue, null));
        Assert.Throws<ArgumentException>(() => new PropertyMetadata(DependencyProperty.UnsetValue, null, null));
    }

    [Fact]
    public void CoerceValueCallback_Set_GetReturnsExpected()
    {
        var metadata = new PropertyMetadata();
        
        // Set value.
        int coerceValueCallbackCallCount = 0;
        CoerceValueCallback coerceValueCallback = (d, e) => coerceValueCallbackCallCount++;
        metadata.CoerceValueCallback = coerceValueCallback;
        Assert.Equal(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Equal(0, coerceValueCallbackCallCount);

        // Set same.
        metadata.CoerceValueCallback = coerceValueCallback;
        Assert.Equal(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Equal(0, coerceValueCallbackCallCount);

        // Set null.
        metadata.CoerceValueCallback = null;
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Equal(0, coerceValueCallbackCallCount);
    }

    [Fact]
    public void CoerceValueCallback_SetSealed_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(PropertyMetadataTests) + MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyObjectTests));
        PropertyMetadata metadata = property.DefaultMetadata;
        Assert.Throws<InvalidOperationException>(() => metadata.CoerceValueCallback = (d, v) => v);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void DefaultValue_Set_GetReturnsExpected(object? value)
    {
        var metadata = new PropertyMetadata
        {
            // Set value.
            DefaultValue = value
        };
        Assert.Equal(value, metadata.DefaultValue);

        // Set same.
        metadata.DefaultValue = value;
        Assert.Equal(value, metadata.DefaultValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void DefaultValue_SetDifferent_GetReturnsExpected(object? value)
    {
        var metadata = new PropertyMetadata
        {
            DefaultValue = new object()
        };
        
        // Set value.
        metadata.DefaultValue = value;
        Assert.Equal(value, metadata.DefaultValue);

        // Set same.
        metadata.DefaultValue = value;
        Assert.Equal(value, metadata.DefaultValue);
    }

    [Fact]
    public void DefaultValue_SetUnset_ThrowsArgumentException()
    {
        var metadata = new PropertyMetadata();
        // TODO: add paramName.
        Assert.Throws<ArgumentException>(() => metadata.DefaultValue = DependencyProperty.UnsetValue);
    }

    [Fact]
    public void DefaultValue_SetSealed_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(PropertyMetadataTests) + MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyObjectTests));
        PropertyMetadata metadata = property.DefaultMetadata;
        Assert.Throws<InvalidOperationException>(() => metadata.DefaultValue = new object());
    }

    [Fact]
    public void IsSealed_GetSealed_ReturnsTrue()
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(nameof(PropertyMetadataTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new SubPropertyMetadata());
        SubPropertyMetadata metadata = Assert.IsType<SubPropertyMetadata>(property.DefaultMetadata);
        Assert.True(metadata.IsSealed);
    }

    [Fact]
    public void PropertyChangedCallback_Set_GetReturnsExpected()
    {
        var metadata = new PropertyMetadata();
        
        // Set value.
        int propertyChangedCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        metadata.PropertyChangedCallback = propertyChangedCallback;
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);

        // Set same.
        metadata.PropertyChangedCallback = propertyChangedCallback;
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);

        // Set null.
        metadata.PropertyChangedCallback = null;
        Assert.Null(metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
    }

    [Fact]
    public void PropertyChangedCallback_SetSealed_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(PropertyMetadataTests) + MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyObjectTests));
        PropertyMetadata metadata = property.DefaultMetadata;
        Assert.Throws<InvalidOperationException>(() => metadata.PropertyChangedCallback = (d, e) => { });
    }

    [Fact]
    public void Merge_InvokeUnchangedOnUnchanged_Success()
    {
        var metadata = new SubPropertyMetadata();
        var baseMetadata = new PropertyMetadata();
        
        metadata.Merge(baseMetadata, null!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);

        // Make sure base metadata is unchanged.
        Assert.Null(baseMetadata.CoerceValueCallback);
        Assert.Null(baseMetadata.DefaultValue);
        Assert.Null(baseMetadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeCustomOnUnchanged_Success()
    {
        var metadata = new SubPropertyMetadata();
        var defaultValue = new object();
        int propertyChangedCallbackCallCount = 0;
        int coerceValueCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        CoerceValueCallback coerceValueCallback = (d, v) => coerceValueCallbackCallCount++;
        var baseMetadata = new PropertyMetadata(defaultValue, propertyChangedCallback, coerceValueCallback);
        
        metadata.Merge(baseMetadata, null!);
        Assert.Equal(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
        Assert.Equal(0, coerceValueCallbackCallCount);

        // Make sure base metadata is unchanged.
        Assert.Equal(coerceValueCallback, baseMetadata.CoerceValueCallback);
        Assert.Equal(defaultValue, baseMetadata.DefaultValue);
        Assert.Equal(propertyChangedCallback, baseMetadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeUnchangedOnCustom_Success()
    {
        var defaultValue = new object();
        int propertyChangedCallbackCallCount = 0;
        int coerceValueCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        CoerceValueCallback coerceValueCallback = (d, v) => coerceValueCallbackCallCount++;
        var metadata = new SubPropertyMetadata(defaultValue, propertyChangedCallback, coerceValueCallback);
        var baseMetadata = new PropertyMetadata();
        
        metadata.Merge(baseMetadata, null!);
        Assert.Equal(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Equal(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Equal(propertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount);
        Assert.Equal(0, coerceValueCallbackCallCount);

        // Make sure base metadata is unchanged.
        Assert.Null(baseMetadata.CoerceValueCallback);
        Assert.Null(baseMetadata.DefaultValue);
        Assert.Null(baseMetadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeCustomOnCustom_Success()
    {
        var defaultValue1 = new object();
        int propertyChangedCallbackCallCount1 = 0;
        int coerceValueCallbackCallCount1 = 0;
        PropertyChangedCallback propertyChangedCallback1 = (d, e) => propertyChangedCallbackCallCount1++;
        CoerceValueCallback coerceValueCallback1 = (d, v) => coerceValueCallbackCallCount1++;
        var metadata = new SubPropertyMetadata(defaultValue1, propertyChangedCallback1, coerceValueCallback1);
        
        var defaultValue2 = new object();
        int propertyChangedCallbackCallCount2 = 0;
        int coerceValueCallbackCallCount2 = 0;
        PropertyChangedCallback propertyChangedCallback2 = (d, e) => propertyChangedCallbackCallCount2++;
        CoerceValueCallback coerceValueCallback2 = (d, v) => coerceValueCallbackCallCount2++;
        var baseMetadata = new SubPropertyMetadata(defaultValue2, propertyChangedCallback2, coerceValueCallback2);
        
        metadata.Merge(baseMetadata, null!);
        Assert.Equal(coerceValueCallback1, metadata.CoerceValueCallback);
        Assert.Equal(defaultValue1, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.NotNull(metadata.PropertyChangedCallback);
        Assert.Same(metadata.PropertyChangedCallback, metadata.PropertyChangedCallback);
        Assert.NotEqual(propertyChangedCallback1, metadata.PropertyChangedCallback);
        Assert.NotEqual(propertyChangedCallback2, metadata.PropertyChangedCallback);
        Assert.Equal(0, propertyChangedCallbackCallCount1);
        Assert.Equal(0, coerceValueCallbackCallCount1);
        Assert.Equal(0, propertyChangedCallbackCallCount2);
        Assert.Equal(0, coerceValueCallbackCallCount2);

        // Make sure base metadata is unchanged.
        Assert.Equal(coerceValueCallback2, baseMetadata.CoerceValueCallback);
        Assert.Equal(defaultValue2, baseMetadata.DefaultValue);
        Assert.Equal(propertyChangedCallback2, baseMetadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeSelfUnchanged_Success()
    {
        var metadata = new SubPropertyMetadata();
        metadata.Merge(metadata, null!);
        Assert.Null(metadata.CoerceValueCallback);
        Assert.Null(metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.Null(metadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeSelfChanged_Success()
    {
        var defaultValue = new object();
        int propertyChangedCallbackCallCount = 0;
        PropertyChangedCallback propertyChangedCallback = (d, e) => propertyChangedCallbackCallCount++;
        int coerceValueCallbackCallCount = 0;
        CoerceValueCallback coerceValueCallback = (d, v) => coerceValueCallbackCallCount++;
        var metadata = new SubPropertyMetadata(defaultValue, propertyChangedCallback, coerceValueCallback);

        metadata.Merge(metadata, null!);
        Assert.Same(coerceValueCallback, metadata.CoerceValueCallback);
        Assert.Same(defaultValue, metadata.DefaultValue);
        Assert.False(metadata.IsSealed);
        Assert.NotNull(metadata.PropertyChangedCallback);
        Assert.NotSame(propertyChangedCallback, metadata.PropertyChangedCallback);
    }

    [Fact]
    public void Merge_InvokeBaseHasPropertyChangedCallback_AddsBaseHandlers()
    {
        var metadata = new SubPropertyMetadata();
        var events = new List<string>();
        var baseMetadata = new PropertyMetadata();
        baseMetadata.PropertyChangedCallback += (d, e) => events.Add("Event3");
        baseMetadata.PropertyChangedCallback += (d, e) => events.Add("Event4");

        metadata.Merge(baseMetadata, null!);
        Assert.Equal(baseMetadata.PropertyChangedCallback, metadata.PropertyChangedCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.PropertyChangedCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event3", "Event4" }, events);
    }

    [Fact]
    public void Merge_InvokeCurrentHasPropertyChangedCallback_KeepsCurrentHandlers()
    {
        var events = new List<string>();
        var metadata = new SubPropertyMetadata();
        metadata.PropertyChangedCallback += (d, e) => events.Add("Event1");
        metadata.PropertyChangedCallback += (d, e) => events.Add("Event2");
        var baseMetadata = new PropertyMetadata();

        metadata.Merge(baseMetadata, null!);
        Assert.NotEqual(baseMetadata.PropertyChangedCallback, metadata.PropertyChangedCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.PropertyChangedCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event1", "Event2" }, events);
    }

    [Fact]
    public void Merge_InvokeCurrentAndBaseHavePropertyChangedCallback_AddsBaseHandlersAtStart()
    {
        var events = new List<string>();
        var metadata = new SubPropertyMetadata();
        metadata.PropertyChangedCallback += (d, e) => events.Add("Event1");
        metadata.PropertyChangedCallback += (d, e) => events.Add("Event2");
        var baseMetadata = new PropertyMetadata();
        baseMetadata.PropertyChangedCallback += (d, e) => events.Add("Event3");
        baseMetadata.PropertyChangedCallback += (d, e) => events.Add("Event4");

        metadata.Merge(baseMetadata, null!);
        Assert.NotEqual(baseMetadata.PropertyChangedCallback, metadata.PropertyChangedCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.PropertyChangedCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event3", "Event4", "Event1", "Event2" }, events);
    }
    
    [Fact]
    public void Merge_InvokeBaseHasCoerceValueCallback_AddsBaseHandlers()
    {
        var events = new List<string>();
        var metadata = new SubPropertyMetadata();
        var baseMetadata = new PropertyMetadata();
        baseMetadata.CoerceValueCallback += (d, e) => { events.Add("Event3"); return 1; };
        baseMetadata.CoerceValueCallback += (d, e) => { events.Add("Event4"); return 1; };

        metadata.Merge(baseMetadata, null!);
        Assert.Equal(baseMetadata.CoerceValueCallback, metadata.CoerceValueCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.CoerceValueCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event3", "Event4" }, events);
    }
    
    [Fact]
    public void Merge_InvokeCurrentHasCoerceValueCallback_KeepsCurrentHandlers()
    {
        var events = new List<string>();
        var metadata = new SubPropertyMetadata();
        metadata.CoerceValueCallback += (d, e) => { events.Add("Event1"); return 1; };
        metadata.CoerceValueCallback += (d, e) => { events.Add("Event2"); return 1; };
        var baseMetadata = new PropertyMetadata();

        metadata.Merge(baseMetadata, null!);
        Assert.NotEqual(baseMetadata.CoerceValueCallback, metadata.CoerceValueCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.CoerceValueCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event1", "Event2" }, events);
    }
    
    [Fact]
    public void Merge_InvokeCurrentAndBaseHaveCoerceValueCallback_DoesNotAddBaseHandlers()
    {
        var events = new List<string>();
        var metadata = new SubPropertyMetadata();
        metadata.CoerceValueCallback += (d, e) => { events.Add("Event1"); return 1; };
        metadata.CoerceValueCallback += (d, e) => { events.Add("Event2"); return 1; };
        var baseMetadata = new PropertyMetadata();
        baseMetadata.CoerceValueCallback += (d, e) => { events.Add("Event3"); return 1; };
        baseMetadata.CoerceValueCallback += (d, e) => { events.Add("Event4"); return 1; };

        metadata.Merge(baseMetadata, null!);
        Assert.NotEqual(baseMetadata.CoerceValueCallback, metadata.CoerceValueCallback);

        var obj = new DependencyObject();
        var e = new DependencyPropertyChangedEventArgs();
        metadata.CoerceValueCallback.Invoke(obj, e);
        Assert.Equal(new[] { "Event1", "Event2" }, events);
    }

    [Fact]
    public void Merge_NullBaseMetadata_ThrowsArgumentNullException()
    {
        var metadata = new SubPropertyMetadata();
        Assert.Throws<ArgumentNullException>("baseMetadata", () => metadata.Merge(null!, null!));
    }

    [Fact]
    public void Merge_InvokeSealed_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.RegisterAttached(nameof(PropertyMetadataTests) + MethodBase.GetCurrentMethod()!.Name, typeof(int), typeof(DependencyObject), new SubPropertyMetadata());
        SubPropertyMetadata metadata = Assert.IsType<SubPropertyMetadata>(property.DefaultMetadata);
        Assert.Throws<InvalidOperationException>(() => metadata.Merge(new PropertyMetadata(), null!));
    }

    private static readonly DependencyProperty s_property = DependencyProperty.Register(nameof(PropertyMetadataTests), typeof(string), typeof(DependencyObject));

    public static IEnumerable<object?[]> OnApply_TestData()
    {
        yield return new object?[] { s_property, typeof(bool) };
        yield return new object?[] { s_property, null };
        yield return new object?[] { null, typeof(bool) };
        yield return new object?[] { null, null };
    }

    [Theory]
    [MemberData(nameof(OnApply_TestData))]
    public void OnApply_Invoke_Nop(DependencyProperty dp, Type targetType)
    {
        var metadata = new SubPropertyMetadata();
        metadata.OnApply(dp, targetType);
    }

    private class SubPropertyMetadata : PropertyMetadata
    {
        public SubPropertyMetadata() : base()
        {
        }

        public SubPropertyMetadata(object defaultValue) : base(defaultValue)
        {
        }

        public SubPropertyMetadata(PropertyChangedCallback propertyChangedCallback) : base(propertyChangedCallback)
        {
        }

        public SubPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) : base(defaultValue, propertyChangedCallback)
        {
        }

        public SubPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) : base(defaultValue, propertyChangedCallback, coerceValueCallback)
        {
        }

        public new bool IsSealed => base.IsSealed;

        public new void Merge(PropertyMetadata baseMetadata, DependencyProperty dp)
            => base.Merge(baseMetadata, dp);

        public new void OnApply(DependencyProperty dp, Type targetType)
            => base.OnApply(dp, targetType);
    }
}