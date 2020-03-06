// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Performs validations for parameters and throws appropriate exceptions.
    /// </summary>
    internal static class Validations
    {
        /// <summary>
        /// Throws if an argument is null.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void CheckNotNull(object value, string paramName)
        {
            Debug.Assert(paramName != null);
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Gets whether the specified value is finite.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// Throws unless the specified value is finite.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void CheckFinite(float value, string paramName)
        {
            Debug.Assert(paramName != null);
            if (!IsFinite(value))
            {
                throw Exceptions.ValueMustBeFinite(paramName, value);
            }
        }

        /// <summary>
        /// Gets whether the specified value is either NaN or finite.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsFiniteOrNaN(float value)
        {
            return float.IsNaN(value) || !float.IsInfinity(value);
        }

        /// <summary>
        /// Throws unless the specified value is finite or NaN.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void CheckFiniteOrNaN(float value, string paramName)
        {
            Debug.Assert(paramName != null);
            if (!IsFiniteOrNaN(value))
            {
                throw Exceptions.ValueMustBeFiniteOrNaN(paramName, value);
            }
        }

        /// <summary>
        /// Gets whether the specified value is finite and non-negative.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsFiniteNonNegative(float value)
        {
            return !float.IsInfinity(value) && !float.IsNaN(value) && (value >= 0);
        }

        /// <summary>
        /// Throws unless the specified value is finite and non-negative.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        public static void CheckFiniteNonNegative(float value, string paramName)
        {
            if (!IsFiniteNonNegative(value))
            {
                throw Exceptions.ValueMustBeFiniteNonNegative(paramName, value);
            }
        }
    }
}