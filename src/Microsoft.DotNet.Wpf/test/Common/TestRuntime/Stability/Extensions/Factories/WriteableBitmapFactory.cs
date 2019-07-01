// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;
using Microsoft.Test.Stability.Extensions.Utilities;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(WriteableBitmap))]
    class WriteableBitmapGeneratedFactory : DiscoverableFactory<WriteableBitmap>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double DpiX { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double DpiY { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat PixelFormat { get; set; }

        public override WriteableBitmap Create(DeterministicRandom random)
        {
            int width = random.Next(400) + 1;
            int height = random.Next(400) + 1;

            WriteableBitmap wbmp = new WriteableBitmap(width, height, DpiX, DpiY, PixelFormat, GetPalette(PixelFormat, random));
            WriteableBitmapWriter writer = WriteableBitmapWriter.CreateWriter(height, wbmp.BackBufferStride, PixelFormat);
            
            writer.SetWriteableBitmapPixels(wbmp, new Int32Rect(0, 0, width, height), random);

            return wbmp;
        }

        private BitmapPalette GetPalette(PixelFormat format, DeterministicRandom random)
        {
            Array formatEnumValues = PixelFormatHelper.GetEnumValues(typeof(PixelFormat), "MS.Internal.PixelFormatEnum");
            Type formatType = format.GetType();

            PropertyInfo getFormatProp = formatType.GetProperty("Format", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            if (getFormatProp == null)
            {
                throw new InvalidOperationException("Could not get the Format property from the format type " + format.ToString());
            }

            Enum formatValue = (Enum)getFormatProp.GetValue(format, null);

            List<Color> colors = new List<Color>();
            int numberOfColors = 0;

            switch (formatValue.ToString())
            {
                case "Indexed1":
                    numberOfColors = 2;
                    break;
                case "Indexed2":
                    numberOfColors = 4;
                    break;
                case "Indexed4":
                    numberOfColors = 16;
                    break;
                case "Indexed8":
                    numberOfColors = 256;
                    break;
                default:
                    return null;
            }

            //HACK: these colors should be supplied by the stress framework
            for (int i = 0; i < numberOfColors; i++)
            {
                colors.Add(RandomColor(random));
            }

            return new BitmapPalette(colors);
        }

        private Color RandomColor(DeterministicRandom random)
        {
            return Color.FromArgb(unchecked((byte)random.Next()),
                                  unchecked((byte)random.Next()),
                                  unchecked((byte)random.Next()),
                                  unchecked((byte)random.Next()));
        }
    }

    class WriteableBitmapImageSourceFactory : DiscoverableFactory<WriteableBitmap>
    {
        [Input(ContentInputSource.CreateFromFactory)]
        public BitmapSource Source { get; set; }

        public override WriteableBitmap Create(DeterministicRandom random)
        {
            if (Source != null)
            {
                WriteableBitmap wbmp = new WriteableBitmap(Source);
                return wbmp;
            }
            else
            {
                return null;
            }
        }
    }

    [TargetTypeAttribute(typeof(WriteableBitmap))]
    class WriteableBitmapFactory : DiscoverableFactory<WriteableBitmap>
    {
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public PixelFormat PixelFormat { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 PixelWidth { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Int32 PixelHeight { get; set; }
        public BitmapPalette BitmapPalette { set; get; }

        public override WriteableBitmap Create(DeterministicRandom random)
        {
            WriteableBitmap writeableBitmap = new WriteableBitmap(PixelWidth, PixelHeight, random.NextDouble() * 100, random.NextDouble() * 100, PixelFormat, BitmapPalette);
            Int32Rect sourceRect = new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
            int bytesPerPixel = (writeableBitmap.Format.BitsPerPixel + 7) / 8;
            int stride = writeableBitmap.PixelWidth * bytesPerPixel;
            int arraySize = stride * writeableBitmap.PixelHeight;
            byte[] colorArray = new byte[arraySize];
            writeableBitmap.WritePixels(sourceRect, colorArray, stride, 0);
            return writeableBitmap;
        }
    }
}
