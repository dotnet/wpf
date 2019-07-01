// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Pairs a combination in consideration for addition to a variation with how many combinations it will cover
    /// </summary>
    internal class CandidateCoverage
    {
        public ValueCombination Value { get; set; }
        public int CoverageCount { get; set; }
    }
}
