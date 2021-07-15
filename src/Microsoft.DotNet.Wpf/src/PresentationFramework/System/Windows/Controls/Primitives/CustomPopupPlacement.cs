// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     Describes where a popup should be placed on screen.
    /// </summary>
    public struct CustomPopupPlacement
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="point">Assigns to Point</param>
        /// <param name="primaryAxis">Assigns to PrimaryAxis</param>
        public CustomPopupPlacement(Point point, PopupPrimaryAxis primaryAxis)
        {
            _point = point;
            _primaryAxis = primaryAxis;
        }

        /// <summary>
        ///     The point, relative to the PlacementTarget, where the upper left corner of the Popup should be.
        /// </summary>
        public Point Point
        {
            get
            {
                return _point;
            }

            set
            {
                _point = value;
            }
        }

        /// <summary>
        ///     The primary axis of the popup that will be used for nudging on-screen.
        /// </summary>
        public PopupPrimaryAxis PrimaryAxis
        {
            get
            {
                return _primaryAxis;
            }

            set
            {
                _primaryAxis = value;
            }
        }

        /// <summary>
        ///     Compares the value of two CustomPopupPlacement structs for equality.
        /// </summary>
        /// <param name="placement1">The first value.</param>
        /// <param name="placement2">The second value.</param>
        /// <returns></returns>
        public static bool operator==(CustomPopupPlacement placement1, CustomPopupPlacement placement2)
        {
            return placement1.Equals(placement2);
        }

        /// <summary>
        ///     Compares the value of two CustomPopupPlacement structs for inequality.
        /// </summary>
        /// <param name="placement1">The first value.</param>
        /// <param name="placement2">The second value.</param>
        /// <returns></returns>
        public static bool operator !=(CustomPopupPlacement placement1, CustomPopupPlacement placement2)
        {
            return !placement1.Equals(placement2);
        }

        /// <summary>
        ///     Compares the value of this struct with another object.
        /// </summary>
        /// <param name="o">An object to compare to.</param>
        /// <returns>True if equivalent. False otherwise.</returns>
        public override bool Equals(object o)
        {
            if (o is CustomPopupPlacement)
            {
                CustomPopupPlacement placement = (CustomPopupPlacement)o;
                return (placement._primaryAxis == _primaryAxis) && (placement._point == _point);
            }

            return false;
        }

        /// <summary>
        ///     Hash function for this type.
        /// </summary>
        /// <returns>A hash code for this struct.</returns>
        public override int GetHashCode()
        {
            return _primaryAxis.GetHashCode() ^ _point.GetHashCode();
        }

        private Point _point;
        private PopupPrimaryAxis _primaryAxis;
    }
}
