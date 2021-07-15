// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace System.Windows
{
    /// <summary>
    /// Point - Defaults to 0,0
    /// </summary>
    public partial struct Point
    {
        #region Constructors

        /// <summary>
        /// Constructor which accepts the X and Y values
        /// </summary>
        /// <param name="x">The value for the X coordinate of the new Point</param>
        /// <param name="y">The value for the Y coordinate of the new Point</param>
        public Point(double x, double y)
        {
            _x = x;
            _y = y;
        }

        #endregion Constructors

        #region Public Methods
        
        /// <summary>
        /// Offset - update the location by adding offsetX to X and offsetY to Y
        /// </summary>
        /// <param name="offsetX"> The offset in the x dimension </param>
        /// <param name="offsetY"> The offset in the y dimension </param>
        public void Offset(double offsetX, double offsetY)
        {
            _x += offsetX;
            _y += offsetY;
        }

        /// <summary>
        /// Operator Point + Vector
        /// </summary>
        /// <returns>
        /// Point - The result of the addition
        /// </returns>
        /// <param name="point"> The Point to be added to the Vector </param>
        /// <param name="vector"> The Vectr to be added to the Point </param>
        public static Point operator + (Point point, Vector vector)
        {
             return new Point(point._x + vector._x, point._y + vector._y);
        }

        /// <summary>
        /// Add: Point + Vector
        /// </summary>
        /// <returns>
        /// Point - The result of the addition
        /// </returns>
        /// <param name="point"> The Point to be added to the Vector </param>
        /// <param name="vector"> The Vector to be added to the Point </param>
        public static Point Add(Point point, Vector vector)
        {
            return new Point(point._x + vector._x, point._y + vector._y);
        }

        /// <summary>
        /// Operator Point - Vector
        /// </summary>
        /// <returns>
        /// Point - The result of the subtraction
        /// </returns>
        /// <param name="point"> The Point from which the Vector is subtracted </param>
        /// <param name="vector"> The Vector which is subtracted from the Point </param>
        public static Point operator - (Point point, Vector vector)
        {
            return new Point(point._x - vector._x, point._y - vector._y);
        }

        /// <summary>
        /// Subtract: Point - Vector
        /// </summary>
        /// <returns>
        /// Point - The result of the subtraction
        /// </returns>
        /// <param name="point"> The Point from which the Vector is subtracted </param>
        /// <param name="vector"> The Vector which is subtracted from the Point </param>
        public static Point Subtract(Point point, Vector vector)
        {
            return new Point(point._x - vector._x, point._y - vector._y);
        }

        /// <summary>
        /// Operator Point - Point
        /// </summary>
        /// <returns>
        /// Vector - The result of the subtraction
        /// </returns>
        /// <param name="point1"> The Point from which point2 is subtracted </param>
        /// <param name="point2"> The Point subtracted from point1 </param>
        public static Vector operator - (Point point1, Point point2)
        {
            return new Vector(point1._x - point2._x, point1._y - point2._y);
        }

        /// <summary>
        /// Subtract: Point - Point
        /// </summary>
        /// <returns>
        /// Vector - The result of the subtraction
        /// </returns>
        /// <param name="point1"> The Point from which point2 is subtracted </param>
        /// <param name="point2"> The Point subtracted from point1 </param>
        public static Vector Subtract(Point point1, Point point2)
        {
            return new Vector(point1._x - point2._x, point1._y - point2._y);
        }

        /// <summary>
        /// Operator Point * Matrix
        /// </summary>
        public static Point operator * (Point point, Matrix matrix)
        {
            return matrix.Transform(point);
        }

        /// <summary>
        /// Multiply: Point * Matrix
        /// </summary>
        public static Point Multiply(Point point, Matrix matrix)
        {
            return matrix.Transform(point);
        }

        /// <summary>
        /// Explicit conversion to Size.  Note that since Size cannot contain negative values,
        /// the resulting size will contains the absolute values of X and Y
        /// </summary>
        /// <returns>
        /// Size - A Size equal to this Point
        /// </returns>
        /// <param name="point"> Point - the Point to convert to a Size </param>
        public static explicit operator Size(Point point)
        {
            return new Size(Math.Abs(point._x), Math.Abs(point._y));
        }

        /// <summary>
        /// Explicit conversion to Vector
        /// </summary>
        /// <returns>
        /// Vector - A Vector equal to this Point
        /// </returns>
        /// <param name="point"> Point - the Point to convert to a Vector </param>
        public static explicit operator Vector(Point point)
        {
            return new Vector(point._x, point._y);
        }

        #endregion Public Methods
   }
}
