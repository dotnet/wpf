// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete GradientBrush factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class GradientBrushFactory<T> : BrushFactory<T> where T : GradientBrush
    {
        #region Public Members

        public GradientStopCollection GradientStops { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="brush"/>
        /// <param name="random"/>
        protected void ApplyGradientBrushProperties(GradientBrush brush, DeterministicRandom random)
        {
            ApplyBrushProperties(brush, random);

            brush.ColorInterpolationMode = random.NextEnum<ColorInterpolationMode>();
            brush.GradientStops = GradientStops;
            brush.MappingMode = random.NextEnum<BrushMappingMode>();
            brush.SpreadMethod = random.NextEnum<GradientSpreadMethod>();
        }

        #endregion
    }
}
