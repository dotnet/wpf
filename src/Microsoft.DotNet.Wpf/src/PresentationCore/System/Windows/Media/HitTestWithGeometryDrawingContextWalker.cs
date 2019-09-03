// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: The implementation of HitTestWithGeometryDrawingContextWalker,
//              used to perform hit tests geometry on renderdata.
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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// HitTestDrawingContextWalker - a DrawingContextWalker which will perform a point or
    /// geometry based hit test on the contents of a render data.
    /// </summary>
    internal class HitTestWithGeometryDrawingContextWalker : HitTestDrawingContextWalker
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="geometry"> Geometry - the geometry to hit test, in local coordinates. </param>
        internal HitTestWithGeometryDrawingContextWalker(PathGeometry geometry)
        {
            // The caller should pre-cull if the geometry is null.
            Debug.Assert(geometry != null);

            _geometry = geometry;
            _currentTransform = null;
            _currentClip = null;
            _intersectionDetail = IntersectionDetail.NotCalculated;
        }


        /// <summary>
        /// IsHit Property - Returns true if geometry intersected the drawing instructions.
        /// </summary>
        internal override bool IsHit
        {
            get
            {
                return (_intersectionDetail != IntersectionDetail.Empty &&
                        _intersectionDetail != IntersectionDetail.NotCalculated);
            }
        }

        internal override IntersectionDetail IntersectionDetail
        {
            get
            {
                if (_intersectionDetail == IntersectionDetail.NotCalculated)
                {
                    return IntersectionDetail.Empty;
                }
                else
                {
                    return _intersectionDetail;
                }
            }
        }

        #region Private helper classes

        private class ModifierNode
        {
        }

        private class TransformModifierNode : ModifierNode
        {
            public TransformModifierNode(Transform transform) {_transform = transform;}
            public Transform _transform;
        }

        private class ClipModifierNode : ModifierNode
        {
            public ClipModifierNode(Geometry clip) {_clip = clip;}
            public Geometry _clip;
        }
        
        #endregion Private helper classes

        #region Static Drawing Context Methods

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
            if ((geometry == null) || geometry.IsEmpty()) 
            {
                return;
            }

            Geometry testedGeometry;

            // Transform if so prescribed
            if ((_currentTransform != null) && !_currentTransform.IsIdentity)
            {
                testedGeometry = geometry.GetTransformedCopy(_currentTransform);
            }
            else
            {
                testedGeometry = geometry;
            }

            // Clip, if so prescribed
            if (_currentClip != null)
            {
                testedGeometry = Geometry.Combine(
                    testedGeometry,
                    _currentClip,
                    GeometryCombineMode.Intersect,
                    null);  // transform
            }

            if (brush != null)
            {
                AccumulateIntersectionDetail(testedGeometry.FillContainsWithDetail(_geometry));
            }

            // If we have a pen and we haven't yet hit, try the widened geometry.
            if ((pen != null) && !_contains)
            {
                AccumulateIntersectionDetail(testedGeometry.StrokeContainsWithDetail(pen, _geometry));
            }

            // If we've hit, stop walking.
            if (_contains)
            {
                StopWalking();
            }
        }


        /// <summary>
        /// Draw a GlyphRun.
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush to draw GlyphRun with. </param>
        /// <param name="glyphRun"> The GlyphRun to draw. </param>
        /// <exception cref="ObjectDisposedException">
        /// This call is illegal if this object has already been closed or disposed.
        /// </exception>
        public override void DrawGlyphRun(Brush foregroundBrush, GlyphRun glyphRun)
        {
            if (glyphRun != null)
            {
                // The InkBoundingBox + the Origin produce the true InkBoundingBox.
                Rect rectangle = glyphRun.ComputeInkBoundingBox();

                if (!rectangle.IsEmpty)
                {
                    rectangle.Offset((Vector)glyphRun.BaselineOrigin);
                    DrawGeometry(Brushes.Black, null /* pen */, new RectangleGeometry(rectangle));
                }
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
            // Intersect the new clip with the current one

            // 1st case:- No change, keep the old clip
            // 2nd case:- No matter what, the intersection will also be empty
            if ((clipGeometry == null)  
                ||
                ((_currentClip!=null) && (_currentClip.IsEmpty()))) 
            {
                clipGeometry = _currentClip;
            }
            else
            {
                // Transform the clip new if so prescribed
                if ((_currentTransform != null) && !_currentTransform.IsIdentity)
                {
                    clipGeometry = clipGeometry.GetTransformedCopy(_currentTransform);
                }

                // Intersect it with the current clip
                if (_currentClip != null)
                {
                    clipGeometry = Geometry.Combine(
                        _currentClip,
                        clipGeometry,
                        GeometryCombineMode.Intersect,
                        null);  // Transform
                }
            }

            // Push the previous clip on the stack
            PushModifierStack(new ClipModifierNode(_currentClip));

            _currentClip = clipGeometry;
        }

        /// <summary>
        ///     PushOpacityMask -
        ///     Push an opacity mask
        /// </summary>
        /// <param name="brush">
        ///     The opacity mask brush
        /// </param>
        public override void PushOpacityMask(Brush brush)
        {
            // Opacity mask does not affect hit testing, but requires a place-holder on the stack
            PushModifierStack(null);
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
            // Opacity does not affect hit testing, but requires a place-holder on the stack
            PushModifierStack(null);
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
            // Combine the new transform with the current one
            if ((transform == null) || transform.IsIdentity)
            {
                // The new transform does not change the existing one
                transform = _currentTransform;
            }
            else if ((_currentTransform != null) && !_currentTransform.IsIdentity)
            {
                // Both the current transform and the new one are nontrivial, combine them
                Matrix combined =  transform.Value * _currentTransform.Value;
                transform = new MatrixTransform(combined);
            }

            // Push the previous transform on the stack
            PushModifierStack(new TransformModifierNode(_currentTransform));

            _currentTransform = transform;
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
            // GuidelineSet does not affect hit testing, but requires a place-holder on the stack
            PushModifierStack(null);
        }


        /// <summary>
        ///     PushGuidelineY1 - 
        ///     Explicitly push one horizontal guideline.
        /// </summary>
        /// <param name="coordinate"> The coordinate of leading guideline. </param>
        internal override void PushGuidelineY1(
            Double coordinate)
        {
            // GuidelineSet does not affect hit testing, but requires a place-holder on the stack
            PushModifierStack(null);
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
            // GuidelineSet does not affect hit testing, but requires a place-holder on the stack
            PushModifierStack(null);
        }


        /// <summary>
        /// Pop
        /// </summary>
        public override void Pop()
        {
            // We must have a modifier stack and it must not be empty.
            Debug.Assert(_modifierStack != null);
            Debug.Assert(_modifierStack.Count > 0);

            object currentModifier = _modifierStack.Pop();

            if (currentModifier is TransformModifierNode)
            {
                _currentTransform = ((TransformModifierNode)currentModifier)._transform;

                // Since the drawing context starts out with no transform and no clip,
                // the first element pushed on the stack will always be null.
                Debug.Assert((_modifierStack.Count > 0) || (_currentTransform == null));
            }
            else if (currentModifier is ClipModifierNode)
            {
                _currentClip = ((ClipModifierNode)currentModifier)._clip;

                // Since the drawing context starts out with no transform and no clip,
                // the first element pushed on the stack will always be null.
                Debug.Assert((_modifierStack.Count > 0) || (_currentClip == null));
            }
            else
            {
                Debug.Assert(currentModifier == null);
            }
        }

        #endregion Static Drawing Context Methods

        #region Private Methods


        /// <summary>
        /// AccumulateIntersectionDetail - accepts a new IntersectionDetail which is the result
        /// of "drawingCommandGeometry.FillContainsWithDetail(hitTestGeometry)" and updates
        /// the current _intersectionDetail, setting _contains as appropriate.
        /// </summary>
        /// <param name="intersectionDetail">
        ///   The IntersectionDetail from hit-testing the current node.
        /// </param>
        private void AccumulateIntersectionDetail(IntersectionDetail intersectionDetail)
        {
            // Note that: 
            // * "FullyContains" means that the target node contains the hit test-geometry,
            // * "FullyInside" means that the target node is fully inside the hit-test geometry

            // The old result cannot be FullyContain, because that would have
            // triggered a StopWalk and we wouldn't be here

            Debug.Assert(_intersectionDetail != IntersectionDetail.FullyContains);
            
            // The new result cannot be NotCalculated, because we just
            // calculated!

            Debug.Assert(intersectionDetail != IntersectionDetail.NotCalculated);

            // The current _intersectionDetail is computed from its old value and the
            // new result according the the following table:

            //     \ old   +
            //  New \      + NotCalc     | Empty       | Intersects  | FullyInside      There
            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++      is
            // Empty       + Empty       | Empty       | Intersects  | Intersects        no
            // ------------+-------------------------------------------------------    Contains
            // Intersects  + Intersects  | Intersects  | Intersects  | Intersects      column
            // ------------+-------------------------------------------------------     (see   
            // Contains    + Contains    | Contains    | Contains    | Contains        assertion
            // ------------+-------------------------------------------------------     above)
            // FullyInside + FullInside  | Intersects  | Intersects  | FullyInside

            if (_intersectionDetail == IntersectionDetail.NotCalculated)
                // This is the first node
            {
                _intersectionDetail = intersectionDetail;
                // Takes care of the first column.
            }
            else if (intersectionDetail == IntersectionDetail.FullyInside
                // This node is fully inside the hit geometry --
                &&
                _intersectionDetail != IntersectionDetail.FullyInside)
                //  -- but we have already encountered a previous node that was not fully inside
            {
                _intersectionDetail = IntersectionDetail.Intersects;

                // Taking care of the second-to-left bottom cell
            }
            else if (intersectionDetail == IntersectionDetail.Empty
                // This node does not touch the hit geometry --
                &&
                _intersectionDetail != IntersectionDetail.Empty)
                //  -- but we have already encountered a previous node that was touched
            {
                _intersectionDetail = IntersectionDetail.Intersects;

                // Taking care of the third and fourth cells in the first row
            }
            else
            {
                // Accept the new result as is
                _intersectionDetail = intersectionDetail;

                // Taking care of the second and third row and the diagonal
            }

            if (_intersectionDetail == IntersectionDetail.FullyContains)
            {
                // The hit geometry is fully contained in the visual, so signal a StopWalk
                _contains = true;
            }
}

        private void PushModifierStack(ModifierNode modifier)
        {
            // Push the old modifier on the stack
            if (_modifierStack == null)
            {
                _modifierStack = new Stack();
            }

            _modifierStack.Push(modifier);
        }

        #endregion Private Methods

        #region Private Fields

        // The geometry with which we are hit-testing
        private PathGeometry _geometry;

        // The stack of previous values of transfrom/clip
        private Stack _modifierStack;

        // The current transform
        private Transform _currentTransform;

        // The current clip
        private Geometry _currentClip;

        // This keeps track of the details of a geometry hit test.
        private IntersectionDetail _intersectionDetail;

        #endregion Private Fields
    }
}

