// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="DWriteTypeConverter"/> internal class.
/// These tests exercise the type conversion methods indirectly through public APIs.
/// Note: Enum types are internal, so we use integer values and cast internally.
/// </summary>
public class DWriteTypeConverterTests
{
    #region FactoryType conversion

    [Fact]
    public void Convert_FactoryType_ShouldConvertWithoutException()
    {
        // FactoryType.Shared (0) is used by DWriteFactory.Instance
        var factory = DWriteFactory.Instance;
        factory.Should().NotBeNull();
    }

    #endregion

    #region FontWeight conversion

    [Theory]
    [InlineData(100)]  // Thin
    [InlineData(200)]  // ExtraLight
    [InlineData(300)]  // Light
    [InlineData(400)]  // Normal
    [InlineData(500)]  // Medium
    [InlineData(600)]  // SemiBold
    [InlineData(700)]  // Bold
    [InlineData(800)]  // ExtraBold
    [InlineData(900)]  // Black
    [InlineData(950)]  // ExtraBlack
    public void Convert_FontWeight_ToNative_ShouldSucceed(int weightValue)
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        var weight = (FontWeight)weightValue;
        var matchingFonts = arialFamily.GetMatchingFonts(weight, FontStretch.Normal, FontStyle.Normal);
        matchingFonts.Should().NotBeNull();
    }

    [Fact]
    public void Convert_FontWeight_FromNative_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        foreach (var font in arialFamily)
        {
            var weight = font.Weight;
            weight.Should().BeDefined();
        }
    }

    [Theory]
    [InlineData(350)] // Non-standard weight
    [InlineData(450)]
    [InlineData(550)]
    [InlineData(650)]
    [InlineData(750)]
    [InlineData(850)]
    public void Convert_FontWeight_CustomValues_ShouldSucceed(int weightValue)
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        var weight = (FontWeight)weightValue;
        var matchingFonts = arialFamily.GetMatchingFonts(weight, FontStretch.Normal, FontStyle.Normal);
        matchingFonts.Should().NotBeNull();
    }

    #endregion

    #region FontStretch conversion

    [Theory]
    [InlineData(1)]  // UltraCondensed
    [InlineData(2)]  // ExtraCondensed
    [InlineData(3)]  // Condensed
    [InlineData(4)]  // SemiCondensed
    [InlineData(5)]  // Normal
    [InlineData(6)]  // SemiExpanded
    [InlineData(7)]  // Expanded
    [InlineData(8)]  // ExtraExpanded
    [InlineData(9)]  // UltraExpanded
    public void Convert_FontStretch_ToNative_ShouldSucceed(int stretchValue)
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        var stretch = (FontStretch)stretchValue;
        var matchingFonts = arialFamily.GetMatchingFonts(FontWeight.Normal, stretch, FontStyle.Normal);
        matchingFonts.Should().NotBeNull();
    }

    [Fact]
    public void Convert_FontStretch_FromNative_ShouldSucceed()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        string[] familyNames = ["Arial", "Arial Narrow", "Verdana"];

        foreach (var familyName in familyNames)
        {
            var family = fontCollection[familyName];
            if (family == null) continue;

            foreach (var font in family)
            {
                var stretch = font.Stretch;
                stretch.Should().BeDefined();
            }
        }
    }

    #endregion

    #region FontStyle conversion

    [Theory]
    [InlineData(0)]  // Normal
    [InlineData(1)]  // Oblique
    [InlineData(2)]  // Italic
    public void Convert_FontStyle_ToNative_ShouldSucceed(int styleValue)
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        var style = (FontStyle)styleValue;
        var matchingFonts = arialFamily.GetMatchingFonts(FontWeight.Normal, FontStretch.Normal, style);
        matchingFonts.Should().NotBeNull();
    }

    [Fact]
    public void Convert_FontStyle_FromNative_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        foreach (var font in arialFamily)
        {
            var style = font.Style;
            style.Should().BeDefined();
        }
    }

    #endregion

    #region FontSimulations conversion

    [Theory]
    [InlineData(0)]  // None
    [InlineData(1)]  // Bold
    [InlineData(2)]  // Oblique
    [InlineData(3)]  // Bold | Oblique
    public void Convert_FontSimulations_ToNative_ShouldSucceed(int simulationsValue)
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var simulations = (FontSimulations)simulationsValue;
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, simulations);
        try
        {
            fontFace.Should().NotBeNull();
            fontFace.SimulationFlags.Should().Be(simulations);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Convert_FontSimulations_FromNative_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];
        // SimulationFlags calls DWriteTypeConverter.Convert(DWRITE_FONT_SIMULATIONS)
        font.SimulationFlags.Should().BeDefined();
    }

    #endregion

    #region FontFaceType conversion

    [Fact]
    public void Convert_FontFaceType_TrueType_FromNative_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            fontFace.Type.Should().Be(FontFaceType.TrueType);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Convert_FontFaceType_TrueTypeCollection_FromNative_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfFontNotAvailable(TestHelpers.CambriaPath, "Cambria");
        var cambriaPath = TestHelpers.CambriaPath;

        var fontFace = factory.CreateFontFace(new Uri(cambriaPath), 0);
        try
        {
            fontFace.Type.Should().Be(FontFaceType.TrueTypeCollection);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void Convert_FontFaceType_VariousFonts_FromNative_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        var fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[] fontFiles = ["arial.ttf", "arialbd.ttf", "times.ttf", "cour.ttf", "verdana.ttf"];

        foreach (var fontFile in fontFiles)
        {
            var fontPath = Path.Combine(fontsPath, fontFile);
            if (!File.Exists(fontPath)) continue;

            var fontFace = factory.CreateFontFace(new Uri(fontPath), 0);
            try
            {
                fontFace.Type.Should().BeDefined();
            }
            finally
            {
                fontFace.Release();
            }
        }
    }

    #endregion

    #region InformationalStringID conversion

    [Theory]
    [InlineData(0)]   // None
    [InlineData(1)]   // CopyrightNotice
    [InlineData(2)]   // VersionStrings
    [InlineData(3)]   // Trademark
    [InlineData(4)]   // Manufacturer
    [InlineData(5)]   // Designer
    [InlineData(6)]   // DesignerURL
    [InlineData(7)]   // Description
    [InlineData(8)]   // FontVendorURL
    [InlineData(9)]   // LicenseDescription
    [InlineData(10)]  // LicenseInfoURL
    [InlineData(11)]  // WIN32FamilyNames
    [InlineData(12)]  // Win32SubFamilyNames
    [InlineData(13)]  // PreferredFamilyNames
    [InlineData(14)]  // PreferredSubFamilyNames
    [InlineData(15)]  // SampleText
    public void Convert_InformationalStringID_ToNative_ShouldSucceed(int stringIdValue)
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];
        var stringId = (InformationalStringID)stringIdValue;

        // GetInformationalStrings calls DWriteTypeConverter.Convert(InformationalStringID)
        _ = font.GetInformationalStrings(stringId, out _);
    }

    #endregion

    #region TextFormattingMode / MeasuringMode conversion

    [Fact]
    public unsafe void Convert_TextFormattingMode_Ideal_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];
        var fontFace = font.GetFontFace();
        try
        {
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }

            // useDisplayNatural: false = Ideal mode
            GlyphMetrics[] metrics = new GlyphMetrics[1];
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDisplayGlyphMetrics(pGlyphIndices, 1, pMetrics,
                    12.0f, useDisplayNatural: false, isSideways: false, pixelsPerDip: 1.0f);
            }

            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public unsafe void Convert_TextFormattingMode_Display_ShouldSucceed()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            uint[] codePoints = [65]; // 'A'
            ushort[] glyphIndices = new ushort[1];

            fixed (uint* pCodePoints = codePoints)
            fixed (ushort* pGlyphIndices = glyphIndices)
            {
                fontFace.GetArrayOfGlyphIndices(pCodePoints, 1, pGlyphIndices);
            }

            // useDisplayNatural: true = Display mode
            GlyphMetrics[] metrics = new GlyphMetrics[1];
            fixed (ushort* pGlyphIndices = glyphIndices)
            fixed (GlyphMetrics* pMetrics = metrics)
            {
                fontFace.GetDisplayGlyphMetrics(pGlyphIndices, 1, pMetrics,
                    12.0f, useDisplayNatural: true, isSideways: false, pixelsPerDip: 1.0f);
            }

            metrics[0].AdvanceWidth.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace.Release();
        }
    }

    #endregion

    #region FontMetrics conversion

    [Fact]
    public void Convert_FontMetrics_FromNative_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];
        var metrics = font.Metrics;

        metrics.Should().NotBeNull();
        metrics.Ascent.Should().BeGreaterThan(0);
        metrics.Descent.Should().BeGreaterThan(0);
        metrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Convert_FontMetrics_DisplayMetrics_FromNative_ShouldSucceed()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];
        var displayMetrics = font.DisplayMetrics(12.0f, 96.0f);

        displayMetrics.Should().NotBeNull();
        displayMetrics.Ascent.Should().BeGreaterThan(0);
    }

    #endregion

    #region DWriteMatrix struct

    [Fact]
    public void DWriteMatrix_DefaultValues_ShouldBeValid()
    {
        var matrix = new DWriteMatrix();

        matrix.M11.Should().Be(0);
        matrix.M12.Should().Be(0);
        matrix.M21.Should().Be(0);
        matrix.M22.Should().Be(0);
        matrix.Dx.Should().Be(0);
        matrix.Dy.Should().Be(0);
    }

    [Fact]
    public void DWriteMatrix_IdentityMatrix_ShouldBeValid()
    {
        var matrix = new DWriteMatrix
        {
            M11 = 1.0f,
            M12 = 0.0f,
            M21 = 0.0f,
            M22 = 1.0f,
            Dx = 0.0f,
            Dy = 0.0f
        };

        matrix.M11.Should().Be(1.0f);
        matrix.M22.Should().Be(1.0f);
    }

    #endregion

    #region Direct DWriteTypeConverter method calls
    
    // The following tests call DWriteTypeConverter methods directly
    // since it's a private ref class (internal in C#) with internal methods
    
    // DWRITE_FACTORY_TYPE enum values (from dwrite.h)
    private const int DWRITE_FACTORY_TYPE_SHARED = 0;
    private const int DWRITE_FACTORY_TYPE_ISOLATED = 1;

    [Theory]
    [InlineData(0, DWRITE_FACTORY_TYPE_SHARED)]   // Shared
    [InlineData(1, DWRITE_FACTORY_TYPE_ISOLATED)] // Isolated
    public void DWriteTypeConverter_Convert_FactoryType_ReturnsExpectedValue(int factoryTypeValue, int expectedNativeValue)
    {
        var factoryType = (FactoryType)factoryTypeValue;
        int result = (int)DWriteTypeConverter.Convert(factoryType);
        result.Should().Be(expectedNativeValue,
            $"FactoryType.{factoryType} should convert to DWRITE_FACTORY_TYPE value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_FactoryType_Invalid_ShouldThrow()
    {
        var invalidFactoryType = (FactoryType)99;
        var act = () => DWriteTypeConverter.Convert(invalidFactoryType);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_FONT_WEIGHT enum values match the numeric weight values (100-950)
    [Theory]
    [InlineData(100, 100)]   // Thin
    [InlineData(200, 200)]   // ExtraLight
    [InlineData(300, 300)]   // Light
    [InlineData(350, 350)]   // SemiLight - passes through as-is
    [InlineData(400, 400)]   // Normal
    [InlineData(500, 500)]   // Medium
    [InlineData(600, 600)]   // SemiBold
    [InlineData(700, 700)]   // Bold
    [InlineData(800, 800)]   // ExtraBold
    [InlineData(900, 900)]   // Black
    [InlineData(950, 950)]   // ExtraBlack
    [InlineData(1, 1)]       // Min valid custom weight
    [InlineData(999, 999)]   // Max valid custom weight
    [InlineData(550, 550)]   // Custom weight in middle
    public void DWriteTypeConverter_Convert_FontWeight_PreservesNumericValue(int weightValue, int expectedNativeValue)
    {
        var weight = (FontWeight)weightValue;
        int result = (int)DWriteTypeConverter.Convert(weight);
        result.Should().Be(expectedNativeValue,
            $"FontWeight({weightValue}) should convert to DWRITE_FONT_WEIGHT value {expectedNativeValue}");
    }
    
    [Theory]
    [InlineData(0)]     // Invalid - too low
    [InlineData(1000)]  // Invalid - too high
    [InlineData(-1)]    // Invalid - negative
    public void DWriteTypeConverter_Convert_FontWeight_Invalid_ShouldThrow(int weightValue)
    {
        var weight = (FontWeight)weightValue;
        var act = () => DWriteTypeConverter.Convert(weight);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_FONT_STRETCH enum values (0-9)
    [Theory]
    [InlineData(0, 0)]  // Undefined
    [InlineData(1, 1)]  // UltraCondensed
    [InlineData(2, 2)]  // ExtraCondensed
    [InlineData(3, 3)]  // Condensed
    [InlineData(4, 4)]  // SemiCondensed
    [InlineData(5, 5)]  // Normal
    [InlineData(6, 6)]  // SemiExpanded
    [InlineData(7, 7)]  // Expanded
    [InlineData(8, 8)]  // ExtraExpanded
    [InlineData(9, 9)]  // UltraExpanded
    public void DWriteTypeConverter_Convert_FontStretch_PreservesNumericValue(int stretchValue, int expectedNativeValue)
    {
        var stretch = (FontStretch)stretchValue;
        int result = (int)DWriteTypeConverter.Convert(stretch);
        result.Should().Be(expectedNativeValue,
            $"FontStretch({stretchValue}) should convert to DWRITE_FONT_STRETCH value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_FontStretch_Invalid_ShouldThrow()
    {
        var invalidStretch = (FontStretch)99;
        var act = () => DWriteTypeConverter.Convert(invalidStretch);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_FONT_STYLE enum values (0-2)
    [Theory]
    [InlineData(0, 0)]  // Normal
    [InlineData(1, 1)]  // Oblique
    [InlineData(2, 2)]  // Italic
    public void DWriteTypeConverter_Convert_FontStyle_PreservesNumericValue(int styleValue, int expectedNativeValue)
    {
        var style = (FontStyle)styleValue;
        int result = (int)DWriteTypeConverter.Convert(style);
        result.Should().Be(expectedNativeValue,
            $"FontStyle({styleValue}) should convert to DWRITE_FONT_STYLE value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_FontStyle_Invalid_ShouldThrow()
    {
        var invalidStyle = (FontStyle)99;
        var act = () => DWriteTypeConverter.Convert(invalidStyle);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_FONT_SIMULATIONS enum values (0-3)
    [Theory]
    [InlineData(0, 0)]  // None
    [InlineData(1, 1)]  // Bold
    [InlineData(2, 2)]  // Oblique
    [InlineData(3, 3)]  // Bold | Oblique
    public void DWriteTypeConverter_Convert_FontSimulations_PreservesNumericValue(int simValue, int expectedNativeValue)
    {
        var simulations = (FontSimulations)simValue;
        int result = (int)DWriteTypeConverter.Convert(simulations);
        result.Should().Be(expectedNativeValue,
            $"FontSimulations({simValue}) should convert to DWRITE_FONT_SIMULATIONS value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_FontSimulations_Invalid_ShouldThrow()
    {
        var invalidSim = (FontSimulations)99;
        var act = () => DWriteTypeConverter.Convert(invalidSim);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_FONT_FACE_TYPE enum values - mapping from WPF FontFaceType to native DWRITE values
    // WPF: CFF=0, TrueType=1, TrueTypeCollection=2, Type1=3, Vector=4, Bitmap=5, Unknown=6
    // Native: Same ordering as WPF (they match)
    [Theory]
    [InlineData(0, 0)]  // CFF -> DWRITE_FONT_FACE_TYPE_CFF
    [InlineData(1, 1)]  // TrueType -> DWRITE_FONT_FACE_TYPE_TRUETYPE
    [InlineData(2, 2)]  // TrueTypeCollection -> DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION
    [InlineData(3, 3)]  // Type1 -> DWRITE_FONT_FACE_TYPE_TYPE1
    [InlineData(4, 4)]  // Vector -> DWRITE_FONT_FACE_TYPE_VECTOR
    [InlineData(5, 5)]  // Bitmap -> DWRITE_FONT_FACE_TYPE_BITMAP
    [InlineData(6, 6)]  // Unknown -> DWRITE_FONT_FACE_TYPE_UNKNOWN
    public void DWriteTypeConverter_Convert_FontFaceType_MapsToCorrectNativeValue(int faceTypeValue, int expectedNativeValue)
    {
        var faceType = (FontFaceType)faceTypeValue;
        int result = (int)DWriteTypeConverter.Convert(faceType);
        result.Should().Be(expectedNativeValue,
            $"FontFaceType({faceTypeValue}) should convert to DWRITE_FONT_FACE_TYPE value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_FontFaceType_Invalid_ShouldThrow()
    {
        var invalidFaceType = (FontFaceType)99;
        var act = () => DWriteTypeConverter.Convert(invalidFaceType);
        act.Should().Throw<InvalidOperationException>();
    }

    // DWRITE_INFORMATIONAL_STRING_ID enum values (0-15)
    [Theory]
    [InlineData(0, 0)]    // None
    [InlineData(1, 1)]    // CopyrightNotice
    [InlineData(2, 2)]    // VersionStrings
    [InlineData(3, 3)]    // Trademark
    [InlineData(4, 4)]    // Manufacturer
    [InlineData(5, 5)]    // Designer
    [InlineData(6, 6)]    // DesignerURL
    [InlineData(7, 7)]    // Description
    [InlineData(8, 8)]    // FontVendorURL
    [InlineData(9, 9)]    // LicenseDescription
    [InlineData(10, 10)]  // LicenseInfoURL
    [InlineData(11, 11)]  // WIN32FamilyNames
    [InlineData(12, 12)]  // Win32SubFamilyNames
    [InlineData(13, 13)]  // PreferredFamilyNames
    [InlineData(14, 14)]  // PreferredSubFamilyNames
    [InlineData(15, 15)]  // SampleText
    public void DWriteTypeConverter_Convert_InformationalStringID_PreservesNumericValue(int stringIdValue, int expectedNativeValue)
    {
        var stringId = (InformationalStringID)stringIdValue;
        int result = (int)DWriteTypeConverter.Convert(stringId);
        result.Should().Be(expectedNativeValue,
            $"InformationalStringID({stringIdValue}) should convert to DWRITE_INFORMATIONAL_STRING_ID value {expectedNativeValue}");
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_InformationalStringID_Invalid_ShouldThrow()
    {
        var invalidStringId = (InformationalStringID)99;
        var act = () => DWriteTypeConverter.Convert(invalidStringId);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DWriteTypeConverter_Convert_FontMetrics_ToNative()
    {
        // Create a FontMetrics object and convert to native
        var fontMetrics = new FontMetrics
        {
            Ascent = 1000,
            Descent = 200,
            LineGap = 50,
            CapHeight = 700,
            XHeight = 500,
            UnderlinePosition = -100,
            UnderlineThickness = 50,
            StrikethroughPosition = 300,
            StrikethroughThickness = 50,
            DesignUnitsPerEm = 2048
        };
        
        _ = DWriteTypeConverter.Convert(fontMetrics);
    }

    [Fact]
    public void DWriteTypeConverter_Convert_DWriteMatrix_ToNative()
    {
        // Create a DWriteMatrix and convert to native
        var matrix = new DWriteMatrix
        {
            M11 = 1.0f,
            M12 = 0.0f,
            M21 = 0.0f,
            M22 = 1.0f,
            Dx = 10.0f,
            Dy = 20.0f
        };
        
        _ = DWriteTypeConverter.Convert(matrix);
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_DWriteMatrix_WithRotation_ToNative()
    {
        // Test with a rotation matrix
        var matrix = new DWriteMatrix
        {
            M11 = 0.707f,  // cos(45)
            M12 = 0.707f,  // sin(45)
            M21 = -0.707f, // -sin(45)
            M22 = 0.707f,  // cos(45)
            Dx = 0.0f,
            Dy = 0.0f
        };
        
        _ = DWriteTypeConverter.Convert(matrix);
    }

    [Theory]
    [InlineData(0)]  // Ideal
    [InlineData(1)]  // Display
    public void DWriteTypeConverter_Convert_TextFormattingMode_ToNative(int modeValue)
    {
        var mode = (System.Windows.Media.TextFormattingMode)modeValue;
        _ = DWriteTypeConverter.Convert(mode);
    }
    
    [Fact]
    public void DWriteTypeConverter_Convert_TextFormattingMode_Invalid_ShouldThrow()
    {
        var invalidMode = (System.Windows.Media.TextFormattingMode)99;
        var act = () => DWriteTypeConverter.Convert(invalidMode);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Native to Managed conversions via Font properties
    
    // These tests exercise the DWRITE_* to managed conversions by accessing
    // font properties that internally call DWriteTypeConverter.Convert(DWRITE_*)
    
    [Fact]
    public void Convert_FontWeight_FromNative_ViaAllSystemFonts()
    {
        // Iterate ALL system fonts to maximize coverage of different DWRITE_FONT_WEIGHT values
        var fontCollection = DWriteFactory.SystemFontCollection;
        var weightsFound = new HashSet<int>();
        
        // Iterate all families (not just first 50) to maximize coverage
        for (uint i = 0; i < fontCollection.FamilyCount; i++)
        {
            var family = fontCollection[i];
            foreach (var font in family)
            {
                var weight = font.Weight;
                weightsFound.Add((int)weight);
            }
        }
        
        // We should find Normal (400) and Bold (700) in system fonts
        weightsFound.Should().Contain(400, "System should have Normal weight fonts");
        weightsFound.Should().Contain(700, "System should have Bold weight fonts");
        
        // Additional weights typically found on Windows: Light (300), Medium (500), SemiBold (600)
        // We don't assert these as they depend on installed fonts, but we exercise all available values
    }
    
    [Fact]
    public void Convert_FontStretch_FromNative_ViaAllSystemFonts()
    {
        // Iterate ALL system fonts to maximize coverage of different DWRITE_FONT_STRETCH values
        var fontCollection = DWriteFactory.SystemFontCollection;
        var stretchesFound = new HashSet<int>();
        
        // Iterate all families (not just first 50) to maximize coverage
        for (uint i = 0; i < fontCollection.FamilyCount; i++)
        {
            var family = fontCollection[i];
            foreach (var font in family)
            {
                var stretch = font.Stretch;
                stretchesFound.Add((int)stretch);
            }
        }
        
        // We should find Normal (5) in system fonts
        stretchesFound.Should().Contain(5, "System should have Normal stretch fonts");
        
        // Other stretches (Condensed=3, SemiCondensed=4, SemiExpanded=6, etc.) depend on installed fonts
    }
    
    [Fact]
    public void Convert_FontStyle_FromNative_ViaAllSystemFonts()
    {
        // Iterate ALL system fonts to maximize coverage of different DWRITE_FONT_STYLE values
        var fontCollection = DWriteFactory.SystemFontCollection;
        var stylesFound = new HashSet<int>();
        
        // Iterate all families (not just first 50) to maximize coverage
        for (uint i = 0; i < fontCollection.FamilyCount; i++)
        {
            var family = fontCollection[i];
            foreach (var font in family)
            {
                var style = font.Style;
                stylesFound.Add((int)style);
            }
        }
        
        // We should find Normal (0) and Italic (2) in system fonts
        stylesFound.Should().Contain(0, "System should have Normal style fonts");
        stylesFound.Should().Contain(2, "System should have Italic style fonts");
        
        // Oblique (1) is rare in system fonts but may be found depending on installed fonts
    }
    
    [Fact]
    public void Convert_FontSimulations_FromNative_ViaFontFace()
    {
        // Test different font simulations via FontFace
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        FontSimulations[] simulations = [FontSimulations.None, FontSimulations.Bold, FontSimulations.Oblique, FontSimulations.Bold | FontSimulations.Oblique];
        
        foreach (var sim in simulations)
        {
            var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, sim);
            try
            {
                // This calls Convert(DWRITE_FONT_SIMULATIONS)
                var resultSim = fontFace.SimulationFlags;
                resultSim.Should().Be(sim);
            }
            finally
            {
                fontFace.Release();
            }
        }
    }
    
    [Fact]
    public void Convert_FontFaceType_FromNative_ViaVariousFonts()
    {
        // Test different font face types
        var factory = DWriteFactory.Instance;
        var fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        var typesFound = new HashSet<FontFaceType>();
        
        // TTF files (TrueType)
        string[] ttfFiles = ["arial.ttf", "times.ttf", "verdana.ttf", "cour.ttf"];
        foreach (var file in ttfFiles)
        {
            var path = Path.Combine(fontsPath, file);
            if (!File.Exists(path)) continue;
            
            var fontFace = factory.CreateFontFace(new Uri(path), 0);
            try
            {
                // This calls Convert(DWRITE_FONT_FACE_TYPE)
                var faceType = fontFace.Type;
                faceType.Should().Be(FontFaceType.TrueType);
                typesFound.Add(faceType);
            }
            finally
            {
                fontFace.Release();
            }
        }
        
        // TTC file (TrueTypeCollection)
        var ttcPath = Path.Combine(fontsPath, "cambria.ttc");
        if (File.Exists(ttcPath))
        {
            var fontFace = factory.CreateFontFace(new Uri(ttcPath), 0);
            try
            {
                var faceType = fontFace.Type;
                faceType.Should().Be(FontFaceType.TrueTypeCollection);
                typesFound.Add(faceType);
            }
            finally
            {
                fontFace.Release();
            }
        }
        
        // OTF files (CFF - OpenType with PostScript outlines)
        string[] otfFiles = ["calibri.ttf", "consola.ttf", "segoeui.ttf"];
        foreach (var file in otfFiles)
        {
            var path = Path.Combine(fontsPath, file);
            if (!File.Exists(path)) continue;
            
            var fontFace = factory.CreateFontFace(new Uri(path), 0);
            try
            {
                // Most Windows fonts are TrueType; CFF fonts are less common
                var faceType = fontFace.Type;
                typesFound.Add(faceType);
            }
            finally
            {
                fontFace.Release();
            }
        }
        
        // We should have found at least TrueType
        typesFound.Should().Contain(FontFaceType.TrueType, "Should find at least TrueType fonts");
    }
    
    [Fact]
    public void Convert_FontMetrics_FromNative_ViaAllSystemFonts()
    {
        // Access FontMetrics from ALL system fonts to cover Convert(DWRITE_FONT_METRICS)
        var fontCollection = DWriteFactory.SystemFontCollection;
        int fontsChecked = 0;
        
        for (uint i = 0; i < fontCollection.FamilyCount; i++)
        {
            var family = fontCollection[i];
            if (family.Count == 0) continue;
            
            var font = family[0u];
            // This calls Convert(DWRITE_FONT_METRICS)
            var metrics = font.Metrics;
            
            metrics.Should().NotBeNull();
            metrics.DesignUnitsPerEm.Should().BeGreaterThan(0);
            metrics.Ascent.Should().BeGreaterThan(0);
            
            fontsChecked++;
        }
        
        fontsChecked.Should().BeGreaterThan(0, "Should have checked at least one font");
    }
    
    #endregion
}
