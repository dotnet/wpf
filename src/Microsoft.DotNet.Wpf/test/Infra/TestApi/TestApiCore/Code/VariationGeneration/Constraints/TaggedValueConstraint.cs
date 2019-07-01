// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    /// <summary>
    /// Tagging values in a model creates implicit constraints (only one tagged value per variation).  This class
    /// explicitly defines those constraints.
    /// </summary>
    internal class TaggedValueConstraint<T> : Constraint<T> where T : new()
    {
        internal override ParameterInteraction GetExcludedCombinations(Model<T> model)
        {
            if (CachedInteraction == null)
            {
                var taggedValues = GetParametersWithTaggedValues(model);
                CachedInteraction = new ParameterInteraction(taggedValues.Select((p) => p.ParameterIndex));
                var combinationIndices = ParameterInteractionTable<T>.GenerateValueTable(model.Parameters, CachedInteraction);
                foreach (var combinationIndex in combinationIndices)
                {
                    int tagCount = 0;
                    ValueCombination combination = new ValueCombination(combinationIndex, CachedInteraction);
                    combination.State = ValueCombinationState.Covered;

                    for (int i = 0; i < taggedValues.Count; i++)
                    {
                        if (taggedValues[i].ValueIndices.BinarySearch(combinationIndex[i]) >= 0)
                        {
                            tagCount++;
                        }

                        if (tagCount > 1)
                        {
                            break;
                        }
                    }

                    if (tagCount > 1)
                    {
                        combination.State = ValueCombinationState.Excluded;
                    }

                    CachedInteraction.Combinations.Add(combination);
                }
            }

            return new ParameterInteraction(CachedInteraction);
        }

        
        internal override ConstraintSatisfaction SatisfiesContraint(Model<T> model, ValueCombination combination)
        {
            if (CachedInteraction == null)
            {
                GetExcludedCombinations(model);
            }

            return InternalConstraintHelpers.SatisfiesContraint(model, combination, CachedInteraction);
        }

        internal override void ClearCache()
        {
            CachedInteraction = null;
        }

        private static IList<ParamaterAndValueIndices> GetParametersWithTaggedValues(Model<T> model)
        {
            var indices = new List<ParamaterAndValueIndices>();
            for (int i = 0; i < model.Parameters.Count; i++)
            {
                ParamaterAndValueIndices index = null;
                for (int j = 0; j < model.Parameters[i].Count; j++)
                {
                    if(IsTagged(model.Parameters[i].GetAt(j), model.DefaultVariationTag))
                    {
                        if(index == null)
                        {
                            index = new ParamaterAndValueIndices();
                            index.ParameterIndex = i;
                        }

                        index.ValueIndices.Add(j);
                    }
                }

                if (index != null)
                {
                    indices.Add(index);
                }
            }

            return indices;
        }

        private static bool IsTagged(object value, object defaultTag)
        {
            return value is ParameterValueBase &&
                ((ParameterValueBase)value).Tag != null &&
                ((ParameterValueBase)value).Tag != defaultTag;
        }

        private class ParamaterAndValueIndices
        {
            public int ParameterIndex { get; set; }

            private List<int> valueIndices = new List<int>();
            public List<int> ValueIndices { get { return valueIndices; } }
        }

    }
}
