// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D directional light implementation.
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht
//
//

using System;
using System.Windows.Media;
using System.Windows.Media.Composition;
using MS.Internal;
using System.ComponentModel.Design.Serialization;
using System.Windows.Markup;

namespace System.Windows.Media.Media3D
{
    /// <summary>
    ///     Directional lights have no position in space and project their light along a
    ///     particular direction, specified by the vector that defines it.
    /// </summary>
    public sealed partial class DirectionalLight : Light
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Constructs a white light looking down the positive z axis.
        /// </summary>
        public DirectionalLight()
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="diffuseColor">Diffuse color of the new light.</param>
        /// <param name="direction">Direction of the new light.</param>
        public DirectionalLight(Color diffuseColor, Vector3D direction)
        {
            Color = diffuseColor;
            Direction = direction;
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
