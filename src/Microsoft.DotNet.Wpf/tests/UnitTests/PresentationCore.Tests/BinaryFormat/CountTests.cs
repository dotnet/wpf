// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class CountTests
{
    [Fact]
    public void CountDoesNotAcceptNegativeValues()
    {
        Func<Count> func = () => (Count)(-1);
        func.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CountEqualsInt()
    {
        ((Count)4).Should().Be(4);
    }

    [Fact]
    public void CountEqualsCount()
    {
        ((Count)4).Should().Be((Count)4);
    }

    [Fact]
    public void CountToStringIsInt()
    {
        ((Count)5).ToString().Should().Be("5");
    }
}