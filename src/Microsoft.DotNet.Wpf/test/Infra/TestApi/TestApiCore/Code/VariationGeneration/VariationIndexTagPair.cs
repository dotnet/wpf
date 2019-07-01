// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Contains the indices of variation and the corresponding tag.  Used to build the actual Variation.
    /// </summary>
    internal class VariationIndexTagPair
    {
        public int[] Indices { get; set; }
        public object Tag { get; set; }
    }
}
