// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary>
    /// High dynamic range Color uses a double per color channel.
    /// The colors used the range [0-1] when converted to one byte per channel,
    /// with any value beyond 1 being clamped.
    /// </summary>
    internal class HdrColor
    {
        public HdrColor()
        {
            a = r = g = b = 0;
        }

        public HdrColor(Color color)
        {
            a = ColorOperations.ByteToDouble(color.A);
            r = ColorOperations.ByteToDouble(color.R);
            g = ColorOperations.ByteToDouble(color.G);
            b = ColorOperations.ByteToDouble(color.B);
        }

        private HdrColor(HdrColor copy)
        {
            this.a = copy.a;
            this.r = copy.r;
            this.g = copy.g;
            this.b = copy.b;
        }

        public double A
        {
            get { return a; }
            set { a = value; }
        }

        /// <summary>
        /// This value clamps any channel value beyond 1.0 to 1.0
        /// when converting back to a Color
        /// </summary>
        public Color ClampedValue
        {
            get
            {
                // This forces the values to be clamped to 0..1 range
                return ColorOperations.ColorFromArgb(a, r, g, b);
            }
        }

        /// <summary>
        /// This value clamps any channel value beyond 1.0 to 1.0
        /// when converting back to a Color.  This forces Alpha to 1.0.
        /// </summary>
        public Color ClampedValueNoAlpha
        {
            get
            {
                // This forces the values to be clamped to 0..1 range
                return ColorOperations.ColorFromArgb(1.0, r, g, b);
            }
        }

        public static HdrColor operator +(HdrColor left, HdrColor right)
        {
            HdrColor answer = new HdrColor(left);
            answer.a += right.a;
            answer.r += right.r;
            answer.g += right.g;
            answer.b += right.b;
            return answer;
        }

        public static HdrColor operator *(HdrColor left, HdrColor right)
        {
            HdrColor answer = new HdrColor(left);
            answer.a *= right.a;
            answer.r *= right.r;
            answer.g *= right.g;
            answer.b *= right.b;
            return answer;
        }

        public static HdrColor operator *(HdrColor left, double scale)
        {
            HdrColor answer = new HdrColor(left);
            answer.a *= scale;
            answer.r *= scale;
            answer.g *= scale;
            answer.b *= scale;
            return answer;
        }

        public static implicit operator HdrColor(Color c)
        {
            return new HdrColor(c);
        }

        public override int GetHashCode()
        {
            return a.GetHashCode()
                    ^ r.GetHashCode()
                    ^ g.GetHashCode()
                    ^ b.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("[{0},{1},{2},{3}]", a, r, g, b);
        }


        private double a;
        private double r;
        private double g;
        private double b;
    }

}