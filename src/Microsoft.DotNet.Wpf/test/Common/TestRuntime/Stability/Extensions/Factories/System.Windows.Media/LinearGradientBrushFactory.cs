// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class LinearGradientBrushFactory : GradientBrushFactory<LinearGradientBrush>
    {
        #region Public Members

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        #endregion

        #region Override Members

        /// <summary/>
        /// <param name="random"/>
        /// <returns>Return a new LinearGradientBrush</returns>
        public override LinearGradientBrush Create(DeterministicRandom random)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            ApplyGradientBrushProperties(brush, random);

            brush.StartPoint = StartPoint;
            brush.EndPoint = EndPoint;
            return brush;
        }

        #endregion
    }
}
