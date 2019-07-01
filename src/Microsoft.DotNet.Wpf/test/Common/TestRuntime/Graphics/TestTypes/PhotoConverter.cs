// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;

using FileMode = System.IO.FileMode;
using AvalonColor = System.Windows.Media.Color;     // Resolving ambiguous class
using DrawingColor = System.Drawing.Color;          // Resolving ambiguous class
using Microsoft.Test.Logging;

#if !STANDALONE_BUILD
using TrustedFileStream = Microsoft.Test.Security.Wrappers.FileStreamSW;
using TrustedStreamWriter = Microsoft.Test.Security.Wrappers.StreamWriterSW;
#else
using TrustedFileStream = System.IO.FileStream;
using TrustedStreamWriter = System.IO.StreamWriter;
#endif

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Tools for converting and saving images
    /// </summary>
    public class PhotoConverter
    {
        /// <summary/>
        public static AvalonColor[,] ToColorArray(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            AvalonColor[,] colors = new AvalonColor[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    DrawingColor c = bitmap.GetPixel(x, y);
                    colors[x, y] = AvalonColor.FromRgb(c.R, c.G, c.B);
                }
            }

            return colors;
        }

        /// <summary/>
        public static AvalonColor[,] ToColorArray(bool[,] bitmap, AvalonColor background, AvalonColor foreground)
        {
            int width = bitmap.GetLength(0);
            int height = bitmap.GetLength(1);

            AvalonColor[,] colors = new AvalonColor[width, height];

            for (int y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    colors[x, y] = (bitmap[x, y]) ? foreground : background;
                }
            }

            return colors;
        }

        /// <summary/>
        public static AvalonColor[,] ToColorArray(float[,] intensityMap)
        {
            int width = intensityMap.GetLength(0);
            int height = intensityMap.GetLength(1);

            AvalonColor[,] colors = new AvalonColor[width, height];

            for (int y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    // We multiply by 255 and add 0.5 so we get the rounded conversion to byte
                    byte b = (byte)(intensityMap[x, y] * 255.0 + 0.5);
                    colors[x, y] = AvalonColor.FromRgb(b, b, b);
                }
            }

            return colors;
        }

        /// <summary>
        /// Converts an Avalon Color array to a BitmapImage.
        /// Preserves Alpha channel information.
        /// </summary>
        /// <param name="bitmap">Color bits</param>
        /// <returns>A BitmapSource object with those bits</returns>
        public static BitmapSource ToImageData(AvalonColor[,] bitmap)
        {
            return ToImageData(bitmap, true);
        }

        /// <summary>
        /// Converts an Avalon Color array to a BitmapImage.
        /// </summary>
        /// <param name="bitmap">Color bits</param>
        /// <param name="preserveAlpha">TRUE for using the original alpha channel,
        /// FALSE for forcing it to 0xff</param>
        /// <returns>A BitmapSource object with those bits</returns>
        public static BitmapSource ToImageData(AvalonColor[,] bitmap, bool preserveAlpha)
        {
            int width = bitmap.GetLength(0);
            int height = bitmap.GetLength(1);

            uint[] bits = new uint[width * height];

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    bits[x + rowOffset] =
                            (uint)(((preserveAlpha) ? bitmap[x, y].A : (byte)0xff) << 24 |
                                    bitmap[x, y].R << 16 |
                                    bitmap[x, y].G << 8 |
                                    bitmap[x, y].B);
                }
            }

            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, bits, width * 4);
        }

        /// <summary>
        /// Saves the Color array to a .PNG file.  Forces ALPHA to 0xFF.
        /// </summary>
        /// <param name="bitmap">Bit matrix to save</param>
        /// <param name="filename">.PNG file name to save it as.</param>
        public static void SaveImageAs(AvalonColor[,] bitmap, string filename)
        {
            SaveImageAs(bitmap, filename, true);
        }

        /// <summary>
        /// Saves the Color array to a .PNG file.
        /// </summary>
        /// <param name="bitmap">Bit matrix to save</param>
        /// <param name="filename">.PNG file name to save it as.</param>
        /// <param name="preserveAlpha">Set to TRUE for keeping Alpha channel information on the file.</param>
        public static void SaveImageAs(AvalonColor[,] bitmap, string filename, bool preserveAlpha)
        {
            SaveImageAs(ToImageData(bitmap, preserveAlpha), filename);
        }

        /// <summary>
        /// Saves the Color array to a .PNG file.
        /// </summary>
        /// <param name="bitmap">Bit matrix to save</param>
        /// <param name="filename">.PNG file name to save it as.</param>
        public static void SaveImageAs(BitmapSource bitmap, string filename)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (TrustedFileStream fs = new TrustedFileStream(filename, FileMode.Create))
            {
                encoder.Save(PT.Untrust(fs));
            }
            GlobalLog.LogFile(filename);
        }
    }
}

