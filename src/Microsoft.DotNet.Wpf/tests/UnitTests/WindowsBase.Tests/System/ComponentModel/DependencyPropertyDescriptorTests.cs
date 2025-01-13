// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Windows;

namespace System.ComponentModel.Tests;

public class DependencyPropertyDescriptorTests
{
    // TODO:
    // FromProperty - success

    [Fact]
    public void FromProperty_InvokeNotDependencyObjectPropertyDescriptor_Success()
    {
        var component = new NotDependencyObject();
        PropertyDescriptor property = TypeDescriptor.GetProperties(component)[nameof(NotDependencyObject.Property)]!;
        
        // Get descriptor.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property));
        
        // Get descriptor again.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property));
    }

    [Fact]
    public void FromProperty_NullProperty_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("property", () => DependencyPropertyDescriptor.FromProperty(null));
    }

    [Fact]
    public void FromProperty_InvokeTypeDependencyProperty_ReturnsNull()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyPropertyDescriptorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        // Get descriptor.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property, typeof(DependencyProperty)));
        
        // Get descriptor again.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property, typeof(DependencyProperty)));
    }

    [Fact]
    public void FromProperty_InvokeTypeNotDependencyProperty_ReturnsNull()
    {
        DependencyProperty property = DependencyProperty.Register(nameof(DependencyPropertyDescriptorTests) + MethodBase.GetCurrentMethod()!.Name, typeof(bool), typeof(DependencyObject));

        // Get descriptor.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property, typeof(int)));
        
        // Get descriptor again.
        Assert.Null(DependencyPropertyDescriptor.FromProperty(property, typeof(int)));
    }
    
    [Fact]
    public void FromProperty_NullDependencyProperty_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("dependencyProperty", () => DependencyPropertyDescriptor.FromProperty(null, typeof(object)));
    }
    
    [Fact]
    public void FromProperty_NullTargetType_ThrowsArgumentNullException()
    {
        DependencyProperty property = DependencyProperty.Register(MethodBase.GetCurrentMethod()!.Name, typeof(string), typeof(DependencyObject));
        Assert.Throws<ArgumentNullException>("targetType", () => DependencyPropertyDescriptor.FromProperty(property, null));
    }

    public class NotDependencyObject
    {
        public int Property { get; set; }
    }

    private class SubDependencyObject : DependencyObject
    {
    }
}