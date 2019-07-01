// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class GeometryDrawingFactory : DiscoverableFactory<GeometryDrawing>
    {
        public Brush Brush { get; set; }
        public Pen Pen { get; set; }
        public Geometry Geometry { get; set; }

        public override GeometryDrawing Create(DeterministicRandom random)
        {
            GeometryDrawing drawing = new GeometryDrawing();
            drawing.Brush = Brush;
            drawing.Pen = Pen;
            drawing.Geometry = Geometry;

            return drawing;
        }
    }
}
