// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    /// <summary>
    /// Provides a constraint that uses expressions to determine the validity of a variation in a model.
    /// </summary>
    public class ConditionalConstraint<T> : Constraint<T> where T : new()
    {
        /// <summary>
        /// Initializes a new ConditionalConstraint(T) instance.
        /// </summary>
        public ConditionalConstraint()
        {
        }

        /// <summary>
        /// Initializes a new ConditionalConstraint(T) instance.
        /// </summary>
        /// <param name="condition">The condition.</param>
        public ConditionalConstraint(Expression<Func<T, bool>> condition)
        {
            Condition = condition;
        }

        /// <summary>
        /// An expression that takes a variation as input and returns true if it is valid.
        /// </summary>
        public Expression<Func<T, bool>> Condition { get; set; }

        internal override ParameterInteraction GetExcludedCombinations(Model<T> model)
        {
            if (CachedInteraction != null)
            {
                return new ParameterInteraction(CachedInteraction);
            }

            parameters = new ParameterListBuilder(mapExpressionsToRequiredParams, model.Parameters, typeof(T)).GetParameters(Condition);
            
            CachedInteraction = CreateInteraction(model, Condition, parameters, Condition.Parameters[0]);

            foreach (var item in mapExpressionsToRequiredParams)
            {
                if (item.Key == Condition)
                {
                    mapExpressionsToRequiredParams[item.Key].Interaction = CachedInteraction;
                    continue;
                }

                mapExpressionsToRequiredParams[item.Key].Interaction = CreateInteraction(model, item.Key, item.Value.Parameters, Condition.Parameters[0]);                
            }
            
            return CachedInteraction;
        }

        static ParameterInteraction CreateInteraction(Model<T> model, Expression expression, IList<ParameterBase> parameters, ParameterExpression parameterExpr)
        {
            
            Func<T, bool> filter = expression is Expression<Func<T, bool>> ?
                ((Expression<Func<T, bool>>)expression).Compile() :
                Expression.Lambda<Func<T, bool>>(expression, parameterExpr).Compile();
            

            var parameterIndices = (from parameter in parameters
                                    select model.Parameters.IndexOf(parameter)).ToList();

            parameterIndices.Sort();

            ParameterInteraction interaction = new ParameterInteraction(parameterIndices);
            List<int[]> valueTable = ParameterInteractionTable<T>.GenerateValueTable(model.Parameters, interaction);

            foreach (var valueIndices in valueTable)
            {
                ValueCombinationState comboState = filter(BuildVariation(model, parameterIndices, valueIndices)) ? ValueCombinationState.Covered : ValueCombinationState.Excluded;
                interaction.Combinations.Add(new ValueCombination(valueIndices, interaction) { State = comboState });
            }

            return interaction;
        }

        internal override ConstraintSatisfaction SatisfiesContraint(Model<T> model, ValueCombination combination)
        {
            if (CachedInteraction == null)
            {
                GetExcludedCombinations(model);
            }

            if(CachedInteraction.Parameters.Any((index) => combination.ParameterToValueMap.ContainsKey(index)))
            {
                // supplied combination is a superset
                if (CachedInteraction.Parameters.All((index) => combination.ParameterToValueMap.ContainsKey(index)))
                {
                    var combo = CachedInteraction.Combinations.First((c) => ParameterInteractionTable<T>.MatchCombination(c, combination));
                    return combo.State == ValueCombinationState.Excluded ? ConstraintSatisfaction.Unsatisfied : ConstraintSatisfaction.Satisfied;
                }
                else
                {
                    return new ConstraintSatisfactionExpressionVisitor(mapExpressionsToRequiredParams).SatisfiesConstraint(Condition, model, combination);
                }
            }
            else
            {
                // supplied combination is disjoint
                return ConstraintSatisfaction.InsufficientData;
            }
        }

        internal override void ClearCache()
        {
            CachedInteraction = null;
        }

        static T BuildVariation(Model<T> model, IList<int> parameterIndices, int[] valueIndices)
        {
            Variation v = new Variation();
            for (int i = 0; i < parameterIndices.Count; i++)
            {
                var parameter = model.Parameters[parameterIndices[i]];
                v[parameter.Name] = ((ParameterValueBase)parameter.GetAt(valueIndices[i])).GetValue();
            }

            if (typeof(T) == typeof(Variation))
            {
                return (T)((object)v);
            }

            return new VariationsWrapper<T>(model.propertiesMap, null).AssignParameterValues(v);

        }

        private Dictionary<Expression, CachedExpressionConstraintData> mapExpressionsToRequiredParams = new Dictionary<Expression, CachedExpressionConstraintData>();
        private List<ParameterBase> parameters;
    }
}
