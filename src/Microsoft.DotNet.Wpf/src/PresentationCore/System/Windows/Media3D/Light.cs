// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D light implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//
//

using System;
using System.Diagnostics;
using System.Windows.Media;
using MS.Internal.Media3D;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Lights are Model3D's.  These include Ambient, Positional, Directional and Spot lights.
    ///     They're very much modeled on the Direct3D lighting set, but have the additional 
    ///     property of being part of a modeling hierarchy, and are thus subject to coordinate 
    ///     space transformations.
    /// </summary>
    public abstract partial class Light : Model3D
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Prevent 3rd parties from extending this abstract base class.
        internal Light() {}

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

        internal override void RayHitTestCore(RayHitTestParameters rayParams)
        {
            // Lights are considered to be part of the model graph, but they
            // have no geometry and therefore can not be hit tested.            
        }
        
        internal override Rect3D CalculateSubgraphBoundsInnerSpace()
        { 
            // Lights are considered to be part of the model graph, but they
            // have no geometry and therefore no bounds.
            
            return Rect3D.Empty;
        }

        #endregion Public Properties
    }
}
