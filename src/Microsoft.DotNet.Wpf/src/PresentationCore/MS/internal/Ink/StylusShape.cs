// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.Ink;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    ///<summary>
    /// Defines the style of pen tip for rendering.
    ///</summary>
    /// <remarks>
    /// The Stylus size and coordinates are in units equal to 1/96th of an inch.
    /// The default in V1 the default width is 1 pixel. This is 53 himetric units.
    /// There are 2540 himetric units per inch.
    /// This means that 53 high metric units is equivalent to 53/2540*96 in avalon.
    /// </remarks>
    public abstract class StylusShape
    {
        #region Fields

        private double    m_width;
        private double    m_height;
        private double    m_rotation;
        private Point[]   m_vertices;
        private StylusTip m_tip;
        private Matrix    _transform = Matrix.Identity;

        #endregion

        #region Constructors

        internal StylusShape(){}

        ///<summary>
        /// constructor for a StylusShape.
        ///</summary>
        internal StylusShape(StylusTip tip, double width, double height, double rotation)
        {
            if (Double.IsNaN(width) || Double.IsInfinity(width) || width < DrawingAttributes.MinWidth || width > DrawingAttributes.MaxWidth)
            {
                throw new ArgumentOutOfRangeException("width");
            }

            if (Double.IsNaN(height) || Double.IsInfinity(height) || height < DrawingAttributes.MinHeight || height > DrawingAttributes.MaxHeight)
            {
                throw new ArgumentOutOfRangeException("height");
            }

            if (Double.IsNaN(rotation) || Double.IsInfinity(rotation))
            {
                throw new ArgumentOutOfRangeException("rotation");
            }

            if (!StylusTipHelper.IsDefined(tip))
            {
                throw new ArgumentOutOfRangeException("tip");
            }


            //
            //  mod rotation to 360 (720 to 0, 361 to 1, -270 to 90)
            //
            m_width = width;
            m_height = height;
            m_rotation = rotation == 0 ? 0 : rotation % 360;
            m_tip = tip;
            if (tip == StylusTip.Rectangle)
            {
                ComputeRectangleVertices();
            }
        }

        #endregion

        #region Public properties

        ///<summary>
        /// Width of the non-rotated shape.
        ///</summary>
        public double Width { get { return m_width; } }

        ///<summary>
        /// Height of the non-rotated shape.
        ///</summary>
        public double Height { get { return m_height; } }

        ///<summary>
        /// The shape's rotation angle. The rotation is done about the origin (0,0).
        ///</summary>
        public double Rotation { get { return m_rotation; } }

        /// <summary>
        /// GetVerticesAsVectors
        /// </summary>
        /// <returns></returns>
        internal Vector[] GetVerticesAsVectors()
        {
            Vector[] vertices;

            if (null != m_vertices)
            {
                // For a Rectangle
                vertices = new Vector[m_vertices.Length];

                if (_transform.IsIdentity)
                {
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = (Vector)m_vertices[i];
                    }
                }
                else
                {
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = _transform.Transform((Vector)m_vertices[i]);
                    }

                    // A transform might make the vertices in counter-clockwise order
                    // Fix it if this is the case.
                    FixCounterClockwiseVertices(vertices);
                }
}
            else
            {
                // For ellipse

                // The transform is already applied on these points.
                Point[] p = GetBezierControlPoints();
                vertices = new Vector[p.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = (Vector)p[i];
                }
            }
            return vertices;
        }

        #endregion

        #region Misc. internal API

        /// <summary>
        /// This is the transform on the StylusShape
        /// </summary>
        internal Matrix Transform
        {
            get
            {
                return _transform;
            }
            set
            {
                System.Diagnostics.Debug.Assert(value.HasInverse);
                _transform = value;
            }
        }

        ///<summary>
        /// A helper property.
        ///</summary>
        internal bool IsEllipse { get { return (null == m_vertices); } }

        ///<summary>
        /// A helper property.
        ///</summary>
        internal bool IsPolygon { get { return (null != m_vertices); } }

        /// <summary>
        /// Generally, there's no need for the shape's bounding box.
        /// We use it to approximate v2 shapes with a rectangle for v1.
        /// </summary>
        internal Rect BoundingBox
        {
            get
            {
                Rect bbox;

                if (this.IsPolygon)
                {
                    bbox = Rect.Empty;
                    foreach (Point vertex in m_vertices)
                    {
                        bbox.Union(vertex);
                    }
                }
                // Future enhancement: Implement bbox for rotated ellipses.
                else //if (DoubleUtil.IsZero(m_rotation) || DoubleUtil.AreClose(m_width, m_height))
                {
                    bbox = new Rect(-(m_width * 0.5), -(m_height * 0.5), m_width, m_height);
                }
                //else
                //{
                //    throw new NotImplementedException("Rotated ellipse");
                //}

                return bbox;
            }
        }
        #endregion

        #region Implementation helpers
        /// <summary>TBS</summary>
        private void ComputeRectangleVertices()
        {
            Point topLeft = new Point(-(m_width * 0.5), -(m_height * 0.5));
            m_vertices = new Point[4] { topLeft,
                                        topLeft + new Vector(m_width, 0),
                                        topLeft + new Vector(m_width, m_height),
                                        topLeft + new Vector(0, m_height)};
            if (false == DoubleUtil.IsZero(m_rotation))
            {
                Matrix rotationTransform = Matrix.Identity;
                rotationTransform.Rotate(m_rotation);
                rotationTransform.Transform(m_vertices);
            }
        }


        /// <summary> A transform might make the vertices in counter-clockwise order Fix it if this is the case.</summary>
        private void FixCounterClockwiseVertices(Vector[] vertices)
        {
            // The private method should only called for Rectangle case.
            System.Diagnostics.Debug.Assert(vertices.Length == 4);

            Point prevVertex = (Point)vertices[vertices.Length - 1];
            int counterClockIndex = 0, clockWiseIndex = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                Point vertex = (Point) vertices[i];
                Vector edge = vertex - prevVertex;

                // Verify that the next vertex is on the right side off the edge vector.
                double det = Vector.Determinant(edge, (Point)vertices[(i + 1) % vertices.Length] - (Point)vertex);
                if (0 > det)
                {
                    counterClockIndex++;
                }
                else if (0 < det)
                {
                    clockWiseIndex++;
                }

                prevVertex = vertex;
            }

            // Assert the transform will make it either clockwise or counter-clockwise.
            System.Diagnostics.Debug.Assert(clockWiseIndex == vertices.Length || counterClockIndex == vertices.Length);

            if (counterClockIndex == vertices.Length)
            {
                // Make it Clockwise
                int lastIndex = vertices.Length -1;
                for (int j = 0; j < vertices.Length/2; j++)
                {
                    Vector tmp = vertices[j];
                    vertices[j] = vertices[lastIndex - j];
                    vertices[lastIndex-j] = tmp;
                }
            }
        }


        private Point[] GetBezierControlPoints()
        {
            System.Diagnostics.Debug.Assert(m_tip == StylusTip.Ellipse);

            // Approximating a 1/4 circle with a Bezier curve (borrowed from Avalon's EllipseGeometry.cs)
            const double ArcAsBezier = 0.5522847498307933984; // =(\/2 - 1)*4/3

            double radiusX = m_width / 2;
            double radiusY = m_height / 2;
            double borderMagicX = radiusX * ArcAsBezier;
            double borderMagicY = radiusY * ArcAsBezier;

            Point[] controlPoints = new Point[] {
                new Point(    -radiusX, -borderMagicY),
                new Point(-borderMagicX,     -radiusY),
                new Point(            0,     -radiusY),
                new Point( borderMagicX,     -radiusY),
                new Point(     radiusX, -borderMagicY),
                new Point(     radiusX,             0),
                new Point(     radiusX,  borderMagicY),
                new Point( borderMagicX,      radiusY),
                new Point(            0,      radiusY),
                new Point(-borderMagicX,      radiusY),
                new Point(    -radiusX,  borderMagicY),
                new Point(    -radiusX,             0)};

            // Future enhancement: Apply the transform to the vertices
            // Apply rotation and the shape transform to the control points
            Matrix transform = Matrix.Identity;
            if (m_rotation != 0)
            {
                transform.Rotate(m_rotation);
            }

            if (_transform.IsIdentity == false)
            {
                transform *= _transform;
            }

            if (transform.IsIdentity == false)
            {
                for (int i = 0; i < controlPoints.Length; i++)
                {
                    controlPoints[i] = transform.Transform(controlPoints[i]);
                }
            }

            return controlPoints;
        }

        #endregion
    }

    /// <summary>
    /// Class for an elliptical StylusShape
    /// </summary>
    public sealed class EllipseStylusShape : StylusShape
    {
        /// <summary>
        /// Constructor for an elliptical StylusShape
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public EllipseStylusShape(double width, double height)
                :this(width, height, 0f)
        {
        }

        /// <summary>
        /// Constructor for an ellptical StylusShape ,with roation in degree
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rotation"></param>
        public EllipseStylusShape(double width, double height, double rotation)
                            : base(StylusTip.Ellipse, width, height, rotation)
        {
        }
}

    /// <summary>
    /// Class for a rectangle StylusShape
    /// </summary>
    public sealed class RectangleStylusShape : StylusShape
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public RectangleStylusShape(double width, double height)
                                : this(width, height, 0f)
        {
        }

        /// <summary>
        /// Constructor with rogation in degree
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rotation"></param>
        public RectangleStylusShape(double width, double height, double rotation)
                                    : base(StylusTip.Rectangle, width, height, rotation)
        {
        }
}
}
