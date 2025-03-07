// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Windows.Threading;

namespace System.Windows.Tests;

public class DependencyObjectTests
{
    [Fact]
    public void Ctor_Default()
    {
        var obj = new DependencyObject();
        Assert.NotNull(obj.DependencyObjectType);
        Assert.Same(obj.DependencyObjectType, obj.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(DependencyObject)), obj.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, obj.Dispatcher);
        Assert.False(obj.IsSealed);
    }

    [Fact]
    public void DependencyObjectType_Get_ReturnsExpected()
    {
        var obj = new DependencyObject();
        Assert.NotNull(obj.DependencyObjectType);
        Assert.Same(obj.DependencyObjectType, obj.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(DependencyObject)), obj.DependencyObjectType);
    }

    [Fact]
    public void DependencyObjectType_GetOnDifferentThread_ReturnsExpected()
    {
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.NotNull(obj.DependencyObjectType);
            Assert.Same(obj.DependencyObjectType, obj.DependencyObjectType);
            Assert.Same(DependencyObjectType.FromSystemType(typeof(DependencyObject)), obj.DependencyObjectType);
        });
    }
    
    [Fact]
    public void Dispatcher_Get_ReturnsExpected()
    {
        var obj = new DependencyObject();
        Assert.Same(Dispatcher.CurrentDispatcher, obj.Dispatcher);
    }

    [Fact]
    public void Dispatcher_GetOnDifferentThread_ReturnsExpected()
    {
        var obj = new DependencyObject();
        Dispatcher expected = Dispatcher.CurrentDispatcher;
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Same(expected, obj.Dispatcher);
        });
    }

    [Fact]
    public void IsSealed_Get_ReturnsFalse()
    {
        var obj = new DependencyObject();
        Assert.False(obj.IsSealed);
    }

    [Fact]
    public void IsSealed_GetOnDifferentThread_ReturnsFalse()
    {
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.False(obj.IsSealed);
        });
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyValueType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Clear default.
        obj.ClearValue(property);
        Assert.False((bool)obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(property);
        Assert.False((bool)obj.GetValue(property));

        // Clear again.
        obj.ClearValue(property);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyNullable_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyReferenceType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyObject_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyCustomDefaultValue_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        var typeMetadata = new PropertyMetadata("default");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);

        // Clear default.
        obj.ClearValue(property);
        Assert.Equal("default", obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(property);
        Assert.Equal("default", obj.GetValue(property));

        // Clear again.
        obj.ClearValue(property);
        Assert.Equal("default", obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyCustomPropertyChangedCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(null, (d, e) =>
        {
            Assert.Same(obj, d);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            changedCallCount++;
        });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyCustomCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Clear custom.
        expectedNewValue = null;
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyCustomPropertyChangedAndCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            },
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);
        Assert.Equal(0, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyCustomOnPropertyChanged_CallsOnPropertyChanged()
    {
        var obj = new CustomDependencyObject();

        int onPropertyChangedCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        obj.OnPropertyChangedAction = (e) =>
        {
            Assert.True(changedCallCount > onPropertyChangedCallCount);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            onPropertyChangedCallCount++;
        };
        var typeMetadata = new PropertyMetadata(null, (d, e) =>
        {
            Assert.Equal(changedCallCount, onPropertyChangedCallCount);
            Assert.Same(obj, d);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            changedCallCount++;
        });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);
        Assert.Equal(0, onPropertyChangedCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, onPropertyChangedCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(2, onPropertyChangedCallCount);

        // Clear again.
        obj.ClearValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(2, onPropertyChangedCallCount);
    }

    [Fact]
    public void ClearValue_DependencyPropertyNullDp_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("dp", () => obj.ClearValue((DependencyProperty)null!));
    }

    [Fact]
    public void ClearValue_DependencyPropertyReadOnly_ThrowsInvalidOperationException()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
        });
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyValueType_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Clear default.
        obj.ClearValue(key);
        Assert.False((bool)obj.GetValue(property));

        // Set custom.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(key);
        Assert.False((bool)obj.GetValue(property));

        // Clear again.
        obj.ClearValue(key);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyNullable_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyReferenceType_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyObject_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyCustomDefaultValue_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        var typeMetadata = new PropertyMetadata("default");
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;

        // Clear default.
        obj.ClearValue(key);
        Assert.Equal("default", obj.GetValue(property));

        // Set custom.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Clear custom.
        obj.ClearValue(key);
        Assert.Equal("default", obj.GetValue(property));

        // Clear again.
        obj.ClearValue(key);
        Assert.Equal("default", obj.GetValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyCustomPropertyChangedCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(null, (d, e) =>
        {
            Assert.Same(obj, d);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            changedCallCount++;
        });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyCustomCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Clear custom.
        expectedNewValue = null;
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyCustomPropertyChangedAndCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            },
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);
        Assert.Equal(0, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyCustomOnPropertyChanged_CallsOnPropertyChanged()
    {
        var obj = new CustomDependencyObject();

        int onPropertyChangedCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        obj.OnPropertyChangedAction = (e) =>
        {
            Assert.True(changedCallCount > onPropertyChangedCallCount);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            onPropertyChangedCallCount++;
        };
        var typeMetadata = new PropertyMetadata(null, (d, e) =>
        {
            Assert.Equal(changedCallCount, onPropertyChangedCallCount);
            Assert.Same(obj, d);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            changedCallCount++;
        });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Clear default.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);
        Assert.Equal(0, onPropertyChangedCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, onPropertyChangedCallCount);

        // Clear custom.
        expectedNewValue = null;
        expectedOldValue = "value";
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(2, onPropertyChangedCallCount);

        // Clear again.
        obj.ClearValue(key);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(2, onPropertyChangedCallCount);
    }

    [Fact]
    public void ClearValue_DependencyPropertyNullKey_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("key", () => obj.ClearValue((DependencyPropertyKey)null!));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(key));
        });
    }

    [Fact]
    public void CoerceValue_InvokeValueType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Coerce default.
        obj.CoerceValue(property);
        Assert.False((bool)obj.GetValue(property));

        // Set true.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Coerce true.
        obj.CoerceValue(property);
        Assert.True((bool)obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.True((bool)obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeNullable_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.True((bool)obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.True((bool)obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeReferenceType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeObject_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));

        // Set custom.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));

        // Set different.
        obj.SetValue(property, 1);
        Assert.Equal(1, obj.GetValue(property));

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal(1, obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal(1, obj.GetValue(property));

        // Set null.
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeCustomPropertyChangeCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
    }

    [Fact]
    public void CoerceValue_InvokeCustomCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("customCoercedValue")]
    public void CoerceValue_InvokeCustomCoerceValueCallbackReturnsCustom_Success(object? customCoercedValue)
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        object? newBaseValue = "coercedValue";
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return newBaseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + customCoercedValue?.GetType().Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(property, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Coerce custom.
        newBaseValue = customCoercedValue;
        obj.CoerceValue(property);
        Assert.Equal(customCoercedValue, obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal(customCoercedValue, obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void CoerceValue_InvokeCustomCoerceValueCallbackReturnsUnset_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedBaseValue = null;
        object? newBaseValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedBaseValue, baseValue);
                coerceValueCallCount++;
                return newBaseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedBaseValue = "value";
        newBaseValue = "coercedValue";
        obj.SetValue(property, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Coerce.
        expectedBaseValue = "value";
        newBaseValue = DependencyProperty.UnsetValue;
        obj.CoerceValue(property);
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);
    }

    [Fact]
    public void CoerceValue_InvokeChangeAndCoerceValueCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            },
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Coerce default.
        obj.CoerceValue(property);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(0, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(2, coerceValueCallCount);

        // Coerce custom.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(3, coerceValueCallCount);

        // Coerce again.
        obj.CoerceValue(property);
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void CoerceValue_InvokeReadOnly_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Coerce default.
        obj.CoerceValue(property);
        Assert.False((bool)obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_DependencyPropertyNullDp_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        // TODO: this should throw ArgumentNullException
        //Assert.Throws<ArgumentNullException>("dp", () => obj.CoerceValue(null!));
        Assert.Throws<NullReferenceException>(() => obj.CoerceValue(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void CoerceValue_InvalidResultDefault_ThrowsArgumentException(object? invalidValue)
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            0,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Equal(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return invalidValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + invalidValue?.GetType().Name, typeof(int), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Coerce default.
        expectedNewValue = 0;
        Assert.Throws<ArgumentException>(() => obj.CoerceValue(property));
        Assert.Equal(0, obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void CoerceValue_InvalidResultCustom_ThrowsArgumentException(object? invalidValue)
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        object? newBaseValue = 1;
        var typeMetadata = new PropertyMetadata(
            0,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Equal(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return newBaseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + invalidValue?.GetType().Name, typeof(int), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = 2;
        newBaseValue = 2;
        obj.SetValue(property, 2);
        Assert.Equal(2, obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Coerce custom.
        newBaseValue = invalidValue;
        Assert.Throws<ArgumentException>(() => obj.CoerceValue(property));
        Assert.Equal(2, coerceValueCallCount);
    }

    [Fact]
    public void CoerceValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.CoerceValue(property));
        });
    }

    [Fact]
    public void Equals_Invoke_ReturnsExpected()
    {
        var obj = new DependencyObject();
        Assert.True(obj.Equals(obj));
        Assert.False(obj.Equals(new DependencyObject()));
        Assert.False(obj.Equals(null));
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsEqual()
    {
        var obj = new DependencyObject();
        Assert.NotEqual(0, obj.GetHashCode());
        Assert.Equal(obj.GetHashCode(), obj.GetHashCode());
    }

    [Fact]
    public void GetLocalValueEnumerator_InvokeNoProperties_ReturnsExpected()
    {
        var obj = new DependencyObject();
        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Fact]
    public void GetLocalValueEnumerator_InvokeWithProperties_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move.
            Assert.True(enumerator.MoveNext());
            LocalValueEntry entry = enumerator.Current;
            Assert.Same(property, entry.Property);
            Assert.Equal("value", entry.Value);
            
            entry = Assert.IsType<LocalValueEntry>(((IEnumerator)enumerator).Current);
            Assert.Same(property, entry.Property);
            Assert.Equal("value", entry.Value);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Reset.
            enumerator.Reset();
        }
    }
    

    [Fact]
    public void GetLocalValueEnumerator_InvokeWithPropertiesCoerced_ReturnsExpected()
    {
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) => "coercedValue");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move.
            Assert.True(enumerator.MoveNext());
            LocalValueEntry entry = enumerator.Current;
            Assert.Same(property, entry.Property);
            Assert.Equal("value", entry.Value);
            
            entry = Assert.IsType<LocalValueEntry>(((IEnumerator)enumerator).Current);
            Assert.Same(property, entry.Property);
            Assert.Equal("value", entry.Value);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Fact]
    public void GetLocalValueEnumerator_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.GetLocalValueEnumerator());
        });
    }

    [Fact]
    public void GetValue_InvokeValueType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeNullableType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReferenceType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeObjectType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeDefaultValue_ReturnsExpected()
    {
        var metadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeSetValue_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeSetValueCoerced_ReturnsExpected()
    {
        object coerceResult = "coercedValue";
        int coerceValueCallbackCallCount = 0;
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                coerceValueCallbackCallCount++;
                return coerceResult;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        Assert.Equal(1, coerceValueCallbackCallCount);

        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Call again to test caching.
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Change coerce result.
        coerceResult = "coercedValue2";
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Set to unset.
        coerceResult = DependencyProperty.UnsetValue;
        obj.SetValue(property, "value2");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallbackCallCount);
    }

    [Fact]
    public void GetValue_InvokeClearedValue_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        obj.ClearValue(property);

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeCustomDefaultValue_ReturnsExpected()
    {
        var obj = new DependencyObject();
        var typeMetadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyProperty_ReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyPropertySet_ReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        obj.SetValue(key, "value");

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyPropertyUIElementIsVisible_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = UIElement.IsVisibleProperty;

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyPropertyUIElement3DIsVisible_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = UIElement3D.IsVisibleProperty;

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyPropertyFrameworkElementActualWidth_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = FrameworkElement.ActualWidthProperty;

        Assert.Equal(0.0, obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal(0.0, obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyPropertyFrameworkElementActualHeight_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = FrameworkElement.ActualHeightProperty;

        Assert.Equal(0.0, obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal(0.0, obj.GetValue(property));
    }

    [Fact]
    public void GetValue_NullProperty_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("dp", () => obj.GetValue(null!));
    }

    [Fact]
    public void GetValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.GetValue(property));
        });
    }

    [Fact]
    public void InvalidateProperty_InvokeNoSuchProperty_Success()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Invalidate.
        obj.InvalidateProperty(property);
        Assert.Null(obj.GetValue(property));

        // Invalidate again.
        obj.InvalidateProperty(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void InvalidateProperty_InvokeSetProperty_Success()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");

        // Invalidate.
        obj.InvalidateProperty(property);
        Assert.Equal("value", obj.GetValue(property));

        // Invalidate again.
        obj.InvalidateProperty(property);
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void InvalidateProperty_InvokeClearedProperty_Success()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        obj.ClearValue(property);

        // Invalidate.
        obj.InvalidateProperty(property);
        Assert.Null(obj.GetValue(property));

        // Invalidate again.
        obj.InvalidateProperty(property);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void InvalidateProperty_InvokeCoercedProperty_Success()
    {
        object coerceResult = "baseValue";
        int coerceValueCallbackCallCount = 0;
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                coerceValueCallbackCallCount++;
                return coerceResult;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        Assert.Equal(coerceResult, obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Change coerce result.
        coerceResult = "other";
        Assert.Equal("baseValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Invalidate.
        obj.InvalidateProperty(property);
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallbackCallCount);

        // Invalidate again.
        obj.InvalidateProperty(property);
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallbackCallCount);

        // Set to unset.
        coerceResult = DependencyProperty.UnsetValue;
        obj.InvalidateProperty(property);
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(4, coerceValueCallbackCallCount);
    }

    [Fact]
    public void InvalidateProperty_NullDp_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("dp", () => obj.InvalidateProperty(null!));
    }

    [Fact]
    public void InvalidateProperty_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.InvalidateProperty(property));
        });
    }

    [Fact]
    public void InvalidateProperty_InvokeCoercedPropertyInvalid_ThrowsArgumentException()
    {
        object coerceResult = "baseValue";
        int coerceValueCallbackCallCount = 0;
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                coerceValueCallbackCallCount++;
                return coerceResult;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        Assert.Equal(coerceResult, obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Change coerce result.
        coerceResult = new object();
        Assert.Equal("baseValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Invalidate.
        coerceResult = new object();
        Assert.Throws<ArgumentException>(() => obj.InvalidateProperty(property));
        Assert.Equal("baseValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallbackCallCount);

        // Invalidate again.
        Assert.Throws<ArgumentException>(() => obj.InvalidateProperty(property));
        Assert.Equal("baseValue", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallbackCallCount);
    }

    [Fact]
    public void ReadLocalValue_InvokeValueType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeNullableType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReferenceType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeObjectType_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));
        var obj = new DependencyObject();

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeDefaultValue_ReturnsExpected()
    {
        var metadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeSetValue_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");

        Assert.Equal("value", obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeSetValueCoerced_ReturnsExpected()
    {
        string coerceResult = "coercedValue";
        int coerceValueCallbackCallCount = 0;
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                coerceValueCallbackCallCount++;
                return coerceResult;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        Assert.Equal(1, coerceValueCallbackCallCount);

        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Call again to test caching.
        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Change coerce result.
        coerceResult = "value";
        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);
    }

    [Fact]
    public void ReadLocalValue_InvokeSetValueCoercedUnset_ReturnsExpected()
    {
        object coerceResult = "coercedValue";
        int coerceValueCallbackCallCount = 0;
        var metadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                coerceValueCallbackCallCount++;
                return coerceResult;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        Assert.Equal(1, coerceValueCallbackCallCount);

        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Call again to test caching.
        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Change coerce result.
        coerceResult = "value";
        Assert.Equal("value", obj.ReadLocalValue(property));
        Assert.Equal(1, coerceValueCallbackCallCount);

        // Set to unset.
        coerceResult = DependencyProperty.UnsetValue;
        obj.SetValue(property, "value2");
        Assert.Equal("value2", obj.ReadLocalValue(property));
        Assert.Equal(2, coerceValueCallbackCallCount);
    }

    [Fact]
    public void ReadLocalValue_InvokeClearedValue_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "value");
        obj.ClearValue(property);

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeCustomDefaultValue_ReturnsExpected()
    {
        var obj = new DependencyObject();
        var typeMetadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyProperty_ReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyPropertySet_ReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        obj.SetValue(key, "value");

        Assert.Equal("value", obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyPropertyUIElementIsVisible_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = UIElement.IsVisibleProperty;

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyPropertyUIElement3DIsVisible_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = UIElement3D.IsVisibleProperty;

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyPropertyFrameworkElementActualWidth_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = FrameworkElement.ActualWidthProperty;

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_InvokeReadOnlyPropertyFrameworkElementActualHeight_ReturnsExpected()
    {
        // This has a special internal getter - GetReadOnlyValueCallback.
        var obj = new DependencyObject();
        DependencyProperty property = FrameworkElement.ActualHeightProperty;

        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));

        // Call again to test caching.
        Assert.Same(DependencyProperty.UnsetValue, obj.ReadLocalValue(property));
    }

    [Fact]
    public void ReadLocalValue_NullProperty_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("dp", () => obj.ReadLocalValue(null!));
    }

    [Fact]
    public void ReadLocalValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ReadLocalValue(property));
        });
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyValueType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Set true.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Set same.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Set false.
        obj.SetValue(property, false);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyNullable_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Set custom.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Set same.
        obj.SetValue(property, true);
        Assert.True((bool)obj.GetValue(property));

        // Set null.
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));

        // Set false.
        obj.SetValue(property, false);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyReferenceType_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Set value.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set different.
        obj.SetValue(property, "other");
        Assert.Equal("other", obj.GetValue(property));

        // Set null.
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyObject_GetValueReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));
        var obj = new DependencyObject();

        // Set value.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set different.
        obj.SetValue(property, 1);
        Assert.Equal(1, obj.GetValue(property));

        // Set null.
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyWithPropertyChangeCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(property, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyWithCoerceCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        obj.SetValue(property, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyWithCoerceCallbackCustom_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return "coercedValue";
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(property, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        obj.SetValue(property, "other");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        obj.SetValue(property, null);
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyWithPropertyChangeAndCoerceCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            },
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(property, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyCustomOnPropertyChanged_CallsOnPropertyChanged()
    {
        var obj = new CustomDependencyObject();

        int onPropertyChangedCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        obj.OnPropertyChangedAction = (e) =>
        {
            Assert.True(changedCallCount > onPropertyChangedCallCount);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            onPropertyChangedCallCount++;
        };
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            });
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set same.
        obj.SetValue(property, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(property, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(property, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
    }

    [Fact]
    public void SetValue_DependencyPropertyNullDp_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("dp", () => obj.SetValue((DependencyProperty)null!, true));
    }

    [Fact]
    public void SetValue_DependencyPropertyReadOnly_ThrowsInvalidOperationException()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Assert.Throws<InvalidOperationException>(() => obj.SetValue(property, "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyInvalidValueValueType_ThrowsArgumentException(object? value)
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool), typeof(DependencyObject));
        var obj = new DependencyObject();
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(property, value));
    }

    [Theory]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyInvalidValueNullable_ThrowsArgumentException(object value)
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool?), typeof(DependencyObject));
        var obj = new DependencyObject();
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(property, value));
    }

    public static IEnumerable<object[]> SetValue_DependencyPropertyInvalidValueReferenceType_TestData()
    {
        yield return new object[] { new object() };
        yield return new object[] { 1 };
    }

    [Theory]
    [MemberData(nameof(SetValue_DependencyPropertyInvalidValueReferenceType_TestData))]
    public void SetValue_DependencyPropertyInvalidValueReferenceType_ThrowsArgumentException(object value)
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(property, value));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyOnDifferentThread_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.SetValue(property, "value"));
        });
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyValueType_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Set true.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Set same.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Set false.
        obj.SetValue(key, false);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyNullable_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Set true.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Set same.
        obj.SetValue(key, true);
        Assert.True((bool)obj.GetValue(property));

        // Set null.
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));

        // Set false.
        obj.SetValue(key, false);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyReferenceType_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Set value.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set different.
        obj.SetValue(key, "other");
        Assert.Equal("other", obj.GetValue(property));

        // Set null.
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyObject_GetValueReturnsExpected()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        // Set value.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));

        // Set different.
        obj.SetValue(key, 1);
        Assert.Equal(1, obj.GetValue(property));

        // Set null.
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyWithPropertyChangeCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(key, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyWithCoerceCallback_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        obj.SetValue(key, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyWithCoerceCallbackCustom_Success()
    {
        var obj = new DependencyObject();

        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            null,
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return "coercedValue";
            }
        );
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        obj.SetValue(key, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        obj.SetValue(key, "other");
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        obj.SetValue(key, null);
        Assert.Equal("coercedValue", obj.GetValue(property));
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyWithPropertyChangeAndCoerceCallback_Success()
    {
        var obj = new DependencyObject();

        int changedCallCount = 0;
        int coerceValueCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            },
            (d, baseValue) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedNewValue, baseValue);
                coerceValueCallCount++;
                return baseValue;
            }
        );
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, coerceValueCallCount);

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(2, coerceValueCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(key, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(3, coerceValueCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
        Assert.Equal(4, coerceValueCallCount);
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyCustomOnPropertyChanged_CallsOnPropertyChanged()
    {
        var obj = new CustomDependencyObject();

        int onPropertyChangedCallCount = 0;
        int changedCallCount = 0;
        DependencyProperty? expectedProperty = null;
        object? expectedOldValue = null;
        object? expectedNewValue = null;
        obj.OnPropertyChangedAction = (e) =>
        {
            Assert.True(changedCallCount > onPropertyChangedCallCount);
            Assert.Same(expectedProperty, e.Property);
            Assert.Same(expectedOldValue, e.OldValue);
            Assert.Same(expectedNewValue, e.NewValue);
            Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
            onPropertyChangedCallCount++;
        };
        var typeMetadata = new PropertyMetadata(
            null,
            (d, e) =>
            {
                Assert.Same(obj, d);
                Assert.Same(expectedProperty, e.Property);
                Assert.Same(expectedOldValue, e.OldValue);
                Assert.Same(expectedNewValue, e.NewValue);
                Assert.Equal(expectedNewValue, obj.GetValue(e.Property));
                changedCallCount++;
            });
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        DependencyProperty property = key.DependencyProperty;
        expectedProperty = property;

        // Set custom.
        expectedNewValue = "value";
        expectedOldValue = null;
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, onPropertyChangedCallCount);

        // Set same.
        obj.SetValue(key, "value");
        Assert.Equal("value", obj.GetValue(property));
        Assert.Equal(1, changedCallCount);
        Assert.Equal(1, onPropertyChangedCallCount);

        // Set different.
        expectedNewValue = "other";
        expectedOldValue = "value";
        obj.SetValue(key, "other");
        Assert.Equal("other", obj.GetValue(property));
        Assert.Equal(2, changedCallCount);
        Assert.Equal(2, onPropertyChangedCallCount);

        // Set null.
        expectedNewValue = null;
        expectedOldValue = "other";
        obj.SetValue(key, null);
        Assert.Null(obj.GetValue(property));
        Assert.Equal(3, changedCallCount);
        Assert.Equal(3, onPropertyChangedCallCount);
    }

    [Fact]
    public void SetValue_NullPropertyKey_ThrowsArgumentNullException()
    {
        var obj = new DependencyObject();
        Assert.Throws<ArgumentNullException>("key", () => obj.SetValue((DependencyPropertyKey)null!, true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyKeyInvalidValueValueType_ThrowsArgumentException(object? value)
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(key, value));
    }

    [Theory]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyKeyInvalidValueNullable_ThrowsArgumentException(object value)
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool?), typeof(DependencyObject), new PropertyMetadata());
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(key, value));
    }

    public static IEnumerable<object[]> SetValue_DependencyPropertyKeyInvalidValueReferenceType_TestData()
    {
        yield return new object[] { new object() };
        yield return new object[] { 1 };
    }

    [Theory]
    [MemberData(nameof(SetValue_DependencyPropertyKeyInvalidValueReferenceType_TestData))]
    public void SetValue_DependencyPropertyKeyInvalidValueReferenceType_ThrowsArgumentException(object value)
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(key, value));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyKeyOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new DependencyObject();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.SetValue(key, "value"));
        });
    }

    [Fact]
    public void OnPropertyChanged_InvokeNoMetadata_Nop()
    {
        var obj = new SubDependencyObject();
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.OnPropertyChanged(new DependencyPropertyChangedEventArgs(property, new object(), new object()));
    }

    [Fact]
    public void OnPropertyChanged_InvokeEmptyMetadata_Nop()
    {
        var obj = new SubDependencyObject();
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        obj.OnPropertyChanged(new DependencyPropertyChangedEventArgs(property, new object(), new object()));
    }

    [Fact]
    public void OnPropertyChanged_InvokeMetadataHasPropertyChangedCallback_Nop()
    {
        var obj = new SubDependencyObject();
        int callCount = 0;
        var metadata = new PropertyMetadata(null, (d, actualE) => callCount++);
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var e = new DependencyPropertyChangedEventArgs(property, new object(), new object());
        obj.OnPropertyChanged(e);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OnPropertyChanged_InvokeOnDifferentThread_Nop()
    {
        var obj = new SubDependencyObject();
        int callCount = 0;
        var metadata = new PropertyMetadata(null, (d, actualE) => callCount++);
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var e = new DependencyPropertyChangedEventArgs(property, new object(), new object());

        Helpers.ExecuteOnDifferentThread(() =>
        {
            obj.OnPropertyChanged(e);
            Assert.Equal(0, callCount);
        });
    }

    [Fact]
    public void OnPropertyChanged_NullEProperty_ThrowsArgumentException()
    {
        var obj = new SubDependencyObject();
        DependencyPropertyChangedEventArgs e = default;
        Assert.Throws<ArgumentException>("e", () => obj.OnPropertyChanged(e));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeSetProperty_ReturnsTrue()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new SubDependencyObject();

        obj.SetValue(property, "value");
        Assert.True(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.True(obj.ShouldSerializeProperty(property));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeSetPropertyReadOnly_ReturnsTrue()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        var obj = new SubDependencyObject();

        obj.SetValue(key, "value");
        Assert.True(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.True(obj.ShouldSerializeProperty(property));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeNoSuchProperty_ReturnsFalse()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new SubDependencyObject();
        Assert.False(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.False(obj.ShouldSerializeProperty(property));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeNoSuchPropertyReadOnly_ReturnsFalse()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        var obj = new SubDependencyObject();
        Assert.False(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.False(obj.ShouldSerializeProperty(property));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeClearedProperty_ReturnsFalse()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new SubDependencyObject();
        obj.SetValue(property, "value");
        obj.ClearValue(property);
        Assert.False(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.False(obj.ShouldSerializeProperty(property));
    }

    [Fact]
    public void ShouldSerializeProperty_InvokeClearedPropertyReadOnly_ReturnsFalse()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(DependencyObjectTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        var obj = new SubDependencyObject();
        obj.SetValue(key, "value");
        obj.ClearValue(key);
        Assert.False(obj.ShouldSerializeProperty(property));

        // Call again to test caching.
        Assert.False(obj.ShouldSerializeProperty(property));
    }

    private class CustomDependencyObject : DependencyObject
    {
        public Action<DependencyPropertyChangedEventArgs>? OnPropertyChangedAction { get; set; }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            OnPropertyChangedAction?.Invoke(e);
        }
    }

    private class SubDependencyObject : DependencyObject
    {
        public new void OnPropertyChanged(DependencyPropertyChangedEventArgs e) => base.OnPropertyChanged(e);

        public new bool ShouldSerializeProperty(DependencyProperty dp) => base.ShouldSerializeProperty(dp);
    }
}
