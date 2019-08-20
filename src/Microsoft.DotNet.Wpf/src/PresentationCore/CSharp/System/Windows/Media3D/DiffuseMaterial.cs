// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D diffuse material
//


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using MS.Internal;

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
