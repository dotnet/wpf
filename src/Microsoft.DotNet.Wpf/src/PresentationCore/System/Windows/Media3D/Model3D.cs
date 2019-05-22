// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D model implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MS.Internal.Media3D;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Model3D is the abstract model that everything builds from.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string
    public abstract partial class Model3D : Animatable
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Model3D() {}

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Gets bounds for this model.
        /// </summary>
        public Rect3D Bounds
        { 
            get
            { 
                ReadPreamble();
                
                return CalculateSubgraphBoundsOuterSpace();
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        
        #region Internal Methods

        // NOTE: Model3D hit testing takes the rayParams in the outer space of the
        //       Model3D.  That is, RayHitTest() will apply this model's transform 
        //       to the ray for the caller.
        //
        //       This is different than Visual hit testing which does not transform
        //       the hit testing parameters by the Visual's transform.
        internal void RayHitTest(RayHitTestParameters rayParams)
        {
            Transform3D transform = Transform;

            rayParams.PushModelTransform(transform);            
            RayHitTestCore(rayParams);
            rayParams.PopTransform(transform);
        }

        internal abstract void RayHitTestCore(RayHitTestParameters rayParams);

        /// <summary>
        ///     Returns the bounds of the Model3D subgraph rooted at this Model3D.
        ///
        ///     Outer space refers to the space after this Model's Transform is
        ///     applied -- or said another way, applying a transform to this Model
        ///     affects it's outer bounds.  (While its inner bounds remain unchanged.)
        /// </summary>
        internal Rect3D CalculateSubgraphBoundsOuterSpace()
        {
            Rect3D innerBounds = CalculateSubgraphBoundsInnerSpace();
            
            return M3DUtil.ComputeTransformedAxisAlignedBoundingBox(ref innerBounds, Transform);
        }

        /// <summary>
        ///     Returns the bounds of the Model3D subgraph rooted at this Model3D.
        ///
        ///     Inner space refers to the space before this Model's Transform is
        ///     applied -- or said another way, applying a transform to this Model
        ///     only affects it's outer bounds.  Its inner bounds remain unchanged.
        /// </summary>
        internal abstract Rect3D CalculateSubgraphBoundsInnerSpace();

        #endregion Internal Methods
    }
}
