// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Windows.Tests;

public class DependencyPropertyChangedEventArgsTests
{
    public static IEnumerable<object?[]> Ctor_DependencyProperty_Object_Object_TestData()
    {
        yield return new object?[] { null, null };
        yield return new object?[] { "", "" };
        yield return new object?[] { "oldValue", "newValue" };
    }

    [Theory]
    [MemberData(nameof(Ctor_DependencyProperty_Object_Object_TestData))]
    public void Ctor_DependencyProperty_Object_Object(object oldValue, object newValue)
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name + (oldValue?.ToString() ?? "null") + (newValue?.ToString() ?? "null"), typeof(string), typeof(DependencyObject));
        var args = new DependencyPropertyChangedEventArgs(property, oldValue, newValue);
        Assert.Equal(newValue, args.NewValue);
        Assert.Equal(oldValue, args.OldValue);
        Assert.Same(property, args.Property);
    }

    [Fact]
    public void Ctor_NullProperty_ThrowsNullReferenceException()
    {
        // TODO: this should throw ANE.
        Assert.Throws<NullReferenceException>(() => new DependencyPropertyChangedEventArgs(null, "oldValue", "newValue"));
    }

    private static readonly DependencyProperty s_property = DependencyProperty.Register(nameof(DependencyPropertyChangedEventArgsTests), typeof(string), typeof(DependencyObject));

    public static IEnumerable<object?[]> Equals_Object_TestData()
    {
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), true };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(ContentElement.IsMouseOverProperty, "oldValue", "newValue"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(s_property, "oldValue2", "newValue"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(s_property, null, "newValue"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue2"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new DependencyPropertyChangedEventArgs(s_property, "oldValue", null), false };

        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, "newValue"), new DependencyPropertyChangedEventArgs(s_property, null, "newValue"), true };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, "newValue"), new DependencyPropertyChangedEventArgs(s_property, "other", "newValue"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", null), new DependencyPropertyChangedEventArgs(s_property, "oldValue", null), true };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", null), new DependencyPropertyChangedEventArgs(s_property, "oldValue", "other"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, null), new DependencyPropertyChangedEventArgs(s_property, null, null), true };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, null), new DependencyPropertyChangedEventArgs(s_property, "other", null), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, null), new DependencyPropertyChangedEventArgs(s_property, null, "other"), false };
        yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, null, null), new DependencyPropertyChangedEventArgs(s_property, "other", "other"), false };

        // TODO: these should not throw.
        //yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), new object(), false };
        //yield return new object?[] { new DependencyPropertyChangedEventArgs(s_property, "oldValue", "newValue"), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_Object_TestData))]
    public void Equals_Object_ReturnsExpected(DependencyPropertyChangedEventArgs args, object other, bool expected)
    {
        Assert.Equal(expected, args.Equals(other));
        if (other is DependencyPropertyChangedEventArgs otherArgs)
        {
            Assert.Equal(expected, args.Equals(otherArgs));
            Assert.Equal(expected, args == otherArgs);
            Assert.Equal(!expected, args != otherArgs);
        }
    }

    [Fact]
    public void Equals_NullObjectOther_ThrowsNullReferenceException()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var args = new DependencyPropertyChangedEventArgs(property, "oldValue", "newValue");
        Assert.Throws<NullReferenceException>(() => args.Equals(null));
    }

    [Fact]
    public void Equals_OtherNotDependencyPropertyChangedEventArgs_ThrowsInvalidCastException()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var args = new DependencyPropertyChangedEventArgs(property, "oldValue", "newValue");
        Assert.Throws<InvalidCastException>(() => args.Equals(new object()));
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsEqual()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        var args = new DependencyPropertyChangedEventArgs(property, "oldValue", "newValue");
        Assert.NotEqual(0, args.GetHashCode());
        Assert.Equal(args.GetHashCode(), args.GetHashCode());
    }
}
