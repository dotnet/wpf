// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;

namespace System.Diagnostics.Tests;

public class PresentationTraceSourcesTests
{
    [Fact]
    public void TraceLevelProperty_Get_ReturnsExpected()
    {
        DependencyProperty property = PresentationTraceSources.TraceLevelProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Equal(PresentationTraceLevel.None, property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal("TraceLevel", property.Name);
        Assert.Equal(typeof(PresentationTraceSources), property.OwnerType);
        Assert.Equal(typeof(PresentationTraceLevel), property.PropertyType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
        Assert.Same(property, PresentationTraceSources.TraceLevelProperty);
    }

    public static IEnumerable<object?[]> Element_TestData()
    {
        yield return new object?[] { new object() };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(Element_TestData))]
    public void GetTraceLevel_Get_ReturnsExpected(object element)
    {
        PresentationTraceLevel value = PresentationTraceSources.GetTraceLevel(element);
        Assert.True(Enum.IsDefined(value));
        Assert.Equal(value, PresentationTraceSources.GetTraceLevel(element));
    }

    [Theory]
    [MemberData(nameof(Element_TestData))]
    public void Refresh_Invoke_Success(object element)
    {
        PresentationTraceLevel value = PresentationTraceSources.GetTraceLevel(element);
        PresentationTraceSources.Refresh();
        Assert.Equal(value, PresentationTraceSources.GetTraceLevel(element));
    }

    public static IEnumerable<object?[]> SetTraceLevel_TestData()
    {
        yield return new object?[] { null, PresentationTraceLevel.None, PresentationTraceLevel.None };
        yield return new object?[] { new object(), PresentationTraceLevel.Low, PresentationTraceLevel.Low };
        yield return new object?[] { new object(), PresentationTraceLevel.None - 1, PresentationTraceLevel.None };
        yield return new object?[] { new object(), PresentationTraceLevel.High + 1, PresentationTraceLevel.High + 1 };
    }

    [Theory]
    [MemberData(nameof(SetTraceLevel_TestData))]
    public void SetTraceLevel_Invoke_GetReturnsExpected(object element, PresentationTraceLevel traceLevel, PresentationTraceLevel expected)
    {
        PresentationTraceSources.SetTraceLevel(element, traceLevel);
        Assert.Equal(expected, PresentationTraceSources.GetTraceLevel(element));
    }

    [Fact]
    public void AnimationSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.AnimationSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.AnimationSource);
        Assert.Equal("System.Windows.Media.Animation", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void DataBindingSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.DataBindingSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.DataBindingSource);
        Assert.Equal("System.Windows.Data", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void DependencyPropertySource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.DependencyPropertySource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.DependencyPropertySource);
        Assert.Equal("System.Windows.DependencyProperty", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void DocumentsSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.DocumentsSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.DocumentsSource);
        Assert.Equal("System.Windows.Documents", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void FreezableSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.FreezableSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.FreezableSource);
        Assert.Equal("System.Windows.Freezable", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void HwndHostSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.HwndHostSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.HwndHostSource);
        Assert.Equal("System.Windows.Interop.HwndHost", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void MarkupSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.MarkupSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.MarkupSource);
        Assert.Equal("System.Windows.Markup", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void NameScopeSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.NameScopeSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.NameScopeSource);
        Assert.Equal("System.Windows.NameScope", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void ResourceDictionarySource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.ResourceDictionarySource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.ResourceDictionarySource);
        Assert.Equal("System.Windows.ResourceDictionary", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void RoutedEventSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.RoutedEventSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.RoutedEventSource);
        Assert.Equal("System.Windows.RoutedEvent", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }

    [Fact]
    public void ShellSource_Get_ReturnsExpected()
    {
        TraceSource source = PresentationTraceSources.ShellSource;
        Assert.NotNull(source);
        Assert.Same(source, PresentationTraceSources.ShellSource);
        Assert.Equal("System.Windows.Shell", source.Name);
        Assert.True(Enum.IsDefined(source.Switch.Level));
    }
}