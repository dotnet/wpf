// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification.Model.Analytical
{
    #region Namespaces.
        using System;
        using System.Xml;
        using System.Drawing;
        using System.Collections;
        using System.Drawing.Imaging;
        using Microsoft.Test.RenderingVerification.Filters;
        using Microsoft.Test.RenderingVerification.Model.Analytical;
        using Microsoft.Test.RenderingVerification.UnmanagedProxies;
    #endregion Namespaces.

    #region EventArgs deriving class (for custom events)
        /// <summary>
        /// Custom EventArg for Feeback event
        /// </summary>
        public class FeedbackEventArgs: EventArgs
        {
            private string _text = string.Empty;
            /// <summary>
            /// Create an instance of hte feedbackEventArgs class
            /// </summary>
            /// <param name="textValue"></param>
            public FeedbackEventArgs(string textValue)
            {
                _text = textValue;
            }
            /// <summary>
            /// Convert argument to string
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Text;
            }
            /// <summary>
            /// The text to be send as feeback
            /// </summary>
            /// <value></value>
            public string Text
            {
                get
                {
                    return _text;
                }
                set
                {
                    _text = value;
                }
            }
        }
        /// <summary>
        /// Custom EventArg for Feeback Broadcast
        /// </summary>
        public class BroadcastEventArgs: EventArgs
        {
            private string _reason = string.Empty;
            /// <summary>
            /// Create an instance of hte feedbackEventArgs class
            /// </summary>
            /// <param name="reason"></param>
            public BroadcastEventArgs(string reason)
            {
                _reason = reason;
            }
            /// <summary>
            /// Convert argument to string
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return Reason;
            }
            /// <summary>
            /// The reason for the broadcast
            /// </summary>
            /// <value></value>
            public string Reason
            {
                get
                {
                    return _reason;
                }
                set
                {
                    _reason = value;
                }
            }
        }
    #endregion EventArgs deriving class (for custom events)

    #region Delegates
        /// <summary>
        /// This delegate can be used to provide feedback.
        /// </summary>
        public delegate void FeedbackEventHandler(object sender, FeedbackEventArgs e);
        /// <summary>
        /// Broadcast handler
        /// </summary>
        public delegate void BroadcastEventHandler(object sender, BroadcastEventArgs e);
    #endregion Delegates

    /// <summary>
    /// Provides visual scanning functionality.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class VScan: IVScanUnmanaged
    {
        #region Events.
            /// <summary>
            /// This event is fired when the engine has feedback about progress.
            /// </summary>
            public event FeedbackEventHandler Feedback;
        #endregion Events.

        #region Private data.
            // Static Private Properties
            private static ArrayList mVScan = new ArrayList();
            // Per-instance private properties.
            private string _name = string.Empty;
            private bool _debug = false;
            private ArrayList _filterPipe = new ArrayList();
            private ModelManager2 _originalData = null;
            private ModelManager2 _filteredData = null;
        #endregion Private data.   

        #region Public data.
            /// <summary>
            /// Get the Filter pipeline.
            /// </summary>
            public ArrayList FilterPipe
            {
                get
                {
                    return _filterPipe;
                }
            }
            /// <summary>
            /// Get / set the debug flag for verbose output
            /// </summary>
            public bool Debug
            {
                get 
                {
                    return _debug;
                }
                set 
                {
                    _debug = value;
                }
            }
            /// <summary>
            /// Container for the unfiltered bitmaps
            /// </summary>
            public ModelManager2 OriginalData 
            {
                get 
                {
                    return _originalData;
                }
            }
            /// <summary>
            /// Container for the filtered bitmaps
            /// </summary>
            public ModelManager2 FilteredData
            {
                get 
                {
                    return _filteredData;
                }
            }
        #endregion Public data.

        #region Constructors.
            /// <summary>
            /// Create an instance of VScan with not image
            /// </summary>
            public VScan()
            {
                VScan.mVScan.Add(this);
                _originalData = new ModelManager2();
                _filteredData = new ModelManager2();
            }
            /// <summary>
            /// Creates a new VScan instance, initialized with the image
            /// loaded from the specified filename.
            /// </summary>
            /// <param name="packageName">Name of image file.</param>
            public VScan(string packageName) : this()
            {
                Package package = null;
                try
                {
                    package = Package.Load(packageName);
                    if (package.PackageCompare == PackageCompareTypes.ImageCompare)
                    {
                        OriginalData.Image = new ImageAdapter(package.MasterBitmap);
                        if (package.IsFailureAnalysis)
                        {
                            FilteredData.Image = new ImageAdapter(package.CapturedBitmap);
                        }
                    }
                    else 
                    {
                        if (package.PackageCompare == PackageCompareTypes.ModelAnalytical)
                        {
                            OriginalData.Image = new ImageAdapter((package.IsFailureAnalysis) ? package.CapturedBitmap : package.MasterBitmap) ;
                            OriginalData.Analyze();
                            FilteredData.Descriptors.Deserialize(package.MasterModel);
                            OriginalData.UpdateModel(FilteredData);
                            // reset Filtered data
                            FilteredData.Image = OriginalData.Image;
                        }
                        else 
                        {
                            // TODO : Model Synthetical
                        }
                    }
                }
                finally
                {
                    if (package != null) { package.Dispose(); package = null; }
                }
            }
            /// <summary>
            /// Creates a new VScan instance, initialized with the specified image.
            /// </summary>
            /// <param name="bmp">Image to use.</param>
            public VScan(Bitmap bmp) : this()
            {
                OriginalData.Image = new ImageAdapter(bmp);
                FilteredData.Image = new ImageAdapter(bmp);
            }
            /// <summary>
            /// Creates a new VScan instance, initialized with the specified IImageAdapter
            /// </summary>
            /// <param name="imageAdapter">The IImageAdapter to use</param>
            public VScan(IImageAdapter imageAdapter) : this()
            {
                OriginalData.Image = (IImageAdapter)imageAdapter.Clone();
                FilteredData.Image = (IImageAdapter)imageAdapter.Clone();
            }
        #endregion Constructors.

        #region Public Methods
            /// <summary>
            /// Returns colors that match the specified color given the 
            /// colorThreshold.
            /// </summary>
            /// <param name="baseColor">Color to calculate distance from.</param>
            /// <param name="colorThreshold">Color threshold.</param>
            /// <returns>An array, possible empty, of colors that match the specified color.</returns>
            /// <remarks>
            /// This method allows the engine to change the algorithm and still
            /// allow tools to provide feedback when tuning the algorithm
            /// arguments.
            /// </remarks>
            public static Color[] ListSampleColors(Color baseColor, int colorThreshold)
            {
                Color[] result = new Color[3];
                int red = baseColor.R;
                int green = baseColor.G;
                int blue = baseColor.B;
                result[0] = Color.FromArgb(AdjustForMaxThreshold(red, colorThreshold), green, blue);
                result[1] = Color.FromArgb(red, AdjustForMaxThreshold(green, colorThreshold), blue);
                result[2] = Color.FromArgb(red, green, AdjustForMaxThreshold(blue, colorThreshold));
                return result;
            }
            /// <summary>
            /// Apply the filters to the original image, output it to the filteredimage 
            /// </summary>
            public void ProcessFilters()
            {   
                if (OriginalData.Image == null)
                {
                    throw new InvalidOperationException("No bitmap available for this operation, load a bitmap or take screenshot first");
                }

                IImageAdapter imageAdapter = OriginalData.Image;
                for (int t = 0; t < _filterPipe.Count; t++)
                {
                    Filter filter = _filterPipe[t] as Filter;
                    if (filter == null)
                    {
                        throw new RenderingVerificationException("Unsupported type passed to FilterPipe (" + _filterPipe[t].GetType().ToString() + "). Type should be deriving from Filter.");
                    }
                    DebugLog("VScan is applying filter " + filter.ToString() + "...");
                    imageAdapter = filter.Process(imageAdapter);
                }
                FilteredData.Image = imageAdapter;
            }
            /// <summary>
            /// Take a snaphot of the screen bounded by rectangle
            /// </summary>
            public void ScreenSnapshot(Rectangle rectangle)
            {
                Bitmap bmpCapture = ImageUtility.CaptureScreen(rectangle);
                OriginalData.Image = new ImageAdapter(bmpCapture);
                FilteredData.Image = new ImageAdapter(bmpCapture);
                bmpCapture.Dispose();
            }
            /// <summary>
            /// Takes a Snapshot of the HWND screen area
            /// </summary>
            /// <param name="HWND">The HWND of the window to take a snapshot for</param>
            /// <param name="clientAreaOnly">Direct to get the client area or the full window</param>
            /// <returns></returns>
            /// Note : Passing an invalid HWND will throw an exception
            public void ScreenSnapshot(IntPtr HWND, bool clientAreaOnly)
            {
                Bitmap bmpCapture = ImageUtility.CaptureScreen(HWND, clientAreaOnly);//, out rect);
                OriginalData.Image = new ImageAdapter(bmpCapture);
                FilteredData.Image = new ImageAdapter(bmpCapture);
                bmpCapture.Dispose();
            }
            /// <summary>
            /// Load a new Bitmap (from file) as Unfiltered Bitmap
            /// </summary>
            /// <param name="fileLocation">The full path (path + name + extension) of the bitmap to load</param>
            public void LoadBmpFromFile(string fileLocation)
            {
                OriginalData.Image = new ImageAdapter(fileLocation);
                FilteredData.Image = (IImageAdapter)OriginalData.Image.Clone();
                GC.Collect(GC.MaxGeneration);
            }
            /// <summary>
            /// Logs a line of text (if Debug property is true).
            /// </summary>
            /// <param name="textValue">Text to log.</param>
            public void DebugLog(string textValue)
            {
                if (Feedback != null)
                {
                    Feedback(this, new FeedbackEventArgs(textValue));
                }
                else
                {
                    if (_debug)
                    {
                        System.Console.WriteLine(textValue);
                    }
                }
            }
        #endregion Public Methods

        #region Private Methods
            /// <summary>
            /// Attempts to apply the maximum possible threshold to a color 
            /// channel.
            /// </summary>
            /// <param name="colorValue">Color channel value.</param>
            /// <param name="colorThreshold">Threshold to apply to channel value.</param>
            /// <returns>The altered color channel value.</returns>
            private static int AdjustForMaxThreshold(int colorValue, int colorThreshold)
            {
                int deltaBelow = (colorThreshold > colorValue) ? colorValue : colorThreshold;
                int deltaAbove = (colorThreshold > 255 - colorValue) ? 255 - colorValue : colorThreshold;
                if (deltaBelow > deltaAbove)
                {
                    return colorValue - deltaBelow;
                }
                else
                {
                    return colorValue + deltaAbove;
                }
            }
        #endregion Private Methods

        #region IVScanUnmanaged extra stuff
/*        
            /// <summary>
            /// Get / Set OriginalData.BitmapResource
            /// </summary>
            IntPtr IVScanUnmanaged.HBitmap
            {
                get
                {
                    Bitmap bmp = ImageUtility.ToBitmap(OriginalData.Image);
                    return bmp.GetHbitmap();
                }
                set
                {
                    Bitmap bmp = System.Drawing.Bitmap.FromHbitmap(value);
                    OriginalData.Image = new ImageAdapter(bmp);
                    bmp.Dispose();
                }
            }
            /// <summary>
            /// Get the OriginalData.BitmapResource.Width
            /// </summary>
            int IVScanUnmanaged.BitmapWidth
            {
                get { return OriginalData.Image.Width; }
            }
            /// <summary>
            /// Get the OriginalData.BitmapResource.Height
            /// </summary>
            int IVScanUnmanaged.BitmapHeight
            {
                get { return OriginalData.Image.Height; }
            }
            /// <summary>
            /// Get OriginalData
            /// </summary>
//            IDataWrapperUnmanaged IVScanUnmanaged.OriginalData
            IModelManager2Unmanaged IVScanUnmanaged.OriginalData
            { 
                get  { return OriginalData; }
            }
*/
            /// <summary>
            /// Saves OriginalData.BitmapResource 
            /// </summary>
            /// <param name="fileName">File Name to save the BMP</param>
            void IVScanUnmanaged.Save (string fileName)
            {
                ImageUtility.ToImageFile(OriginalData.Image, fileName);
            }

            /// <summary>
            /// Load BMP from the file into OriginalData
            /// </summary>
            /// <param name="fileName">File Name to load the BMP from</param>
            /// <returns>Returns the loaded BMP</returns>
            IImageAdapterUnmanaged  IVScanUnmanaged.Load (string fileName)
            {
                LoadBmpFromFile (fileName);
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Take a snasphot of the screen bounded by rect
            /// </summary>
            /// <param name="x">Left of the rect</param>
            /// <param name="y">Top of the rect</param>
            /// <param name="width">Width of the rect</param>
            /// <param name="height">Height of the rect</param>
            /// <returns>Returns the captured BMP</returns>
            IImageAdapterUnmanaged IVScanUnmanaged.ScreenSnapshot (int x, int y, int width, int height)
            {
                ScreenSnapshot(new Rectangle (x, y, width, height));
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Take a snapshot of the screen bounded by rect. 
            /// </summary>
            /// <param name="rc">the rect</param>
            /// <returns>Returns the captured BMP</returns>
            IImageAdapterUnmanaged IVScanUnmanaged.ScreenSnapshotRc (RECT rc)
            {
                ScreenSnapshot ( new Rectangle(rc.Left, rc.Top, (rc.Right - rc.Left), (rc.Bottom - rc.Top)));
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Takes a Snapshot of the HWND screen area
            /// The bouding Rectangle for the HWND (client for full window depending on the value passed to 'clientOnlyArea'
            /// </summary>
            /// <param name="HWND">The HWND of the window to take a snapshot for</param>
            /// <param name="clientAreaOnly">Direct to get the client area or the full window</param>
            /// <returns>Returns the captured BMP</returns>
            /// Note : Passing an invalid HWND will throw an exception
            IImageAdapterUnmanaged IVScanUnmanaged.ScreenSnapshotWnd (IntPtr HWND, bool clientAreaOnly)
            {
                ScreenSnapshot(HWND, clientAreaOnly);
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Retrieve the Bitmap associated with a DC
            /// </summary>
            /// <param name="HDC">The HDC to query</param>
            /// <param name="x">x of the area to grab from the HDC</param>
            /// <param name="y">y of the area to grab from the HDC</param>
            /// <param name="width">width of the area to grab from the HDC</param>
            /// <param name="height">height of the area to grab from the HDC</param>
            /// <returns>Returns the associated BMP</returns>
            IImageAdapterUnmanaged IVScanUnmanaged.ScreenSnapshotDc(IntPtr HDC, int x, int y, int width, int height)
            {
                using (Bitmap bmp = ImageUtility.CaptureBitmapFromDC(HDC, new Rectangle(x, y, width, height)))
                {
                    OriginalData.Image = new ImageAdapter(bmp);
                }
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Get the bitmap associated with a DC in the specified area
            /// </summary>
            /// <param name="HDC">(IntPtr) The HDC associated with the Bitmap</param>
            /// <param name="areaToCopy">(RECT) The area to copy</param>
            /// <returns>Returns the associated BMP</returns>
            IImageAdapterUnmanaged IVScanUnmanaged.ScreenSnapshotDcRc(IntPtr HDC, RECT areaToCopy)
            {
                using (Bitmap bmp = ImageUtility.CaptureBitmapFromDC(HDC, new Rectangle(areaToCopy.Left, areaToCopy.Top, areaToCopy.Right - areaToCopy.Left, areaToCopy.Bottom - areaToCopy.Top)))
                {
                    OriginalData.Image = new ImageAdapter(bmp);
                }
                return (IImageAdapterUnmanaged)OriginalData.Image;
            }

            /// <summary>
            /// Return the ModelManager
            /// </summary>
            IModelManager2Unmanaged IVScanUnmanaged.OriginalModel
            {
                get
                {
                    return (IModelManager2Unmanaged)OriginalData;
                }
            }

/*
            /// <summary>
            /// Apply Single Alpha to OriginalData.BitmapResource
            /// </summary>
            /// <param name="alpha">Alpha value (between 0 and 1)</param>
            void IVScanUnmanaged.ApplySingleAlpha (double alpha)
            {
                if (alpha > 1.0 || alpha < 0)
                {
                    throw new ArgumentOutOfRangeException ("alpha must be between 0.0 and 1.0 (this is percentage)");
                }

                IColor argb = ColorByte.Empty;
                for (int x = 0; x < OriginalData.Image.Width; x++)
                {
                    for (int y = 0; y < OriginalData.Image.Height; y++)
                    {
                        argb = OriginalData.Image[x, y];
                        argb.ExtendedAlpha *= alpha;
                    }
                }
            }
*/
        #endregion IVScanUnmanaged extra stuff

    }
}
