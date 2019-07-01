// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using Microsoft.Test.Layout.TestTypes;
using Microsoft.Test.Logging;
using Microsoft.Test.RenderingVerification;
using Microsoft.Test.Threading;

namespace Microsoft.Test.Layout.VisualScan
{
    /// <summary>
    /// Master Index for Layout Test Cases
    /// </summary>
    public class LayoutMasterIndex : MasterIndex
    {
        /// <summary>
        /// Contructor
        /// Will add weighted dimensions to the MasterIndex critera.
        /// </summary>
        /// <param name="masterDimensions">Array of IMasterDimension for MasterIndex criteria</param>
        public LayoutMasterIndex(IMasterDimension[] masterDimensions)
        {
            if (masterDimensions != null)
            {
                for (int i = 0; i < masterDimensions.Length; i++)
                {
                    this.AddCriteria(masterDimensions[i], i ^ 2);
                }
            }
        }
    }

    /// <summary>
    /// VScan Api's
    /// </summary>
    public class VScanCommon
    {
        private LayoutMasterIndex index = null;
        private IntPtr hwnd = IntPtr.Zero;
        private UIElement target = null;
        private string masterPath = string.Empty;
        private bool resizeWindowForDpi = true; //default
        private static readonly string dpi96Tolerance = "<Tolerance dpiRatio=\"1\"><Point x=\"2\" y=\"0.035\"/><Point x=\"18\" y=\"0.015\"/><Point x=\"80\" y=\"0.0004\"/><Point x=\"135\" y=\"0.0003\"/><Point x=\"145\" y=\"0.0002\"/><Point x=\"155\" y=\"0.000015\"/></Tolerance>";
        private static readonly string dpi120Tolerance = "<Tolerance dpiRatio=\"1.25\"><Point x=\"2\" y=\"0.15\"/><Point x=\"6\" y=\"0.05\"/><Point x=\"25\" y=\"0.009\"/><Point x=\"50\" y=\"0.008\"/><Point x=\"125\" y=\"0.005\"/><Point x=\"200\" y=\"0.0015\"/><Point x=\"250\" y=\"0.001\"/></Tolerance>"; 
        /// <summary>
        /// Custom Layout Master Index
        /// </summary>
        public LayoutMasterIndex Index
        {
            get { return index; }
            set { index = value; }
        }
        
        /// <summary>
        /// HWND that will be captured.
        /// </summary>
        public IntPtr HWND
        {
            get { return hwnd; }
            set { hwnd = value; }
        }
        
        /// <summary>
        /// UIElement that will be captured.
        /// </summary>
        public UIElement Target
        {
            get { return target; }
            set { target = value; }
        }

        /// <summary>
        /// Get/Set to determine if we should scale the bitmap b/c of dpi
        /// </summary>
        public bool ResizeWindowForDpi
        {
            get { return resizeWindowForDpi; }
            set { resizeWindowForDpi = value; }
        }

        /// <summary>
        /// Default tolerance (strict) 
        /// Specifies a dpi ration of 1
        /// </summary>
        public string DefaultStrictTolerance
        {
            get { return dpi96Tolerance; }            
        }
             
        private void SetWindow(Window testwin)
        {
            GlobalLog.LogStatus("VISUAL COMPARE");

            WindowInteropHelper iwh = new WindowInteropHelper(testwin);
            HWND = iwh.Handle;

            if (iwh == null || HWND == IntPtr.Zero)
            {
                GlobalLog.LogEvidence(new Exception("Could not find the hwnd of the main window"));
            }
        }

        /// <summary>
        /// Constructor for Vscan Common
        /// </summary>
        ///<param name="test">Test of type LayoutTest that is currently running</param>        
        public VScanCommon(LayoutTest test)
        {
            //HACK: Visual Verification technology should not be taking a dependency on layout technology.
            if (test != null)
            {
                index = new LayoutMasterIndex(null);
                index.FileName = Common.ResolveName(test);
                index.Path = Common.ResolvePath(test);

                GlobalLog.LogStatus("Master Name : {0}", index.FileName);
                GlobalLog.LogStatus("Master Path : {0}", index.Path);

                SetWindow(test.window);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="test"></param>
        /// <param name="masterDimensions"></param>
        public VScanCommon(LayoutTest test, IMasterDimension[] masterDimensions)
        {
            if (test != null)
            {
                index = new LayoutMasterIndex(masterDimensions);
                index.FileName = Common.ResolveName(test);
                index.Path = Common.ResolvePath(test);

                GlobalLog.LogStatus("Master Name : {0}", index.FileName);
                GlobalLog.LogStatus("Master Path : {0}", index.Path);

                SetWindow(test.window);
            }
        }

        /// <summary>
        /// Constructor for Vscan Common
        /// </summary>
        /// <param name="testwin">Window to capture image of</param>
        /// <param name="test">Calling Test</param>
        public VScanCommon(Window testwin, object test)
        {
            if (test != null && testwin != null)
            {
                masterPath = Common.ResolvePath(test);

                Index = new LayoutMasterIndex(null);
                index.FileName = Common.ResolveName(test); ;
                GlobalLog.LogStatus("Master Name : {0}", index.FileName);
                index.Path = masterPath;
                GlobalLog.LogStatus("Master Path : {0}", index.Path);

                SetWindow(testwin);
            }
        }

        /// <summary>
        /// Constructor for Vscan Common
        /// Master is specivif Path 
        /// </summary>
        /// <param name="testwin">Window to capture image of</param>
        /// <param name="test">Calling Test</param>
        /// <param name="master">Master Name</param>
        public VScanCommon(Window testwin, object test, string master)
        {
            if (test != null && testwin != null)
            {
                masterPath = Common.ResolvePath(test);

                Index = new LayoutMasterIndex(null);

                if (master == null || master == string.Empty)
                {
                    index.FileName = test.GetType().Name;
                }
                else
                {
                    index.FileName = master;
                }

                GlobalLog.LogStatus("Master Name : {0}", index.FileName);
                index.Path = masterPath;
                GlobalLog.LogStatus("Master Path : {0}", index.Path);

                SetWindow(testwin);
            }
        }

        /// <summary>
        /// Constructor for Vscan Common
        /// Master and Master Path are spcified by caller.
        /// </summary>
        /// <param name="testwin">Window to capture image of</param>
        /// <param name="master">Master Name</param>
        /// <param name="masterpath">Relative Path to Master</param>
        public VScanCommon(Window testwin, string master, string masterpath)
        {
            if (testwin != null)
            {
                masterPath = masterpath;

                Index = new LayoutMasterIndex(null);

                if (master != null && master != string.Empty)
                {
                    index.FileName = master;
                }

                GlobalLog.LogStatus("Master Name : {0}", index.FileName);

                if (masterpath != null && masterpath != string.Empty)
                {
                    index.Path = masterpath;
                }

                GlobalLog.LogStatus("Master Path : {0}", index.Path);

                SetWindow(testwin);
            }
        }

        /// <summary>
        /// Simple Visual Comparison.
        /// </summary>
        /// <returns></returns>
        public bool CompareImage()
        {
            DispatcherHelper.DoEvents(1000);
            MasterImageComparer comparer = new MasterImageComparer(index);
            comparer.ToleranceSettings = ImageComparisonSettings.CreateCustomTolerance(DefaultTolerance());
            return comparer.Compare(HWND);
        }

        /// <summary>
        /// TEMP
        /// </summary>
        /// <param name="videoGroup"></param>
        /// <param name="tolerance"></param>
        /// <param name="masterName"></param>
        public bool CompareImage(string videoGroup, string tolerance, string masterName)
        {
            if (masterName != "none" || masterName != string.Empty || masterName != null)
                index.FileName = masterName;

            MasterImageComparer comparer = new MasterImageComparer(index);
            comparer.ResizeWindowForDpi = resizeWindowForDpi;

            if (tolerance == null || tolerance == string.Empty)
            {
                comparer.ToleranceSettings = ImageComparisonSettings.CreateCustomTolerance(DefaultTolerance());
            }
            else
            {
                XmlDocument customTolerance = new XmlDocument();                
                customTolerance.LoadXml(tolerance);
                ImageComparisonSettings.CreateCustomTolerance((XmlNode)customTolerance.DocumentElement);
            }

            return comparer.Compare(HWND);
        }

        /// <summary>
        /// TEMP
        /// </summary>
        /// <param name="vscan"></param>
        /// <param name="w"></param>
        /// <param name="imgFile"></param>
        /// <param name="bSaveMaster"></param>
        public void CompareImage(VScanCommon vscan, Window w, string imgFile, bool bSaveMaster)
        {
            GlobalLog.LogEvidence(new Exception("Not fully implemented."));
        }

        private XmlNode DefaultTolerance()
        {
            //TODO: Investigate needs for different tolerance's.
            GlobalLog.LogStatus("Default tolerance being applied.");
            XmlDocument defaultTolerance = new XmlDocument();            
            defaultTolerance.LoadXml("<Tolerances>" + dpi96Tolerance + dpi120Tolerance + "</Tolerances>");  
            return (XmlNode)defaultTolerance.DocumentElement;
        }
    }
}