// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Windows.Tests;

public class LocalValueEntryTests
{
    [Fact]
    public void Ctor_Default()
    {
        var entry = new LocalValueEntry();
        Assert.Null(entry.Property);
        Assert.Null(entry.Value);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new LocalValueEntry(), new LocalValueEntry(), true };
        //yield return new object?[] { new LocalValueEntry(), new object(), false };
        //yield return new object?[] { new LocalValueEntry(), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(LocalValueEntry entry, object? obj, bool expected)
    {
        Assert.Equal(expected, entry.Equals(obj));
        if (obj is LocalValueEntry other)
        {
            Assert.Equal(expected, other.Equals(entry));
            Assert.Equal(expected, entry == other);
            Assert.Equal(expected, other == entry);
            Assert.Equal(!expected, entry != other);
            Assert.Equal(!expected, other != entry);
        }
    }

    [Fact]
    public void Equals_InvokeCustom_ReturnsExpected()
    {
        DependencyProperty property1 = DependencyProperty.Register(nameof(LocalValueEntryTests) + MethodBase.GetCurrentMethod()!.Name + "1", typeof(string), typeof(DependencyObject));
        DependencyProperty property2 = DependencyProperty.Register(nameof(LocalValueEntryTests) + MethodBase.GetCurrentMethod()!.Name + "2", typeof(string), typeof(DependencyObject));
        var obj1 = new DependencyObject();
        var obj2 = new DependencyObject();
        var obj3 = new DependencyObject();
        var obj4 = new DependencyObject();
        obj1.SetValue(property1, "a");
        obj2.SetValue(property1, "a");
        obj3.SetValue(property1, "b");
        obj4.SetValue(property2, "a");
        
        LocalValueEnumerator enumerator1 = obj1.GetLocalValueEnumerator();
        enumerator1.MoveNext();
        LocalValueEnumerator enumerator1Copy = obj1.GetLocalValueEnumerator();
        enumerator1Copy.MoveNext();
        LocalValueEnumerator enumerator2 = obj2.GetLocalValueEnumerator();
        enumerator2.MoveNext();
        LocalValueEnumerator enumerator3 = obj3.GetLocalValueEnumerator();
        enumerator3.MoveNext();
        LocalValueEnumerator enumerator4 = obj4.GetLocalValueEnumerator();
        enumerator4.MoveNext();

        Equals_Invoke_ReturnsExpected(enumerator1.Current, enumerator1.Current, true);
        Equals_Invoke_ReturnsExpected(enumerator1.Current, enumerator1Copy.Current, true);
        Equals_Invoke_ReturnsExpected(enumerator1.Current, enumerator2.Current, true);
        Equals_Invoke_ReturnsExpected(enumerator1.Current, enumerator3.Current, false);
        Equals_Invoke_ReturnsExpected(enumerator1.Current, enumerator4.Current, false);
        Equals_Invoke_ReturnsExpected(enumerator1.Current, new LocalValueEntry(), false);
        Equals_Invoke_ReturnsExpected(new LocalValueEntry(), enumerator1.Current, false);
        // TODO: should return false.
        //Equals_Invoke_ReturnsExpected(enumerator1.Current, new object(), false);
        //Equals_Invoke_ReturnsExpected(enumerator1.Current, null!, false);
    }

    [Fact]
    public void Equals_ObjNull_ThrowsNullReferenceException()
    {
        var entry = new LocalValueEntry();
        // TODO: should return false.
        Assert.Throws<NullReferenceException>(() => entry.Equals(null));
    }

    [Fact]
    public void Equals_ObjNotLocalValueEntry_ThrowsInvalidCastException()
    {
        var entry = new LocalValueEntry();
        Assert.Throws<InvalidCastException>(() => entry.Equals(new object()));
    }

    [Fact]
    public void GetHashCode_InvokeDefault_ReturnsExpected()
    {
        var entry = new LocalValueEntry();
        Assert.NotEqual(0, entry.GetHashCode());
        Assert.Equal(entry.GetHashCode(), entry.GetHashCode());
    }

    [Fact]
    public void GetHashCode_InvokeCustom_ReturnsExpected()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(LocalValueEntryTests) + MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var obj = new DependencyObject();
        obj.SetValue(property, "a");

        LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
        enumerator.MoveNext();

        LocalValueEntry entry = enumerator.Current;
        Assert.NotEqual(0, entry.GetHashCode());
        Assert.Equal(entry.GetHashCode(), entry.GetHashCode());
    }
}
