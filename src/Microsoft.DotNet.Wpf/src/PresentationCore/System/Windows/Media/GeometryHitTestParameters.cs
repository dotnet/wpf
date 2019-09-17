// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Security;

using System.Collections;
using System.Diagnostics;
using MS.Internal;

namespace System.Windows.Media 
{
    /// <summary>
    /// This is the class for specifying parameters hit testing with a geometry.
    /// </summary>
    public class GeometryHitTestParameters : HitTestParameters
    {
        private PathGeometry _hitGeometryInternal;  // The internal geometry we use for hit testing
        private Geometry _hitGeometryCache = null;  // Cached frozen copy we hand out from HitGeometry property.
        private Rect _origBounds;
        private Rect _bounds;
        private MatrixStack _matrixStack;

        /// <summary>
        /// The constructor takes the geometry to hit test with.
        /// </summary>
        public GeometryHitTestParameters(Geometry geometry) : base()
        {
            // This ctor guarantees that we are initialized in the following way:
            //
            //  1.  The geometry provided by the user is unmodified
            //  2.  _hitGeometryInternal is a PathGeometry equivilent to the supplied geometry
            //  3.  _hitGeometryInternal.Tranform is a MatrixTransform equivilent to the supplied
            //      geometry.Transform
            //  4.  _origBounds is the untransformed bounds of the _hitGeometryInternal (inner space).
            //  5.  _bounds is the transformed bounds of the _hitGeometryInternal (outer space).
            //  6.  _matrixStack is an empty stack.            

            if (geometry == null)
            {                
                throw new ArgumentNullException("geometry");
            }
            
            // Convert the Geometry to an equivilent PathGeometry up front to prevent
            // conversion on every call to DoesContainWithDetail.  If the geometry is
            // animate this also has the side effect of eliminating animation interplay.
            _hitGeometryInternal = geometry.GetAsPathGeometry();

            // If GetAsPathGeometry was a no-op, force the copy because we do not
            // went to modify the user's Geometry.
            if (object.ReferenceEquals(_hitGeometryInternal, geometry))
            {
                _hitGeometryInternal = _hitGeometryInternal.Clone();
            }
            
            // Convert _hitGeometryInternal.Transform to an equivilent MatrixTransform
            // so that we can aggregate in PushMatrix/PopMatrix without building a
            // TransformCollection.
            Transform origTransform = _hitGeometryInternal.Transform;            
            MatrixTransform newTransform = new MatrixTransform();

            _hitGeometryInternal.Transform = newTransform;

            // Before we initialize MatrixTransform.Matrix, cache the bounds of this
            // geometry without any transforms.
            _origBounds = _hitGeometryInternal.Bounds;

            // If we had a non-Identity transform, update our MatrixTransform.Matrix
            // with its Value.  (Note that when GetAsPathGeometry *isn't* a no-op
            // it applies the transform to the figures and returns with a Geometry
            // with an identity Transform.)
            if (origTransform != null && !origTransform.IsIdentity)
            {
                newTransform.Matrix = origTransform.Value;
            }
            
            // Initialize the current transformed bounds of this Geometry
            _bounds = _hitGeometryInternal.Bounds;
            _matrixStack = new MatrixStack();
        }
    
        /// <summary>
        /// The geometry to hit test against.  This will be a frozen copy of the
        /// hit geometry transformed into the local space of the Visual being hit
        /// tested.
        /// </summary>
        public Geometry HitGeometry
        {
            get
            {
                if (_hitGeometryCache == null)
                {
                    _hitGeometryCache = (Geometry) _hitGeometryInternal.GetAsFrozen();
                }

                Debug.Assert(_hitGeometryInternal.Transform.Value == _hitGeometryCache.Transform.Value,
                    "HitGeometry has a different transform than HitGeometryInternal. Did we forget to invalidate the cache?");
                
                return _hitGeometryCache;
            }
        }

        // Use this property when retrieving the hit geometry internally.  Right now it avoids a
        // a cast.  In the future it will avoid a copy as well. 
        // There is a bug that allows the HitGeometry returned from GeometryHitTestParamaters
        // to be unintentially mutable, allowing users to modify the hit geometry or attach
        // changed handlers after the GeometryHitTestParamaters is constructed.
        // The value from this property should never be exposed to the user (e.g., do not use it to return
        // hit test results.)

        internal PathGeometry InternalHitGeometry
        {
            get
            {
                return _hitGeometryInternal;
            }
        }

        internal Rect Bounds
        {
            get 
            {
                return _bounds;
            }
        }

        internal void PushMatrix(ref Matrix newMatrix)
        {
            MatrixTransform matrixTransform = (MatrixTransform) _hitGeometryInternal.Transform;

            // Grab the old composedMatrix from our MatrixTransform and save
            // it on our MatrixStack.
            Matrix composedMatrix = matrixTransform.Value;
            _matrixStack.Push(ref composedMatrix, false);
            
            // Compose the new matrix into our MatrixTransform
            MatrixUtil.MultiplyMatrix(ref composedMatrix, ref newMatrix);
            
            matrixTransform.Matrix = composedMatrix;
            
            // Update active bounds based on new tranform.
            _bounds = Rect.Transform(_origBounds, composedMatrix);

            ClearHitGeometryCache();
        }

        internal void PopMatrix()
        {
            Matrix matrix = _matrixStack.Peek();

            // Restore saved transform.
            ((MatrixTransform) (_hitGeometryInternal.Transform)).Matrix = matrix;
            
            // Update active bounds based on new X-form.
            _bounds = Rect.Transform(_origBounds, matrix);
            
            _matrixStack.Pop();

            ClearHitGeometryCache();
        }

        // This method is called to restore the _hitGeometryInternal's transform to its original state
        // in the event that a user's delegate threw an exception during the hit test walk.
        internal void EmergencyRestoreOriginalTransform()
        {
            // Replace the current transform with the first matrix pushed onto the stack.
            Matrix matrix = ((MatrixTransform) (_hitGeometryInternal.Transform)).Matrix;
            
            while (!_matrixStack.IsEmpty)
            {
                matrix = _matrixStack.Peek();
                _matrixStack.Pop();
            }

            ((MatrixTransform) (_hitGeometryInternal.Transform)).Matrix = matrix;

            ClearHitGeometryCache();
        }

        // This clears the 
        private void ClearHitGeometryCache()
        {
            _hitGeometryCache = null;
        }
    }
}

