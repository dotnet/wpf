// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Linq.Expressions;
using Microsoft.Test.VariationGeneration.Constraints;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Holds extension methods that construct if-then-else constraints.
    /// </summary>
    public static class ConstraintExtensions
    {
        /// <summary>
        /// Constructs an if-then constraint.
        /// </summary>
        /// <typeparam name="T">Type of the variation being acted on.</typeparam>
        /// <param name="ifPredicate">The "if" portion of the if-then.</param>
        /// <param name="predicate">The test of the "then".</param>
        /// <returns>The if-then constraint.</returns>
        public static IfThenConstraint<T> Then<T>(this IfPredicate<T> ifPredicate, Expression<Func<T, bool>> predicate) where T : new()
        {
            return new IfThenConstraint<T>(ifPredicate, predicate);
        }

        /// <summary>
        /// Constructs an if-then-else constraint.
        /// </summary>
        /// <typeparam name="T">Type of the variation being acted on.</typeparam>
        /// <param name="ifThenConstraint">The "if-then" portion of the if-then-else.</param>
        /// <param name="predicate">The test of the "else".</param>
        /// <returns>The if-then-else constraint.</returns>
        public static IfThenElseConstraint<T> Else<T>(this IfThenConstraint<T> ifThenConstraint, Expression<Func<T, bool>> predicate) where T : new()
        {
            return new IfThenElseConstraint<T>(ifThenConstraint, predicate);
        }
    }
}
