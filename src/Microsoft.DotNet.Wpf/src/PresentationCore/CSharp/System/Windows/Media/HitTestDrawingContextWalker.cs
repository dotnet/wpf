// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of HitTestDrawingContextWalker.
//              This DrawingContextWalker is used to perform hit tests on renderdata.
//
//

using MS.Internal;
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// HitTestDrawingContextWalker - a DrawingContextWalker which will perform a point or
    /// geometry based hit test on the contents of a render data.
    /// </summary>
    internal abstract class HitTestDrawingContextWalker: DrawingContextWalker
    {
        /// <summary>
        /// Constructor
        /// </summary>
        internal HitTestDrawingContextWalker()
        {
        }

        /// <summary>
        /// IsHit Property - Returns whether the point or geometry intersected the drawing instructions.
        /// </summary>
        internal abstract bool IsHit { get; }

        abstract internal IntersectionDetail IntersectionDetail { get; }

        #region Static Drawing Context Methods

        /// <summary>
        ///     DrawLine -
        ///     Draws a line with the specified pen.
        ///     Note that this API does not accept a Brush, as there is no area to fill.
        /// </summary>
        /// <param name="pen"> The Pen with which to stroke the line. </param>
        /// <param name="point0"> The start Point for the line. </param>
        /// <param name="point1"> The end Point for the line. </param>
        public override void DrawLine(
            Pen pen,
            Point point0,
            Point point1)
        {
            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            DrawGeometry(null /* brush */, pen, new LineGeometry(point0, point1));
        }

        /// <summary>
        ///     DrawRectangle -
        ///     Draw a rectangle with the provided Brush and/or Pen.
        ///     If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        /// <param name="brush">
        ///     The Brush with which to fill the rectangle.
        ///     This is optional, and can be null, in which case no fill is performed.
        /// </param>
        /// <param name="pen">
        ///     The Pen with which to stroke the rectangle.
        ///     This is optional, and can be null, in which case no stroke is performed.
        /// </param>
        /// <param name="rectangle"> The Rect to fill and/or stroke. </param>
        public override void DrawRectangle(
            Brush brush,
            Pen pen,
            Rect rectangle)
        {
            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            DrawGeometry(brush, pen, new RectangleGeometry(rectangle));
        }

        /// <summary>
        ///     DrawRoundedRectangle -
        ///     Draw a rounded rectangle with the provided Brush and/or Pen.
        ///     If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        /// <param name="brush">
        ///     The Brush with which to fill the rectangle.
        ///     This is optional, and can be null, in which case no fill is performed.
        /// </param>
        /// <param name="pen">
        ///     The Pen with which to stroke the rectangle.
        ///     This is optional, and can be null, in which case no stroke is performed.
        /// </param>
        /// <param name="rectangle"> The Rect to fill and/or stroke. </param>
        /// <param name="radiusX">
        ///     The radius in the X dimension of the rounded corners of this
        ///     rounded Rect.  This value will be clamped to the range [0..rectangle.Width/2]
        /// </param>
        /// <param name="radiusY">
        ///     The radius in the Y dimension of the rounded corners of this
        ///     rounded Rect.  This value will be clamped to the range [0..rectangle.Height/2].
        /// </param>
        public override void DrawRoundedRectangle(
            Brush brush,
            Pen pen,
            Rect rectangle,
            Double radiusX,
            Double radiusY)
        {
            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            DrawGeometry(brush, pen, new RectangleGeometry(rectangle, radiusX, radiusY));
        }

        /// <summary>
        ///     DrawEllipse -
        ///     Draw an ellipse with the provided Brush and/or Pen.
        ///     If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        /// <param name="brush">
        ///     The Brush with which to fill the ellipse.
        ///     This is optional, and can be null, in which case no fill is performed.
        /// </param>
        /// <param name="pen">
        ///     The Pen with which to stroke the ellipse.
        ///     This is optional, and can be null, in which case no stroke is performed.
        /// </param>
        /// <param name="center">
        ///     The center of the ellipse to fill and/or stroke.
        /// </param>
        /// <param name="radiusX">
        ///     The radius in the X dimension of the ellipse.
        ///     The absolute value of the radius provided will be used.
        /// </param>
        /// <param name="radiusY">
        ///     The radius in the Y dimension of the ellipse.
        ///     The absolute value of the radius provided will be used.
        /// </param>
        public override void DrawEllipse(
            Brush brush,
            Pen pen,
            Point center,
            Double radiusX,
            Double radiusY)
        {
            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            DrawGeometry(brush, pen, new EllipseGeometry(center, radiusX, radiusY));
        }


        /// <summary>
        ///     DrawImage -
        ///     Draw an Image into the region specified by the Rect.
        ///     The Image will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with an ImageBrush via
        ///     DrawRectangle.
        /// </summary>
        /// <param name="imageSource"> The ImageSource to draw. </param>
        /// <param name="rectangle">
        ///     The Rect into which the ImageSource will be fit.
        /// </param>
        public override void DrawImage(
            ImageSource imageSource,
            Rect rectangle)
        {
            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            ImageBrush imageBrush = new ImageBrush();

            // The ImageSource provided will be shared between the original location and the new ImageBrush
            // we're creating - this will by default break property inheritance, dynamic resource references
            // and databinding.  To prevent this, we mark the new ImageBrush.CanBeInheritanceContext == false.
            imageBrush.CanBeInheritanceContext = false;

            imageBrush.ImageSource = imageSource;

            DrawGeometry(imageBrush, null /* pen */, new RectangleGeometry(rectangle));
        }

        /// <summary>
        ///     DrawVideo -
        ///     Draw a Video into the region specified by the Rect.
        ///     The Video will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with a VideoBrush via
        ///     DrawRectangle.
        /// </summary>
        /// <param name="video"> The MediaPlayer to draw. </param>
        /// <param name="rectangle">
        ///     The Rect into which the MediaPlayer will be fit.
        /// </param>
        public override void DrawVideo(
            MediaPlayer video,
            Rect rectangle)
        {
            // Hit test a rect with a VideoBrush once it exists.
            // DrawGeometry(new VideoBrush(video), null /* pen */, new RectangleGeometry(rectangle));

            // PERF: Consider ways to reduce the allocation of Geometries during managed hit test and bounds passes.
            DrawGeometry(Brushes.Black, null /* pen */, new RectangleGeometry(rectangle));
        }

        #endregion Static Drawing Context Methods

        #region Protected Fields

        // Indicates if the Visual fully contains the hit-test point or geometry
        protected bool _contains;

        #endregion Protected Fields
    }
}

