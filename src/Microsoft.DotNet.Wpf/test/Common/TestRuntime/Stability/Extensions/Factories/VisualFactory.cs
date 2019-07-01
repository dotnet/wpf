// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;
using System.Collections;

//NOTE: BitmapEffect properties's are not set on visuals due to horrible perf.

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class ContainerVisualFactory : DiscoverableFactory<ContainerVisual>
    {
        public Geometry Geometry { get; set; }
        public Vector Vector { get; set; }
        public Brush Brush { get; set; }
        public Transform Transform { get; set; }
        public DoubleCollection XSnappingGuidelines { get; set; }
        public DoubleCollection YSnappingGuidelines { get; set; }
        public List<Visual> Children { get; set; }

        public override ContainerVisual Create(DeterministicRandom random)
        {
            ContainerVisual containerVisual = new ContainerVisual();
            containerVisual.Clip = Geometry;
            containerVisual.Offset = Vector;
            containerVisual.Opacity = random.NextDouble();
            containerVisual.OpacityMask = Brush;
            containerVisual.Transform = Transform;
            containerVisual.XSnappingGuidelines = XSnappingGuidelines;
            containerVisual.YSnappingGuidelines = YSnappingGuidelines;
            HomelessTestHelpers.Merge(containerVisual.Children, Children);
            return containerVisual;
        }
    }

    class Viewport3DVisualFactory : DiscoverableFactory<Viewport3DVisual>
    {
        public Camera Camera { get; set; }
        public Geometry Geometry { get; set; }
        public Rect Rect { get; set; }
        public Vector Vector { get; set; }
        public Brush Brush { get; set; }
        public Size Size { get; set; }
        public Transform Transform { get; set; }
        public Point Point { get; set; }
        public DoubleCollection DoubleCollection { get; set; }
        public List<Visual3D> Children { get; set; }

        public override Viewport3DVisual Create(DeterministicRandom random)
        {
            Viewport3DVisual viewport3DVisual = new Viewport3DVisual();
            viewport3DVisual.Camera = Camera;
            HomelessTestHelpers.Merge(viewport3DVisual.Children, Children);
            viewport3DVisual.Clip = Geometry;
            viewport3DVisual.Offset = Vector;
            viewport3DVisual.Opacity = random.NextDouble();
            viewport3DVisual.OpacityMask = Brush;
            viewport3DVisual.Transform = Transform;
            viewport3DVisual.Viewport = Rect;
            return viewport3DVisual;
        }
    }

}
