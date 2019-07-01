// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

using FileMode = System.IO.FileMode;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
using Microsoft.Test.Graphics.TestTypes;
#else
using TrustedFileStream = System.IO.FileStream;
using Microsoft.Test.Graphics.TestTypes;
#endif

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary/>
    public enum MaterialType
    {
        /// <summary/>
        Diffuse,
        /// <summary/>
        Specular,
        /// <summary/>
        Emissive
    };

    /// <summary>
    /// Texture look-up abstraction
    /// </summary>
    public abstract class TextureFilter
    {
        /// <summary/>
        public TextureFilter(BitmapSource texture)
        {
            height = texture.PixelHeight;
            width = texture.PixelWidth;

            // we are assuming BGRA 32 bit color - assert that here
            if (texture.Format != PixelFormats.Bgra32)
            {
                texture = new FormatConvertedBitmap(texture, PixelFormats.Bgra32, null, 0);
            }

            // extract pixel data, 4 bytes since one per channel ARGB
            pixels = new byte[width * height * 4];
            texture.CopyPixels(pixels, width * 4, 0);

            if (saveTextures)
            {
                originalTexture = texture;
                WriteTextureToFile(String.Format("TR_Texture_{0}.png", textureCount));
                textureCount++;
            }

            materialType = MaterialType.Diffuse;
        }

        /// <summary/>
        protected TextureFilter()
        {
            // we only want this to be called by overrides that do not need to store a pixel[]
        }

        /// <summary/>
        abstract public Color FilteredTextureLookup(Point uv);

        /// <summary/>
        virtual public Color FilteredErrorLookup(Point uvLow, Point uvHigh, Color computedColor)
        {
            // Mipmapping uses a recursive texture pyramid where each level is 1/4 of the previous one.
            // This makes it necessary to maintain a 1:1 width:height sample ratio for
            // error tolerance estimation since that is the area that the mipmapped sample was computed from.
            double uvWidth = uvHigh.X - uvLow.X;
            double uvHeight = uvHigh.Y - uvLow.Y;
            // Force 1:1 aspect ratio on the larger side
            if (uvWidth > uvHeight)
            {
                double vFix = (uvWidth - uvHeight) / 2.0;
                uvLow.Y -= vFix;
                uvHigh.Y += vFix;
            }
            else
            {
                double uFix = (uvHeight - uvWidth) / 2.0;
                uvLow.X -= uFix;
                uvHigh.X += uFix;
            }

            // Define where we want to have texel centers
            double texelCenterX = 0.5;
            double texelCenterY = 0.5;
            // -/+ texelCenters so that we always at least compare against the raw indices of bilinear filtering
            int xLo = (int)Math.Floor(uvLow.X * width - (1 - texelCenterX));
            int xHi = (int)Math.Ceiling(uvHigh.X * width + texelCenterX);
            int yLo = (int)Math.Floor(uvLow.Y * height - (1 - texelCenterY));
            int yHi = (int)Math.Ceiling(uvHigh.Y * height + texelCenterY);

            Color tolerance = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            for (int y = yLo; y <= yHi; y++)
            {
                for (int x = xLo; x <= xHi; x++)
                {
                    Color current = GetColor(x, y);
                    Color diff = ColorOperations.AbsoluteDifference(current, computedColor);
                    tolerance = ColorOperations.Max(tolerance, diff);
                }
            }

            return tolerance;
        }

        /// <summary/>
        public MaterialType MaterialType
        {
            get { return materialType; }
            set { materialType = value; }
        }

        /// <summary/>
        public bool HasErrorEstimation
        {
            get { return hasErrorEstimation; }
            set { hasErrorEstimation = value; }
        }

        private void WriteTextureToFile(string fileName)
        {
            if (originalTexture != null)
            {
                PhotoConverter.SaveImageAs(originalTexture, fileName);
            }
        }

        /// <summary/>
        public static bool SaveTextures
        {
            get { return saveTextures; }
            set { saveTextures = value; }
        }

        internal static TextureFilter[] CreateTextures(
                List<Material> materials,
                Point originalMinUV,
                Point originalMaxUV,
                Rect screenSpaceBounds)
        {
            TextureFilter[] textures = new TextureFilter[materials.Count];
            int materialCount = 0;
            foreach (Material material in materials)
            {
                if (material is DiffuseMaterial)
                {
                    textures[materialCount] = TextureGenerator.RenderBrushToTextureFilter(
                            ((DiffuseMaterial)material).Brush,
                            originalMinUV, originalMaxUV, screenSpaceBounds);
                    if (textures[materialCount] != null)
                    {
                        textures[materialCount].MaterialType = MaterialType.Diffuse;
                        textures[materialCount].MaterialColor = ((DiffuseMaterial)material).Color;
                        textures[materialCount].AmbientColor = ((DiffuseMaterial)material).AmbientColor;
                    }
                }
                else if (material is SpecularMaterial)
                {
                    textures[materialCount] = TextureGenerator.RenderBrushToTextureFilter(
                            ((SpecularMaterial)material).Brush,
                            originalMinUV, originalMaxUV, screenSpaceBounds);
                    if (textures[materialCount] != null)
                    {
                        textures[materialCount].MaterialType = MaterialType.Specular;
                        textures[materialCount].SpecularPower = ((SpecularMaterial)material).SpecularPower;
                        textures[materialCount].MaterialColor = ((SpecularMaterial)material).Color;
                    }
                }
                else if (material is EmissiveMaterial)
                {
                    textures[materialCount] = TextureGenerator.RenderBrushToTextureFilter(
                            ((EmissiveMaterial)material).Brush,
                            originalMinUV, originalMaxUV, screenSpaceBounds);
                    if (textures[materialCount] != null)
                    {
                        textures[materialCount].MaterialType = MaterialType.Emissive;
                        textures[materialCount].MaterialColor = ((EmissiveMaterial)material).Color;
                    }
                }
                materialCount++;
            }
            return textures;
        }

        /// <summary/>
        protected Color GetColor(int x, int y)
        {
            // TODO: investigate returning null Color instead of clamping

            // clamp to a safe value
            x = Clamp(x, width);
            y = Clamp(y, height);

            int offset = (x + (y * width)) * 4;

            // the format is BGRA ...
            return Color.FromArgb(
                pixels[offset + 3],  // A
                pixels[offset + 2],  // R
                pixels[offset + 1],  // G
                pixels[offset + 0]); // B
        }

        private int Clamp(int val, int max)
        {
            if (val >= max)
            {
                return max - 1;
            }

            if (val < 0)
            {
                return 0;
            }

            return val;
        }

        private bool hasErrorEstimation = false;
        private MaterialType materialType;
        /// <summary/>
        protected byte[] pixels;
        /// <summary/>
        protected int width;
        /// <summary/>
        protected int height;

        // Color Knobs for this texture
        /// <summary/>
        public Color AmbientColor;
        /// <summary/>
        public Color MaterialColor;

        /// <summary/>
        public double SpecularPower;

        /// <summary/>
        protected BitmapSource originalTexture = null;

        private static bool saveTextures = false;
        private static int textureCount = 0;
    };
}
