// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D positional light implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Composition;
using MS.Internal;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Positional lights have a position in space and project their light in all directions.
    ///     The falloff of the light is controlled by attenuation and range properties.
    /// </summary>
    public sealed partial class PointLight : PointLightBase
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Default constructor that creates a white PointLight at the origin.
        /// </summary>
        public PointLight() {}

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="diffuseColor">Diffuse color for the new positional light.</param>
        /// <param name="position">Position of the new positional light.</param>
        public PointLight(Color diffuseColor, Point3D position)
            : this()
        {
            // Set PointLightBase properties
            Color = diffuseColor;
            Position = position;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
    }
}
