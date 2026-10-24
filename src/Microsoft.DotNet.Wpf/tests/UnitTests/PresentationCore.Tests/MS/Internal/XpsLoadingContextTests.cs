// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Packaging;
using MS.Internal;

namespace PresentationCore.Tests.MS.Internal;

/// <summary>
/// Tests for <see cref="XpsLoadingContext"/> XPS package-boundary enforcement.
/// </summary>
/// <remarks>
/// Regression coverage for the servicing bug where an in-memory XPS document
/// registered in <see cref="PackageStore"/> under a bare <c>pack://</c> identity
/// (the customer's PackageStore key) could no longer load its own resources.
///
/// The ambient/deferred sinks (FontSource, BitmapDecoder, Glyphs, ...) pass the
/// already-extracted <c>ActivePackageUri</c> as the package origin. Re-parsing
/// that bare <c>pack://</c> identity through <see cref="PackUriHelper.GetPackageUri"/>
/// makes the underlying <see cref="Uri"/> constructor throw <see cref="UriFormatException"/>.
/// The original code only caught <see cref="ArgumentException"/> at that inner
/// site, so a same-package resource was wrongly rejected (fail-closed), which
/// surfaced to the user as an unhandled <c>FileFormatException</c> /
/// <c>XamlParseException</c> during print preview. The fix also catches
/// <see cref="UriFormatException"/> and <see cref="InvalidOperationException"/>
/// so the origin falls back to the identity as-is.
///
/// These tests must keep the negative (external / cross-package) cases passing
/// to prove the fix does NOT widen the security boundary.
/// </remarks>
public sealed class XpsLoadingContextTests
{
    // A relative XPS part used to compose realistic package part URIs.
    private static readonly Uri s_partPath = new("/Documents/1/FixedPage.fpage", UriKind.Relative);

    /// <summary>
    /// Builds the (origin, samePackageResource) pair the deferred sinks see for a
    /// given package identity: the origin is the already-extracted package URI
    /// (as published to <c>ActivePackageUri</c>), and the resource is another part
    /// in the same package.
    /// </summary>
    private static (Uri origin, Uri sameResource) BuildSamePackage(string identity)
    {
        Uri packageIdentity = new(identity, UriKind.Absolute);
        Uri partBase = PackUriHelper.Create(packageIdentity, s_partPath);
        Uri origin = PackUriHelper.GetPackageUri(partBase);
        Uri sameResource = new(partBase, "/Resources/img.png");
        return (origin, sameResource);
    }

    [Fact]
    public void IsUriAllowedAgainstPackage_SamePackagePackScheme_Allowed()
    {
        // Customer scenario: PackageStore key uses the pack:// scheme.
        (Uri origin, Uri sameResource) = BuildSamePackage("pack://memorystream.xps");

        // Before the fix this returned false (UriFormatException escaped the inner
        // catch), crashing print preview. It must now allow the same-package resource.
        Assert.True(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, sameResource));
    }

    [Fact]
    public void IsAllowedPackageRelativeUri_SamePackagePackScheme_Allowed()
    {
        // Same regression along the explicit-parent path used by Glyphs / PageContent
        // (the font/Glyphs sink in the customer crash stack).
        (Uri parent, Uri sameResource) = BuildSamePackage("pack://memorystream.xps");

        Assert.True(XpsLoadingContext.IsAllowedPackageRelativeUri(parent, sameResource));
    }

    [Fact]
    public void EnforcePackageRelativeUri_SamePackagePackScheme_DoesNotThrow()
    {
        (Uri parent, Uri sameResource) = BuildSamePackage("pack://memorystream.xps");

        // The customer crash was an unhandled FileFormatException thrown here.
        var ex = Record.Exception(() => XpsLoadingContext.EnforcePackageRelativeUri(parent, sameResource));
        Assert.Null(ex);
    }

    [Fact]
    public void IsUriAllowedAgainstPackage_SamePackageXpsPackScheme_Allowed()
    {
        // The documented workaround (xpspack:// identity) must keep working.
        (Uri origin, Uri sameResource) = BuildSamePackage("xpspack://memorystream.xps");

        Assert.True(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, sameResource));
    }

    [Fact]
    public void IsUriAllowedAgainstPackage_NullOrigin_AllowedNonXpsScenario()
    {
        // Outside an XPS load there is no origin and everything is permitted.
        Assert.True(XpsLoadingContext.IsUriAllowedAgainstPackage(null, new Uri("http://example.test/a.png")));
    }

    [Fact]
    public void IsUriAllowedAgainstPackage_RelativeResource_Allowed()
    {
        (Uri origin, _) = BuildSamePackage("pack://memorystream.xps");

        // Relative URIs are resolved later and are not rejected here.
        Assert.True(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, new Uri("/Resources/img.png", UriKind.Relative)));
    }

    [Theory]
    [InlineData("http://evil.example.test/a.png")]   // external http
    [InlineData("https://evil.example.test/a.png")]  // external https
    [InlineData("file:///C:/Windows/win.ini")]       // local file
    public void IsUriAllowedAgainstPackage_ExternalResource_Blocked(string resourceUri)
    {
        // Enforcement must remain fail-closed for non-pack schemes.
        (Uri origin, _) = BuildSamePackage("pack://memorystream.xps");

        Assert.False(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, new Uri(resourceUri, UriKind.Absolute)));
    }

    [Fact]
    public void IsUriAllowedAgainstPackage_CrossPackageResource_Blocked()
    {
        // A resource that belongs to a DIFFERENT package must stay blocked.
        (Uri origin, _) = BuildSamePackage("pack://memorystream.xps");
        (_, Uri otherResource) = BuildSamePackage("pack://other.xps");

        Assert.False(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, otherResource));
    }

    [Theory]
    [InlineData("application:")]
    [InlineData("siteoforigin:")]
    public void IsUriAllowedAgainstPackage_WpfInternalPackAuthority_Blocked(string innerScheme)
    {
        // The WPF-internal pack authorities point outside the XPS package.
        (Uri origin, _) = BuildSamePackage("pack://memorystream.xps");
        Uri resource = new($"pack://{innerScheme},,/Resources/img.png", UriKind.Absolute);

        Assert.False(XpsLoadingContext.IsUriAllowedAgainstPackage(origin, resource));
    }
}
