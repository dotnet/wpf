// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageSource. PresentationCore.dll
using System.Windows.Media.Imaging;

using System.Security;
//using System.Drawing.Printing;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Decode ImageSource into PARGB32 format, keep in managed memory to allow multiple blending,
    /// finally generate ImageSource when needed to interface with Avalon. 
    /// Avalon ImageSource converts data to unmanaged memory.
    /// </summary>
    internal class ImageProxy
    {
        /// <summary>
        /// Maximum ratio between pixel count of requested clip rectangle and actual image rectangle
        /// for clipping of image data to be performed.
        /// </summary>
        /// <remarks>
        /// The flattening process draws primitive intersection regions by blending brushes together,
        /// then clipping to that region. The problem is when blending image primitive with something:
        /// the entire image is drawn regardless of the intersection region size. This can significantly
        /// increase spool file size.
        /// 
        /// The solution is to detect such cases and clip image data down prior to blending and drawing
        /// the intersection. This ratio controls when this clipping occurs.
        /// </remarks>
        private const double MaximumClipRatio = 0.9;

        /// <summary>
        /// Minimum ratio between this image's size and brush size when blending before we magnify
        /// this image. Without magnification, the brush will lose detail due to being scaled down
        /// to image's size.
        /// </summary>
        private const double MinimumBlendRatio = 0.5;

        /// <summary>
        /// Maximum size to use when magnify image if scale is less than MinimumBlendRatio, to avoid
        /// huge image
        /// </summary>
        private const int    MaximumOpacityMaskViewport = 1024;
        
        protected int          _pixelWidth;
        protected int          _pixelHeight;
        protected BitmapSource _image;

        protected Byte[]       _pixels;

        public ImageProxy(BitmapSource image)
        {
            Debug.Assert(image != null);

            _pixelWidth  = image.PixelWidth;
            _pixelHeight = image.PixelHeight;
            _image       = image;
        //  _pixels      = null;
        }

        public BitmapSource Image
        {
            get
            {
                return _image;
            }
        }

        public Byte[] Buffer
        {
            get
            {
                return _pixels;
            }
        }

        public int PixelWidth
        {
            get
            {
                return _pixelWidth;
            }
        }

        public int PixelHeight
        {
            get
            {
                return _pixelHeight;
            }
        }

        /// <summary>
        /// Scales the image.
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        public void Scale(double scaleX, double scaleY)
        {
            _image = new TransformedBitmap(
                _image,
                new MatrixTransform(Matrix.CreateScaling(scaleX, scaleY))
                );

            _pixelWidth = _image.PixelWidth;
            _pixelHeight = _image.PixelHeight;
            _pixels = null;
        }
        
        private void Decode()
        {
            if (_pixels == null)
            {
                _pixels = GetDecodedPixels(new Int32Rect(0, 0, _pixelWidth, _pixelHeight));
            }
        }

        /// <summary>
        /// Decodes a subimage, returning the decoded pixels.
        /// </summary>
        /// <param name="bounds">Bounds of subimage to decode</param>
        /// <returns>Returns critical pixels</returns>
        private byte[] GetDecodedPixels(Int32Rect bounds)
        {
            Debug.Assert(
                (bounds.X >= 0) &&
                (bounds.Y >= 0) &&
                ((bounds.X + bounds.Width) <= _pixelWidth) &&
                ((bounds.Y + bounds.Height) <= _pixelHeight)
                );

            int stride = bounds.Width * 4;

            byte[] pixels = new Byte[stride * bounds.Height];

            FormatConvertedBitmap converter = new FormatConvertedBitmap();
            converter.BeginInit();
            converter.Source = _image;
            converter.DestinationFormat = PixelFormats.Pbgra32;
            converter.EndInit();

            converter.CriticalCopyPixels(bounds, pixels, stride, 0);

            return pixels;
        }
        
        /// <param name="opacity"></param>
        /// <param name="opacityMask"></param>
        /// <param name="rect">Image destination rectangle</param>
        /// <param name="trans">Transformation from image to final destination</param>
        public void PushOpacity(double opacity, BrushProxy opacityMask, Rect rect, Matrix trans)
        {
            if (opacityMask != null)
            {
                rect.Transform(trans);

                //
                // Blend this image on top of opacity mask.
                //

                // Calculate scaling factor from opacity mask to this image.
                TileBrush opacityBrush = opacityMask.Brush as TileBrush;
                Rect viewport;

                if (opacityBrush != null)
                {
                    Debug.Assert(opacityBrush.ViewportUnits == BrushMappingMode.Absolute, "TileBrush must have absolute viewport by this point");

                    viewport = opacityBrush.Viewport;
                }
                else
                {
                    // viewport covers entire image
                    viewport = rect;
                }

                // Fix for 1689025: 
                
                double scaleX = _pixelWidth  / rect.Width;
                double scaleY = _pixelHeight / rect.Height;

                // If current image is too small, magnify it to match opacity mask's size,
                // otherwise we lose the detail in opacity mask.
                if ((scaleX < MinimumBlendRatio || scaleY < MinimumBlendRatio) &&
                    (rect.Width  <= MaximumOpacityMaskViewport) &&
                    (rect.Height <= MaximumOpacityMaskViewport)) // Avoiding generate huge bitmap
                {
                    Scale(rect.Width  / _pixelWidth, 
                          rect.Height / _pixelHeight);
                    scaleX = 1.0;
                    scaleY = 1.0;
                }

                // Transform brush to image space.
                Matrix transform = new Matrix();
                transform.Translate(-rect.Left, -rect.Top);
                transform.Scale(scaleX, scaleY);

                // Blend opacity mask into image.
                BlendUnderBrush(false, opacityMask, transform);
            }

            int op = Utility.OpacityToByte(opacity);

            if (op <= 0)
            {
                _image  = null;
                _pixels = null;
                return;
            }
            else if (op >= 255)
            {
                return;
            }
            
            Decode();

            Byte[] map = new Byte[256];
            
            for (int i = 0; i < 256; i ++)
            {
                map[i] = (Byte)(i * op / 255);
            }

            int count = _pixelWidth * _pixelHeight * 4;
            
            for (int i = 0; i < count; i++)
            {
                _pixels[i] = map[_pixels[i]];
            }
        }

        public void BlendUnderColor(Color color, double opacity, bool opacityOnly)
        {
            Decode();
            Utility.BlendUnderColor(_pixels, _pixelWidth * _pixelHeight, color, opacity, opacityOnly);
        }

        public void BlendOverColor(Color color, double opacity, bool opacityOnly)
        {
            if (opacityOnly || !Utility.IsOpaque(opacity) || !IsOpaque())
            {
                // Always blend if image is opacity mask, so that a proper opacity mask image
                // is formed, otherwise the original image pixels will be used.
                Decode();
                Utility.BlendOverColor(_pixels, _pixelWidth * _pixelHeight, color, opacity, opacityOnly);
            }
        }

        /// <summary>
        /// Render a brush on top of current image
        /// </summary>
        /// <param name="opacityOnly"></param>
        /// <param name="brush"></param>
        /// <param name="trans"></param>
        public void BlendUnderBrush(bool opacityOnly, BrushProxy brush, Matrix trans)
        {
            if (brush.Brush is SolidColorBrush)
            {
                SolidColorBrush sb = brush.Brush as SolidColorBrush;

                BlendUnderColor(Utility.Scale(sb.Color, brush.Opacity), 1, opacityOnly);
            }
            else
            {
                Byte[] brushPixels = RasterizeBrush(brush, trans);

                Decode();

                Utility.BlendPixels(_pixels, opacityOnly, brushPixels, brush.OpacityOnly, _pixelWidth * _pixelHeight, _pixels);
            }
        }

        /// <summary>
        /// Rasterize a brush into a bitmap
        /// </summary>
        /// <param name="brush">Brush to rasterize</param>
        /// <param name="trans"></param>
        /// <returns>Pbgra32 pixel byte array</returns>
        private Byte[] RasterizeBrush(BrushProxy brush, Matrix trans)
        {
            return brush.CreateBrushImage(trans, _pixelWidth, _pixelHeight);
        }

        /// <summary>
        /// Render a brush under current image
        /// </summary>
        /// <param name="opacityOnly"></param>
        /// <param name="brush"></param>
        /// <param name="trans"></param>
        public void BlendOverBrush(bool opacityOnly, BrushProxy brush, Matrix trans)
        {
            if (IsOpaque())
            {
                Debug.Assert(!opacityOnly, "Opaque image OpacityMask should not be blended with brush");
                return;
            }

            if (brush.Brush is SolidColorBrush)
            {
                SolidColorBrush sb = brush.Brush as SolidColorBrush;

                BlendOverColor(Utility.Scale(sb.Color, brush.Opacity), 1.0, opacityOnly);
            }
            else
            {
                Byte[] brushPixels = RasterizeBrush(brush, trans);

                Decode();

                Utility.BlendPixels(brushPixels, brush.OpacityOnly, _pixels, opacityOnly, _pixelWidth * _pixelHeight, _pixels);
            }
        }

        internal static int HasAlpha(BitmapSource bitmap)
        {
            if (bitmap.Format.HasAlpha)
            {
                return 1;
            }

            if (bitmap.Format.Palettized)
            {
                BitmapPalette palette = bitmap.Palette;

                if (palette != null)
                {
                    IList<System.Windows.Media.Color> palColor = palette.Colors;

                    if (palColor != null)
                    {
                        foreach (Color c in palColor)
                        {
                            if (! Utility.IsOpaque(c.ScA))
                            {
                                return 2;
                            }
                        }
                    }
                }

            }

            return 0;
        }
        
        public bool IsOpaque()
        {
            if (_image == null)
            {
                return false;
            }

            if (_pixels == null) // Not decoded yet
            {
                int hasAlpha = HasAlpha(_image);

                if (hasAlpha == 2)
                {
                    return false;
                }

                if (hasAlpha == 0)
                {
                    return true;
                }
            }

            Decode();

            int count = _pixelWidth * _pixelHeight;

            for (int i = 0; i < count; i++)
            {
                if (_pixels[i * 4 + 3] != 255)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if an image is totally transparent
        /// </summary>
        /// <returns></returns>
        public bool IsTransparent()
        {
            if (_image == null)
            {
                return true;
            }

            Decode();

            int count = _pixelWidth * _pixelHeight * 4;

            // _pixels is in PBGRA format, check all channels
            
            for (int i = 0; i < count; i++)
            {
                if (_pixels[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public BitmapSource GetImage()
        {
            if (_pixels == null)
            {
                return _image;
            }
            else if (_image != null)
            {
                return BitmapSource.Create(_pixelWidth, _pixelHeight, _image.DpiX, _image.DpiY, PixelFormats.Pbgra32, null, _pixels, _pixelWidth * 4);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a BitmapSource that has image clipped to the specified bounds.
        /// </summary>
        /// <param name="bounds">Desired clipping bounds in image DPI</param>
        /// <param name="clipBounds">Receives actual bounds to which image was clipped</param>
        /// <remarks>
        /// clipBounds may be one of following:
        /// - Empty: Entire image was clipped.
        /// - Equal to original image size: No image clipping performed.
        /// - Other: Some clipping performed.
        /// 
        /// Clipping is not always performed; see MaximumClipRatio.
        /// </remarks>
        public BitmapSource GetClippedImage(Rect bounds, out Rect clipBounds)
        {
            BitmapSource result = null; // default to entire image clipped away
            clipBounds = Rect.Empty;

            // scale bounds according to image DPI
            double dpiScaleX = _image.DpiX / 96.0;
            double dpiScaleY = _image.DpiY / 96.0;

            if (Utility.IsZero(dpiScaleX))
                dpiScaleX = 1;
            if (Utility.IsZero(dpiScaleY))
                dpiScaleY = 1;

            bounds.Scale(dpiScaleX, dpiScaleY);
            bounds.Intersect(new Rect(0, 0, _pixelWidth, _pixelHeight));

            double currentPixelCount = _pixelWidth * _pixelHeight;
            double clipPixelCount = bounds.Width * bounds.Height;

            if (currentPixelCount > 0)
            {
                if ((clipPixelCount / currentPixelCount) > MaximumClipRatio)
                {
                    // Desired clip bounds not small enough to necessitate clipping image data.
                    result = GetImage();
                    clipBounds = new Rect(0, 0, _pixelWidth, _pixelHeight);
                }
                else
                {
                    //
                    // Clipped rectangle significantly smaller than image size. Manually
                    // clip image down to bounds.
                    //
                    // Fix bug 1494512: Round so that we try to get at least a pixel, otherwise
                    // bounds < 1 pixel (which'll display a solid color) may get clipped away.
                    //
                    int x0 = (int)Math.Max(Math.Floor(bounds.Left), 0);
                    int y0 = (int)Math.Max(Math.Floor(bounds.Top), 0);
                    int x1 = (int)Math.Ceiling(bounds.Right);
                    int y1 = (int)Math.Ceiling(bounds.Bottom);

                    int width = x1 - x0;
                    int height = y1 - y0;

                    if (width > 0 && height > 0)
                    {
                        byte[] pixels;

                        if (_pixels == null)
                        {
                            // not decoded yet, we perform clipping while decoding
                            pixels = GetDecodedPixels(new Int32Rect(x0, y0, width, height));
                        }
                        else
                        {
                            // clip previously decoded pixels
                            pixels = Utility.ClipPixels(_pixels, _pixelWidth, _pixelHeight, x0, y0, width, height);
                        }

                        result = BitmapSource.Create(
                            width, height,
                            _image.DpiX, _image.DpiY,
                            PixelFormats.Pbgra32,
                            null,
                            pixels,
                            width * 4
                            );
                        
                        clipBounds = bounds;
                    }
                }
            }

            // unscale according to image DPI
            if (!clipBounds.IsEmpty)
            {
                clipBounds.Scale(1.0 / dpiScaleX, 1.0 / dpiScaleY);
            }

            return result;
        }

        public ImageProxy Clone()
        {
            return new ImageProxy(GetImage());
        }
    } // end of ImageProxy class
} // end of namespace
