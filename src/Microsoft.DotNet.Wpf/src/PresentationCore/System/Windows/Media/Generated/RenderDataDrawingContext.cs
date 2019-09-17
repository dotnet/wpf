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
    ///     RenderDataDrawingContext - A DrawingContext which produces a Drawing.
    /// </summary>
    internal partial class RenderDataDrawingContext : DrawingContext
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
            VerifyApiNonstructuralChange();

            if (pen == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawLine(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_LINE record =
                    new MILCMD_DRAW_LINE (
                        _renderData.AddDependentResource(pen),
                        point0,
                        point1
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_LINE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawLine,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_LINE) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if (pen == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawLine(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                UInt32 hPoint0Animations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hPoint1Animations = CompositionResourceManager.InvalidResourceHandle;
                hPoint0Animations = UseAnimations(point0, point0Animations);
                hPoint1Animations = UseAnimations(point1, point1Animations);

                MILCMD_DRAW_LINE_ANIMATE record =
                    new MILCMD_DRAW_LINE_ANIMATE (
                        _renderData.AddDependentResource(pen),
                        point0,
                        hPoint0Animations,
                        point1,
                        hPoint1Animations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_LINE_ANIMATE) == 48);

                _renderData.WriteDataRecord(MILCMD.MilDrawLineAnimate,
                                            (byte*)&record,
                                            48 /* sizeof(MILCMD_DRAW_LINE_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawRectangle(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_RECTANGLE record =
                    new MILCMD_DRAW_RECTANGLE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        rectangle
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_RECTANGLE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawRectangle,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_RECTANGLE) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawRectangle(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                UInt32 hRectangleAnimations = CompositionResourceManager.InvalidResourceHandle;
                hRectangleAnimations = UseAnimations(rectangle, rectangleAnimations);

                MILCMD_DRAW_RECTANGLE_ANIMATE record =
                    new MILCMD_DRAW_RECTANGLE_ANIMATE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        rectangle,
                        hRectangleAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_RECTANGLE_ANIMATE) == 48);

                _renderData.WriteDataRecord(MILCMD.MilDrawRectangleAnimate,
                                            (byte*)&record,
                                            48 /* sizeof(MILCMD_DRAW_RECTANGLE_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawRoundedRectangle(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_ROUNDED_RECTANGLE record =
                    new MILCMD_DRAW_ROUNDED_RECTANGLE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        rectangle,
                        radiusX,
                        radiusY
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE) == 56);

                _renderData.WriteDataRecord(MILCMD.MilDrawRoundedRectangle,
                                            (byte*)&record,
                                            56 /* sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawRoundedRectangle(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                UInt32 hRectangleAnimations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hRadiusXAnimations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hRadiusYAnimations = CompositionResourceManager.InvalidResourceHandle;
                hRectangleAnimations = UseAnimations(rectangle, rectangleAnimations);
                hRadiusXAnimations = UseAnimations(radiusX, radiusXAnimations);
                hRadiusYAnimations = UseAnimations(radiusY, radiusYAnimations);

                MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE record =
                    new MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        rectangle,
                        hRectangleAnimations,
                        radiusX,
                        hRadiusXAnimations,
                        radiusY,
                        hRadiusYAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE) == 72);

                _renderData.WriteDataRecord(MILCMD.MilDrawRoundedRectangleAnimate,
                                            (byte*)&record,
                                            72 /* sizeof(MILCMD_DRAW_ROUNDED_RECTANGLE_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawEllipse(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_ELLIPSE record =
                    new MILCMD_DRAW_ELLIPSE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        center,
                        radiusX,
                        radiusY
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_ELLIPSE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawEllipse,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_ELLIPSE) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if ((brush == null) && (pen == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawEllipse(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                UInt32 hCenterAnimations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hRadiusXAnimations = CompositionResourceManager.InvalidResourceHandle;
                UInt32 hRadiusYAnimations = CompositionResourceManager.InvalidResourceHandle;
                hCenterAnimations = UseAnimations(center, centerAnimations);
                hRadiusXAnimations = UseAnimations(radiusX, radiusXAnimations);
                hRadiusYAnimations = UseAnimations(radiusY, radiusYAnimations);

                MILCMD_DRAW_ELLIPSE_ANIMATE record =
                    new MILCMD_DRAW_ELLIPSE_ANIMATE (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        center,
                        hCenterAnimations,
                        radiusX,
                        hRadiusXAnimations,
                        radiusY,
                        hRadiusYAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_ELLIPSE_ANIMATE) == 56);

                _renderData.WriteDataRecord(MILCMD.MilDrawEllipseAnimate,
                                            (byte*)&record,
                                            56 /* sizeof(MILCMD_DRAW_ELLIPSE_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();

            if (((brush == null) && (pen == null)) || (geometry == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawGeometry(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_GEOMETRY record =
                    new MILCMD_DRAW_GEOMETRY (
                        _renderData.AddDependentResource(brush),
                        _renderData.AddDependentResource(pen),
                        _renderData.AddDependentResource(geometry)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_GEOMETRY) == 16);

                _renderData.WriteDataRecord(MILCMD.MilDrawGeometry,
                                            (byte*)&record,
                                            16 /* sizeof(MILCMD_DRAW_GEOMETRY) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if (imageSource == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawImage(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_DRAW_IMAGE record =
                    new MILCMD_DRAW_IMAGE (
                        _renderData.AddDependentResource(imageSource),
                        rectangle
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_IMAGE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawImage,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_IMAGE) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if (imageSource == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawImage(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();



                UInt32 hRectangleAnimations = CompositionResourceManager.InvalidResourceHandle;
                hRectangleAnimations = UseAnimations(rectangle, rectangleAnimations);

                MILCMD_DRAW_IMAGE_ANIMATE record =
                    new MILCMD_DRAW_IMAGE_ANIMATE (
                        _renderData.AddDependentResource(imageSource),
                        rectangle,
                        hRectangleAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_IMAGE_ANIMATE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawImageAnimate,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_IMAGE_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();

            if ((foregroundBrush == null) || (glyphRun == null))
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawGlyphRun(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_DRAW_GLYPH_RUN record =
                    new MILCMD_DRAW_GLYPH_RUN (
                        _renderData.AddDependentResource(foregroundBrush),
                        _renderData.AddDependentResource(glyphRun)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_GLYPH_RUN) == 8);

                _renderData.WriteDataRecord(MILCMD.MilDrawGlyphRun,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_DRAW_GLYPH_RUN) */);
            }                           
}

        /// <summary>
        ///     DrawDrawing - 
        ///     Draw a Drawing by appending a sub-Drawing to the current Drawing.
        /// </summary>
        /// <param name="drawing"> The drawing to draw. </param>
        public override void DrawDrawing(
            Drawing drawing)
        {
            VerifyApiNonstructuralChange();

            if (drawing == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawDrawing(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_DRAW_DRAWING record =
                    new MILCMD_DRAW_DRAWING (
                        _renderData.AddDependentResource(drawing)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_DRAWING) == 8);

                _renderData.WriteDataRecord(MILCMD.MilDrawDrawing,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_DRAW_DRAWING) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if (player == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawVideo(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_DRAW_VIDEO record =
                    new MILCMD_DRAW_VIDEO (
                        _renderData.AddDependentResource(player),
                        rectangle
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_VIDEO) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawVideo,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_VIDEO) */);
            }                           
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
            VerifyApiNonstructuralChange();

            if (player == null)
            {
                return;
            }


        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("DrawVideo(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();



                UInt32 hRectangleAnimations = CompositionResourceManager.InvalidResourceHandle;
                hRectangleAnimations = UseAnimations(rectangle, rectangleAnimations);

                MILCMD_DRAW_VIDEO_ANIMATE record =
                    new MILCMD_DRAW_VIDEO_ANIMATE (
                        _renderData.AddDependentResource(player),
                        rectangle,
                        hRectangleAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_DRAW_VIDEO_ANIMATE) == 40);

                _renderData.WriteDataRecord(MILCMD.MilDrawVideoAnimate,
                                            (byte*)&record,
                                            40 /* sizeof(MILCMD_DRAW_VIDEO_ANIMATE) */);
            }                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushClip(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_CLIP record =
                    new MILCMD_PUSH_CLIP (
                        _renderData.AddDependentResource(clipGeometry)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_CLIP) == 8);

                _renderData.WriteDataRecord(MILCMD.MilPushClip,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_PUSH_CLIP) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushOpacityMask(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                // Always assume visual and drawing brushes need realization updates

                MILCMD_PUSH_OPACITY_MASK record =
                    new MILCMD_PUSH_OPACITY_MASK (
                        _renderData.AddDependentResource(opacityMask)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_OPACITY_MASK) == 24);

                _renderData.WriteDataRecord(MILCMD.MilPushOpacityMask,
                                            (byte*)&record,
                                            24 /* sizeof(MILCMD_PUSH_OPACITY_MASK) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushOpacity(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_OPACITY record =
                    new MILCMD_PUSH_OPACITY (
                        opacity
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_OPACITY) == 8);

                _renderData.WriteDataRecord(MILCMD.MilPushOpacity,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_PUSH_OPACITY) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushOpacity(animate)");
        #endif

            unsafe
            {
                EnsureRenderData();



                UInt32 hOpacityAnimations = CompositionResourceManager.InvalidResourceHandle;
                hOpacityAnimations = UseAnimations(opacity, opacityAnimations);

                MILCMD_PUSH_OPACITY_ANIMATE record =
                    new MILCMD_PUSH_OPACITY_ANIMATE (
                        opacity,
                        hOpacityAnimations
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_OPACITY_ANIMATE) == 16);

                _renderData.WriteDataRecord(MILCMD.MilPushOpacityAnimate,
                                            (byte*)&record,
                                            16 /* sizeof(MILCMD_PUSH_OPACITY_ANIMATE) */);
            }                            

            _stackDepth++;
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushTransform(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_TRANSFORM record =
                    new MILCMD_PUSH_TRANSFORM (
                        _renderData.AddDependentResource(transform)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_TRANSFORM) == 8);

                _renderData.WriteDataRecord(MILCMD.MilPushTransform,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_PUSH_TRANSFORM) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushGuidelineSet(const)");
        #endif

            unsafe
            {
                EnsureRenderData();

                if (guidelines != null && guidelines.IsFrozen && guidelines.IsDynamic)
                {
                    DoubleCollection guidelinesX = guidelines.GuidelinesX;
                    DoubleCollection guidelinesY = guidelines.GuidelinesY;
                    int countX = guidelinesX == null ? 0 : guidelinesX.Count;
                    int countY = guidelinesY == null ? 0 : guidelinesY.Count;

                    if (countX == 0 && (countY == 1 || countY == 2)
                        )
                    {
                        if (countY == 1)
                        {
                            MILCMD_PUSH_GUIDELINE_Y1 record =
                                new MILCMD_PUSH_GUIDELINE_Y1(
                                    guidelinesY[0]
                                    );

                            _renderData.WriteDataRecord(
                                MILCMD.MilPushGuidelineY1,
                                (byte*)&record,
                                sizeof(MILCMD_PUSH_GUIDELINE_Y1)
                                );
                        }
                        else
                        {
                            MILCMD_PUSH_GUIDELINE_Y2 record =
                                new MILCMD_PUSH_GUIDELINE_Y2(
                                    guidelinesY[0],
                                    guidelinesY[1] - guidelinesY[0]
                                    );

                            _renderData.WriteDataRecord(
                                MILCMD.MilPushGuidelineY2,
                                (byte*)&record,
                                sizeof(MILCMD_PUSH_GUIDELINE_Y2)
                                );
                        }
                    }
                }
                else
                {
                    MILCMD_PUSH_GUIDELINE_SET record =
                        new MILCMD_PUSH_GUIDELINE_SET (
                            _renderData.AddDependentResource(guidelines)
                            );

                    // Assert that the calculated packet size is the same as the size returned by sizeof().
                    Debug.Assert(sizeof(MILCMD_PUSH_GUIDELINE_SET) == 8);

                    _renderData.WriteDataRecord(MILCMD.MilPushGuidelineSet,
                                                (byte*)&record,
                                                8 /* sizeof(MILCMD_PUSH_GUIDELINE_SET) */);
                }
            }

            _stackDepth++;
        }

        /// <summary>
        ///     PushGuidelineY1 - 
        ///     Explicitly push one horizontal guideline.
        /// </summary>
        /// <param name="coordinate"> The coordinate of leading guideline. </param>
        internal override void PushGuidelineY1(
            Double coordinate)
        {
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushGuidelineY1(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_GUIDELINE_Y1 record =
                    new MILCMD_PUSH_GUIDELINE_Y1 (
                        coordinate
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_GUIDELINE_Y1) == 8);

                _renderData.WriteDataRecord(MILCMD.MilPushGuidelineY1,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_PUSH_GUIDELINE_Y1) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushGuidelineY2(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_GUIDELINE_Y2 record =
                    new MILCMD_PUSH_GUIDELINE_Y2 (
                        leadingCoordinate,
                        offsetToDrivenCoordinate
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_GUIDELINE_Y2) == 16);

                _renderData.WriteDataRecord(MILCMD.MilPushGuidelineY2,
                                            (byte*)&record,
                                            16 /* sizeof(MILCMD_PUSH_GUIDELINE_Y2) */);
            }                           

            _stackDepth++;                            
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
            VerifyApiNonstructuralChange();



        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("PushEffect(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_PUSH_EFFECT record =
                    new MILCMD_PUSH_EFFECT (
                        _renderData.AddDependentResource(effect),
                        _renderData.AddDependentResource(effectInput)
                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                Debug.Assert(sizeof(MILCMD_PUSH_EFFECT) == 8);

                _renderData.WriteDataRecord(MILCMD.MilPushEffect,
                                            (byte*)&record,
                                            8 /* sizeof(MILCMD_PUSH_EFFECT) */);
            }                           

            _stackDepth++;                            
            if (_renderData.BitmapEffectStackDepth == 0)
            {
                _renderData.BeginTopLevelBitmapEffect(_stackDepth);
            }                                                                                  
        }

        /// <summary>
        /// Pop
        /// </summary>
        public override void Pop(
            )
        {
            VerifyApiNonstructuralChange();

            if (_stackDepth <= 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.DrawingContext_TooManyPops));
            }

        #if DEBUG
            MediaTrace.DrawingContextOp.Trace("Pop(const)");
        #endif

            unsafe
            {
                EnsureRenderData();



                MILCMD_POP record =
                    new MILCMD_POP (

                        );

                // Assert that the calculated packet size is the same as the size returned by sizeof().
                // Note that since sizeof(emptyStruct) returns 1, we compare against 1 for empty structs.
                Debug.Assert(sizeof(MILCMD_POP) == 1);

                _renderData.WriteDataRecord(MILCMD.MilPop,
                                            (byte*)&record,
                                            0 /* sizeof(MILCMD_POP) */);
            }                           

            _stackDepth--;                            
            // end the top level effect, if we are popping the top
            // level push effect instruction
            if (_renderData.BitmapEffectStackDepth == (_stackDepth + 1))
            {
                _renderData.EndTopLevelBitmapEffect();
            }
}

        private UInt32 UseAnimations(
            Double baseValue,
            AnimationClock animations)
        {
            if (animations == null)
            {
                return 0;
            }
            else
            {
                return _renderData.AddDependentResource(
                    new DoubleAnimationClockResource(
                        baseValue,
                        animations));
            }
        }

        private UInt32 UseAnimations(
            Point baseValue,
            AnimationClock animations)
        {
            if (animations == null)
            {
                return 0;
            }
            else
            {
                return _renderData.AddDependentResource(
                    new PointAnimationClockResource(
                        baseValue,
                        animations));
            }
        }

        private UInt32 UseAnimations(
            Size baseValue,
            AnimationClock animations)
        {
            if (animations == null)
            {
                return 0;
            }
            else
            {
                return _renderData.AddDependentResource(
                    new SizeAnimationClockResource(
                        baseValue,
                        animations));
            }
        }

        private UInt32 UseAnimations(
            Rect baseValue,
            AnimationClock animations)
        {
            if (animations == null)
            {
                return 0;
            }
            else
            {
                return _renderData.AddDependentResource(
                    new RectAnimationClockResource(
                        baseValue,
                        animations));
            }
        }
    }
}
