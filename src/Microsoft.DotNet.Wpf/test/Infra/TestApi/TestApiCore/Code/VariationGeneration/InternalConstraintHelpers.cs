// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Diagnostics;

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Helper function for determining relations between internal constraint tables.
    /// </summary>
    internal static class InternalConstraintHelpers
    {
        // helper to implement Constraint.SatisfiesConstraint
        internal static ConstraintSatisfaction SatisfiesContraint<T>(Model<T> model, ValueCombination combination, ParameterInteraction interaction) where T : new()
        {
            Debug.Assert(model != null && combination != null && interaction != null);

            var parameterMap = combination.ParameterToValueMap;
            for(int i = 0; i < interaction.Parameters.Count; i++)
            {
                if (!parameterMap.ContainsKey(interaction.Parameters[i]))
                {
                    return ConstraintSatisfaction.InsufficientData;
                }
            }

            for(int i = 0; i < interaction.Combinations.Count; i++)
            {
                if (ParameterInteractionTable<T>.MatchCombination(interaction.Combinations[i], combination))
                {
                    if (interaction.Combinations[i].State == ValueCombinationState.Excluded)
                    {
                        return ConstraintSatisfaction.Unsatisfied;
                    }
                }
            }

            return ConstraintSatisfaction.Satisfied;
        }        
    }
}
