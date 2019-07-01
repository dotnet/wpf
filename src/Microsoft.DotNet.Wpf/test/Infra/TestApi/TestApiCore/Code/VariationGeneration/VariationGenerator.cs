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
    /// The core generation engine. Takes in a model and transforms it into a table of all possible values that
    /// need to be covered or excluded. Uses that table to create variations and transforms that into the public
    /// <see cref="Variation"/>
    /// </summary>
    internal static class VariationGenerator
    {
        /// <summary>
        /// Entry point to VariationGenerator
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="order">The order of the parameters (pairwise, 3-wise, etc)</param>
        /// <param name="seed">Random seed to use</param>
        /// <returns>Generated Variations</returns>
        public static IEnumerable<Variation> GenerateVariations<T>(Model<T> model, int order, int seed) where T : new()
        {
            // calculate the number variations to exhaustively test the model, 
            // useful to determine if something is wrong during generation
            long maxVariations = model.Parameters.Aggregate((long)1, (total, next) => total * (long)next.Count);
            var variationIndices = GenerateVariationIndices(Prepare(model, order),model.Parameters.Count, seed, maxVariations, model.DefaultVariationTag);

            return from v in variationIndices
                   select IndicesToVariation(model, v);
        }

        // generate all the values to cover or exclude
        private static ParameterInteractionTable<T> Prepare<T>(Model<T> model, int order) where T : new()
        {
            return new ParameterInteractionTable<T>(model, order);
        }

        // this is the actual generation function
        // returns a list of indices that allow lookup of the actual value in the model
        private static IList<VariationIndexTagPair> GenerateVariationIndices<T>(ParameterInteractionTable<T> interactions, int variationSize, int seed, long maxVariations, object defaultTag) where T : new()
        {
            Random random = new Random(seed);
            List<VariationIndexTagPair> variations = new List<VariationIndexTagPair>();

            // while there a uncovered values
            while (!interactions.IsCovered())
            {
                int[] candidate = new int[variationSize];
                object variationTag = defaultTag;

                // this is a scatch variable so new arrays won't be allocated for every candidate
                int[] proposedCandidate = new int[variationSize];
                for (int i = 0; i < candidate.Length; i++)
                {
                    // -1 indicates an empty slot
                    candidate[i] = -1;
                }

                IEnumerable<ParameterInteraction> candidateInteractions = interactions.Interactions;
                // while there are empty slots
                while (candidate.Any((i) => i == -1))
                {
                    // if all the slots are empty
                    if (candidate.All((i) => i == -1))
                    {
                        // then pick the first uncovered combination from the most uncovered parameter interaction
                        int mostUncovered =
                            interactions.Interactions.Max((i) => i.GetUncoveredCombinationsCount());

                        var interaction = interactions.Interactions.First((i) => i.GetUncoveredCombinationsCount() == mostUncovered);
                        var combination = interaction.Combinations.First((c) => c.State == ValueCombinationState.Uncovered);

                        foreach (var valuePair in combination.ParameterToValueMap)
                        {
                            candidate[valuePair.Key] = valuePair.Value;
                        }

                        variationTag = combination.Tag == null || combination.Tag == defaultTag ? variationTag : combination.Tag;
                        combination.State = ValueCombinationState.Covered;
                    }
                    else
                    {
                        // find interactions that aren't covered by the current candidate variation
                        var incompletelyCoveredInteractions =
                            from interaction in candidateInteractions
                            where interaction.Parameters.Any((i) => candidate[i] == -1)
                            select interaction;

                        candidateInteractions = incompletelyCoveredInteractions;
                        // find values that can be added to the current candidate
                        var compatibleValues = new List<ValueCombination>();
                        foreach (var interaction in incompletelyCoveredInteractions)
                        {
                            foreach (var combination in interaction.Combinations)
                            {
                                if (IsCompatibleValue(combination, candidate))
                                {
                                    compatibleValues.Add(combination);
                                }
                            }
                        }

                        // get the uncovered values
                        var uncoveredValues = compatibleValues.Where((v) => v.State == ValueCombinationState.Uncovered).ToList();

                        // calculate what the candidate will look like if add an uncovered value
                        var proposedCandidates = new List<CandidateCoverage>();
                        foreach (var uncoveredValue in uncoveredValues)
                        {
                            CreateProposedCandidate(uncoveredValue, candidate, proposedCandidate);

                            if (!IsExcluded(interactions.ExcludedCombinations, proposedCandidate))
                            {
                                var coverage = new CandidateCoverage
                                {
                                    Value = uncoveredValue,
                                    CoverageCount = uncoveredValues.Count((v) => IsCovered(v, proposedCandidate)),
                                };

                                proposedCandidates.Add(coverage);
                            }

                        }                            

                        // if any of the proposed candidates isn't exclude
                        if (proposedCandidates.Count > 0)
                        {
                            // find the value that will cover the most combinations
                            int maxCovered = proposedCandidates.Max((c) => c.CoverageCount);
                            double maxWeight = proposedCandidates.Where((c) => c.CoverageCount == maxCovered).Max((c) => c.Value.Weight);
                            ValueCombination proposedValue = proposedCandidates.First((c) => c.CoverageCount == maxCovered && c.Value.Weight == maxWeight).Value;

                            // add this value to candidate and mark all values as such
                            foreach (var valuePair in proposedValue.ParameterToValueMap)
                            {
                                candidate[valuePair.Key] = valuePair.Value;
                            }

                            variationTag = proposedValue.Tag == null || proposedValue.Tag == defaultTag ? variationTag : proposedValue.Tag;

                            // get the newly covered values so they can be marked
                            var newlyCoveredValue = uncoveredValues.Where((v) => IsCovered(v, candidate)).ToList();

                            foreach (var value in newlyCoveredValue)
                            {
                                value.State = ValueCombinationState.Covered;
                            }
                        }
                        else
                        {
                            // no uncovered values can be added with violating a constraint, add a random covered value
                            var compatibleWeightBuckets = compatibleValues.GroupBy((v) => v.Weight).OrderByDescending((v) => v.Key);
                            ValueCombination value = null;
                            bool combinationFound = false;
                            foreach (var bucket in compatibleWeightBuckets)
                            {
                                int count = bucket.Count();
                                int attempts = 0;

                                do
                                {
                                    value = bucket.ElementAt(random.Next(count - 1));
                                    CreateProposedCandidate(value, candidate, proposedCandidate);

                                    if (!interactions.ExcludedCombinations.Any((c) => IsCovered(c, proposedCandidate)))
                                    {
                                        combinationFound = true;
                                    }

                                    attempts++;

                                    // this is a heuristic, since we're pulling random values just going to count probably
                                    // means we've attempted duplicates, going to 2 * count means we've probably tried
                                    // everything at least once
                                    if (attempts > count * 2)
                                    {
                                        break;
                                    }
                                }
                                while (!combinationFound);

                                if (combinationFound)
                                {
                                    break;
                                }
                            }

                            if (!combinationFound)
                            {
                                throw new InternalVariationGenerationException("Unable to find candidate with no exclusions.");
                            }

                            // add this value to candidate and mark all values as such

                            foreach (var valuePair in value.ParameterToValueMap)
                            {
                                candidate[valuePair.Key] = valuePair.Value;
                            }

                            variationTag = value.Tag == null || value.Tag == defaultTag ? variationTag : value.Tag;
                        }
                    }
                }

                variations.Add(new VariationIndexTagPair { Indices = candidate, Tag = variationTag });

                // more variations than are need to exhaustively test the model have been adde
                if (variations.Count > maxVariations)
                {
                    throw new InternalVariationGenerationException("More variations than an exhaustive suite produced.");
                }
            }

            return variations;
        }

        // is this value covered by the candidate
        private static bool IsCovered(ValueCombination value, int[] candidate)
        {
            foreach (int i in value.Keys)
            {
                if (candidate[i] != value.ParameterToValueMap[i])
                {
                    return false;
                }
            }

            return true;
        }

        // does this candidate violate any constraints
        private static bool IsExcluded(IEnumerable<ValueCombination> excludedValues, int[] candidate)
        {
            return excludedValues.Any((c) => IsCovered(c, candidate));
        }

        // add this value to the candidate
        private static void CreateProposedCandidate(ValueCombination value, int[] baseCandidate, int[] proposed)
        {
            baseCandidate.CopyTo(proposed, 0);

            foreach (var valuePair in value.ParameterToValueMap)
            {
                proposed[valuePair.Key] = valuePair.Value;
            }
        }

        // can this value be added to this candidate
        private static bool IsCompatibleValue(ValueCombination value, int[] candidate)
        {
            foreach (int i in value.Keys)
            {
                if (candidate[i] != value.ParameterToValueMap[i] && candidate[i] != -1)
                {
                    return false;
                }
            }

            return true;
        }

        // map value indices to actual values and create a Variation
        private static Variation IndicesToVariation<T>(Model<T> model, VariationIndexTagPair pair) where T : new ()
        {
            Variation v = new Variation(pair.Tag);

            for (int i = 0; i < pair.Indices.Length; i++)
            {
                var value = model.Parameters[i].GetAt(pair.Indices[i]) is ParameterValueBase ?
                    ((ParameterValueBase)model.Parameters[i].GetAt(pair.Indices[i])).GetValue() : model.Parameters[i].GetAt(pair.Indices[i]);
                v.Add(model.Parameters[i].Name, value);
            }

            return v;
        }
    }
}
