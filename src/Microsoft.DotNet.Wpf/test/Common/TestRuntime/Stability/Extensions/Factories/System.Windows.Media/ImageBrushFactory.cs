// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal class ImageBrushFactory : TileBrushFactory<ImageBrush>
    {
        #region Public Members

        public ImageSource ImageSource { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new ImageBrush</returns>
        public override ImageBrush Create(DeterministicRandom random)
        {
            ImageBrush brush = new ImageBrush();
            ApplyTileBrushProperties(brush, random);

            brush.ImageSource = ImageSource;
            return brush;
        }

        #endregion
    }
}
