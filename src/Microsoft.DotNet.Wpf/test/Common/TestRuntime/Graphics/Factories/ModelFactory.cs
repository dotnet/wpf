// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Make a Model3D by using other Factories (when we don't know what we want yet)
    /// </summary>
    public class ModelFactory
    {

        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3D MakeModel(string model)
        {
            return MakeModel(model, null, null);
        }


        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static Model3D MakeModel(string model, Material material, Material backMaterial)
        {
            string exceptions = string.Empty;

            try
            {
                MeshGeometry3D mesh = MeshFactory.MakeMesh(model);
                Material front = (material == null) ? MaterialFactory.Default : material;
                Material back = backMaterial;
                GeometryModel3D model2 = new GeometryModel3D(mesh, front);
                model2.BackMaterial = back;
                return model2;
            }
            catch (ArgumentException ex)
            {
                /* It wasn't a mesh */
                exceptions += ex.Message + ", ";
            }
#if SSL
            try
            {
                ScreenSpaceLines3D lines = ScreenSpaceLinesFactory.MakeLines( model );
                return lines;
            }
            catch ( ArgumentException )
            {
                /* It wasn't ScreenSpaceLines */
                exceptions += ex.Message + ", ";
            }
#endif
            try
            {
                Light light = LightFactory.MakeLight(model);
                return light;
            }
            catch (ArgumentException ex)
            {
                /* It wasn't a Light */
                exceptions += ex.Message + ", ";
            }

            try
            {
                Model3DGroup group = SceneFactory.MakeScene(model);
                return group;
            }
            catch (ArgumentException ex)
            {
                /* It wasn't a Scene */
                exceptions += ex.Message;
            }

            throw new ArgumentException("Cannot create (" + model + ").  Exceptions thrown: " + exceptions);
        }
    }
}
