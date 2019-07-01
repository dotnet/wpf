// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class SolidColorBrushFactory : BrushFactory<SolidColorBrush>
    {
        #region Public Members

        public Color Color { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new SolidColorBrush</returns>
        public override SolidColorBrush Create(DeterministicRandom random)
        {
            SolidColorBrush brush = new SolidColorBrush(Color);
            ApplyBrushProperties(brush, random);
            return brush;
        }

        #endregion
    }
}
