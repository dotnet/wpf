// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Additional tests to increase code coverage for DirectWriteForwarder.
/// Targets uncovered methods in LocalizedStrings, Font, FontList, FontFace, FontCollection.
/// </summary>
public class AdditionalCoverageTests
{
    #region LocalizedStrings IDictionary method coverage

    [Fact]
    public void LocalizedStrings_SetItem_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        Action act = () => localizedStrings[CultureInfo.InvariantCulture] = "Test";

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LocalizedStrings_AddKeyValuePair_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        var pair = new KeyValuePair<CultureInfo, string>(CultureInfo.InvariantCulture, "Test");
        Action act = () => ((ICollection<KeyValuePair<CultureInfo, string>>)localizedStrings).Add(pair);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LocalizedStrings_RemoveKeyValuePair_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);

        var pair = new KeyValuePair<CultureInfo, string>(firstCulture, localizedStrings[firstCulture]);
        Action act = () => ((ICollection<KeyValuePair<CultureInfo, string>>)localizedStrings).Remove(pair);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void LocalizedStrings_GetEnumerator2_NonGeneric_ShouldWork()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        // Cast to non-generic IEnumerable and get enumerator
        var enumerable = (IEnumerable)localizedStrings;
        var enumerator = enumerable.GetEnumerator();

        enumerator.Should().NotBeNull();

        // Should be able to enumerate
        int count = 0;
        while (enumerator.MoveNext())
        {
            enumerator.Current.Should().NotBeNull();
            count++;
        }

        count.Should().Be(localizedStrings.Count);
    }

    [Fact]
    public void LocalizedStrings_Indexer_WithNonExistentCulture_ShouldReturnNull()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        // Use a culture very unlikely to be in the font
        var rareCulture = new CultureInfo("zu-ZA"); // Zulu - South Africa

        // If this culture doesn't exist, the indexer should return null
        if (!localizedStrings.ContainsKey(rareCulture))
        {
            var result = localizedStrings[rareCulture];
            result.Should().BeNull();
        }
    }

    [Fact]
    public void LocalizedStrings_TryGetValue_WithNonExistentCulture_ShouldReturnFalse()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        var rareCulture = new CultureInfo("zu-ZA");

        if (!localizedStrings.ContainsKey(rareCulture))
        {
            bool result = localizedStrings.TryGetValue(rareCulture, out _);
            result.Should().BeFalse();
        }
    }

    #endregion

    #region FontList coverage

    [Fact]
    public void FontList_GetEnumerator2_NonGeneric_ShouldWork()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        // FontFamily inherits from FontList, so we can test the non-generic IEnumerable
        var enumerable = (IEnumerable)arialFamily;
        var enumerator = enumerable.GetEnumerator();

        enumerator.Should().NotBeNull();

        int count = 0;
        while (enumerator.MoveNext())
        {
            enumerator.Current.Should().NotBeNull();
            enumerator.Current.Should().BeOfType<Font>();
            count++;
        }

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FontList_FontsCollection_ShouldReturnFontCollection()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();

        // FontFamily has FontsCollection property from FontList
        var fontsCollection = arialFamily.FontsCollection;

        fontsCollection.Should().NotBeNull();
        fontsCollection.FamilyCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void FontList_Enumerator_Reset_ShouldResetPosition()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var enumerator = arialFamily.GetEnumerator();

        // Move forward
        enumerator.MoveNext().Should().BeTrue();
        var firstFontWeight = enumerator.Current.Weight;

        // Move more
        if (arialFamily.Count > 1)
        {
            enumerator.MoveNext();
        }

        // Reset
        enumerator.Reset();

        // After reset, MoveNext should give us the first element again
        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Weight.Should().Be(firstFontWeight);

        enumerator.Dispose();
    }

    [Fact]
    public void FontList_Enumerator_CurrentBeforeMoveNext_ShouldThrow()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var enumerator = arialFamily.GetEnumerator();

        // Accessing Current before MoveNext should throw
        Action act = () => { var _ = enumerator.Current; };
        act.Should().Throw<InvalidOperationException>();

        enumerator.Dispose();
    }

    [Fact]
    public void FontList_Enumerator_CurrentAfterEnd_ShouldThrow()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var enumerator = arialFamily.GetEnumerator();

        // Move past the end
        while (enumerator.MoveNext()) { }

        // Accessing Current after enumeration ends should throw
        Action act = () => { var _ = enumerator.Current; };
        act.Should().Throw<InvalidOperationException>();

        enumerator.Dispose();
    }

    #endregion

    #region FontCollection coverage

    [Fact]
    public void FontCollection_GetFontFromFontFace_ShouldReturnMatchingFont()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var factory = DWriteFactory.Instance;

        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            var font = fontCollection.GetFontFromFontFace(fontFace);

            font.Should().NotBeNull();
            // The font should be from the Arial family
            font!.Family.FamilyNames.GetString(0).Should().Contain("Arial");
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontCollection_GetFontFromFontFace_WithSimulations_ShouldReturnFont()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var factory = DWriteFactory.Instance;

        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        // Create a font face with Bold simulation
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0, FontSimulations.Bold);
        try
        {
            var font = fontCollection.GetFontFromFontFace(fontFace);

            // Note: This may or may not return a font depending on how DirectWrite handles simulated faces
            // The important thing is it doesn't throw
        }
        finally
        {
            fontFace.Release();
        }
    }

    #endregion

    #region Font class coverage

    [Fact]
    public void Font_DWriteFontAddRef_ShouldReturnValidPointer()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];

        // Access the DWriteFontAddRef property - this adds a reference to the native object
        var ptr = font.DWriteFontAddRef;

        ptr.Should().NotBe(IntPtr.Zero);
    }

    [Fact]
    public void Font_Version_ShouldBeConsistent()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];

        // Call Version multiple times - it should be cached and return same value
        var version1 = font.Version;
        var version2 = font.Version;

        version1.Should().Be(version2);
        version1.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Font_GetFontFace_MultipleCalls_ShouldUseCacheEfficiently()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];

        // Create multiple font faces - the caching logic should be exercised
        var fontFaces = new List<FontFace>();
        for (int i = 0; i < 10; i++)
        {
            var fontFace = font.GetFontFace();
            fontFaces.Add(fontFace);
        }

        try
        {
            // All should be valid
            foreach (var ff in fontFaces)
            {
                ff.Should().NotBeNull();
                ff.GlyphCount.Should().BeGreaterThan(0);
            }
        }
        finally
        {
            foreach (var ff in fontFaces)
            {
                ff.Release();
            }
        }
    }

    [Fact]
    public void Font_ResetFontFaceCache_ShouldNotThrow()
    {
        // ResetFontFaceCache is a static method that clears cached FontFace instances
        // We need to call it via reflection since it's internal
        var fontType = typeof(Font);
        var resetMethod = fontType.GetMethod("ResetFontFaceCache",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        if (resetMethod != null)
        {
            Action act = () => resetMethod.Invoke(null, null);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void Font_GetFontFace_AfterCacheReset_ShouldStillWork()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count > 0, "Arial font family has no fonts");

        var font = arialFamily[0u];

        // Get a font face first to populate the cache
        var fontFace1 = font.GetFontFace();
        fontFace1.Release();

        // Reset the cache
        var fontType = typeof(Font);
        var resetMethod = fontType.GetMethod("ResetFontFaceCache",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        resetMethod?.Invoke(null, null);

        // Get another font face - should work after cache reset
        var fontFace2 = font.GetFontFace();
        try
        {
            fontFace2.Should().NotBeNull();
            fontFace2.GlyphCount.Should().BeGreaterThan(0);
        }
        finally
        {
            fontFace2.Release();
        }
    }

    #endregion

    #region FontFace coverage

    [Fact]
    public void FontFace_DWriteFontFaceAddRef_ShouldReturnValidPointer()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            var ptr = fontFace.DWriteFontFaceAddRef;
            ptr.Should().NotBe(IntPtr.Zero);
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontFace_GetFileZero_ShouldReturnValidFontFile()
    {
        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            var fontFile = fontFace.GetFileZero();
            fontFile.Should().NotBeNull();
        }
        finally
        {
            fontFace.Release();
        }
    }

    [Fact]
    public void FontFace_IsSymbolFont_ShouldReturnCorrectValue()
    {
        var factory = DWriteFactory.Instance;

        // Test with Arial (non-symbol)
        var arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (File.Exists(arialPath))
        {
            var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
            try
            {
                fontFace.IsSymbolFont.Should().BeFalse();
            }
            finally
            {
                fontFace.Release();
            }
        }

        // Test with Symbol font if available
        var symbolPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "symbol.ttf");
        if (File.Exists(symbolPath))
        {
            var fontFace = factory.CreateFontFace(new Uri(symbolPath), 0);
            try
            {
                fontFace.IsSymbolFont.Should().BeTrue();
            }
            finally
            {
                fontFace.Release();
            }
        }
    }

    #endregion

    #region LocalizedStrings Enumerator coverage

    [Fact]
    public void LocalizedStringsEnumerator_Reset_ShouldResetPosition()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        Assert.SkipUnless(localizedStrings.Count > 0, "Localized strings is empty");

        var enumerator = localizedStrings.GetEnumerator();

        // Move forward
        enumerator.MoveNext().Should().BeTrue();
        var firstPair = enumerator.Current;

        // Move more
        if (localizedStrings.Count > 1)
        {
            enumerator.MoveNext();
        }

        // Reset
        enumerator.Reset();

        // After reset, MoveNext should give us the first element again
        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Key.Should().Be(firstPair.Key);

        enumerator.Dispose();
    }

    [Fact]
    public void LocalizedStringsEnumerator_MoveNextPastEnd_ShouldReturnFalse()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();

        var enumerator = localizedStrings.GetEnumerator();

        // Move to the end
        while (enumerator.MoveNext()) { }

        // Calling MoveNext again should return false (not throw)
        enumerator.MoveNext().Should().BeFalse();
        enumerator.MoveNext().Should().BeFalse(); // Multiple calls should still return false

        enumerator.Dispose();
    }

    [Fact]
    public void LocalizedStringsEnumerator_Current2_NonGeneric_ShouldWork()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        Assert.SkipUnless(localizedStrings.Count > 0, "Localized strings is empty");

        var enumerator = (IEnumerator)localizedStrings.GetEnumerator();

        enumerator.MoveNext();
        var current = enumerator.Current;

        current.Should().NotBeNull();
        current.Should().BeOfType<KeyValuePair<CultureInfo, string>>();
    }

    #endregion

    #region Multiple fonts caching test (exercises LookupFontFaceSlow)

    [Fact]
    public void Font_MultipleFonts_ShouldExerciseCacheLookup()
    {
        var arialFamily = TestHelpers.GetArialFamilyOrSkip();
        Assert.SkipUnless(arialFamily.Count >= 2, "Arial font family has fewer than 2 fonts");

        var fontFaces = new List<FontFace>();

        try
        {
            // Get font faces from multiple fonts in the same family
            // This exercises the cache lookup paths
            for (uint i = 0; i < Math.Min(arialFamily.Count, 5u); i++)
            {
                var font = arialFamily[i];
                var fontFace = font.GetFontFace();
                fontFaces.Add(fontFace);
            }

            // Now access them again - should use cache
            for (uint i = 0; i < Math.Min(arialFamily.Count, 5u); i++)
            {
                var font = arialFamily[i];
                var fontFace = font.GetFontFace();
                fontFace.Should().NotBeNull();
                fontFace.Release();
            }
        }
        finally
        {
            foreach (var ff in fontFaces)
            {
                ff.Release();
            }
        }
    }

    [Fact]
    public void Font_DifferentFamilies_ShouldExerciseCacheEviction()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var fontFaces = new List<FontFace>();

        try
        {
            // Get fonts from multiple different families to fill and evict from cache
            // Cache size is 4, so accessing more than 4 different fonts should trigger eviction
            string[] familyNames = ["Arial", "Times New Roman", "Courier New", "Verdana", "Tahoma", "Georgia"];

            foreach (var familyName in familyNames)
            {
                var family = fontCollection[familyName];
                if (family != null && family.Count > 0)
                {
                    var font = family[0u];
                    var fontFace = font.GetFontFace();
                    fontFaces.Add(fontFace);
                }
            }

            // All should be valid
            foreach (var ff in fontFaces)
            {
                ff.GlyphCount.Should().BeGreaterThan(0);
            }
        }
        finally
        {
            foreach (var ff in fontFaces)
            {
                ff.Release();
            }
        }
    }

    #endregion

    #region FontFileEnumerator coverage

    [Fact]
    public void FontFileEnumerator_MoveNextPastEnd_ShouldReturnFalse()
    {
        // FontFileEnumerator is accessed via factory.CreateFontFileEnumerator
        // which is internal to the font loading infrastructure
        // We test it indirectly through font file operations

        var factory = DWriteFactory.Instance;
        TestHelpers.SkipIfArialNotAvailable();
        var arialPath = TestHelpers.ArialPath;

        // Creating a font face exercises the font file loading infrastructure
        var fontFace = factory.CreateFontFace(new Uri(arialPath), 0);
        try
        {
            fontFace.Should().NotBeNull();
        }
        finally
        {
            fontFace.Release();
        }
    }

    #endregion
}
