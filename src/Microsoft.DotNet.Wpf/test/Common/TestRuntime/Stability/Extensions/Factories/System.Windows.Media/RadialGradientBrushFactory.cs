// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{

    class RadialGradientBrushFactory : GradientBrushFactory<RadialGradientBrush>
    {
        #region Public Members

        public Point GradientOrigin { get; set; }
        public Point Center { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new RadialGradientBrush</returns>
        public override RadialGradientBrush Create(DeterministicRandom random)
        {
            RadialGradientBrush brush = new RadialGradientBrush();
            ApplyGradientBrushProperties(brush, random);

            brush.Center = Center;
            brush.GradientOrigin = GradientOrigin;
            brush.RadiusX = GradientOrigin.X * 2 * random.NextDouble();
            brush.RadiusY = GradientOrigin.Y * 2 * random.NextDouble();
            return brush;
        }

        #endregion
    }
}
