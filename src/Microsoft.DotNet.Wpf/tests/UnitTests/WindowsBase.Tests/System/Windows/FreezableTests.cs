// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Windows.Threading;

namespace System.Windows.Tests;

public class FreezableTests
{
    [Fact]
    public void Ctor_Default()
    {
        var freezable = new SubFreezable();
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void Changed_AddRemove_Success()
    {
        var freezable = new SubFreezable();

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        freezable.Changed += handler;
        Assert.Equal(0, callCount);

        freezable.Changed -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        Assert.Throws<ArgumentException>("handler", () => freezable.Changed -= handler);
        Assert.Equal(0, callCount);

        // Add null.
        freezable.Changed += null;
        Assert.Equal(0, callCount);

        // Remove null.
        freezable.Changed -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Changed_AddRemoveFrozen_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        Assert.Throws<InvalidOperationException>(() => freezable.Changed += handler);
        Assert.Throws<InvalidOperationException>(() => freezable.Changed += null);
        Assert.Throws<InvalidOperationException>(() => freezable.Changed -= handler);
        Assert.Throws<InvalidOperationException>(() => freezable.Changed -= null);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Changed_AddRemoveOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.Changed += handler);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed += null);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed -= handler);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed -= null);
            Assert.Equal(0, callCount);
        });
    }

    [Fact]
    public void Changed_AddRemoveFrozenOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.Changed += handler);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed += null);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed -= handler);
            Assert.Throws<InvalidOperationException>(() => freezable.Changed -= null);
            Assert.Equal(0, callCount);
        });
    }

    [Fact]
    public void CanFreeze_Get_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        Assert.True(freezable.CanFreeze);
    }

    [Fact]
    public void CanFreeze_GetWithProperties_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        Assert.True(freezable.CanFreeze);
    }
    
    [Fact]
    public void CanFreeze_GetWithUnfreezableProperties_ReturnsFalse()
    {
        var freezable = new SubFreezable();
        Assert.True(freezable.CanFreeze);
        
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable();
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (checking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        Assert.False(freezable.CanFreeze);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanFreeze_GetCustomFreezeCore_ReturnsExpected(bool result)
    {
        var freezable = new CustomFreezable();
        int callCount = 0;
        freezable.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return result;
        };

        // Get once.
        Assert.Equal(result, freezable.CanFreeze);
        Assert.Equal(1, callCount);

        // get again.
        Assert.Equal(result, freezable.CanFreeze);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void CanFreeze_GetFrozen_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Assert.True(freezable.CanFreeze);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanFreeze_GetFrozenCustomFreezeCore_ReturnsTrue(bool result)
    {
        var freezable = new CustomFreezable();
        freezable.Freeze();

        int callCount = 0;
        freezable.FreezeCoreAction = (checking) =>
        {
            callCount++;
            return result;
        };

        // Get once.
        Assert.True(freezable.CanFreeze);
        Assert.Equal(0, callCount);

        // get again.
        Assert.True(freezable.CanFreeze);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void CanFreeze_GetOnDifferentThread_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.CanFreeze);
        });
    }

    [Fact]
    public void CanFreeze_GetFrozenOnDifferentThread_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.CanFreeze);
        });
    }

    [Fact]
    public void DependencyObjectType_Get_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        DependencyObjectType dependencyObjectType = freezable.DependencyObjectType;
        Assert.NotNull(dependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), dependencyObjectType);
    }

    [Fact]
    public void DependencyObjectType_GetFrozen_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        DependencyObjectType dependencyObjectType = freezable.DependencyObjectType;
        Assert.NotNull(dependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), dependencyObjectType);
    }

    [Fact]
    public void DependencyObjectType_GetOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            DependencyObjectType dependencyObjectType = freezable.DependencyObjectType;
            Assert.NotNull(dependencyObjectType);
            Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
            Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), dependencyObjectType);
        });
    }

    [Fact]
    public void DependencyObjectType_GetFrozenOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            DependencyObjectType dependencyObjectType = freezable.DependencyObjectType;
            Assert.NotNull(dependencyObjectType);
            Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
            Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), dependencyObjectType);
        });
    }

    [Fact]
    public void Dispatcher_GetFrozen_ReturnsNull()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Assert.Null(freezable.Dispatcher);
    }

    [Fact]
    public void Dispatcher_Get_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
    }

    [Fact]
    public void Dispatcher_GetOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        Dispatcher expected = Dispatcher.CurrentDispatcher;
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Dispatcher? dispatcher = freezable.Dispatcher;
            Assert.NotNull(dispatcher);
            Assert.Same(expected, dispatcher);
        });
    }

    [Fact]
    public void Dispatcher_GetFrozenOnDifferentThread_ReturnsNull()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Null(freezable.Dispatcher);
        });
    }

    [Fact]
    public void IsFrozen_Get_ReturnsFalse()
    {
        var freezable = new SubFreezable();
        Assert.False(freezable.IsFrozen);
    }

    [Fact]
    public void IsFrozen_GetFrozen_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Assert.True(freezable.IsFrozen);
    }

    [Fact]
    public void IsFrozen_GetOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.IsFrozen);
        });
    }

    [Fact]
    public void IsFrozen_GetFrozenOnDifferentThread_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.IsFrozen);
        });
    }

    [Fact]
    public void IsSealed_Get_ReturnsFalse()
    {
        var freezable = new SubFreezable();
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void IsSealed_GetFrozen_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void IsSealed_GetOnDifferentThread_ReturnsFalse()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.False(freezable.IsSealed);
        });
    }

    [Fact]
    public void IsSealed_GetFrozenOnDifferentThread_ReturnsTrue()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.IsSealed);
        });
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyValueType_GetValueReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        var typeMetadata = new PropertyMetadata("default");
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);

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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new CustomFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
    public void ClearValue_InvokeWithHandler_CallsChanged()
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        }; 
        freezable.Changed += handler;

        // Clear.
        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(0, callCount);

        // Set.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, callCount);

        // Clear.
        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, callCount);

        // Remove handler.
        freezable.Changed -= handler;
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(2, callCount);

        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ClearValue_InvokeWithCustomOnChanged_CallsChanged()
    {
        var freezable = new CustomFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            Assert.Equal(changedCallCount, onChangedCallCount);
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.True(onChangedCallCount > changedCallCount);
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount++;
        };

        // Clear.
        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(0, onChangedCallCount);
        Assert.Equal(0, changedCallCount);

        // Set.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount);

        // Clear.
        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);

        // Clear again.
        freezable.ClearValue(property);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);
    }

    [Fact]
    public void ClearValue_DependencyPropertyNullDp_ThrowsArgumentNullException()
    {
        var obj = new SubFreezable();
        Assert.Throws<ArgumentNullException>("dp", () => obj.ClearValue((DependencyProperty)null!));
    }

    [Fact]
    public void ClearValue_DependencyPropertyReadOnly_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyFrozen_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        obj.Freeze();

        Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
        });
    }

    [Fact]
    public void ClearValue_InvokePropertyFrozenOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(property));
        });
    }


    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyValueType_GetValueReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
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
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject), new PropertyMetadata());
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
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
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
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
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
        var obj = new SubFreezable();
        var typeMetadata = new PropertyMetadata("default");
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new CustomFreezable();

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
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
    public void ClearValue_InvokePropertyKeyWithHandler_CallsChanged()
    {
        var freezable = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());

        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        }; 
        freezable.Changed += handler;

        // Clear.
        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(0, callCount);

        // Set.
        freezable.SetValue(key, true);
        Assert.True((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(1, callCount);

        // Clear.
        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(2, callCount);

        // Remove handler.
        freezable.Changed -= handler;
        freezable.SetValue(key, true);
        Assert.True((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(2, callCount);

        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ClearValue_InvokePropertyKeyWithCustomOnChanged_CallsChanged()
    {
        var freezable = new CustomFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());

        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            Assert.Equal(changedCallCount, onChangedCallCount);
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.True(onChangedCallCount > changedCallCount);
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount++;
        };

        // Clear.
        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(0, onChangedCallCount);
        Assert.Equal(0, changedCallCount);

        // Set.
        freezable.SetValue(key, true);
        Assert.True((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount);

        // Clear.
        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);

        // Clear again.
        freezable.ClearValue(key);
        Assert.False((bool)freezable.GetValue(key.DependencyProperty));
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);
    }

    [Fact]
    public void ClearValue_DependencyPropertyNullKey_ThrowsArgumentNullException()
    {
        var obj = new SubFreezable();
        Assert.Throws<ArgumentNullException>("key", () => obj.ClearValue((DependencyPropertyKey)null!));
    }

    [Fact]
    public void ClearValue_InvokeFrozen_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata(null));
        obj.Freeze();

        Assert.Throws<InvalidOperationException>(() => obj.ClearValue(key));
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(key));
        });
    }

    [Fact]
    public void ClearValue_InvokeDependencyPropertyKeyFrozenOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata(null));
        obj.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.ClearValue(key));
        });
    }

    [Fact]
    public void Clone_InvokeDefault_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(1, createInstanceCallCount);

        // Clone again.
        SubFreezable clone2 = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.NotSame(clone, clone2);
        Assert.True(clone2.CanFreeze);
        Assert.NotNull(clone2.DependencyObjectType);
        Assert.Same(clone2.DependencyObjectType, clone2.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone2.DependencyObjectType);
        Assert.NotNull(clone2.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone2.Dispatcher);
        Assert.False(clone2.IsFrozen);
        Assert.False(clone2.IsSealed);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(2, createInstanceCallCount);
    }

    [Fact]
    public void Clone_InvokeWithProperties_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void Clone_InvokeWithUnfreezableProperties_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable
        {
            CreateInstanceCoreAction = () => new CustomFreezable()
        };
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void Clone_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        freezable.SetValue(propertyKey, "value");

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.Null(clone.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", freezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void Clone_InvokeFrozen_ReturnsExpected()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.Freeze();

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void Clone_InvokeNullCreateInstanceCore_ThrowsNullReferenceException()
    {
        var freezable = new CustomFreezable
        {
            CreateInstanceCoreAction = () => null!
        };
        Assert.Throws<NullReferenceException>(() => freezable.Clone());
    }

    [Fact]
    public void Clone_InvokeDifferentCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new SubFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.Clone());
        Assert.False(freezable.IsFrozen);
        Assert.False(clone.IsFrozen);
    }

    [Fact]
    public void Clone_InvokeSameCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => freezable;
        Assert.Same(freezable, freezable.Clone());
        Assert.False(freezable.IsFrozen);
    }

    [Fact]
    public void Clone_InvokeFrozenCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        clone.Freeze();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.Clone());
        Assert.False(freezable.IsFrozen);
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void Clone_InvokeCantFreeze_ReturnsExpected()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        int callCount = 0;
        clone.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        Assert.Same(clone, freezable.Clone());
        Assert.NotSame(freezable, clone);
        Assert.False(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void Clone_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.Clone());
        });
    }

    [Fact]
    public void Clone_InvokeFrozenOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            SubFreezable clone = Assert.IsType<SubFreezable>(freezable.Clone());    
            Assert.NotSame(freezable, clone);
            Assert.True(clone.CanFreeze);
            Assert.NotNull(clone.DependencyObjectType);
            Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
            Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
            Assert.NotNull(clone.Dispatcher);
            Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
            Assert.False(clone.IsFrozen);
            Assert.False(clone.IsSealed);
        });
    }

    [Fact]
    public void CloneCore_InvokeDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable();
        var sourceFreezable = new SubFreezable();
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        sourceFreezable.SetValue(propertyKey, "value");

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.Null(freezable.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", sourceFreezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void CloneCore_InvokeSourceFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeSourceFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeDestinationFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Clone.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeDestinationFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Invoke.
        Assert.Throws<InvalidOperationException>(() => freezable.CloneCore(sourceFreezable));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Invoke.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeSourceFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeSourceFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Invoke.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeDestinationFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.CloneCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_InvokeDestinationFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Invoke.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCore_NullSourceFreezable_ThrowsArgumentNullException()
    {
        var freezable = new SubFreezable();
        // TODO: this should not throw NullReferenceException.
        //Assert.Throws<ArgumentNullException>("sourceFreezable", () => freezable.CloneCore(null!));
        Assert.Throws<NullReferenceException>(() => freezable.CloneCore(null!));
    }

    [Fact]
    public void CloneCurrentValue_InvokeDefault_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(1, createInstanceCallCount);

        // Clone again.
        SubFreezable clone2 = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.NotSame(clone, clone2);
        Assert.True(clone2.CanFreeze);
        Assert.NotNull(clone2.DependencyObjectType);
        Assert.Same(clone2.DependencyObjectType, clone2.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone2.DependencyObjectType);
        Assert.NotNull(clone2.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone2.Dispatcher);
        Assert.False(clone2.IsFrozen);
        Assert.False(clone2.IsSealed);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(2, createInstanceCallCount);
    }

    [Fact]
    public void CloneCurrentValue_InvokeWithProperties_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void CloneCurrentValue_InvokeWithUnfreezableProperties_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable
        {
            CreateInstanceCoreAction = () => new CustomFreezable()
        };
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void CloneCurrentValue_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        freezable.SetValue(propertyKey, "value");

        // CloneCurrentValue.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.Null(clone.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", freezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void CloneCurrentValue_InvokeFrozen_ReturnsExpected()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.Freeze();

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void CloneCurrentValue_InvokeNullCreateInstanceCore_ThrowsNullReferenceException()
    {
        var freezable = new CustomFreezable
        {
            CreateInstanceCoreAction = () => null!
        };
        Assert.Throws<NullReferenceException>(() => freezable.CloneCurrentValue());
    }

    [Fact]
    public void CloneCurrentValue_InvokeCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new SubFreezable();
        freezable.CreateInstanceCoreAction = () => freezable;
        Assert.Same(freezable, freezable.CloneCurrentValue());
        Assert.False(clone.IsFrozen);
    }

    [Fact]
    public void CloneCurrentValue_InvokeSameCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => freezable;
        Assert.Same(freezable, freezable.CloneCurrentValue());
    }

    [Fact]
    public void CloneCurrentValue_InvokeFrozenCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        clone.Freeze();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.CloneCurrentValue());
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void CloneCurrentValue_InvokeCantFreeze_ReturnsExpected()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        int callCount = 0;
        clone.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        Assert.Same(clone, freezable.CloneCurrentValue());
        Assert.NotSame(freezable, clone);
        Assert.False(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), clone.DependencyObjectType);
        Assert.NotNull(clone.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
        Assert.False(clone.IsFrozen);
        Assert.False(clone.IsSealed);
    }

    [Fact]
    public void CloneCurrentValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCurrentValue());
        });
    }

    [Fact]
    public void CloneCurrentValue_InvokeFrozenOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            SubFreezable clone = Assert.IsType<SubFreezable>(freezable.CloneCurrentValue());    
            Assert.NotSame(freezable, clone);
            Assert.True(clone.CanFreeze);
            Assert.NotNull(clone.DependencyObjectType);
            Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
            Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
            Assert.NotNull(clone.Dispatcher);
            Assert.Same(Dispatcher.CurrentDispatcher, clone.Dispatcher);
            Assert.False(clone.IsFrozen);
            Assert.False(clone.IsSealed);
        });
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable();
        var sourceFreezable = new SubFreezable();
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        sourceFreezable.SetValue(propertyKey, "value");

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.Null(freezable.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", sourceFreezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeSourceFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeSourceFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.NotSame(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeDestinationFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Clone.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeDestinationFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Clone.
        Assert.Throws<InvalidOperationException>(() => freezable.CloneCurrentValueCore(sourceFreezable));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCurrentValueCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCurrentValueCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeSourceFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCurrentValueCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeSourceFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCurrentValueCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeDestinationFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.CloneCurrentValueCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.CloneCurrentValueCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_InvokeDestinationFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.CloneCurrentValueCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void CloneCurrentValueCore_NullSourceFreezable_ThrowsArgumentNullException()
    {
        var freezable = new SubFreezable();
        // TODO: this should not throw NullReferenceException.
        //Assert.Throws<ArgumentNullException>("sourceFreezable", () => freezable.CloneCurrentValueCore(null!));
        Assert.Throws<NullReferenceException>(() => freezable.CloneCurrentValueCore(null!));
    }

    public static IEnumerable<object?[]> CreateInstance_TestData()
    {
#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
        yield return new object?[] { null };
#endif

        yield return new object?[] { new SubFreezable() };

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
        yield return new object?[] { new SubFreezable2() };

        var frozen = new SubFreezable2();
        frozen.Freeze();
        yield return new object?[] { frozen };
#endif
    }

    [Theory]
    [MemberData(nameof(CreateInstance_TestData))]
    public void CreateInstance_Invoke_ReturnsExpected(Freezable result)
    {
        var freezable = new SubFreezable();
        int callCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            callCount++;
            return result;
        };

        Assert.Same(result, freezable.CreateInstance());
        Assert.Equal(1, callCount);

        // Call again.
        Assert.Same(result, freezable.CreateInstance());
        Assert.Equal(2, callCount);
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Theory]
    [MemberData(nameof(CreateInstance_TestData))]
    public void CreateInstance_InvokeFrozen_ReturnsExpected(Freezable result)
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        int callCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            callCount++;
            return result;
        };

        Assert.Same(result, freezable.CreateInstance());
        Assert.Equal(1, callCount);

        // Call again.
        Assert.Same(result, freezable.CreateInstance());
        Assert.Equal(2, callCount);
    }

    [Theory]
    [MemberData(nameof(CreateInstance_TestData))]
    public void CreateInstance_InvokeOnDifferentThread_ReturnsExpected(Freezable result)
    {
        var freezable = new SubFreezable();
        int callCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            callCount++;
            return result;
        };

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Same(result, freezable.CreateInstance());
            Assert.Equal(1, callCount);
        });
    }

    [Theory]
    [MemberData(nameof(CreateInstance_TestData))]
    public void CreateInstance_InvokeFrozenOnDifferentThread_ReturnsExpected(Freezable result)
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        int callCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            callCount++;
            return result;
        };

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Same(result, freezable.CreateInstance());
            Assert.Equal(1, callCount);
        });
    }
#endif

    [Fact]
    public void CoerceValue_InvokeValueType_GetValueReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool?), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));

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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject));

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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + customCoercedValue?.GetType().Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
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
        var obj = new SubFreezable();
        // TODO: this should throw ArgumentNullException
        //Assert.Throws<ArgumentNullException>("dp", () => obj.CoerceValue(null!));
        Assert.Throws<NullReferenceException>(() => obj.CoerceValue(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    public void CoerceValue_InvalidResultDefault_ThrowsArgumentException(object? invalidValue)
    {
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + invalidValue?.GetType().Name, typeof(int), typeof(DependencyObject), typeMetadata);
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
        var obj = new SubFreezable();

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
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + invalidValue?.GetType().Name, typeof(int), typeof(DependencyObject), typeMetadata);
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
    public void CoerceValue_InvokeFrozen_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        obj.Freeze();

        // Coerce default.
        obj.CoerceValue(property);
        Assert.False((bool)obj.GetValue(property));

        // Coerce again.
        obj.CoerceValue(property);
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.CoerceValue(property));
        });
    }

    [Fact]
    public void CoerceValue_InvokeFrozenOnDifferentThread_Nop()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        obj.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            obj.CoerceValue(property);
        });
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void CoerceValue_InvokeFrozenCustomOnDifferentThread_Nop()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        obj.SetValue(property, true);
        obj.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            obj.CoerceValue(property);
        });
        Assert.True((bool)obj.GetValue(property));
    }

    [Fact]
    public void Freeze_Invoke_Success()
    {
        var freezable = new SubFreezable();

        // Freeze once.
        freezable.Freeze();
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Freeze again.
        freezable.Freeze();
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeWithProperties_Success()
    {
        var freezable = new SubFreezable();
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Freeze once.
        freezable.Freeze();
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Freeze again.
        freezable.Freeze();
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeWithUnfreezableProperties_Success()
    {
        var freezable = new SubFreezable();
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable();
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        // Freeze once.
        Assert.Throws<InvalidOperationException>(() => freezable.Freeze());
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.False(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Freeze again.
        Assert.Throws<InvalidOperationException>(() => freezable.Freeze());
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.False(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeCustomFreezeCore_Success()
    {
        var freezable = new CustomFreezable();
        int callCount = 0;
        freezable.FreezeCoreAction = (checking) =>
        {
            Assert.Equal(callCount == 0, checking);
            callCount++;
            return true;
        };

        // Freeze once.
        freezable.Freeze();
        Assert.Equal(2, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Freeze again.
        freezable.Freeze();
        Assert.Equal(2, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeWithHandler_CallsChanged()
    {
        var freezable = new SubFreezable();

        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            Assert.Null(freezable.Dispatcher);
            Assert.True(freezable.IsFrozen);
            Assert.True(freezable.IsSealed);
            callCount++;
        }; 
        freezable.Changed += handler;

        // Freeze.
        freezable.Freeze();
        Assert.Equal(1, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
        
        // Freeze again.
        freezable.Freeze();
        Assert.Equal(1, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeCustomOnChanged_CallsChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            Assert.Equal(changedCallCount, onChangedCallCount);
            Assert.Null(freezable.Dispatcher);
            Assert.True(freezable.IsFrozen);
            Assert.True(freezable.IsSealed);
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.True(onChangedCallCount > changedCallCount);
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            Assert.Null(freezable.Dispatcher);
            Assert.True(freezable.IsFrozen);
            Assert.True(freezable.IsSealed);
            changedCallCount++;
        }; 

        // Freeze.
        freezable.Freeze();
        Assert.Equal(1, changedCallCount);
        Assert.Equal(2, onChangedCallCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
        
        // Freeze again.
        freezable.Freeze();
        Assert.Equal(1, changedCallCount);
        Assert.Equal(2, onChangedCallCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeWithRemovedHandler_CallsChanged()
    {
        var freezable = new SubFreezable();

        int callCount = 0;
        EventHandler handler = (sender, e) => callCount++;
        freezable.Changed += handler;
        freezable.Changed -= handler;

        // Freeze.
        freezable.Freeze();
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
        
        // Freeze again.
        freezable.Freeze();
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
    }

    [Fact]
    public void Freeze_InvokeCantFreeze_ThrowsInvalidOperationException()
    {
        var freezable = new CustomFreezable();
        int callCount = 0;
        freezable.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        // Freeze.
        Assert.Throws<InvalidOperationException>(() => freezable.Freeze());
        Assert.Equal(1, callCount);
        Assert.False(freezable.IsFrozen);
    }

    [Fact]
    public void Freeze_InvokeFrozenCantFreeze_Nop()
    {
        var freezable = new CustomFreezable();
        
        // Freeze once.
        freezable.Freeze();
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        int callCount = 0;
        freezable.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        // Freeze again.
        freezable.Freeze();
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(CustomFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void Freeze_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.Freeze());
        });
        Assert.False(freezable.IsFrozen);
    }

    [Fact]
    public void Freeze_InvokeFrozenOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.Freeze();
        });
        Assert.True(freezable.IsFrozen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeDefault_Success(bool isChecking)
    {
        var freezable = new SubFreezable();

        // Freeze once.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Freeze again.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeWithProperties_Success(bool isChecking)
    {
        var freezable = new SubFreezable();
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);
        
        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Freeze once.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Freeze again.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeWithUnfreezableProperties_Success(bool isChecking)
    {
        var freezable = new SubFreezable();
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);
        
        var anotherFreezable1 = new SubFreezable();
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);
        
        var anotherFreezable2 = new SubFreezable();
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);
        
        var anotherFreezable3 = new SubFreezable();
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable();
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        // Freeze once.
        Assert.Equal(!isChecking, freezable.FreezeCore(isChecking));
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.Same(anotherFreezable4, freezable.GetValue(SubFreezable.Property9));
        Assert.Equal(90, ((CustomFreezable)freezable.GetValue(SubFreezable.Property9)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((CustomFreezable)freezable.GetValue(SubFreezable.Property9)).IsFrozen);
        Assert.False(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Freeze again.
        Assert.Equal(!isChecking, freezable.FreezeCore(isChecking));
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.Equal(!isChecking, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.False(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeWithHandler_DoesNotCallChanged(bool isChecking)
    {
        var freezable = new SubFreezable();

        int callCount = 0;
        EventHandler handler = (sender, e) => callCount++;
        freezable.Changed += handler;

        // Freeze.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        
        // Freeze again.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeWithRemovedHandler_CallsChanged(bool isChecking)
    {
        var freezable = new SubFreezable();

        int callCount = 0;
        EventHandler handler = (sender, e) => callCount++;
        freezable.Changed += handler;
        freezable.Changed -= handler;

        // Freeze.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
        
        // Freeze again.
        Assert.True(freezable.FreezeCore(isChecking));
        Assert.Equal(0, callCount);
        Assert.True(freezable.CanFreeze);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeOnDifferentThread_ReturnsTrue(bool isChecking)
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.FreezeCore(isChecking));
        });
        Assert.False(freezable.IsFrozen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FreezeCore_InvokeFrozenOnDifferentThread_ReturnsTrue(bool isChecking)
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.True(freezable.FreezeCore(isChecking));
        });
        Assert.True(freezable.IsFrozen);
    }

    [Fact]
    public void GetAsFrozen_InvokeDefault_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(1, createInstanceCallCount);

        // Clone again.
        SubFreezable clone2 = Assert.IsType<SubFreezable>(freezable.GetAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.NotSame(clone, clone2);
        Assert.True(clone2.CanFreeze);
        Assert.NotNull(clone2.DependencyObjectType);
        Assert.Same(clone2.DependencyObjectType, clone2.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone2.DependencyObjectType);
        Assert.Null(clone2.Dispatcher);
        Assert.True(clone2.IsFrozen);
        Assert.True(clone2.IsSealed);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(2, createInstanceCallCount);
    }

    [Fact]
    public void GetAsFrozen_InvokeWithProperties_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
    }

    [Fact]
    public void GetAsFrozen_InvokeWithUnfreezableProperties_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable
        {
            CreateInstanceCoreAction = () => new CustomFreezable()
        };
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
    }

    [Fact]
    public void GetAsFrozen_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        freezable.SetValue(propertyKey, "value");

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetAsFrozen());
        Assert.Null(clone.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", freezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void GetAsFrozen_InvokeFrozen_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();
        Assert.Same(freezable, freezable.GetAsFrozen());
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void GetAsFrozen_InvokeNullCreateInstanceCore_ThrowsNullReferenceException()
    {
        var freezable = new CustomFreezable
        {
            CreateInstanceCoreAction = () => null!
        };
        Assert.Throws<NullReferenceException>(() => freezable.GetAsFrozen());
    }

    [Fact]
    public void GetAsFrozen_InvokeDifferentCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new SubFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.GetAsFrozen());
        Assert.False(freezable.IsFrozen);
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void GetAsFrozen_InvokeSameCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => freezable;
        Assert.Same(freezable, freezable.GetAsFrozen());
        Assert.True(freezable.IsFrozen);
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void GetAsFrozen_InvokeFrozenCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        clone.Freeze();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.GetAsFrozen());
        Assert.False(freezable.IsFrozen);
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void GetAsFrozen_InvokeCantFreeze_ThrowsInvalidOperationException()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        int callCount = 0;
        clone.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozen());
    }

    [Fact]
    public void GetAsFrozen_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozen());
        });
    }

    [Fact]
    public void GetAsFrozen_InvokeFrozenOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Same(freezable, freezable.GetAsFrozen());
        });
    }

    [Fact]
    public void GetAsFrozenCore_InvokeDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable();
        var sourceFreezable = new SubFreezable();
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        sourceFreezable.SetValue(propertyKey, "value");

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.Null(freezable.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", sourceFreezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void GetAsFrozenCore_InvokeSourceFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeSourceFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeDestinationFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Clone.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeDestinationFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Clone.
        Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozenCore(sourceFreezable));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozenCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeSourceFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeSourceFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozenCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeDestinationFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.GetAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_InvokeDestinationFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetAsFrozenCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetAsFrozenCore_NullSourceFreezable_ThrowsArgumentNullException()
    {
        var freezable = new SubFreezable();
        // TODO: this should not throw NullReferenceException.
        //Assert.Throws<ArgumentNullException>("sourceFreezable", () => freezable.GetAsFrozenCore(null!));
        Assert.Throws<NullReferenceException>(() => freezable.GetAsFrozenCore(null!));
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeDefault_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetCurrentValueAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(1, createInstanceCallCount);

        // Clone again.
        SubFreezable clone2 = Assert.IsType<SubFreezable>(freezable.GetCurrentValueAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.NotSame(clone, clone2);
        Assert.True(clone2.CanFreeze);
        Assert.NotNull(clone2.DependencyObjectType);
        Assert.Same(clone2.DependencyObjectType, clone2.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone2.DependencyObjectType);
        Assert.Null(clone2.Dispatcher);
        Assert.True(clone2.IsFrozen);
        Assert.True(clone2.IsSealed);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
        Assert.True(freezable.CanFreeze);
        Assert.Equal(2, createInstanceCallCount);
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeWithProperties_Success()
    {
        var freezable = new SubFreezable();
        int createInstanceCallCount = 0;
        freezable.CreateInstanceCoreAction = () =>
        {
            createInstanceCallCount++;
            return new SubFreezable();
        };

        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetCurrentValueAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeWithUnfreezableProperties_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        freezable.SetValue(SubFreezable.Property1, 10);
        freezable.SetValue(SubFreezable.Property2, 20);
        freezable.SetCurrentValue(SubFreezable.Property3, 30);
        freezable.SetValue(SubFreezable.Property4, 40);
        freezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        freezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        freezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        freezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        var anotherFreezable4 = new CustomFreezable
        {
            CreateInstanceCoreAction = () => new CustomFreezable()
        };
        anotherFreezable4.SetValue(SubFreezable.Property1, 90);
        anotherFreezable4.FreezeCoreAction = (isChecking) => false;
        freezable.SetValue(SubFreezable.Property9, anotherFreezable4);

        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetCurrentValueAsFrozen());
        Assert.NotSame(freezable, clone);
        Assert.Equal(10, clone.GetValue(SubFreezable.Property1));
        Assert.Equal(20, clone.GetValue(SubFreezable.Property2));
        Assert.Equal(30, clone.GetValue(SubFreezable.Property3));
        Assert.Equal(40, clone.GetValue(SubFreezable.Property4));
        Assert.Equal(50, clone.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)clone.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)clone.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)clone.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)clone.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)clone.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)clone.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)clone.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(clone.CanFreeze);
        Assert.NotNull(clone.DependencyObjectType);
        Assert.Same(clone.DependencyObjectType, clone.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), clone.DependencyObjectType);
        Assert.Null(clone.Dispatcher);
        Assert.True(clone.IsFrozen);
        Assert.True(clone.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        freezable.SetValue(propertyKey, "value");

        // Clone.
        SubFreezable clone = Assert.IsType<SubFreezable>(freezable.GetCurrentValueAsFrozen());
        Assert.Null(clone.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", freezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeFrozen_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();
        Assert.Same(freezable, freezable.GetCurrentValueAsFrozen());
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void GetCurrentValueAsFrozen_InvokeNullCreateInstanceCore_ThrowsNullReferenceException()
    {
        var freezable = new CustomFreezable
        {
            CreateInstanceCoreAction = () => null!
        };
        Assert.Throws<NullReferenceException>(() => freezable.GetCurrentValueAsFrozen());
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeDifferentCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new SubFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.GetCurrentValueAsFrozen());
        Assert.False(freezable.IsFrozen);
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeSameCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => freezable;
        Assert.Same(freezable, freezable.GetCurrentValueAsFrozen());
        Assert.True(freezable.IsFrozen);
    }

#if !DEBUG // This can trigger a Debug.Assert in Freezable.CreateInstanceCore.
    [Fact]
    public void GetCurrentValueAsFrozen_InvokeFrozenCreateInstanceCore_Success()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        clone.Freeze();
        freezable.CreateInstanceCoreAction = () => clone;
        Assert.Same(clone, freezable.GetCurrentValueAsFrozen());
        Assert.False(freezable.IsFrozen);
        Assert.True(clone.IsFrozen);
    }
#endif

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeCantFreeze_ThrowsInvalidOperationException()
    {
        var freezable = new CustomFreezable();
        var clone = new CustomFreezable();
        freezable.CreateInstanceCoreAction = () => clone;
        int callCount = 0;
        clone.FreezeCoreAction = (checking) =>
        {
            Assert.True(checking);
            callCount++;
            return false;
        };

        Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozen());
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozen());
        });
    }

    [Fact]
    public void GetCurrentValueAsFrozen_InvokeFrozenOnDifferentThread_ReturnsExpected()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Same(freezable, freezable.GetCurrentValueAsFrozen());
        });
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Invoke.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Invoke.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.NotSame(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.NotSame(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.False(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeWithReadOnlyProperty_DoesNotCopy()
    {
        var freezable = new SubFreezable();
        var sourceFreezable = new SubFreezable();
        DependencyPropertyKey propertyKey = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        sourceFreezable.SetValue(propertyKey, "value");

        // Clone.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.Null(freezable.GetValue(propertyKey.DependencyProperty));
        Assert.Equal("value", sourceFreezable.GetValue(propertyKey.DependencyProperty));
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeSourceFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();

        // Invoke.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeSourceFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Invoke.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.Equal(10, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(20, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(30, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(40, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(50, freezable.GetValue(SubFreezable.Property5));
        Assert.Same(anotherFreezable1, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)));
        Assert.Equal(60, ((SubFreezable)freezable.GetValue(SubFreezable.Property6)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property6)).IsFrozen);
        Assert.Same(anotherFreezable2, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)));
        Assert.Equal(70, ((SubFreezable)freezable.GetValue(SubFreezable.Property7)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property7)).IsFrozen);
        Assert.Same(anotherFreezable3, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)));
        Assert.Equal(80, ((SubFreezable)freezable.GetValue(SubFreezable.Property8)).GetValue(SubFreezable.Property1));
        Assert.True(((SubFreezable)freezable.GetValue(SubFreezable.Property8)).IsFrozen);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeDestinationFrozenDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Invoke.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeDestinationFrozenWithProperties_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Invoke.
        Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozenCore(sourceFreezable));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozenCore(sourceFreezable));
        });

        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeSourceFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        sourceFreezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeSourceFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        sourceFreezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozenCore(sourceFreezable));
        });
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.False(freezable.IsFrozen);
        Assert.False(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeDestinationFrozenOnDifferentThreadDefault_Success()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();
        freezable.Freeze();
        
        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        });
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);

        // Clone again.
        freezable.GetCurrentValueAsFrozenCore(sourceFreezable);
        Assert.True(freezable.CanFreeze);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.Null(freezable.Dispatcher);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_InvokeDestinationFrozenOnDifferentThreadWithProperties_ThrowsInvalidOperationException()
    {
        var sourceFreezable = new SubFreezable();
        var freezable = new SubFreezable();

        sourceFreezable.SetValue(SubFreezable.Property1, 10);
        sourceFreezable.SetValue(SubFreezable.Property2, 20);
        sourceFreezable.SetCurrentValue(SubFreezable.Property3, 30);
        sourceFreezable.SetValue(SubFreezable.Property4, 40);
        sourceFreezable.SetCurrentValue(SubFreezable.Property5, 50);

        var anotherFreezable1 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable1.SetValue(SubFreezable.Property1, 60);
        sourceFreezable.SetValue(SubFreezable.Property6, anotherFreezable1);

        var anotherFreezable2 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable2.SetValue(SubFreezable.Property1, 70);
        sourceFreezable.SetCurrentValue(SubFreezable.Property7, anotherFreezable2);

        var anotherFreezable3 = new SubFreezable
        {
            CreateInstanceCoreAction = () => new SubFreezable()
        };
        anotherFreezable3.SetValue(SubFreezable.Property1, 80);
        anotherFreezable3.Freeze();
        sourceFreezable.SetCurrentValue(SubFreezable.Property8, anotherFreezable3);
        
        freezable.Freeze();

        // Clone.
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.GetCurrentValueAsFrozenCore(sourceFreezable));
        });
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property1));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property2));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property3));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property4));
        Assert.Equal(0, freezable.GetValue(SubFreezable.Property5));
        Assert.Null(freezable.GetValue(SubFreezable.Property6));
        Assert.Null(freezable.GetValue(SubFreezable.Property7));
        Assert.Null(freezable.GetValue(SubFreezable.Property8));
        Assert.True(freezable.CanFreeze);
        Assert.Null(freezable.Dispatcher);
        Assert.NotNull(freezable.DependencyObjectType);
        Assert.Same(freezable.DependencyObjectType, freezable.DependencyObjectType);
        Assert.Same(DependencyObjectType.FromSystemType(typeof(SubFreezable)), freezable.DependencyObjectType);
        Assert.True(freezable.IsFrozen);
        Assert.True(freezable.IsSealed);
    }

    [Fact]
    public void GetCurrentValueAsFrozenCore_NullSourceFreezable_ThrowsArgumentNullException()
    {
        var freezable = new SubFreezable();
        // TODO: this should not throw NullReferenceException.
        //Assert.Throws<ArgumentNullException>("sourceFreezable", () => freezable.GetCurrentValueAsFrozenCore(null!));
        Assert.Throws<NullReferenceException>(() => freezable.GetCurrentValueAsFrozenCore(null!));
    }

    [Fact]
    public void GetValue_Invoke_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeSetValue_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.SetValue(property, "value");

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeCustomDefaultValue_ReturnsExpected()
    {
        var obj = new SubFreezable();
        var typeMetadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeReadOnlyProperty_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeFrozen_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.Freeze();

        Assert.Null(obj.GetValue(property));

        // Call again to test caching.
        Assert.Null(obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeFrozenSetValue_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.SetValue(property, "value");
        obj.Freeze();

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeFrozenCustomDefaultValue_ReturnsExpected()
    {
        var obj = new SubFreezable();
        var typeMetadata = new PropertyMetadata("value");
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), typeMetadata);
        obj.Freeze();

        Assert.Equal("value", obj.GetValue(property));

        // Call again to test caching.
        Assert.Equal("value", obj.GetValue(property));
    }

    [Fact]
    public void GetValue_InvokeFrozenReadOnlyProperty_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        obj.Freeze();

        Assert.False((bool)obj.GetValue(property));

        // Call again to test caching.
        Assert.False((bool)obj.GetValue(property));
    }

    [Fact]
    public void GetValue_NullProperty_ThrowsArgumentNullException()
    {
        var obj = new SubFreezable();
        Assert.Throws<ArgumentNullException>("dp", () => obj.GetValue(null!));
    }

    [Fact]
    public void GetValue_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => obj.GetValue(property));
        });
    }

    [Fact]
    public void GetValue_InvokeFrozenOnDifferentThread_ReturnsExpected()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        obj.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.False((bool)obj.GetValue(property));
        });
    }

    [Fact]
    public void ReadPreamble_InvokeSuccess()
    {
        var freezable = new SubFreezable();

        // Call.
        freezable.ReadPreamble();

        // Call again.
        freezable.ReadPreamble();
    }

    [Fact]
    public void ReadPreamble_InvokeWithHandler_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        freezable.OnChangedAction = () => onChangedCallCount++;
        int changedCallCount = 0;
        freezable.Changed += (sender, e) => changedCallCount++;
        freezable.ReadPreamble();
        Assert.Equal(0, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
    }

    [Fact]
    public void ReadPreamble_InvokeFrozen_Nop()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Call.
        freezable.ReadPreamble();
        
        // Call again.
        freezable.ReadPreamble();
    }

    [Fact]
    public void ReadPreamble_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.ReadPreamble());
        });
    }

    [Fact]
    public void ReadPreamble_InvokeFrozenOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.ReadPreamble();
        });
    }

    [Fact]
    public void OnChanged_Invoke_Nop()
    {
        var freezable = new SubFreezable();
        freezable.OnChanged();
    }

    [Fact]
    public void OnChanged_InvokeWithHandler_DoesNotCallChanged()
    {
        var freezable = new SubFreezable();
        int callCount = 0;
        freezable.Changed += (sender, e) => callCount++;
        
        freezable.OnChanged();
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OnChanged_InvokeFrozen_Nop()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        freezable.OnChanged();
    }

    [Fact]
    public void OnChanged_InvokeOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnChanged();
        });
    }

    [Fact]
    public void OnChanged_InvokeFrozenOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnChanged();
        });
    }

    [Fact]
    public void OnFreezablePropertyChanged_Invoke_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.OnFreezablePropertyChanged(value1, value2);
        freezable.OnFreezablePropertyChanged(value1, null!);
        freezable.OnFreezablePropertyChanged(null!, value2);
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeFrozen_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.Freeze();
        freezable.OnFreezablePropertyChanged(value1, value2);
        freezable.OnFreezablePropertyChanged(value1, null!);
        freezable.OnFreezablePropertyChanged(null!, value2);
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(value1, value2);
            freezable.OnFreezablePropertyChanged(value1, null!);
            freezable.OnFreezablePropertyChanged(null!, value2);
        });
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeFrozenOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(value1, value2);
            freezable.OnFreezablePropertyChanged(value1, null!);
            freezable.OnFreezablePropertyChanged(null!, value2);
        });
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value4 = new SubFreezable();
        value4.Freeze();

        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value1));
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value3));
        freezable.OnFreezablePropertyChanged(null!, value4);
        freezable.OnFreezablePropertyChanged(value1, value2);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value1, value3));
        freezable.OnFreezablePropertyChanged(value1, value4);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value1));
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value3));
        freezable.OnFreezablePropertyChanged(value2, value4);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value3, value1));
        freezable.OnFreezablePropertyChanged(value3, value2);
        freezable.OnFreezablePropertyChanged(value3, value4);
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeDifferentThreadFrozen_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        freezable.Freeze();
        var value4 = new SubFreezable();
        value4.Freeze();

        freezable.OnFreezablePropertyChanged(null!, value1);
        freezable.OnFreezablePropertyChanged(null!, value2);
        freezable.OnFreezablePropertyChanged(null!, value3);
        freezable.OnFreezablePropertyChanged(null!, value4);
        freezable.OnFreezablePropertyChanged(value1, value1);
        freezable.OnFreezablePropertyChanged(value1, value2);
        freezable.OnFreezablePropertyChanged(value1, value3);
        freezable.OnFreezablePropertyChanged(value1, value4);
        freezable.OnFreezablePropertyChanged(value2, value1);
        freezable.OnFreezablePropertyChanged(value2, value2);
        freezable.OnFreezablePropertyChanged(value2, value3);
        freezable.OnFreezablePropertyChanged(value2, value4);
        freezable.OnFreezablePropertyChanged(value3, value1);
        freezable.OnFreezablePropertyChanged(value3, value2);
        freezable.OnFreezablePropertyChanged(value3, value3);
        freezable.OnFreezablePropertyChanged(value3, value4);
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeDifferentThreadOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value4 = new SubFreezable();
        value4.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value1));
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value3));
            freezable.OnFreezablePropertyChanged(null!, value4);
            freezable.OnFreezablePropertyChanged(value1, value2);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value1, value3));
            freezable.OnFreezablePropertyChanged(value1, value4);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value1));
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value3));
            freezable.OnFreezablePropertyChanged(value2, value4);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value3, value1));
            freezable.OnFreezablePropertyChanged(value3, value2);
            freezable.OnFreezablePropertyChanged(value3, value4);
        });
    }

    [Fact]
    public void OnFreezablePropertyChanged_InvokeDifferentThreadFrozenOnDifferentThread_Nop()
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        freezable.Freeze();
        var value4 = new SubFreezable();
        value4.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(null!, value1);
            freezable.OnFreezablePropertyChanged(null!, value2);
            freezable.OnFreezablePropertyChanged(null!, value3);
            freezable.OnFreezablePropertyChanged(null!, value4);
            freezable.OnFreezablePropertyChanged(value1, value1);
            freezable.OnFreezablePropertyChanged(value1, value2);
            freezable.OnFreezablePropertyChanged(value1, value3);
            freezable.OnFreezablePropertyChanged(value1, value4);
            freezable.OnFreezablePropertyChanged(value2, value1);
            freezable.OnFreezablePropertyChanged(value2, value2);
            freezable.OnFreezablePropertyChanged(value2, value3);
            freezable.OnFreezablePropertyChanged(value2, value4);
            freezable.OnFreezablePropertyChanged(value3, value1);
            freezable.OnFreezablePropertyChanged(value3, value2);
            freezable.OnFreezablePropertyChanged(value3, value3);
            freezable.OnFreezablePropertyChanged(value3, value4);
        });
    }
    public static IEnumerable<object?[]> OnFreezablePropertyChanged_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { SubFreezable.Property1 };
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyProperty_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.OnFreezablePropertyChanged(value1, value2, property);
        freezable.OnFreezablePropertyChanged(value1, null!, property);
        freezable.OnFreezablePropertyChanged(null!, value2, property);
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyFrozen_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.Freeze();
        freezable.OnFreezablePropertyChanged(value1, value2, property);
        freezable.OnFreezablePropertyChanged(value1, null!, property);
        freezable.OnFreezablePropertyChanged(null!, value2, property);
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyOnDifferentThread_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(value1, value2, property);
            freezable.OnFreezablePropertyChanged(value1, null!, property);
            freezable.OnFreezablePropertyChanged(null!, value2, property);
        });
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyFrozenOnDifferentThread_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = new SubDependencyObject();
        var value2 = new SubDependencyObject();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(value1, value2, property);
            freezable.OnFreezablePropertyChanged(value1, null!, property);
            freezable.OnFreezablePropertyChanged(null!, value2, property);
        });
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyDifferentThread_ThrowsInvalidOperationException(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value4 = new SubFreezable();
        value4.Freeze();

        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value1, property));
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value3, property));
        freezable.OnFreezablePropertyChanged(null!, value4, property);
        freezable.OnFreezablePropertyChanged(value1, value2, property);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value1, value3, property));
        freezable.OnFreezablePropertyChanged(value1, value4, property);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value1, property));
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value3, property));
        freezable.OnFreezablePropertyChanged(value2, value4, property);
        Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value3, value1, property));
        freezable.OnFreezablePropertyChanged(value3, value2, property);
        freezable.OnFreezablePropertyChanged(value3, value4, property);
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyDifferentThreadFrozen_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        freezable.Freeze();
        var value4 = new SubFreezable();
        value4.Freeze();

        freezable.OnFreezablePropertyChanged(null!, value1, property);
        freezable.OnFreezablePropertyChanged(null!, value2, property);
        freezable.OnFreezablePropertyChanged(null!, value3, property);
        freezable.OnFreezablePropertyChanged(null!, value4, property);
        freezable.OnFreezablePropertyChanged(value1, value1, property);
        freezable.OnFreezablePropertyChanged(value1, value2, property);
        freezable.OnFreezablePropertyChanged(value1, value3, property);
        freezable.OnFreezablePropertyChanged(value1, value4, property);
        freezable.OnFreezablePropertyChanged(value2, value1, property);
        freezable.OnFreezablePropertyChanged(value2, value2, property);
        freezable.OnFreezablePropertyChanged(value2, value3, property);
        freezable.OnFreezablePropertyChanged(value2, value4, property);
        freezable.OnFreezablePropertyChanged(value3, value1, property);
        freezable.OnFreezablePropertyChanged(value3, value2, property);
        freezable.OnFreezablePropertyChanged(value3, value3, property);
        freezable.OnFreezablePropertyChanged(value3, value4, property);
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyDifferentThreadOnDifferentThread_ThrowsInvalidOperationException(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value4 = new SubFreezable();
        value4.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value1, property));
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(null!, value3, property));
            freezable.OnFreezablePropertyChanged(null!, value4, property);
            freezable.OnFreezablePropertyChanged(value1, value2, property);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value1, value3, property));
            freezable.OnFreezablePropertyChanged(value1, value4, property);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value1, property));
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value2, value3, property));
            freezable.OnFreezablePropertyChanged(value2, value4, property);
            Assert.Throws<InvalidOperationException>(() => freezable.OnFreezablePropertyChanged(value3, value1, property));
            freezable.OnFreezablePropertyChanged(value3, value2, property);
            freezable.OnFreezablePropertyChanged(value3, value4, property);
        });
    }

    [Theory]
    [MemberData(nameof(OnFreezablePropertyChanged_TestData))]
    public void OnFreezablePropertyChanged_InvokeDependencyPropertyDifferentThreadFrozenOnDifferentThread_Nop(DependencyProperty property)
    {
        var freezable = new SubFreezable();
        var value1 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        var value2 = new SubDependencyObject();
        var value3 = Helpers.ExecuteOnDifferentThread(() => new SubDependencyObject());
        freezable.Freeze();
        var value4 = new SubFreezable();
        value4.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.OnFreezablePropertyChanged(null!, value1, property);
            freezable.OnFreezablePropertyChanged(null!, value2, property);
            freezable.OnFreezablePropertyChanged(null!, value3, property);
            freezable.OnFreezablePropertyChanged(null!, value4, property);
            freezable.OnFreezablePropertyChanged(value1, value1, property);
            freezable.OnFreezablePropertyChanged(value1, value2, property);
            freezable.OnFreezablePropertyChanged(value1, value3, property);
            freezable.OnFreezablePropertyChanged(value1, value4, property);
            freezable.OnFreezablePropertyChanged(value2, value1, property);
            freezable.OnFreezablePropertyChanged(value2, value2, property);
            freezable.OnFreezablePropertyChanged(value2, value3, property);
            freezable.OnFreezablePropertyChanged(value2, value4, property);
            freezable.OnFreezablePropertyChanged(value3, value1, property);
            freezable.OnFreezablePropertyChanged(value3, value2, property);
            freezable.OnFreezablePropertyChanged(value3, value3, property);
            freezable.OnFreezablePropertyChanged(value3, value4, property);
        });
    }

    [Fact]
    public void OnPropertyChanged_InvokeNoMetadata_Nop()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        obj.OnPropertyChanged(new DependencyPropertyChangedEventArgs(property, new object(), new object()));
    }

    [Fact]
    public void OnPropertyChanged_InvokeEmptyMetadata_Nop()
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), new PropertyMetadata());
        obj.OnPropertyChanged(new DependencyPropertyChangedEventArgs(property, new object(), new object()));
    }

    [Fact]
    public void OnPropertyChanged_InvokeMetadataHasPropertyChangedCallback_Nop()
    {
        var obj = new SubFreezable();
        int callCount = 0;
        var metadata = new PropertyMetadata(null, (d, actualE) => callCount++);
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
        var e = new DependencyPropertyChangedEventArgs(property, new object(), new object());
        obj.OnPropertyChanged(e);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void OnPropertyChanged_InvokeOnDifferentThread_Nop()
    {
        var obj = new SubFreezable();
        int callCount = 0;
        var metadata = new PropertyMetadata(null, (d, actualE) => callCount++);
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject), metadata);
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
        var obj = new SubFreezable();
        DependencyPropertyChangedEventArgs e = default;
        Assert.Throws<ArgumentException>("e", () => obj.OnPropertyChanged(e));
    }

    [Fact]
    public void SetValue_InvokeDependencyProperty_GetValueReturnsExpected()
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        // Set true.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));

        // Set same.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));

        // Set false.
        freezable.SetValue(property, false);
        Assert.False((bool)freezable.GetValue(property));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyWithHandler_CallsChanged()
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        int callCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        }; 
        freezable.Changed += handler;

        // Set.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, callCount);

        // Set same.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, callCount);

        // Set different.
        freezable.SetValue(property, false);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, callCount);

        // Remove handler.
        freezable.Changed -= handler;
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void SetValue_InvokeWithCustomOnChanged_CallsChanged()
    {
        var freezable = new CustomFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            Assert.Equal(changedCallCount, onChangedCallCount);
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.True(onChangedCallCount > changedCallCount);
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount++;
        }; 

        // Set.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount);

        // Set same.
        freezable.SetValue(property, true);
        Assert.True((bool)freezable.GetValue(property));
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount);

        // Set different.
        freezable.SetValue(property, false);
        Assert.False((bool)freezable.GetValue(property));
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);
    }


    [Fact]
    public void SetValue_DependencyPropertyNullDp_ThrowsArgumentNullException()
    {
        var obj = new SubFreezable();
        Assert.Throws<ArgumentNullException>("dp", () => obj.SetValue((DependencyProperty)null!, true));
    }

    [Fact]
    public void SetValue_DependencyPropertyReadOnly_ThrowsInvalidOperationException()
    {
        var obj = new SubFreezable();
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Assert.Throws<InvalidOperationException>(() => obj.SetValue(property, "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyInvalidValueValueType_ThrowsArgumentException(object? value)
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool), typeof(DependencyObject));
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(property, value));
    }

    [Theory]
    [InlineData("value")]
    [InlineData(1)]
    public void SetValue_DependencyPropertyInvalidValueNullable_ThrowsArgumentException(object value)
    {
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(bool?), typeof(DependencyObject));
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
        var obj = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + value?.GetType().Name, typeof(string), typeof(DependencyObject));
        // TODO: add paramName
        Assert.Throws<ArgumentException>(() => obj.SetValue(property, value));
    }

    [Fact]
    public void SetValue_InvokeDependencyPropertyFrozen_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));
        freezable.SetValue(property, true);
        freezable.Freeze();

        // Set true.
        Assert.Throws<InvalidOperationException>(() => freezable.SetValue(property, true));
        Assert.True((bool)freezable.GetValue(property));

        // Set same.
        Assert.Throws<InvalidOperationException>(() => freezable.SetValue(property, true));
        Assert.True((bool)freezable.GetValue(property));

        // Set false.
        Assert.Throws<InvalidOperationException>(() => freezable.SetValue(property, false));
        Assert.True((bool)freezable.GetValue(property));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetValue_InvokeDifferentThread_ThrowsInvalidOperationException(bool value)
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + value.ToString(), typeof(bool), typeof(DependencyObject));
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.SetValue(property, value));
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetValue_InvokeFrozenOnDifferentThread_ThrowsInvalidOperationException(bool value)
    {
        var freezable = new SubFreezable();
        DependencyProperty property = DependencyProperty.Register(nameof(FreezableTests) + MethodBase.GetCurrentMethod()!.Name + value.ToString(), typeof(bool), typeof(DependencyObject));
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.SetValue(property, value));
        });
    }

    [Fact]
    public void WritePostscript_Invoke_Success()
    {
        var freezable = new SubFreezable();

        // Call.
        freezable.WritePostscript();
        
        // Call again.
        freezable.WritePostscript();
    }

    [Fact]
    public void WritePostscript_InvokeWithHandler_CallsChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            Assert.Equal(changedCallCount, onChangedCallCount);
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.True(onChangedCallCount > changedCallCount);
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount++;
        };

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount);
    }

    [Fact]
    public void WritePostscript_InvokeWithRemovedHandler_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            onChangedCallCount++;
        };
        EventHandler handler = (sender, e) => changedCallCount++;
        freezable.Changed += handler;
        freezable.Changed -= handler;

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
    }

    [Fact]
    public void WritePostscript_InvokeWithMultipleHandlers_CallsChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount1 = 0;
        int changedCallCount2 = 0;
        int changedCallCount3 = 0;
        freezable.OnChangedAction = () =>
        {
            onChangedCallCount++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount1++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount2++;
        };
        freezable.Changed += (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount3++;
        };

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount1);
        Assert.Equal(1, changedCallCount2);
        Assert.Equal(1, changedCallCount3);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount1);
        Assert.Equal(2, changedCallCount2);
        Assert.Equal(2, changedCallCount3);
    }

    [Fact]
    public void WritePostscript_InvokeWithMultipleHandlersRemovedOne_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount1 = 0;
        int changedCallCount2 = 0;
        int changedCallCount3 = 0;
        freezable.OnChangedAction = () =>
        {
            onChangedCallCount++;
        };
        EventHandler handler1 = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount1++;
        };
        freezable.Changed += handler1;
        EventHandler handler2 = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount2++;
        };
        freezable.Changed += handler2;
        EventHandler handler3 = (sender, e) => changedCallCount3++;
        freezable.Changed += handler3;
        freezable.Changed -= handler3;

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(1, changedCallCount1);
        Assert.Equal(1, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(2, changedCallCount1);
        Assert.Equal(2, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
    }

    [Fact]
    public void WritePostscript_InvokeWithMultipleHandlersRemovedAllButOne_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount1 = 0;
        int changedCallCount2 = 0;
        int changedCallCount3 = 0;
        freezable.OnChangedAction = () =>
        {
            onChangedCallCount++;
        };
        EventHandler handler1 = (sender, e) => changedCallCount1++;
        freezable.Changed += handler1;
        EventHandler handler2 = (sender, e) =>
        {
            Assert.Same(freezable, sender);
            Assert.Same(EventArgs.Empty, e);
            changedCallCount2++;
        };
        freezable.Changed += handler2;
        EventHandler handler3 = (sender, e) => changedCallCount3++;
        freezable.Changed += handler3;
        freezable.Changed -= handler3;
        freezable.Changed -= handler1;

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(0, changedCallCount1);
        Assert.Equal(1, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(0, changedCallCount1);
        Assert.Equal(2, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
    }

    [Fact]
    public void WritePostscript_InvokeWithMultipleHandlersRemovedAll_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        int changedCallCount1 = 0;
        int changedCallCount2 = 0;
        int changedCallCount3 = 0;
        freezable.OnChangedAction = () =>
        {
            onChangedCallCount++;
        };
        EventHandler handler1 = (sender, e) => changedCallCount1++;
        freezable.Changed += handler1;
        EventHandler handler2 = (sender, e) => changedCallCount2++;
        freezable.Changed += handler2;
        EventHandler handler3 = (sender, e) => changedCallCount3++;
        freezable.Changed += handler3;
        freezable.Changed -= handler3;
        freezable.Changed -= handler1;

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(0, changedCallCount1);
        Assert.Equal(1, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(0, changedCallCount1);
        Assert.Equal(2, changedCallCount2);
        Assert.Equal(0, changedCallCount3);
    }

    [Fact]
    public void WritePostscript_InvokeFrozen_Success()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Call.
        freezable.WritePostscript();

        // Call again.
        freezable.WritePostscript();
    }

    [Fact]
    public void WritePostscript_InvokeFrozenWithHandler_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        bool shouldTrack = false;
        int onChangedCallCount = 0;
        int changedCallCount = 0;
        freezable.OnChangedAction = () =>
        {
            if (shouldTrack)
            {
                onChangedCallCount++;
            }
        };
        freezable.Changed += (sender, e) =>
        {
            if (shouldTrack)
            {
                Assert.Same(freezable, sender);
                Assert.Same(EventArgs.Empty, e);
                changedCallCount++;
            }
        };
        freezable.Freeze();
        shouldTrack = true;

        // Call.
        freezable.WritePostscript();
        Assert.Equal(1, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
        
        // Call again.
        freezable.WritePostscript();
        Assert.Equal(2, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
    }

    [Fact]
    public void WritePostscript_InvokeOnDifferentThread_Success()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.WritePostscript();
        });
    }

    [Fact]
    public void WritePostscript_InvokeFrozenOnDifferentThread_Success()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            freezable.WritePostscript();
        });
    }

    [Fact]
    public void WritePreamble_Invoke_Success()
    {
        var freezable = new SubFreezable();

        // Call.
        freezable.WritePreamble();

        // Call again.
        freezable.WritePreamble();
    }

    [Fact]
    public void WritePreamble_InvokeWithHandler_DoesNotCallChanged()
    {
        var freezable = new CustomFreezable();
        int onChangedCallCount = 0;
        freezable.OnChangedAction = () => onChangedCallCount++;
        int changedCallCount = 0;
        freezable.Changed += (sender, e) => changedCallCount++;
        
        // Call.
        freezable.WritePreamble();
        Assert.Equal(0, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
        
        // Call again.
        freezable.WritePreamble();
        Assert.Equal(0, onChangedCallCount);
        Assert.Equal(0, changedCallCount);
    }

    [Fact]
    public void WritePreamble_InvokeFrozen_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        // Call.
        Assert.Throws<InvalidOperationException>(() => freezable.WritePreamble());
        
        // Call again.
        Assert.Throws<InvalidOperationException>(() => freezable.WritePreamble());
    }

    [Fact]
    public void WritePreamble_InvokeOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.WritePreamble());
        });
    }

    [Fact]
    public void WritePreamble_InvokeFrozenOnDifferentThread_ThrowsInvalidOperationException()
    {
        var freezable = new SubFreezable();
        freezable.Freeze();

        Helpers.ExecuteOnDifferentThread(() =>
        {
            Assert.Throws<InvalidOperationException>(() => freezable.WritePreamble());
        });
    }

    private class SubDependencyObject : DependencyObject
    {
    }

    private class CustomFreezable : Freezable
    {
        public Func<Freezable>? CreateInstanceCoreAction { get; set; }

        protected override Freezable CreateInstanceCore()
        {
            if (CreateInstanceCoreAction is null)
            {
                throw new NotImplementedException();
            }

            return CreateInstanceCoreAction();
        }

        public Func<bool, bool>? FreezeCoreAction { get; set; }

        protected override bool FreezeCore(bool isChecking)
        {
            if (FreezeCoreAction != null)
            {
                return FreezeCoreAction(isChecking);
            }

            return base.FreezeCore(isChecking);
        }

        public Action? OnChangedAction { get; set; }

        protected override void OnChanged()
        {
            if (OnChangedAction != null)
            {
                OnChangedAction();
            }

            base.OnChanged();
        }

        public Action<DependencyPropertyChangedEventArgs>? OnPropertyChangedAction { get; set; }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            OnPropertyChangedAction?.Invoke(e);
        }

        public new void ReadPreamble() => base.ReadPreamble();

        public new void WritePreamble() => base.WritePreamble();

        public new void WritePostscript() => base.WritePostscript();
    }

    private class SubFreezable : Freezable
    {
        public static DependencyProperty Property1 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property1)}", typeof(int), typeof(SubFreezable));
        public static DependencyProperty Property2 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property2)}", typeof(int), typeof(SubFreezable));
        public static DependencyProperty Property3 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property3)}", typeof(int), typeof(SubFreezable));
        public static DependencyProperty Property4 { get; } = DependencyProperty.RegisterAttached($"{nameof(SubFreezable)}_{nameof(Property4)}", typeof(int), typeof(SubFreezable));
        public static DependencyProperty Property5 { get; } = DependencyProperty.RegisterAttached($"{nameof(SubFreezable)}_{nameof(Property5)}", typeof(int), typeof(SubFreezable));
        public static DependencyProperty Property6 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property6)}", typeof(DependencyObject), typeof(SubFreezable));
        public static DependencyProperty Property7 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property7)}", typeof(DependencyObject), typeof(SubFreezable));
        public static DependencyProperty Property8 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property8)}", typeof(DependencyObject), typeof(SubFreezable));
        public static DependencyProperty Property9 { get; } = DependencyProperty.Register($"{nameof(SubFreezable)}_{nameof(Property9)}", typeof(DependencyObject), typeof(SubFreezable));
        public static DependencyPropertyKey ReadOnlyProperty { get; } = DependencyProperty.RegisterReadOnly($"{nameof(SubFreezable)}_{nameof(ReadOnlyProperty)}", typeof(int), typeof(SubFreezable), new PropertyMetadata(1));

        public new Freezable CreateInstance() => base.CreateInstance();

        public Func<Freezable>? CreateInstanceCoreAction { get; set; }

        protected override Freezable CreateInstanceCore()
        {
            if (CreateInstanceCoreAction is null)
            {
                throw new NotImplementedException();
            }

            return CreateInstanceCoreAction();
        }

        public new void CloneCore(Freezable sourceFreezable) => base.CloneCore(sourceFreezable);

        public new void CloneCurrentValueCore(Freezable sourceFreezable) => base.CloneCurrentValueCore(sourceFreezable);

        public new bool FreezeCore(bool isChecking) => base.FreezeCore(isChecking);

        public new void GetAsFrozenCore(Freezable sourceFreezable) => base.GetAsFrozenCore(sourceFreezable);
        
        public new void GetCurrentValueAsFrozenCore(Freezable sourceFreezable) => base.GetCurrentValueAsFrozenCore(sourceFreezable);

        public new void ReadPreamble() => base.ReadPreamble();

        public new void OnChanged() => base.OnChanged();
        
        public new void OnFreezablePropertyChanged(DependencyObject oldValue, DependencyObject newValue) => base.OnFreezablePropertyChanged(oldValue, newValue, Property1);

        public new void OnFreezablePropertyChanged(DependencyObject oldValue, DependencyObject newValue, DependencyProperty property) => base.OnFreezablePropertyChanged(oldValue, newValue, property);

        public new void OnPropertyChanged(DependencyPropertyChangedEventArgs e) => base.OnPropertyChanged(e);

        public new void WritePreamble() => base.WritePreamble();

        public new void WritePostscript() => base.WritePostscript();
    }

    private class SubFreezable2 : Freezable
    {
        public SubFreezable2() : base()
        {
        }

        protected override Freezable CreateInstanceCore() => throw new NotImplementedException();
    }
}
