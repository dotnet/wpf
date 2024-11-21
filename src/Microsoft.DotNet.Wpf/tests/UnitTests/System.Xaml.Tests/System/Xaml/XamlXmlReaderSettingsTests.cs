// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class XamlXmlReaderSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlXmlReaderSettings();
        Assert.False(settings.AllowProtectedMembersOnRoot);
        Assert.Equal(settings.ProvideLineInfo, settings.ProvideLineInfo);
        Assert.Null(settings.BaseUri);
        Assert.Null(settings.LocalAssembly);
        Assert.False(settings.IgnoreUidsOnPropertyElements);
        Assert.False(settings.ValuesMustBeString);
        Assert.Null(settings.XmlLang);
        Assert.False(settings.XmlSpacePreserve);
        Assert.False(settings.SkipXmlCompatibilityProcessing);
        Assert.False(settings.CloseInput);
    }

    public static IEnumerable<object?[]> Ctor_XamlXmlReaderSettings_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[]
        {
            new XamlXmlReaderSettings
            {
                AllowProtectedMembersOnRoot = true,
                ProvideLineInfo = true,
                BaseUri = new Uri("http://google.com"),
                LocalAssembly = typeof(XamlXmlReaderSettings).Assembly,
                IgnoreUidsOnPropertyElements = true,
                ValuesMustBeString = true,
                XmlLang = "lang",
                XmlSpacePreserve = true,
                SkipXmlCompatibilityProcessing = true,
                CloseInput = true
            }
        };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlXmlReaderSettings_TestData))]
    public void Ctor_XamlReaderSettings(XamlXmlReaderSettings settings)
    {
        var newSettings = new XamlXmlReaderSettings(settings);
        Assert.Equal(settings?.AllowProtectedMembersOnRoot ?? false, newSettings.AllowProtectedMembersOnRoot);
        if (settings != null)
        {
            Assert.Equal(settings.ProvideLineInfo, newSettings.ProvideLineInfo);
        }
        else
        {
            Assert.Equal(newSettings.ProvideLineInfo, newSettings.ProvideLineInfo);
        }
        Assert.Equal(settings?.BaseUri, newSettings.BaseUri);
        Assert.Equal(settings?.LocalAssembly, newSettings.LocalAssembly);
        Assert.Equal(settings?.IgnoreUidsOnPropertyElements ?? false, newSettings.IgnoreUidsOnPropertyElements);
        Assert.Equal(settings?.ValuesMustBeString ?? false, newSettings.ValuesMustBeString);
        Assert.Equal(settings?.XmlLang, newSettings.XmlLang);
        Assert.Equal(settings?.XmlSpacePreserve ?? false, newSettings.XmlSpacePreserve);
        Assert.Equal(settings?.SkipXmlCompatibilityProcessing ?? false, newSettings.SkipXmlCompatibilityProcessing);
        Assert.Equal(settings?.CloseInput ?? false, newSettings.CloseInput);
    }
}
