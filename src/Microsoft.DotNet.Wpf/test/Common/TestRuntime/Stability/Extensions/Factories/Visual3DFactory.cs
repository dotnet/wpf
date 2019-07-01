// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

//TODO: Make User Derived UIElement3D Factory
namespace Microsoft.Test.Stability.Extensions.Factories
{
    abstract class Visual3DFactory<T> : DiscoverableFactory<T> where T : Visual3D
    {
        public Transform3D Transform3D { get; set; }

        protected void ApplyTransform(T visual3D)
        {
            visual3D.Transform = Transform3D;
        }
    }

    class ModelVisual3DFactory : Visual3DFactory<ModelVisual3D>
    {
        public Model3D Model3D { get; set; }
        //HACK:Visual3DCollection has an internal only constructor!!!
        public List<Visual3D> Children { get; set; }

        public override ModelVisual3D Create(DeterministicRandom random)
        {
            ModelVisual3D modelVisual3D = new ModelVisual3D();
            ApplyTransform(modelVisual3D);
            modelVisual3D.Content = Model3D;
            HomelessTestHelpers.Merge(modelVisual3D.Children,Children);
            return modelVisual3D;
        }
    }

    class ModelUIElement3DFactory : Visual3DFactory<ModelUIElement3D>
    {
        public Model3D Model3D { get; set; }

        public override ModelUIElement3D Create(DeterministicRandom random)
        {
            ModelUIElement3D modelUIElement3D = new ModelUIElement3D();
            ApplyTransform(modelUIElement3D);
            modelUIElement3D.Model = Model3D;
            return modelUIElement3D;
        }
    }

    class ContainerUIElement3DFactory : Visual3DFactory<ContainerUIElement3D>
    {
        //HACK:Visual3DCollection has an internal only constructor!!!
        public List<Visual3D> Children { get; set; }

        public override ContainerUIElement3D Create(DeterministicRandom random)
        {
            ContainerUIElement3D containerUIElement3D = new ContainerUIElement3D();
            ApplyTransform(containerUIElement3D);
            HomelessTestHelpers.Merge(containerUIElement3D.Children, Children);
            return containerUIElement3D;
        }
    }

    class Viewport2DVisual3DFactory : Visual3DFactory<Viewport2DVisual3D>
    {
        public Visual Visual { get; set; }
        public Material Material { get; set; }

        public override Viewport2DVisual3D Create(DeterministicRandom random)
        {
            Viewport2DVisual3D viewport2DVisual3D = new Viewport2DVisual3D();
            ApplyTransform(viewport2DVisual3D);
            viewport2DVisual3D.Visual = Visual;
            viewport2DVisual3D.Material = Material;
            return viewport2DVisual3D;
        }
    }
}
