// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.VariationGeneration.Constraints;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Table containing all the interactions between parameters in the model, and all possible value combinations with their current state.
    /// </summary>
    internal class ParameterInteractionTable<T> where T : new()
    {
        public ParameterInteractionTable(Model<T> model, int order)
        {
            GenerateInteractionsForParameters(model, order);
            GenerateInteractionsForConstraints(model, order);

            excludedCombinations = (from interaction in Interactions
                                   from combination in interaction.Combinations
                                   where combination.State == ValueCombinationState.Excluded
                                   select combination).ToList();
        }

        List<ParameterInteraction> interactions = new List<ParameterInteraction>();
        public IList<ParameterInteraction> Interactions { get { return interactions; } }

        /// <summary>
        /// Test whether all value combinations are Covered or Excluded
        /// </summary>
        /// <returns>The result</returns>
        public bool IsCovered() 
        { 
            return !Interactions.Any((i) => i.Combinations.Any((c) => c.State == ValueCombinationState.Uncovered)); 
        }

        List<ValueCombination> excludedCombinations;

        /// <summary>
        /// Returns all excluded combinations in the table
        /// </summary>
        public IEnumerable<ValueCombination> ExcludedCombinations { get { return excludedCombinations; } }

        // implicit constraints are represented as explicit constraints internally
        // this is the constraints in the model + the internal constraints
        List<Constraint<T>> completeConstraints = new List<Constraint<T>>();

        // walks the constraints and adds any needed interactions and marks existing combinations as Excluded
        private void GenerateInteractionsForConstraints(Model<T> model, int order)
        {
            completeConstraints.Clear();
            completeConstraints.AddRange(model.Constraints);
            completeConstraints.Add(new TaggedValueConstraint<T>());

            // clear any pregenerated data
            foreach (var constraint in completeConstraints)
            {
                constraint.ClearCache();
            }

            var constraintInteractions = new List<ParameterInteraction>();
            foreach (var constraint in completeConstraints)
            {
                var constraintInteraction = constraint.GetExcludedCombinations(model);
                constraintInteractions.Add(constraintInteraction);

                MergeConstraintInteraction(order, constraintInteraction);
            }

            var uncoveredCombinations =
                from interaction in Interactions
                from combination in interaction.Combinations
                where combination.State == ValueCombinationState.Uncovered
                select combination;

            // make sure uncovered combinations don't violate any constraints
            foreach (var combination in uncoveredCombinations)
            {
                foreach (var constraint in completeConstraints)
                {
                    if (constraint.SatisfiesContraint(model, combination) == ConstraintSatisfaction.Unsatisfied)
                    {
                        combination.State = ValueCombinationState.Excluded;
                    }
                }
            }

            GenerateDependentConstraintInteractions(model, order, constraintInteractions);

            // if any interaction has only excluded combinations then everything is excluded
            if (Interactions.Any((i) => i.Combinations.All((c) => c.State == ValueCombinationState.Excluded)))
            {
                uncoveredCombinations =
                    from interaction in Interactions
                    from combination in interaction.Combinations
                    where combination.State == ValueCombinationState.Uncovered
                    select combination;

                foreach (var combination in uncoveredCombinations)
                {
                    combination.State = ValueCombinationState.Excluded;
                }
            }
        }

        // adds an interaction generated from a constraint to the table
        private void MergeConstraintInteraction(int order, ParameterInteraction constraintInteraction)
        {
            // is the constraint already in the table
            if (!Interactions.Contains(constraintInteraction))
            {
                // if the interaction has more parameters than the order we can't mark any of the existing combinations excluded
                // if the interaction is shorter we need to mark the existing combinations excluded
                if (constraintInteraction.Parameters.Count > order)
                {
                    Interactions.Add(constraintInteraction);
                }
                else
                {
                    var excludedValues = constraintInteraction.Combinations.Where((c) => c.State == ValueCombinationState.Excluded);

                    var candidateInteractions =
                        from interaction in Interactions
                        where constraintInteraction.Parameters.All((i) => interaction.Parameters.Contains(i))
                        select interaction;

                    var excludedCombinations =
                        from interaction in candidateInteractions
                        from combination in interaction.Combinations
                        where excludedValues.Any((c) => MatchCombination(c, combination))
                        select combination;

                    foreach (var combination in excludedCombinations)
                    {
                        combination.State = ValueCombinationState.Excluded;
                    }
                }
            }
            else
            {
                // mark combinations excluded by the constraint excluded in the table
                var interaction = Interactions.First((i) => i.Equals(constraintInteraction));

                var excludedValues = constraintInteraction.Combinations.Where((c) => c.State == ValueCombinationState.Excluded);

                var combinations = interaction.Combinations.Where((c) => excludedValues.Any((excluded) => MatchCombination(excluded, c)));
                foreach (var combination in combinations)
                {
                    combination.State = ValueCombinationState.Excluded;
                }
            }
        }

        // for the following system:
        // if A == 0 then B == 0
        // if B == 0 then C == 0
        // if C == 0 then A == 1
        // all combinations with A == 0 need to be excluded, but this is impossible to determine when looking at individual constraints
        // to do this:
        //      - create groups of constraints where all members have a parameter that overlaps with another constraint
        //      - create ParameterInteraction for each group of constraints
        //      - for the existing uncovered combinations, if they excluded in every matching combination in the group interaction, mark excluded
        //      - higher order combinations that match the order of a constraint's interaction can also need to be exculded
        private void GenerateDependentConstraintInteractions(Model<T> model, int order, List<ParameterInteraction> constraintInteractions)
        {
            // group all the constraint parameter interactions that share parameters
            var dependentConstraintSets = new List<List<ParameterInteraction>>();
            while (constraintInteractions.Count > 0)
            {
                var dependentConstraintSet = new List<ParameterInteraction>();
                var interactionsToExplore = new List<ParameterInteraction>();

                interactionsToExplore.Add(constraintInteractions[0]);
                constraintInteractions.RemoveAt(0);

                while (interactionsToExplore.Count > 0)
                {
                    ParameterInteraction current = interactionsToExplore[0];
                    interactionsToExplore.RemoveAt(0);
                    dependentConstraintSet.Add(current);

                    var dependentInteractions =
                        (from constraint in constraintInteractions
                        where constraint.Parameters.Any((i) => current.Parameters.Contains(i))
                        select constraint).ToList();

                    foreach (var dependentInteraction in dependentInteractions)
                    {
                        constraintInteractions.Remove(dependentInteraction);
                    }

                    interactionsToExplore.AddRange(dependentInteractions);
                }

                dependentConstraintSets.Add(dependentConstraintSet);
            }

            // walk over the groups of constraints
            foreach (var dependentConstraintSet in dependentConstraintSets)
            {
                // if there's only one constraint no more processing is necessary
                if (dependentConstraintSet.Count <= 1)
                {
                    continue;
                }

                // merge the interactions of all the constraints
                var uniqueParameters = new Dictionary<int,bool>();
                var parameterInteractionCounts = new List<int>();
                for (int i = 0; i < dependentConstraintSet.Count; i++)
                {
                    foreach (var parameter in dependentConstraintSet[i].Parameters)
                    {
                        uniqueParameters[parameter] = true;
                    }

                    if (dependentConstraintSet[i].Parameters.Count > order)
                    {
                        parameterInteractionCounts.Add(dependentConstraintSet[i].Parameters.Count);
                    }
                }

                var sortedParameters = uniqueParameters.Keys.ToList();
                sortedParameters.Sort();

                ParameterInteraction completeInteraction = new ParameterInteraction(sortedParameters);
                var valueTable = ParameterInteractionTable<T>.GenerateValueTable(model.Parameters, completeInteraction);
                foreach (var value in valueTable)
                {
                    completeInteraction.Combinations
                        .Add(new ValueCombination(value, completeInteraction){ State = ValueCombinationState.Covered });
                }

                // calculate the excluded combinations in the new uber interaction
                var completeInteractionExcludedCombinations = new List<ValueCombination>();

                // find the combinations from the uber interaction that aren't excluded
                // if a combination is a subset of any of these it is not excluded
                var allowedCombinations = new List<ValueCombination>();

                foreach (var combination in completeInteraction.Combinations)
                {
                    bool exclude = false;
                    foreach (var constraint in completeConstraints)
                    {
                        if (constraint.SatisfiesContraint(model, combination) == ConstraintSatisfaction.Unsatisfied)
                        {
                            exclude = true;
                            break;
                        }
                    }

                    if (exclude)
                    {
                        combination.State = ValueCombinationState.Excluded;
                        completeInteractionExcludedCombinations.Add(combination);
                    }
                    else
                    {
                        allowedCombinations.Add(combination);
                    }
                }

                // find the existing combinations that are never allowed in the uber interaction
                var individualInteractionExcludedCombinations =
                    from interaction in Interactions
                    from combination in interaction.Combinations
                    where !combination.ParameterToValueMap.Any((p) => !completeInteraction.Parameters.Contains(p.Key)) 
                        && !allowedCombinations.Any((c) => MatchCombination(combination, c))
                    select combination;

                // mark the combinations in the table as excluded
                foreach (var combination in individualInteractionExcludedCombinations)
                {
                    combination.State = ValueCombinationState.Excluded;
                }

                GenerateHigherOrderDependentExclusions(model, order, parameterInteractionCounts, completeInteraction, allowedCombinations);
            }
        }

        private void GenerateHigherOrderDependentExclusions(Model<T> model, int order, List<int> parameterInteractionCounts, ParameterInteraction completeInteraction, IEnumerable<ValueCombination> allowedCombinations)
        {
            // generate the combinations for orders between order and completerInteraction.Parameters.Count
            foreach (var count in parameterInteractionCounts)
            {
                IList<int[]> parameterCombinations = GenerateCombinations(completeInteraction.Parameters.Count, count);
                foreach (var combinations in parameterCombinations)
                {
                    var interaction = new ParameterInteraction(combinations.Select((i) => completeInteraction.Parameters[i]));
                    var possibleValues = ParameterInteractionTable<T>.GenerateValueTable(model.Parameters, interaction);
                    foreach (var value in possibleValues)
                    {
                        interaction.Combinations
                            .Add(new ValueCombination(value, interaction) { State = ValueCombinationState.Covered });
                    }

                    // find combinations that should be excluded
                    var excludedCombinations = new List<ValueCombination>();
                    foreach (var combination in interaction.Combinations)
                    {
                        if (!combination.ParameterToValueMap.Any((p) => !completeInteraction.Parameters.Contains(p.Key))
                            && !allowedCombinations.Any((c) => MatchCombination(combination, c)))
                        {
                            excludedCombinations.Add(combination);
                        }
                    }

                    if (excludedCombinations.Count() > 0)
                    {
                        foreach (var combination in excludedCombinations)
                        {
                            combination.State = ValueCombinationState.Excluded;
                        }

                        MergeConstraintInteraction(order, interaction);
                    }
                }
            }
        }

        // do all the values in subset match with the whole
        public static bool MatchCombination(ValueCombination subSet, ValueCombination whole)
        {
            var wholeParameterMap = whole.ParameterToValueMap;
            var subSetParameterMap = subSet.ParameterToValueMap;
            foreach (var key in subSet.Keys)
            {
                if (!wholeParameterMap.ContainsKey(key) ||
                    subSetParameterMap[key] != wholeParameterMap[key])
                {
                    return false;
                }
            }

            return true;
        }

        // generate the n-wise interactions for the parameters
        private void GenerateInteractionsForParameters(Model<T> model, int order)
        {
            IList<int[]> parameterCombinations = GenerateCombinations(model.Parameters.Count, order);

            foreach (var parameterCombination in parameterCombinations)
            {
                Interactions.Add(new ParameterInteraction(parameterCombination));
            }

            GenerateValueCombinations(model);
        }

        // create all the values for each combination
        private void GenerateValueCombinations(Model<T> model)
        {
            for (int i = 0; i < Interactions.Count; i++)
            {
                var interaction = Interactions[i];
                var valueTable = GenerateValueTable(model.Parameters, interaction);

                for (int j = 0; j < valueTable.Count; j++)
                {
                    var values = valueTable[j];
                    var combination = new ValueCombination(values, interaction);
                    var tag = model.DefaultVariationTag;
                    double weight = 1.0;

                    interaction.Combinations.Add(combination);
                    for (int k = 0; k < values.Length; k++)
                    {
                        var value = model.Parameters[interaction.Parameters[k]].GetAt(values[k]);

                        if (value is ParameterValueBase)
                        {
                            var parameterValue = (ParameterValueBase)value;
                            if (parameterValue.Weight != 1.0)
                            {
                                weight = weight == 1.0 ? parameterValue.Weight : Math.Max(weight, parameterValue.Weight);
                            }

                            if (parameterValue.Tag != null && parameterValue.Tag != model.DefaultVariationTag)
                            {
                                if (tag != model.DefaultVariationTag)
                                {
                                    combination.State = ValueCombinationState.Excluded;
                                }
                                else
                                {
                                    tag = parameterValue.Tag;
                                }
                            }
                        }
                    }

                    combination.Weight = weight;
                    combination.Tag = tag;
                }
            }
        }

        // returns a table with all possible value combinations for the given interaction
        internal static List<int[]> GenerateValueTable(IList<ParameterBase> parameters, ParameterInteraction interaction)
        {

            List<int[]> possibleValueTable = new List<int[]>();
            for (int i = 0; i < interaction.Parameters.Count; i++)
            {
                possibleValueTable.Add(GenerateOrderedArray(parameters[interaction.Parameters[i]].Count));
            }

            int valueTableCount = possibleValueTable.Aggregate(1, (seed, array) => seed * array.Length);

            var table = new List<int[]>(valueTableCount);
            for (int i = 0; i < valueTableCount; i++)
            {
                table.Add(new int[possibleValueTable.Count]);
            }

            int[] repeats = new int[possibleValueTable.Count];
            for (int i = possibleValueTable.Count - 1; i >= 0; i--)
            {
                int repeat = 1;
                if (i < possibleValueTable.Count - 1)
                {
                    repeat = possibleValueTable[i + 1].Length * repeats[i + 1];
                }

                repeats[i] = repeat;
            }

            for (int i = 0; i < table.Count; i++)
            {
                for (int j = 0; j < table[i].Length; j++)
                {
                    table[i][j] = possibleValueTable[j][(i / repeats[j]) % possibleValueTable[j].Length];
                }
            }

            return table;
        }

        private static int[] GenerateOrderedArray(int count)
        {
            int[] value = new int[count];
            for (int i = 0; i < count; i++)
            {
                value[i] = i;
            }
            return value;
        }

        private static IList<int[]> GenerateCombinations(int totalElements, int combinationSize)
        {
            // calculate the number of combinations totalElements choose combinationSize
            // totalElements!/(combinationSize! * (totalElements - combinationSize)!)
            long totalCombinations = Factorial(totalElements) / (Factorial(combinationSize) * Factorial(totalElements - combinationSize));

            var combinations = new List<int[]>(totalCombinations > int.MaxValue ? int.MaxValue : (int)totalCombinations);

            for (int i = 0; i < totalCombinations; i++)
            {
                combinations.Add(new int[combinationSize]);
            }

            int[] currentCombination = GenerateOrderedArray(combinationSize);

            for (int i = 0; i < combinations.Count; i++)
            {
                currentCombination.CopyTo(combinations[i], 0);

                int startIndex = combinationSize - 1;
                for (; startIndex >= 0; startIndex--)
                {
                    if (currentCombination[startIndex] < totalElements - (combinationSize - startIndex))
                    {
                        break;
                    }
                }

                if (startIndex < 0)
                {
                    break;
                }

                int currentItem = currentCombination[startIndex];
                for (; startIndex < combinationSize; startIndex++)
                {
                    currentItem++;
                    currentCombination[startIndex] = currentItem;
                }
            }

            return combinations;
        }

        private static long Factorial(int n)
        {
            long total = 1;
            for (int i = 2; i <= n; i++)
            {
                total *= (long)i;
            }
            return total;
        }
    }
}
