// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Microsoft.Test.VariationGeneration
{
    /// <summary>
    /// Represents a single value in a parameter.
    /// </summary>
    public abstract class ParameterValueBase
    {
        /// <summary>
        /// Tags the value with a user-defined expected result. 
        /// At most, one tagged value appears in a variation. The default is null.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// A value that indicates whether this value should be chosen more or less frequently. 
        /// Larger values are chosen more often. The default is 1.0.
        /// </summary>
        /// <remarks>
        /// Weighting creates preferences for certain values. Because of the nature of the algorithm used, 
        /// the actual weight has no intrinsic meaning (weighting one value at 10.0 and the others at 1.0 
        /// does not mean the first value appears 10 times more often). The primary goal of the algorithm 
        /// is to cover all the combinations while using the fewest possible test cases, which often 
        /// contradicts the preference for honoring the weight. Weight acts as a tie breaker when candidate 
        /// values cover the same number of combinations.  
        /// </remarks>
        public double Weight { get; set; }

        /// <summary>
        /// Returns the value that this ParameterValue represents.
        /// </summary>
        /// <returns>The value.</returns>
        public abstract object GetValue();
    }
}
