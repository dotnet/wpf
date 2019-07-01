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
    /// Represents a relationship between parameters and their values, or other constraints. 
    /// </summary>
    /// <remarks>
    /// Exhaustively testing all possible inputs to any nontrivial software component is generally not possible
    /// because of the enormous number of variations. Combinatorial testing is one approach that achieves high coverage
    /// with a much smaller set of variations. Pairwise, the most common combinatorial strategy, tests every possible 
    /// pair of values. Higher orders of combinations (three-wise, four-wise, and so on) can also be used for higher coverage
    /// at the expense of more variations. See <a href="http://pairwise.org">Pairwise Testing</a> and 
    /// <a href="http://www.pairwise.org/docs/pnsqc2006/PNSQC%20140%20-%20Jacek%20Czerwonka%20-%20Pairwise%20Testing%20-%20BW.pdf">
    /// Pairwise Testing in Real World</a> for more resources.
    /// 
    /// Ideally, all parameters in a model are independent; however, this is generally not the case. Constraints define 
    /// combinations of values that are impossible in the variations produced by the <see cref="Model{T}"/> using 
    /// combinatorial testing techniques.
    /// </remarks>
    
    public abstract class Constraint<T> where T : new()
    {
        /// <summary>
        /// Calculates the exclusions for this constraint.
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>
        /// A table containing the interaction between parameters for this constraint. All values are marked Excluded or Covered.
        /// </returns>
        internal abstract ParameterInteraction GetExcludedCombinations(Model<T> model);

        /// <summary>
        /// Calcultes whether the specified value satisfies the constraint or has insufficient data to do so.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="combination">The value.</param>
        /// <returns>The calculated result.</returns>
        internal abstract ConstraintSatisfaction SatisfiesContraint(Model<T> model, ValueCombination combination);

        /// <summary>
        /// Holds a precalculated <see cref="ParameterInteraction" /> to avoid recalculation.
        /// </summary>
        internal ParameterInteraction CachedInteraction { get; set; }

        /// <summary>
        /// Clears CachedInteraction for the constraint and any children.
        /// </summary>
        internal abstract void ClearCache();

        /// <summary>
        /// Creates a new predicate for an if-then-else constraint.
        /// </summary>
        /// <param name="predicate">The test as an expression.</param>
        /// <returns>The predicate.</returns>
        public static IfPredicate<T> If(Expression<Func<T, bool>> predicate)
        {
            return new IfPredicate<T>(predicate);
        }

        /// <summary>
        /// Creates a new ConditionalConstraint.
        /// </summary>
        /// <param name="predicate">The test as an expression.</param>
        /// <returns>The constraint.</returns>
        public static ConditionalConstraint<T> Conditional(Expression<Func<T, bool>> predicate)
        {
            return new ConditionalConstraint<T>(predicate);
        }
    }
}
