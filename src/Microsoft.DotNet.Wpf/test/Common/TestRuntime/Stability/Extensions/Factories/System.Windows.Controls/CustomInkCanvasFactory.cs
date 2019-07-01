// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Ink;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.CustomTypes;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create a custom InkCanvas.
    /// </summary>
    internal class CustomInkCanvasFactory : DiscoverableFactory<CustomInkCanvas>
    {
        /// <summary>
        /// Gets or sets a StrokeCollection to set InkCanvas Strokes property.
        /// </summary>
        public StrokeCollection Strokes { get; set; }

        public override CustomInkCanvas Create(DeterministicRandom random)
        {
            CustomInkCanvas inkCanvas = new CustomInkCanvas();
            inkCanvas.Strokes = Strokes;
            return inkCanvas;
        }
    }
}
