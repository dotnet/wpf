// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D model collection.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using MS.Internal;
using MS.Internal.Media3D;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    /// 3D model group.
    /// </summary>
    [ContentProperty("Children")]
    public sealed partial class Model3DGroup : Model3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public Model3DGroup() {}

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal override void RayHitTestCore(
            RayHitTestParameters rayParams)
        {    
            Model3DCollection children = Children;

            if (children == null)
            {
                return;
            }
            
            for (int i = children.Count - 1; i >= 0; i--)
            {
                Model3D child = children.Internal_GetItem(i);

                // Perform the hit-test against the child.
                child.RayHitTest(rayParams);
            }
        }
        
        internal override Rect3D CalculateSubgraphBoundsInnerSpace()
        {
            Model3DCollection children = Children;

            if (children == null)
            {
                return Rect3D.Empty;
            }
            
            Rect3D bounds = Rect3D.Empty;

            for (int i = 0, count = children.Count; i < count; i++)
            {
                Model3D child = children.Internal_GetItem(i);
                
                // Calls CSBOS rather than Bounds to avoid ReadPreamble.
                bounds.Union(child.CalculateSubgraphBoundsOuterSpace());
            }

            return bounds;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // named EmptyGroup not to collide with public Model3D.Empty
        internal static Model3DGroup EmptyGroup
        {
            get
            {
                if (s_empty == null)
                {
                    s_empty = new Model3DGroup();
                    s_empty.Freeze();
                }
                return s_empty;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private static Model3DGroup s_empty;

        #endregion Private Fields
    }
}
