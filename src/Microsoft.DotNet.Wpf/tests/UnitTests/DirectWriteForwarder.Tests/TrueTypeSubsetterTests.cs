// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="TrueTypeSubsetter"/> class.
/// </summary>
public class TrueTypeSubsetterTests
{
    // TrueType font structure constants
    private const uint TrueTypeSignature = 0x00010000;
    private const int OffsetTableSize = 12; // sfntVersion(4) + numTables(2) + searchRange(2) + entrySelector(2) + rangeShift(2)
    private const int TableRecordSize = 16; // tag(4) + checksum(4) + offset(4) + length(4)

    // Required TrueType tables for a valid subset
    private static readonly string[] s_requiredTables = ["head", "hhea", "hmtx", "maxp", "cmap", "loca", "glyf"];

    private byte[] LoadArialFontData()
    {
        Assert.SkipUnless(File.Exists(TestHelpers.ArialPath), "Arial font not found");
        return File.ReadAllBytes(TestHelpers.ArialPath);
    }

    /// <summary>
    /// Gets glyph indices for the given code points using the specified font face.
    /// </summary>
    private static unsafe ushort[] GetGlyphIndices(FontFace fontFace, uint[] codePoints)
    {
        ushort[] glyphIndices = new ushort[codePoints.Length];
        fixed (uint* pCodePoints = codePoints)
        fixed (ushort* pGlyphIndices = glyphIndices)
        {
            fontFace.GetArrayOfGlyphIndices(pCodePoints, (uint)codePoints.Length, pGlyphIndices);
        }
        return glyphIndices;
    }

    /// <summary>
    /// Reads a 4-character table tag from a byte span.
    /// </summary>
    private static string ReadTableTag(ReadOnlySpan<byte> data)
    {
        return new string([(char)data[0], (char)data[1], (char)data[2], (char)data[3]]);
    }

    /// <summary>
    /// Finds a table in a TrueType font and returns its offset and length.
    /// Returns (0, 0) if not found.
    /// </summary>
    private static (uint offset, uint length) FindTable(byte[] fontData, string tableTag)
    {
        ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(fontData.AsSpan(4));

        for (int i = 0; i < numTables; i++)
        {
            int recordOffset = OffsetTableSize + (i * TableRecordSize);
            string tag = ReadTableTag(fontData.AsSpan(recordOffset, 4));
            if (tag == tableTag)
            {
                uint offset = BinaryPrimitives.ReadUInt32BigEndian(fontData.AsSpan(recordOffset + 8, 4));
                uint length = BinaryPrimitives.ReadUInt32BigEndian(fontData.AsSpan(recordOffset + 12, 4));
                return (offset, length);
            }
        }

        return (0, 0);
    }

    /// <summary>
    /// Reads numGlyphs from the maxp table.
    /// maxp table structure: version (4 bytes), numGlyphs (2 bytes), ...
    /// </summary>
    private static ushort ReadMaxpNumGlyphs(byte[] fontData)
    {
        var (offset, length) = FindTable(fontData, "maxp");
        if (offset == 0 || length < 6)
            return 0;

        return BinaryPrimitives.ReadUInt16BigEndian(fontData.AsSpan((int)offset + 4, 2));
    }

    /// <summary>
    /// Parses the table directory from a TrueType font and returns table names.
    /// </summary>
    private static HashSet<string> GetTableNames(byte[] fontData)
    {
        var tables = new HashSet<string>();
        ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(fontData.AsSpan(4));

        for (int i = 0; i < numTables; i++)
        {
            int recordOffset = OffsetTableSize + (i * TableRecordSize);
            string tag = ReadTableTag(fontData.AsSpan(recordOffset, 4));
            tables.Add(tag);
        }

        return tables;
    }

    [Fact]
    public void ComputeSubset_WithValidFont_ShouldReturnValidTrueTypeStructure()
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

                // Verify TrueType signature (sfntVersion = 0x00010000)
                uint signature = BinaryPrimitives.ReadUInt32BigEndian(result.AsSpan(0, 4));
                signature.Should().Be(TrueTypeSignature, "Subset should have valid TrueType signature");

                // Verify numTables is reasonable (at least the required tables)
                ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(4, 2));
                numTables.Should().BeGreaterThanOrEqualTo((ushort)s_requiredTables.Length,
                    $"Subset should contain at least {s_requiredTables.Length} tables");

                // Verify offset table fields are consistent
                ushort searchRange = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(6, 2));
                ushort rangeShift = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(10, 2));

                // searchRange = (maximum power of 2 <= numTables) * 16
                int maxPow2 = 1;
                while (maxPow2 * 2 <= numTables) maxPow2 *= 2;
                searchRange.Should().Be((ushort)(maxPow2 * 16), "searchRange should be correctly calculated");

                // rangeShift = numTables * 16 - searchRange
                rangeShift.Should().Be((ushort)(numTables * 16 - searchRange), "rangeShift should be correctly calculated");
            }
        }
    }

    [Fact]
    public void ComputeSubset_ShouldContainRequiredTables()
    {
        var fontData = LoadArialFontData();

        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [0, 1, 2, 3];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);

                var subsetTables = GetTableNames(result);

                // Verify all required tables are present
                foreach (var requiredTable in s_requiredTables)
                {
                    subsetTables.Should().Contain(requiredTable,
                        $"Subset should contain required table '{requiredTable}'");
                }
            }
        }
    }

    [Fact]
    public void ComputeSubset_TableOffsetsAndLengths_ShouldBeValid()
    {
        var fontData = LoadArialFontData();

        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [0, 1, 2, 3];

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);

                ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(result.AsSpan(4, 2));

                for (int i = 0; i < numTables; i++)
                {
                    int recordOffset = OffsetTableSize + (i * TableRecordSize);

                    // Read table record fields
                    string tag = ReadTableTag(result.AsSpan(recordOffset, 4));
                    uint tableOffset = BinaryPrimitives.ReadUInt32BigEndian(result.AsSpan(recordOffset + 8, 4));
                    uint tableLength = BinaryPrimitives.ReadUInt32BigEndian(result.AsSpan(recordOffset + 12, 4));

                    // Verify offset and length are within bounds
                    tableOffset.Should().BeLessThan((uint)result.Length,
                        $"Table '{tag}' offset should be within file bounds");
                    (tableOffset + tableLength).Should().BeLessThanOrEqualTo((uint)result.Length,
                        $"Table '{tag}' should not extend beyond file end");
                    tableLength.Should().BeGreaterThan(0,
                        $"Table '{tag}' should have non-zero length");
                }
            }
        }
    }

    [Fact]
    public void ComputeSubset_WithNullGlyphArray_ShouldThrow()
    {
        var fontData = LoadArialFontData();

        var arialUri = new Uri(TestHelpers.ArialPath);

        Assert.Throws<NullReferenceException>(() =>
        {
            unsafe
            {
                fixed (byte* pFontData = fontData)
                {
                    TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, null!);
                }
            }
        });
    }

    [Fact]
    public void ComputeSubset_WithEmptyGlyphArray_ShouldThrow()
    {
        var fontData = LoadArialFontData();

        var arialUri = new Uri(TestHelpers.ArialPath);
        ushort[] glyphArray = [];

        Assert.Throws<IndexOutOfRangeException>(() =>
        {
            unsafe
            {
                fixed (byte* pFontData = fontData)
                {
                    TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphArray);
                }
            }
        });
    }

    [Fact]
    public void ComputeSubset_SubsetShouldBeSmallerThanOriginal()
    {
        var fontData = LoadArialFontData();

        var arialUri = new Uri(TestHelpers.ArialPath);

        // Get actual glyph indices for characters 'A', 'B', 'C' plus .notdef (0)
        var factory = DWriteFactory.Instance;
        var fontFace = factory.CreateFontFace(new Uri(TestHelpers.ArialPath), 0);
        ushort[] glyphIndices;
        try
        {
            uint[] codePoints = ['A', 'B', 'C'];
            var charGlyphs = GetGlyphIndices(fontFace, codePoints);
            glyphIndices = [0, charGlyphs[0], charGlyphs[1], charGlyphs[2]]; // .notdef + A, B, C
        }
        finally
        {
            fontFace.Release();
        }

        unsafe
        {
            fixed (byte* pFontData = fontData)
            {
                var result = TrueTypeSubsetter.ComputeSubset(pFontData, fontData.Length, arialUri, 0, glyphIndices);

                result.Should().NotBeNull();

                // Subset with few glyphs should be smaller than original
                // Note: The subsetter keeps the full glyph table structure (maxp.numGlyphs unchanged)
                // but only embeds outlines for the requested glyphs, reducing overall file size
                result.Length.Should().BeLessThan(fontData.Length,
                    "Subset should be smaller than original font");
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
                result.Length.Should().BeGreaterThan(OffsetTableSize + TableRecordSize,
                    "Output should be large enough to contain header and at least one table record");

                // Verify TrueType signature using BinaryPrimitives
                uint signature = BinaryPrimitives.ReadUInt32BigEndian(result.AsSpan(0, 4));

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
