// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Test.Layout.PropertyDump;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Layout.TestTypes
{
    /// <summary>
    /// Base Class for Property Dump Test Cases.
    /// </summary>
    public class PropertyDumpTest : LayoutTest
    {
        /// <summary>
        /// Public Constructor.  
        /// </summary>
        public PropertyDumpTest()
        {
            Arguments = new Arguments(this);
        }

        /// <summary>
        /// 
        /// </summary>
        public Arguments Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }
        private Arguments arguments = null;

        private string xamlfile = string.Empty;
        private ResourceDictionary resourceDictionary = null;
        /// <summary>
        /// Entry Point accepts name for XAMLFILE to be loaded.
        /// - Default used for FILTER
        /// </summary>
        /// <param name="xaml"></param>
        public void DumpTest(string xaml)
        {
            DumpTest(xaml, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xaml"></param>
        /// <param name="newResourceDictionary"></param>
        public void DumpTest(string xaml, ResourceDictionary newResourceDictionary)
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
        /// Runs Property Dump Test.
        /// </summary>
        public void DumpAndCompare()
        {
            if (windowHasContent)
            {
                PropertyDumpHelper propdump = new PropertyDumpHelper(this.window.Content as Visual);
                CommonFunctionality.FlushDispatcher();

                this.Result = propdump.CompareLogShow(Arguments);
                CommonFunctionality.FlushDispatcher();
            }
            else { this.Result = false; }
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
                windowHasContent = false;
                GlobalLog.LogEvidence("No XAMLFILE specified for window content.");
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

        bool windowHasContent = true;
    }
}