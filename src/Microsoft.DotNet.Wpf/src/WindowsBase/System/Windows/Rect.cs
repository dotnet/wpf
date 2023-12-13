// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using MS.Internal;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Security;
using MS.Internal.WindowsBase;

namespace System.Windows
{
    /// <summary>
    /// Rect - The primitive which represents a rectangle.  Rects are stored as
    /// X, Y (Location) and Width and Height (Size).  As a result, Rects cannot have negative
    /// Width or Height.
    /// </summary>
    public partial struct Rect
    {
        #region Constructors

        /// <summary>
        /// Constructor which sets the initial values to the values of the parameters
        /// </summary>
        public Rect(Point location,
                    Size size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = location._x;
                _y = location._y;
                _width = size._width;
                _height = size._height;
            }
        }

        /// <summary>
        /// Constructor which sets the initial values to the values of the parameters.
        /// Width and Height must be non-negative
        /// </summary>
        public Rect(double x,
                    double y,
                    double width,
                    double height)
        {
            if (width < 0 || height < 0)
            {
                throw new System.ArgumentException(SR.Size_WidthAndHeightCannotBeNegative);
            }

            _x    = x;
            _y     = y;
            _width   = width;
            _height  = height;
        }

        /// <summary>
        /// Constructor which sets the initial values to bound the two points provided.
        /// </summary>
        public Rect(Point point1,
                    Point point2)
        {
            _x = Math.Min(point1._x, point2._x);
            _y = Math.Min(point1._y, point2._y);

            //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
            _width = Math.Max(Math.Max(point1._x, point2._x) - _x, 0);
            _height = Math.Max(Math.Max(point1._y, point2._y) - _y, 0);
        }

        /// <summary>
        /// Constructor which sets the initial values to bound the point provided and the point
        /// which results from point + vector.
        /// </summary>
        public Rect(Point point,
                    Vector vector): this(point, point+vector)
        {
        }

        /// <summary>
        /// Constructor which sets the initial values to bound the (0,0) point and the point 
        /// that results from (0,0) + size. 
        /// </summary>
        public Rect(Size size)
        {
            if(size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = _y = 0;
                _width = size.Width;
                _height = size.Height;
            }
        }

        #endregion Constructors

        #region Statics

        /// <summary>
        /// Empty - a static property which provides an Empty rectangle.  X and Y are positive-infinity
        /// and Width and Height are negative infinity.  This is the only situation where Width or
        /// Height can be negative.
        /// </summary>
        public static Rect Empty
        {
            get
            {
                return s_empty;
            }
        }

        #endregion Statics

        #region Public Properties

        /// <summary>
        /// IsEmpty - this returns true if this rect is the Empty rectangle.
        /// Note: If width or height are 0 this Rectangle still contains a 0 or 1 dimensional set
        /// of points, so this method should not be used to check for 0 area.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // The funny width and height tests are to handle NaNs
                Debug.Assert((!(_width < 0) && !(_height < 0)) || (this == Empty));

                return _width < 0;
            }
        }

        /// <summary>
        /// Location - The Point representing the origin of the Rectangle
        /// </summary>
        public Point Location
        {
            get
            {
                return new Point(_x, _y);
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                }
                
                _x = value._x;
                _y = value._y;
            }
        }

        /// <summary>
        /// Size - The Size representing the area of the Rectangle
        /// </summary>
        public Size Size
        {
            get
            {
                if (IsEmpty)
                    return Size.Empty;
                return new Size(_width, _height);
            }
            set
            {
                if (value.IsEmpty)
                {
                    this = s_empty;
                }
                else
                {
                    if (IsEmpty)
                    {
                        throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                    }

                    _width = value._width;
                    _height = value._height;
                }
            }
        }

        /// <summary>
        /// X - The X coordinate of the Location.
        /// If this is the empty rectangle, the value will be positive infinity.
        /// If this rect is Empty, setting this property is illegal.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                }

                _x = value;
            }
}

        /// <summary>
        /// Y - The Y coordinate of the Location
        /// If this is the empty rectangle, the value will be positive infinity.
        /// If this rect is Empty, setting this property is illegal.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                }

                _y = value;
            }
        }

        /// <summary>
        /// Width - The Width component of the Size.  This cannot be set to negative, and will only
        /// be negative if this is the empty rectangle, in which case it will be negative infinity.
        /// If this rect is Empty, setting this property is illegal.
        /// </summary>
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                }
                                
                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Size_WidthCannotBeNegative);
                }

                _width = value;
            }
        }

        /// <summary>
        /// Height - The Height component of the Size.  This cannot be set to negative, and will only
        /// be negative if this is the empty rectangle, in which case it will be negative infinity.
        /// If this rect is Empty, setting this property is illegal.
        /// </summary>
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Rect_CannotModifyEmptyRect);
                }

                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Size_HeightCannotBeNegative);
                }

                _height = value;
            }
        }

        /// <summary>
        /// Left Property - This is a read-only alias for X
        /// If this is the empty rectangle, the value will be positive infinity.
        /// </summary>
        public double Left
        {
            get
            {
                return _x;
            }
        }

        /// <summary>
        /// Top Property - This is a read-only alias for Y
        /// If this is the empty rectangle, the value will be positive infinity.
        /// </summary>
        public double Top
        {
            get
            {
                return _y;
            }
        }

        /// <summary>
        /// Right Property - This is a read-only alias for X + Width
        /// If this is the empty rectangle, the value will be negative infinity.
        /// </summary>
        public double Right
        {
            get
            {
                if (IsEmpty)
                {
                    return Double.NegativeInfinity;
                }

                return _x + _width;
            }
        }

        /// <summary>
        /// Bottom Property - This is a read-only alias for Y + Height
        /// If this is the empty rectangle, the value will be negative infinity.
        /// </summary>
        public double Bottom
        {
            get
            {
                if (IsEmpty)
                {
                    return Double.NegativeInfinity;
                }

                return _y + _height;
            }
        }

        /// <summary>
        /// TopLeft Property - This is a read-only alias for the Point which is at X, Y
        /// If this is the empty rectangle, the value will be positive infinity, positive infinity.
        /// </summary>
        public Point TopLeft
        {
            get
            {
                return new Point(Left, Top);
            }
        }

        /// <summary>
        /// TopRight Property - This is a read-only alias for the Point which is at X + Width, Y
        /// If this is the empty rectangle, the value will be negative infinity, positive infinity.
        /// </summary>
        public Point TopRight
        {
            get
            {
                return new Point(Right, Top);
            }
        }

        /// <summary>
        /// BottomLeft Property - This is a read-only alias for the Point which is at X, Y + Height
        /// If this is the empty rectangle, the value will be positive infinity, negative infinity.
        /// </summary>
        public Point BottomLeft
        {
            get
            {
                return new Point(Left, Bottom);
            }
        }

        /// <summary>
        /// BottomRight Property - This is a read-only alias for the Point which is at X + Width, Y + Height
        /// If this is the empty rectangle, the value will be negative infinity, negative infinity.
        /// </summary>
        public Point BottomRight
        {
            get
            {
                return new Point(Right, Bottom);
            }
        }
        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Contains - Returns true if the Point is within the rectangle, inclusive of the edges.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="point"> The point which is being tested </param>
        /// <returns>
        /// Returns true if the Point is within the rectangle.
        /// Returns false otherwise
        /// </returns>
        public bool Contains(Point point)
        {
            return Contains(point._x, point._y);
        }

        /// <summary>
        /// Contains - Returns true if the Point represented by x,y is within the rectangle inclusive of the edges.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="x"> X coordinate of the point which is being tested </param>
        /// <param name="y"> Y coordinate of the point which is being tested </param>
        /// <returns>
        /// Returns true if the Point represented by x,y is within the rectangle.
        /// Returns false otherwise.
        /// </returns>
        public bool Contains(double x, double y)
        {
            if (IsEmpty)
            {
                return false;
            }

            return ContainsInternal(x,y);
        }

        /// <summary>
        /// Contains - Returns true if the Rect non-Empty and is entirely contained within the
        /// rectangle, inclusive of the edges.
        /// Returns false otherwise
        /// </summary>
        public bool Contains(Rect rect)
        {
            if (IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            return (_x <= rect._x &&
                    _y <= rect._y &&
                    _x+_width >= rect._x+rect._width &&
                    _y+_height >= rect._y+rect._height );
        }

        /// <summary>
        /// IntersectsWith - Returns true if the Rect intersects with this rectangle
        /// Returns false otherwise.
        /// Note that if one edge is coincident, this is considered an intersection.
        /// </summary>
        /// <returns>
        /// Returns true if the Rect intersects with this rectangle
        /// Returns false otherwise.
        /// or Height
        /// </returns>
        /// <param name="rect"> Rect </param>
        public bool IntersectsWith(Rect rect)
        {
            if (IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            return (rect.Left <= Right) &&
                   (rect.Right >= Left) &&
                   (rect.Top <= Bottom) &&
                   (rect.Bottom >= Top);
        }

        /// <summary>
        /// Intersect - Update this rectangle to be the intersection of this and rect
        /// If either this or rect are Empty, the result is Empty as well.
        /// </summary>
        /// <param name="rect"> The rect to intersect with this </param>
        public void Intersect(Rect rect)
        {
            if (!this.IntersectsWith(rect))
            {
                this = Empty;
            }
            else
            {
                double left   = Math.Max(Left, rect.Left);
                double top    = Math.Max(Top, rect.Top);
                
                //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                _width = Math.Max(Math.Min(Right, rect.Right) - left, 0);
                _height = Math.Max(Math.Min(Bottom, rect.Bottom) - top, 0);

                _x = left;
                _y = top;
            }
        }

        /// <summary>
        /// Intersect - Return the result of the intersection of rect1 and rect2.
        /// If either this or rect are Empty, the result is Empty as well.
        /// </summary>
        public static Rect Intersect(Rect rect1, Rect rect2)
        {
            rect1.Intersect(rect2);
            return rect1;
        }

        /// <summary>
        /// Union - Update this rectangle to be the union of this and rect.
        /// </summary>
        public void Union(Rect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double left = Math.Min(Left, rect.Left);
                double top = Math.Min(Top, rect.Top);

                
                // We need this check so that the math does not result in NaN
                if ((rect.Width == Double.PositiveInfinity) || (Width == Double.PositiveInfinity))
                {
                    _width = Double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)                    
                    double maxRight = Math.Max(Right, rect.Right);
                    _width = Math.Max(maxRight - left, 0);
                }

                // We need this check so that the math does not result in NaN
                if ((rect.Height == Double.PositiveInfinity) || (Height == Double.PositiveInfinity))
                {
                    _height = Double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxBottom = Math.Max(Bottom, rect.Bottom);
                    _height = Math.Max(maxBottom - top, 0);
                }

                _x = left;
                _y = top;
            }
        }

        /// <summary>
        /// Union - Return the result of the union of rect1 and rect2.
        /// </summary>
        public static Rect Union(Rect rect1, Rect rect2)
        {
            rect1.Union(rect2);
            return rect1;
        }

        /// <summary>
        /// Union - Update this rectangle to be the union of this and point.
        /// </summary>
        public void Union(Point point)
        {
            Union(new Rect(point, point));
        }

        /// <summary>
        /// Union - Return the result of the union of rect and point.
        /// </summary>
        public static Rect Union(Rect rect, Point point)
        {
            rect.Union(new Rect(point, point));
            return rect;
        }

        /// <summary>
        /// Offset - translate the Location by the offset provided.
        /// If this is Empty, this method is illegal.
        /// </summary>
        public void Offset(Vector offsetVector)
        {
            if (IsEmpty)
            {
                throw new System.InvalidOperationException(SR.Rect_CannotCallMethod);
            }

            _x += offsetVector._x;
            _y += offsetVector._y;
        }

        /// <summary>
        /// Offset - translate the Location by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        public void Offset(double offsetX, double offsetY)
        {
            if (IsEmpty)
            {
                throw new System.InvalidOperationException(SR.Rect_CannotCallMethod);
            }

            _x += offsetX;
            _y += offsetY;
        }

        /// <summary>
        /// Offset - return the result of offsetting rect by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        public static Rect Offset(Rect rect, Vector offsetVector)
        {
            rect.Offset(offsetVector.X, offsetVector.Y);
            return rect;
        }

        /// <summary>
        /// Offset - return the result of offsetting rect by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        public static Rect Offset(Rect rect, double offsetX, double offsetY)
        {
            rect.Offset(offsetX, offsetY);
            return rect;
        }

        /// <summary>
        /// Inflate - inflate the bounds by the size provided, in all directions
        /// If this is Empty, this method is illegal.
        /// </summary>
        public void Inflate(Size size)
        {
            Inflate(size._width, size._height);
        }

        /// <summary>
        /// Inflate - inflate the bounds by the size provided, in all directions.
        /// If -width is > Width / 2 or -height is > Height / 2, this Rect becomes Empty
        /// If this is Empty, this method is illegal.
        /// </summary>
        public void Inflate(double width, double height)
        {
            if (IsEmpty)
            {
                throw new System.InvalidOperationException(SR.Rect_CannotCallMethod);
            }

            _x -= width;
            _y -= height;
            
            // Do two additions rather than multiplication by 2 to avoid spurious overflow
            // That is: (A + 2 * B) != ((A + B) + B) if 2*B overflows.
            // Note that multiplication by 2 might work in this case because A should start
            // positive & be "clamped" to positive after, but consider A = Inf & B = -MAX.
            _width += width;
            _width += width;
            _height += height;
            _height += height;

            // We catch the case of inflation by less than -width/2 or -height/2 here.  This also
            // maintains the invariant that either the Rect is Empty or _width and _height are
            // non-negative, even if the user parameters were NaN, though this isn't strictly maintained
            // by other methods.
            if ( !(_width >= 0 && _height >= 0) )
            {
                this = s_empty;
            }
        }

        /// <summary>
        /// Inflate - return the result of inflating rect by the size provided, in all directions
        /// If this is Empty, this method is illegal.
        /// </summary>
        public static Rect Inflate(Rect rect, Size size)
        {
            rect.Inflate(size._width, size._height);
            return rect;
        }

        /// <summary>
        /// Inflate - return the result of inflating rect by the size provided, in all directions
        /// If this is Empty, this method is illegal.
        /// </summary>
        public static Rect Inflate(Rect rect, double width, double height)
        {
            rect.Inflate(width, height);
            return rect;
        }

        /// <summary>
        /// Returns the bounds of the transformed rectangle.
        /// The Empty Rect is not affected by this call.
        /// </summary>
        /// <returns>
        /// The rect which results from the transformation.
        /// </returns>
        /// <param name="rect"> The Rect to transform. </param>
        /// <param name="matrix"> The Matrix by which to transform. </param>
        public static Rect Transform(Rect rect, Matrix matrix)
        {
            MatrixUtil.TransformRect(ref rect, ref matrix);
            return rect;
        }
    
        /// <summary>
        /// Updates rectangle to be the bounds of the original value transformed
        /// by the matrix.
        /// The Empty Rect is not affected by this call.        
        /// </summary>
        /// <param name="matrix"> Matrix </param>
        public void Transform(Matrix matrix)
        {
            MatrixUtil.TransformRect(ref this, ref matrix);
        }

        /// <summary>
        /// Scale the rectangle in the X and Y directions
        /// </summary>
        /// <param name="scaleX"> The scale in X </param>
        /// <param name="scaleY"> The scale in Y </param>
        public void Scale(double scaleX, double scaleY)
        {
            if (IsEmpty)
            {
                return;
            }

            _x *= scaleX;
            _y *= scaleY;
            _width *= scaleX;
            _height *= scaleY;

            // If the scale in the X dimension is negative, we need to normalize X and Width
            if (scaleX < 0)
            {
                // Make X the left-most edge again
                _x += _width;

                // and make Width positive
                _width *= -1;
            }

            // Do the same for the Y dimension
            if (scaleY < 0)
            {
                // Make Y the top-most edge again
                _y += _height;

                // and make Height positive
                _height *= -1;
            }
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// ContainsInternal - Performs just the "point inside" logic
        /// </summary>
        /// <returns>
        /// bool - true if the point is inside the rect
        /// </returns>
        /// <param name="x"> The x-coord of the point to test </param>
        /// <param name="y"> The y-coord of the point to test </param>
        private bool ContainsInternal(double x, double y)
        {
            // We include points on the edge as "contained".
            // We do "x - _width <= _x" instead of "x <= _x + _width"
            // so that this check works when _width is PositiveInfinity
            // and _x is NegativeInfinity.
            return ((x >= _x) && (x - _width <= _x) &&
                    (y >= _y) && (y - _height <= _y));
        }

        static private Rect CreateEmptyRect()
        {
            Rect rect = new Rect();
            // We can't set these via the property setters because negatives widths
            // are rejected in those APIs.
            rect._x = Double.PositiveInfinity;
            rect._y = Double.PositiveInfinity;
            rect._width = Double.NegativeInfinity;
            rect._height = Double.NegativeInfinity;
            return rect;
        }

        #endregion Private Methods

        #region Private Fields

        private readonly static Rect s_empty = CreateEmptyRect();

        #endregion Private Fields
    }
}
