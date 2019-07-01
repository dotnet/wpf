// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Collections.Generic;

using Microsoft.Test.Graphics.Factories;
using Microsoft.Test.Graphics.ReferenceRender;



namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Central parsing for factory-generated objects
    /// </summary>

    public class FactoryParser
    {
        /// <summary/>
        public static Model3D MakeModel(Variation v)
        {
            if (v["Lines"] != null)
            {
#if SSL
                v.AssertAbsenceOf( "Mesh", "Material" );
                return MakeScreenSpaceLines( v );
#else
                throw new ArgumentException("Can't create ScreenSpaceLines3D anymore");
#endif
            }
            else
            {
                Material mat = MakeFrontMaterial(v);
                MeshGeometry3D mesh = MakeMesh(v);
                GeometryModel3D gm = new GeometryModel3D(mesh, mat);
                gm.BackMaterial = MakeBackMaterial(v);
                return gm;
            }
        }

        /// <summary/>
        public static Material MakeFrontMaterial(Variation v)
        {
            return MakeMaterial(v, "Material");
        }

        /// <summary/>
        public static Material MakeBackMaterial(Variation v)
        {
            return MakeMaterial(v, "BackMaterial");
        }

        private static Material MakeMaterial(Variation v, string namePrefix)
        {
            Material material = null;
            string type = namePrefix + "Type";
            string specularPower = namePrefix + "SpecularPower";
            string ambientColor = namePrefix + "AmbientColor";
            string color = namePrefix + "Color";

            // Material CAN be null

            if (v[namePrefix] == "Group")
            {
                v.AssertAbsenceOf(ambientColor, color, specularPower);
                if (v[type] != null && v[type] != "Group")
                {
                    throw new ArgumentException("Can't have " + namePrefix + "=Group and type=" + v[type]);
                }
                material = MakeMaterialGroup(v, namePrefix);
            }
            else if (v[namePrefix] != null)
            {
                if (v[type] != null)
                {
                    switch (v[type])
                    {
                        case "Group":
                            throw new ArgumentException("Can't have 'Group' type without " + namePrefix + "='Group'");

                        case "Specular":
                            v.AssertExistenceOf(specularPower);
                            material = MaterialFactory.MakeMaterial(
                                            v[namePrefix],
                                            v[type],
                                            StringConverter.ToDouble(v[specularPower]));
                            break;

                        default:
                            v.AssertAbsenceOf(specularPower);
                            material = MaterialFactory.MakeMaterial(v[namePrefix], v[type]);
                            break;
                    }
                }
                else
                {
                    v.AssertAbsenceOf(specularPower);
                    material = MaterialFactory.MakeMaterial(v[namePrefix]);
                }

                // Parse Color Knobs
                if (v[ambientColor] != null)
                {
                    if (material is DiffuseMaterial)
                    {
                        ((DiffuseMaterial)material).AmbientColor = StringConverter.ToColor(v[ambientColor]);
                    }
                    else
                    {
                        throw new ArgumentException("Can't set " + namePrefix + "AmbientColor on non-DiffuseMaterial");
                    }
                }
                if (v[color] != null)
                {
                    if (material is DiffuseMaterial)
                    {
                        ((DiffuseMaterial)material).Color = StringConverter.ToColor(v[color]);
                    }
                    else if (material is EmissiveMaterial)
                    {
                        ((EmissiveMaterial)material).Color = StringConverter.ToColor(v[color]);
                    }
                    else if (material is SpecularMaterial)
                    {
                        ((SpecularMaterial)material).Color = StringConverter.ToColor(v[color]);
                    }
                    else
                    {
                        throw new ArgumentException("Can't set " + namePrefix + "Color on MaterialGroup");
                    }
                }
            }
            return material;
        }

        /// <summary/>
        public static MeshGeometry3D MakeMesh(Variation v)
        {
            v.AssertExistenceOf("Mesh");
            return MeshFactory.MakeMesh(v["Mesh"]);
        }

#if SSL
        public static ScreenSpaceLines3D MakeScreenSpaceLines( Variation v )
        {
            v.AssertExistenceOf( "Lines" );
            return ScreenSpaceLinesFactory.MakeLines( v[ "Lines" ] );
        }
#endif

        /// <summary/>
        public static Light MakeLight(Variation v)
        {
            Light light = null;

            if (v["Light"] != null)
            {
                v.AssertAbsenceOf("LightType", "LightColor", "LightPosition", "LightDirection", "LightRange", "LightConstantAttenuation", "LightLinearAttenuation", "LightQuadraticAttenuation", "LightInnerConeAngle", "LightOuterConeAngle");
                light = LightFactory.MakeLight(v["Light"]);
            }
            else
            {
                v.AssertExistenceOf("LightType", "LightColor");
                switch (v["LightType"])
                {
                    case "Ambient":
                        light = new AmbientLight();
                        break;

                    case "Directional":
                        v.AssertExistenceOf("LightDirection");
                        light = new DirectionalLight();
                        ((DirectionalLight)light).Direction = StringConverter.ToVector3D(v["LightDirection"]);
                        break;

                    case "Point":
                        v.AssertExistenceOf("LightPosition", "LightRange", "LightConstantAttenuation", "LightLinearAttenuation", "LightQuadraticAttenuation");
                        light = new PointLight();
                        SetLocalLightParameters((PointLight)light, v);
                        break;

                    case "Spot":
                        v.AssertExistenceOf("LightPosition", "LightDirection", "LightRange", "LightConstantAttenuation", "LightLinearAttenuation", "LightQuadraticAttenuation", "LightInnerConeAngle", "LightOuterConeAngle");
                        light = new SpotLight();
                        SetLocalLightParameters((SpotLight)light, v);
                        ((SpotLight)light).Direction = StringConverter.ToVector3D(v["LightDirection"]);
                        ((SpotLight)light).InnerConeAngle = StringConverter.ToDouble(v["LightInnerConeAngle"]);
                        ((SpotLight)light).OuterConeAngle = StringConverter.ToDouble(v["LightOuterConeAngle"]);
                        break;

                    default:
                        throw new ApplicationException("Invalid light type: " + v["LightType"]);
                }
                light.Color = StringConverter.ToColor(v["LightColor"]);
            }

            return light;
        }

        private static void SetLocalLightParameters(PointLightBase light, Variation v)
        {
            light.Position = StringConverter.ToPoint3D(v["LightPosition"]);
            light.Range = StringConverter.ToDouble(v["LightRange"]);
            light.ConstantAttenuation = StringConverter.ToDouble(v["LightConstantAttenuation"]);
            light.LinearAttenuation = StringConverter.ToDouble(v["LightLinearAttenuation"]);
            light.QuadraticAttenuation = StringConverter.ToDouble(v["LightQuadraticAttenuation"]);
        }

        /// <summary/>
        public static Camera MakeCamera(Variation v)
        {
            Camera camera = null;

            if (v["Camera"] != null)
            {
                v.AssertAbsenceOf("CameraType", "CameraPosition", "CameraLookDirection", "CameraUp", "CameraNearPlaneDistance", "CameraFarPlaneDistance", "CameraWidth", "CameraFieldOfView", "CameraViewMatrix", "CameraProjectionMatrix");
                camera = CameraFactory.MakeCamera(v["Camera"]);
            }
            else
            {
                v.AssertExistenceOf("CameraType");
                switch (v["CameraType"])
                {
                    case "Orthographic":
                        v.AssertExistenceOf("CameraPosition", "CameraLookDirection", "CameraUp", "CameraNearPlaneDistance", "CameraFarPlaneDistance", "CameraWidth");
                        camera = new OrthographicCamera();
                        SetProjectionCameraParameters(camera, v);
                        ((OrthographicCamera)camera).Width = StringConverter.ToDouble(v["CameraWidth"]);
                        break;

                    case "Perspective":
                        v.AssertExistenceOf("CameraPosition", "CameraLookDirection", "CameraUp", "CameraNearPlaneDistance", "CameraFarPlaneDistance", "CameraFieldOfView");
                        camera = new PerspectiveCamera();
                        SetProjectionCameraParameters(camera, v);
                        ((PerspectiveCamera)camera).FieldOfView = StringConverter.ToDouble(v["CameraFieldOfView"]);
                        break;

                    case "Matrix":
                        v.AssertExistenceOf("CameraViewMatrix", "CameraProjectionMatrix");
                        camera = new MatrixCamera();
                        ((MatrixCamera)camera).ViewMatrix = StringConverter.ToMatrix3D(v["CameraViewMatrix"]);
                        ((MatrixCamera)camera).ProjectionMatrix = StringConverter.ToMatrix3D(v["CameraProjectionMatrix"]);
                        break;

                    case "MatrixOrtho":
                        v.AssertExistenceOf("CameraPosition", "CameraLookDirection", "CameraUp", "CameraNearPlaneDistance", "CameraFarPlaneDistance", "CameraWidth", "CameraHeight");
                        camera = new MatrixCamera();
                        ((MatrixCamera)camera).ViewMatrix = MakeViewMatrix(v);
                        ((MatrixCamera)camera).ProjectionMatrix = MakeOrthoMatrix(v);
                        break;

                    case "MatrixPersp":
                        v.AssertExistenceOf("CameraPosition", "CameraLookDirection", "CameraUp", "CameraNearPlaneDistance", "CameraFarPlaneDistance", "CameraFieldOfViewX", "CameraFieldOfViewY");
                        camera = new MatrixCamera();
                        ((MatrixCamera)camera).ViewMatrix = MakeViewMatrix(v);
                        ((MatrixCamera)camera).ProjectionMatrix = MakePerspMatrix(v);
                        break;

                    default:
                        throw new ApplicationException("Invalid camera type: " + v["CameraType"]);
                }
            }

            return camera;
        }

        private static void SetProjectionCameraParameters(Camera camera, Variation v)
        {
            ProjectionCamera c = (ProjectionCamera)camera;

            c.Position = StringConverter.ToPoint3D(v["CameraPosition"]);
            c.LookDirection = StringConverter.ToVector3D(v["CameraLookDirection"]);
            c.UpDirection = StringConverter.ToVector3D(v["CameraUp"]);
            c.NearPlaneDistance = StringConverter.ToDouble(v["CameraNearPlaneDistance"]);
            c.FarPlaneDistance = StringConverter.ToDouble(v["CameraFarPlaneDistance"]);
        }

        private static Matrix3D MakeViewMatrix(Variation v)
        {
            return MatrixUtils.MakeViewMatrix(
                                    StringConverter.ToPoint3D(v["CameraPosition"]),
                                    StringConverter.ToVector3D(v["CameraLookDirection"]),
                                    StringConverter.ToVector3D(v["CameraUp"])
                                    );
        }

        private static Matrix3D MakeOrthoMatrix(Variation v)
        {
            return MatrixUtils.MakeOrthographicProjection(
                                    StringConverter.ToDouble(v["CameraNearPlaneDistance"]),
                                    StringConverter.ToDouble(v["CameraFarPlaneDistance"]),
                                    StringConverter.ToDouble(v["CameraWidth"]),
                                    StringConverter.ToDouble(v["CameraHeight"])
                                    );
        }

        private static Matrix3D MakePerspMatrix(Variation v)
        {
            return MatrixUtils.MakePerspectiveProjection(
                                    StringConverter.ToDouble(v["CameraNearPlaneDistance"]),
                                    StringConverter.ToDouble(v["CameraFarPlaneDistance"]),
                                    StringConverter.ToDouble(v["CameraFieldOfViewX"]),
                                    StringConverter.ToDouble(v["CameraFieldOfViewY"])
                                    );
        }

        /// <summary/>
        public static Visual3D[] MakeScene(Variation v)
        {
            v.AssertExistenceOf("Scene");

            string scene = v["Scene"];
            if (scene == "Explicit")
            {
                v.AssertExistenceOf("Visual0");
                return MakeVisuals(v, "Visual");
            }
            else if (scene == "VisualPerModel")
            {
                v.AssertExistenceOf("Visual0");
                Visual3D[] visuals = MakeVisuals(v, "Visual");
                ExpandModelGroups(visuals);
                return visuals;
            }
            else
            {
                ModelVisual3D visual = new ModelVisual3D();
                visual.Content = SceneFactory.MakeScene(scene);
                return new Visual3D[] { visual };
            }
        }

        private static Visual3D[] MakeVisuals(Variation v, string namePrefix)
        {
            // Meta-symbology:
            //  { }* == Klene star, or 0 or more occurrances of the stuff inside the brackets
            //  { }? == Optional, the stuff inside the brackets may or may not exist
            //  < >  == Description of what's parsed, the stuff inside the brackets is not a literal string

            // You can specify up to 10 children per model group (0-9)
            // namePrefix has format:  "Visual{n}*"

            List<Visual3D> visuals = new List<Visual3D>();

            string visualName = null;
            try
            {

                for (int i = 0; i < 10; i++)
                {

                    visualName = namePrefix + i.ToString();
                    string visualContent = v[visualName];
                    if (visualContent == null)
                    {
                        break;
                    }

                    // Each visual can have extra properties set on it:
                    // i.e:
                    //      Visual0="mesh"
                    //      Material0="255,255,255,255"
                    //      BackMaterial0="255,255,255,255"
                    //      Skip0="SkipSelf"
                    //      VisualTransform0="Translate 0,0,1"
                    //      ModelTransform0="Translate 0,0,-1"
                    //
                    //      Visual1="Group"
                    //      Child10="light"
                    //      Child11="mesh"
                    //      Child12="Group"
                    //      Child120="mesh"


                    visuals.Add(MakeVisual3D(v, visualName));

                }
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException("Failed to create (" + visualName + ")", ex);
            }

            Visual3D[] result = new Visual3D[visuals.Count];
            visuals.CopyTo(result);

            return result;
        }

        private static Visual3D MakeVisual3D(Variation v, string visualName)
        {
            string skipName = visualName.Replace("Visual", "Skip");
            string skipType = v[skipName] == null ? string.Empty : v[skipName];
            string visualType = v[visualName.Replace("Visual", "VisualType")];
            string visualTransformName = visualName.Replace("Visual", "VisualTransform");

            Visual3D visual = null;

            if (visualType == null || visualType == "ModelVisual3D")
            {
                //Default V1 test codepath
                visual = MakeModelVisual3D(v, visualName);
                ((ModelVisual3D)visual).Transform = TransformFactory.MakeTransform(v[visualTransformName]);
            }

#if TARGET_NET3_5
            else if (visualType == "ModelUIElement3D")
            {
                visual = MakeModelUIElement3D(v, visualName);
                visual.Transform = TransformFactory.MakeTransform(v[visualTransformName]);
            }
            else if (visualType == "ViewportVisual3D")
            {
                visual = MakeViewportVisual3D(v, visualName);
                visual.Transform = TransformFactory.MakeTransform(v[visualTransformName]);
            }
#endif

            else
            {
                throw new ArgumentException("Unable to create VisualType (" + visualType + ")." +
                    "Supported types are ModelVisual3D (+V3.5 ModelUIElement3D and ViewportVisual3D).");
            }

            
            visual.SetValue(Const.SkipProperty, skipType);
            return visual;
        }

        private static void PopulateChildVisuals(Variation v, string visualName, Visual3DCollection visualChildren)
        {
            if (v[visualName + "0"] != null)
            {
                Visual3D[] children = MakeVisuals(v, visualName);
                foreach (Visual3D v3d in children)
                {
                    visualChildren.Add(v3d);
                }
            }
        }

#if TARGET_NET3_5
        private static Visual3D MakeModelUIElement3D(Variation v, string visualName)
        {
            Model3D model = MakeCustomModel3D(v, visualName);
            ModelUIElement3D visual = new ModelUIElement3D();
            visual.Model = model;

            return visual;
        }

        private static Visual3D MakeViewportVisual3D(Variation v, string visualName)
        {
            Viewport2DVisual3D visual = new Viewport2DVisual3D();

            string visualContent = v[visualName];
            string materialName = visualName.Replace("Visual", "Material");
            //Back Material is intentionally omitted
            string modelTransformName = visualName.Replace("Visual", "VisualTransform");
            string modelChildName = visualName.Replace("Visual", "Child");

            //Only Geometry, Material and child are accepted, not general Model3D's. 
            visual.Geometry = MeshFactory.MakeMesh(visualContent);
            Material material = MakeMaterial(v, materialName);
            //Material can be null.
            if (material != null)
            {
                //TODO: Select Material which gets the IsChildHostProperty
                material.SetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty, true);
                visual.Material = material;
            }
            visual.Transform = TransformFactory.MakeTransform(v[modelTransformName]);

            //TODO: Select Visual Host material via DP

            //This operates at the same tree level as the parent, as it has 
            //a one-to-one parent relationship.

            if (v[modelChildName] != null)
            {
                Visual childVisual = VisualFactory.MakeVisual(v[modelChildName]);
                if (childVisual != null)
                {
                    visual.Visual = childVisual;
                }
            }
            return visual;
        }
#endif

        private static Visual3D MakeModelVisual3D(Variation v, string visualName)
        {
            Model3D model = MakeCustomModel3D(v, visualName);
            ModelVisual3D visual = new ModelVisual3D();
            visual.Content = model;

            PopulateChildVisuals(v, visualName, visual.Children);
            return visual;
        }

        private static Model3D MakeCustomModel3D(Variation v, string visualName)
        {
            string visualContent = v[visualName];
            string materialName = visualName.Replace("Visual", "Material");
            string backMaterialName = visualName.Replace("Visual", "BackMaterial");
            string modelTransformName = visualName.Replace("Visual", "ModelTransform");
            string modelChildrenName = visualName.Replace("Visual", "Child");

            Model3D model = ModelFactory.MakeModel(visualContent);
            if (model is GeometryModel3D)
            {
                // Material syntax is going to look a little wacky, but it's the easiest way to do this.
                //
                //      Material0="255,255,255,255"
                //      Material0Type="Specular"
                //      Material0Color="255,255,255,255"
                //      Material0AmbientColor="255,255,255,255"
                //      Material0SpecularPower="20"
                //

                Material front = MakeMaterial(v, materialName);
                if (front != null)
                {
                    ((GeometryModel3D)model).Material = front;
                }
                Material back = MakeMaterial(v, backMaterialName);
                if (back != null)
                {
                    ((GeometryModel3D)model).BackMaterial = back;
                }
            }
            model.Transform = TransformFactory.MakeTransform(v[modelTransformName]);

            if (model is Model3DGroup && v[modelChildrenName + "0"] != null)
            {
                ((Model3DGroup)model).Children = MakeModelCollection(v, modelChildrenName);
            }
            return model;
        }

        private static Model3DCollection MakeModelCollection(Variation v, string namePrefix)
        {
            // Meta-symbology:
            //  { }* == Klene star, or 0 or more occurrances of the stuff inside the brackets
            //  { }? == Optional, the stuff inside the brackets may or may not exist
            //  < >  == Description of what's parsed, the stuff inside the brackets is not a literal string

            // You can specify up to 10 children per model group (0-9)
            // namePrefix has format:  "Child{n}*"

            Model3DCollection collection = new Model3DCollection();

            for (int i = 0; i < 10; i++)
            {
                string modelName = namePrefix + i.ToString();
                string modelValue = v[modelName];
                if (modelValue == null)
                {
                    break;
                }

                string materialName = modelName.Replace("Child", "Material");
                string backMaterialName = modelName.Replace("Child", "BackMaterial");
                string transformName = modelName.Replace("Child", "ModelTransform");

                try
                {
                    Model3D model = ModelFactory.MakeModel(modelValue);
                    if (model is GeometryModel3D)
                    {
                        Material front = MakeMaterial(v, materialName);
                        Material back = MakeMaterial(v, backMaterialName);
                        if (front != null)
                        {
                            ((GeometryModel3D)model).Material = front;
                        }
                        ((GeometryModel3D)model).BackMaterial = back;
                    }
                    model.Transform = TransformFactory.MakeTransform(v[transformName]);

                    if (model is Model3DGroup && v[modelName + "0"] != null)
                    {
                        ((Model3DGroup)model).Children = MakeModelCollection(v, modelName);
                    }
                    collection.Add(model);
                }
                catch (ArgumentException)
                {
                    throw new ApplicationException("Confused by attribute (" + modelName + " = " + modelValue + ")");
                }
            }

            return collection;
        }

        private static void ExpandModelGroups(Visual3D[] visuals)
        {
            // Expand the model groups into Visual3Ds with children
            foreach (Visual3D visual in visuals)
            {
                ExpandModelGroups(visual);
            }
        }

        private static void ExpandModelGroups(Visual3D visual)
        {
            if (visual is ModelVisual3D)
            {
                ModelVisual3D modelVisual = (ModelVisual3D)visual;
                if (modelVisual.Content is Model3DGroup)
                {
                    Model3DGroup group = (Model3DGroup)modelVisual.Content;

                    // We will be replacing the content with children.
                    // Don't add them twice!
                    modelVisual.Content = null;
                    modelVisual.Transform = MergeTransforms(modelVisual.Transform, group.Transform);

                    foreach (Model3D model in ObjectUtils.GetChildren(group))
                    {
                        ModelVisual3D child = new ModelVisual3D();
                        child.Content = model;
                        ExpandModelGroups(child);
                        modelVisual.Children.Add(child);
                    }
                }
            }
        }

        private static Transform3D MergeTransforms(Transform3D t1, Transform3D t2)
        {
            if (t1 == null)
            {
                return t2;
            }
            if (t2 == null)
            {
                return t1;
            }
            Transform3DGroup result = new Transform3DGroup();
            result.Children = new Transform3DCollection(new Transform3D[] { t1, t2 });
            return result;
        }

        private static MaterialGroup MakeMaterialGroup(Variation v, string namePrefix)
        {
            // Material syntax is going to look a little wacky, but it's the easiest way to do this.
            //
            //      MaterialType="Group"
            //
            //      Material0="255,255,255,255"
            //      Material0Type="Specular"
            //      Material0Color="255,255,255,255"
            //      Material0AmbientColor="255,255,255,255"
            //      Material0SpecularPower="20"
            //
            //      Material1="cars.bmp"
            //      Material1Type="Emissive"
            //
            //      Material2="255,255,255,255"
            //
            //      Material3="Group"
            //
            //      Material30="..."
            //

            MaterialGroup mg = new MaterialGroup();
            mg.Children = new MaterialCollection();

            for (int i = 0; i < 10; i++)
            {
                // Get the current tree node
                string name = namePrefix + i.ToString();
                string currentMaterialChild = v[name];
                if (currentMaterialChild == null)
                {
                    break;
                }

                mg.Children.Add(MakeMaterial(v, name));
            }

            return mg;
        }

        /// <summary/>
        public static Transform3D MakeTransform3D(Variation v)
        {
            Transform3D tx;

            if (v["TransformType"] == null)
            {
                v.AssertAbsenceOf("TranslateOffset", "RotateAngle", "RotateAxis", "RotateQuaternion", "RotateCenter", "ScaleVector", "ScaleCenter", "MatrixValue");
                tx = Transform3D.Identity;
            }
            else
            {
                switch (v["TransformType"])
                {
                    case "Translate":
                        v.AssertExistenceOf("TranslateOffset");
                        tx = new TranslateTransform3D(StringConverter.ToVector3D(v["TranslateOffset"]));
                        break;

                    case "RotateAxisAngle":
                        v.AssertExistenceOf("RotateAngle", "RotateAxis");
                        tx = new RotateTransform3D(new AxisAngleRotation3D(StringConverter.ToVector3D(v["RotateAxis"]), StringConverter.ToDouble(v["RotateAngle"])));
                        break;

                    case "RotateAxisAngleCenter":
                        v.AssertExistenceOf("RotateAngle", "RotateAxis", "RotateCenter");
                        tx = new RotateTransform3D(new AxisAngleRotation3D(StringConverter.ToVector3D(v["RotateAxis"]), StringConverter.ToDouble(v["RotateAngle"])), StringConverter.ToPoint3D(v["RotateCenter"]));
                        break;

                    case "RotateQuaternion":
                        v.AssertExistenceOf("RotateQuaternion");
                        tx = new RotateTransform3D(new QuaternionRotation3D(StringConverter.ToQuaternion(v["RotateQuaternion"])));
                        break;

                    case "RotateQuaternionCenter":
                        v.AssertExistenceOf("RotateQuaternion", "RotateCenter");
                        tx = new RotateTransform3D(new QuaternionRotation3D(StringConverter.ToQuaternion(v["RotateQuaternion"])), StringConverter.ToPoint3D(v["RotateCenter"]));
                        break;

                    case "RotateNullRotation":
                        tx = new RotateTransform3D();
                        ((RotateTransform3D)tx).Rotation = null;
                        break;

                    case "Scale":
                        v.AssertExistenceOf("ScaleVector");
                        tx = new ScaleTransform3D(StringConverter.ToVector3D(v["ScaleVector"]));
                        break;

                    case "ScaleCenter":
                        v.AssertExistenceOf("ScaleVector", "ScaleCenter");
                        tx = new ScaleTransform3D(StringConverter.ToVector3D(v["ScaleVector"]), StringConverter.ToPoint3D(v["ScaleCenter"]));
                        break;

                    case "Matrix":
                        v.AssertExistenceOf("MatrixValue");
                        tx = new MatrixTransform3D(StringConverter.ToMatrix3D(v["MatrixValue"]));
                        break;

                    case "Group":
                        tx = new Transform3DGroup();
                        break;

                    case "GroupNullChildren":
                        tx = new Transform3DGroup();
                        ((Transform3DGroup)tx).Children = null;
                        break;

                    case "Null":
                        tx = null;
                        break;

                    default:
                        throw new ApplicationException("Invalid TransformType specified: " + v["TransformType"]);
                }
            }

            return tx;
        }

        /// <summary/>
        public static void MakeTolerance(Variation v)
        {
            RenderTolerance.ResetDefaults();

            if (v["PixelToEdgeTolerance"] != null)
            {
                RenderTolerance.PixelToEdgeTolerance = StringConverter.ToDouble(v["PixelToEdgeTolerance"]);
            }

            if (v["LightingRangeTolerance"] != null)
            {
                RenderTolerance.LightingRangeTolerance = StringConverter.ToDouble(v["LightingRangeTolerance"]);
            }

            if (v["SpotLightAngleTolerance"] != null)
            {
                RenderTolerance.SpotLightAngleTolerance = StringConverter.ToDouble(v["SpotLightAngleTolerance"]);
            }

            if (v["ZBufferTolerance"] != null)
            {
                RenderTolerance.ZBufferTolerance = StringConverter.ToDouble(v["ZBufferTolerance"]);
            }

            if (v["SpecularLightDotProductTolerance"] != null)
            {
                RenderTolerance.SpecularLightDotProductTolerance = StringConverter.ToDouble(v["SpecularLightDotProductTolerance"]);
            }

            if (v["DefaultColorTolerance"] != null)
            {
                RenderTolerance.DefaultColorTolerance = StringConverter.ToColor(v["DefaultColorTolerance"]);
            }

            if (v["TextureLookUpTolerance"] != null)
            {
                RenderTolerance.TextureLookUpTolerance = StringConverter.ToDouble(v["TextureLookUpTolerance"]);
            }

            if (v["SilhouetteEdgeTolerance"] != null)
            {
                RenderTolerance.SilhouetteEdgeTolerance = StringConverter.ToDouble(v["SilhouetteEdgeTolerance"]);
            }

            if (v["ViewportClippingTolerance"] != null)
            {
                RenderTolerance.ViewportClippingTolerance = StringConverter.ToDouble(v["ViewportClippingTolerance"]);
            }
        }

        /// <summary/>
        public static HitTestFilterCallback MakeFilter(Variation v)
        {
            if (v["HitTestFilter"] == null)
            {
                // TODO: return SkipNone again once RetainedVisual3D is pulled.
                return null;
            }
            else
            {
                return HitTestFilterFactory.MakeFilter(v["HitTestFilter"]);
            }
        }
    }
}


