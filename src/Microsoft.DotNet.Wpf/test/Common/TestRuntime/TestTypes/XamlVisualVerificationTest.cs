// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using Microsoft.Test.Display;
using Microsoft.Test.Graphics;
using Microsoft.Test.Logging;
using Microsoft.Test.RenderingVerification;
using Microsoft.Test.Threading;


namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Performs a visual verification of a XAML file against a pre-rendered reference image.
    /// </summary>
    public static class XamlVisualVerificationTest
    {
        #region Public Methods and Properties
        /// <summary>
        /// Parses Args for path to master bmp and source xaml, as a limited scenario replacement to VScanLoader
        /// </summary>
        public static void Launch()
        {
            TestLog log = new TestLog("XamlVisual Verification Test");
            log.Result = TestResult.Unknown;
            String args = null;
            int windowsMediaPlayerVersion = 0;
            bool hasMedia = false;
            
            try
            {                
                args = DriverState.DriverParameters["Args"];
                log.LogStatus("Running with Arguments: " + args);

                masterDpiX = double.Parse(GetFlag(args, "DpiX"), System.Globalization.CultureInfo.InvariantCulture);
                masterDpiY = double.Parse(GetFlag(args, "DpiY"), System.Globalization.CultureInfo.InvariantCulture);
                xtcContainsDpiInfo = true;
            }
            catch (System.ArgumentException)
            {
                log.LogStatus("No master image Dpi info found in test definition");
            }
            catch
            {
                log.LogStatus("ERROR: Ignoring master image Dpi info due to parsing error");
            }

            DriverState.DriverParameters["ShowsNavigationUI"] = GetFlag(args, "ShowsNavigationUI");
            if (DriverState.DriverParameters["ShowsNavigationUI"] != null && String.Compare(DriverState.DriverParameters["ShowsNavigationUI"], "false", StringComparison.InvariantCultureIgnoreCase).Equals(0))
            {
                log.LogStatus("Xbap Navigation Chrome will be HIDDEN");
            }
            else
            {
                log.LogStatus("Xbap Navigation Chrome will be SHOWN");
            }

            // Parse IsHostedInNavigationWindow. 
            
            string isHostedInNavigationWindowStr = GetFlag(args, "IsHostedInNavigationWindow");
            if (!string.IsNullOrEmpty(isHostedInNavigationWindowStr) && string.Compare(isHostedInNavigationWindowStr, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                isHostedInNavigationWindow = false;
            }
            log.LogStatus("Hosted in Nabigation window: " + isHostedInNavigationWindow);

            // if windows media player is installed, lets get the version
            if (Microsoft.Test.Diagnostics.SystemInformation.WindowsMediaPlayerVersion != null)
            {
                windowsMediaPlayerVersion = ((System.Version)Microsoft.Test.Diagnostics.SystemInformation.WindowsMediaPlayerVersion).Major;
            }

            // get the containsMedia flag to determine if we need to check for media player 10+

            bool.TryParse(GetFlag(args, "containsMedia"), out hasMedia);

            if( hasMedia && (windowsMediaPlayerVersion < 10))
            {
                TestLog.Current.LogStatus("Windows Media Player version is not 10+ Ignoring media containing XAML case");
                TestLog.Current.Result = TestResult.Ignore;
            }
            else
            {
                try
                {
                    IImageAdapter testImageAdapter = PrepareXamlImageAdapter(GetFlag(args, "capture"));

                    IImageAdapter materImageAdapter = PrepareBitmapImageAdapter(GetFlag(args, "master"));

                    ((ImageAdapter)materImageAdapter).DpiX = masterDpiX;
                    ((ImageAdapter)materImageAdapter).DpiY = masterDpiY;

                    toleranceFilePath = GetFlag(args, "tolerance");

                    if (materImageAdapter == null)
                    {
                        log.LogStatus("Test Problem - master image adapter is null");
                    }
                    else if (testImageAdapter == null)
                    {
                        log.LogStatus("Test Problem - test image adapter is null");
                    }
                    else
                    {
                        //Run visual verification
                        if (Compare(testImageAdapter, materImageAdapter))
                        {
                            log.LogStatus("Test Pass - Image comparison is a match");
                            log.Result = TestResult.Pass;
                        }
                        else
                        {
                            log.LogStatus("Test Failure - Image comparison is a mismatch");
                            log.Result = TestResult.Fail;
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogStatus(e.ToString());
                }
            }

            log.Close();
        }

        /// <summary>
        ///  Time to wait for doing window events before taking screen shot, the xaml 
        ///  hosted in Window (not NavigationWindow).
        /// </summary>
        public static TimeSpan WaitTime
        {
            set
            {
                waitTime = value;
            }
            get
            {
                return waitTime;
            }
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Extract command-line parameter from argument string
        /// Operates on the /key=value format
        /// </summary>
        private static string GetFlag(string arguments, string key)
        {
            String result = null;
            String query = @"/" + key + @"=(?<value>\S+)";
            Match m = Regex.Match(arguments, query);
            if (m.Success)
            {
                result = m.Groups["value"].Value;
            }
            return result;
        }

        /// <summary>
        /// Perform the comparison operation
        /// This method abstracts out all the details of image comparison to a boolean result
        /// The basic assumption is that the default set of tolerances is adequate
        /// </summary>
        /// <param name="testImageAdapter"></param>
        /// <param name="masterImageAdapter"></param>
        /// <returns></returns>
        private static bool Compare(IImageAdapter testImageAdapter, IImageAdapter masterImageAdapter)
        {
            bool TestPassed = false;
            ImageComparator comparator = null;

            if (File.Exists(toleranceFilePath))
            {
                CurveTolerance tolerance = new CurveTolerance();
                tolerance.LoadTolerance(toleranceFilePath);
                comparator = new ImageComparator(tolerance);
                TestLog.Current.LogStatus("Using custom tolerance (" + toleranceFilePath  + ")");
            }
            else
            {
                comparator = new ImageComparator();
                TestLog.Current.LogStatus("Using default tolerance");
            }

            if (!xtcContainsDpiInfo)
            {
                // No master image dpi info found in test definition
                TestPassed = comparator.Compare(masterImageAdapter, testImageAdapter, true);
            }
            else
            {
                TestPassed = comparator.Compare(masterImageAdapter, // master image adapter
                                                new Point((int)Math.Round(masterImageAdapter.DpiX), (int)Math.Round(masterImageAdapter.DpiY)),  // master image dpi info
                                                testImageAdapter,  // test image adapter
                                                new Point((int)Math.Round(testImageAdapter.DpiX), (int)Math.Round(testImageAdapter.DpiY)),  // test image dpi info
                                                true); // populateMismatchingPoints
            }

            if (!TestPassed)
            {
                Package package = Package.Create(".\\FailurePackage.vscan",
                                                 ImageUtility.ToBitmap(masterImageAdapter),
                                                 ImageUtility.ToBitmap(testImageAdapter),
                                                 comparator.Curve.CurveTolerance.WriteToleranceToNode());
                package.Save();
                TestLog.Current.LogFile(package.PackageName);
            }

            return TestPassed;
        }

        /// <summary>
        /// Prepares an image adapter based on a bitmapped Master image
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private static IImageAdapter PrepareBitmapImageAdapter(String filepath)
        {
            IImageAdapter adapter = null;
            TestLog.Current.LogStatus("Master=" + filepath);
            if (filepath.Contains(".bmp"))
            {
                adapter = new ImageAdapter(filepath);
            }
            else
            {
                throw new Exception("Test error - PrepareBitmapImageAdapter expects a bmp file for its filepath");
            }
            return adapter;
        }

        public static IImageAdapter PrepareXamlImageAdapter(String filename)
        {
            if (isHostedInNavigationWindow)
            {
                return PrepareXamlImageAdapterInNavigationWindow(filename, defaultWidth, defaultHeight);
            }
            else
            {
                return PrepareXamlImageAdapterInWindow(filename, defaultWidth, defaultHeight);
            }
        }

        /// <summary>
        /// Prepares an image adapter by hosting Xaml document in a Window. Screen shot
        /// is take waitTime after the window is shown. 
        /// </summary>
        /// <param name="filename">xaml file name</param>
        /// <param name="width">width of the window</param>
        /// <param name="height">height of the window</param>
        /// <returns>Image Adapter</returns>
        public static IImageAdapter PrepareXamlImageAdapterInWindow(String filename, double width, double height)
        {
            System.Windows.Window window = new System.Windows.Window();
            
            window.Width = width;
            window.Height = height;
            window.Topmost = true;
            window.WindowStyle = System.Windows.WindowStyle.None;
            window.BorderThickness = new System.Windows.Thickness(0);
            window.ResizeMode = System.Windows.ResizeMode.NoResize;

            //move window off the leftmost so it would be under the mouse. 
            window.Left = 50;
            window.Top = 50;

            // parse content from xaml file.
            FileStream xamlStream = File.OpenRead(filename);
            System.Windows.FrameworkElement content = XamlReader.Load(xamlStream) as System.Windows.FrameworkElement;
            window.Content = content;

            
            //using the local resource dictionary to avoid test noise due to varying system themes.
            AddLocalResourceDictionary(window, localResourceDictionaryPath);

            window.Show();

            //scale down to default system dpi to use same master for different dpi. 
            DpiScalingHelper.ScaleWindowToFixedDpi(window, defaultSystemDpi, defaultSystemDpi);

            DispatcherHelper.DoEvents(waitTime.Milliseconds);

            Bitmap capture = ImageUtility.CaptureElement(window);
            ImageAdapter captureAdapter = new ImageAdapter(capture);

            return captureAdapter;
        }


        /// <summary>
        /// Add a locally available resource dictionary to the element. 
        /// The purpose is to prevent the element from rendering differently 
        /// with different theme. 
        /// </summary>
        /// <param name="root">element to add the resource dictionary</param>
        /// <returns>True if added sucessfully, false otherwise.</returns>
        internal static bool AddLocalResourceDictionary(System.Windows.FrameworkElement root, string localResourceDictionaryPath)
        {
            System.Windows.ResourceDictionary resourceDictionary;

            if(string.IsNullOrEmpty(localResourceDictionaryPath))
            {
                resourceDictionary = Microsoft.Test.Serialization.SerializationHelper.ParseXamlFile(localResourceDictionaryPath) as System.Windows.ResourceDictionary;
                root.Resources.MergedDictionaries.Add(resourceDictionary);
                return true;
            }

            return false;
        }
        /// <summary>
        ///Prepares an image adapter by hosting Xaml document in a NavigationWindow. 
        /// </summary>
        private static IImageAdapter PrepareXamlImageAdapterInNavigationWindow(String filename, double width, double height)
        {
            //// Make XamlVisualVerfication test process DPI Aware under Windows Vista so it returns accurate DPI for VScan scaling.
            //TestLog.Current.LogStatus("Set ProcessDpiAware");
            //Microsoft.Test.Diagnostics.ProcessorInformation.Current.SetProcessDpiAware();

            //TestLog.Current.LogStatus("System DPI=" + Monitor.Dpi.x.ToString());
            //TestLog.Current.LogStatus("Capture size before Dpi conversion is: " + width.ToString() + " x " + height.ToString());
            //width *= (Monitor.Dpi.x / defaultSystemDpi);
            //height *= (Monitor.Dpi.y / defaultSystemDpi);
            //TestLog.Current.LogStatus("Capture size after Dpi conversion is: " + width.ToString() + " x " + height.ToString());

            //IImageAdapter adapter = null;
            //if (filename.Contains(".xaml"))
            //{
            //    XamlBrowserHost xbh = null;
            //    try
            //    {
            //        xbh = new XamlBrowserHost();
            //        xbh.StartHost(filename);
            //        adapter = xbh.CaptureWindow(width, height);
            //    }
            //    finally
            //    {
            //        if (xbh != null)
            //        {
            //            xbh.Close();
            //        }
            //    }
            //}
            //else
            //{
            //    throw new Exception("Test error - PrepareXamlImageAdapter expects a xaml file for its filepath");
            //}
            //return adapter;

            throw new Exception("Core - Xaml Browser Hosting not enabled in Core - miguep");
        }
        #endregion

        #region Private members

        private static readonly double defaultSystemDpi = 96.0;
        private static readonly double defaultWidth = 800;
        private static readonly double defaultHeight = 480;
        private static double masterDpiX = 96.0;
        private static double masterDpiY = 96.0;
        private static bool xtcContainsDpiInfo = false;
        private static string toleranceFilePath = null;
        private static bool isHostedInNavigationWindow = true;
        private static readonly string localResourceDictionaryPath = "InvariantTheme.xaml";
        private static TimeSpan waitTime = TimeSpan.FromMilliseconds(500);

        #endregion
    }
}

