// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    /// DrawingContextDrawingContextWalker is a DrawingContextWalker
    /// that forwards all of it's calls to a DrawingContext.
    /// </summary>
    internal partial class DrawingContextDrawingContextWalker: DrawingContextWalker
    {
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
            _drawingContext.DrawLine(
                pen,
                point0,
                point1
                );
        }

        /// <summary>
        ///     DrawLine - 
        ///     Draws a line with the specified pen.
        ///     Note that this API does not accept a Brush, as there is no area to fill.
        /// </summary>
        /// <param name="pen"> The Pen with which to stroke the line. </param>
        /// <param name="point0"> The start Point for the line. </param>
        /// <param name="point0Animations"> Optional AnimationClock for point0. </param>
        /// <param name="point1"> The end Point for the line. </param>
        /// <param name="point1Animations"> Optional AnimationClock for point1. </param>
        public override void DrawLine(
            Pen pen,
            Point point0,
            AnimationClock point0Animations,
            Point point1,
            AnimationClock point1Animations)
        {
            _drawingContext.DrawLine(
                pen,
                point0,
                point0Animations,
                point1,
                point1Animations
                );
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
            _drawingContext.DrawRectangle(
                brush,
                pen,
                rectangle
                );
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
        /// <param name="rectangleAnimations"> Optional AnimationClock for rectangle. </param>
        public override void DrawRectangle(
            Brush brush,
            Pen pen,
            Rect rectangle,
            AnimationClock rectangleAnimations)
        {
            _drawingContext.DrawRectangle(
                brush,
                pen,
                rectangle,
                rectangleAnimations
                );
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
            _drawingContext.DrawRoundedRectangle(
                brush,
                pen,
                rectangle,
                radiusX,
                radiusY
                );
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
        /// <param name="rectangleAnimations"> Optional AnimationClock for rectangle. </param>
        /// <param name="radiusX">
        ///     The radius in the X dimension of the rounded corners of this
        ///     rounded Rect.  This value will be clamped to the range [0..rectangle.Width/2]
        /// </param>
        /// <param name="radiusXAnimations"> Optional AnimationClock for radiusX. </param>
        /// <param name="radiusY">
        ///     The radius in the Y dimension of the rounded corners of this
        ///     rounded Rect.  This value will be clamped to the range [0..rectangle.Height/2].
        /// </param>
        /// <param name="radiusYAnimations"> Optional AnimationClock for radiusY. </param>
        public override void DrawRoundedRectangle(
            Brush brush,
            Pen pen,
            Rect rectangle,
            AnimationClock rectangleAnimations,
            Double radiusX,
            AnimationClock radiusXAnimations,
            Double radiusY,
            AnimationClock radiusYAnimations)
        {
            _drawingContext.DrawRoundedRectangle(
                brush,
                pen,
                rectangle,
                rectangleAnimations,
                radiusX,
                radiusXAnimations,
                radiusY,
                radiusYAnimations
                );
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
            _drawingContext.DrawEllipse(
                brush,
                pen,
                center,
                radiusX,
                radiusY
                );
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
        /// <param name="centerAnimations"> Optional AnimationClock for center. </param>
        /// <param name="radiusX">
        ///     The radius in the X dimension of the ellipse.
        ///     The absolute value of the radius provided will be used.
        /// </param>
        /// <param name="radiusXAnimations"> Optional AnimationClock for radiusX. </param>
        /// <param name="radiusY">
        ///     The radius in the Y dimension of the ellipse.
        ///     The absolute value of the radius provided will be used.
        /// </param>
        /// <param name="radiusYAnimations"> Optional AnimationClock for radiusY. </param>
        public override void DrawEllipse(
            Brush brush,
            Pen pen,
            Point center,
            AnimationClock centerAnimations,
            Double radiusX,
            AnimationClock radiusXAnimations,
            Double radiusY,
            AnimationClock radiusYAnimations)
        {
            _drawingContext.DrawEllipse(
                brush,
                pen,
                center,
                centerAnimations,
                radiusX,
                radiusXAnimations,
                radiusY,
                radiusYAnimations
                );
        }
        /// <summary>
        ///     DrawGeometry - 
        ///     Draw a Geometry with the provided Brush and/or Pen.
        ///     If both the Brush and Pen are null this call is a no-op.
        /// </summary>
        /// <param name="brush">
        ///     The Brush with which to fill the Geometry.
        ///     This is optional, and can be null, in which case no fill is performed.
        /// </param>
        /// <param name="pen">
        ///     The Pen with which to stroke the Geometry.
        ///     This is optional, and can be null, in which case no stroke is performed.
        /// </param>
        /// <param name="geometry"> The Geometry to fill and/or stroke. </param>
        public override void DrawGeometry(
            Brush brush,
            Pen pen,
            Geometry geometry)
        {
            _drawingContext.DrawGeometry(
                brush,
                pen,
                geometry
                );
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
            _drawingContext.DrawImage(
                imageSource,
                rectangle
                );
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
        /// <param name="rectangleAnimations"> Optional AnimationClock for rectangle. </param>
        public override void DrawImage(
            ImageSource imageSource,
            Rect rectangle,
            AnimationClock rectangleAnimations)
        {
            _drawingContext.DrawImage(
                imageSource,
                rectangle,
                rectangleAnimations
                );
        }
        /// <summary>
        ///     DrawGlyphRun - 
        ///     Draw a GlyphRun
        /// </summary>
        /// <param name="foregroundBrush">
        ///     Foreground brush to draw the GlyphRun with.
        /// </param>
        /// <param name="glyphRun"> The GlyphRun to draw.  </param>
        public override void DrawGlyphRun(
            Brush foregroundBrush,
            GlyphRun glyphRun)
        {
            _drawingContext.DrawGlyphRun(
                foregroundBrush,
                glyphRun
                );
        }

        /// <summary>
        ///     DrawDrawing - 
        ///     Draw a Drawing by appending a sub-Drawing to the current Drawing.
        /// </summary>
        /// <param name="drawing"> The drawing to draw. </param>
        public override void DrawDrawing(
            Drawing drawing)
        {
            _drawingContext.DrawDrawing(
                drawing
                );
        }

        /// <summary>
        ///     DrawVideo - 
        ///     Draw a Video into the region specified by the Rect.
        ///     The Video will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with an VideoBrush via 
        ///     DrawRectangle.
        /// </summary>
        /// <param name="player"> The MediaPlayer to draw. </param>
        /// <param name="rectangle"> The Rect into which the media will be fit. </param>
        public override void DrawVideo(
            MediaPlayer player,
            Rect rectangle)
        {
            _drawingContext.DrawVideo(
                player,
                rectangle
                );
        }

        /// <summary>
        ///     DrawVideo - 
        ///     Draw a Video into the region specified by the Rect.
        ///     The Video will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with an VideoBrush via 
        ///     DrawRectangle.
        /// </summary>
        /// <param name="player"> The MediaPlayer to draw. </param>
        /// <param name="rectangle"> The Rect into which the media will be fit. </param>
        /// <param name="rectangleAnimations"> Optional AnimationClock for rectangle. </param>
        public override void DrawVideo(
            MediaPlayer player,
            Rect rectangle,
            AnimationClock rectangleAnimations)
        {
            _drawingContext.DrawVideo(
                player,
                rectangle,
                rectangleAnimations
                );
        }
        /// <summary>
        ///     PushClip - 
        ///     Push a clip region, which will apply to all drawing primitives until the 
        ///     corresponding Pop call.
        /// </summary>
        /// <param name="clipGeometry"> The Geometry to which we will clip. </param>
        public override void PushClip(
            Geometry clipGeometry)
        {
            _drawingContext.PushClip(
                clipGeometry
                );
        }

        /// <summary>
        ///     PushOpacityMask - 
        ///     Push an opacity mask which will blend the composite of all drawing primitives added 
        ///     until the corresponding Pop call.
        /// </summary>
        /// <param name="opacityMask"> The opacity mask </param>
        public override void PushOpacityMask(
            Brush opacityMask)
        {
            _drawingContext.PushOpacityMask(
                opacityMask
                );
        }

        /// <summary>
        ///     PushOpacity - 
        ///     Push an opacity which will blend the composite of all drawing primitives added 
        ///     until the corresponding Pop call.
        /// </summary>
        /// <param name="opacity">
        ///     The opacity with which to blend - 0 is transparent, 1 is opaque.
        /// </param>
        public override void PushOpacity(
            Double opacity)
        {
            _drawingContext.PushOpacity(
                opacity
                );
        }

        /// <summary>
        ///     PushOpacity - 
        ///     Push an opacity which will blend the composite of all drawing primitives added 
        ///     until the corresponding Pop call.
        /// </summary>
        /// <param name="opacity">
        ///     The opacity with which to blend - 0 is transparent, 1 is opaque.
        /// </param>
        /// <param name="opacityAnimations"> Optional AnimationClock for opacity. </param>
        public override void PushOpacity(
            Double opacity,
            AnimationClock opacityAnimations)
        {
            _drawingContext.PushOpacity(
                opacity,
                opacityAnimations
                );
        }
        /// <summary>
        ///     PushTransform - 
        ///     Push a Transform which will apply to all drawing operations until the corresponding 
        ///     Pop.
        /// </summary>
        /// <param name="transform"> The Transform to push. </param>
        public override void PushTransform(
            Transform transform)
        {
            _drawingContext.PushTransform(
                transform
                );
        }

        /// <summary>
        ///     PushGuidelineSet - 
        ///     Push a set of guidelines which will apply to all drawing operations until the 
        ///     corresponding Pop.
        /// </summary>
        /// <param name="guidelines"> The GuidelineSet to push. </param>
        public override void PushGuidelineSet(
            GuidelineSet guidelines)
        {
            _drawingContext.PushGuidelineSet(
                guidelines
                );
        }

        /// <summary>
        ///     PushGuidelineY1 - 
        ///     Explicitly push one horizontal guideline.
        /// </summary>
        /// <param name="coordinate"> The coordinate of leading guideline. </param>
        internal override void PushGuidelineY1(
            Double coordinate)
        {
            _drawingContext.PushGuidelineY1(
                coordinate
                );
        }

        /// <summary>
        ///     PushGuidelineY2 - 
        ///     Explicitly push a pair of horizontal guidelines.
        /// </summary>
        /// <param name="leadingCoordinate">
        ///     The coordinate of leading guideline.
        /// </param>
        /// <param name="offsetToDrivenCoordinate">
        ///     The offset from leading guideline to driven guideline.
        /// </param>
        internal override void PushGuidelineY2(
            Double leadingCoordinate,
            Double offsetToDrivenCoordinate)
        {
            _drawingContext.PushGuidelineY2(
                leadingCoordinate,
                offsetToDrivenCoordinate
                );
        }

        /// <summary>
        ///     PushEffect - 
        ///     Push a BitmapEffect which will apply to all drawing operations until the 
        ///     corresponding Pop.
        /// </summary>
        /// <param name="effect"> The BitmapEffect to push. </param>
        /// <param name="effectInput"> The BitmapEffectInput. </param>
        [Obsolete(MS.Internal.Media.VisualTreeUtils.BitmapEffectObsoleteMessage)]
        public override void PushEffect(
            BitmapEffect effect,
            BitmapEffectInput effectInput)
        {
            _drawingContext.PushEffect(
                effect,
                effectInput
                );
        }

        /// <summary>
        /// Pop
        /// </summary>
        public override void Pop(
            )
        {
            _drawingContext.Pop(

                );
        }
}
}
