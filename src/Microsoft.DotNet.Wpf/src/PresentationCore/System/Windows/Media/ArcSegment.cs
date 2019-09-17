// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description: Impelementation of ArcSegment
//
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Composition;
using System.Windows.Media.Animation;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// ArcSegment
    /// </summary>
    public sealed partial class ArcSegment : PathSegment
    {
        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public ArcSegment() 
        {
        }

        /// <summary>
        ///
        /// </summary>
        public ArcSegment(
            Point point,
            Size size,
            double rotationAngle,
            bool isLargeArc,
            SweepDirection sweepDirection,
            bool isStroked)
        {
            Size = size;
            RotationAngle = rotationAngle;
            IsLargeArc = isLargeArc;
            SweepDirection = sweepDirection;
            Point = point;
            IsStroked = isStroked;
        }

        #endregion

        #region AddToFigure
        internal override void AddToFigure(
            Matrix matrix,          // The transformation matrid
            PathFigure figure,      // The figure to add to
            ref Point current)      // In: Segment start point, Out: Segment endpoint, neither transformed
        {
            Point endPoint = Point;

            if (matrix.IsIdentity)
            {
                figure.Segments.Add(this);
            }
            else
            {
                // The arc segment is approximated by up to 4 Bezier segments
                unsafe
                {
                    int count;
                    Point* points = stackalloc Point[12];
                    Size    size = Size;
                    Double  rotation = RotationAngle;
                    MilMatrix3x2D mat3X2 = CompositionResourceManager.MatrixToMilMatrix3x2D(ref matrix);

                    Composition.MilCoreApi.MilUtility_ArcToBezier(
                        current,    // =start point
                        size,
                        rotation,
                        IsLargeArc,
                        SweepDirection,
                        endPoint,
                        &mat3X2,
                        points,
                        out count); // = number of Bezier segments

                    Invariant.Assert(count <= 4);

                    // To ensure no buffer overflows
                    count = Math.Min(count, 4);

                    bool isStroked = IsStroked;
                    bool isSmoothJoin = IsSmoothJoin;

                    // Add the segments
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            figure.Segments.Add(new BezierSegment(
                                    points[3*i], 
                                    points[3*i + 1], 
                                    points[3*i + 2], 
                                    isStroked, 
                                    (i < count - 1) || isSmoothJoin));    // Smooth join between arc pieces
                        }
                    }
                    else if (count == 0)
                    {
                        figure.Segments.Add(new LineSegment(points[0], isStroked, isSmoothJoin));
                    }
                }

                // Update the last point
                current = endPoint;
            }
        }
        #endregion

        /// <summary>
        /// SerializeData - Serialize the contents of this Segment to the provided context.
        /// </summary>
        internal override void SerializeData(StreamGeometryContext ctx)
        {
            ctx.ArcTo(Point, Size, RotationAngle, IsLargeArc, SweepDirection, IsStroked, IsSmoothJoin);
        }

        internal override bool IsCurved()
        {
            return true;
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            // Helper to get the numeric list separator for a given culture.
            char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);
            return String.Format(provider,
                                 "A{1:" + format + "}{0}{2:" + format + "}{0}{3}{0}{4}{0}{5:" + format + "}",
                                 separator,
                                 Size,
                                 RotationAngle,
                                 IsLargeArc ? "1" : "0",
                                 SweepDirection == SweepDirection.Clockwise ? "1" : "0",
                                 Point);
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 6; }
        }

        /// <summary>
        /// For the purposes of this class, Size.Empty should be treated as if it were Size(0,0).
        /// </summary>
        private static object CoerceSize(DependencyObject d, object value)
        {
            if (((Size)value).IsEmpty)
            {
                return new Size(0,0);
            }
            else
            {
                return value;
            }
        }
    }
}

