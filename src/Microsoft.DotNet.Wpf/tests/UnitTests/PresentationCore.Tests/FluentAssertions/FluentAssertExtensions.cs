// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using FluentAssertions;
using FluentAssertions.Collections;
using System.Collections.Generic;
using FluentAssertions.Numeric;

namespace  PresentationCore.Tests.FluentAssertions;

public static class FluentAssertExtensions
{
    /// <summary>
    ///  Returns a <see cref="RectangleFAssertions"/> object that can be used to assert the
    ///  current <see cref="RectangleF"/>.
    /// </summary>
    public static RectangleFAssertions Should(this RectangleF actualValue) => new(actualValue);

    /// <summary>
    ///  Returns a <see cref="PointFAssertions"/> object that can be used to assert the
    ///  current <see cref="PointF"/>.
    /// </summary>
    public static PointFAssertions Should(this PointF actualValue) => new(actualValue);

    /// <summary>
    ///  Asserts a <see cref="RectangleF"/> value approximates another value as close as possible.
    /// </summary>
    /// <param name="parent">The <see cref="RectangleFAssertions"/> object that is being extended.</param>
    /// <inheritdoc cref="NumericAssertionsExtensions.BeApproximately(NullableNumericAssertions{float}, float, float, string, object[])"/>
    public static AndConstraint<RectangleFAssertions> BeApproximately(
        this RectangleFAssertions parent,
        RectangleF expectedValue,
        float precision,
        string because = "",
        params object[] becauseArgs)
    {
        parent.Subject.X.Should().BeApproximately(expectedValue.X, precision, because, becauseArgs);
        parent.Subject.Y.Should().BeApproximately(expectedValue.Y, precision, because, becauseArgs);
        parent.Subject.Width.Should().BeApproximately(expectedValue.Width, precision, because, becauseArgs);
        parent.Subject.Height.Should().BeApproximately(expectedValue.Height, precision, because, becauseArgs);

        return new(parent);
    }

    /// <summary>
    ///  Asserts that two <see cref="RectangleF"/> collections contain the same items in the same order
    ///  within the given <paramref name="precision"/>.
    /// </summary>
    /// <param name="precision">The maximum amount of which the two values may differ.</param>
    public static AndConstraint<GenericCollectionAssertions<RectangleF>> BeApproximatelyEquivalentTo(
        this GenericCollectionAssertions<RectangleF> parent,
        IEnumerable<RectangleF> expectation,
        float precision,
        string because = "",
        params object[] becauseArgs)
        => parent.Equal(
            expectation,
            (RectangleF actual, RectangleF expected) =>
                 ComparisonHelpers.EqualsFloating(expected.X, actual.X, precision)
                    && ComparisonHelpers.EqualsFloating(expected.Y, actual.Y, precision)
                    && ComparisonHelpers.EqualsFloating(expected.Width, actual.Width, precision)
                    && ComparisonHelpers.EqualsFloating(expected.Height, actual.Height, precision),
            because,
            becauseArgs);

    /// <summary>
    ///  Asserts a <see cref="PointF"/> value approximates another value as close as possible.
    /// </summary>
    /// <param name="parent">The <see cref="PointFAssertions"/> object that is being extended.</param>
    /// <inheritdoc cref="NumericAssertionsExtensions.BeApproximately(NullableNumericAssertions{float}, float, float, string, object[])"/>
    public static AndConstraint<PointFAssertions> BeApproximately(
        this PointFAssertions parent,
        PointF expectedValue,
        float precision,
        string because = "",
        params object[] becauseArgs)
    {
        parent.Subject.X.Should().BeApproximately(expectedValue.X, precision, because, becauseArgs);
        parent.Subject.Y.Should().BeApproximately(expectedValue.Y, precision, because, becauseArgs);

        return new AndConstraint<PointFAssertions>(parent);
    }

    /// <summary>
    ///  Asserts that two <see cref="PointF"/> collections contain the same items in the same order
    ///  within the given <paramref name="precision"/>.
    /// </summary>
    /// <param name="precision">The maximum amount of which the two values may differ.</param>
    public static AndConstraint<GenericCollectionAssertions<PointF>> BeApproximatelyEquivalentTo(
        this GenericCollectionAssertions<PointF> parent,
        IEnumerable<PointF> expectation,
        float precision,
        string because = "",
        params object[] becauseArgs)
        => parent.Equal(
            expectation,
            (PointF actual, PointF expected) =>
                 ComparisonHelpers.EqualsFloating(expected.X, actual.X, precision)
                    && ComparisonHelpers.EqualsFloating(expected.Y, actual.Y, precision),
            because,
            becauseArgs);
}