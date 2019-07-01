// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics.ReferenceRender
{    
    /// <summary>
    /// Brush to texture conversions
    /// </summary>
    public class TextureGenerator
    {
        /// <summary/>
        public static void ForceDpiOnBrush(ref ImageBrush original)
        {
            BitmapSource originalBitmap = original.ImageSource as BitmapSource;

            // we want to unify all source images to 96.0 dpi
            if (originalBitmap != null && originalBitmap.DpiX == 96.0 && originalBitmap.DpiY == 96.0) return;

            // we are assuming BGRA 32 bit color - assert that here
            if (originalBitmap.Format != PixelFormats.Bgra32)
            {
                originalBitmap = new FormatConvertedBitmap(originalBitmap, PixelFormats.Bgra32, null, 0);
            }

            // get the raw bitmap form the original brush
            int width = originalBitmap.PixelWidth;
            int height = originalBitmap.PixelHeight;
            byte[] bitmap = new byte[width * height * 4];
            originalBitmap.CopyPixels(bitmap, width * 4, 0);

            // Create image data for new brush, forcing 96 DPI
            BitmapSource image = BitmapSource.Create(
                width,
                height,
                96, 96, // FORCE DPI here
                PixelFormats.Bgra32,
                null,
                bitmap,
                width * 4);

            // reset imagedata
            original.ImageSource = image;
        }

        /// <summary/>
        public static TextureFilter RenderBrushToTextureFilter(Brush b, Point uvMin, Point uvMax, Rect screenSpaceBounds)
        {
            // For null brushes we create a corresponding null TextureFilter so that the material pass is ignored
            if (b == null)
            {
                return null;
            }
            if (b is SolidColorBrush)
            {
                return RenderBrushToTextureFilter((SolidColorBrush)b.CloneCurrentValue());
            }
            if (b is GradientBrush)
            {
                return RenderBrushToTextureFilter((GradientBrush)b.CloneCurrentValue(), uvMin, uvMax);
            }
            if (b is TileBrush)
            {
                return RenderBrushToTextureFilter((TileBrush)b.CloneCurrentValue(), uvMin, uvMax, screenSpaceBounds);
            }

            // unsupported types throw            
            throw new ApplicationException("Unknown brush type");
        }

        private static TextureFilter RenderBrushToTextureFilter(SolidColorBrush brush)
        {
            // Account for Opacity
            Color color = brush.Color;
            color = ColorOperations.Blend(
                    color,
                    Color.FromArgb(0, color.R, color.G, color.B),
                    Math.Max(Math.Min(brush.Opacity, 1.0), 0.0));

            // We don't want to rely on RenderTargetBitmap for SolidColorBrush
            return new SolidColorTextureFilter(color);
        }

        private static TextureFilter RenderBrushToTextureFilter(GradientBrush brush, Point uvMin, Point uvMax)
        {
            // Special Case 0 - acts as transparent solid color
            if (brush.GradientStops == null || brush.GradientStops.Count == 0)
            {
                return new SolidColorTextureFilter(Colors.Transparent);
            }

            // Special Case 1 - acts as solid color of the only stop
            if (brush.GradientStops.Count == 1)
            {
                return new SolidColorTextureFilter(brush.GradientStops[0].Color);
            }

            if (brush is LinearGradientBrush)
            {
                return RenderBrushToTextureFilter((LinearGradientBrush)brush, uvMin, uvMax);
            }
            else if (brush is RadialGradientBrush)
            {
                return RenderBrushToTextureFilter((RadialGradientBrush)brush, uvMin, uvMax);
            }
            throw new NotSupportedException(brush.GetType().Name + " is not a supported GradientBrush");
        }

        private static TextureFilter RenderBrushToTextureFilter(LinearGradientBrush brush, Point uvMin, Point uvMax)
        {
            // 0-255 spread for each color pass
            int size = (brush.GradientStops.Count - 1) * 256;

            if (brush.MappingMode == BrushMappingMode.Absolute)
            {
                // Convert to relative

                Matrix scale = GetConversionToRelative(uvMin, uvMax);

                brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                brush.StartPoint *= scale;
                brush.EndPoint *= scale;
            }

            BilinearTextureFilter filter = new BilinearTextureFilter(RenderBrushToImageData(brush, size, size));
            filter.HasErrorEstimation = false;
            return filter;
        }

        private static TextureFilter RenderBrushToTextureFilter(RadialGradientBrush brush, Point uvMin, Point uvMax)
        {
            if (brush.MappingMode == BrushMappingMode.Absolute)
            {
                // Convert to relative

                Matrix scale = GetConversionToRelative(uvMin, uvMax);

                brush.MappingMode = BrushMappingMode.RelativeToBoundingBox;
                brush.Center *= scale;
                brush.GradientOrigin *= scale;
                brush.RadiusX *= scale.M11;
                brush.RadiusY *= scale.M22;
            }

#if HIGH_RES
            // How close is the GradientOrigin to the edge of the ellipse defined by Center and RadiusX/Y ?
            // The gradient has the least room to interpolate between o and c+r.
            // We need to draw the gradient large enough to allow 256 color values for the shortest segment.
            //
            //         ,,-----,,
            //       ,'         ',
            //      /             \     c == Center
            //     ;               ;    o == GradientOrigin
            //     |       c       |    c+r == Center + RadiusX/Y
            //     ;            o  ;
            //      \             /
            //       ",         ,"
            //         ''-----''
            //
            //      c------o----c+r     x is the thin area between the GradientOrigin and the edge of the ellipse
            //                x         512 * r/x is how we scale that area into an acceptable rendering space
            //                          
            //      o--1--2---3-c+r     But we must also note that x may be subdivided by GradientStops...
            //       x1 x2  x3 x4       512 * r/xn is the actual scale we want (xn is the shortest GradientStop segment)
            //
            double x = brush.RadiusX - Math.Abs( brush.Center.X - brush.GradientOrigin.X );
            double y = brush.RadiusY - Math.Abs( brush.Center.Y - brush.GradientOrigin.Y );
            x = Math.Max( x, 0 );
            y = Math.Max( y, 0 );

            SortedList<int,double> list = new SortedList<int,double>();
            foreach ( GradientStop stop in brush.GradientStops )
            {
                list.Add( list.Count, stop.Offset );
            }
            double shortest = 1.0;
            for ( int n = 1; n < list.Count; n++ )
            {
                double offset = Math.Abs( list.Values[n] - list.Values[n-1] );

                // We can't do anything with 0, so skip it if it shows up.
                if ( MathEx.NotCloseEnough( offset, 0 ) )
                {
                    shortest = Math.Min( shortest, offset );
                }
            }

            double w = 512 * brush.RadiusX / ( x * shortest );
            double h = 512 * brush.RadiusY / ( y * shortest );
#endif
            double w = brush.GradientStops.Count * 256 * 2;
            double h = w;

            BilinearTextureFilter filter = new BilinearTextureFilter(RenderBrushToImageData(brush, (int)w, (int)h));
            filter.HasErrorEstimation = false;
            return filter;
        }

        private static TextureFilter RenderBrushToTextureFilter(TileBrush brush, Point uvMin, Point uvMax, Rect screenSpaceBounds)
        {
            if (brush.ViewportUnits == BrushMappingMode.Absolute)
            {
                // Convert to relative (including extra tiling caused by repeating UV's)

                Matrix scale = GetConversionToRelative(uvMin, uvMax);

                Point tl = brush.Viewport.TopLeft * scale;
                Point br = brush.Viewport.BottomRight * scale;

                brush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
                brush.Viewport = new Rect(tl, br);
            }

            // TODO: We don't test "Absolute" Viewbox at all and may need to make some considerations for it...

            double desiredWidth = 0;
            double desiredHeight = 0;
            if (brush is ImageBrush)
            {
                ImageBrush ib = (ImageBrush)brush;
                desiredWidth = ((BitmapSource)ib.ImageSource).PixelWidth;
                desiredHeight = ((BitmapSource)ib.ImageSource).PixelHeight;

                desiredWidth = desiredHeight = MathEx.Length(desiredWidth, desiredHeight);
            }
            else if (brush is DrawingBrush || brush is VisualBrush)
            {
                // We don't know how big to make it because WPF can scale the content to an arbitrary size
                //  and still look good (unlike images and their finite amount of data provided).
                //
                // If we make it too large, we will have more detail than WPF and if we make it too small,
                //  we won't have enough detail.  Making the image the as big as the diagonal of the screen
                //  space bounds Rect seems to work best.

                desiredWidth = desiredHeight = MathEx.Length(screenSpaceBounds.Width, screenSpaceBounds.Height);
            }

            double uvWidth = uvMax.X - uvMin.X;
            double uvHeight = uvMax.Y - uvMin.Y;
            switch (brush.Stretch)
            {
                case Stretch.None:
                    // Overwrite the previous computation and set bounds based on UV span
                    desiredWidth = uvWidth;
                    desiredHeight = uvHeight;
                    break;

                case Stretch.Uniform:
                case Stretch.UniformToFill:
                    // Match the aspect ratio of the brush content (currently at 1:1)
                    if (uvWidth > uvHeight)
                    {
                        desiredWidth *= uvWidth / uvHeight;
                    }
                    else
                    {
                        desiredHeight *= uvHeight / uvWidth;
                    }
                    break;

                case Stretch.Fill:
                    // desiredWidth/Height are already correct
                    break;
            }

            BitmapSource image = RenderBrushToImageData(brush, (int)desiredWidth, (int)desiredHeight);

            //// Avalon chooses different filtering based on brush type and tiling mode:
            ////      ImageBrush -> Trilinear
            ////      DrawingBrush -> Bilinear see Change 164703 by REDMOND\milesc on 2006/03/28 20:37:04
            ////      VisualBrush -> Bilinear see Change 164703 by REDMOND\milesc on 2006/03/28 20:37:04
            TextureFilter filter = (brush is ImageBrush)
                            ? new TrilinearTextureFilter(image)
                            : new BilinearTextureFilter(image);

            // Perform a texture lookup error estimation
            filter.HasErrorEstimation = true;
            return filter;
        }

        private static Matrix GetConversionToRelative(Point min, Point max)
        {
            double width = max.X - min.X;
            double height = max.Y - min.Y;
            double scaleX = MathEx.AreCloseEnough(width, 0.0) ? 0.0 : 1.0 / width;
            double scaleY = MathEx.AreCloseEnough(height, 0.0) ? 0.0 : 1.0 / height;

            Matrix result = new ScaleTransform(scaleX, scaleY).Value;
            result.OffsetX = -min.X * scaleX;
            result.OffsetY = -min.Y * scaleY;

            return result;
        }

        /// <summary/>
        public static BitmapSource RenderBrushToImageData(Brush b, int width, int height)
        {
            return RenderBrushToImageData(b, width, height, 96.0, 96.0);
        }

        /// <summary/>
        public static BitmapSource RenderBrushToImageData(Brush b, int width, int height, double dpiX, double dpiY)
        {
            Rect realizationSize = new Rect(0, 0, width, height);
            if (Dispatcher.CurrentDispatcher == null)
            {
                throw new ApplicationException("A valid Avalon Dispatcher is required to use this method.");
            }

            if (height < 1)
            {
                height = 1;
            }
            if (width < 1)
            {
                width = 1;
            }

            // Create image data
            RenderTargetBitmap id = new RenderTargetBitmap(width, height, dpiX, dpiY, PixelFormats.Pbgra32);

            // Create a visual to render
            DrawingVisual myDrawingVisual = new DrawingVisual();

            // draw a rectangle using the given brush
            DrawingContext myDrawingContext = myDrawingVisual.RenderOpen();
            myDrawingContext.DrawRectangle(b, null, realizationSize);
            myDrawingContext.Close();

            // Render into Bitmap
            id.Render(myDrawingVisual);

            // We need this here since TR is asuming ARGB32 not PARGB32 - we do our own format conversions
            FormatConvertedBitmap bitmap = new FormatConvertedBitmap(id, PixelFormats.Bgra32, null, 0.0);

            return bitmap;
        }

        /// <summary>
        /// Render a brush to a buffer of given size at the current DPI setting
        /// </summary>
        public static Color[,] RenderBrushToColorArray(Brush b, int width, int height)
        {
            return ColorOperations.ToColorArray(TextureGenerator.RenderBrushToImageData(b, width, height, Const.DpiX, Const.DpiY));
        }
    }
}

