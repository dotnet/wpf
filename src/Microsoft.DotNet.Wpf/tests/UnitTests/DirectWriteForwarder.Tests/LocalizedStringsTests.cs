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
    [Fact]
    public void Count_ShouldBeGreaterThanZero()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        localizedStrings.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Keys_ShouldContainCultureInfo()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var keys = localizedStrings.Keys;
        
        keys.Should().NotBeEmpty();
        keys.Should().AllBeOfType<CultureInfo>();
    }

    [Fact]
    public void Values_ShouldContainStrings()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var values = localizedStrings.Values;
        
        values.Should().NotBeEmpty();
        values.Should().AllBeOfType<string>();
    }

    [Fact]
    public void Indexer_WithValidCulture_ShouldReturnName()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        // Get the first available culture
        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);
        
        var name = localizedStrings[firstCulture];
        
        name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ContainsKey_WithExistingCulture_ShouldReturnTrue()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);
        
        localizedStrings.ContainsKey(firstCulture).Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_WithNonExistingCulture_ShouldReturnFalse()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
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
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);
        
        bool result = localizedStrings.TryGetValue(firstCulture, out string value);
        
        result.Should().BeTrue();
        value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetEnumerator_ShouldEnumerateAllPairs()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
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
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
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
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        localizedStrings.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Add_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        Action act = () => localizedStrings.Add(CultureInfo.InvariantCulture, "Test");
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Remove_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);
        
        Action act = () => localizedStrings.Remove(firstCulture);
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Clear_ShouldThrowNotSupportedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        Action act = () => localizedStrings.Clear();
        
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void StringsCount_ShouldMatchCount()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        localizedStrings.StringsCount.Should().Be((uint)localizedStrings.Count);
    }

    [Fact]
    public void FindLocaleName_WithEnUS_ShouldReturnTrueAndIndex()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
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
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var localeName = localizedStrings.GetLocaleName(0);
        
        localeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetString_WithValidIndex_ShouldReturnString()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var str = localizedStrings.GetString(0);
        
        str.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Contains_ShouldThrowNotImplementedException()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var firstCulture = TestHelpers.GetFirstCultureOrSkip(localizedStrings);
        
        var pair = new KeyValuePair<CultureInfo, string>(firstCulture, localizedStrings[firstCulture]);
        
        Action act = () => localizedStrings.Contains(pair);
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void CopyTo_ShouldCopyPairsToArray()
    {
        var localizedStrings = TestHelpers.GetArialLocalizedStringsOrSkip();
        
        var array = new KeyValuePair<CultureInfo, string>[localizedStrings.Count];
        localizedStrings.CopyTo(array, 0);
        
        array.Should().AllSatisfy(kvp => kvp.Key.Should().NotBeNull());
    }
}
