// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// Inherited this abstract class to implement a concrete TileBrush factory.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class TileBrushFactory<T> : BrushFactory<T> where T : TileBrush
    {
        #region Public Members

        public Rect Viewbox { get; set; }
        public Rect Viewport { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="brush"/>
        /// <param name="random"/>
        protected void ApplyTileBrushProperties(TileBrush brush, DeterministicRandom random)
        {
            ApplyBrushProperties(brush, random);

            brush.AlignmentX = random.NextEnum<AlignmentX>();
            brush.AlignmentY = random.NextEnum<AlignmentY>();
            brush.Stretch = random.NextEnum<Stretch>();
            brush.TileMode = random.NextEnum<TileMode>();
            brush.Viewbox = Viewbox;
            brush.ViewboxUnits = random.NextEnum<BrushMappingMode>();
            brush.Viewport = Viewport;
            brush.ViewportUnits = random.NextEnum<BrushMappingMode>();
        }

        #endregion
    }
}
