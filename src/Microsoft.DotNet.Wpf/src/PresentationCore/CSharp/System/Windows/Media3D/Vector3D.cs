// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D vector implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//

using MS.Internal;
using MS.Internal.Media3D;
using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// Vector3D - 3D vector representation.
    /// </summary>
    public partial struct Vector3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor that sets vector's initial values.
        /// </summary>
        /// <param name="x">Value of the X coordinate of the new vector.</param>
        /// <param name="y">Value of the Y coordinate of the new vector.</param>
        /// <param name="z">Value of the Z coordinate of the new vector.</param>
        public Vector3D(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Length of the vector.
        /// </summary>
        public double Length 
        { 
            get
            {
                return Math.Sqrt(_x * _x + _y * _y + _z * _z);
            } 
        }  

        /// <summary>
        /// Length of the vector squared.
        /// </summary>
        public double LengthSquared 
        { 
            get
            {
                return _x * _x + _y * _y + _z * _z;
            }
        }
  
        /// <summary>
        /// Updates the vector to maintain its direction, but to have a length
        /// of 1. Equivalent to dividing the vector by its Length.
        /// Returns NaN if length is zero.
        /// </summary>
        public void Normalize()
        {
            // Computation of length can overflow easily because it
            // first computes squared length, so we first divide by
            // the largest coefficient.
            double m = Math.Abs(_x);
            double absy = Math.Abs(_y);
            double absz = Math.Abs(_z);
            if (absy > m)
            {
                m = absy;
            }
            if (absz > m)
            {
                m = absz;
            }

            _x /= m;
            _y /= m;
            _z /= m;
            
            double length = Math.Sqrt(_x * _x + _y * _y + _z * _z);
            this /= length;
        }
        
        /// <summary>
        /// Computes the angle between two vectors.
        /// </summary>
        /// <param name="vector1">First vector.</param>
        /// <param name="vector2">Second vector.</param>
        /// <returns>
        /// Returns the angle required to rotate vector1 into vector2 in degrees.
        /// This will return a value between [0, 180] degrees.
        /// (Note that this is slightly different from the Vector member
        /// function of the same name.  Signed angles do not extend to 3D.)
        /// </returns>
        public static double AngleBetween(Vector3D vector1, Vector3D vector2)
        {
            vector1.Normalize();
            vector2.Normalize();

            double ratio = DotProduct(vector1, vector2);

            // The "straight forward" method of acos(u.v) has large precision
            // issues when the dot product is near +/-1.  This is due to the
            // steep slope of the acos function as we approach +/- 1.  Slight
            // precision errors in the dot product calculation cause large
            // variation in the output value.
            //
            //        |                   |
            //         \__                |
            //            ---___          |
            //                  ---___    |
            //                        ---_|_
            //                            | ---___
            //                            |       ---___
            //                            |             ---__
            //                            |                  \
            //                            |                   |
            //       -|-------------------+-------------------|-
            //       -1                   0                   1
            //
            //                         acos(x)
            //
            // To avoid this we use an alternative method which finds the
            // angle bisector by (u-v)/2:
            //
            //                            _>
            //                       u  _-  \ (u-v)/2
            //                        _-  __-v
            //                      _=__--      
            //                    .=----------->
            //                            v
            //
            // Because u and v and unit vectors, (u-v)/2 forms a right angle
            // with the angle bisector.  The hypotenuse is 1, therefore
            // 2*asin(|u-v|/2) gives us the angle between u and v.
            //
            // The largest possible value of |u-v| occurs with perpendicular
            // vectors and is sqrt(2)/2 which is well away from extreme slope
            // at +/-1.
            //
            
            double theta;
            
            if (ratio < 0)
            {
                theta = Math.PI - 2.0 * Math.Asin((-vector1 - vector2).Length / 2.0);
            }
            else
            {
                theta = 2.0 * Math.Asin((vector1 - vector2).Length / 2.0);
            }

            return M3DUtil.RadiansToDegrees(theta);
        }

        /// <summary>
        /// Operator -Vector (unary negation).
        /// </summary>
        /// <param name="vector">Vector being negated.</param>
        /// <returns>Negation of the given vector.</returns>
        public static Vector3D operator -(Vector3D vector)
        {
            return new Vector3D(-vector._x, -vector._y, -vector._z);
        }

        /// <summary>
        /// Negates the values of X, Y, and Z on this Vector3D
        /// </summary>
        public void Negate()
        {
            _x = -_x;
            _y = -_y;
            _z = -_z;
        }

        /// <summary>
        /// Vector addition.
        /// </summary>
        /// <param name="vector1">First vector being added.</param>
        /// <param name="vector2">Second vector being added.</param>
        /// <returns>Result of addition.</returns>
        public static Vector3D operator +(Vector3D vector1, Vector3D vector2)
        {
            return new Vector3D(vector1._x + vector2._x, 
                                vector1._y + vector2._y, 
                                vector1._z + vector2._z);
        }

        /// <summary>
        /// Vector addition.
        /// </summary>
        /// <param name="vector1">First vector being added.</param>
        /// <param name="vector2">Second vector being added.</param>
        /// <returns>Result of addition.</returns>
        public static Vector3D Add(Vector3D vector1, Vector3D vector2)
        {
            return new Vector3D(vector1._x + vector2._x, 
                                vector1._y + vector2._y, 
                                vector1._z + vector2._z);
        }

        /// <summary>
        /// Vector subtraction.
        /// </summary>
        /// <param name="vector1">Vector that is subtracted from.</param>
        /// <param name="vector2">Vector being subtracted.</param>
        /// <returns>Result of subtraction.</returns>
        public static Vector3D operator -(Vector3D vector1, Vector3D vector2)
        {
            return new Vector3D(vector1._x - vector2._x, 
                                vector1._y - vector2._y, 
                                vector1._z - vector2._z);
        }

        /// <summary>
        /// Vector subtraction.
        /// </summary>
        /// <param name="vector1">Vector that is subtracted from.</param>
        /// <param name="vector2">Vector being subtracted.</param>
        /// <returns>Result of subtraction.</returns>
        public static Vector3D Subtract(Vector3D vector1, Vector3D vector2)
        {
            return new Vector3D(vector1._x - vector2._x, 
                                vector1._y - vector2._y, 
                                vector1._z - vector2._z);
        }

        /// <summary>
        /// Vector3D + Point3D addition.
        /// </summary>
        /// <param name="vector">Vector by which we offset the point.</param>
        /// <param name="point">Point being offset by the given vector.</param>
        /// <returns>Result of addition.</returns>
        public static Point3D operator +(Vector3D vector, Point3D point)
        {   
            return new Point3D(vector._x + point._x, 
                               vector._y + point._y, 
                               vector._z + point._z);
        }

        /// <summary>
        /// Vector3D + Point3D addition.
        /// </summary>
        /// <param name="vector">Vector by which we offset the point.</param>
        /// <param name="point">Point being offset by the given vector.</param>
        /// <returns>Result of addition.</returns>
        public static Point3D Add(Vector3D vector, Point3D point)
        {   
            return new Point3D(vector._x + point._x, 
                               vector._y + point._y, 
                               vector._z + point._z);
        }

        /// <summary>
        /// Vector3D - Point3D subtraction.
        /// </summary>
        /// <param name="vector">Vector by which we offset the point.</param>
        /// <param name="point">Point being offset by the given vector.</param>
        /// <returns>Result of subtraction.</returns>
        public static Point3D operator -(Vector3D vector, Point3D point)
        {
            return new Point3D(vector._x - point._x, 
                               vector._y - point._y, 
                               vector._z - point._z);
        }

        /// <summary>
        /// Vector3D - Point3D subtraction.
        /// </summary>
        /// <param name="vector">Vector by which we offset the point.</param>
        /// <param name="point">Point being offset by the given vector.</param>
        /// <returns>Result of subtraction.</returns>
        public static Point3D Subtract(Vector3D vector, Point3D point)
        {
            return new Point3D(vector._x - point._x, 
                               vector._y - point._y, 
                               vector._z - point._z);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <param name="vector">Vector being multiplied.</param>
        /// <param name="scalar">Scalar value by which the vector is multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D operator *(Vector3D vector, double scalar)
        {
            return new Vector3D(vector._x * scalar, 
                                vector._y * scalar, 
                                vector._z * scalar);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <param name="vector">Vector being multiplied.</param>
        /// <param name="scalar">Scalar value by which the vector is multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D Multiply(Vector3D vector, double scalar)
        {
            return new Vector3D(vector._x * scalar, 
                                vector._y * scalar, 
                                vector._z * scalar);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <param name="scalar">Scalar value by which the vector is multiplied</param>
        /// <param name="vector">Vector being multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D operator *(double scalar, Vector3D vector)
        {
            return new Vector3D(vector._x * scalar, 
                                vector._y * scalar, 
                                vector._z * scalar);
        }

        /// <summary>
        /// Scalar multiplication.
        /// </summary>
        /// <param name="scalar">Scalar value by which the vector is multiplied</param>
        /// <param name="vector">Vector being multiplied.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D Multiply(double scalar, Vector3D vector)
        {
            return new Vector3D(vector._x * scalar, 
                                vector._y * scalar, 
                                vector._z * scalar);
        }

        /// <summary>
        /// Scalar division.
        /// </summary>
        /// <param name="vector">Vector being divided.</param>
        /// <param name="scalar">Scalar value by which we divide the vector.</param>
        /// <returns>Result of division.</returns>
        public static Vector3D operator /(Vector3D vector, double scalar)
        {
            return vector * (1.0 / scalar);
        }

        /// <summary>
        /// Scalar division.
        /// </summary>
        /// <param name="vector">Vector being divided.</param>
        /// <param name="scalar">Scalar value by which we divide the vector.</param>
        /// <returns>Result of division.</returns>
        public static Vector3D Divide(Vector3D vector, double scalar)
        {
            return vector * (1.0 / scalar);
        }

        /// <summary>
        /// Vector3D * Matrix3D multiplication
        /// </summary>
        /// <param name="vector">Vector being tranformed.</param>
        /// <param name="matrix">Transformation matrix applied to the vector.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D operator *(Vector3D vector, Matrix3D matrix)
        {
            return matrix.Transform(vector);
        }

        /// <summary>
        /// Vector3D * Matrix3D multiplication
        /// </summary>
        /// <param name="vector">Vector being tranformed.</param>
        /// <param name="matrix">Transformation matrix applied to the vector.</param>
        /// <returns>Result of multiplication.</returns>
        public static Vector3D Multiply(Vector3D vector, Matrix3D matrix)
        {
            return matrix.Transform(vector);
        }

        /// <summary>
        /// Vector dot product.
        /// </summary>
        /// <param name="vector1">First vector.</param>
        /// <param name="vector2">Second vector.</param>
        /// <returns>Dot product of two vectors.</returns>
        public static double DotProduct(Vector3D vector1, Vector3D vector2)
        {
            return DotProduct(ref vector1, ref vector2);
        }

        /// <summary>
        /// Faster internal version of DotProduct that avoids copies
        ///
        /// vector1 and vector2 to a passed by ref for perf and ARE NOT MODIFIED
        /// </summary>
        internal static double DotProduct(ref Vector3D vector1, ref Vector3D vector2)
        {
            return vector1._x * vector2._x + 
                   vector1._y * vector2._y +
                   vector1._z * vector2._z;
        }

        /// <summary>
        /// Vector cross product.
        /// </summary>
        /// <param name="vector1">First vector.</param>
        /// <param name="vector2">Second vector.</param>
        /// <returns>Cross product of two vectors.</returns>
        public static Vector3D CrossProduct(Vector3D vector1, Vector3D vector2)
        {
            Vector3D result;
            CrossProduct(ref vector1, ref vector2, out result);
            return result;
        }

        /// <summary>
        /// Faster internal version of CrossProduct that avoids copies
        ///
        /// vector1 and vector2 to a passed by ref for perf and ARE NOT MODIFIED
        /// </summary>
        internal static void CrossProduct(ref Vector3D vector1, ref Vector3D vector2, out Vector3D result)
        {
            result._x = vector1._y * vector2._z - vector1._z * vector2._y;
            result._y = vector1._z * vector2._x - vector1._x * vector2._z;
            result._z = vector1._x * vector2._y - vector1._y * vector2._x;
        }

        /// <summary>
        /// Vector3D to Point3D conversion.
        /// </summary>
        /// <param name="vector">Vector being converted.</param>
        /// <returns>Point representing the given vector.</returns>
        public static explicit operator Point3D(Vector3D vector)
        {
            return new Point3D(vector._x, vector._y, vector._z);
        }

        /// <summary>
        /// Explicit conversion to Size3D.  Note that since Size3D cannot contain negative values,
        /// the resulting size will contains the absolute values of X, Y, and Z.
        /// </summary>
        /// <param name="vector">The vector to convert to a size.</param>
        /// <returns>A size equal to this vector.</returns>
        public static explicit operator Size3D(Vector3D vector)
        {
            return new Size3D(Math.Abs(vector._x), Math.Abs(vector._y), Math.Abs(vector._z));
        }

        #endregion Public Methods
    }
}
