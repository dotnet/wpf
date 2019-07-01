// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class DiffuseMaterialFactory : DiscoverableFactory<DiffuseMaterial>
    {
        public Color Color { get; set; }
        public Color AmbientColor { get; set; }
        public Brush Brush { get; set; }

        public override DiffuseMaterial Create(DeterministicRandom random)
        {
            DiffuseMaterial diffuseMaterial = new DiffuseMaterial();
            diffuseMaterial.AmbientColor = AmbientColor;
            diffuseMaterial.Color = Color;
            diffuseMaterial.Brush = Brush;
            return diffuseMaterial;
        }
    }

    class EmissiveMaterialFactory : DiscoverableFactory<EmissiveMaterial>
    {
        public Color Color { get; set; }
        public Brush Brush { get; set; }

        public override EmissiveMaterial Create(DeterministicRandom random)
        {
            EmissiveMaterial emissiveMaterial = new EmissiveMaterial();
            emissiveMaterial.Color = Color;
            emissiveMaterial.Brush = Brush;
            return emissiveMaterial;
        }
    }

    class SpecularMaterialFactory : DiscoverableFactory<SpecularMaterial>
    {
        public Color Color { get; set; }
        public Brush Brush { get; set; }

        public override SpecularMaterial Create(DeterministicRandom random)
        {
            SpecularMaterial specularMaterial = new SpecularMaterial();
            specularMaterial.Color = Color;
            specularMaterial.SpecularPower = random.NextDouble() * 100;
            specularMaterial.Brush = Brush;
            return specularMaterial;
        }
    }

    class MaterialCollectionFactory : DiscoverableCollectionFactory<MaterialCollection, Material> { }

    class MaterialGroupFactory : DiscoverableFactory<MaterialGroup>
    {
        public MaterialCollection MaterialCollection { get; set; }

        public override MaterialGroup Create(DeterministicRandom random)
        {
            MaterialGroup materialGroup = new MaterialGroup();
            materialGroup.Children = MaterialCollection;
            return materialGroup;
        }
    }
}
