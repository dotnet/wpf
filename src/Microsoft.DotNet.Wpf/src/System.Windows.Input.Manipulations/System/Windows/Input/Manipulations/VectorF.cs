// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents a displacement in 2-D space.
    /// </summary>
    /// <remarks>
    /// Yes, this is basically the same as System.Windows.Vector. Why don't we use
    /// that one? Because we don't want to link to that assembly.
    /// </remarks>
    internal struct VectorF
    {
        private float x;
        private float y;

        /// <summary>
        /// Initializes a new instance of the VectorF structure.
        /// </summary>
        /// <param name="x">The X-offset of the new VectorF.</param>
        /// <param name="y">The Y-offset of the new VectorF.</param>
        public VectorF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets or sets the X component of this vector.
        /// </summary>
        public float X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Gets or sets the Y component of this vector.
        /// </summary>
        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        #region Conversion

        /// <summary>
        /// Creates a point with the X and Y values of this vector.
        /// </summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>A point with X- and Y-coordinate values equal to the X and Y offset values of vector.</returns>
        public static explicit operator PointF(VectorF vector)
        {
            return new PointF(vector.x, vector.y);
        }

        #endregion

        #region Negation

        /// <summary>
        /// Negates the specified vector.
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector with X and Y values opposite of the X and Y values of vector.
        ///</returns>
        public static VectorF operator -(VectorF vector)
        {
            return new VectorF(-vector.x, -vector.y);
        }

#if false // unused
        /// <summary>
        /// Negates this vector.
        /// </summary>
        public void Negate()
        {
            x = -x;
            y = -y;
        }
#endif

        #endregion

        #region Equality

        /// <summary>
        /// Compares two vectors for inequality.
        /// </summary>
        /// <param name="vector1">The first vector to compare.</param>
        /// <param name="vector2">The second vector to compare.</param>
        /// <returns>true if the X and Y components of vector1 and vector2
        /// are different; otherwise, false.</returns>
        public static bool operator !=(VectorF vector1, VectorF vector2)
        {
            return (vector1.x != vector2.x || vector1.y != vector2.y);
        }

        /// <summary>
        /// Compares two vectors for equality.
        /// </summary>
        /// <param name="vector1">The first vector to compare.</param>
        /// <param name="vector2">The second vector to compare.</param>
        /// <returns>true if the X and Y components of vector1 and vector2
        /// are equal; otherwise, false.</returns>
        public static bool operator ==(VectorF vector1, VectorF vector2)
        {
            return (vector1.x == vector2.x && vector1.y == vector2.y);
        }

        /// <summary>
        /// Determines whether the specified System.Object is a VectorF structure and,
        /// if it is, whether it has the same X and Y values as this vector.
        /// </summary>
        /// <param name="o">The vector to compare.</param>
        /// <returns>true if o is a VectorF and has the same X and Y values as this vector;
        /// otherwise, false.</returns>
        public override bool Equals(object o)
        {
            if (o is VectorF)
            {
                return (VectorF)o == this;
            }
            return false;
        }

#if false // unused
        /// <summary>
        /// Compares the two specified vectors for equality.
        /// </summary>
        /// <param name="vector1">The first vector to compare.</param>
        /// <param name="vector2">The second vector to compare.</param>
        /// <returns>true if the X and Y components of vector1 and vector2 are equal;
        /// otherwise, false.</returns>
        public static bool Equals(VectorF vector1, VectorF vector2)
        {
            return vector1 == vector2;
        }

        /// <summary>
        /// Compares two vectors for equality.
        /// </summary>
        /// <param name="value">The vector to compare with this vector.</param>
        /// <returns>true if value has the same X and Y values as this vector;
        /// otherwise, false.</returns>
        public bool Equals(VectorF value)
        {
            return value == this;
        }
#endif

        #endregion

        #region Addition

        /// <summary>
        /// Adds two vectors and returns the result as a vector.
        /// </summary>
        /// <param name="vector1">The first vector to add.</param>
        /// <param name="vector2">The second vector to add.</param>
        /// <returns>The sum of vector1 and vector2.</returns>
        public static VectorF operator +(VectorF vector1, VectorF vector2)
        {
            return new VectorF(vector1.x + vector2.x, vector1.y + vector2.y);
        }

        /// <summary>
        /// Translates a point by the specified vector and returns the resulting point.
        /// </summary>
        /// <param name="vector">The vector used to translate point.</param>
        /// <param name="point">The point to translate.</param>
        /// <returns>The result of translating point by vector.</returns>
        public static PointF operator +(VectorF vector, PointF point)
        {
            return new PointF(point.X + vector.x, point.Y + vector.y);
        }

#if false // unused
        /// <summary>
        /// Adds two vectors and returns the result.
        /// </summary>
        /// <param name="vector1">The first vector to add.</param>
        /// <param name="vector2">The second vector to add.</param>
        /// <returns> The sum of vector1 and vector2.</returns>
        public static VectorF Add(VectorF vector1, VectorF vector2)
        {
            return vector1 + vector2;
        }

        /// <summary>
        /// Translates the specified point by the specified vector and returns the resulting point.
        /// </summary>
        /// <param name="vector">The amount to translate the specified point.</param>
        /// <param name="point">The point to translate.</param>
        /// <returns>The result of translating point by vector.</returns>
        public static PointF Add(VectorF vector, PointF point)
        {
            return vector + point;
        }
#endif

        #endregion

        #region Subtraction

        /// <summary>
        /// Subtracts one specified vector from another.
        /// </summary>
        /// <param name="vector1">The vector from which vector2 is subtracted.</param>
        /// <param name="vector2">The vector to subtract from vector1.</param>
        /// <returns>The difference between vector1 and vector2.</returns>
        public static VectorF operator -(VectorF vector1, VectorF vector2)
        {
            return new VectorF(vector1.x - vector2.x, vector1.y - vector2.y);
        }

#if false // unused
        /// <summary>
        /// Subtracts the specified vector from another specified vector.
        /// </summary>
        /// <param name="vector1">The vector from which vector2 is subtracted.</param>
        /// <param name="vector2">The vector to subtract from vector1.</param>
        /// <returns>The difference between vector1 and vector2.</returns>
        public static VectorF Subtract(VectorF vector1, VectorF vector2)
        {
            return vector1 - vector2;
        }
#endif

        #endregion

        #region Scaling

        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <param name="vector">The vector to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorF operator *(float scalar, VectorF vector)
        {
            return new VectorF(vector.x * scalar, vector.y * scalar);
        }

        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to multiply.</param>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorF operator *(VectorF vector, float scalar)
        {
            return new VectorF(vector.x * scalar, vector.y * scalar);
        }

        /// <summary>
        /// Divides the specified vector by the specified scalar and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to divide.</param>
        /// <param name="scalar">The scalar by which vector will be divided.</param>
        /// <returns>The result of dividing vector by scalar.</returns>
        public static VectorF operator /(VectorF vector, float scalar)
        {
            return new VectorF(vector.x / scalar, vector.y / scalar);
        }

#if false // unused
        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <param name="vector">The vector to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorF Multiply(float scalar, VectorF vector)
        {
            return scalar * vector;
        }

        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to multiply.</param>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorF Multiply(VectorF vector, float scalar)
        {
            return vector * scalar;
        }

        /// <summary>
        /// Divides the specified vector by the specified scalar and returns the result.
        /// </summary>
        /// <param name="vector">The vector structure to divide.</param>
        /// <param name="scalar">The amount by which vector is divided.</param>
        /// <returns>The result of dividing vector by scalar.</returns>
        public static VectorF Divide(VectorF vector, float scalar)
        {
            return vector / scalar;
        }
#endif

        #endregion

        #region Dot-Product

        /// <summary>
        /// Calculates the dot product of the two specified vector structures.
        /// </summary>
        /// <param name="vector1">The first vector to multiply.</param>
        /// <param name="vector2">The second vector to multiply.</param>
        /// <returns>Returns the scalar dot product of vector1 and vector2, which is calculated
        /// using the following formula: vector1.X * vector2.X + vector1.Y * vector2.Y</returns>
        public static float operator *(VectorF vector1, VectorF vector2)
        {
            return (vector1.x * vector2.x) + (vector1.y * vector2.y);
        }

#if false // unused
        /// <summary>
        /// Calculates the dot product of the two specified vectors and returns the result.
        /// </summary>
        /// <param name="vector1">The first vector to multiply.</param>
        /// <param name="vector2">The second vector structure to multiply.</param>
        /// <returns>The scalar dot product of vector1 and vector2, which is calculated
        /// using the following formula: (vector1.X * vector2.X) + (vector1.Y * vector2.Y)</returns>
        public static float Multiply(VectorF vector1, VectorF vector2)
        {
            return vector1 * vector2;
        }
#endif

        #endregion

        #region Magnitude

        /// <summary>
        /// Gets the length of this vector.
        /// </summary>
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(LengthSquared);
            }
        }

        /// <summary>
        /// Gets the square of the length of this vector.
        /// </summary>
        public float LengthSquared
        {
            get
            {
                return this * this;
            }
        }

        /// <summary>
        /// Normalizes this vector.
        /// </summary>
        public void Normalize()
        {
            float length = Length;
            x /= length;
            y /= length;
        }

        #endregion

        #region Vector Operations

#if false // unused
        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector to evaluate.</param>
        /// <param name="vector2">The second vector to evaluate.</param>
        /// <returns>The cross product of vector1 and vector2. The following formula is used to
        /// calculate the cross product: (Vector1.X * Vector2.Y) - (Vector1.Y * Vector2.X)</returns>
        public static float CrossProduct(VectorF vector1, VectorF vector2)
        {
            return (vector1.x * vector2.y) - (vector1.y * vector2.x);
        }

        /// <summary>
        /// Calculates the determinant of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector to evaluate.</param>
        /// <param name="vector2">The second vector to evaluate.</param>
        /// <returns>The determinant of vector1 and vector2.</returns>
        public static float Determinant(VectorF vector1, VectorF vector2)
        {
            // In the case of two 2D vectors, the determinant is the cross-product.
            return CrossProduct(vector1, vector2);
        }
#endif

        /// <summary>
        /// Retrieves the angle, expressed in radians, between the two specified vectors.
        /// </summary>
        /// <param name="vector1">The first vector to evaluate.</param>
        /// <param name="vector2">The second vector to evaluate.</param>
        /// <returns>The angle, in radians, between vector1 and vector2.</returns>
        public static float AngleBetween(VectorF vector1, VectorF vector2)
        {
            vector1.Normalize();
            vector2.Normalize();
            double angle = Math.Atan2(vector2.y, vector2.x) - Math.Atan2(vector1.y, vector1.x);
            if (angle > Math.PI)
            {
                angle -= Math.PI * 2.0;
            }
            else if (angle < -Math.PI)
            {
                angle += Math.PI * 2.0;
            }
            return (float)angle;
        }

        #endregion

        /// <summary>
        /// Returns the hash code for this vector.
        /// </summary>
        /// <returns>The hash code for this vector.</returns>
        public override int GetHashCode()
        {
            return (x.GetHashCode() ^ y.GetHashCode());
        }

        /// <summary>
        /// Returns the string representation of this VectorF structure.
        /// </summary>
        /// <returns>A string that represents the X and Y values of this VectorF.</returns>
        public override string ToString()
        {
            return String.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                "(X={0}, Y={1})", // okay not to use resources, since this is internal class
                x,
                y);
        }
    }
}
