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
    /// Gouraud shader. Precomputes multiple lights per vertex.
    /// </summary>
    internal class PrecomputedGouraudShader : Shader
    {
        public PrecomputedGouraudShader(
                Triangle[] triangles,
                RenderBuffer buffer,
                Light[] lights,
                TextureFilter[] textures,
                Matrix3D view)
            : base(triangles, buffer)
        {
            // Don't write to z-buffer by default ... only if DiffuseMaterial is present in the scene
            bool writeToZBuffer = false;

            // Materials
            this.textures = textures;
            if (textures != null)
            {
                // See from input wether we need to compute additional information
                foreach (TextureFilter texture in textures)
                {
                    if (texture != null)
                    {
                        // texture lookup tolerance
                        if (texture.HasErrorEstimation)
                        {
                            needsUVTolerance = true;
                        }
                        // mip map coefficcient
                        if (texture is TrilinearTextureFilter)
                        {
                            needsMipMapCoefficient = true;
                        }
                        // z-write
                        if (texture.MaterialType == MaterialType.Diffuse)
                        {
                            // if there's at least 1 DiffuseMaterial in the shader, it will force a z-write
                            writeToZBuffer = true;
                        }
                    }
                }
            }

            // Lighting
            lightEquations = new LightEquation[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                lightEquations[i] = LightEquation.For(lights[i], view);
            }

            // Z-writes
            this.buffer.WriteToZBuffer = writeToZBuffer;
        }

        /// <summary>
        /// Gouraud shading computes lighting per-vertex, in this method.
        /// Final pixel values are gathered from interpolating between pixels.
        /// Lighting is precomputed in this step per material.
        /// </summary>
        /// <param name="v">Input/Output per-vertex data.</param>
        protected override void ComputeVertexProgram(Vertex v)
        {
            // No color here ...
            v.Color = emptyColor;
            // Tolerance
            v.ColorTolerance = emptyColor;

            v.PrecomputedLight = new Color[textures.Length];
            v.PrecomputedLightTolerance = new Color[textures.Length];
            int textureIndex = 0;
            Point3D position = v.PositionAsPoint3D;
            Vector3D normal = v.Normal;

            // Precompute lighting for all textures
            foreach (TextureFilter texture in textures)
            {
                HdrColor color = emptyColor;
                HdrColor tolerance = emptyColor;

                if (texture != null)
                {
                    // Lighting is done in Premultiplied color space.
                    //
                    //  - BIG NOTE! - We do not actually premultiply the light contribution
                    //    because lighting is always opaque and premultiplying opaque colors
                    //    is a no-op.
                    //
                    //  - Tolerance CANNOT be done in Premultiplied color space because
                    //    it needs to know the final pixel color in order to premultiply properly.
                    //    We won't know the final pixel color until ComputePixelProgram is called.

                    // We ignore Alpha values during the computation and set the final value to
                    //  materialColor.A at the end.  This is why you will not see any clamping of
                    //  light values, etc in the code below.

                    Color materialColor = ColorOperations.PreMultiplyColor(texture.MaterialColor);

                    switch (texture.MaterialType)
                    {
                        case MaterialType.Diffuse:
                            foreach (LightEquation light in lightEquations)
                            {
                                Color lightContribution = light.IluminateDiffuse(position, normal);

                                if (light is AmbientLightEquation)
                                {
                                    // AmbientColor knobs are reminiscent of additive material passes
                                    //  because the alpha value will not be considered in the final color value
                                    //  (i.e. premultiply to scale RGB by alpha, then never use alpha again)
                                    Color ambientColor = ColorOperations.PreMultiplyColor(texture.AmbientColor);
                                    color += ColorOperations.Modulate(lightContribution, ambientColor);
                                }
                                else
                                {
                                    color += ColorOperations.Modulate(lightContribution, materialColor);
                                }
                                tolerance += light.GetLastError();
                            }
                            break;

                        case MaterialType.Specular:
                            foreach (LightEquation light in lightEquations)
                            {
                                // Don't need to check light equation type, since Ambient will return black
                                Color lightContribution = light.IluminateSpecular(position, normal, texture.SpecularPower);
                                color += ColorOperations.Modulate(lightContribution, materialColor);

                                tolerance += light.GetLastError();
                            }
                            break;

                        case MaterialType.Emissive:
                            color = materialColor;
                            break;
                    }

                    // Alpha is only considered at the end.  Overwrite whatever happened during the precomputation.
                    //  - Note that the alpha of the AmbientColor knob is NOT considered in the final value.

                    color.A = ColorOperations.ByteToDouble(materialColor.A);
                }

                v.PrecomputedLight[textureIndex] = color.ClampedValue;
                v.PrecomputedLightTolerance[textureIndex] = tolerance.ClampedValue;

                textureIndex++;
            }
        }

        /// <summary>
        /// Lights this pixel using precomputed lighting information.
        /// </summary>
        /// <param name="v">Interpolated vertex for this pixel position.</param>
        protected override void ComputePixelProgram(Vertex v)
        {
            bool rendered = false;
            Color totalTexturingTolerance = emptyColor;

            for (int pass = 0; pass < textures.Length; pass++)
            {
                // A Filter can be null if the Material or the Brush are null.
                // For those cases, we skip the material entirely.
                if (textures[pass] == null)
                {
                    continue;
                }
                TextureFilter currentTexture = textures[pass];
                rendered = true;

                // We need extra information for trilinear
                if (currentTexture is TrilinearTextureFilter)
                {
                    ((TrilinearTextureFilter)currentTexture).MipMapFactor = v.MipMapFactor;
                }

                // Textures are not stored in premultiplied color space.
                // This means that we have to wait until we find the lookup tolerance before we can premultiply
                //  (otherwise Alpha will be way off)

                Color texel = currentTexture.FilteredTextureLookup(v.TextureCoordinates);
                Color texelTolerance = emptyColor;
                if (currentTexture.HasErrorEstimation)
                {
                    texelTolerance = currentTexture.FilteredErrorLookup(
                            v.UVToleranceMin,
                            v.UVToleranceMax,
                            texel);
                }

                // Now we can premultiply.
                Color premultTexel = ColorOperations.PreMultiplyColor(texel);
                Color premultTexelTolerance = ColorOperations.PreMultiplyTolerance(texelTolerance, texel.A);

                // Modulate precomputed lighting (which is also premultiplied) by the Brush value
                Color premultColor = ColorOperations.Modulate(v.PrecomputedLight[pass], premultTexel);
                Color premultTolerance = ColorOperations.Modulate(v.PrecomputedLight[pass], premultTexelTolerance);

                // PrecomputedLightTolerance is NOT premultipled yet (see ComputeVertexProgram above).
                //  This is because we needed to know the final alpha value of lighting * texture.
                //
                Color premultLightTolerance = ColorOperations.PreMultiplyTolerance(v.PrecomputedLightTolerance[pass], premultColor.A);
                premultTolerance = ColorOperations.Add(premultTolerance, premultLightTolerance);

                // For additive materials, we need to force the alpha channel to zero.
                // See notes on premultiplied blending in ColorOperations.cs
                if (currentTexture.MaterialType != MaterialType.Diffuse)
                {
                    premultColor.A = 0x00;
                    // Nothing needs to be done to tolerance's alpha
                    //  because the framebuffer's alpha value will be used in the blend
                }

                // we need to blend
                // Write to frame buffer according to alpha value of pixel
                v.Color = ColorOperations.PreMultipliedAlphaBlend(premultColor, v.Color);

                // Accumulate tolerance for each material pass
                totalTexturingTolerance = ColorOperations.PreMultipliedToleranceBlend(
                        premultTolerance,
                        totalTexturingTolerance,
                        premultColor.A);
            }

            // Only set a pixel if we actually rendered at least one material for it ...
            if (rendered)
            {
                // Add texturing tolerance to our existing lighting tolerance.
                v.ColorTolerance = ColorOperations.Add(v.ColorTolerance, totalTexturingTolerance);

                // Send the pixel to be rendered
                buffer.SetPixel(
                        (int)Math.Floor(v.ProjectedPosition.X),
                        (int)Math.Floor(v.ProjectedPosition.Y),
                        (float)v.ProjectedPosition.Z,
                        v.Color,
                        v.ColorTolerance
                        );
            }
        }

        protected TextureFilter[] textures;
        protected LightEquation[] lightEquations;
    }
}
