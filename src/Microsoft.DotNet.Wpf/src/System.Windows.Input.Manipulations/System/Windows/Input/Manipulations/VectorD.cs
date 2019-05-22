// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Represents a displacement in 2-D space.
    /// </summary>
    /// <remarks>
    /// Yes, this is basically the same as System.Windows.Vector. Why don't we use
    /// that one? Because we don't want to link to that assembly.
    /// </remarks>
    internal struct VectorD
    {
        private double x;
        private double y;

        /// <summary>
        /// Initializes a new instance of the VectorD structure.
        /// </summary>
        /// <param name="x">The X-offset of the new VectorD.</param>
        /// <param name="y">The Y-offset of the new VectorD.</param>
        public VectorD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Gets or sets the X component of this vector.
        /// </summary>
        public double X
        {
            get { return x; }
            // unused set { x = value; }
        }

        /// <summary>
        /// Gets or sets the Y component of this vector.
        /// </summary>
        public double Y
        {
            get { return y; }
            // unused set { y = value; }
        }

#if false // unused
        #region Conversion


        /// <summary>
        /// Creates a point with the X and Y values of this vector.
        /// </summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>A point with X- and Y-coordinate values equal to the X and Y offset values of vector.</returns>
        public static explicit operator PointD(VectorD vector)
        {
            return new PointD(vector.x, vector.y);
        }

        /// <summary>
        /// Creates a size from the offsets of this vector.
        /// </summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>A size with a Width equal to the absolute value of this vector's X property
        /// and a Height equal to the absolute value of this vector's Y property.</returns>
        public static explicit operator SizeD(VectorD vector)
        {
            return new SizeD(vector.x, vector.y);
        }

        #endregion
#endif

        #region Negation

        /// <summary>
        /// Negates the specified vector.
        /// </summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector with X and Y values opposite of the X and Y values of vector.
        ///</returns>
        public static VectorD operator -(VectorD vector)
        {
            return new VectorD(-vector.x, -vector.y);
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
        public static bool operator !=(VectorD vector1, VectorD vector2)
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
        public static bool operator ==(VectorD vector1, VectorD vector2)
        {
            return (vector1.x == vector2.x && vector1.y == vector2.y);
        }

        /// <summary>
        /// Determines whether the specified System.Object is a VectorD structure and,
        /// if it is, whether it has the same X and Y values as this vector.
        /// </summary>
        /// <param name="o">The vector to compare.</param>
        /// <returns>true if o is a VectorD and has the same X and Y values as this vector;
        /// otherwise, false.</returns>
        public override bool Equals(object o)
        {
            if (o is VectorD)
            {
                return (VectorD)o == this;
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
        public static bool Equals(VectorD vector1, VectorD vector2)
        {
            return vector1 == vector2;
        }

        /// <summary>
        /// Compares two vectors for equality.
        /// </summary>
        /// <param name="value">The vector to compare with this vector.</param>
        /// <returns>true if value has the same X and Y values as this vector;
        /// otherwise, false.</returns>
        public bool Equals(VectorD value)
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
        public static VectorD operator +(VectorD vector1, VectorD vector2)
        {
            return new VectorD(vector1.x + vector2.x, vector1.y + vector2.y);
        }

#if false // unused
        /// <summary>
        /// Adds two vectors and returns the result.
        /// </summary>
        /// <param name="vector1">The first vector to add.</param>
        /// <param name="vector2">The second vector to add.</param>
        /// <returns> The sum of vector1 and vector2.</returns>
        public static VectorD Add(VectorD vector1, VectorD vector2)
        {
            return vector1 + vector2;
        }

        /// <summary>
        /// Translates a point by the specified vector and returns the resulting point.
        /// </summary>
        /// <param name="vector">The vector used to translate point.</param>
        /// <param name="point">The point to translate.</param>
        /// <returns>The result of translating point by vector.</returns>
        public static PointD operator +(VectorD vector, PointD point)
        {
            return new PointD(point.X + vector.x, point.Y + vector.y);
        }


        /// <summary>
        /// Translates the specified point by the specified vector and returns the resulting point.
        /// </summary>
        /// <param name="vector">The amount to translate the specified point.</param>
        /// <param name="point">The point to translate.</param>
        /// <returns>The result of translating point by vector.</returns>
        public static PointD Add(VectorD vector, PointD point)
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
        public static VectorD operator -(VectorD vector1, VectorD vector2)
        {
            return new VectorD(vector1.x - vector2.x, vector1.y - vector2.y);
        }

#if false // unused
        /// <summary>
        /// Subtracts the specified vector from another specified vector.
        /// </summary>
        /// <param name="vector1">The vector from which vector2 is subtracted.</param>
        /// <param name="vector2">The vector to subtract from vector1.</param>
        /// <returns>The difference between vector1 and vector2.</returns>
        public static VectorD Subtract(VectorD vector1, VectorD vector2)
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
        public static VectorD operator *(double scalar, VectorD vector)
        {
            return new VectorD(vector.x * scalar, vector.y * scalar);
        }

        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to multiply.</param>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorD operator *(VectorD vector, double scalar)
        {
            return new VectorD(vector.x * scalar, vector.y * scalar);
        }

        /// <summary>
        /// Divides the specified vector by the specified scalar and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to divide.</param>
        /// <param name="scalar">The scalar by which vector will be divided.</param>
        /// <returns>The result of dividing vector by scalar.</returns>
        public static VectorD operator /(VectorD vector, double scalar)
        {
            return new VectorD(vector.x / scalar, vector.y / scalar);
        }

#if false // unused
        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <param name="vector">The vector to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorD Multiply(double scalar, VectorD vector)
        {
            return scalar * vector;
        }

        /// <summary>
        /// Multiplies the specified scalar by the specified vector and returns the resulting vector.
        /// </summary>
        /// <param name="vector">The vector to multiply.</param>
        /// <param name="scalar">The scalar to multiply.</param>
        /// <returns>The result of multiplying scalar and vector.</returns>
        public static VectorD Multiply(VectorD vector, double scalar)
        {
            return vector * scalar;
        }

        /// <summary>
        /// Divides the specified vector by the specified scalar and returns the result.
        /// </summary>
        /// <param name="vector">The vector structure to divide.</param>
        /// <param name="scalar">The amount by which vector is divided.</param>
        /// <returns>The result of dividing vector by scalar.</returns>
        public static VectorD Divide(VectorD vector, double scalar)
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
        public static double operator *(VectorD vector1, VectorD vector2)
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
        public static double Multiply(VectorD vector1, VectorD vector2)
        {
            return vector1 * vector2;
        }
#endif

        #endregion

        #region Magnitude

        /// <summary>
        /// Gets the length of this vector.
        /// </summary>
        public double Length
        {
            get
            {
                return Math.Sqrt(LengthSquared);
            }
        }

        /// <summary>
        /// Gets the square of the length of this vector.
        /// </summary>
        public double LengthSquared
        {
            get
            {
                return this * this;
            }
        }

#if false // unused
        /// <summary>
        /// Normalizes this vector.
        /// </summary>
        public void Normalize()
        {
            double length = Length;
            x /= length;
            y /= length;
        }
#endif

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
        public static double CrossProduct(VectorD vector1, VectorD vector2)
        {
            return (vector1.x * vector2.y) - (vector1.y * vector2.x);
        }

        /// <summary>
        /// Calculates the determinant of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector to evaluate.</param>
        /// <param name="vector2">The second vector to evaluate.</param>
        /// <returns>The determinant of vector1 and vector2.</returns>
        public static double Determinant(VectorD vector1, VectorD vector2)
        {
            // In the case of two 2D vectors, the determinant is the cross-product.
            return CrossProduct(vector1, vector2);
        }

        /// <summary>
        /// Retrieves the angle, expressed in radians, between the two specified vectors.
        /// </summary>
        /// <param name="vector1">The first vector to evaluate.</param>
        /// <param name="vector2">The second vector to evaluate.</param>
        /// <returns>The angle, in radians, between vector1 and vector2.</returns>
        public static double AngleBetween(VectorD vector1, VectorD vector2)
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
            return angle;
        }
#endif

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
        /// Returns the string representation of this VectorD structure.
        /// </summary>
        /// <returns>A string that represents the X and Y values of this VectorD.</returns>
        public override string ToString()
        {
            return ToString(System.Globalization.CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns the string representation of this VectorD structure with the specified
        /// formatting information.
        /// </summary>
        /// <param name="provider">The culture-specific formatting information.</param>
        /// <returns>A string that represents the X and Y values of this VectorD.</returns>
        public string ToString(IFormatProvider provider)
        {
            return String.Format(provider, "X={0}, Y={1}", x, y);
        }
    }
}
