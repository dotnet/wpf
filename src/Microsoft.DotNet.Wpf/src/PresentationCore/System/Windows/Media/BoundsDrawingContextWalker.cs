// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of BoundsDrawingContextWalker.
//              This DrawingContextWalker is used to perform bounds calculations
//              on renderdata.
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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Security;

namespace System.Windows.Media
{
    /// <summary>
    /// BoundsDrawingContextWalker - a DrawingContextWalker which will calculate the bounds
    /// of the contents of a render data.
    /// </summary>
    internal class BoundsDrawingContextWalker : DrawingContextWalker
    {
        /// <summary>
        /// PushType enum - this defines the type of Pushes in a context, so that our
        /// untyped Pops know what to Pop.
        /// </summary>
        private enum PushType
        {
            Transform,
            Clip,
            Opacity,
            OpacityMask,
            Guidelines,
            BitmapEffect
        }

        /// <summary>
        /// Constructor for BoundsDrawingContextWalker
        /// </summary>
        public BoundsDrawingContextWalker()
        {
            _bounds = Rect.Empty;
            _transform = Matrix.Identity;
        }

        public Rect Bounds
        {
            get
            {
                return _bounds;
            }
        }

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
            if (Pen.ContributesToBounds(pen))
            {
                // _bounds is always in "world" space
                // So, we need to transform the geometry to world to bound it
                Rect geometryBounds = LineGeometry.GetBoundsHelper(
                    pen,
                    _transform, // world transform
                    point0,
                    point1,
                    Matrix.Identity, // geometry transform
                    Geometry.StandardFlatteningTolerance,
                    ToleranceType.Absolute
                    );

                AddTransformedBounds(ref geometryBounds);
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
            if ((brush != null) || Pen.ContributesToBounds(pen))
            {
                // _bounds is always in "world" space
                // So, we need to transform the geometry to world to bound it
                Rect geometryBounds = RectangleGeometry.GetBoundsHelper(
                    pen,
                    _transform, // world transform
                    rectangle,
                    0.0,
                    0.0,
                    Matrix.Identity, // geometry transform
                    Geometry.StandardFlatteningTolerance,
                    ToleranceType.Absolute
                    );

                AddTransformedBounds(ref geometryBounds);
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
            if ((brush != null) || Pen.ContributesToBounds(pen))
            {
                // _bounds is always in "world" space
                // So, we need to transform the geometry to world to bound it
                Rect geometryBounds = RectangleGeometry.GetBoundsHelper(
                    pen,
                    _transform, // world transform
                    rectangle,
                    radiusX,
                    radiusY,
                    Matrix.Identity, // geometry transform
                    Geometry.StandardFlatteningTolerance,
                    ToleranceType.Absolute
                    );

                AddTransformedBounds(ref geometryBounds);
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
            if ((brush != null) || Pen.ContributesToBounds(pen))
            {
                // _bounds is always in "world" space
                // So, we need to transform the geometry to world to bound it
                Rect geometryBounds = EllipseGeometry.GetBoundsHelper(
                    pen,
                    _transform, // world transform
                    center,
                    radiusX,
                    radiusY,
                    Matrix.Identity, // geometry transform
                    Geometry.StandardFlatteningTolerance,
                    ToleranceType.Absolute
                    );

                AddTransformedBounds(ref geometryBounds);
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
            if ((geometry != null) && ((brush != null) || Pen.ContributesToBounds(pen)))
            {
                // _bounds is always in "world" space
                // So, we need to transform the geometry to world to bound it
                Rect geometryBounds = geometry.GetBoundsInternal(pen, _transform);

                AddTransformedBounds(ref geometryBounds);
            }
        }

        /// <summary>
        ///     DrawImage -
        ///     Draw an Image into the region specified by the Rect.
        ///     The Image will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with an ImageBrush via
        ///     DrawRectangle.
        /// </summary>
        /// <param name="imageSource"> The BitmapSource to draw. </param>
        /// <param name="rectangle">
        ///     The Rect into which the BitmapSource will be fit.
        /// </param>
        public override void DrawImage(
            ImageSource imageSource,
            Rect rectangle)
        {
            if (imageSource != null)
            {
                AddBounds(ref rectangle);
            }
        }

        /// <summary>
        ///     DrawVideo -
        ///     Draw a Video into the region specified by the Rect.
        ///     The Video will potentially be stretched and distorted to fit the Rect.
        ///     For more fine grained control, consider filling a Rect with an VideoBrush via
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
            if (video != null)
            {
                AddBounds(ref rectangle);
            }
        }

        /// <summary>
        /// Draw a GlyphRun.
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush to draw GlyphRun with. </param>
        /// <param name="glyphRun"> The GlyphRun to draw. </param>
        public override void DrawGlyphRun(Brush foregroundBrush, GlyphRun glyphRun)
        {
            if ((foregroundBrush != null) && (glyphRun != null))
            {
                // The InkBoundingBox + the Origin produce the true InkBoundingBox.
                Rect rectangle = glyphRun.ComputeInkBoundingBox();

                if (!rectangle.IsEmpty)
                {
                    rectangle.Offset((Vector)glyphRun.BaselineOrigin);
                    AddBounds(ref rectangle);
                }
            }
        }

        /// <summary>
        ///     PushOpacityMask -
        ///     Push an opacity mask, which will apply to all drawing primitives until the
        ///     corresponding Pop call.
        /// </summary>
        /// <param name="brush"> Brush for opacity mask. </param>
        public override void PushOpacityMask(
            Brush brush)
        {
            // Push the opacity type
            PushTypeStack(PushType.OpacityMask);
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
            // If we have an old clip, push the old clip onto the stack.
            if (_haveClip)
            {
                // Ensure the clip stack
                if (_clipStack == null)
                {
                    _clipStack = new Stack<Rect>(2);
                }

                _clipStack.Push(_clip);
            }

            // Push the clip type
            PushTypeStack(PushType.Clip);

            if (clipGeometry != null)
            {
                // Since _clip is a value type, we need to know whether we have a clip or not.
                // If not, we can assert that the initial value is present (Rect.Empty).
                // We should also now set the _haveClip flag.
                if (!_haveClip)
                {
                    _haveClip = true;
                    _clip = clipGeometry.GetBoundsInternal(null /* pen */, _transform);
                }
                else
                {
                    // update current clip
                    _clip.Intersect(clipGeometry.GetBoundsInternal(null /* pen */, _transform));
                }
            }
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
            // Push the opacity type
            PushTypeStack(PushType.Opacity);
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
            // Ensure the transform stack
            if (_transformStack == null)
            {
                _transformStack = new Stack<Matrix>(2);
            }

            // Push the old transform.
            _transformStack.Push(_transform);

            // Push the transform type
            PushTypeStack(PushType.Transform);

            Matrix newValue = Matrix.Identity;

            // Retrieve the new transform as a matrix if it exists
            if ((transform != null) && !transform.IsIdentity)
            {
                // If the transform is degeneraate, we can skip all instructions until the
                // corresponding Pop.
                newValue = transform.Value;
            }

            // Update the current transform
            _transform = newValue * _transform;
        }

        /// <summary>
        ///     PushGuidelineSet - 
        ///     Push a set of guidelines which should be applied
        ///     to all drawing operations until the 
        ///     corresponding Pop.
        /// </summary>
        /// <param name="guidelines"> The GuidelineSet to push. </param>
        public override void PushGuidelineSet(
            GuidelineSet guidelines)
        {
            // Push the guidelines type
            PushTypeStack(PushType.Guidelines);

            // Nothing else to do. Guidelines are not used,
            // so we only need to register Push() type in order to treat
            // Pop() properly.
        }

        /// <summary>
        ///     PushGuidelineY1 - 
        ///     Explicitly push one horizontal guideline.
        /// </summary>
        /// <param name="coordinate"> The coordinate of leading guideline. </param>
        internal override void PushGuidelineY1(
            Double coordinate)
        {
            // Push the guidelines type
            PushTypeStack(PushType.Guidelines);

            // Nothing else to do. Guidelines are not used,
            // so we only need to register Push() type in order to treat
            // Pop() properly.
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
            // Push the guidelines type
            PushTypeStack(PushType.Guidelines);

            // Nothing else to do. Guidelines are not used,
            // so we only need to register Push() type in order to treat
            // Pop() properly.
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
            // Ensure the type stack
            PushTypeStack(PushType.BitmapEffect);

            // This API has been deprecated, so any BitmapEffect is ignored.
        }


        /// <summary>
        /// Pop
        /// </summary>
        public override void Pop()
        {
            // We must have a type stack and it must not be empty.
            Debug.Assert(_pushTypeStack != null);
            Debug.Assert(_pushTypeStack.Count > 0);

            // Retrieve the PushType to figure out what what this Pop is.
            PushType pushType = _pushTypeStack.Pop();

            switch (pushType)
            {
                case PushType.Transform:
                    // We must have a Transform stack and it must not be empty.
                    Debug.Assert(_transformStack != null);
                    Debug.Assert(_transformStack.Count > 0);

                    // Restore the transform
                    _transform = _transformStack.Pop();

                    break;

                case PushType.Clip:

                    // Restore the clip, if there's one to restore
                    if ((_clipStack != null) &&
                        (_clipStack.Count > 0))
                    {
                        _clip = _clipStack.Pop();
                    }
                    else
                    {
                        // If the _clipStack was empty or null, then we no longer have a clip.
                        _haveClip = false;
                    }

                    break;
                case PushType.BitmapEffect:
                    // This API has been disabled, so any BitmapEffect is ignored.

                    break;
                default:
                    // Ignore the rest
                    break;
            }
        }

        #endregion Static Drawing Context Methods

        #region Private Methods

        /// <summary>
        /// AddBounds - Unions the non-transformed bounds which are
        /// local to the current Drawing operation with the
        /// aggregate bounds of other Drawing operations encountered
        /// during this walk.
        /// </summary>
        /// <param name="bounds"> 
        ///     In:  The bounds of the geometry to union in the coordinate
        ///          space of the current Drawing operations
        ///     Out: The transformed and clipped bounds of the geometry
        ///          in the coordinate space of the top-level Drawing
        ///          operation.
        /// </param>
        private void AddBounds(ref Rect bounds)
        {
            // _bounds is always in "world" space
            // So, we need to transform the Rect to world to bound it
            if (!_transform.IsIdentity)
            {
                MatrixUtil.TransformRect(ref bounds, ref _transform);
            }

            AddTransformedBounds(ref bounds);
        }

        /// <summary>
        /// AddTransformedBounds - Unions bounds which have been transformed
        /// into the top-level Drawing operation with the aggregate bounds of 
        /// other Drawing operations encountered during this walk.
        /// </summary>
        /// <param name="bounds"> 
        ///     In:  The bounds of the geometry to union in the coordinate
        ///          space of the current Drawing operations
        ///     Out: The transformed and clipped bounds of the geometry
        ///          in the coordinate space of the top-level Drawing
        ///          operation.
        /// </param>        
        private void AddTransformedBounds(ref Rect bounds)
        {
            if (DoubleUtil.RectHasNaN(bounds))
            {
                // We set the bounds to infinity if it has NaN
                bounds.X = Double.NegativeInfinity;
                bounds.Y = Double.NegativeInfinity;
                bounds.Width = Double.PositiveInfinity;
                bounds.Height = Double.PositiveInfinity;
            }

            if (_haveClip)
            {
                bounds.Intersect(_clip);
            }

            _bounds.Union(bounds);
        }

        /// <summary>
        /// Ensure the type stack exists, and store given push type there.
        /// </summary>
        /// <param name="pushType">the push type to store</param>
        private void PushTypeStack(PushType pushType)
        {
            if (_pushTypeStack == null)
            {
                _pushTypeStack = new Stack<PushType>(2);
            }

            _pushTypeStack.Push(pushType);
        }

        /// <summary>
        /// Ensure that the state is clear and is good for next use.
        /// </summary>   
        internal void ClearState()
        {
            _clip = Rect.Empty;
            _bounds = Rect.Empty;
            _haveClip = false;
            _transform = new Matrix();
            _pushTypeStack = null;
            _transformStack = null;
            _clipStack = null;            
        }

        #endregion Private Methods

        // The accumulated bounds, in world space
        private Rect _bounds;

        // The current clip in world space, if _haveClip is true.  Otherwise, this
        // variable may hold random rects (stuff left over from previously pop'ed clipped)
        private Rect _clip;

        // States whether or not we have a clip (because Rect isn't nullable).
        private bool _haveClip;

        // The current local->world Transform as a matrix.
        private Matrix _transform;

        // The Type stack for our Push/Pop calls.  This tells whether a given Pop corresponds
        // to a Transform, Clip, etc.
        private Stack<PushType> _pushTypeStack;

        // This stack contains the Matricies encountered during our walk.
        // The current transform is stored in _transform and not in the Stack.
        private Stack<Matrix> _transformStack;

        // This stack contains the clip rects encountered during our walk.
        // The current clip is stored in _clip and not in the Stack.
        private Stack<Rect> _clipStack;
    }
}

