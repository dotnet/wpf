// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class XamlReaderSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlReaderSettings();
        Assert.False(settings.AllowProtectedMembersOnRoot);
        Assert.Equal(settings.ProvideLineInfo, settings.ProvideLineInfo);
        Assert.Null(settings.BaseUri);
        Assert.Null(settings.LocalAssembly);
        Assert.False(settings.IgnoreUidsOnPropertyElements);
        Assert.False(settings.ValuesMustBeString);
    }

    public static IEnumerable<object?[]> Ctor_XamlReaderSettings_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[]
        {
            new XamlReaderSettings
            {
                AllowProtectedMembersOnRoot = true,
                ProvideLineInfo = true,
                BaseUri = new Uri("http://google.com"),
                LocalAssembly = typeof(XamlReaderSettings).Assembly,
                IgnoreUidsOnPropertyElements = true,
                ValuesMustBeString = true
            }
        };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlReaderSettings_TestData))]
    public void Ctor_XamlReaderSettings(XamlReaderSettings settings)
    {
        var newSettings = new XamlReaderSettings(settings);
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
    }
}
