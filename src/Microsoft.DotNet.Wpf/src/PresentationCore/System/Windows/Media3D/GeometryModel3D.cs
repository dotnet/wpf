// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D geometry primitive implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using MS.Internal;
using MS.Internal.Media3D;
using System;
using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Media.Media3D;
using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     GeometryModel3D is for modeling with a Geometry3D and a Material.
    /// </summary>
    public sealed partial class GeometryModel3D : Model3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        ///     Builds a GeometryModel3D with empty Geometry3D and Material.
        /// </summary>
        public GeometryModel3D() {}
        
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="geometry">Geometry of the new mesh primitive.</param>
        /// <param name="material">Material of the new mesh primitive.</param>
        public GeometryModel3D(Geometry3D geometry, Material material)
        {
            Geometry = geometry;
            Material = material;
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods
        
        internal override Rect3D CalculateSubgraphBoundsInnerSpace()
        {
            Geometry3D geometry = Geometry;
            
            if (geometry == null)
            {
                return Rect3D.Empty;
            }

            return geometry.Bounds;
        }

        internal override void RayHitTestCore(RayHitTestParameters rayParams)
        {
            Geometry3D geometry = Geometry;
            
            if (geometry != null)
            {
                // If our Geometry3D hit test intersects anything we should return "this" Model3D
                // as the HitTestResult.ModelHit.
                rayParams.CurrentModel = this;

                FaceType facesToHit = FaceType.None;

                if (Material != null)
                {
                    facesToHit |= FaceType.Front;
                }
                
                if (BackMaterial != null)
                {
                    facesToHit |= FaceType.Back;
                }

                if (facesToHit != FaceType.None)
                {
                    geometry.RayHitTest(rayParams, facesToHit);
                }
            }
        }

        internal void MaterialPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
}

        internal void BackMaterialPropertyChangedHook(DependencyPropertyChangedEventArgs e)
        {
            MaterialPropertyChangedHook(e);
        }

        #endregion Internal Methods
    }
}
