// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Helper methods for DirectWriteForwarder tests.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Path to the Windows Fonts directory.
    /// </summary>
    public static string FontsDirectory => Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
    
    /// <summary>
    /// Path to Arial font file.
    /// </summary>
    public static string ArialPath => Path.Combine(FontsDirectory, "arial.ttf");
    
    /// <summary>
    /// Path to Cambria font file.
    /// </summary>
    public static string CambriaPath => Path.Combine(FontsDirectory, "cambria.ttc");
    
    /// <summary>
    /// Skips the test if Arial font is not available.
    /// </summary>
    public static void SkipIfArialNotAvailable()
    {
        Assert.SkipUnless(File.Exists(ArialPath), "Arial font not found");
    }
    
    /// <summary>
    /// Skips the test if the specified font file is not available.
    /// </summary>
    public static void SkipIfFontNotAvailable(string fontPath, string fontName)
    {
        Assert.SkipUnless(File.Exists(fontPath), $"{fontName} font not found");
    }
    
    /// <summary>
    /// Skips the test if Arial font family cannot be retrieved.
    /// </summary>
    public static FontFamily GetArialFamilyOrSkip()
    {
        var factory = DWriteFactory.Instance;
        var fontCollection = factory.GetSystemFontCollection();
        var arialFamily = fontCollection["Arial"];
        Assert.SkipUnless(arialFamily != null, "Arial font family not found");
        return arialFamily!;
    }
    
    /// <summary>
    /// Skips the test if no font with the specified name can be found.
    /// </summary>
    public static FontFamily GetFontFamilyOrSkip(string familyName)
    {
        var factory = DWriteFactory.Instance;
        var fontCollection = factory.GetSystemFontCollection();
        var family = fontCollection[familyName];
        Assert.SkipUnless(family != null, $"{familyName} font family not found");
        return family!;
    }
    
    /// <summary>
    /// Gets Arial font or skips the test.
    /// </summary>
    public static Font GetArialFontOrSkip()
    {
        var family = GetArialFamilyOrSkip();
        Assert.SkipUnless(family.Count > 0, "Arial font family has no fonts");
        return family[0];
    }
    
    /// <summary>
    /// Gets a localized strings object from Arial or skips the test.
    /// </summary>
    public static LocalizedStrings GetArialLocalizedStringsOrSkip()
    {
        var family = GetArialFamilyOrSkip();
        var localizedStrings = family.FamilyNames;
        Assert.SkipUnless(localizedStrings != null, "Arial font family has no localized names");
        return localizedStrings!;
    }
    
    /// <summary>
    /// Gets the first culture from localized strings or skips the test.
    /// </summary>
    public static CultureInfo GetFirstCultureOrSkip(LocalizedStrings localizedStrings)
    {
        Assert.SkipUnless(localizedStrings.Count > 0, "Localized strings is empty");
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        Assert.SkipUnless(firstCulture != null, "No culture found in localized strings");
        return firstCulture!;
    }
}
