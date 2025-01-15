// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Threading.Tests;

public class DispatcherProcessingDisabledTests
{
    [Fact]
    public void Dispose_Default_Nop()
    {
        var disabled = new DispatcherProcessingDisabled();
        disabled.Dispose();

        // Call again.
        disabled.Dispose();
    }
    
    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new DispatcherProcessingDisabled(), new DispatcherProcessingDisabled(), true };
        yield return new object?[] { new DispatcherProcessingDisabled(), new object(), false };
        yield return new object?[] { new DispatcherProcessingDisabled(), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_Success(DispatcherProcessingDisabled disabled, object? obj, bool expected)
    {
        Assert.Equal(expected, disabled.Equals(obj));
        if (obj is DispatcherProcessingDisabled other)
        {
            Assert.Equal(expected, disabled.GetHashCode().Equals(other.GetHashCode()));
            Assert.Equal(expected, disabled == other);
            Assert.Equal(!expected, disabled != other);
        }
    }
    
    [Fact]
    public void GetHashCode_Invoke_ReturnsExpected()
    {
        var disabled = new DispatcherProcessingDisabled();
        Assert.Equal(disabled.GetHashCode(), disabled.GetHashCode());
    }
}
