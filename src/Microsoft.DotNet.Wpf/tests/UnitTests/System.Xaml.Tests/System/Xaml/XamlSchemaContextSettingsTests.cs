// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class XamlSchemaContextSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlSchemaContextSettings();
        Assert.False(settings.SupportMarkupExtensionsWithDuplicateArity);
        Assert.False(settings.FullyQualifyAssemblyNamesInClrNamespaces);
    }

    public static IEnumerable<object?[]> Ctor_XamlSchemaContextSettings_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[]
        {
            new XamlSchemaContextSettings
            {
                SupportMarkupExtensionsWithDuplicateArity = true,
                FullyQualifyAssemblyNamesInClrNamespaces = true
            }
        };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlSchemaContextSettings_TestData))]
    public void Ctor_XamlSchemaContextSettings(XamlSchemaContextSettings settings)
    {
        var newSettings = new XamlSchemaContextSettings(settings);
        Assert.Equal(settings?.SupportMarkupExtensionsWithDuplicateArity ?? false, newSettings.SupportMarkupExtensionsWithDuplicateArity);
        Assert.Equal(settings?.FullyQualifyAssemblyNamesInClrNamespaces ?? false, newSettings.FullyQualifyAssemblyNamesInClrNamespaces);
    }
}
