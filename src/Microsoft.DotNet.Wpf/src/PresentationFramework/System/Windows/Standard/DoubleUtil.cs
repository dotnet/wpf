// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Standard
{
    /// <summary>
    /// DoubleUtil uses fixed eps to provide fuzzy comparison functionality for doubles.
    /// Note that FP noise is a big problem and using any of these compare 
    /// methods is not a complete solution, but rather the way to reduce 
    /// the probability of repeating unnecessary work.
    /// </summary>
    internal static class DoubleUtilities
    {
        /// <summary>
        /// Epsilon - more or less random, more or less small number.
        /// </summary>
        private const double Epsilon = 0.00000153;

        /// <summary>
        /// AreClose returns whether or not two doubles are "close".  That is, whether or 
        /// not they are within epsilon of each other.
        /// There are plenty of ways for this to return false even for numbers which
        /// are theoretically identical, so no code calling this should fail to work if this 
        /// returns false. 
        /// </summary>
        /// <param name="value1">The first double to compare.</param>
        /// <param name="value2">The second double to compare.</param>
        /// <returns>The result of the AreClose comparision.</returns>
        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
            {
                return true;
            }

            double delta = value1 - value2;

            return delta is < Epsilon and > (-Epsilon);
        }
    }
}
