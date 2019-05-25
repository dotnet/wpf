// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D rectangle implementation. 
//
//              
//
//


using System;
using System.Windows;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.Media3D;
using System.Reflection;
using System.ComponentModel.Design.Serialization;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// The primitive which represents a 3D rectangle, i.e. a box.  Rect3D is stored as
    /// location (Point3D) and rectangle's size (Size3D). As a result, Rect3D cannot have 
    /// negative sizes.
    /// </summary>
    public partial struct Rect3D: IFormattable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor which sets the initial values to the values of the parameters.
        /// </summary>
        /// <param name="location">Location of the new rectangle.</param>
        /// <param name="size">Size of the new rectangle.</param>
        public Rect3D(Point3D location, Size3D size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = location._x;
                _y = location._y;
                _z = location._z;
                _sizeX = size._x;
                _sizeY = size._y;
                _sizeZ = size._z;
            }
            Debug.Assert(size.IsEmpty == IsEmpty);
        }

        /// <summary>
        /// Constructor which sets the initial values to the values of the parameters.
        /// SizeX, sizeY, sizeZ must be non-negative.
        /// </summary>
        /// <param name="x">Value of the X location coordinate of the new rectangle.</param>
        /// <param name="y">Value of the X location coordinate of the new rectangle.</param>
        /// <param name="z">Value of the X location coordinate of the new rectangle.</param>
        /// <param name="sizeX">Size of the new rectangle in X dimension.</param>
        /// <param name="sizeY">Size of the new rectangle in Y dimension.</param>
        /// <param name="sizeZ">Size of the new rectangle in Z dimension.</param>
        public Rect3D(double x, double y, double z, double sizeX, double sizeY, double sizeZ)
        {
            if (sizeX < 0 || sizeY < 0 || sizeZ < 0)
            {
                throw new System.ArgumentException(SR.Get(SRID.Size3D_DimensionCannotBeNegative));
            }

            _x = x;
            _y = y;
            _z = z;
            _sizeX = sizeX;
            _sizeY = sizeY;
            _sizeZ = sizeZ;
        }

        /// <summary>
        /// Constructor which sets the initial values to bound the two points provided.
        /// </summary>
        /// <param name="point1">First point.</param>
        /// <param name="point2">Second point.</param>
        internal Rect3D(Point3D point1, Point3D point2)
        {
            _x = Math.Min(point1._x, point2._x);
            _y = Math.Min(point1._y, point2._y);
            _z = Math.Min(point1._z, point2._z);
            _sizeX = Math.Max(point1._x, point2._x) - _x;
            _sizeY = Math.Max(point1._y, point2._y) - _y;
            _sizeZ = Math.Max(point1._z, point2._z) - _z;
        }

        /// <summary>
        /// Constructor which sets the initial values to bound the point provided and the point
        /// which results from point + vector.
        /// </summary>
        /// <param name="point">Location of the rectangle.</param>
        /// <param name="vector">Vector extending the rectangle from the location.</param>
        internal Rect3D(Point3D point, Vector3D vector): this(point, point+vector)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Empty - a static property which provides an Empty rectangle.  X, Y, and Z are 
        /// positive-infinity and sizes are negative infinity.  This is the only situation
        /// where size can be negative.
        /// </summary>
        public static Rect3D Empty
        {
            get
            {
                return s_empty;
            }
        }

        /// <summary>
        /// IsEmpty - this returns true if this rect is the Empty rectangle.
        /// Note: If size is 0 this Rectangle still contains a 0 or 1 dimensional set
        /// of points, so this method should not be used to check for 0 area.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _sizeX < 0;
            }
        }

        /// <summary>
        /// The point representing the origin of the rectangle.
        /// </summary>
        public Point3D Location
        {
            get
            {
                return new Point3D(_x, _y, _z);
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }

                _x = value._x;
                _y = value._y;
                _z = value._z;
            }
        }

        /// <summary>
        /// The size representing the area of the rectangle.
        /// </summary>
        public Size3D Size
        {
            get
            {
                if( IsEmpty )
                    return Size3D.Empty;
                else
                    return new Size3D(_sizeX, _sizeY, _sizeZ);
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
                        throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                    }

                    _sizeX = value._x;
                    _sizeY = value._y;
                    _sizeZ = value._z;
                }
            }
        }

        /// <summary>
        /// Size of the rectangle in the X dimension.
        /// </summary>
        public double SizeX
        {
            get
            {
                return _sizeX;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }
                
                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Size3D_DimensionCannotBeNegative));
                }

                _sizeX = value;
            }
        }

        /// <summary>
        /// Size of the rectangle in the Y dimension.
        /// </summary>
        public double SizeY
        {
            get
            {
                return _sizeY;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }

                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Size3D_DimensionCannotBeNegative));
                }
                
                _sizeY = value;
            }
        }

        /// <summary>
        /// Size of the rectangle in the Z dimension.
        /// </summary>
        public double SizeZ
        {
            get
            {
                return _sizeZ;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }
                
                if (value < 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.Size3D_DimensionCannotBeNegative));
                }

                _sizeZ = value;
            }
        }

        /// <summary>
        /// Value of the X coordinate of the rectangle.
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
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }

                _x = value;
            }
        }

        /// <summary>
        /// Value of the Y coordinate of the rectangle.
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
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }

                _y = value;
            }
        }

        /// <summary>
        /// Value of the Z coordinate of the rectangle.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotModifyEmptyRect));
                }

                _z = value;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns true if the point is within the rectangle, inclusive of the edges.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="point">The point which is being tested.</param>
        /// <returns>True if the point is within the rectangle. False otherwise</returns>
        public bool Contains(Point3D point)
        {
            return Contains(point._x, point._y, point._z);
        }

        /// <summary>
        /// Contains - Returns true if the Point represented by x,y,z is within the rectangle 
        /// inclusive of the edges. Returns false otherwise.
        /// </summary>
        /// <param name="x">X coordinate of the point which is being tested.</param>
        /// <param name="y">Y coordinate of the point which is being tested.</param>
        /// <param name="z">Y coordinate of the point which is being tested.</param>
        /// <returns> True if the Point represented by x,y is within the rectangle.
        /// False otherwise. </returns>
        public bool Contains(double x, double y, double z)
        {
            if (IsEmpty)
            {
                return false;
            }

            return ContainsInternal(x, y, z);
        }

        /// <summary>
        /// Returns true if the rectangle is non-Empty and is entirely contained within the
        /// rectangle, inclusive of the edges. Returns false otherwise.
        /// </summary>
        /// <param name="rect">Rectangle being tested.</param>
        /// <returns>Returns true if the rectangle is non-Empty and is entirely contained within the
        /// rectangle, inclusive of the edges. Returns false otherwise.</returns>
        public bool Contains(Rect3D rect)
        {
            if (IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            return (_x <= rect._x &&
                    _y <= rect._y &&
                    _z <= rect._z &&
                    _x+_sizeX >= rect._x+rect._sizeX &&
                    _y+_sizeY >= rect._y+rect._sizeY &&
                    _z+_sizeZ >= rect._z+rect._sizeZ);
        }

        /// <summary>
        /// Returns true if the rectangle intersects with this rectangle. 
        /// Returns false otherwise. Note that if one edge is coincident, this is considered 
        /// an intersection.
        /// </summary>
        /// <param name="rect">Rectangle being tested.</param>
        /// <returns>True if the rectangle intersects with this rectangle. 
        /// False otherwise.</returns>
        public bool IntersectsWith(Rect3D rect)
        {
            if (IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            return (rect._x                 <= (_x + _sizeX)) &&
                   ((rect._x + rect._sizeX) >= _x)            &&
                   (rect._y                 <= (_y + _sizeY)) &&
                   ((rect._y + rect._sizeY) >= _y)            &&
                   (rect._z                 <= (_z + _sizeZ)) &&
                   ((rect._z + rect._sizeZ) >= _z);
        }

        /// <summary>
        /// Intersect - Update this rectangle to be the intersection of this and rect
        /// If either this or rect are Empty, the result is Empty as well.
        /// </summary>
        /// <param name="rect"> The rect to intersect with this </param>
        public void Intersect(Rect3D rect)
        {
            if (IsEmpty || rect.IsEmpty || !this.IntersectsWith(rect))
            {
                this = Empty;
            }
            else
            {
                double x = Math.Max(_x, rect._x);
                double y = Math.Max(_y, rect._y);
                double z = Math.Max(_z, rect._z);
                _sizeX = Math.Min(_x + _sizeX, rect._x + rect._sizeX) - x;
                _sizeY = Math.Min(_y + _sizeY, rect._y + rect._sizeY) - y;
                _sizeZ = Math.Min(_z + _sizeZ, rect._z + rect._sizeZ) - z;

                _x = x;
                _y = y;
                _z = z;
            }
        }

        /// <summary>
        /// Return the result of the intersection of rect1 and rect2.
        /// If either this or rect are Empty, the result is Empty as well.
        /// </summary>
        /// <param name="rect1">First rectangle.</param>
        /// <param name="rect2">Second rectangle.</param>
        /// <returns>The result of the intersection of rect1 and rect2.</returns>
        public static Rect3D Intersect(Rect3D rect1, Rect3D rect2)
        {
            rect1.Intersect(rect2);
            return rect1;
        }

        /// <summary>
        /// Update this rectangle to be the union of this and rect.
        /// </summary>
        /// <param name="rect">Rectangle.</param>
        public void Union(Rect3D rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double x = Math.Min(_x, rect._x);
                double y = Math.Min(_y, rect._y);
                double z = Math.Min(_z, rect._z);
                _sizeX = Math.Max(_x + _sizeX, rect._x + rect._sizeX) - x;
                _sizeY = Math.Max(_y + _sizeY, rect._y + rect._sizeY) - y;
                _sizeZ = Math.Max(_z + _sizeZ, rect._z + rect._sizeZ) - z;
                _x = x;
                _y = y;
                _z = z;
            }
        }

        /// <summary>
        /// Return the result of the union of rect1 and rect2.
        /// </summary>
        /// <param name="rect1">First rectangle.</param>
        /// <param name="rect2">Second rectangle.</param>
        /// <returns>The result of the union of the two rectangles.</returns>
        public static Rect3D Union(Rect3D rect1, Rect3D rect2)
        {
            rect1.Union(rect2);
            return rect1;
        }

        /// <summary>
        /// Update this rectangle to be the union of this and point.
        /// </summary>
        /// <param name="point">Point.</param>
        public void Union(Point3D point)
        {
            Union(new Rect3D(point, point));
        }

        /// <summary>
        /// Return the result of the union of rect and point.
        /// </summary>
        /// <param name="rect">Rectangle.</param>
        /// <param name="point">Point.</param>
        /// <returns>The result of the union of rect and point.</returns>
        public static Rect3D Union(Rect3D rect, Point3D point)
        {
            rect.Union(new Rect3D(point, point));
            return rect;
        }

        /// <summary>
        /// Translate the Location by the offset provided.
        /// If this is Empty, this method is illegal.
        /// </summary>
        /// <param name="offsetVector"></param>
        public void Offset(Vector3D offsetVector)
        {
            Offset(offsetVector._x, offsetVector._y, offsetVector._z);
        }

        /// <summary>
        /// Offset - translate the Location by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="offsetZ"></param>
        public void Offset(double offsetX, double offsetY, double offsetZ)
        {
            if (IsEmpty)
            {
                throw new System.InvalidOperationException(SR.Get(SRID.Rect3D_CannotCallMethod));
            }
            
            _x += offsetX;
            _y += offsetY;
            _z += offsetZ;
        }

        /// <summary>
        /// Offset - return the result of offsetting rect by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="offsetVector"></param>
        /// <returns></returns>
        public static Rect3D Offset(Rect3D rect, Vector3D offsetVector)
        {
            rect.Offset(offsetVector._x, offsetVector._y, offsetVector._z);
            return rect;
        }

        /// <summary>
        /// Offset - return the result of offsetting rect by the offset provided
        /// If this is Empty, this method is illegal.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="offsetZ"></param>
        /// <returns></returns>
        public static Rect3D Offset(Rect3D rect, double offsetX, double offsetY, double offsetZ)
        {
            rect.Offset(offsetX, offsetY, offsetZ);
            return rect;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------
        
        #region Internal Fields

        internal readonly static Rect3D Infinite = CreateInfiniteRect3D();
        
        #endregion Internal Fields
        
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        
        #region Private Methods

        /// <summary>
        /// ContainsInternal - Performs just the "point inside" logic.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>bool - true if the point is inside the rect</returns>
        private bool ContainsInternal(double x, double y, double z)
        {
            // We include points on the edge as "contained"
            return ((x >= _x) && (x <= _x + _sizeX) &&
                    (y >= _y) && (y <= _y + _sizeY) &&
                    (z >= _z) && (z <= _z + _sizeZ));
        }

        private static Rect3D CreateEmptyRect3D()
        {
            Rect3D empty = new Rect3D();
            empty._x = Double.PositiveInfinity;
            empty._y = Double.PositiveInfinity;
            empty._z = Double.PositiveInfinity;
            // Can't use setters because they throw on negative values
            empty._sizeX = Double.NegativeInfinity;
            empty._sizeY = Double.NegativeInfinity;
            empty._sizeZ = Double.NegativeInfinity;
            return empty;
        }

        private static Rect3D CreateInfiniteRect3D()
        {
            // Robustness with infinities
            //
            //   Once the issue with Rect robustness with infinities is addressed we
            //   should change the values below to make this rectangle truely extend
            //   from -Infinite to +Infinity.
            //
            //   Until then we use a Rect from -float.MaxValue to +float.MaxValue.
            //   Because this rect is used only as a conservative bounding box for
            //   ScreenSpaceLines3D this span should be sufficient for the following
            //   reasons:
            //
            //     1.  Our meshes and transforms are reprensented in single precision
            //         at render time.  If it's not in this range it will not be
            //         rendered.
            //
            //     2.  SSLines3Ds are constructed as simple quads at render time.
            //         We will hit the guard band on the GPU at a limit far less than
            //         +/- float.MaxValue.
            //
            //     3.  We do our managed math in double precision so this still
            //         leaves us ample space to account for transforms, etc.
            //

            Rect3D infinite = new Rect3D();
            infinite._x = -float.MaxValue;
            infinite._y = -float.MaxValue;
            infinite._z = -float.MaxValue;
            infinite._sizeX = float.MaxValue*2.0;
            infinite._sizeY = float.MaxValue*2.0;
            infinite._sizeZ = float.MaxValue*2.0;
            return infinite;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly static Rect3D s_empty = CreateEmptyRect3D();

        #endregion Private Fields
    }
}
