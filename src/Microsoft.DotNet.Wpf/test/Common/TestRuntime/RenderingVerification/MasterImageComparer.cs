// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
namespace Microsoft.Test.RenderingVerification
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading;
    using System.ComponentModel;

    using System.Runtime.InteropServices;

    /// <summary>
    /// Helper to ease image comparison
    /// </summary>
    public class MasterImageComparer : IDisposable
    {
        #region Private Members

        private const string PACKAGE_EXTENSION = ".vscan";
        private const string IMAGE_EXTENSION = ".tif";

        /// <summary>
        /// Callback being called by the framework once the hwnd is visible (but not necessary rendered)
        /// </summary>
        public event DoWorkEventHandler WaitForRendering;
        /// <summary>
        /// Callback called by the framework just before taking the snapshot (so user can hover, click, ...)
        /// </summary>
        public event DoWorkEventHandler BeforeSnapshot;

        private string _failurePackagePath;
        private int _counter = 0;
        private int _originalBlinkTime = -1;
        private int _currentBlinkTime = -1;
        private ImageComparisonSettings _toleranceSettings = ImageComparisonSettings.Default;
        private MasterIndex _masterIndex = null;
        private bool _resizeWindowForDpi = true; //default
        private bool _stabilizeWindowBeforeCapture = true;

        #endregion

        #region Public Properties

        /// <summary>
        /// Path where the failure package should be created
        /// </summary>
        public string FailurePackagePath
        { 
            get { return _failurePackagePath; }
            set 
            {
                if (string.IsNullOrEmpty(value)) { throw new ArgumentException("FailurePackagePath cannot be null or empty"); }
                if (!System.IO.Directory.Exists(value)) { throw new System.IO.DirectoryNotFoundException("Specified Path ('" + value + "') not found"); }
                // TODO ? check if can write to path (dvd-rom, protected path, ...);
                value = value.Trim();
                if( ! value.EndsWith("\\")) { value += "\\"; } 
                _failurePackagePath = value;
            }
        }

        /// <summary>
        /// Get /set the caret blink time.
        /// </summary>
        public int CaretBlinkTimeMs
        {
            get { return _currentBlinkTime; }
            set
            {
                if (_currentBlinkTime == value) { return; }
                _currentBlinkTime = value;
                User32.SetCaretBlinkTime(value);
            }
        }

        /// <summary>
        /// Get/set the tolerance to be applied before comparing
        /// </summary>
        public ImageComparisonSettings ToleranceSettings
        {
            get { return _toleranceSettings; }
            set 
            {
                if (value == null) { throw new ArgumentNullException("ToleranceSettings", "Must be set to a valid instance (null passed in)"); }
                _toleranceSettings = value; 
            }
        }

        /// <summary>
        /// Return the MasterIndex (passed at construction time by the user)
        /// </summary>
        public MasterIndex MasterIndex
        {
            get { return _masterIndex; }
        }

        /// <summary>
        /// Get/Set to determine if we should scale the bitmap b/c of dpi
        /// </summary>
        public bool ResizeWindowForDpi
        {
            get { return _resizeWindowForDpi; }
            set { _resizeWindowForDpi = value; }
        }

        /// <summary>
        /// Determine if we should stabilize the test window (move to 0,0 and resize)
        /// </summary>
        public bool StabilizeWindowBeforeCapture
        {
            get { return _stabilizeWindowBeforeCapture; }
            set { _stabilizeWindowBeforeCapture = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create an instance of the object
        /// </summary>
        private MasterImageComparer()
        {
            _failurePackagePath = ".\\";
            _originalBlinkTime = User32.GetCaretBlinkTime();
            _currentBlinkTime = unchecked((int)uint.MaxValue);
            CaretBlinkTimeMs = _currentBlinkTime;
            User32.SetProcessDPIAware();

        }

        /// <summary>
        /// Create an instance of the object
        /// </summary>
        /// <param name="masterIndex">The MasterIndex to use</param>
        public MasterImageComparer(MasterIndex masterIndex) : this()
        {
            if (masterIndex == null) { throw new ArgumentNullException("masterIndex", "Cannot be null"); }
            _masterIndex = masterIndex;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Destructor (will execute - eventually - if IDisposable.Dispose not called)
        /// </summary>
        ~MasterImageComparer()
        {
            Cleanup();
        }

        /// <summary>
        /// Perform a synchronous comparison
        /// </summary>
        /// <param name="hwnd">The hwnd of the window to capture</param>
        /// <returns>returns true on success, false on failure</returns>
        public bool Compare(IntPtr hwnd)
        {
            return Compare(hwnd, System.Drawing.Rectangle.Empty);
        }

        /// <summary>
        /// Perform a synchronous comparison
        /// </summary>
        /// <param name="hwnd">The hwnd of the window to capture</param>
        /// <param name="rect">The area to capture within the hwnd </param>
        /// <returns>returns true on success, false on failure</returns>
        public bool Compare(IntPtr hwnd, System.Drawing.Rectangle rect)
        {
            using (AsyncData asyncData = CommonCompare(hwnd, rect, null))
            {
                if (asyncData.MasterImage != null) { DoVscanCompare(asyncData); }
                return asyncData.Result.Succeeded;
            }
        }

        /// <summary>
        /// Perform a synchronous comparison
        /// </summary>
        /// <param name="uiElement">The UIElement to capture</param>
        /// <returns>returns true on success, false on failure</returns>
        public bool Compare(System.Windows.UIElement uiElement)
        {
            System.Drawing.Rectangle rect = System.Drawing.Rectangle.Empty;
            IntPtr hwnd = GetWin32InfoFromUIElement(uiElement, out rect);

            return Compare(hwnd, rect);
        }

        /// <summary>
        /// Perform an asynchronous image comparison
        /// </summary>
        /// <param name="hwnd">The hwnd of the window to capture</param>
        public void EnqueueCompare(IntPtr hwnd)
        {
            EnqueueCompare(hwnd, System.Drawing.Rectangle.Empty);
        }

        /// <summary>
        /// Perform an asynchronous image comparison
        /// </summary>
        /// <param name="hwnd">The hwnd of the window to capture</param>
        /// <param name="rect">The area to capture within the hwnd </param>
        public void EnqueueCompare(IntPtr hwnd, System.Drawing.Rectangle rect) 
        {
            AsyncData asyncData = CommonCompare(hwnd, rect ,new ManualResetEvent(false));
            AsyncHelper.Data.Add(asyncData);

            if (asyncData.MasterImage != null)  { ThreadPool.QueueUserWorkItem(DoVscanCompare, asyncData); }
            else { asyncData.SynchronizationObject.Set();  }
        }

        /// <summary>
        /// Perform an asynchronous image comparison
        /// </summary>
        /// <param name="uiElement">The name of the WPF element to capture</param>
        public void EnqueueCompare(System.Windows.UIElement uiElement)
        {
            System.Drawing.Rectangle rect = System.Drawing.Rectangle.Empty;
            IntPtr hwnd = GetWin32InfoFromUIElement(uiElement, out rect);

            EnqueueCompare(hwnd, rect);
        }

        /// <summary>
        /// Block the calling thread until all enqueued comparison are evaluated.
        /// </summary>
        /// <returns>Returns a collection of ImageComparisonResult containing the result of each comparison</returns>
        public static ImageComparisonResult[] WaitForAllEnqueuedCompare()
        {
            ImageComparisonResult[] retVal = new ImageComparisonResult[] { };

            WaitHandle[] waitHandles = AsyncHelper.WaitHandles;
            if (waitHandles.Length != 0)
            {
                // Wait for Enqueued operation to finish.
                WaitHandle.WaitAll(waitHandles);

                retVal = AsyncHelper.Results;
                AsyncHelper.ClearAll();
            }

            return retVal;
        }

        /// <summary>
        /// Release native memory and cleanup
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private Methods

        private AsyncData CommonCompare(IntPtr hwnd, System.Drawing.Rectangle rect, ManualResetEvent resetEvent)
        {
            _counter++;

            Bitmap masterImage = _masterIndex.Resolve();
            string masterName = string.Empty;
            if (masterImage != null)
            {
                masterName = _masterIndex.ResolvedMasterName;
                Microsoft.Test.Logging.GlobalLog.LogStatus("Picking master '" + masterName + "'");
            }
            else 
            {
                Microsoft.Test.Logging.GlobalLog.LogStatus("No Matching master found !");
            }

            if (_stabilizeWindowBeforeCapture)
            {
                StabilizeWindow(hwnd, masterImage);
            }

            Bitmap actualImage;
            if (rect != System.Drawing.Rectangle.Empty)
            {
                actualImage = SnapshotRect(rect);
            }
            else
            {
                actualImage = SnapshotWindow(hwnd);
            }

            if (masterImage == null)
            {
                Microsoft.Test.Logging.GlobalLog.LogStatus("Encoding information in new master");

                MasterMetadata metadata = ImageMetadata.MetadataFromImage(actualImage);
                metadata._criteria.Clear();
                metadata._criteria.AddRange(_masterIndex.GetCurrentCriteriaValue().Keys);
                ImageMetadata.SetMetadataToImage(metadata, actualImage);

                // Save rendered as actual
                string actualName = ".\\Actual_" + _counter + IMAGE_EXTENSION;
                actualImage.Save(actualName, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(actualName);

                // Save render with new name already set
                actualName = _masterIndex.GetNewMasterName();
                Microsoft.Test.Logging.GlobalLog.LogStatus("Saving master as '" + System.IO.Path.GetFileName(actualName) + "'");
                actualImage.Save(actualName, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(actualName);
            }

            return new AsyncData(masterImage, masterName, actualImage, resetEvent, _toleranceSettings, _counter);

        }

        private void DoVscanCompare(object asyncData)
        {
            AsyncData data = asyncData as AsyncData;
            if (data == null) { throw new ArgumentException("Parameter passed in to the Method not of type AsyncData (or null)", "asyncData"); }

            ImageComparator ic = new ImageComparator();
            ic.Curve.CurveTolerance.LoadTolerance(data.ToleranceSettings.XmlNodeTolerance);

            IImageAdapter masterAdapter = new ImageAdapter(data.MasterImage);
            IImageAdapter capturedAdapter = new ImageAdapter(data.CapturedImage);

            // compare Master to the Capture image using the Compare overload that will scale the images size accounting for the DPI
            data.Result.Succeeded = ic.Compare(masterAdapter, MetadataInfoHelper.GetDpi(masterAdapter), capturedAdapter, MetadataInfoHelper.GetDpi(capturedAdapter), false);
            if (data.Result.Succeeded == false) { Microsoft.Test.Logging.GlobalLog.LogStatus("Regular comparison failed"); }
            // On filaure, check if user whats to filter the image ( IgnoreAntiAliasing will do )
            IImageAdapter masterFiltered = null;
            IImageAdapter captureFiltered = null;
            if (data.Result.Succeeded == false && data.ToleranceSettings.Filter != null)
            {
                // first save error diff image
                string errorDiffName = ".\\ErrorDiff_" + data.Index + IMAGE_EXTENSION;
                ImageUtility.ToImageFile(ic.GetErrorDifference(ErrorDifferenceType.IgnoreAlpha), errorDiffName);
                Microsoft.Test.Logging.GlobalLog.LogFile(errorDiffName);

                // Compare failed, filter the images and retry
                Microsoft.Test.Logging.GlobalLog.LogStatus("Filtering and recompare");
                masterFiltered = data.ToleranceSettings.Filter.Process(masterAdapter);
                captureFiltered = data.ToleranceSettings.Filter.Process(capturedAdapter);
                data.Result.Succeeded = ic.Compare(masterFiltered, captureFiltered, false);
                if (data.Result.Succeeded == false) { Microsoft.Test.Logging.GlobalLog.LogStatus("==> Filtered comparison failed as well"); }
            }

            if (data.Result.Succeeded )
            {
                Microsoft.Test.Logging.GlobalLog.LogStatus("Comparison SUCCEEDED.");
            }
            else
            {
                // Save Masters * filtered master for easy analysis
                string masterName = ".\\Master_" + data.Index + IMAGE_EXTENSION;
                data.MasterImage.Save(masterName, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(masterName);
                if (masterFiltered != null)
                {
                    string filteredMasterName = ".\\MasterFiltered_" + data.Index + IMAGE_EXTENSION;
                    using (Bitmap filteredMaster = ImageUtility.ToBitmap(masterFiltered))
                    {
                        SetMetadataToImage(filteredMaster);
                        filteredMaster.Save(filteredMasterName, System.Drawing.Imaging.ImageFormat.Tiff);
                    }
                    Microsoft.Test.Logging.GlobalLog.LogFile(filteredMasterName);
                }

                // Save rendered image (as "Actual_n") for easy analysis
                string capturedName = ".\\Actual_" + data.Index + IMAGE_EXTENSION;
                data.CapturedImage.Save(capturedName, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(capturedName);
                // Save actual filtered for easy analysis
                if (captureFiltered != null)
                {
                    string filteredRenderedName = ".\\ActualFiltered_" + data.Index + IMAGE_EXTENSION;
                    using (Bitmap filteredRendered = ImageUtility.ToBitmap(captureFiltered))
                    {
                        SetMetadataToImage(filteredRendered);
                        filteredRendered.Save(filteredRenderedName, System.Drawing.Imaging.ImageFormat.Tiff);
                    }
                    Microsoft.Test.Logging.GlobalLog.LogFile(filteredRenderedName);
                }

                // Master might need to be updated, save with correct name and metadata
                //
                // In this image, encode full criteria 
                string name = System.IO.Path.GetFileName(data.MasterName);
                string originalName = name.Replace(IMAGE_EXTENSION, "_FullCtriteria" + IMAGE_EXTENSION);
                Microsoft.Test.Logging.GlobalLog.LogStatus("Saving master with all criteria (new master) as '" + originalName + "'");
                SetMetadataToImage(data.CapturedImage);
                data.CapturedImage.Save(originalName, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(originalName);
                //
                // In this image, encode only criteria that match the master
                string originalNameFull = name.Replace(IMAGE_EXTENSION, "_MatchingCriteria" + IMAGE_EXTENSION);
                Microsoft.Test.Logging.GlobalLog.LogStatus("Saving master with matching criteria encoded (to replace previous master) as '" + originalNameFull + "'");
                MasterMetadata metadata = ImageMetadata.MetadataFromImage(data.MasterImage);
                // Keep master Criteria but update its Description.
                IMasterDimension[] keys = new IMasterDimension[metadata.Description.Count];
                metadata.Description.Keys.CopyTo(keys,0);
                for (int t = 0; t < keys.Length; t++)
                {
                    metadata.Description[keys[t]] = keys[t].GetCurrentValue();
                }
                ImageMetadata.SetMetadataToImage(metadata, data.CapturedImage);
                data.CapturedImage.Save(originalNameFull, System.Drawing.Imaging.ImageFormat.Tiff);
                Microsoft.Test.Logging.GlobalLog.LogFile(originalNameFull );

                // first save error diff image
                string errorDiffFilterName = ".\\ErrorDiffFiltered_" + data.Index + IMAGE_EXTENSION;
                if (data.ToleranceSettings.Filter == null)
                {
                    // Not filter were applied, change name (so it's not confusing)
                    errorDiffFilterName = ".\\ErrorDiff_" + data.Index + IMAGE_EXTENSION;
                }
                ImageUtility.ToImageFile(ic.GetErrorDifference(ErrorDifferenceType.IgnoreAlpha), errorDiffFilterName);
                Microsoft.Test.Logging.GlobalLog.LogFile(errorDiffFilterName);

            }

            data.Result.IsCompleted = true;

            if (data.SynchronizationObject != null) 
            {
                data.SynchronizationObject.Set(); 
            }
        }

        private void SetMetadataToImage(Image image)
        {
            MasterMetadata metadata = ImageMetadata.MetadataFromImage(image);
            metadata._criteria.Clear();
            metadata._criteria.AddRange(_masterIndex.GetCurrentCriteriaValue().Keys);
            ImageMetadata.SetMetadataToImage(metadata, image);
        }

        private void StabilizeWindow(IntPtr hwnd, Image masterBmp)
        {
            DefaultWaitForRendering(hwnd);
            MoveWindow(hwnd, 0, 0, false);
            if (masterBmp != null) { FitToMaster(hwnd, masterBmp); }
            MoveMouse(0, 0, false);
        }

        private Bitmap SnapshotWindow(IntPtr hwnd)
        {
            // Allow user to do whatever just before snapshot (Hover, click, presskey, ...)
            if (BeforeSnapshot != null) { BeforeSnapshot(this, new DoWorkEventArgs(_counter)); }

            // Take Snapshot
            Microsoft.Test.Logging.GlobalLog.LogStatus("Taking snapshot");
            return ImageUtility.CaptureScreen(hwnd, true);
        }

        private Bitmap SnapshotRect(System.Drawing.Rectangle rect)
        {
            // Allow user to do whatever just before snapshot (Hover, click, presskey, ...)
            if (BeforeSnapshot != null) { BeforeSnapshot(this, new DoWorkEventArgs(_counter)); }

            // Take Snapshot
            Microsoft.Test.Logging.GlobalLog.LogStatus("Capturing rect : " + rect.ToString());
            return ImageUtility.CaptureScreen(rect);
        }

        private void DefaultWaitForRendering(IntPtr hwnd)
        {
            // Callback testCase if event is hooked up.
            if (WaitForRendering != null) { WaitForRendering(this, new DoWorkEventArgs(_counter)); }
            else { WaitForVisibleHwnd(hwnd, 1000); }            // TODO : The timeout should be customizable from test.
        }

        private void WaitForVisibleHwnd(IntPtr hwnd, int timeoutMs)
        {
            System.Runtime.InteropServices.HandleRef handleRef = new System.Runtime.InteropServices.HandleRef(null, hwnd);

            DateTime startTime = DateTime.Now;

            string errorMessage = string.Empty;
            do
            {
                if (Microsoft.Test.Win32.NativeMethods.IsWindow(handleRef) == false)
                {
                    errorMessage = "Window does not exist";
                }
                else
                {
                    if (Microsoft.Test.Win32.NativeMethods.IsWindowVisible(handleRef) == false)
                    {
                        errorMessage = "Window exist but is not visible";
                    }
                    else
                    {
                        if (User32.IsWindowMinimized(hwnd)) 
                        {
                            errorMessage = "Window is minimized"; 
                        }
                    }
                }
            } while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs && errorMessage != string.Empty) ;

            if (errorMessage != string.Empty) { throw new InvalidOperationException(errorMessage); }

        }

        private Microsoft.Test.Win32.NativeStructs.RECT GetWindowRect(IntPtr hwnd)
        {
            System.Runtime.InteropServices.HandleRef href = new System.Runtime.InteropServices.HandleRef(null, hwnd);

            Microsoft.Test.Win32.NativeStructs.RECT retval = new Microsoft.Test.Win32.NativeStructs.RECT();
            bool success = Microsoft.Test.Win32.NativeMethods.GetWindowRect(href, ref retval);
            if (!success) { throw new System.Runtime.InteropServices.ExternalException("Call to Win32 API ('GetWindowRect') failed "); }

            return retval;
        }

        private System.Drawing.Rectangle/*Microsoft.Test.Win32.NativeStructs.RECT*/ GetClientWindowRect(IntPtr hwnd)
        {
            System.Drawing.Rectangle retval = User32.GetClientRect(hwnd);
 
            //System.Runtime.InteropServices.HandleRef href = new System.Runtime.InteropServices.HandleRef(null, hwnd);
            //Microsoft.Test.Win32.NativeStructs.RECT retval = new Microsoft.Test.Win32.NativeStructs.RECT();
            //bool success = Microsoft.Test.Win32.NativeMethods.GetWindowRect(href, ref retval);
            //if (!success) { throw new System.Runtime.InteropServices.ExternalException("Call to Win32 API ('GetWindowRect') failed "); }

            return retval;
        }

        private void FitToMaster(IntPtr hwnd ,Image masterImage) 
        {
            // TODO : Determine window size using ClientTestRuntime APIs
            System.Drawing.Rectangle /*Microsoft.Test.Win32.NativeStructs.RECT*/ rect = GetClientWindowRect(hwnd);
            StretchForDifferentDpi(hwnd, rect.Width, rect.Height, masterImage);
        }

        private void StretchForDifferentDpi(IntPtr hwnd, int width, int height, Image masterImage)
        {            
            Size offset =  new Size(masterImage.Width - width, masterImage.Height - height);

            //The original application of this method assumes that a master image was captured on a machine 
            //set to 96dpi and scales the window size appropriately.  However, if a master was actually captured on a 
            //machine that is set to a dpi other than 96, we want to provide the option not to automatically scale the Window.
            if (_resizeWindowForDpi)
            {
                IMasterDimension currentDpi = MasterMetadata.GetDimension("DpiDimension");
                int standardDpi = 96;
                if (currentDpi.GetCurrentValue() != standardDpi.ToString())
                {
                    int dpi = int.Parse(currentDpi.GetCurrentValue());

                    float ratio = (float)dpi / standardDpi;
                    offset.Width = (int)(masterImage.Width * ratio - width + .5);
                    offset.Height = (int)(masterImage.Height * ratio - height + .5);
                }
            }

            if (offset.IsEmpty) { return; }

            ResizeWindow(hwnd, offset.Width, offset.Height, true);
        }

        private void MoveMouse(int x, int y, bool relativeOffsets)
        {
            // TODO : Allow framework to use Win32 calls, user emulation or UIAutomation
            Point pt = User32.GetCursorPos();

            int dx = x;
            int dy = y;

            if (relativeOffsets)
            {
                dx = pt.X + x;
                dy = pt.Y + y;
            }

            if (dx == pt.X && dy == pt.Y) { return; }

            Microsoft.Test.Input.Input.SendMouseInput((double)dx, (double)dy, 0, Microsoft.Test.Input.SendMouseInputFlags.Move | Microsoft.Test.Input.SendMouseInputFlags.Absolute);
        }

        private void MoveWindow(IntPtr hwnd, int x, int y, bool relativeOffsets)
        {
            System.Runtime.InteropServices.HandleRef href = new System.Runtime.InteropServices.HandleRef(null, User32.GetAncestor(hwnd, User32.GA_FlagEnum.GA_ROOT));

            // Get current width & height
            Microsoft.Test.Win32.NativeStructs.RECT rect = GetWindowRect(hwnd);

            int dx = x;
            int dy = y;

            if (relativeOffsets)
            {
                dx = rect.right + x;
                dy = rect.bottom + y;
            }

            if (dx == rect.left && dy == rect.top) { return; }

            Microsoft.Test.Win32.NativeStructs.RECT parentRect = GetWindowRect(href.Handle);
            bool success = Microsoft.Test.Win32.NativeMethods.MoveWindow(href, dx, dy, parentRect.Width, parentRect.Height, true);
            if (!success) { throw new System.Runtime.InteropServices.ExternalException("Call to Win32 API ('MoveWindow') failed "); }
        }

        private void ResizeWindow(IntPtr hwnd, int x, int y, bool relativeOffsets)
        {
            System.Runtime.InteropServices.HandleRef href = new System.Runtime.InteropServices.HandleRef(null, User32.GetAncestor(hwnd, User32.GA_FlagEnum.GA_ROOT));

            // Get current width & height
            Microsoft.Test.Win32.NativeStructs.RECT parentRect = GetWindowRect(href.Handle);
            Microsoft.Test.Win32.NativeStructs.RECT rect = GetWindowRect(hwnd);

            int dx = x + parentRect.Width - rect.Width;
            int dy = y + parentRect.Height - rect.Height;

            if (relativeOffsets)
            {
                dx = parentRect.right + x;
                dy = parentRect.bottom + y;
            }

            if (dx == rect.left && dy == rect.top) { return; }

            bool success = Microsoft.Test.Win32.NativeMethods.MoveWindow(href, parentRect.left, parentRect.top, dx, dy, true);
            Microsoft.Test.Threading.DispatcherHelper.DoEvents(100);
            if (!success) { throw new System.Runtime.InteropServices.ExternalException("Call to Win32 API ('MoveWindow') failed "); }
        }

        private void Cleanup()
        {
            User32.SetCaretBlinkTime(_originalBlinkTime);
        }

        private IntPtr GetWin32InfoFromUIElement(System.Windows.UIElement uiElement, out System.Drawing.Rectangle rect)
        {
            // Convert UIElement to Rectangle
            System.Windows.Media.Matrix matrix;
            System.Windows.PresentationSource presentationSource = System.Windows.PresentationSource.FromVisual(uiElement);
            if (presentationSource == null)
            {
                throw new InvalidOperationException("The specified UiElement is not connected to a rendering Visual Tree.");
            }
            try
            {
                System.Windows.Media.GeneralTransform generalTransform = uiElement.TransformToAncestor(presentationSource.RootVisual);
                System.Windows.Media.Transform transform = generalTransform as System.Windows.Media.Transform;
                if (transform == null)
                {
                    throw new ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change");
                }
                matrix = transform.Value;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The specified UiElement is not connected to a rendering Visual Tree.");
            }
            System.Windows.Rect rect1 = new System.Windows.Rect(new System.Windows.Point(), uiElement.RenderSize);
            rect1.Transform(matrix);
            System.Windows.Point point1 = rect1.TopLeft;
            point1 = presentationSource.CompositionTarget.TransformToDevice.Transform(point1);
            Point point2 = new Point((int)point1.X, (int)point1.Y);
            IntPtr hwnd = ((System.Windows.Interop.HwndSource)presentationSource).Handle; // PresentaionSource.RootVisual ?
            User32.ClientToScreen(hwnd, ref point2);
            rect = new System.Drawing.Rectangle(point2, new System.Drawing.Size(
                Convert.ToInt32(Microsoft.Test.Display.Monitor.ConvertLogicalToScreen(Microsoft.Test.Display.Dimension.Width, uiElement.RenderSize.Width)),
                Convert.ToInt32(Microsoft.Test.Display.Monitor.ConvertLogicalToScreen(Microsoft.Test.Display.Dimension.Height, uiElement.RenderSize.Height))));

            return hwnd;
        }

        #endregion
    }
}
