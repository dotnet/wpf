// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.Generic;

namespace Microsoft.Test.VariationGeneration.Constraints
{
    class CachedExpressionConstraintData
    {
        public IList<ParameterBase> Parameters { get; set; }
        public ParameterInteraction Interaction { get; set; }
    }
}
