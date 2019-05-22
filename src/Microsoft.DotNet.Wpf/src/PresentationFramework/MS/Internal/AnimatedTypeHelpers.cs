// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MS.Internal.PresentationFramework
{
    internal static class AnimatedTypeHelpers
    {
        #region Interpolation Methods

        /// <summary>
        /// To support "By" animations and to make our internal code easily readable
        /// this method will interpolate between Auto and non-Auto values treating 
        /// the Auto value essentially as a zero value for whatever the UnitType
        /// of the other value is via the fact that Auto returns 0.0 for the Value
        /// property rather than throwing an exception as a Nullable would.
        /// 
        /// If both values are Auto, we return Auto.
        /// </summary>
        /// <param name="from">The from value.</param>
        /// <param name="to">The to value.</param>
        /// <param name="progress">The progress value used for interpolation.</param>
        /// <returns>The interpolated value.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if one of the values is a percent and the other is not.
        /// </exception>
        private static Double InterpolateDouble(Double from, Double to, Double progress)
        {
            return from + ((to - from) * progress);
        }

        internal static Thickness InterpolateThickness(Thickness from, Thickness to, double progress)
        {
            return new Thickness(
                InterpolateDouble(from.Left,   to.Left,   progress),
                InterpolateDouble(from.Top,    to.Top,    progress),
                InterpolateDouble(from.Right,  to.Right,  progress),
                InterpolateDouble(from.Bottom, to.Bottom, progress));
        }

        #endregion

        #region Add Methods

        private static Double AddDouble(Double value1, Double value2)
        {
            return value1 + value2;
        }

        internal static Thickness AddThickness(Thickness value1, Thickness value2)
        {
            return new Thickness(
                AddDouble(value1.Left,   value2.Left),
                AddDouble(value1.Top,    value2.Top),
                AddDouble(value1.Right,  value2.Right),
                AddDouble(value1.Bottom, value2.Bottom));
        }

        #endregion

        #region Subtract Methods

        internal static Thickness SubtractThickness(Thickness value1, Thickness value2)
        {
            return new Thickness(
                value1.Left - value2.Left,
                value1.Top - value2.Top,
                value1.Right - value2.Right,
                value1.Bottom - value2.Bottom);
        }

        #endregion

        #region GetSegmentLength Methods

        private static Double GetSegmentLengthDouble(Double from, Double to)
        {
            return Math.Abs(to - from);
        }

        internal static double GetSegmentLengthThickness(Thickness from, Thickness to)
        {
            double totalLength = 
                  Math.Pow(GetSegmentLengthDouble(from.Left,   to.Left),   2.0)
                + Math.Pow(GetSegmentLengthDouble(from.Top,    to.Top),    2.0)
                + Math.Pow(GetSegmentLengthDouble(from.Right,  to.Right),  2.0)
                + Math.Pow(GetSegmentLengthDouble(from.Bottom, to.Bottom), 2.0);

            return Math.Sqrt(totalLength);
        }

        #endregion

        #region Scale Methods

        private static Double ScaleDouble(Double value, Double factor)
        {
            return value * factor;
        }

        internal static Thickness ScaleThickness(Thickness value, double factor)
        {
            return new Thickness(
                ScaleDouble(value.Left,   factor),
                ScaleDouble(value.Top,    factor),
                ScaleDouble(value.Right,  factor),
                ScaleDouble(value.Bottom, factor));
        }

        #endregion

        #region IsValidAnimationValue Methods

        private static bool IsValidAnimationValueDouble(Double value)
        {
            if (IsInvalidDouble(value))
            {
                return false;
            }

            return true;
        }

        internal static bool IsValidAnimationValueThickness(Thickness value)
        {
            // At least one of the sub-values must be an interpolatable length.
            if (   IsValidAnimationValueDouble(value.Left)
                || IsValidAnimationValueDouble(value.Top)
                || IsValidAnimationValueDouble(value.Right)
                || IsValidAnimationValueDouble(value.Bottom))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region GetZeroValueMethods

        private static Double GetZeroValueDouble(Double baseValue)
        {
            return 0.0;
        }

        internal static Thickness GetZeroValueThickness(Thickness baseValue)
        {
            return new Thickness(
                GetZeroValueDouble(baseValue.Left),
                GetZeroValueDouble(baseValue.Top),
                GetZeroValueDouble(baseValue.Right),
                GetZeroValueDouble(baseValue.Bottom));
        }

        #endregion

        #region Helpers

        private static bool IsInvalidDouble(double value)
        {
            return Double.IsInfinity(value)
                || DoubleUtil.IsNaN(value);
        }

        #endregion
    }
}
