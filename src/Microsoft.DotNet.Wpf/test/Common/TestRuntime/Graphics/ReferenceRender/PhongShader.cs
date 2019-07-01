// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// Phong shader - Precomputes multiple lights per pixel.
    /// </summary>
    internal class PrecomputedPhongShader : PrecomputedGouraudShader
    {
        public PrecomputedPhongShader(
                Triangle[] triangles,
                RenderBuffer buffer,
                Light[] lights,
                TextureFilter[] textures,
                Matrix3D view)
            : base(triangles, buffer, lights, textures, view)
        {
        }

        /// <summary>
        /// Lights this pixel using precomputed lighting information.
        /// </summary>
        /// <param name="v">Interpolated vertex for this pixel position.</param>
        override protected void ComputePixelProgram(Vertex v)
        {
            // In the base Gouraud shader light is computed on a per-vertex level.
            // Reuse that here... :)
            ComputeVertexProgram(v);
            // Now do the Gouraud per-pixel program
            base.ComputePixelProgram(v);
        }
    }
}
