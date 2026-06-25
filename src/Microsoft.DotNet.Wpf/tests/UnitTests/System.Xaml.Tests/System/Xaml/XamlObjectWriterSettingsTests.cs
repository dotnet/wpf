// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xaml.Permissions;
using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Tests;

public class XamlObjectWriterSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlObjectWriterSettings();
        Assert.Null(settings.AfterBeginInitHandler);
        Assert.Null(settings.BeforePropertiesHandler);
        Assert.Null(settings.AfterPropertiesHandler);
        Assert.Null(settings.AfterEndInitHandler);
        Assert.Null(settings.XamlSetValueHandler);
        Assert.Null(settings.RootObjectInstance);
        Assert.False(settings.IgnoreCanConvert);
        Assert.Null(settings.ExternalNameScope);
        Assert.False(settings.SkipDuplicatePropertyCheck);
        Assert.False(settings.RegisterNamesOnExternalNamescope);
        Assert.False(settings.SkipProvideValueOnRoot);
        Assert.False(settings.PreferUnconvertedDictionaryKeys);
        Assert.Null(settings.SourceBamlUri);
        Assert.Null(settings.AccessLevel);
    }

    [Fact]
    public void Ctor_XamlObjectWriterSettings()
    {
        var settings = new XamlObjectWriterSettings
        {
            AfterBeginInitHandler = EventHandler!,
            BeforePropertiesHandler = EventHandler!,
            AfterPropertiesHandler = EventHandler!,
            AfterEndInitHandler = EventHandler!,
            XamlSetValueHandler = SetValueEventHandler!,
            RootObjectInstance = new object(),
            IgnoreCanConvert= true,
            ExternalNameScope = new CustomNameScope(),
            SkipDuplicatePropertyCheck = true,
            RegisterNamesOnExternalNamescope = true,
            SkipProvideValueOnRoot = true,
            PreferUnconvertedDictionaryKeys = true,
            SourceBamlUri = new Uri("http://google.com"),
            AccessLevel = XamlAccessLevel.AssemblyAccessTo(typeof(int).Assembly)
        };
        var newSettings = new XamlObjectWriterSettings(settings);
        Assert.Equal(settings.AfterBeginInitHandler, newSettings.AfterBeginInitHandler);
        Assert.Equal(settings.BeforePropertiesHandler, newSettings.BeforePropertiesHandler);
        Assert.Equal(settings.AfterPropertiesHandler, newSettings.AfterPropertiesHandler);
        Assert.Equal(settings.AfterEndInitHandler, newSettings.AfterEndInitHandler);
        Assert.Equal(settings.XamlSetValueHandler, newSettings.XamlSetValueHandler);
        Assert.Equal(settings.RootObjectInstance, newSettings.RootObjectInstance);
        Assert.Equal(settings.IgnoreCanConvert, newSettings.IgnoreCanConvert);
        Assert.Equal(settings.ExternalNameScope, newSettings.ExternalNameScope);
        Assert.Equal(settings.SkipDuplicatePropertyCheck, newSettings.SkipDuplicatePropertyCheck);
        Assert.Equal(settings.RegisterNamesOnExternalNamescope, newSettings.RegisterNamesOnExternalNamescope);
        Assert.Equal(settings.SkipProvideValueOnRoot, newSettings.SkipProvideValueOnRoot);
        Assert.Equal(settings.PreferUnconvertedDictionaryKeys, newSettings.PreferUnconvertedDictionaryKeys);
        Assert.Equal(settings.SourceBamlUri, newSettings.SourceBamlUri);
        Assert.Equal(settings.AccessLevel, newSettings.AccessLevel);
    }

    [Fact]
    public void Ctor_NullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("settings", () => new XamlObjectWriterSettings(null));
    }

    private static void EventHandler(object sender, XamlObjectEventArgs e)
    {
    }

    private static void SetValueEventHandler(object sender, XamlSetValueEventArgs e)
    {
    }

    private class CustomNameScope : INameScope
    {
        public void RegisterName(string name, object scopedElement)
        {
        }

        public void UnregisterName(string name)
        {
        }

        public object FindName(string name) => null!;            
    }
}
