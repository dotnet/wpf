// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Windows.Tests;

public class DependencyPropertyKeyTests
{
    [Fact]
    public void OverrideMetadata_InvokeSuper_Success()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), new PropertyMetadata());
        _ = key.DependencyProperty;
        var typeMetadata = new PropertyMetadata();

        key.OverrideMetadata(typeof(DependencyObject), typeMetadata);
    }

    [Fact]
    public void OverrideMetadata_InvokeSub_Success()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(SubDependencyObject), new PropertyMetadata());
        _ = key.DependencyProperty;
        var typeMetadata = new PropertyMetadata();

        key.OverrideMetadata(typeof(SubSubDependencyObject), typeMetadata);
    }

    [Fact]
    public void OverrideMetadata_NullForType_ThrowsArgumentNullException()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        Assert.Throws<ArgumentNullException>("forType", () => key.OverrideMetadata(null!, new PropertyMetadata()));
    }
    
    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(object))]
    public void OverrideMetadata_ForTypeNotDependencyObject_ThrowsArgumentException(Type forType)
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name + forType.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        Assert.Throws<ArgumentException>(() => key.OverrideMetadata(forType, new PropertyMetadata()));
    }

    [Fact]
    public void OverrideMetadata_NullTypeMetadata_Th1rowsArgumentNullException()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        Assert.Throws<ArgumentNullException>("typeMetadata", () => key.OverrideMetadata(typeof(int), null));
    }
    
    [Fact]
    public void OverrideMetadata_SealedTypeMetadata_Th1rowsArgumentNullException()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        DependencyProperty property = key.DependencyProperty;
        Assert.Throws<ArgumentException>(() => key.OverrideMetadata(typeof(int), property.DefaultMetadata));
    }

    [Fact]
    public void OverrideMetadata_AlreadyRegistered_ThrowsArgumentException()
    {
        DependencyPropertyKey key = DependencyProperty.RegisterReadOnly(MethodBase.GetCurrentMethod()!.Name, typeof(object), typeof(DependencyObject), new PropertyMetadata());
        Assert.Throws<ArgumentException>(() => key.OverrideMetadata(typeof(DependencyObject), new PropertyMetadata()));
    }

    private class SubDependencyObject : DependencyObject
    {
    }

    private class SubSubDependencyObject : SubDependencyObject
    {
    }
}
