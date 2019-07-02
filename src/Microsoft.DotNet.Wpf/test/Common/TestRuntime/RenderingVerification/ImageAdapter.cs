// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        


namespace Microsoft.Test.RenderingVerification
{
    #region using
    using System;
    using System.IO;
    using System.Drawing;
    using System.Reflection;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using Microsoft.Test.RenderingVerification.Filters;
    using Microsoft.Test.RenderingVerification.UnmanagedProxies;
    using Microsoft.Test.Display;
    #endregion using

    /// <summary>
    /// Summary description for ImageAdapter
    /// </summary>
    public class ImageAdapter: IImageAdapterUnmanaged,IImageAdapter, ICloneable  // IImageAdapter implements ICloneable
    {
        #region Properties
        #region Private Properties
        private IColor [,] _colors = null; 
        private IMetadataInfo _metadataInfo = null;
        #endregion Private Properties
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Marked as private to block the default constructor
        /// </summary>
        internal protected ImageAdapter() 
        {
            _metadataInfo = new MetadataInfo();
        }
        /// <summary>
        /// Instanciate a new width * height ImageAdapter object 
        /// </summary>
        /// <param name="width">The width of the image adapter</param>
        /// <param name="height">The height of the image adapter</param>
        public ImageAdapter(int width,int height) : this()
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException("Width nor Height can have negative values");
            }
            _colors = new IColor[width, height];
        }
        /// <summary>
        /// Instanciate a new width * height ImageAdapter object and initalize every entry with the specified color
        /// </summary>
        /// <param name="width">The width of the image adapter</param>
        /// <param name="height">The height of the image adapter</param>
        /// <param name="initColor">The color to use for initalization</param>
        public ImageAdapter(int width, int height, IColor initColor) : this(width, height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException("Width nor Height can have negative values");
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (initColor != null)
                    {
                        _colors[x, y] = (IColor)initColor.Clone();
                    }
                }
            }
        }
        /// <summary>
        /// Instanciate a new ImageAdapter based on a Bitmap
        /// </summary>
        /// <param name="bmp">The bitmap to use</param>
        public ImageAdapter(Bitmap bmp)
        {
            InternalConstructor(bmp);
        }
        /// <summary>
        /// Instanciate a new ImageAdapter object using the specified file containing the Bitmap
        /// </summary>
        /// <param name="bitmapFileName">The file containing the Bitmap to use in the Adapter</param>
        public ImageAdapter(string bitmapFileName)
        {
            using (Bitmap bmp = ImageUtility.GetUnlockedBitmap(bitmapFileName))
            {
                InternalConstructor(bmp);
            }
        }
        /// <summary>
        /// Build an ImageAdapter based on an existing IImageAdapter
        /// </summary>
        /// <param name="imageAdapterToClone"></param>
        public ImageAdapter(IImageAdapter imageAdapterToClone)
        {
            // Check argument
            if (imageAdapterToClone == null)
            {
                throw new ArgumentNullException("imageAdapterToClone", "Argument passed in must be set to valid instanceof the object (null was passed in)");
            }

            if (imageAdapterToClone.Width <= 0 || imageAdapterToClone.Height <= 0)
            {
                string errorMessage = string.Empty;
                string actualValue = string.Empty;

                if (imageAdapterToClone.Width <= 0)
                {
                    errorMessage += "ImageAdapter Width is invalid (should be strictly > 0)\n";
                    actualValue += "Width=" + imageAdapterToClone.Width + "\n";
                }

                if (imageAdapterToClone.Height <= 0)
                {
                    errorMessage += "ImageAdapter Height is invalid (should be strictly > 0)";
                    actualValue += "Height=" + imageAdapterToClone.Height;
                }

                throw new ArgumentOutOfRangeException("imageAdapterToClone", actualValue, errorMessage);
            }

            _colors = new IColor[imageAdapterToClone.Width, imageAdapterToClone.Height];
            for (int y = 0; y < imageAdapterToClone.Height; y++)
            {
                for (int x = 0; x < imageAdapterToClone.Width; x++)
                {
                    if (imageAdapterToClone[x, y] != null)
                    {
                        _colors[x, y] = (IColor)imageAdapterToClone[x, y].Clone();
                    }
                }
            }
            if (imageAdapterToClone.Metadata != null)
            {
                _metadataInfo = (IMetadataInfo)imageAdapterToClone.Metadata.Clone();
            }
        }
        /// <summary>
        /// Build an ImageAdapter based on an area of the screen
        /// </summary>
        /// <param name="screenAreaToCapture">The area to capture (in screen coordinate) on the screen</param>
        public ImageAdapter(System.Drawing.Rectangle screenAreaToCapture)
        {
            using (Bitmap bmp = ImageUtility.CaptureScreen(screenAreaToCapture))
            {
                InternalConstructor(bmp);
            }
        }
        
        /// <summary>
        /// Build an ImageAdapter based on a Window handler
        /// </summary>
        /// <param name="Hwnd">The handle to window</param>
        /// <param name="clientAreaOnly">bool to capture the client are only, true to capture the whole window</param>
        public ImageAdapter(IntPtr Hwnd, bool clientAreaOnly)
        {
            using (Bitmap bmp = ImageUtility.CaptureScreen(Hwnd, clientAreaOnly))
            {
                InternalConstructor(bmp);
            }
        }
        #endregion Constructors

        #region Private Methods
        private void InternalConstructor(Bitmap bmp)
        {
            if (bmp == null)
            {
                throw new ArgumentNullException("Bitmap passed in is null");
            }
            _colors = new IColor[bmp.Width, bmp.Height];
            
            _metadataInfo = new MetadataInfo(bmp.PropertyItems);

            
            //Verify that metadata correctly set the DPI
            //In the case where no dpi meta is stored with the image it defaults to the screen resolution
            //we fix that here in the metadata info
            Point metaDpi = MetadataInfoHelper.GetDpi(_metadataInfo);
            Point bmpDpi = new Point((int)Math.Round(bmp.HorizontalResolution), (int)Math.Round(bmp.VerticalResolution));
            if (!metaDpi.Equals(bmpDpi))
            {
                MetadataInfoHelper.SetDpi(_metadataInfo, bmpDpi);
            }

            ImageUtility image = null;
            try
            {
                image = new ImageUtility(bmp); // Conversion to 32 bits done internally.
                image.GetSetPixelUnsafeBegin();
                byte[] BGRAStream = image.GetStreamBufferBGRA();
                image.GetSetPixelUnsafeRollBack();
                int size = BGRAStream.Length;
                for (int index = 0, x = 0, y = 0; index < size; index += 4, x++) // flat array to remove the cost of the computation (x + y *x)
                {
                    if (x >= Width) { x = 0; y++; }
                    _colors[x, y] = new ColorByte(BGRAStream[index + 3], BGRAStream[index + 2], BGRAStream[index + 1], BGRAStream[index]);
                    if (_colors[x, y].ARGB == 0) { _colors[x, y].IsEmpty = true; }
                }
                BGRAStream = null;
            }
            finally
            {
                if (image != null) { image.Dispose(); image = null; }
                GC.Collect(GC.MaxGeneration);
            }
        }

        internal void SetMetadata(IMetadataInfo metadataInfo)
        {
            if (metadataInfo == null) { return; }
            _metadataInfo = (IMetadataInfo)metadataInfo.Clone();
        }
        #endregion Private Methods

        #region Interfaces implementation
        #region IClonable implementation
        /// <summary>
        /// Clones an ImageAdapter
        /// </summary>
        /// <returns>The cloned image</returns>
        public virtual object Clone()
        {
            return new ImageAdapter(this);
        }
        #endregion IClonable implementation
        #region IImageAdapter implementation
        /// <summary>
        /// Return the Width of the image
        /// </summary>
        public int Width
        {
            get
            {
                if (_colors == null)
                {
                    throw new RenderingVerificationException("ImageAdapter is null");
                }

                return _colors.GetUpperBound(0) + 1;
            }
        }
        /// <summary>
        /// Return the Height of the image
        /// </summary>
        public int Height
        {
            get
            {
                if (_colors == null)
                {
                    throw new RenderingVerificationException("ImageAdapter is null");
                }

                return _colors.GetUpperBound(1) + 1;
            }
        }
        /// <summary>
        /// indexer (Item Method in VB) for the IColor
        /// </summary>
        public IColor this[int x, int y]
        {
            get
            {
                if (_colors == null)
                {
                    throw new RenderingVerificationException("ImageAdapter is null");
                }

                if (x >= Width || y >= Height || x < 0 || y < 0)
                {
                    throw new RenderingVerificationException("Out of bound");
                }

                return _colors[x, y];
            }
            set
            {
                if (_colors == null)
                {
                    throw new RenderingVerificationException("ImageAdapter is null");
                }

                if (x >= Width || y >= Height || x < 0 || y < 0)
                {
                    throw new RenderingVerificationException("Out of bound");
                }
                
                _colors[x, y] = value;
            }
        }

        /// <summary>
        /// Contains the image metadata information
        /// </summary>
        /// <value></value>
        public IMetadataInfo Metadata
        {
            get { return _metadataInfo; }
        }

        /// <summary>
        /// Returns the Dpi data for the X dimension
        /// </summary>
        public double DpiX
        {
            get 
            { 
                return MetadataInfoHelper.GetDpi(this).X; 
            }
            set
            {
                MetadataInfoHelper.SetDpi(this.Metadata, new Point((int)value, (int)MetadataInfoHelper.GetDpi(this).Y));
            }
        }
        /// <summary>
        /// Returns the Dpi data for the Y dimension
        /// </summary>
        public double DpiY
        {
            get 
            { 
                return MetadataInfoHelper.GetDpi(this).Y; 
            }
            set
            {
                MetadataInfoHelper.SetDpi(this.Metadata, new Point((int)MetadataInfoHelper.GetDpi(this).X, (int)value));
            }
        }
        #endregion IImageAdapter implementation
        #region IImageAdapterUnmanaged
        #region ApplySingleAlpha
        /// <summary>
        /// Multiply all the pixels with an alpha value.
        /// </summary>
        /// <param name="alpha">Alpha value(between 0 and 1)</param>
        /// <param name="image">BitmapResources to have Alpha applied on</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged IImageAdapterUnmanaged.ApplySingleAlpha (
            double alpha, 
            IImageAdapterUnmanaged image)
        {
#if DEBUG
            ImageUtility.ToImageFile(this, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_ApplyAlpha_this.png"));
            ImageUtility.ToImageFile(image, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_ApplyAlpha_image.png"));
#endif 

            if (alpha > 1.0 || alpha < 0.0)
            {
                throw new ArgumentOutOfRangeException("Alpha must be between 0.0 and 1.0 (this is percentage)");
            }

            System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() started");

            IColor argb = ColorByte.Empty;
            if ((ImageAdapter)this != (ImageAdapter)image)
            {
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() this != image");
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() Before for-loop");
                for (int x = 0; x < image.Width; x++)
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        argb = (IColor)image[x, y].Clone();
                        argb.ExtendedAlpha *= alpha;
                        this[x, y] = argb;
                    }
                }
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() After for-loop");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() this == image");
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() Before for-loop");
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        this[x, y].ExtendedAlpha = this[x, y].ExtendedAlpha * alpha;
                    }
                }
                System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() After for-loop");
            }
#if DEBUG
            ImageUtility.ToImageFile(this, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "After_ApplyAlpha_Result.png"));
            ImageUtility.ToImageFile(image, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "After_ApplyAlpha_Operand.png"));
#endif 

            System.Diagnostics.Debug.WriteLine("ApplySingleAlpha() ended \n");
            return (IImageAdapterUnmanaged)this;
        }
        #endregion ApplySingleAlpha

        #region Merge
        /// <summary>
        /// Merges two images taking into consideration their alpha values.
        /// </summary>
        /// <param name="bmpBg">The background Image</param>
        /// <param name="bmpTop">The image that goes on top of the background</param>
        /// <param name="xBg">Left corner position of background</param>
        /// <param name="yBg">Top corner position of the background</param>
        /// <param name="xTop">Left corner position of the top image</param>
        /// <param name="yTop">Top corner position of the top image</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged IImageAdapterUnmanaged.Merge(IImageAdapterUnmanaged bmpBg, IImageAdapterUnmanaged bmpTop, int xBg, int yBg, int xTop, int yTop)
        {
            System.Diagnostics.Debug.WriteLine("Merge() started");
#if DEBUG
            ImageUtility.ToImageFile(bmpBg, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_Merge_Bg.png"));
            ImageUtility.ToImageFile(bmpTop, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Before_Merge_Top.png"));
#endif // debug

#if DEBUG
            ImageUtility.ToImageFile((IImageAdapter)bmpBg, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Merge_BgImage.bmp"));
            ImageUtility.ToImageFile((IImageAdapter)bmpTop, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Merge_TopImage.bmp"));
#endif // DEBUG

            Rectangle rBg = new Rectangle(xBg, yBg, bmpBg.Width, bmpBg.Height);
            Rectangle rTop = new Rectangle(xTop, yTop, bmpTop.Width, bmpTop.Height);
            System.Diagnostics.Debug.WriteLine("Merge() \n\nrBg " + rBg.ToString() + "\nrTop " + rTop.ToString());
            if (Rectangle.Intersect(rBg, rTop) != rTop) 
            { 
                throw new ArgumentOutOfRangeException("Top BMP must be inside Bg one"); 
            }

            int maxAlpha = 0;
            float bgAlpha = 0;
            float topAlpha = 0;

            // Copy Color from background image into this (same as cloning bg image into 'this')
            ImageAdapter temp = new ImageAdapter((IImageAdapter)bmpBg);

            System.Diagnostics.Debug.WriteLine ("Merge1() : Before the loop");
            for (int x = rBg.Left; x < rBg.Right; x++)
            {
                for (int y = rBg.Top; y < rBg.Bottom; y++)
                {
                    if (rTop.Contains(x, y))
                    {
                        IColor colorBg = bmpBg[x - rBg.Left, y - rBg.Top];
                        IColor colorTop = bmpTop[x - rTop.Left, y - rTop.Top];
                        maxAlpha = Math.Max(colorBg.A, colorTop.A);
                        topAlpha = (float)colorTop.Alpha;
                        bgAlpha = (float)((1f - colorTop.Alpha) * colorBg.Alpha);

                        IColor color = new ColorByte((byte)maxAlpha,
                            (byte)(bgAlpha * colorBg.R + topAlpha * colorTop.R),
                            (byte)(bgAlpha * colorBg.G + topAlpha * colorTop.G),
                            (byte)(bgAlpha * colorBg.B + topAlpha * colorTop.B));
                        temp[x - rBg.Left, y - rBg.Top] = color;
                    }
                }
            }
#if DEBUG
            ImageUtility.ToImageFile(temp, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "After_Merge_This.png"));
#endif 

            return (IImageAdapterUnmanaged)temp;
        }
        #endregion Merge

        #region Load
        /// <summary>
        /// Load an image as IImageAdapterUnmananged
        /// </summary>
        /// <param name="fileName">File name of the image to be loaded</param>
        void IImageAdapterUnmanaged.Load(
            string fileName)
        {
            using (Bitmap bmp = ImageUtility.GetUnlockedBitmap(fileName))
            {
                InternalConstructor(bmp);
            }
        }
        #endregion Load

        #region Save
        /// <summary>
        /// Serialize an IImageAdapterUnmananged 
        /// </summary>
        /// <param name="fileName">The filename of the image to be saved</param>
        void IImageAdapterUnmanaged.Save(
            string fileName)
        {
            ImageUtility.ToImageFile(this, fileName);
        }
        #endregion Save

        #region Scale
        /// <summary>
        /// Scales the image.
        /// </summary>
        /// <param name="resultWidth">The width of the image after scaling</param>
        /// <param name="resultHeight">The height of the image after scaling</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged IImageAdapterUnmanaged.Scale(
            int resultWidth, 
            int resultHeight)
        {
            if (resultWidth < 0 || resultHeight < 0)
            {
                throw (new ArgumentOutOfRangeException("The resultWidth and resultHeight parameters are less than zero\n\r"));
            }
            SpatialTransformFilter spatialFilter = new SpatialTransformFilter();
            spatialFilter.HorizontalScaling = resultWidth / (double)this.Width;
            spatialFilter.VerticalScaling = resultHeight / (double)this.Height;
            return (IImageAdapterUnmanaged)spatialFilter.Process(this);
        }
        #endregion Scale
   
        #region Width
        /// <summary>
        /// Returns the width of the image
        /// </summary>
        /// <returns>The width</returns>
        int IImageAdapterUnmanaged.GetWidth()
        {
            return this.Width;
        }
        #endregion Width   

        #region Height
        /// <summary>
        /// Returns the height of the image
        /// </summary>
        /// <returns>The height</returns>
        int IImageAdapterUnmanaged.GetHeight()
        {
            return this.Height;
        }
        #endregion Height

        #region GetPixel
        /// <summary>
        /// Returns the colorref at the specified point in the image
        /// </summary>
        /// <param name="x">X coordinate of the point</param>
        /// <param name="y">Y coordinate of the point</param>
        /// <returns>Retruns an uint which is same as COLORREF</returns>
        int IImageAdapterUnmanaged.GetPixel(
            int x, 
            int y)
        {
            if (x >= this.Width || y >= this.Height || x < 0 || y < 0) 
            { 
                System.Diagnostics.Debug.WriteLine("x and y parameters are not within range" + x + "  " + y + "\n\r");
                throw (new ArgumentOutOfRangeException("The x and y parameters are not within range\n\r"));
            }
            IColor c = this[x, y];
            if (c == null) 
            { 
                System.Diagnostics.Debug.WriteLine("Could not access the point\n\r");
            }
            System.Diagnostics.Debug.WriteLine("Red is " + c.R + "\n");
            System.Diagnostics.Debug.WriteLine("Green is " + c.G + "\n");
            System.Diagnostics.Debug.WriteLine("Blue is " + c.B + "\n");
            System.Diagnostics.Debug.WriteLine("Alpha is " + c.Alpha + "\n");
            return  ((int) (c.B * c.Alpha) << 16) | ((int) (c.G * c.Alpha) << 8) | ((int) (c.R * c.Alpha));
        }
        #endregion GetColorRef

        #region Overlay
        /// <summary>
        /// Places one image on top of the other. The top image should fit within the bottom
        /// image. The top image completely cover the bottom image.
        /// </summary>
        /// <param name="bmpTop"></param>
        /// <param name="xstart">The x value of the point in the bottom image at which the left 
        /// hand top corner of the top image is placed</param>
        /// <param name="ystart">The y value of the point in the bottom image at which the left 
        /// hand top corner of the top image is placed</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged IImageAdapterUnmanaged.Overlay(
            IImageAdapterUnmanaged bmpTop, 
            int xstart, 
            int ystart)
        {
            System.Diagnostics.Debug.WriteLine("Overlay() started");

            Rectangle rBg = new Rectangle(0, 0, this.Width, this.Height);
            Rectangle rTop = new Rectangle(xstart, ystart, bmpTop.Width, bmpTop.Height);
            System.Diagnostics.Debug.WriteLine("Overlay() \n\nrBg " + rBg.ToString() + "\nrTop " + rTop.ToString());
            if (Rectangle.Intersect(rBg, rTop) != rTop) 
            { 
                throw new ArgumentOutOfRangeException("Top BMP must be inside Bg one"); 
            }

            // Copy Color from background image into this (same as cloning bg image into 'this')
            ImageAdapter temp = new ImageAdapter((IImageAdapter)this);

            System.Diagnostics.Debug.WriteLine ("Overlay() : Before the loop");
            for (int x = rBg.Left; x < rBg.Right; x++)
            {
                for (int y = rBg.Top; y < rBg.Bottom; y++)
                {
                    if (rTop.Contains(x, y))
                    {
                        IColor colorTop = bmpTop[x - rTop.Left, y - rTop.Top];
                        temp[x, y] = colorTop;
                    }
                }
            }
            return (IImageAdapterUnmanaged)temp;
        }
        #endregion Overlay

        #region Clip
        /// <summary>
        /// Clips the image at the RECT specified
        /// </summary>
        /// <param name="left">The left side of the RECT</param>
        /// <param name="top">The top side of the RECT</param>
        /// <param name="right">The right side of the RECT</param>
        /// <param name="bottom">The bottom side of the RECT</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged IImageAdapterUnmanaged.Clip(
            int left, 
            int top, 
            int right, 
            int bottom)
        {
            System.Diagnostics.Debug.WriteLine("Clip() started");

            Rectangle rBg = new Rectangle(0, 0, this.Width, this.Height);
            Rectangle rTop = new Rectangle(left, top, right-left, bottom-top);
            System.Diagnostics.Debug.WriteLine("Clip() rBg " + rBg.ToString() + "\nrTop " + rTop.ToString());
            Rectangle intersection = Rectangle.Intersect(rBg, rTop);
            System.Diagnostics.Debug.WriteLine("\n\rClip() Intersection " + intersection.ToString());
            if (intersection != rTop) 
            { 
                System.Diagnostics.Debug.WriteLine("Clip area is not perfectly within the image\n");                
            }

            if (intersection.Width > 0 && intersection.Height > 0)
            {
                ImageAdapter temp = new ImageAdapter(intersection.Width, intersection.Height, (ColorByte)Color.Black);

                System.Diagnostics.Debug.WriteLine ("Clip() : Before the loop");
                for (int x = intersection.Left; x < intersection.Right; x++)
                {
                    for (int y = intersection.Top; y < intersection.Bottom; y++)
                    {
                        IColor colorTop = this[x, y];
                        temp[x - rTop.Left, y - rTop.Top] = colorTop;
                    }
                }
                return (IImageAdapterUnmanaged)temp;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Width or Height of the Image are negetive\n\r");
            }

        }
        #endregion Clip

        #endregion IImageAdapterUnmanaged
        #endregion Interfaces implementation
    }
}



