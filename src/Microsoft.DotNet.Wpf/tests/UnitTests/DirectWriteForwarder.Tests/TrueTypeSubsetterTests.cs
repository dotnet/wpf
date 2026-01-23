// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="TrueTypeSubsetter"/> class.
/// </summary>
public class TrueTypeSubsetterTests
{
    private byte[] LoadArialFontData()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial font not found");
        return File.ReadAllBytes(TestHelpers.ArialPath);
    }

    [Fact]
    public void ComputeSubset_WithValidFont_ShouldReturnData()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [0, 1, 2, 3]; // Basic glyph indices
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);
                
                result.Should().NotBeNull();
                result.Should().NotBeEmpty();
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithNullGlyphArray_ShouldThrow()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                try
                {
                    TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, null!);
                    Assert.Fail("Expected NullReferenceException");
                }
                catch (NullReferenceException)
                {
                    // Expected
                }
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithEmptyGlyphArray_ShouldThrow()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [];
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                try
                {
                    TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);
                    Assert.Fail("Expected IndexOutOfRangeException");
                }
                catch (IndexOutOfRangeException)
                {
                    // Expected
                }
            }
        }
    }

    [Fact]
    public void ComputeSubset_SubsetShouldBeSmallerThanOriginal()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        // Request only a few glyphs
        ushort[] glyphArray = [0, 65, 66, 67]; // .notdef, A, B, C
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);
                
                result.Should().NotBeNull();
                // Subset with few glyphs should be smaller than original
                result.Length.Should().BeLessThan(fontData.Length);
            }
        }
    }

    [Fact]
    public void ComputeSubset_OutputShouldBeValidTrueType()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [0, 1, 2, 3];
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);
                
                result.Should().NotBeNull();
                result.Length.Should().BeGreaterThan(4);
                
                // TrueType files start with specific magic numbers
                // 0x00010000 for TrueType or 'OTTO' (0x4F54544F) for CFF
                // or 'true' (0x74727565) for some Mac TrueType fonts
                uint signature = (uint)((result[0] << 24) | (result[1] << 16) | (result[2] << 8) | result[3]);
                
                // Valid TrueType signature is 0x00010000
                bool isValidTrueType = signature is 0x00010000 or 0x74727565 or 0x4F54544F; // 0x00010000, 'true', 'OTTO'
                
                isValidTrueType.Should().BeTrue($"Output should have valid TrueType signature, got 0x{signature:X8}");
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithDifferentGlyphCounts_ShouldProduceDifferentSizes()
    {
        var fontData = LoadArialFontData();
        
        var arialUri = new Uri(TestHelpers.ArialPath);
        
        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                // Small subset
                ushort[] smallGlyphArray = [0, 1, 2];
                var smallResult = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, smallGlyphArray);
                
                // Larger subset
                ushort[] largeGlyphArray = Enumerable.Range(0, 100).Select(i => (ushort)i).ToArray();
                var largeResult = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, largeGlyphArray);
                
                smallResult.Should().NotBeNull();
                largeResult.Should().NotBeNull();
                
                // More glyphs should generally result in larger subset
                largeResult.Length.Should().BeGreaterThanOrEqualTo(smallResult.Length);
            }
        }
    }
}
