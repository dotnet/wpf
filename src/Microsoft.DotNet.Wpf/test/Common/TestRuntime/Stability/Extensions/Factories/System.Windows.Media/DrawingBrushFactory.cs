// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class DrawingBrushFactory : TileBrushFactory<DrawingBrush>
    {
        #region Public Members

        public Drawing Drawing { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new DrawingBrush</returns>
        public override DrawingBrush Create(DeterministicRandom random)
        {
            DrawingBrush brush = new DrawingBrush();
            ApplyTileBrushProperties(brush, random);

            brush.Drawing = Drawing;
            return brush;
        }

        #endregion
    }
}
