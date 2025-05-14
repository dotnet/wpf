// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: 3D emissive material
//
//              See spec at *** FILL IN LATER ***
//

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     EmissiveMaterial allows a 2d brush to be used on a 3d model that has been lit
    ///     as if it were emitting light equal to the color of the brush
    /// </summary>
    public sealed partial class EmissiveMaterial : Material
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Constructs a EmissiveMaterial
        /// </summary>
        public EmissiveMaterial()
        {
        }

        /// <summary>
        ///     Constructor that sets the Brush property to "brush"
        /// </summary>
        /// <param name="brush">The new material's brush</param>
        public EmissiveMaterial(Brush brush)
        {
            Brush = brush;
        }

        #endregion Constructors
    }
}
