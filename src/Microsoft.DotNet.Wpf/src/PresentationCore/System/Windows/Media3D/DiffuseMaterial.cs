// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
// Description: 3D diffuse material
//

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     DiffuseMaterial allows a 2d brush to be used on a 3d model that has been lit
    ///     with a diffuse lighting model
    /// </summary>
    public sealed partial class DiffuseMaterial : Material
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Constructs a DiffuseMaterial
        /// </summary>
        public DiffuseMaterial()
        {
        }

        /// <summary>
        ///     Constructor that sets the Brush property to "brush"
        /// </summary>
        /// <param name="brush">The new material's brush</param>
        public DiffuseMaterial(Brush brush)
        {
            Brush = brush;
        }

        #endregion Constructors
    }
}
