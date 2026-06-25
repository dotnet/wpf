// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows;

public class ResourceDictionaryTests
{
    private readonly ResourceDictionary _dictionary;
    private string SampleDictionaryPath => "/PresentationFramework.Tests;component/System/Windows/Resources/SampleResourceDictionary.xaml";

    public ResourceDictionaryTests()
    {
        _dictionary = (ResourceDictionary)Application.LoadComponent(new Uri(SampleDictionaryPath, UriKind.Relative));
    }

    [Fact]
    public void Dictionary_ShouldContainStaticResource()
    {
        _dictionary.Contains("StaticBrush").Should().BeTrue();
        _dictionary["StaticBrush"].Should().BeOfType<SolidColorBrush>();
    }

    [Fact]
    public void Dictionary_ShouldContainDynamicResource()
    {
        _dictionary.Contains("DynamicBrush").Should().BeTrue();
        _dictionary["DynamicBrush"].Should().BeAssignableTo<Brush>();
    }

    [Fact]
    public void Dictionary_ShouldContainGradientBrush()
    {
        _dictionary.Contains("GradientBackground").Should().BeTrue();
        _dictionary["GradientBackground"].Should().BeOfType<LinearGradientBrush>();
    }

    [Fact]
    public void Dictionary_ShouldContainStyleForButton()
    {
        _dictionary.Contains("PrimaryButtonStyle").Should().BeTrue();

        var style = _dictionary["PrimaryButtonStyle"] as Style;

        style.Should().NotBeNull();

        style!.TargetType.Should().Be(typeof(Button));
        style.Setters.Should().NotBeEmpty();
    }

    [Fact]
    public void Dictionary_StyleShouldContainTrigger()
    {
        var style = _dictionary["PrimaryButtonStyle"] as Style;

        style.Should().NotBeNull();

#pragma warning disable IDE0002 // Name should be simplified
        style!.Triggers.Should().NotBeNull();
        style.Triggers.OfType<Trigger>().Should().ContainSingle(t =>
            t.Property == Button.IsMouseOverProperty &&
            t.Value.Equals(true));
#pragma warning restore IDE0002 // Name should be simplified
    }

    [Fact]
    public void Dictionary_ShouldContainDataTemplate()
    {
        _dictionary.Contains("ItemTemplate").Should().BeTrue();
        _dictionary["ItemTemplate"].Should().BeOfType<DataTemplate>();
    }

    [Fact]
    public void Dictionary_ShouldContainControlTemplate()
    {
        _dictionary.Contains("CustomControlTemplate").Should().BeTrue();
        _dictionary["CustomControlTemplate"].Should().BeOfType<ControlTemplate>();
    }

    [Fact]
    public void Dictionary_ShouldContainStyleWithBasedOn()
    {
        var baseStyle = _dictionary["BaseTextBlockStyle"] as Style;
        var derivedStyle = _dictionary["DerivedTextBlockStyle"] as Style;

        baseStyle.Should().NotBeNull();
        derivedStyle.Should().NotBeNull();
        derivedStyle!.BasedOn.Should().Be(baseStyle);
    }

    [Fact]
    public void Dictionary_StyleShouldHaveMultiTrigger()
    {
        var style = _dictionary["MultiTriggerStyle"] as Style;

        style.Should().NotBeNull();
        style!.Triggers.OfType<MultiTrigger>().Should().Contain(mt =>
            mt.Conditions.Count == 2);
    }

    [WpfFact]
    public void DynamicResource_ShouldResolve_FromLocalResourceDictionary()
    {
        var key = "DynamicBrush";

        var brush = new SolidColorBrush(Colors.Green);
        var textBlock = new TextBlock();

        textBlock.Resources[key] = brush;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, key);

        textBlock.Foreground.Should().BeSameAs(brush);
    }

    [WpfFact]
    public void DynamicResource_ShouldUpdate_WhenLocalResourceChanges()
    {
        var key = "DynamicBrush";
        var initialBrush = new SolidColorBrush(Colors.Green);
        var updatedBrush = new SolidColorBrush(Colors.Red);

        var textBlock = new TextBlock();
        textBlock.Resources[key] = initialBrush;
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, key);

        textBlock.Foreground.Should().BeSameAs(initialBrush);

        // Update local resource
        textBlock.Resources[key] = updatedBrush;

        // Force UI changes
        textBlock.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

        textBlock.Foreground.Should().BeSameAs(updatedBrush);
    }

    [WpfFact]
    public void DynamicResource_ShouldRemainLastValue_WhenLocalResourceRemoved()
    {
        var key = "DynamicBrush";
        var initialBrush = new SolidColorBrush(Colors.Green);

        var textBlock = new TextBlock();
        textBlock.SetResourceReference(TextBlock.ForegroundProperty, key);

        textBlock.Resources[key] = initialBrush;
        textBlock.Foreground.Should().BeSameAs(initialBrush);

        // Remove the key from resources
        textBlock.Resources.Remove(key);

        // Force UI Changes
        textBlock.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

        // The default foreground color of textBlock (as in aero2)
        textBlock.Foreground.Should().BeSameAs(Brushes.Black);
    }
}
