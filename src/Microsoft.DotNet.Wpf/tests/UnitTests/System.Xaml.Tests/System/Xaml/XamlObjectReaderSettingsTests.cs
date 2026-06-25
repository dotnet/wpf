// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests;

public class XamlObjectReaderSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlObjectReaderSettings();
        Assert.False(settings.AllowProtectedMembersOnRoot);
        Assert.Equal(settings.ProvideLineInfo, settings.ProvideLineInfo);
        Assert.Null(settings.BaseUri);
        Assert.Null(settings.LocalAssembly);
        Assert.False(settings.IgnoreUidsOnPropertyElements);
        Assert.False(settings.ValuesMustBeString);
        Assert.False(settings.RequireExplicitContentVisibility);
    }
}
