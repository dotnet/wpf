// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests;

public class XamlXmlWriterSettingsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var settings = new XamlXmlWriterSettings();
        Assert.False(settings.AssumeValidInput);
        Assert.False(settings.CloseOutput);
    }

    [Fact]
    public void Copy_Invoke_ReturnsExpected()
    {
        var settings = new XamlXmlWriterSettings
        {
            AssumeValidInput = true,
            CloseOutput = true
        };
        XamlXmlWriterSettings newSettings = settings.Copy();
        Assert.Equal(settings.AssumeValidInput, newSettings.AssumeValidInput);
        Assert.Equal(settings.CloseOutput, newSettings.CloseOutput);
    }
}
