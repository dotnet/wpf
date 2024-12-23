// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class XamlWriterSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        new XamlWriterSettings();
    }

    public static IEnumerable<object?[]> Ctor_XamlWriterSettings_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new XamlWriterSettings() };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlWriterSettings_TestData))]
    public void Ctor_XamlWriterSettings(XamlWriterSettings settings)
    {
        new XamlWriterSettings(settings);
    }
}
