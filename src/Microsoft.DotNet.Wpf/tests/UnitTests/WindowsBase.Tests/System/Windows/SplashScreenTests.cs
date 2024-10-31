// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows;

public class SplashScreenTests
{
    [Fact]
    public void Constructor_NullImageSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SplashScreen(null));
    }

    [WpfFact]
    public void Create()
    {
        SplashScreen splash = new(typeof(SplashScreenTests).Assembly, "Needle");
        splash.Show(autoClose: true);
    }
}
