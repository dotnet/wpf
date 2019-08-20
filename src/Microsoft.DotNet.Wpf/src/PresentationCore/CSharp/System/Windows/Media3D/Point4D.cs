// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 4D point implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//
//

using System.Windows;
using System.Windows.Media.Media3D;

using System;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Point4D - 4D point representation. 
    /// Defaults to (0,0,0,0).
    /// </summary>
    public partial struct Point4D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor that sets point's initial values.
        /// </summary>
        /// <param name="x">Value of the X coordinate of the new point.</param>
        /// <param name="y">Value of the Y coordinate of the new point.</param>
        /// <param name="z">Value of the Z coordinate of the new point.</param>
        /// <param name="w">Value of the W coordinate of the new point.</param>
        public Point4D(double x, double y, double z, double w)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Offset - update point position by adding deltaX to X, deltaY to Y, deltaZ to Z, and deltaW to W.
        /// </summary>
        /// <param name="deltaX">Offset in the X direction.</param>
        /// <param name="deltaY">Offset in the Y direction.</param>
        /// <param name="deltaZ">Offset in the Z direction.</param>
        /// <param name="deltaW">Offset in the W direction.</param>
        public void Offset(double deltaX, double deltaY, double deltaZ, double deltaW)
        {
            _x += deltaX;
            _y += deltaY;
            _z += deltaZ;
            _w += deltaW;
        }

        /// <summary>
        /// Addition.
        /// </summary>
        /// <param name="point1">First point being added.</param>
        /// <param name="point2">Second point being added.</param>
        /// <returns>Result of addition.</returns>
        public static Point4D operator +(Point4D point1, Point4D point2)
        {
            return new Point4D(point1._x + point2._x, 
                               point1._y + point2._y, 
                               point1._z + point2._z,
                               point1._w + point2._w);
        }

        /// <summary>
        /// Addition.
        /// </summary>
        /// <param name="point1">First point being added.</param>
        /// <param name="point2">Second point being added.</param>
        /// <returns>Result of addition.</returns>
        public static Point4D Add(Point4D point1, Point4D point2)
        {
            return new Point4D(point1._x + point2._x, 
                               point1._y + point2._y, 
                               point1._z + point2._z,
                               point1._w + point2._w);
        }
        
        /// <summary>
        /// Subtraction.
        /// </summary>
        /// <param name="point1">Point from which we are subtracting the second point.</param>
        /// <param name="point2">Point being subtracted.</param>
        /// <returns>Vector between the two points.</returns>
        public static Point4D operator -(Point4D point1, Point4D point2)
        {
            return new Point4D(point1._x - point2._x, 
                               point1._y - point2._y, 
                               point1._z - point2._z,
                               point1._w - point2._w);
        }

        /// <summary>
        /// Subtraction.
        /// </summary>
        /// <param name="point1">Point from which we are subtracting the second point.</param>
        /// <param name="point2">Point being subtracted.</param>
        /// <returns>Vector between the two points.</returns>
        public static Point4D Subtract(Point4D point1, Point4D point2)
        {
            return new Point4D(point1._x - point2._x, 
                               point1._y - point2._y, 
                               point1._z - point2._z,
                               point1._w - point2._w);
        }

        /// <summary>
        /// Point4D * Matrix3D multiplication.
        /// </summary>
        /// <param name="point">Point being transformed.</param>
        /// <param name="matrix">Transformation matrix applied to the point.</param>
        /// <returns>Result of the transformation matrix applied to the point.</returns>
        public static Point4D operator *(Point4D point, Matrix3D matrix)
        {
            return matrix.Transform(point);
        }

        /// <summary>
        /// Point4D * Matrix3D multiplication.
        /// </summary>
        /// <param name="point">Point being transformed.</param>
        /// <param name="matrix">Transformation matrix applied to the point.</param>
        /// <returns>Result of the transformation matrix applied to the point.</returns>
        public static Point4D Multiply(Point4D point, Matrix3D matrix)
        {
            return matrix.Transform(point);
        }

        #endregion Public Methods
    }
}
