// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// Bi-Linear texturing lookup
    /// </summary>
    internal class BilinearTextureFilter : TextureFilter
    {
        public BilinearTextureFilter(BitmapSource texture)
            : base(texture)
        {
        }

        public override Color FilteredTextureLookup(Point uv)
        {
            double x = uv.X * width;
            double y = uv.Y * height;

            // Define where we want to have texel centers
            double texelCenterX = 0.5;
            double texelCenterY = 0.5;

            // Compute integer array indices of texel above and to the left of (x,y)
            int topLeftX = (int)Math.Floor(x - (1 - texelCenterX));
            int topLeftY = (int)Math.Floor(y - (1 - texelCenterY));

            // Get colors of 4 nearest neighbours
            Color ctl = GetColor(topLeftX, topLeftY);
            Color ctr = GetColor(topLeftX + 1, topLeftY);
            Color cbl = GetColor(topLeftX, topLeftY + 1);
            Color cbr = GetColor(topLeftX + 1, topLeftY + 1);

            // Shift (x,y) to align it with array indices
            x -= texelCenterX;
            y -= texelCenterY;

            // Compute position within our texel
            double dx = x - Math.Floor(x);
            double dy = y - Math.Floor(y);

            // Argh!
            double ctlA = ColorOperations.ByteToDouble(ctl.A);
            double ctlR = ColorOperations.ByteToDouble(ctl.R);
            double ctlG = ColorOperations.ByteToDouble(ctl.G);
            double ctlB = ColorOperations.ByteToDouble(ctl.B);

            double cblA = ColorOperations.ByteToDouble(cbl.A);
            double cblR = ColorOperations.ByteToDouble(cbl.R);
            double cblG = ColorOperations.ByteToDouble(cbl.G);
            double cblB = ColorOperations.ByteToDouble(cbl.B);

            double ctrA = ColorOperations.ByteToDouble(ctr.A);
            double ctrR = ColorOperations.ByteToDouble(ctr.R);
            double ctrG = ColorOperations.ByteToDouble(ctr.G);
            double ctrB = ColorOperations.ByteToDouble(ctr.B);

            double cbrA = ColorOperations.ByteToDouble(cbr.A);
            double cbrR = ColorOperations.ByteToDouble(cbr.R);
            double cbrG = ColorOperations.ByteToDouble(cbr.G);
            double cbrB = ColorOperations.ByteToDouble(cbr.B);

            // Precompute values for perf reasons
            double ddx = 1 - dx;
            double ddy = 1 - dy;
            double k1 = ddx * ddy;
            double k2 = dx * ddy;
            double k3 = ddx * dy;
            double k4 = dx * dy;

            // Perform interpolation, store result in a, r, g, and b
            double a = k1 * ctlA + k2 * ctrA + k3 * cblA + k4 * cbrA;
            double r = k1 * ctlR + k2 * ctrR + k3 * cblR + k4 * cbrR;
            double g = k1 * ctlG + k2 * ctrG + k3 * cblG + k4 * cbrG;
            double b = k1 * ctlB + k2 * ctrB + k3 * cblB + k4 * cbrB;

            return ColorOperations.ColorFromArgb(a, r, g, b);
        }
    }
}
