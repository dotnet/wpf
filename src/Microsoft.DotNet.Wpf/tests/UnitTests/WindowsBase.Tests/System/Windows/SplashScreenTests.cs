// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Resources;

namespace System.Windows.Tests;

public class SplashScreenTests
{
    [WpfFact]
    public void Create()
    {
        SplashScreen splash = new(typeof(SplashScreenTests).Assembly, "Needle");
        string resourceName = splash.TestAccessor().Dynamic._resourceName;
        resourceName.Should().Be("needle");
        splash.Show(autoClose: true);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("resourceName")]
    public void Ctor_String(string resourceName)
    {
        new SplashScreen(resourceName);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("resourceName")]
    public void Ctor_Assembly_String(string resourceName)
    {
        new SplashScreen(Assembly.GetEntryAssembly(), resourceName);
    }

    [Fact]
    public void Ctor_NullResourceName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("resourceName", () => new SplashScreen(null));
        Assert.Throws<ArgumentNullException>("resourceName", () => new SplashScreen(Assembly.GetEntryAssembly(), null));
    }
    
    [Fact]
    public void Ctor_EmptyResourceName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("resourceName", () => new SplashScreen(string.Empty));
        Assert.Throws<ArgumentNullException>("resourceName", () => new SplashScreen(Assembly.GetEntryAssembly(), string.Empty));
    }

    [Fact]
    public void Ctor_NullResourceAssembly_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("resourceAssembly", () => new SplashScreen(null!, "resourceName"));
    }
    
    [Theory]
    [InlineData(" ")]
    [InlineData("resourceName")]
    public void Close_NotShown_Nop(string resourceName)
    {
        var splashScreen = new SplashScreen(resourceName);
        splashScreen.Close(TimeSpan.Zero);
    }
    
    [Theory]
    [InlineData(" ")]
    [InlineData("resourceName")]
    public void Show_NoSuchResource_ThrowsMissingManifestResourceException(string resourceName)
    {
        var splashScreen = new SplashScreen(resourceName);
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(false));
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(true));
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(false, false));
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(false, true));
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(true, false));
        Assert.Throws<MissingManifestResourceException>(() => splashScreen.Show(true, true));
    }
}
