// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Diagnostics.CodeAnalysis;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents the possible affine two-dimensional (2-D) manipulations.
    /// </summary>
    [Flags]
    [SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification="Name is plural")]
    public enum Manipulations2D
    {
        /// <summary> No manipulations. </summary>
        None = 0,
        /// <summary> A translation in the x-axis. </summary>
        TranslateX = 1,
        /// <summary> A translation in the y-axis. </summary>
        TranslateY = 2,
        /// <summary> A translation in the x and/or y axes. </summary>
        Translate = TranslateX | TranslateY,
        /// <summary> A scale in both directions. </summary>
        Scale = 4,
        /// <summary> A rotation. </summary>
        Rotate = 8,
        /// <summary> All available manipulations. </summary>
        All = Translate | Rotate | Scale
    }

    /// <summary>
    /// A static class used for useful extension methods to Manipulations2D.
    /// </summary>
    internal static class Manipulations2DUtil
    {
        /// <summary>
        /// Extension method that determines whether a Manipulations2D
        /// falls within the range of allowed values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsValid(this Manipulations2D value)
        {
            int maskedValue = (int)value & ~(int)Manipulations2D.All;
            return maskedValue == 0;
        }

        /// <summary>
        /// Validates the specified value against the possible range of values
        /// for Manipulations2D. Throws an ArgumentOutOfRangeException
        /// listing the specified property name if the value is out of range.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="property"></param>
        public static void CheckValue(this Manipulations2D value, string property)
        {
            if (!value.IsValid())
            {
                throw Exceptions.ArgumentOutOfRange(property, value);
            }
        }

        /// <summary>
        /// Gets whether one or more of the specified manipulations are supported.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="supported"></param>
        /// <returns></returns>
        public static bool SupportsAny(this Manipulations2D value, Manipulations2D supported)
        {
            return (value & supported) != 0;
        }
    }
}
