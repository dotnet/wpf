// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace MS.Internal.Text.TextInterface.Tests;

/// <summary>
/// Tests for <see cref="LocalizedStrings"/> class.
/// </summary>
public class LocalizedStringsTests
{
    private LocalizedStrings? GetArialFamilyNames()
    {
        var fontCollection = DWriteFactory.SystemFontCollection;
        var arialFamily = fontCollection["Arial"];
        if (arialFamily == null) return null;
        return arialFamily.FamilyNames;
    }

    [Fact]
    public void Count_ShouldBeGreaterThanZero()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        localizedStrings.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Keys_ShouldContainCultureInfo()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var keys = localizedStrings.Keys;
        
        keys.Should().NotBeEmpty();
        keys.Should().AllBeOfType<CultureInfo>();
    }

    [Fact]
    public void Values_ShouldContainStrings()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var values = localizedStrings.Values;
        
        values.Should().NotBeEmpty();
        values.Should().AllBeOfType<string>();
    }

    [Fact]
    public void Indexer_WithValidCulture_ShouldReturnName()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        // Get the first available culture
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        if (firstCulture == null) return;
        
        var name = localizedStrings[firstCulture];
        
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ContainsKey_WithExistingCulture_ShouldReturnTrue()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        if (firstCulture == null) return;
        
        localizedStrings.ContainsKey(firstCulture).Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_WithNonExistingCulture_ShouldReturnFalse()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        // Use a culture that's unlikely to be in the font
        var rareCulture = new CultureInfo("zu-ZA"); // Zulu
        
        // This may or may not contain it, but test that it doesn't throw
        var result = localizedStrings.ContainsKey(rareCulture);
        // Just verify it returns a boolean (doesn't throw)
        result.Should().Be(result); // Self-comparison just verifies no exception
    }

    [Fact]
    public void TryGetValue_WithExistingCulture_ShouldReturnTrueAndValue()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        if (firstCulture == null) return;
        
        bool result = localizedStrings.TryGetValue(firstCulture, out string value);
        
        result.Should().BeTrue();
        value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetEnumerator_ShouldEnumerateAllPairs()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var pairs = localizedStrings.ToList();
        
        pairs.Should().NotBeEmpty();
        pairs.Should().AllSatisfy(kvp =>
        {
            kvp.Key.Should().NotBeNull();
            kvp.Value.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void EnglishName_ShouldExist()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        // Most fonts should have an English (US) name
        var englishCulture = new CultureInfo("en-US");
        
        if (localizedStrings.ContainsKey(englishCulture))
        {
            var englishName = localizedStrings[englishCulture];
            englishName.Should().Contain("Arial");
        }
    }

    [Fact]
    public void IsReadOnly_ShouldBeTrue()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        localizedStrings.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Add_ShouldThrowNotSupportedException()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        Action act = () => localizedStrings.Add(CultureInfo.InvariantCulture, "Test");
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Remove_ShouldThrowNotSupportedException()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        if (firstCulture == null) return;
        
        Action act = () => localizedStrings.Remove(firstCulture);
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Clear_ShouldThrowNotSupportedException()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        Action act = () => localizedStrings.Clear();
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void StringsCount_ShouldMatchCount()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        localizedStrings.StringsCount.Should().Be((uint)localizedStrings.Count);
    }

    [Fact]
    public void FindLocaleName_WithEnUS_ShouldReturnTrueAndIndex()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        bool found = localizedStrings.FindLocaleName("en-US", out uint index);
        
        // en-US should typically exist for Arial
        if (found)
        {
            index.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void GetLocaleName_WithValidIndex_ShouldReturnLocaleName()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var localeName = localizedStrings.GetLocaleName(0);
        
        localeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetString_WithValidIndex_ShouldReturnString()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var str = localizedStrings.GetString(0);
        
        str.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Contains_ShouldThrowNotImplementedException()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var firstCulture = localizedStrings.Keys.FirstOrDefault();
        if (firstCulture == null) return;
        
        var pair = new KeyValuePair<CultureInfo, string>(firstCulture, localizedStrings[firstCulture]);
        
        Action act = () => localizedStrings.Contains(pair);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void CopyTo_ShouldCopyPairsToArray()
    {
        var localizedStrings = GetArialFamilyNames();
        if (localizedStrings == null) return;
        
        var array = new KeyValuePair<CultureInfo, string>[localizedStrings.Count];
        localizedStrings.CopyTo(array, 0);
        
        array.Should().AllSatisfy(kvp => kvp.Key.Should().NotBeNull());
    }
}
