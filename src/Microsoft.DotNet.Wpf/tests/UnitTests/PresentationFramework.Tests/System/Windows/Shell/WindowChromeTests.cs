// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Markup;
using System.Windows.Media;

namespace System.Windows.Shell;

public sealed class WindowChromeTests
{
    [WpfFact]
    public void NewProperties_HaveExpectedDefaults()
    {
        var chrome = new WindowChrome();

        chrome.FrameMode.Should().Be(WindowChromeFrameMode.Custom);
        chrome.ShowSystemIcon.Should().BeNull();
        chrome.ShowTitle.Should().BeNull();
        chrome.CaptionColor.Should().BeNull();
        chrome.CaptionTextColor.Should().BeNull();
        chrome.BorderColor.Should().BeNull();
        chrome.CornerPreference.Should().Be(WindowCornerPreference.Default);
        chrome.CaptionTheme.Should().Be(WindowCaptionTheme.Auto);
        chrome.BackdropType.Should().Be(WindowBackdropKind.Auto);
    }

    [WpfFact]
    public void Xaml_ParsesNullableColorsAndEnums()
    {
        const string xaml =
            "<WindowChrome xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "CaptionColor='#FF112233' BorderColor='Transparent' CornerPreference='RoundSmall' " +
            "BackdropType='Mica' CaptionTheme='Dark' FrameMode='ExtendedClientArea' " +
            "ShowTitle='False' />";

        var chrome = (WindowChrome)XamlReader.Parse(xaml);

        chrome.CaptionColor.Should().Be(Color.FromArgb(0xFF, 0x11, 0x22, 0x33));
        chrome.BorderColor.Should().Be(Colors.Transparent);
        chrome.CaptionTextColor.Should().BeNull();
        chrome.CornerPreference.Should().Be(WindowCornerPreference.RoundSmall);
        chrome.BackdropType.Should().Be(WindowBackdropKind.Mica);
        chrome.CaptionTheme.Should().Be(WindowCaptionTheme.Dark);
        chrome.FrameMode.Should().Be(WindowChromeFrameMode.ExtendedClientArea);
        chrome.ShowTitle.Should().Be(false);
    }

    [WpfTheory]
    [InlineData(-1)]
    [InlineData(3)]
    public void FrameMode_UndefinedValue_Throws(int value)
    {
        var chrome = new WindowChrome();

        Action act = () => chrome.FrameMode = (WindowChromeFrameMode)value;

        act.Should().Throw<ArgumentException>();
    }

    [WpfTheory]
    [InlineData(-1)]
    [InlineData(4)]
    public void CornerPreference_UndefinedValue_Throws(int value)
    {
        var chrome = new WindowChrome();

        Action act = () => chrome.CornerPreference = (WindowCornerPreference)value;

        act.Should().Throw<ArgumentException>();
    }

    [WpfTheory]
    [InlineData(-1)]
    [InlineData(5)]
    public void BackdropType_UndefinedValue_Throws(int value)
    {
        var chrome = new WindowChrome();

        Action act = () => chrome.BackdropType = (WindowBackdropKind)value;

        act.Should().Throw<ArgumentException>();
    }

    [WpfFact]
    public void AttachedReadOnlyProperties_HaveSafeDefaultsOnAnUnshownWindow()
    {
        var window = new Window();

        WindowChrome.GetCaptionButtonsBounds(window).Should().Be(default(Rect));
        WindowChrome.GetCaptionButtonsClip(window).Should().BeNull();
        WindowChrome.GetTitleBarBounds(window).Should().Be(default(Rect));
        WindowChrome.GetCaptionForeground(window).Should().NotBeNull();
    }

    [WpfFact]
    public void SharedChrome_CanBeAssignedToSeveralWindows()
    {
        // Per-window state lives on the worker / on the Window, never on the (shareable) chrome.
        var chrome = new WindowChrome { FrameMode = WindowChromeFrameMode.SystemFrame, CaptionColor = Colors.Red };
        var first = new Window();
        var second = new Window();

        WindowChrome.SetWindowChrome(first, chrome);
        WindowChrome.SetWindowChrome(second, chrome);

        WindowChrome.GetWindowChrome(first).Should().BeSameAs(chrome);
        WindowChrome.GetWindowChrome(second).Should().BeSameAs(chrome);
    }

    [WpfFact]
    public void ShowTitleAndIcon_UnsetMeansHiddenOnlyInExtendedClientArea()
    {
        var chrome = new WindowChrome();

        chrome.EffectiveShowTitle.Should().BeTrue();
        chrome.EffectiveShowSystemIcon.Should().BeTrue();

        chrome.FrameMode = WindowChromeFrameMode.ExtendedClientArea;
        chrome.EffectiveShowTitle.Should().BeFalse();
        chrome.EffectiveShowSystemIcon.Should().BeFalse();

        // An explicit value always wins.
        chrome.ShowTitle = true;
        chrome.EffectiveShowTitle.Should().BeTrue();
        chrome.ShowSystemIcon = false;
        chrome.FrameMode = WindowChromeFrameMode.Custom;
        chrome.EffectiveShowSystemIcon.Should().BeFalse();
    }

    [WpfFact]
    public void ResolveCaptionColor_TransparentMeansNoFillOnlyOverABackdrop()
    {
        bool allowNone;

        // Transparent over a backdrop: ask DWM for no caption fill.
        WindowChromeWorker.ResolveCaptionColor(Colors.Transparent, backdropApplied: true, out allowNone)
            .Should().Be(Colors.Transparent);
        allowNone.Should().BeTrue();

        // Transparent without a backdrop: treated as not set.
        WindowChromeWorker.ResolveCaptionColor(Colors.Transparent, backdropApplied: false, out allowNone)
            .Should().BeNull();
        allowNone.Should().BeFalse();

        // Opaque colors and null pass through unchanged, whatever the backdrop state.
        WindowChromeWorker.ResolveCaptionColor(Colors.Red, backdropApplied: true, out allowNone).Should().Be(Colors.Red);
        allowNone.Should().BeFalse();
        WindowChromeWorker.ResolveCaptionColor(null, backdropApplied: true, out allowNone).Should().BeNull();
        allowNone.Should().BeFalse();
    }

    [WpfFact]
    public void CaptionHeightAndGlassFrameThickness_AreExplicitOnlyWhenTheApplicationSetsThem()
    {
        var chrome = new WindowChrome();

        // Fresh chrome: both follow the system defaults bound by the constructor.
        chrome.IsCaptionHeightSet.Should().BeFalse();
        chrome.IsGlassFrameThicknessSet.Should().BeFalse();

        chrome.CaptionHeight = 40;
        chrome.IsCaptionHeightSet.Should().BeTrue();
        chrome.IsGlassFrameThicknessSet.Should().BeFalse();

        chrome.GlassFrameThickness = new Thickness(1);
        chrome.IsGlassFrameThicknessSet.Should().BeTrue();

        // A binding of the application's own counts as explicit too.
        var other = new WindowChrome();
        System.Windows.Data.BindingOperations.SetBinding(other, WindowChrome.CaptionHeightProperty,
            new System.Windows.Data.Binding("(SystemParameters.WindowCaptionHeight)"));
        other.IsCaptionHeightSet.Should().BeTrue();
    }

    [WpfFact]
    public void Clone_KeepsTheSystemDefaultsImplicit()
    {
        // A clone copies the binding expressions of its source: those still point at the constructor's bindings.
        var chrome = new WindowChrome { FrameMode = WindowChromeFrameMode.ExtendedClientArea };

        var clone = (WindowChrome)chrome.Clone();

        clone.IsCaptionHeightSet.Should().BeFalse();
        clone.IsGlassFrameThicknessSet.Should().BeFalse();
        clone.FrameMode.Should().Be(WindowChromeFrameMode.ExtendedClientArea);

        chrome.CaptionHeight = 40;
        ((WindowChrome)chrome.Clone()).IsCaptionHeightSet.Should().BeTrue();
    }

    [WpfFact]
    public void CreateSystemFrameChrome_UsesSystemFrameMode()
    {
        WindowChrome.CreateSystemFrameChrome().FrameMode.Should().Be(WindowChromeFrameMode.SystemFrame);
    }

    [WpfFact]
    public void EnsureWorker_AttachesASystemFrameChromeWhenTheWindowHasNone()
    {
        var window = new Window();

        WindowChromeWorker worker = WindowChromeWorker.EnsureWorker(window);

        worker.Should().NotBeNull();
        WindowChrome.GetWindowChrome(window).Should().NotBeNull();
        WindowChrome.GetWindowChrome(window)!.FrameMode.Should().Be(WindowChromeFrameMode.SystemFrame);

        // An application chrome set later replaces the automatic one.
        var custom = new WindowChrome();
        WindowChrome.SetWindowChrome(window, custom);
        WindowChrome.GetWindowChrome(window).Should().BeSameAs(custom);
        WindowChromeWorker.EnsureWorker(window).Should().BeSameAs(worker);
    }

    [WpfFact]
    public void SupportStatics_FollowTheOsBuild()
    {
        int build = Environment.OSVersion.Version.Build;

        WindowChrome.IsCaptionCustomizationSupported.Should().Be(build >= 22000);
        WindowChrome.IsBackdropSupported.Should().Be(build >= 22621);
    }
}
