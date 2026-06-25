// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media.Animation;

public sealed class RepeatBehaviorTests
{
    [Theory]
    // NaN is invalid
    [InlineData(double.NaN)]
    // Infinity is invalid
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    // Negative values are invalid
    [InlineData(double.MinValue)]
    [InlineData(-0.0000000000001)]
    public void Constructor_InvalidCount_ThrowsArgumentOutOfRangeException(double count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeatBehavior(count));
    }

    [Theory]
    // Zero and above is allowed
    [InlineData(0)]
    [InlineData(1.0)]
    [InlineData(double.MaxValue)]
    public void Kind_Count_ReturnsExpected_Value(double count)
    {
        RepeatBehavior behavior = new RepeatBehavior(count);

        Assert.Equal(behavior.Count, count);
    }

    [Fact]
    public void Kind_Count_ReturnsExpected_Type()
    {
        RepeatBehavior behavior = new RepeatBehavior(1.0);

        Assert.True(behavior.HasCount);
        Assert.False(behavior.HasDuration);
    }

    [Fact]
    public void Kind_Count_ThrowsInvalidOperationException()
    {
        RepeatBehavior behavior = new RepeatBehavior(1.0);

        Assert.Throws<InvalidOperationException>(() => behavior.Duration);
    }

    [Theory]
    [MemberData(nameof(Constructor_NegativeDuration_ThrowsArgumentOutOfRangeException_Data))]
    public void Constructor_NegativeDuration_ThrowsArgumentOutOfRangeException(TimeSpan duration)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RepeatBehavior(duration));
    }

    public static IEnumerable<object[]> Constructor_NegativeDuration_ThrowsArgumentOutOfRangeException_Data
    {
        get
        {
            yield return new object[] { TimeSpan.MinValue };
            yield return new object[] { TimeSpan.FromTicks(-1) };
        }
    }

    [Theory]
    [MemberData(nameof(Kind_Duration_ReturnsExpected_Value_Data))]
    public void Kind_Duration_ReturnsExpected_Value(TimeSpan duration)
    {
        RepeatBehavior behavior = new RepeatBehavior(duration);

        Assert.Equal(behavior.Duration, duration);
    }

    public static IEnumerable<object[]> Kind_Duration_ReturnsExpected_Value_Data
    {
        get
        {
            yield return new object[] { TimeSpan.Zero };
            yield return new object[] { TimeSpan.MaxValue };
            yield return new object[] { TimeSpan.FromSeconds(1) };
        }
    }

    [Fact]
    public void Kind_Duration_ReturnsExpected_Type()
    {
        RepeatBehavior behavior = new RepeatBehavior(TimeSpan.FromSeconds(1));

        Assert.False(behavior.HasCount);
        Assert.True(behavior.HasDuration);
    }

    [Fact]
    public void Kind_Duration_ThrowsInvalidOperationException()
    {
        RepeatBehavior behavior = new RepeatBehavior(TimeSpan.FromSeconds(1));

        Assert.Throws<InvalidOperationException>(() => behavior.Count);
    }

    [Fact]
    public void Kind_Forever_ThrowsInvalidOperationException()
    {
        RepeatBehavior behavior = RepeatBehavior.Forever;

        Assert.Throws<InvalidOperationException>(() => behavior.Count);
        Assert.Throws<InvalidOperationException>(() => behavior.Duration);
    }

    [Fact]
    public void Kind_Forever_ReturnsExpected_Type()
    {
        RepeatBehavior behavior = RepeatBehavior.Forever;

        Assert.False(behavior.HasCount);
        Assert.False(behavior.HasDuration);
    }

    [Fact]
    public void Kind_Forever_Instance_Value_HashCode_Equals()
    {
        RepeatBehavior behavior1 = RepeatBehavior.Forever;
        RepeatBehavior behavior2 = RepeatBehavior.Forever;

        Assert.Equal(behavior1.GetHashCode(), behavior2.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(Compare_Instance_Value_Equals_Data))]
    public void Compare_Instance_Value_Equals(RepeatBehavior behavior1, RepeatBehavior behavior2, bool outcome)
    {
        // Check boxed
        Assert.Equal(outcome, behavior1.Equals((object)behavior2));
        Assert.Equal(outcome, behavior2.Equals((object)behavior1));
        // Check overide
        Assert.Equal(outcome, behavior1.Equals(behavior2));
        Assert.Equal(outcome, behavior2.Equals(behavior1));

        // Check operators
        Assert.Equal(outcome, behavior1 == behavior2);
        Assert.Equal(outcome, behavior2 == behavior1);
        Assert.Equal(!outcome, behavior1 != behavior2);
        Assert.Equal(!outcome, behavior2 != behavior1);
    }

    public static IEnumerable<object[]> Compare_Instance_Value_Equals_Data
    {
        get
        {
            // IterationCount
            yield return new object[] { new RepeatBehavior(1.0), new RepeatBehavior(1.0), true };
            yield return new object[] { new RepeatBehavior(1111.1), new RepeatBehavior(1111.1), true };

            // IterationCount (false)
            yield return new object[] { new RepeatBehavior(1111.0), new RepeatBehavior(1112.0), false };
            yield return new object[] { new RepeatBehavior(0.0), new RepeatBehavior(8888.2), false };

            // Duration
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(1)), new RepeatBehavior(TimeSpan.FromSeconds(1)), true };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromDays(3)), new RepeatBehavior(TimeSpan.FromDays(3)), true };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(1111)), new RepeatBehavior(TimeSpan.FromSeconds(1111)), true };

            // Duration (false)
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(1111)), new RepeatBehavior(TimeSpan.FromSeconds(1112)), false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromSeconds(0)), new RepeatBehavior(TimeSpan.FromSeconds(8888)), false };
            yield return new object[] { new RepeatBehavior(TimeSpan.FromDays(5)), new RepeatBehavior(TimeSpan.FromDays(7)), false };

            // Cross type (false)
            yield return new object[] { new RepeatBehavior(1), new RepeatBehavior(TimeSpan.FromSeconds(1)), false };
            yield return new object[] { new RepeatBehavior(1111), new RepeatBehavior(TimeSpan.FromSeconds(1112)), false };

            // Forever
            yield return new object[] { RepeatBehavior.Forever, RepeatBehavior.Forever, true };

            // Forever / Random (false)
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(TimeSpan.FromSeconds(1112)), false };
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(1112), false };

            // Forever default inits (false)
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(TimeSpan.Zero), false };
            yield return new object[] { RepeatBehavior.Forever, new RepeatBehavior(0.0), false };
        }
    }
}

