// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// A table containing a subset of parameters and all the possible value combinations of that subset.
    /// </summary>
    internal class ParameterInteraction
    {
        public ParameterInteraction(ParameterInteraction interaction)
        {
            this.Parameters = new List<int>(interaction.Parameters);
            this.Combinations = new List<ValueCombination>(interaction.Combinations.Count);
            foreach (var combination in interaction.Combinations)
            {
                var newCombination = new ValueCombination(combination);
                Combinations.Add(newCombination);
            }
        }

        public ParameterInteraction(IEnumerable<int> parameters)
        {
            this.Combinations = new List<ValueCombination>();
            this.Parameters = new List<int>(parameters);
        }

        public IList<int> Parameters { get; private set; }

        public IList<ValueCombination> Combinations { get; private set; }

        public int GetUncoveredCombinationsCount()
        {
            return Combinations.Count((c) => c.State == ValueCombinationState.Uncovered);   
        }

        /// <summary>
        /// Returns true when obj refers to the same subset of parameters.
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>Whether subsets are equal</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is ParameterInteraction))
            {
                return false;
            }

            var interaction = (ParameterInteraction)obj;

            if (this.Parameters.Count != interaction.Parameters.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Parameters.Count; i++)
            {
                if (this.Parameters[i] != interaction.Parameters[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return Parameters.Aggregate(0, (seed, next) => seed ^ next);
        }

        /// <summary>
        /// Returns a new table that is the union of the input tables.
        /// </summary>
        /// <param name="parameters">The model's parameters</param>
        /// <param name="first">First table</param>
        /// <param name="second">Second table</param>
        /// <param name="newState">Function to calculate state of merged value combinations</param>
        /// <returns>The new table</returns>
        public static ParameterInteraction Merge<T>(IList<ParameterBase> parameters, ParameterInteraction first, ParameterInteraction second, Func<ValueCombinationState, ValueCombinationState, ValueCombinationState> newState) 
            where T : new()
        {
            List<int> parameterIndices = first.Parameters.Union(second.Parameters).ToList();
            parameterIndices.Sort();

            var mergedInteraction = new ParameterInteraction(parameterIndices);

            var valueTable = ParameterInteractionTable<T>.GenerateValueTable(parameters, mergedInteraction);
            foreach (var value in valueTable)
            {
                mergedInteraction.Combinations.Add(new ValueCombination(value, mergedInteraction));
            }

            foreach (var combination in mergedInteraction.Combinations)
            {
                var firstMatch = first.Combinations.First((c) => ParameterInteractionTable<T>.MatchCombination(c, combination));
                var secondMatch = second.Combinations.First((c) => ParameterInteractionTable<T>.MatchCombination(c, combination));
                combination.State = newState(firstMatch.State, secondMatch.State);
            }

            return mergedInteraction;
        }
    }
}
