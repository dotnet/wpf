// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Linq.Expressions;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    /// <summary>
    /// Represents the predicate of a constraint with a logical implication.
    /// </summary>
    /// <typeparam name="T">The type of variation being operated on.</typeparam>
    public class IfPredicate<T> where T : new()
    {
        internal IfPredicate(Expression<Func<T, bool>> predicate) { Predicate = predicate; }

        internal Expression<Func<T, bool>> Predicate { get; set; }
    }
}
