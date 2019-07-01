// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

using Microsoft.Test.Graphics.TestTypes;

namespace Microsoft.Test.Graphics
{
    /// <summary>
    /// Static helper class that holds color utilities
    /// </summary>
    public class ColorOperations
    {
        private ColorOperations()
        {
            // Helper function container class, so prevent instantiation.
        }

        /// <summary>
        /// Converts a byte value (0x00 .. 0xff) to a double value (0.0 .. 1.0)
        /// </summary>
        /// <param name="b">Byte value</param>
        /// <returns>A double in range 0.0 - 1.0</returns>
        public static double ByteToDouble(byte b)
        {
            return ((double)b) / 255.0;
        }

        /// <summary>
        /// Converts a double in range 0.0 - 1.0 to a byte in range 0x00 - 0xff
        /// </summary>
        /// <param name="d">Double value, gets clamped to 0.0 - 1.0 range</param>
        /// <returns>A byte 0x00 maps to 0.0, 0xff maps to 1.0</returns>
        public static byte DoubleToByte(double d)
        {
            // Clamp to [0..1] range
            d = Math.Max(0.0, d);
            d = Math.Min(1.0, d);

            // We add 0.5 so that we actually get rounding, not truncation
            // Without this, d = 0.999 would still be 254.
            return (byte)((d * 255.0) + 0.5);
        }

        /// <summary/>
        public static Color[,] ToColorArray(BitmapSource bitmap)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // we are assuming BGRA 32 bit color - assert that here
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                bitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }

            byte[] bytes = new byte[width * height * 4];
            bitmap.CopyPixels(bytes, width * 4, 0);

            return ToColorArray(bytes, width, height);
        }

        /// <summary/>
        public static Color[,] ToColorArray(byte[] bytes, int width, int height)
        {
            System.Diagnostics.Debug.Assert(width * height * 4 == bytes.Length, "incorrect number of bytes found");

            Color[,] colors = new Color[width, height];
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * (4 * width);
                for (int x = 0; x < width; x++)
                {
                    int offset = rowOffset + (4 * x);
                    colors[x, y] = Color.FromArgb(
                                            bytes[offset + 3],  // A
                                            bytes[offset + 2],  // R
                                            bytes[offset + 1],  // G
                                            bytes[offset]);  // B
                }
            }
            return colors;
        }

        /// <summary>
        /// Creates a Color from double values in the range 0.0 - 1.0
        /// </summary>
        /// <param name="a">Alpha</param>
        /// <param name="r">Red</param>
        /// <param name="g">Green</param>
        /// <param name="b">Blue</param>
        /// <returns>Newly built color</returns>
        public static Color ColorFromArgb(double a, double r, double g, double b)
        {
            return Color.FromArgb(DoubleToByte(a), DoubleToByte(r), DoubleToByte(g), DoubleToByte(b));
        }

        /// <summary>
        /// Adds two color values to saturation - i.e. if 0xff + 0x01 = 0xff. No overflow occurs.
        /// </summary>
        /// <param name="color1">one color</param>
        /// <param name="color2">another color</param>
        /// <returns>Combined color.</returns>
        public static Color Add(Color color1, Color color2)
        {
            return Color.FromArgb(
                    ClampedByteAdd(color1.A, color2.A),
                    ClampedByteAdd(color1.R, color2.R),
                    ClampedByteAdd(color1.G, color2.G),
                    ClampedByteAdd(color1.B, color2.B));
        }

        /// <summary>
        /// Single channel byte addition, clamped at 0xff
        /// </summary>
        public static byte ClampedByteAdd(byte b1, byte b2)
        {
            // We expect overflow from byte addition, so we store in an int
            return (byte)Math.Min((int)b1 + (int)b2, 255);
        }

        /// <summary>
        /// Multiplies one color value by another, component by component.
        /// </summary>
        /// <param name="color1">one color</param>
        /// <param name="color2">another color</param>
        /// <returns>C1 * C2</returns>
        public static Color Modulate(Color color1, Color color2)
        {
            return ColorFromArgb(
                ByteToDouble(color1.A) * ByteToDouble(color2.A),
                ByteToDouble(color1.R) * ByteToDouble(color2.R),
                ByteToDouble(color1.G) * ByteToDouble(color2.G),
                ByteToDouble(color1.B) * ByteToDouble(color2.B));
        }

        /// <summary>
        /// Multiplies RGB color components by given scale factor.
        /// ALPHA component is forced to fully opaque ( 1.0 ).
        /// </summary>
        /// <param name="color">source color</param>
        /// <param name="scaleFactor">scaling factor</param>
        /// <returns></returns>
        public static Color ScaleOpaque(Color color, double scaleFactor)
        {
            return ColorFromArgb(
                1.0, // alpha channel is always opaque
                scaleFactor * ByteToDouble(color.R),
                scaleFactor * ByteToDouble(color.G),
                scaleFactor * ByteToDouble(color.B));
        }

        /// <summary>
        /// Scale the alpha channel of a Color by the specified factor.
        /// Not to be used on Premultiplied colors.
        /// </summary>
        public static Color ScaleAlpha(Color color, double scaleFactor)
        {
            color.A = DoubleToByte(scaleFactor * ByteToDouble(color.A));
            return color;
        }

        /// <summary>
        /// Computes Abs( original - subtraction ) for each color channel.
        /// </summary>
        /// <param name="original">one color</param>
        /// <param name="subtraction">another color</param>
        /// <returns>Maximum absolute difference per channel</returns>
        public static Color AbsoluteDifference(Color original, Color subtraction)
        {
            int a = (int)original.A - (int)subtraction.A;
            int r = (int)original.R - (int)subtraction.R;
            int g = (int)original.G - (int)subtraction.G;
            int b = (int)original.B - (int)subtraction.B;

            return Color.FromArgb(
                        (byte)Math.Abs(a),
                        (byte)Math.Abs(r),
                        (byte)Math.Abs(g),
                        (byte)Math.Abs(b));
        }

        /// <summary>
        /// Mixes two colors by a weight factor:
        /// foreground * foregroundWeight + background * (1.0 - foregroundWeight).
        /// </summary>
        /// <param name="foreground">foreground color</param>
        /// <param name="background">background color</param>
        /// <param name="foregroundWeight">percent of foreground, in 0.0 - 1.0 range</param>
        /// <returns>Weighted mix of the two colors</returns>
        public static Color Blend(Color foreground, Color background, double foregroundWeight)
        {
            double backgroundWeight = 1.0 - foregroundWeight;
            return ColorFromArgb(
                ByteToDouble(foreground.A) * foregroundWeight + backgroundWeight * ByteToDouble(background.A),
                ByteToDouble(foreground.R) * foregroundWeight + backgroundWeight * ByteToDouble(background.R),
                ByteToDouble(foreground.G) * foregroundWeight + backgroundWeight * ByteToDouble(background.G),
                ByteToDouble(foreground.B) * foregroundWeight + backgroundWeight * ByteToDouble(background.B));
        }

        /// <summary>
        /// Mixes two colors by a weight factor:
        /// foreground * foregroundWeight + background * (1.0 - foregroundWeight).
        /// If one Color is null, return the other color (weight factor will be ignored in this case).
        /// </summary>
        public static Color? Blend(Color? foreground, Color? background, double foregroundWeight)
        {
            if (!foreground.HasValue)
            {
                return background;
            }
            if (!background.HasValue)
            {
                return foreground;
            }
            return Blend(foreground.Value, background.Value, foregroundWeight);
        }

        /// <summary>
        /// Scales RGB channels by alpha channel amount.
        /// Final A value will be be preserved (source over).
        /// </summary>
        /// <param name="original">non premultiplied color</param>
        /// <returns>Premultiplied alpha color</returns>
        public static Color PreMultiplyColor(Color original)
        {
            // See equations 3 & 4 in PreMultipliedAlphaBlend ...

            double weight = ByteToDouble(original.A);
            return ColorFromArgb(
                weight,
                ByteToDouble(original.R) * weight,
                ByteToDouble(original.G) * weight,
                ByteToDouble(original.B) * weight);
        }

        /// <summary>
        /// Mixes two colors using the foreground's alpha value as a blend factor.
        /// This version asumes that foreground has premultiplied alpha values.
        /// </summary>
        /// <param name="foreground">foreground color - must be premultiplied.</param>
        /// <param name="background">background color </param>
        /// <returns>Weighted mix of the two colors</returns>
        public static Color PreMultipliedAlphaBlend(Color foreground, Color background)
        {
            // Notes on pre-multiplied color space.
            //
            // Regular alpha blend uses the following:
            //
            //    Cf = foreground color in ARGB where each of ARGB is in range [0.0-1.0]
            //    Cb = background color in ARGB where each of ARGB is in range [0.0-1.0]
            //
            // 1. result.RGB  =  Cf.A * Cf.RGB + ( 1 - Cf.A ) * Cb.RGB
            // 2. result.A    =  Cf.A          + ( 1 - Cf.A ) * Cb.A
            //
            // Pre-multiplied alpha blend needs Cf to be pre-multiplied by alpha.
            // This pre-multiplication step works differently for RGB channels than for A channel.
            // Also, depending on the value stored in the A channel we can use this blend to achieve
            // source over alpha blend and additive blend.
            //
            // We create Cf_pm in two steps:
            //
            // 3. Cf_pm.RGB   =  Cf.RGB * Cf.A                          -- RGB channels always get multiplied by A
            // 4. Cf_pm.A     =  Cf.A | 0                               -- A channel defines the blend mode
            //
            // The pre-multiplied alpha blend equation asumes that Cf is pre-multiplied:
            //
            // 5. result.RGB  =  Cf_pm.RGB + ( 1 - Cf_pm.A ) * Cb.RGB
            // 6. result.A    =  Cf_pm.A   + ( 1 - Cf_pm.A ) * Cb.A     -- same as non pre-multiplied version.
            //
            // For "source over" blends we keep the original A value.
            // If we substitute this (3,4) into the pre-multiplied blend equation (5) we get the non
            // pre-multiplied version (1):
            //
            //    Cf_pm.RGB   =  Cf.RGB * Cf.A                          -- from (3)
            //    Cf_pm.A     =  Cf.A                                   -- from (4)
            //    result.RGB  =  Cf_pm.RGB + ( 1 - Cf_pm.A ) * Cb.RGB   -- from (5)
            //    result.A    =  Cf_pm.A   + ( 1 - Cf_pm.A ) * Cb.A     -- from (6)
            //
            //    result.RGB  =  Cf.A * Cf.RGB + ( 1 - Cf.A ) * Cb.RGB  -- simplified, it becomes equation (1)
            //    result.A    =  Cf.A          + ( 1 - Cf.A ) * Cb.A    -- simplified, it becomes equation (2)
            //
            // For "additive" blends we set A to zero.
            // Substituting this into the pre-multiplied blend equation (5):
            //
            //    Cf_pm.RGB   = Cf.RGB * Cf.A                           -- from (3)
            //    Cf_pm.A     = 0                                       -- from (4)
            //    result.RGB  = Cf_pm.RGB + ( 1 - Cf_pm.A ) * Cb.RGB    -- from (5)
            //    result.A    = Cf_pm.A   + ( 1 - Cf_pm.A ) * Cb.A      -- from (6)
            //
            //    result.RGB  = Cf.A * Cf.RGB + Cb.RGB                  -- simplified
            //    result.A    = Cb.A
            //
            // This is additive: we add to RGB and A stays as it was.  How much to add gets scaled by
            // the foreground's A channel.
            //
            // By always using pre-multiplied blending, we can do the right thing in all scenarios.

            double backgroundWeight = 1.0 - ByteToDouble(foreground.A);
            return ColorFromArgb(
                ByteToDouble(foreground.A) + backgroundWeight * ByteToDouble(background.A),
                ByteToDouble(foreground.R) + backgroundWeight * ByteToDouble(background.R),
                ByteToDouble(foreground.G) + backgroundWeight * ByteToDouble(background.G),
                ByteToDouble(foreground.B) + backgroundWeight * ByteToDouble(background.B));
        }

        /// <summary>
        /// Scales a premultiplied color by the given opacity.
        /// </summary>
        public static Color PreMultipliedOpacityScale(Color premultipliedColor, double opacity)
        {
            return ColorFromArgb(
                ByteToDouble(premultipliedColor.A) * opacity,
                ByteToDouble(premultipliedColor.R) * opacity,
                ByteToDouble(premultipliedColor.G) * opacity,
                ByteToDouble(premultipliedColor.B) * opacity);
        }

        /// <summary>
        /// Scales RGB tolerance channels by the alpha value of the expected pixel.
        /// </summary>
        public static Color PreMultiplyTolerance(Color tolerance, byte expectedAlpha)
        {
            // Notes on pre-multiplied color space as applied to tolerance buffer values.
            //
            // Rule: Tolerance and Color values should blend the same way.
            // This means that when moving to pre-multiplied color space, we need to substitute
            // the current alpha value with the peer Color value to do the pre-multiplication.
            //
            // Issue: Dealing with alpha channel tolerance.
            //  Alpha channel tolerance means that we are not certain of how transparent our peer color
            //  pixel is.  If we get our transparency wrong, then our color values will be potentially wrong by
            //  that amount since they would have been blended less or more with a previous layer.
            //
            // Solution: When blending, we add the foreground tolerance's alpha value into each RGB channel.
            //  See PreMultipliedToleranceBlend for details.

            // TODO: 53605 - rounding issues.

            // Use the peer color value's alpha as blend weight
            double weight = ByteToDouble(expectedAlpha);
            return ColorFromArgb(
                ByteToDouble(tolerance.A),
                ByteToDouble(tolerance.R) * weight,
                ByteToDouble(tolerance.G) * weight,
                ByteToDouble(tolerance.B) * weight);
        }

        /// <summary>
        /// Mix two tolerance values using the alpha value of the pixel this tolerance describes.
        /// Blending is the the same as the premultiplied alpha blend with the exception of using
        ///  the described pixel's alpha as the foreground weight.
        /// The foreground's alpha value is also added to each of the RGB channels of the result.
        /// The resulting color's alpha value is always 0.
        /// </summary>
        public static Color PreMultipliedToleranceBlend(Color foreground, Color background, byte expectedAlpha)
        {
            // TODO: 53605 - rounding issues.
            double backgroundWeight = 1.0 - ByteToDouble(expectedAlpha);
            double alphaTolerance = ByteToDouble(foreground.A);   // Background already has its alpha tolerance factored in.

            return ColorFromArgb(
                    0.0,
                    ByteToDouble(foreground.R) + backgroundWeight * ByteToDouble(background.R) + alphaTolerance,
                    ByteToDouble(foreground.G) + backgroundWeight * ByteToDouble(background.G) + alphaTolerance,
                    ByteToDouble(foreground.B) + backgroundWeight * ByteToDouble(background.B) + alphaTolerance);
        }
        /// <summary>
        /// Max implementation for Color
        /// </summary>
        /// <param name="one">one color</param>
        /// <param name="two">other color</param>
        /// <returns>Component-wise maximum</returns>
        public static Color Max(Color one, Color two)
        {
            return Color.FromArgb(
                Math.Max(one.A, two.A),
                Math.Max(one.R, two.R),
                Math.Max(one.G, two.G),
                Math.Max(one.B, two.B));
        }

        /// <summary/>
        public static void DiscoverBitDepth(Point windowPosition)
        {
            IntPtr hdc = Interop.GetDC(IntPtr.Zero);
            bitDepth = Interop.GetDeviceCaps(hdc, Interop.BitsPerPixel);
            Interop.ReleaseDC(IntPtr.Zero, hdc);
        }

        /// <summary/>
        public static int BitDepth
        {
            get
            {
                if (bitDepth < 0)
                {
                    DiscoverBitDepth(new Point());
                }
                return bitDepth;
            }
        }

        /// <summary/>
        public static Color ConvertFrom32BitTo16Bit(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;

            // I'm not quite sure why Avalon subtracts 1,
            //  but it makes our conversion pixel perfect with theirs.
            if (r != 0)
            {
                r--;
            }
            if (g != 0)
            {
                g--;
            }
            if (b != 0)
            {
                b--;
            }
            r &= 0xf8;
            g &= 0xfc;
            b &= 0xf8;

            // Because we store a 5-6 bit channel in an 8 bit channel,
            //  we interpolate the ignored low bits from 7 -> 0 (5 bit) or 3 -> 0 (6 bit)
            //  to provide a more tolerance friendly conversion.
            r |= (byte)(r >> 5);
            g |= (byte)(g >> 6);
            b |= (byte)(b >> 5);

            // Transparency is lost
            return Color.FromArgb(255, r, g, b);
        }

        /// <summary/>
        public static Color ConvertToleranceFrom32BitTo16Bit(Color tolerance)
        {
            // Because we do the comparison in 32-bit, a tolerance of 2 for a given channel doesn't mean much.
            // Therefore, we will multiply the 32-bit tolerance by the amount that we have to round up.
            //      i.e.
            //          2 -> 2*7 == 14  for the R,B channels
            //          2 -> 2*3 == 6   for the G channel
            //
            byte r = (byte)Math.Min((int)tolerance.R * 7, 255);
            byte g = (byte)Math.Min((int)tolerance.G * 3, 255);
            byte b = (byte)Math.Min((int)tolerance.B * 7, 255);
            return Color.FromArgb(0x00, r, g, b);
        }

        /// <summary>
        /// Performs a tolerance test
        /// </summary>
        /// <param name="expected">first color</param>
        /// <param name="actual">second color</param>
        /// <param name="tolerance">maximum absolute difference allowed</param>
        /// <returns></returns>
        public static bool AreWithinTolerance(Color expected, Color actual, Color tolerance)
        {
            int a = (int)expected.A - (int)actual.A;
            int r = (int)expected.R - (int)actual.R;
            int g = (int)expected.G - (int)actual.G;
            int b = (int)expected.B - (int)actual.B;

            return (Math.Abs(a) <= (int)tolerance.A &&
                     Math.Abs(r) <= (int)tolerance.R &&
                     Math.Abs(g) <= (int)tolerance.G &&
                     Math.Abs(b) <= (int)tolerance.B);
        }

        private static int bitDepth = -1;
    }
}
