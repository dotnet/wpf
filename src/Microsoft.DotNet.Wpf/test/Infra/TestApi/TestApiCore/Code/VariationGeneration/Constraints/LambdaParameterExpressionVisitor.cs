// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Linq.Expressions;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    class LambdaParameterExpressionVisitor : ExpressionVisitor
    {
        
        public LambdaParameterExpressionVisitor(ParameterExpression parameter)
        {
            this.parameter = parameter;
        }

        public Expression ReplaceParameter(Expression exp)
        {
            return Visit(exp);
        }

        ParameterExpression parameter;

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return this.parameter;
        }

        
    }
}
