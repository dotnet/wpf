// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input.Manipulations
{
    /// <summary>
    /// Yes, this is basically the same as System.Drawing.PointF. Why don't we use
    /// that one? Because we don't want to link to that assembly. We had been just using
    /// two floats everywhere, but I just couldn't take it any more. A point class is just
    /// too nice a thing.
    /// </summary>
    internal readonly struct PointF
    {
        private readonly float x;
        private readonly float y;

        /// <summary>
        /// Create a basic point structure
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public PointF(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Creates a VectorF structure with an X value equal to this point's X value
        /// and a Y value equal to this point's Y value.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>A VectorF structure with an X value equal to this point's X value
        /// and a Y value equal to this point's Y value.</returns>
        public static explicit operator VectorF(in PointF point)
        {
            return new VectorF(point.x, point.y);
        }

        /// <summary>
        /// Returns true if the two points are different
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(in PointF left, in PointF right)
        {
            return left.X != right.X || left.Y != right.Y;
        }

        /// <summary>
        /// Returns true if the points are the same. Note that since
        /// these are floats a small amount of error may cause similar
        /// points to not be equal. If you want precision adjustment
        /// you need to do that yourself.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(in PointF left, in PointF right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        /// <summary>
        /// Translates the specified PointF by the specified VectorF
        /// and returns the result.
        /// </summary>
        /// <param name="pt">The point to translate.</param>
        /// <param name="offset">The amount by which to translate the point.</param>
        /// <returns>The result of translating the specified point by the specified vector.</returns>
        public static PointF operator +(in PointF pt, VectorF offset)
        {
            return new PointF(pt.X + offset.X, pt.Y + offset.Y);
        }

        /// <summary>
        /// Subtracts the specified PointF from another specified PointF
        /// and returns the difference as a VectorF.
        /// </summary>
        /// <param name="point1">The point from which point2 is subtracted.</param>
        /// <param name="point2">The point to subtract from point1.</param>
        /// <returns>The difference between point1 and point2.</returns>
        public static VectorF operator -(in PointF point1, in PointF point2)
        {
            return new VectorF(point1.x - point2.x, point1.y - point2.y);
        }

        /// <summary>
        /// Subtracts the specified VectorF from the specified PointF
        /// and returns the resulting PointF.
        /// </summary>
        /// <param name="point">The point from which vector is subtracted.</param>
        /// <param name="vector">The vector to subtract from point</param>
        /// <returns>The difference between point and vector.</returns>
        public static PointF operator -(in PointF point, VectorF vector)
        {
            return new PointF(point.x - vector.X, point.y - vector.Y);
        }

        /// <summary>
        /// The x coordinate
        /// </summary>
        public float X 
        {
            get { return x; }
            // unused set { x = value; }
        }

        /// <summary>
        /// the y coordinate
        /// </summary>
        public float Y 
        {
            get { return y; }
            // unused set { y = value; }
        }

        /// <summary>
        /// equals override
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is PointF)
                return (PointF)obj == this;

            return false;
        }

        /// <summary>
        /// hash code override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (x.GetHashCode() ^ y.GetHashCode());
        }

        /// <summary>
        /// tostring override
        /// </summary>
        /// <returns></returns>
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
