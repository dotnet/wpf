// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Controls;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CanvasFactory : PanelFactory<Canvas>
    {
        public override Canvas Create(DeterministicRandom random)
        {
            Canvas canvas = new Canvas();
            ApplyCommonProperties(canvas, random);
            SetChildrenLayout(canvas, random);
            return canvas;
        }

        private void SetChildrenLayout(Canvas canvas, DeterministicRandom random)
        {
            foreach (UIElement item in canvas.Children)
            {
                switch (random.Next() % 4)
                {
                    case 0:
                        Canvas.SetLeft(item, (random.NextDouble() - 0.5) * canvas.ActualWidth * 2);
                        Canvas.SetBottom(item, (random.NextDouble() - 0.5) * canvas.ActualHeight * 2);
                        break;
                    case 1:
                        Canvas.SetLeft(item, (random.NextDouble() - 0.5) * canvas.ActualWidth * 2);
                        Canvas.SetTop(item, (random.NextDouble() - 0.5) * canvas.ActualHeight * 2);
                        break;
                    case 2:
                        Canvas.SetRight(item, (random.NextDouble() - 0.5) * canvas.ActualWidth * 2);
                        Canvas.SetBottom(item, (random.NextDouble() - 0.5) * canvas.ActualHeight * 2);
                        break;
                    case 3:
                        Canvas.SetRight(item, (random.NextDouble() - 0.5) * canvas.ActualWidth * 2);
                        Canvas.SetTop(item, (random.NextDouble() - 0.5) * canvas.ActualHeight * 2);
                        break;
                }
            }
        }
    }
}
