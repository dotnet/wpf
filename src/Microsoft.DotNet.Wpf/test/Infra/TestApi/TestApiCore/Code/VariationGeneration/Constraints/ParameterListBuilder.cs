// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    internal class ParameterListBuilder : ExpressionVisitor
    {
        public ParameterListBuilder(Dictionary<Expression, CachedExpressionConstraintData> parameterMap, IEnumerable<ParameterBase> modelParameters, Type variationType)
        {
            this.parameterMap = parameterMap;
            this.modelParameters = modelParameters;
            this.variationType = variationType;
        }

        Dictionary<Expression, CachedExpressionConstraintData> parameterMap;
        IEnumerable<ParameterBase> modelParameters;
        Type variationType;

        List<ParameterBase> Parameters { get; set; }
        public List<ParameterBase> GetParameters(Expression expression)
        {
            Parameters = new List<ParameterBase>();
            Visit(expression);
            parameterMap[expression] = new CachedExpressionConstraintData
            {
                Parameters = this.Parameters
            };

            return Parameters;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            if (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse)
            {
                var parametersLeft = new ParameterListBuilder(parameterMap, modelParameters, variationType).GetParameters(b.Left);
                var parametersRight = new ParameterListBuilder(parameterMap, modelParameters, variationType).GetParameters(b.Right);

                MergeParameterLists(parametersLeft);
                MergeParameterLists(parametersRight);
                return b;
            }
            else
            {
                return base.VisitBinary(b);
            }
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            var parametersTest = new ParameterListBuilder(parameterMap, modelParameters, variationType).GetParameters(c.Test);
            var parametersIfTrue = new ParameterListBuilder(parameterMap, modelParameters, variationType).GetParameters(c.IfTrue);
            var parametersIfFalse = new ParameterListBuilder(parameterMap, modelParameters, variationType).GetParameters(c.IfFalse);

            MergeParameterLists(parametersTest);
            MergeParameterLists(parametersIfTrue);
            MergeParameterLists(parametersIfFalse);
            return c;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Expression.Type == variationType)
            {
                ParameterBase parameter = modelParameters.FirstOrDefault(p => p.Name == m.Member.Name);
                if (parameter != null && !Parameters.Contains(parameter))
                {
                    Parameters.Add(parameter);
                }
            }
            return m;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (CheckForGetValueCall(m.Method))
            {
                var parameter = Expression.Lambda<Func<ParameterBase>>(m.Object).Compile()();

                if (!Parameters.Contains(parameter))
                {
                    Parameters.Add(parameter);
                }

                return m;
            }
            else
            {
                return base.VisitMethodCall(m);
            }
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            if (p.Type == variationType)
            {
                throw new InvalidOperationException("The Variation parameter can only be passed to calls of Parameter<>.GetValue.");
            }

            return base.VisitParameter(p);
        }

        bool CheckForGetValueCall(MethodInfo method)
        {
            if (!method.DeclaringType.IsGenericType)
            {
                return false;
            }

            var genericArgs = method.DeclaringType.GetGenericArguments();

            if (genericArgs.Length != 1)
            {
                return false;
            }

            var expectedType = typeof(Parameter<>).MakeGenericType(genericArgs[0]);

            return method == expectedType.GetMethod("GetValue");
        }

        void MergeParameterLists(IList<ParameterBase> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (!Parameters.Contains(candidate))
                {
                    Parameters.Add(candidate);
                }
            }
        }
    }
}
