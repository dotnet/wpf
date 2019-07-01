// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.TestTypes
{    
    /// <summary>
    /// Test animation properties
    /// </summary>
    public class AnimationAPITest : AnimationAPITestBase
    {
        private const int sleepTime = 1000;

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public override void Init(Variation v)
        {
            base.Init(v);
            parameters = new UnitTestObjects(v);

            PrepareAnimation(v["ClassName"], v["Property"], v["From"], v["To"], v["DefaultValue"]);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        protected override IAnimatable GetAnimatingObject(string property, string propertyOwner)
        {
            switch (propertyOwner)
            {
                case "Camera":
                case "MatrixCamera":
                case "OrthographicCamera":
                case "PerspectiveCamera":
                case "ProjectionCamera":
                    return GetAnimatable(property, parameters.Camera);

                case "Light":
                case "AmbientLight":
                case "DirectionalLight":
                case "PointLight":
                case "SpotLight":
                    return GetAnimatable(property, parameters.Light);

                case "Transform3D":
                case "TranslateTransform3D":
                case "RotateTransform3D":
                case "ScaleTransform3D":
                case "MatrixTransform3D":
                case "Transform3DGroup":
                    return GetAnimatable(property, parameters.Transform);

                case "Model3D":
                case "GeometryModel3D":
                case "ScreenSpaceLines3D":
                    return GetAnimatable(property, parameters.Model);

                case "Model3DGroup":
                    return GetAnimatable(property, parameters.Group);

                // Shortcut to materials within models
                // They can also be reached by using ClassName="Model3D" and Property="Material.<property>"
                case "DiffuseMaterial":
                case "SpecularMaterial":
                case "EmissiveMaterial":
                case "MaterialGroup":
                case "Material":
                    return GetAnimatable(property, ((GeometryModel3D)parameters.Model).Material);

                default:
                    throw new NotSupportedException("Can't do animations on " + propertyOwner);
            }
        }

        /// <summary/>
        public override Visual GetWindowContent()
        {
            return parameters.Content;
        }

        private UnitTestObjects parameters;
    }
}
