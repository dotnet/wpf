// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Represents colors channels as a set of 0-1 floats for higher fidelity internal color processing.
    /// </summary>
    internal struct HighFidelityColor
    {
        internal HighFidelityColor(Color color)
        {
            A = (float)color.A / 255;
            R = (float)color.R / 255;
            G = (float)color.G / 255;
            B = (float)color.B / 255;
        }

        internal Color ToColor()
        {
            return Color.FromArgb((int)(A * 255), (int)(R * 255), (int)(G * 255), (int)(B * 255));
        }

        public static HighFidelityColor operator +(HighFidelityColor color1, HighFidelityColor color2)
        {
            HighFidelityColor result;
            result.A = color1.A + color2.A;
            result.R = color1.R + color2.R;
            result.G = color1.G + color2.G;
            result.B = color1.B + color2.B;
            return result;
        }


        internal HighFidelityColor Modulate(float scale)
        {
            A *= scale;
            R *= scale;
            G *= scale;
            B *= scale;
            return this;
        }

        internal float A;
        internal float R;
        internal float G;
        internal float B;
    }
}