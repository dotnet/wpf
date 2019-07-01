// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class VisualBrushFactory : TileBrushFactory<VisualBrush>
    {
        #region Public Members

        public Visual Visual { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new VisualBrush</returns>
        public override VisualBrush Create(DeterministicRandom random)
        {
            VisualBrush brush = new VisualBrush();
            ApplyTileBrushProperties(brush, random);

            brush.Visual = Visual;
            return brush;
        }

        #endregion
    }
}
