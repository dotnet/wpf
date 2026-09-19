// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for the FactoryType enum.
/// </summary>
public class FactoryTypeTests
{
    [Fact]
    public void FactoryType_HasExpectedValues()
    {
        // FactoryType should have Shared and Isolated values
        FactoryType.Shared.Should().BeDefined();
        FactoryType.Isolated.Should().BeDefined();
    }

    [Fact]
    public void FactoryType_Shared_IsDefaultValue()
    {
        // Shared should be 0 (first enum value)
        ((int)FactoryType.Shared).Should().Be(0);
    }

    [Fact]
    public void FactoryType_Isolated_IsOne()
    {
        ((int)FactoryType.Isolated).Should().Be(1);
    }

    [Fact]
    public void FactoryType_HasOnlyTwoValues()
    {
        var values = Enum.GetValues<FactoryType>();
        values.Should().HaveCount(2);
    }
}

/// <summary>
/// Tests for the FontStyle enum.
/// </summary>
public class FontStyleTests
{
    [Fact]
    public void FontStyle_Normal_IsZero()
    {
        ((int)FontStyle.Normal).Should().Be(0);
    }

    [Fact]
    public void FontStyle_Oblique_IsOne()
    {
        ((int)FontStyle.Oblique).Should().Be(1);
    }

    [Fact]
    public void FontStyle_Italic_IsTwo()
    {
        ((int)FontStyle.Italic).Should().Be(2);
    }

    [Fact]
    public void FontStyle_HasThreeValues()
    {
        var values = Enum.GetValues<FontStyle>();
        values.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(FontStyle.Normal)]
    [InlineData(FontStyle.Oblique)]
    [InlineData(FontStyle.Italic)]
    public void FontStyle_AllValues_AreDefined(object style)
    {
        Enum.IsDefined(typeof(FontStyle), style).Should().BeTrue();
    }
}

/// <summary>
/// Tests for the FontSimulations flags enum.
/// </summary>
public class FontSimulationsTests
{
    [Fact]
    public void FontSimulations_None_IsZero()
    {
        ((int)FontSimulations.None).Should().Be(0);
    }

    [Fact]
    public void FontSimulations_Bold_IsOne()
    {
        ((int)FontSimulations.Bold).Should().Be(1);
    }

    [Fact]
    public void FontSimulations_Oblique_IsTwo()
    {
        ((int)FontSimulations.Oblique).Should().Be(2);
    }

    [Fact]
    public void FontSimulations_CanCombineBoldAndOblique()
    {
        // FontSimulations is a flags enum, so Bold | Oblique should be valid
        var combined = FontSimulations.Bold | FontSimulations.Oblique;
        ((int)combined).Should().Be(3);
    }

    [Fact]
    public void FontSimulations_HasFlagsAttribute()
    {
        typeof(FontSimulations).GetCustomAttributes(typeof(FlagsAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void FontSimulations_CombinedValue_ContainsBothFlags()
    {
        var combined = FontSimulations.Bold | FontSimulations.Oblique;

        combined.HasFlag(FontSimulations.Bold).Should().BeTrue();
        combined.HasFlag(FontSimulations.Oblique).Should().BeTrue();
        combined.HasFlag(FontSimulations.None).Should().BeTrue(); // None (0) is always contained
    }
}

/// <summary>
/// Tests for the FontWeight enum.
/// </summary>
public class FontWeightTests
{
    [Theory]
    [InlineData(FontWeight.Thin, 100)]
    [InlineData(FontWeight.ExtraLight, 200)]
    [InlineData(FontWeight.UltraLight, 200)]
    [InlineData(FontWeight.Light, 300)]
    [InlineData(FontWeight.Normal, 400)]
    [InlineData(FontWeight.Regular, 400)]
    [InlineData(FontWeight.Medium, 500)]
    [InlineData(FontWeight.DemiBold, 600)]
    [InlineData(FontWeight.SemiBOLD, 600)]
    [InlineData(FontWeight.Bold, 700)]
    [InlineData(FontWeight.ExtraBold, 800)]
    [InlineData(FontWeight.UltraBold, 800)]
    [InlineData(FontWeight.Black, 900)]
    [InlineData(FontWeight.Heavy, 900)]
    [InlineData(FontWeight.ExtraBlack, 950)]
    [InlineData(FontWeight.UltraBlack, 950)]
    public void FontWeight_HasExpectedValues(object weight, int expectedValue)
    {
        ((int)weight).Should().Be(expectedValue);
    }

    [Fact]
    public void FontWeight_ExtraLightAndUltraLight_AreEqual()
    {
        // These are aliases with the same value
        FontWeight.ExtraLight.Should().Be(FontWeight.UltraLight);
    }

    [Fact]
    public void FontWeight_NormalAndRegular_AreEqual()
    {
        FontWeight.Normal.Should().Be(FontWeight.Regular);
    }

    [Fact]
    public void FontWeight_DemiBoldAndSemiBold_AreEqual()
    {
        FontWeight.DemiBold.Should().Be(FontWeight.SemiBOLD);
    }

    [Fact]
    public void FontWeight_BlackAndHeavy_AreEqual()
    {
        FontWeight.Black.Should().Be(FontWeight.Heavy);
    }
}

/// <summary>
/// Tests for the FontStretch enum.
/// </summary>
public class FontStretchTests
{
    [Theory]
    [InlineData(FontStretch.Undefined, 0)]
    [InlineData(FontStretch.UltraCondensed, 1)]
    [InlineData(FontStretch.ExtraCondensed, 2)]
    [InlineData(FontStretch.Condensed, 3)]
    [InlineData(FontStretch.SemiCondensed, 4)]
    [InlineData(FontStretch.Normal, 5)]
    [InlineData(FontStretch.Medium, 5)]
    [InlineData(FontStretch.SemiExpanded, 6)]
    [InlineData(FontStretch.Expanded, 7)]
    [InlineData(FontStretch.ExtraExpanded, 8)]
    [InlineData(FontStretch.UltraExpanded, 9)]
    public void FontStretch_HasExpectedValues(object stretch, int expectedValue)
    {
        ((int)stretch).Should().Be(expectedValue);
    }

    [Fact]
    public void FontStretch_NormalAndMedium_AreEqual()
    {
        FontStretch.Normal.Should().Be(FontStretch.Medium);
    }

    [Fact]
    public void FontStretch_ValuesAreSequential()
    {
        // Values should go from 0 (Undefined) to 9 (UltraExpanded)
        ((int)FontStretch.Undefined).Should().Be(0);
        ((int)FontStretch.UltraExpanded).Should().Be(9);
    }
}

/// <summary>
/// Tests for the FontFaceType enum.
/// </summary>
public class FontFaceTypeTests
{
    [Fact]
    public void FontFaceType_HasExpectedValues()
    {
        FontFaceType.CFF.Should().BeDefined();
        FontFaceType.TrueType.Should().BeDefined();
        FontFaceType.TrueTypeCollection.Should().BeDefined();
        FontFaceType.Type1.Should().BeDefined();
        FontFaceType.Vector.Should().BeDefined();
        FontFaceType.Bitmap.Should().BeDefined();
        FontFaceType.Unknown.Should().BeDefined();
    }

    [Theory]
    [InlineData(FontFaceType.CFF, 0)]
    [InlineData(FontFaceType.TrueType, 1)]
    [InlineData(FontFaceType.TrueTypeCollection, 2)]
    [InlineData(FontFaceType.Type1, 3)]
    [InlineData(FontFaceType.Vector, 4)]
    [InlineData(FontFaceType.Bitmap, 5)]
    [InlineData(FontFaceType.Unknown, 6)]
    public void FontFaceType_HasSequentialValues(object faceType, int expectedValue)
    {
        ((int)faceType).Should().Be(expectedValue);
    }
}

/// <summary>
/// Tests for the FontFileType enum.
/// </summary>
public class FontFileTypeTests
{
    [Fact]
    public void FontFileType_HasExpectedValues()
    {
        FontFileType.Unknown.Should().BeDefined();
        FontFileType.CFF.Should().BeDefined();
        FontFileType.TrueType.Should().BeDefined();
        FontFileType.TrueTypeCollection.Should().BeDefined();
        FontFileType.Type1PFM.Should().BeDefined();
        FontFileType.Type1PFB.Should().BeDefined();
        FontFileType.Vector.Should().BeDefined();
        FontFileType.Bitmap.Should().BeDefined();
    }

    [Theory]
    [InlineData(FontFileType.Unknown, 0)]
    [InlineData(FontFileType.CFF, 1)]
    [InlineData(FontFileType.TrueType, 2)]
    [InlineData(FontFileType.TrueTypeCollection, 3)]
    [InlineData(FontFileType.Type1PFM, 4)]
    [InlineData(FontFileType.Type1PFB, 5)]
    [InlineData(FontFileType.Vector, 6)]
    [InlineData(FontFileType.Bitmap, 7)]
    public void FontFileType_HasSequentialValues(object fileType, int expectedValue)
    {
        ((int)fileType).Should().Be(expectedValue);
    }
}

/// <summary>
/// Tests for the InformationalStringID enum.
/// </summary>
public class InformationalStringIDTests
{
    [Fact]
    public void InformationalStringID_None_IsZero()
    {
        ((int)InformationalStringID.None).Should().Be(0);
    }

    [Fact]
    public void InformationalStringID_HasAllExpectedValues()
    {
        InformationalStringID.None.Should().BeDefined();
        InformationalStringID.CopyrightNotice.Should().BeDefined();
        InformationalStringID.VersionStrings.Should().BeDefined();
        InformationalStringID.Trademark.Should().BeDefined();
        InformationalStringID.Manufacturer.Should().BeDefined();
        InformationalStringID.Designer.Should().BeDefined();
        InformationalStringID.DesignerURL.Should().BeDefined();
        InformationalStringID.Description.Should().BeDefined();
        InformationalStringID.FontVendorURL.Should().BeDefined();
        InformationalStringID.LicenseDescription.Should().BeDefined();
        InformationalStringID.LicenseInfoURL.Should().BeDefined();
        InformationalStringID.WIN32FamilyNames.Should().BeDefined();
        InformationalStringID.Win32SubFamilyNames.Should().BeDefined();
        InformationalStringID.PreferredFamilyNames.Should().BeDefined();
        InformationalStringID.PreferredSubFamilyNames.Should().BeDefined();
        InformationalStringID.SampleText.Should().BeDefined();
    }

    [Fact]
    public void InformationalStringID_ValuesAreSequential()
    {
        var values = Enum.GetValues<InformationalStringID>();

        // Should have 16 values (0-15)
        values.Should().HaveCount(16);

        // Values should be sequential from 0 to 15
        for (int i = 0; i < values.Length; i++)
        {
            ((int)values[i]).Should().Be(i);
        }
    }
}

/// <summary>
/// Tests for the OpenTypeTableTag enum.
/// </summary>
public class OpenTypeTableTagTests
{
    [Fact]
    public void OpenTypeTableTag_HasExpectedTableTags()
    {
        // Core tables
        OpenTypeTableTag.CharToIndexMap.Should().BeDefined();    // cmap
        OpenTypeTableTag.FontHeader.Should().BeDefined();        // head
        OpenTypeTableTag.HoriHeader.Should().BeDefined();        // hhea
        OpenTypeTableTag.HorizontalMetrics.Should().BeDefined(); // hmtx
        OpenTypeTableTag.ControlValue.Should().BeDefined();      // cvt
        OpenTypeTableTag.FontProgram.Should().BeDefined();       // fpgm
        OpenTypeTableTag.MaxProfile.Should().BeDefined();        // maxp
        OpenTypeTableTag.NamingTable.Should().BeDefined();       // name
        OpenTypeTableTag.IndexToLoc.Should().BeDefined();        // loca
        OpenTypeTableTag.GlyphData.Should().BeDefined();         // glyf
    }

    [Fact]
    public void OpenTypeTableTag_HasOpenTypeLayoutTables()
    {
        // OpenType layout tables
        OpenTypeTableTag.TTO_GSUB.Should().BeDefined();
        OpenTypeTableTag.TTO_GPOS.Should().BeDefined();
        OpenTypeTableTag.TTO_GDEF.Should().BeDefined();
    }

    [Fact]
    public void OpenTypeTableTag_HasOS2Table()
    {
        // OS/2 table (Windows-specific metrics)
        OpenTypeTableTag.OS_2.Should().BeDefined();
    }

    [Fact]
    public void OpenTypeTableTag_ValuesAreOpenTypeTags()
    {
        // OpenType table tags are 4-byte ASCII identifiers
        // 'head' = 0x68656164
        var headTag = (uint)OpenTypeTableTag.FontHeader;
        var headBytes = BitConverter.GetBytes(headTag);
        
        // Should decode to 'head' (little-endian on Windows)
        var tagString = System.Text.Encoding.ASCII.GetString(headBytes);
        tagString.Should().Be("head");
    }

    [Fact]
    public void OpenTypeTableTag_CmapDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)OpenTypeTableTag.CharToIndexMap);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("cmap");
    }

    [Fact]
    public void OpenTypeTableTag_GsubDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)OpenTypeTableTag.TTO_GSUB);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("GSUB");
    }

    [Fact]
    public void OpenTypeTableTag_NameDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)OpenTypeTableTag.NamingTable);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("name");
    }
}

/// <summary>
/// Tests for the DWriteFontFeatureTag enum.
/// </summary>
public class DWriteFontFeatureTagTests
{
    [Fact]
    public void DWriteFontFeatureTag_HasCommonFeatures()
    {
        // Common OpenType features
        DWriteFontFeatureTag.Kerning.Should().BeDefined();           // kern
        DWriteFontFeatureTag.StandardLigatures.Should().BeDefined(); // liga
        DWriteFontFeatureTag.SmallCapitals.Should().BeDefined();     // smcp
        DWriteFontFeatureTag.Fractions.Should().BeDefined();         // frac
        DWriteFontFeatureTag.SlashedZero.Should().BeDefined();       // zero
    }

    [Fact]
    public void DWriteFontFeatureTag_HasStylisticSets()
    {
        // Stylistic sets ss01-ss20
        DWriteFontFeatureTag.StylisticSet1.Should().BeDefined();
        DWriteFontFeatureTag.StylisticSet10.Should().BeDefined();
        DWriteFontFeatureTag.StylisticSet20.Should().BeDefined();
    }

    [Fact]
    public void DWriteFontFeatureTag_HasPositioningFeatures()
    {
        DWriteFontFeatureTag.Superscript.Should().BeDefined();  // sups
        DWriteFontFeatureTag.Subscript.Should().BeDefined();    // subs
        DWriteFontFeatureTag.Ordinals.Should().BeDefined();     // ordn
    }

    [Fact]
    public void DWriteFontFeatureTag_KerningDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)DWriteFontFeatureTag.Kerning);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("kern");
    }

    [Fact]
    public void DWriteFontFeatureTag_LigaDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)DWriteFontFeatureTag.StandardLigatures);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("liga");
    }

    [Fact]
    public void DWriteFontFeatureTag_SmcpDecodesToCorrectString()
    {
        var tagBytes = BitConverter.GetBytes((uint)DWriteFontFeatureTag.SmallCapitals);
        var tagString = System.Text.Encoding.ASCII.GetString(tagBytes);
        tagString.Should().Be("smcp");
    }
}

/// <summary>
/// Tests for the GlyphOffset struct.
/// </summary>
public class GlyphOffsetTests
{
    [Fact]
    public void GlyphOffset_DefaultValues_AreZero()
    {
        var offset = new GlyphOffset();
        
        offset.du.Should().Be(0);
        offset.dv.Should().Be(0);
    }

    [Fact]
    public void GlyphOffset_CanSetValues()
    {
        var offset = new GlyphOffset
        {
            du = 10,
            dv = -5
        };
        
        offset.du.Should().Be(10);
        offset.dv.Should().Be(-5);
    }
}

/// <summary>
/// Tests for the DWriteFontFeature struct.
/// </summary>
public class DWriteFontFeatureTests
{
    [Fact]
    public void DWriteFontFeature_Constructor_SetsProperties()
    {
        var feature = new DWriteFontFeature(DWriteFontFeatureTag.Kerning, 1);
        
        feature.nameTag.Should().Be(DWriteFontFeatureTag.Kerning);
        feature.parameter.Should().Be(1);
    }

    [Fact]
    public void DWriteFontFeature_DisabledFeature_HasZeroParameter()
    {
        var feature = new DWriteFontFeature(DWriteFontFeatureTag.StandardLigatures, 0);
        
        feature.parameter.Should().Be(0);
    }

    [Fact]
    public void DWriteFontFeature_EnabledFeature_HasNonZeroParameter()
    {
        var feature = new DWriteFontFeature(DWriteFontFeatureTag.SmallCapitals, 1);
        
        feature.parameter.Should().BeGreaterThan(0);
    }
}
