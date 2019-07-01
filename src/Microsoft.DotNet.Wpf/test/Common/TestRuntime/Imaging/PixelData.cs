// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Imaging
{
    #region Namespaces.

    using System;
    using System.Drawing;
    using System.Drawing.Imaging;

    #endregion Namespaces.

	/// <summary>
	/// Structure that maps to memory layout for Format24bppRgb.
	/// </summary>
	[System.Runtime.InteropServices.StructLayout(
         System.Runtime.InteropServices.LayoutKind.Sequential)]
	public struct PixelData
	{
        #region Public data.

		/// <summary>Blue component of pixel (0-255).</summary>
		public byte blue;
		/// <summary>Green component of pixel (0-255).</summary>
		public byte green;
		/// <summary>Red component of pixel (0-255).</summary>
		public byte red;

        #endregion Public data.

        /// <summary>Creates an initialized pixel data structure.</summary>
        /// <param name="red">Red component of color.</param>
        /// <param name="green">Green component of color.</param>
        /// <param name="blue">Blue component of color.</param>
        public PixelData(byte red, byte green, byte blue)
        {
            this.blue = blue;
            this.green = green;
            this.red = red;
        }
        
        /// <summary>Returns a regular object of Color type.</summary>
        /// <returns>The Color corresponding to the pixel data.</returns>
        public Color ToColor()
        {
            return Color.FromArgb(red, green, blue);
        }
        
        /// <summary>Calculates the number of bytes in a row for a Bitmap
        /// of a given size.</summary>
        /// <param name="size">Size of the Bitmap.</param>
        /// <returns>The number of bytes in a row.</returns>
        /// <remarks>This is an unsafe method; if you need a safe
        /// verison, use SafeGetScanLineWidth.</remarks>
        /// <example>The following sample shows how to print out all the
        /// pixels in a bitmap.<code>...
        /// public unsafe void ShowPixels(Bitmap b) {
        ///   BitmapData bitmapData = BitmapUtils.LockBitmapDataRead(b);
        ///   try {
        ///     PixelData* pixels = (PixelData*) bitmapData.Scan0;
        ///     Size s = b.Size;
        ///     int width = PixelData.GetScanLineWidth(s);
        ///     for (int y=0; y &lt; s.Height; y++)
        ///     {
        ///         pixels = (PixelData*)
        ///             ((byte*)bitmapData.Scan0.ToPointer() + y * width);
        ///         for (int x=0; x &lt; s.Width; x++)
        ///         {
        ///             System.Console.WriteLine(pixels-&gt;ToColor().ToString());
        ///             pixels++;
        ///         }
        ///     }
        ///   } finally {
        ///     b.UnlockBits(bitmapData);
        ///   }
        /// }
        /// </code></example>
        public static unsafe int GetScanLineWidth(Size size)
        {
            // Figure out the number of bytes in a row.
            // This is rounded up to be a multiple of 4 bytes, since a 
            // scan line in an image must always be a multiple of 4 bytes.
            int width = size.Width * sizeof(PixelData);
            if (width % 4 != 0)
            {
                width = 4 * (width / 4 + 1);
            }
            return width;
        }

        /// <summary>Calculates the number of bytes in a row for a Bitmap
        /// of a given size.</summary>
        /// <param name="size">Size of the Bitmap.</param>
        /// <returns>The number of bytes in a row.</returns>
        /// <remarks>This is a safe method; if you are in an unsafe context,
        /// GetScanLineWidth is more efficient.</remarks>
        public static int SafeGetScanLineWidth(Size size)
        {
            new System.Security.Permissions.SecurityPermission(
                System.Security.Permissions.PermissionState.Unrestricted)
                .Assert();
            return GetScanLineWidth(size);
        }

        /// <summary>Makes the pixel greyscale.</summary>
        /// <remarks>Note that the pixel itself is modified in-place.</remarks>
        public void ToGreyScale()
        {
            red = green = blue = (byte)
                ((byte) (red*0.21) + (byte) (green*0.72) + (byte) (blue*0.07));
        }

        /// <summary>Makes the pixel black or white.</summary>
        /// <param name="luminanceThreshold">A number between 0 and 255
        /// to set the threshold.</param>
        /// <remarks>Note that the pixel itself is modified in-place.</remarks>
        public void ToBlackWhite(byte luminanceThreshold)
        {
            byte value = (byte)
                ((byte) (red*0.21) + (byte) (green*0.72) + (byte) (blue*0.07));
            if (value < luminanceThreshold)
                red = green = blue = 0;
            else
                red = green = blue = 255;
        }
	}
}
