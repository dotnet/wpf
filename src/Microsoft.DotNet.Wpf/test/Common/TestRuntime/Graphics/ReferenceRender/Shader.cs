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
    /// Abstracts the rasterization process
    /// </summary>
    internal abstract class Shader
    {
        public Shader(Triangle[] triangles, RenderBuffer buffer)
        {
            this.triangles = triangles;
            this.buffer = buffer;

            if (this.buffer == null)
            {
                throw new ApplicationException("Shader needs a valid RenderBuffer as input.");
            }

            // Shader always Z-writes by default
            this.buffer.WriteToZBuffer = true;
        }

        /// <summary>
        /// Rasterizes the scene to the given final size
        /// </summary>
        virtual public void Rasterize(Rect viewport)
        {
            this.viewport = viewport;

            foreach (Triangle t in triangles)
            {
                // Don't bother rendering pixels outside our viewport
                if (Rect.Intersect(t.Bounds, viewport).IsEmpty)
                {
                    continue;
                }
                ProcessTriangle(t);
            }
        }

        /// <summary>
        /// Dictate the behavior of the rasterization of each triangle.
        /// </summary>
        virtual protected void ProcessTriangle(Triangle triangle)
        {
            // Compute vertex programs on each vertex
            ComputeVertexProgram(triangle.vertex1);
            ComputeVertexProgram(triangle.vertex2);
            ComputeVertexProgram(triangle.vertex3);

            // Create anything the triangle needs for interpolation
            TriangleInterpolator ti = triangle.TriangleInterpolator;
            Rect bounds = GetRenderingBounds(triangle.Bounds);

            // Interpolate across all pixels that this triangle potentially covers
            for (double y = bounds.Top + pixelCenterX; y < bounds.Bottom; y += 1.0)
            {
                for (double x = bounds.Left + pixelCenterY; x < bounds.Right; x += 1.0)
                {
                    // If the point is inside this triangle
                    if (ti.Contains(x, y))
                    {
                        // Interpolate vertex data for this pixel
                        Vertex v = ti.GetVertex(x, y);

                        // Check for a valid projection
                        if (double.IsNaN(v.ProjectedPosition.Z))
                        {
                            // NaN z-value means this vertex is degenerate, so skip it.
                            continue;
                        }

                        // only do this step if we need to perform trilinear filtering
                        if (needsMipMapCoefficient)
                        {
                            // find the projected extents of the pixel in UV Space
                            Point uvTL = ti.GetTextureCoordinates(x - pixelCenterX, y - pixelCenterY);
                            Point uvTR = ti.GetTextureCoordinates(x + (1 - pixelCenterX), y - pixelCenterY);
                            Point uvBL = ti.GetTextureCoordinates(x - pixelCenterX, y + (1 - pixelCenterY));
                            Point uvBR = ti.GetTextureCoordinates(x + (1 - pixelCenterX), y + (1 - pixelCenterY));

                            // Bound this
                            Point uvMin = MathEx.Min(uvTL, uvTR, uvBL, uvBR);
                            Point uvMax = MathEx.Max(uvTL, uvTR, uvBL, uvBR);
                            Vector uvDiff = uvMax - uvMin;
                            v.MipMapFactor = Math.Max(uvDiff.X, uvDiff.Y);
                        }

                        // only do this step if required by the derived shader
                        if (needsUVTolerance)
                        {
                            double texTol = RenderTolerance.TextureLookUpTolerance;
                            //Crank up texture tolerance in irregular DPI
                            if (!RenderTolerance.IsSquare96Dpi)
                            {
                                texTol *= 2;
                            }

                            Point uvA = ti.GetTextureCoordinates(x - texTol, y - texTol);
                            Point uvB = ti.GetTextureCoordinates(x + texTol, y - texTol);
                            Point uvC = ti.GetTextureCoordinates(x - texTol, y + texTol);
                            Point uvD = ti.GetTextureCoordinates(x + texTol, y + texTol);

                            // the UV values are not necessarily screen aligned
                            v.UVToleranceMin = MathEx.Min(uvA, uvB, uvC, uvD);
                            v.UVToleranceMax = MathEx.Max(uvA, uvB, uvC, uvD);
                        }

                        // Pass interpolated data down to pixel program
                        ComputePixelProgram(v);
                    }
                }
            }
        }

        protected Rect GetRenderingBounds(Rect triangleBounds)
        {
            Rect bounds = Rect.Intersect(triangleBounds, viewport);

            return MathEx.InflateToIntegerBounds(bounds);
        }

        /// <summary>
        /// Dictate the behavior of the shader on a per-vertex basis.
        /// </summary>
        /// <param name="v">Original input vertex from mesh</param>
        virtual protected void ComputeVertexProgram(Vertex v)
        {
        }

        /// <summary>
        /// Dictate the behavior of the shader on a per-pixel basis.
        /// </summary>
        /// <param name="v">Interpolated output from the Vertex program</param>
        virtual protected void ComputePixelProgram(Vertex v)
        {
            // Move color and tolerance to premultiplied space
            Color color = ColorOperations.PreMultiplyColor(v.Color);
            Color tolerance = ColorOperations.PreMultiplyTolerance(v.ColorTolerance, v.Color.A);

            // Send the pixel to be rendered
            buffer.SetPixel(
                (int)Math.Floor(v.ProjectedPosition.X),
                (int)Math.Floor(v.ProjectedPosition.Y),
                (float)v.ProjectedPosition.Z,
                color,
                tolerance
                );
        }

        protected Triangle[] triangles;
        protected RenderBuffer buffer;
        protected Rect viewport;
        protected bool needsUVTolerance = false;
        protected bool needsMipMapCoefficient = false;

        protected const double pixelCenterX = 0.5;
        protected const double pixelCenterY = 0.5;
        protected static Color emptyColor = Color.FromArgb(0, 0, 0, 0);
    }
}
