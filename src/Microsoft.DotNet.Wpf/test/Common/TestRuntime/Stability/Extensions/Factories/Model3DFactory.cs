// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    abstract class Model3DFactory<T> : DiscoverableFactory<T> where T : Model3D
    {
        public Transform3D Transform3D { get; set; }

        protected void ApplyModel3DProperties(T model3D)
        {
            model3D.Transform = Transform3D;
        }
    }

    abstract class LightFactory<T> : Model3DFactory<T> where T : Light
    {
        public Color Color { get; set; }

        protected void ApplyLightProperties(T light, DeterministicRandom random)
        {
            light.Color = Color;
        }
    }

    class AmbientLightFactory : LightFactory<AmbientLight>
    {
        public override AmbientLight Create(DeterministicRandom random)
        {
            AmbientLight ambientLight = new AmbientLight();
            ApplyLightProperties(ambientLight, random);
            return ambientLight;
        }
    }

    class DirectionalLightFactory : LightFactory<DirectionalLight>
    {
        public override DirectionalLight Create(DeterministicRandom random)
        {
            DirectionalLight directionalLight = new DirectionalLight();
            ApplyLightProperties(directionalLight, random);
            directionalLight.Direction = new Vector3D(random.NextDouble() * 50, random.NextDouble() * 50, random.NextDouble() * 50);
            return directionalLight;
        }
    }

    abstract class PointLightBaseFactory<T> : LightFactory<T> where T : PointLightBase
    {
        protected void ApplyPointLightBaseProperties(T light, DeterministicRandom random)
        {
            light.Range = random.NextDouble() * 100;
            light.Position = new Point3D(random.NextDouble() * 200, random.NextDouble() * 200, random.NextDouble() * 200);
            light.Range = random.NextDouble() * 100;
            light.ConstantAttenuation = random.NextDouble() * 4;
            light.LinearAttenuation = random.NextDouble();
            light.QuadraticAttenuation = random.NextDouble();
        }
    }

    class PointLightFactory : PointLightBaseFactory<PointLight>
    {
        public override PointLight Create(DeterministicRandom random)
        {
            PointLight light = new PointLight();
            ApplyPointLightBaseProperties(light, random);
            ApplyLightProperties(light, random);
            return light;
        }
    }


    class SpotLightFactory : PointLightBaseFactory<SpotLight>
    {
        public override SpotLight Create(DeterministicRandom random)
        {
            SpotLight light = new SpotLight();
            ApplyLightProperties(light, random);
            ApplyPointLightBaseProperties(light, random);
            light.Direction = new Vector3D(random.NextDouble() * 10, random.NextDouble() * 10, random.NextDouble() * 10);
            light.InnerConeAngle = random.NextDouble() * 180;
            light.OuterConeAngle = random.NextDouble() * 180;
            return light;
        }
    }

    class GeometryModel3DFactory : Model3DFactory<GeometryModel3D>
    {
        public Geometry3D Geometry3D { get; set; }
        public Material Material { get; set; }
        public Material BackMaterial { get; set; }

        public override GeometryModel3D Create(DeterministicRandom random)
        {
            GeometryModel3D geometryModel3D = new GeometryModel3D();
            ApplyModel3DProperties(geometryModel3D);
            geometryModel3D.Geometry = Geometry3D;
            geometryModel3D.Material = Material;
            geometryModel3D.BackMaterial = BackMaterial;
            return geometryModel3D;
        }
    }

    class Model3DCollectionFactory : DiscoverableCollectionFactory<Model3DCollection, Model3D> { }

    class Model3DGroupFactory : Model3DFactory<Model3DGroup>
    {
        public Model3DCollection Model3DCollection { get; set; }

        public override Model3DGroup Create(DeterministicRandom random)
        {
            Model3DGroup model3DGroup = new Model3DGroup();
            model3DGroup.Children = Model3DCollection;
            return model3DGroup;
        }
    }
}
