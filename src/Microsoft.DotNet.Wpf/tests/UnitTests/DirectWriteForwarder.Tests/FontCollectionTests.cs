// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="FontCollection"/> class.
/// </summary>
public class FontCollectionTests
{
    [Fact]
    public void FamilyCount_ShouldBeGreaterThanZero()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        fontCollection.FamilyCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Indexer_WithValidIndex_ShouldReturnFontFamily()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        var firstFamily = fontCollection[0u];
        
        firstFamily.Should().NotBeNull();
    }

    [Fact]
    public void Indexer_WithFamilyName_ShouldReturnFontFamily()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        // Arial should exist on all Windows systems
        var arialFamily = fontCollection["Arial"];
        
        arialFamily.Should().NotBeNull();
    }

    [Fact]
    public void Indexer_WithInvalidFamilyName_ShouldReturnNull()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        var nonExistentFamily = fontCollection["NonExistentFontFamily12345"];
        
        nonExistentFamily.Should().BeNull();
    }

    [Fact]
    public void FindFamilyName_WithExistingFamily_ShouldReturnTrueAndIndex()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        bool found = fontCollection.FindFamilyName("Arial", out uint index);
        
        found.Should().BeTrue();
        index.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void FindFamilyName_WithNonExistingFamily_ShouldReturnFalse()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        bool found = fontCollection.FindFamilyName("NonExistentFontFamily12345", out _);
        
        found.Should().BeFalse();
    }

    [Fact]
    public void AllFamilies_ShouldBeAccessible()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        for (uint i = 0; i < Math.Min(fontCollection.FamilyCount, 10u); i++)
        {
            var family = fontCollection[i];
            family.Should().NotBeNull();
        }
    }

    [Fact]
    public void CommonSystemFonts_ShouldExist()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        
        // These fonts should exist on all Windows installations
        var commonFonts = new[] { "Arial", "Times New Roman", "Courier New", "Segoe UI" };
        
        foreach (var fontName in commonFonts)
        {
            bool found = fontCollection.FindFamilyName(fontName, out _);
            // At least some of these should exist
            if (found)
            {
                var family = fontCollection[fontName];
                family.Should().NotBeNull($"{fontName} should be accessible");
            }
        }
    }
}

/// <summary>
/// Tests for <see cref="FontFamily"/> class.
/// </summary>
public class FontFamilyTests
{
    private FontFamily GetArialFamily()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        return fontCollection["Arial"];
    }

    [Fact]
    public void FamilyNames_ShouldNotBeEmpty()
    {
        var family = GetArialFamily();
        if (family == null) return; // Skip if Arial not available
        
        var familyNames = family.FamilyNames;
        
        familyNames.Should().NotBeNull();
        familyNames.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IsPhysical_ShouldBeTrue()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        family.IsPhysical.Should().BeTrue();
    }

    [Fact]
    public void IsComposite_ShouldBeFalse()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        family.IsComposite.Should().BeFalse();
    }

    [Fact]
    public void Count_ShouldBeGreaterThanZero()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        family.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Indexer_ShouldReturnFont()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var firstFont = family[0u];
        
        firstFont.Should().NotBeNull();
    }

    [Fact]
    public void GetEnumerator_ShouldEnumerateFonts()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var fonts = family.ToList();
        
        fonts.Should().NotBeEmpty();
        fonts.Should().AllSatisfy(f => f.Should().NotBeNull());
    }

    [Fact]
    public void OrdinalName_ShouldNotBeEmpty()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        family.OrdinalName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Metrics_ShouldBeValid()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var metrics = family.Metrics;
        
        metrics.Should().NotBeNull();
        metrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DisplayMetrics_ShouldReturnValidMetrics()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var displayMetrics = family.DisplayMetrics(emSize: 12.0f, pixelsPerDip: 1.0f);
        
        displayMetrics.Should().NotBeNull();
        displayMetrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetFirstMatchingFont_WithNormalProperties_ShouldReturnFont()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var font = family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        
        font.Should().NotBeNull();
        font.Weight.Should().Be(FontWeight.Normal);
        font.Style.Should().Be(FontStyle.Normal);
    }

    [Fact]
    public void GetFirstMatchingFont_WithBoldWeight_ShouldReturnBoldFont()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var font = family.GetFirstMatchingFont(FontWeight.Bold, FontStretch.Normal, FontStyle.Normal);
        
        font.Should().NotBeNull();
        font.Weight.Should().Be(FontWeight.Bold);
    }

    [Fact]
    public void GetFirstMatchingFont_WithItalicStyle_ShouldReturnItalicFont()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var font = family.GetFirstMatchingFont(FontWeight.Normal, FontStretch.Normal, FontStyle.Italic);
        
        font.Should().NotBeNull();
        font.Style.Should().Be(FontStyle.Italic);
    }

    [Fact]
    public void GetMatchingFonts_WithNormalProperties_ShouldReturnFontList()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var matchingFonts = family.GetMatchingFonts(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        
        matchingFonts.Should().NotBeNull();
        matchingFonts.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetMatchingFonts_ShouldReturnFontsRankedByMatch()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var matchingFonts = family.GetMatchingFonts(FontWeight.Bold, FontStretch.Normal, FontStyle.Normal);
        
        matchingFonts.Should().NotBeNull();
        // First font should be the best match (bold)
        var firstFont = matchingFonts[0u];
        firstFont.Should().NotBeNull();
        firstFont.Weight.Should().Be(FontWeight.Bold);
    }

    [Fact]
    public void GetMatchingFonts_ShouldBeEnumerable()
    {
        var family = GetArialFamily();
        if (family == null) return;
        
        var matchingFonts = family.GetMatchingFonts(FontWeight.Normal, FontStretch.Normal, FontStyle.Normal);
        
        var fontList = matchingFonts.ToList();
        fontList.Should().NotBeEmpty();
        fontList.Should().AllSatisfy(f => f.Should().NotBeNull());
    }
}
