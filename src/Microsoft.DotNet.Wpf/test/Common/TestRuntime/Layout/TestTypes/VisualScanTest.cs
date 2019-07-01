// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Test.Logging;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Test.Layout.VisualScan;
using Microsoft.Test.RenderingVerification;
using Microsoft.Test.Threading;

namespace Microsoft.Test.Layout.TestTypes
{
    /// <summary>
    /// Constructor for Visual Scan Test.
    /// </summary>
    public class VisualScanTest : LayoutTest
    {
        private string xamlfile = string.Empty;
        private ResourceDictionary resourceDictionary = null;
        private IMasterDimension[] masterDimensions = null;

        /// <summary>
        /// Visaul Scan xaml case.  
        /// </summary>
        /// <param name="xaml">xaml file to load into window</param>
        public VisualScanTest(string xaml)
        {
            xamlfile = xaml;
        }

        /// <summary/>
        /// <param name="xaml"></param>
        /// <param name="newResourceDictionary"></param>
        public VisualScanTest(string xaml, ResourceDictionary newResourceDictionary)
        {
            if (xaml != null || xaml != string.Empty)
            {
                xamlfile = xaml;
            }
            if (newResourceDictionary != null)
            {
                resourceDictionary = newResourceDictionary;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xaml"></param>
        /// <param name="dimensions"></param>
        public VisualScanTest(string xaml, IMasterDimension[] dimensions)
        {
            xamlfile = xaml;
            masterDimensions = dimensions;
        }

        /// <summary>
        /// Capture and compare window content
        /// </summary>
        public void CaptureAndCompare()
        {
            // Wait for dispatcher to empty.  This should make sure that window is fully loaded and rendered 
            // before image comparison starts.  This will minimize false failures that i have seen due to non-rendered windows.
            DispatcherHelper.DoEvents(1000);

            VScanCommon vscancommon = null;

            if (masterDimensions != null)
            {
                vscancommon = new VScanCommon(this, masterDimensions);
            }
            else
            {
                vscancommon = new VScanCommon(this);
            }

            this.Result = vscancommon.CompareImage();
        }

        /// <summary>
        /// Override for WindowSetup
        /// </summary>
        public override void WindowSetup()
        {
            if (xamlfile != null || xamlfile != string.Empty)
            {
                FileStream f = new FileStream(xamlfile, FileMode.Open, FileAccess.Read);
                this.window.Content = (FrameworkElement)XamlReader.Load(f);
                f.Close();
            }
            else
            {
                GlobalLog.LogEvidence(new Exception("No xamlfile specified for window content."));
            }

            if (resourceDictionary != null)
            {
                this.window.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            if (this.window.Content is FrameworkElement)
            {
                // Setting Window.Content size to ensure same size of root element over all themes.  
                // Different themes have diffent sized window chrome which will cause property dump 
                // and vscan failures even though the rest of the content is the same.
                // 784x564 is the content size of a 800x600 window in Aero them.
                ((FrameworkElement)this.window.Content).Height = 564;
                ((FrameworkElement)this.window.Content).Width = 784;
            }
            else
            {
                this.window.Height = 600;
                this.window.Width = 800;
            }
        }
    }
}