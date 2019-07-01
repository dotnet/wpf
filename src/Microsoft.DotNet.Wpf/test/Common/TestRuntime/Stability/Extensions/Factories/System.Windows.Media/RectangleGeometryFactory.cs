// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class RectangleGeometryFactory : GeometryFactory<RectangleGeometry>
    {
        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new RectangleGeometry.</returns>
        public override RectangleGeometry Create(DeterministicRandom random)
        {
            RectangleGeometry geometry;

            double rectX = random.NextDouble() * 50.0;
            double rectY = random.NextDouble() * 50.0;
            double width = random.NextDouble() * 500.0;
            double height = random.NextDouble() * 500.0;

            Rect rect = new Rect(rectX, rectY, width, height);
            double radiusX = random.NextDouble() * 10.0;
            double radiusY = random.NextDouble() * 10.0;

            if (UseDefaultConstructor)
            {
                geometry = new RectangleGeometry();
                geometry.Rect = rect;
                geometry.RadiusX = radiusX;
                geometry.RadiusY = radiusY;
            }
            else
            {
                geometry = new RectangleGeometry(rect, radiusX, radiusY);
            }

            ApplyTransform(geometry);

            return geometry;
        }

        #endregion
    }
}
