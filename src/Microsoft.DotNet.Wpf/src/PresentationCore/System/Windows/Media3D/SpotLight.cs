// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: 3D spot light implementation.
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
    ///     The SpotLight derives from PointLightBase as it has a position, range, and attenuation,
    ///     but also adds in a direction and parameters to control the "cone" of the light.
    ///     In order to control the "cone", outerConeAngle (beyond which nothing is illuminated),
    ///     and innerConeAngle (within which everything is fully illuminated) must be specified.
    ///     Lighting between the outside of the inner cone and the outer cone falls off linearly.
    /// </summary>
    public sealed partial class SpotLight : PointLightBase
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="diffuseColor">Diffuse color of the new light.</param>
        /// <param name="position">Position of the new light.</param>
        /// <param name="direction">Direction of the new light.</param>
        /// <param name="outerConeAngle">Outer cone angle of the new light.</param>
        /// <param name="innerConeAngle">Inner cone angle of the new light.</param>
        public SpotLight(Color diffuseColor, Point3D position, Vector3D direction,
                         double outerConeAngle, double innerConeAngle) : this()
        {
            // Set PointLightBase properties
            Color = diffuseColor;
            Position = position;

            // Set SpotLight properties
            Direction = direction;
            OuterConeAngle = outerConeAngle;
            InnerConeAngle = innerConeAngle;
        }

        /// <summary>
        ///     Builds a default spotlight shining onto the origin from the (0,0,-1)
        /// </summary>
        public SpotLight() {}

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

