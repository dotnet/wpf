// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement.Tests;

public class LocalizedNameDescriptionPairTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "  ")]
    [InlineData("name", "description")]
    public void Ctor_String_String(string name, string description)
    {
        var pair = new LocalizedNameDescriptionPair(name, description);
        Assert.Equal(name, pair.Name);
        Assert.Equal(description, pair.Description);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var pair = new LocalizedNameDescriptionPair("name", "description");

        yield return new object?[] { pair, pair, true, true };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("name", "description"), true, true };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("NAME", "description"), false, false };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("name", "DESCRIPTION"), false, false };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("name2", "description"), false, false };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("name", "description2"), false, false };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("", "description"), false, false };
        yield return new object?[] { pair, new LocalizedNameDescriptionPair("name", ""), false, false };
        yield return new object?[] { pair, new SubLocalizedNameDescriptionPair("name", "description"), false, true };
        yield return new object?[] { pair, new object(), false, false };
        yield return new object?[] { pair, null, false, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(LocalizedNameDescriptionPair pair, object obj, bool expected, bool expectedHashCode)
    {
        Assert.Equal(expected, pair.Equals(obj));
        if (obj is LocalizedNameDescriptionPair otherPair)
        {
            Assert.Equal(expected, otherPair.Equals(pair));
            Assert.Equal(expectedHashCode, pair.GetHashCode().Equals(otherPair.GetHashCode()));
        }
    }

    [Fact]
    public void GetHashCode_Invoke_ReturnsEqual()
    {
        var pair = new LocalizedNameDescriptionPair("name", "description");
        Assert.NotEqual(0, pair.GetHashCode());
        Assert.Equal(pair.GetHashCode(), pair.GetHashCode());
    }

    private class SubLocalizedNameDescriptionPair : LocalizedNameDescriptionPair
    {
        public SubLocalizedNameDescriptionPair(string name, string description) : base(name, description)
        {
        }
    }
}
