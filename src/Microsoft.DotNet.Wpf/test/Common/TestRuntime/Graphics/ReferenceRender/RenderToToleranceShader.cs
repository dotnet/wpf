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
    /// Ignore shader. Adds the rendered color to an existing tolerance buffer.
    /// </summary>
    internal class RenderToToleranceShader : Shader
    {
        public RenderToToleranceShader(Triangle[] triangles, RenderBuffer buffer)
            :
            base(triangles, buffer)
        {
        }

        override protected void ComputePixelProgram(Vertex v)
        {
            // We are only interested in the tolerance here
            int x = (int)Math.Floor(v.ProjectedPosition.X);
            int y = (int)Math.Floor(v.ProjectedPosition.Y);
            buffer.AddToTolerance(x, y, v.Color);

            // But we also want to set the z-values for rendering on empty space
            if (x >= 0 && x < buffer.Width && y >= 0 && y < buffer.Height)
            {
                if (buffer.ZBuffer[x, y] == buffer.ZBufferClearValue)
                {
                    buffer.ZBuffer[x, y] = (float)v.ProjectedPosition.Z;
                }
            }
        }
    }

}
