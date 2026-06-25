// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace System.Windows.Tests;

public class LocalValueEnumeratorTests
{
    [Fact]
    public void Ctor_Default()
    {
        var enumerator = new LocalValueEnumerator();
        Assert.Equal(0, enumerator.Count);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetDefault_ThrowsInvalidOperationException()
    {
        var enumerator = new LocalValueEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetDefaultFinished_ThrowsInvalidOperationException()
    {
        var enumerator = new LocalValueEnumerator();
        enumerator.MoveNext();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetDefaultReset_ThrowsInvalidOperationException()
    {
        var enumerator = new LocalValueEnumerator();
        enumerator.Reset();

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetCustom_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetCustomFinished_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Current_GetCustomReset_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetDefault_ThrowsInvalidOperationException()
    {
        IEnumerator enumerator = new LocalValueEnumerator();
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetDefaultFinished_ThrowsInvalidOperationException()
    {
        IEnumerator enumerator = new LocalValueEnumerator();
        enumerator.MoveNext();

        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetDefaultReset_ThrowsInvalidOperationException()
    {
        IEnumerator enumerator = new LocalValueEnumerator();
        enumerator.Reset();

        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetCustom_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetCustomFinished_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.MoveNext();
        enumerator.MoveNext();
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    [Fact]
    public void IEnumeratorCurrent_GetCustomReset_ThrowsInvalidOperationException()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new LocalValueEnumerator(), new LocalValueEnumerator(), true, true };
        
        var enumerator1 = new LocalValueEnumerator();
        enumerator1.MoveNext();
        yield return new object?[] { new LocalValueEnumerator(), enumerator1, false, false };
        
        var enumerator2 = new LocalValueEnumerator();
        enumerator2.MoveNext();
        var enumerator3 = new LocalValueEnumerator();
        enumerator3.MoveNext();
        enumerator3.MoveNext();
        yield return new object?[] { enumerator2, enumerator2, true, true };
        yield return new object?[] { enumerator2, enumerator1, true, true };
        yield return new object?[] { enumerator2, enumerator3, false, false };
        
        yield return new object?[] { new LocalValueEnumerator(), new object(), false, false };
        yield return new object?[] { new LocalValueEnumerator(), null, false, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(LocalValueEnumerator enumerator, object? obj, bool expected, bool expectedHashCode)
    {
        Assert.Equal(expected, enumerator.Equals(obj));
        if (obj is LocalValueEnumerator other)
        {
            Assert.Equal(expected, other.Equals(enumerator));
            Assert.Equal(expected, enumerator == other);
            Assert.Equal(expected, other == enumerator);
            Assert.Equal(!expected, enumerator != other);
            Assert.Equal(!expected, other != enumerator);
            Assert.Equal(expectedHashCode, enumerator.GetHashCode().Equals(other.GetHashCode()));
        }
    }

    [Fact]
    public void Equals_InvokeCustom_ReturnsExpected()
    {
        DependencyProperty property1 = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name + "1", typeof(string), typeof(DependencyObject));
        DependencyProperty property2 = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name + "2", typeof(string), typeof(DependencyObject));
        var obj1 = new DependencyObject();
        var obj2 = new DependencyObject();
        var obj3 = new DependencyObject();
        var obj4 = new DependencyObject();
        var obj5 = new DependencyObject();
        obj1.SetValue(property1, "a");
        obj2.SetValue(property1, "a");
        obj3.SetValue(property1, "b");
        obj4.SetValue(property1, "a");
        obj4.SetValue(property2, "b");
        obj5.SetValue(property2, "a");

        LocalValueEnumerator enumerator1 = obj1.GetLocalValueEnumerator();
        enumerator1.MoveNext();
        LocalValueEnumerator enumerator1Copy1 = obj1.GetLocalValueEnumerator();
        enumerator1Copy1.MoveNext();
        LocalValueEnumerator enumerator1Copy2 = enumerator1;
        LocalValueEnumerator enumerator1Copy3 = enumerator1;
        enumerator1Copy3.MoveNext();
        LocalValueEnumerator enumerator2 = obj2.GetLocalValueEnumerator();
        enumerator2.MoveNext();
        LocalValueEnumerator enumerator3 = obj3.GetLocalValueEnumerator();
        enumerator3.MoveNext();
        LocalValueEnumerator enumerator4 = obj4.GetLocalValueEnumerator();
        enumerator4.MoveNext();
        LocalValueEnumerator enumerator5 = obj5.GetLocalValueEnumerator();
        enumerator5.MoveNext();

        Equals_Invoke_ReturnsExpected(enumerator1, enumerator1, true, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator1Copy1, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator1Copy2, true, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator1Copy3, false, false);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator2, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator3, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator4, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, enumerator5, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, new LocalValueEnumerator(), false, true);
        Equals_Invoke_ReturnsExpected(new LocalValueEnumerator(), enumerator1, false, true);
        Equals_Invoke_ReturnsExpected(enumerator1, new object(), false, false);
        Equals_Invoke_ReturnsExpected(enumerator1, null, false, false);
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsExpected()
    {
        var enumerator = new LocalValueEnumerator();
        Assert.NotEqual(0, enumerator.GetHashCode());
        Assert.Equal(enumerator.GetHashCode(), enumerator.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeCustom_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        Assert.NotEqual(0, enumerator.GetHashCode());
        Assert.Equal(enumerator.GetHashCode(), enumerator.GetHashCode());
    }

    [Fact]
    public void MoveNext_InvokeDefault_ReturnsFalse()
    {
        var enumerator = new LocalValueEnumerator();
        
        // Move end.
        Assert.False(enumerator.MoveNext());

        // Move again.
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MoveNext_InvokeCustom_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        Assert.True(enumerator.MoveNext());

        // Move end.
        Assert.False(enumerator.MoveNext());

        // Move again.
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reset_InvokeDefault_Success()
    {
        var enumerator = new LocalValueEnumerator();
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        // Reset again.
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }

    [Fact]
    public void Reset_InvokeCustom_Success()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEnumeratorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        // Reset again.
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
    }
}
