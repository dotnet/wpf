// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    [TargetTypeAttribute(typeof(EllipseGeometry))]
    internal class EllipseGeometryFactory : GeometryFactory<EllipseGeometry>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets EllipseGeometry center point.
        /// </summary>
        public Point Center { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double RadiusX { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double RadiusY { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new EllipseGeometry.</returns>
        public override EllipseGeometry Create(DeterministicRandom random)
        {
            EllipseGeometry geometry;

            if (UseDefaultConstructor)
            {
                geometry = new EllipseGeometry();
                geometry.Center = Center;
                geometry.RadiusX = RadiusX;
                geometry.RadiusY = RadiusY;
            }
            else
            {
                geometry = new EllipseGeometry(Center, RadiusX, RadiusX);
            }

            ApplyTransform(geometry);
            return geometry;
        }

        #endregion
    }
}
