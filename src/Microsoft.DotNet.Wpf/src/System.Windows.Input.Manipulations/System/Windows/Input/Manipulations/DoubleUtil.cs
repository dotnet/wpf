// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// This file contains the implementation of DoubleUtil, which
// provides "fuzzy" comparison functionality for doubles.
// The file is based on the similar util class from the Avalon tree.

namespace System.Windows.Input.Manipulations
{
    internal static class DoubleUtil
    {
        // Const values come from sdk\inc\crt\float.h
        private const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */

        /// <summary>
        /// Verifies if the given value is close to 0.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool IsZero(double d)
        {
            // IsZero(d) check can be used to make sure that dividing by 'd' will never produce Infinity.
            // use DBL_EPSILON instead of double.Epsilon because double.Epsilon is too small and doesn't guarantee that.
            return Math.Abs(d) <= DBL_EPSILON;
        }

        /// <summary>
        /// Limits a value to the given internal.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Limit(double d, double min, double max)
        {
            if (!double.IsNaN(max) && d > max)
            {
                return max;
            }
            if (!double.IsNaN(min) && d < min)
            {
                return min;
            }

            return d;
        }
    }
}
