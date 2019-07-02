// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.UnmanagedProxies
{
    #region using
        using System;
        using System.Drawing;
        using System.ComponentModel;
        using System.Runtime.Serialization;
        using System.Runtime.InteropServices;
        using Microsoft.Test.Win32;
   #endregion using

    #region GDIRECT
    /// <summary>
    /// RECT struct for compatibility with GDI RECT )
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public struct RECT
    { 
        /// <summary>
        /// Left of the RECT (for compatibility with GDI RECT )
        /// </summary>
        public int Left;
        /// <summary>
        /// Top of the RECT (for compatibility with GDI RECT )
        /// </summary>
        public int Top;
        /// <summary>
        /// Right of the RECT (for compatibility with GDI RECT )
        /// </summary>
        public int Right;
        /// <summary>
        /// Bottom of the RECT (for compatibility with GDI RECT )
        /// </summary>
        public int Bottom;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="left">Left of the RECT</param>
        /// <param name="top">Top of the RECT</param>
        /// <param name="right">Right of the RECT</param>
        /// <param name="bottom">Bottom of the RECT</param>
        RECT (int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
    
    #endregion GDIRECT

    #region  ImageUtilityUnmanaged
    /// <summary>
    /// Interface for Native access
    /// </summary>
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public interface IImageUtilityUnmanaged
    {
        /// <summary>
        /// Get a snapshot of the screen
        /// </summary>
        /// <param name="x">Where to start the capture on the x axis</param>
        /// <param name="y">Where to start the capture on the y axis</param>
        /// <param name="width">The width of the rect to capture</param>
        /// <param name="height">The height of the rect to capture</param>
        /// <returns>Pointer to an HBitmap of the screen captured</returns>
        IImageAdapterUnmanaged ScreenSnapshotRc(int x, int y, int width, int height);
        
        /// <summary>
        /// Get a snapshot of the screen based on a HWND
        /// </summary>
        /// <param name="HWND">The HWND to capture</param>
        /// <param name="clientAreaOnly">Get the full window sor only the client area</param>
        /// <returns>Pointer to an HBitmap of the screen captured</returns>
        IImageAdapterUnmanaged ScreenSnapshotWnd(IntPtr HWND, bool clientAreaOnly);
        
        /// <summary>
        /// Get a snapshot of the screen based on an HDC
        /// </summary>
        /// <param name="HDC">Handle to the Drawing context</param>
        /// <param name="areaToCopy">Thre area to capture</param>
        /// <returns>Pointer to an HBitmap of the screen captured</returns>
        IImageAdapterUnmanaged ScreenSnapshotDc(IntPtr HDC, NativeStructs.RECT areaToCopy);
        
        /// <summary>
        /// Save the underlying bitmap (after ScreenCapture)
        /// </summary>
        /// <param name="image">The IImageAdapter to serialize</param>
        /// <param name="fileName">Path and name of the file containing the image to be saved</param>
        void ToImageFile(IImageAdapterUnmanaged image, string fileName);
    }
    #endregion ImageUtilityUnmanaged

    #region VscanUnmanaged
    /// <summary>
    /// Unmanaged interface, VScan class
    /// </summary>
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public interface IVScanUnmanaged
    {
        /// <summary>
        /// Take a snasphot of the screen bounded by rect
        /// </summary>
        /// <param name="x">Left of the rect</param>
        /// <param name="y">Top of the rect</param>
        /// <param name="width">Width of the rect</param>
        /// <param name="height">Height of the rect</param>
        /// <returns>Returns the captured BMP</returns>
        IImageAdapterUnmanaged ScreenSnapshot (int x, int y, int width, int height);
        
        /// <summary>
        /// Take a snapshot of the screen bounded by rect. 
        /// </summary>
        /// <param name="rc">the rect</param>
        /// <returns>Returns the captured BMP</returns>
        IImageAdapterUnmanaged ScreenSnapshotRc (RECT rc);
        
        /// <summary>
        /// Takes a Snapshot of the HWND screen area
        /// The bouding Rectangle for the HWND (client for full window depending on the value passed to 'clientOnlyArea'
        /// </summary>
        /// <param name="HWND">The HWND of the window to take a snapshot for</param>
        /// <param name="clientAreaOnly">Direct to get the client area or the full window</param>
        /// <returns>Returns the captured BMP</returns>
        /// Note : Passing an invalid HWND will throw an exception
        IImageAdapterUnmanaged ScreenSnapshotWnd (IntPtr HWND, bool clientAreaOnly);
        
        /// <summary>
        /// Retrieve the Bitmap associated with a DC
        /// </summary>
        /// <param name="HDC">The HDC to query</param>
        /// <param name="x">x of the area to grab from the HDC</param>
        /// <param name="y">y of the area to grab from the HDC</param>
        /// <param name="width">width of the area to grab from the HDC</param>
        /// <param name="height">height of the area to grab from the HDC</param>
        /// <returns>Returns the associated BMP</returns>
        IImageAdapterUnmanaged ScreenSnapshotDc (IntPtr HDC, int x, int y, int width, int height);
        
        /// <summary>
        /// Get the bitmap associated with a DC in the specified area
        /// </summary>
        /// <param name="HDC">(IntPtr) The HDC associated with the Bitmap</param>
        /// <param name="areaToCopy">(RECT) The area to copy</param>
        /// <returns>Returns the associated BMP</returns>
        IImageAdapterUnmanaged  ScreenSnapshotDcRc (IntPtr HDC, RECT areaToCopy);
        
        /// <summary>
        /// Load BMP from the file into OriginalData
        /// </summary>
        /// <param name="fileName">File Name to load the BMP from</param>
        /// <returns>Returns the loaded BMP</returns>
        IImageAdapterUnmanaged Load (string fileName);
        
        /// <summary>
        /// Saves OriginalData.BitmapResource 
        /// </summary>
        /// <param name="fileName">File Name to save the BMP</param>
        void Save (string fileName);
        
        /// <summary>
        /// Return the ModelManager
        /// </summary>
        IModelManager2Unmanaged OriginalModel 
        {
            get;
        }
    }
    #endregion VscanUnmanaged

    #region ImageComparatorUnmanaged
    /// <summary>
    /// Unmanaged interface, ImageComparator class
    /// </summary>
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public interface IImageComparatorUnmanaged
    {
        /// <summary>
        /// Compares the image histogram with the given tolerance. 
        /// Returns true if the histogram fits the tolerance.
        /// <param name="histogram">[in] image histogram (the error in % by energy level 0-255)</param>
        /// <param name="toleranceFileName">[out] the xml file name which contains tolerance</param>
        /// </summary>
        bool CompTolerance(double[] histogram, string toleranceFileName);
        
        /// <summary>
        /// create XML file with histogram values
        /// </summary>
        /// <param name="fileName">[out] .xml file name which contains a histogram</param>
        /// <param name="histogram">[in] histogram (the error in % by energy level 0-255)</param>
        void   SaveHistogram  (string fileName, double[] histogram);
        
        /// <summary>
        /// Compares two images in ARGB mode and 
        /// returns the result of the comparison (true if images are the same). 
        /// <param name="master">[in] master image</param>
        /// <param name="rendered">[in] rendered image</param>
        /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
        /// <param name="normializedErr">[out] the overall error / # of pixels</param>
        /// <param name="histoFileName">[out] .xml file name which contains a histogram (the error in % by energy level 0-255). null if not used</param>
        /// <param name="diffImgFileName">[out] .bmp file name which contains bitmap of the differences. null if not used</param>
        /// <returns>Returns true if if images are the same, false otherwise</returns>
        /// </summary>
        bool CompStrict(
            IImageAdapterUnmanaged master,
            IImageAdapterUnmanaged rendered,
            string vscanFileName, 
            ref float normializedErr,
            string histoFileName, 
            string diffImgFileName);
        
        /// <summary>
        /// Compares two images in ARGB mode with tolerance and 
        /// returns the result of the comparison (true if images are the same). 
        /// Saves .vscan package in case of failure
        /// <param name="master">[in] master image</param>
        /// <param name="rendered">[in] rendered image</param>
        /// <param name="toleranceFileName">[in] the xml file name which contains tolerance. null if not used</param>
        /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
        /// <param name="normializedErr">[out] the overall error / # of pixels</param>
        /// <param name="histoFileName">[out] .xml file name which contains a histogram (the error in % by energy level 0-255). null if not used</param>
        /// <param name="diffImgFileName">[out] .bmp file name which contains bitmap of the differences. null if not used</param>
        /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <returns>Returns true if if images are the same, false otherwise</returns>
        /// </summary>
        bool CompStrictOrToleranceSavePackage(
            IImageAdapterUnmanaged master,
            IImageAdapterUnmanaged rendered,
            string toleranceFileName, 
            string vscanFileName, 
            ref float normializedErr,
            string histoFileName, 
            string diffImgFileName,
            int rcToCompareLeft, 
            int rcToCompareTop, 
            int rcToCompareRight, 
            int rcToCompareBottom);
        
        /// <summary>
        /// Compares two images in ARGB mode and 
        /// returns the result of the comparison (true if images are the same). 
        /// <param name="master">[in] master image</param>
        /// <param name="rendered">[in] rendered image</param>
        /// <param name="toleranceFileName">[in] the xml file name which contains Tolerance. null if not used</param>
        /// <param name="vscanFileName">[out] .vscan file name - cab which will package failures</param>
        /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// </summary>
        bool CompLenient(IImageAdapterUnmanaged master,
             IImageAdapterUnmanaged rendered,
             string toleranceFileName, 
             string vscanFileName,            
             int rcToCompareLeft, 
             int rcToCompareTop, 
             int rcToCompareRight, 
             int rcToCompareBottom);
    }
    #endregion ImageComparatorUnmanaged

    #region ModelManager2Unmanaged
    /// <summary>
    /// Unmanaged interface, IModelManager2 class
    /// </summary>
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public interface IModelManager2Unmanaged
    {
        /// <summary>
        /// Create Model
        /// </summary>
        /// <param name="silhouetteTolerance">Silhouette tolerance</param>
        /// <param name="xTolerance">x shift tolerance</param>
        /// <param name="yTolerance">y shift tolerance</param>
        /// <param name="imgTolerance">image tolerance</param>
        /// <param name="colorTolerance">ARGB tolerance</param>
        void CreateModel(double silhouetteTolerance,
             double xTolerance,
             double yTolerance,
             double imgTolerance,
             IColor colorTolerance);

        /// <summary>
        /// Compares this(Rendered img) to model created for masterFileName
        /// </summary>
        /// <param name="masterImg">Contains master img</param>
        /// <param name="vscanFileName">.vscan file name - cab which will package failures</param>
        /// <param name="shapeTolerance">Shape tolerance</param>
        /// <param name="xTolerance">x shift tolerance</param>
        /// <param name="yTolerance">y shift tolerance</param>
        /// <param name="imgTolerance">image tolerance</param>
        /// <param name="a">A part of ARGB tolerance</param>
        /// <param name="r">R part of ARGB tolerance</param>
        /// <param name="g">G part of ARGB tolerance</param>
        /// <param name="b">B part of ARGB tolerance</param>
        /// <param name="rcToCompareLeft">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareTop">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareRight">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <param name="rcToCompareBottom">Rectangle-part of the image which will be compared. 0 if whole image will be compared</param>
        /// <returns>Returns true if every descriptor in the model was found (within tolerance), false otherwise</returns>
        bool CompareModelsSavePackage(
            IImageAdapterUnmanaged masterImg,
            string vscanFileName,
            double shapeTolerance,
            double xTolerance,
            double yTolerance,
            double imgTolerance,
            byte a,
            byte r,
            byte g,
            byte b,
            int rcToCompareLeft,
            int rcToCompareTop,
            int rcToCompareRight,
            int rcToCompareBottom);

        /// <summary>
        /// Get/set the internal image
        /// </summary>
        IImageAdapterUnmanaged ImageAdapter 
        { 
            get;
            set;
        }
    }
    #endregion ModelManger2Unmanaged

    #region ImageAdapterUnmanaged
    /// <summary>
    /// Unmanaged interface, ImageAdapter class
    /// </summary>
    [ComVisibleAttribute(true)]
    [BrowsableAttribute(false)]
    public interface IImageAdapterUnmanaged : IImageAdapter
    {
        /// <summary>
        /// Load an image as IImageAdapterUnmananged
        /// </summary>
        /// <param name="fileName">File name of the image to be loaded</param>
        void Load(string fileName);

        /// <summary>
        /// Serialize an IImageAdapterUnmananged 
        /// </summary>
        /// <param name="fileName">The filename of the image to be saved</param>
        void Save(string fileName);
        
        /// <summary>
        /// Multiply all the pixels with an alpha value.
        /// </summary>
        /// <param name="alpha">Alpha value(between 0 and 1)</param>
        /// <param name="image">BitmapResources to have Alpha applied on</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged ApplySingleAlpha(double alpha, IImageAdapterUnmanaged image);

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
        IImageAdapterUnmanaged Merge(IImageAdapterUnmanaged bmpBg,
            IImageAdapterUnmanaged bmpTop,
            int xBg, 
            int yBg,
            int xTop, 
            int yTop);

        /// <summary>
        /// Scales the image.
        /// </summary>
        /// <param name="resultWidth">The width of the image after scaling</param>
        /// <param name="resultHeight">The height of the image after scaling</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged Scale(int resultWidth, int resultHeight);

        /// <summary>
        /// Returns the width of the image
        /// </summary>
        /// <returns>The width</returns>
        int GetWidth();

        /// <summary>
        /// Returns the height of the image
        /// </summary>
        /// <returns>The height</returns>
        int GetHeight();

        /// <summary>
        /// Returns the colorref at the specified point in the image
        /// </summary>
        /// <param name="x">X coordinate of the point</param>
        /// <param name="y">Y coordinate of the point</param>
        /// <returns>Retruns an int which is same as COLORREF</returns>
        int GetPixel(int x, int y);

        /// <summary>
        /// Places one image on top of the other. The top image should fit within the bottom
        /// image. The top image completely cover the bottom image.
        /// </summary>
        /// <param name="bmpTop"></param>
        /// <param name="x">The x value of the point in the bottom image at which the left 
        /// hand top corner of the top image is placed</param>
        /// <param name="y">The y value of the point in the bottom image at which the left 
        /// hand top corner of the top image is placed</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged Overlay(
            IImageAdapterUnmanaged bmpTop, 
            int x, 
            int y);

        /// <summary>
        /// Clips the image at the RECT specified
        /// </summary>
        /// <param name="left">The left side of the RECT</param>
        /// <param name="top">The top side of the RECT</param>
        /// <param name="right">The right side of the RECT</param>
        /// <param name="bottom">The bottom side of the RECT</param>
        /// <returns>Returns an interface to the image</returns>
        IImageAdapterUnmanaged Clip(
            int left,
            int top,
            int right,
            int bottom);
    }
    #endregion ImageAdapterUnmanaged
}

