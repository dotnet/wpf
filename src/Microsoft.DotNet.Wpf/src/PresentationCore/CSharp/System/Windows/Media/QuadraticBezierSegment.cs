// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// QuadraticBezierSegment
    /// </summary>
    public sealed partial class QuadraticBezierSegment : PathSegment
    {
        #region Constructors
        /// <summary>
        ///
        /// </summary>
        public QuadraticBezierSegment()
        {
        }

        /// <summary>
        ///
        /// </summary>
        public QuadraticBezierSegment(Point point1, Point point2, bool isStroked) 
        {
            Point1 = point1;
            Point2 = point2;
            IsStroked = isStroked;
        }

        /// <summary>
        ///
        /// </summary>
        internal QuadraticBezierSegment(Point point1, Point point2, bool isStroked, bool isSmoothJoin) 
        {
            Point1 = point1;
            Point2 = point2;
            IsStroked = isStroked;
            IsSmoothJoin = isSmoothJoin;
        }

        #endregion

        #region AddToFigure
        internal override void AddToFigure(
            Matrix matrix,          // The transformation matrid
            PathFigure figure,      // The figure to add to
            ref Point current)      // Out: Segment endpoint, not transformed
        {
            current = Point2;

            if (matrix.IsIdentity)
            {
                figure.Segments.Add(this);
            }
            else
            {
                Point pt1 = Point1;
                pt1 *= matrix;

                Point pt2 = current;
                pt2 *= matrix;
                figure.Segments.Add(new QuadraticBezierSegment(pt1, pt2, IsStroked, IsSmoothJoin));
            }
        }
        #endregion

        #region Resource
        
        /// <summary>
        /// SerializeData - Serialize the contents of this Segment to the provided context.
        /// </summary>
        internal override void SerializeData(StreamGeometryContext ctx)
        {
            ctx.QuadraticBezierTo(Point1, Point2, IsStroked, IsSmoothJoin);
        }
        
        #endregion

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
                                 "Q{1:" + format + "}{0}{2:" + format + "}",
                                 separator,
                                 Point1,
                                 Point2);
        }
    }
}

