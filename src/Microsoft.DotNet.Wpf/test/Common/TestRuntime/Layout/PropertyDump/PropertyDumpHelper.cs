// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Layout.PropertyDump
{
    /// <summary>
    /// Summary description for PropertyCoreHelper.
    /// </summary>
    public class PropertyDumpHelper
    {
        PropertyDumpCore core = null;

        /// <summary>
        /// Instance of Property Dump Core
        /// </summary>
        public PropertyDumpCore Core
        {
            get { return core; }
        }

        /// <summary>
        /// Contstructor.
        /// </summary>
        /// <param name="visualRoot"></param>
        public PropertyDumpHelper(Visual visualRoot)
        {
            FindDumpRoot(string.Empty, visualRoot);
        }

        /// <summary>
        /// Contstructor.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="visualRoot"></param>
        public PropertyDumpHelper(string ID, Visual visualRoot)
        {
            FindDumpRoot(ID, visualRoot);
        }

        /// <summary>
        /// Instantiates Property Dump Core with Visual that is root of XML dump
        /// </summary>
        /// <param name="visualId"></param>
        /// <param name="visualRoot"></param>
        void FindDumpRoot(string visualId, DependencyObject visualRoot)
        {
            DependencyObject DumpRoot = null;

            if (visualId == null || visualId == string.Empty)
            {
                DumpRoot = visualRoot;
            }
            else
            {
                DumpRoot = LogicalTreeHelper.FindLogicalNode(visualRoot, visualId);
            }

            if (DumpRoot == null)
            {               
                GlobalLog.LogEvidence(new NullReferenceException("Dump root cannot be null."));
                return;
            }

            if (DumpRoot is Visual)
            {
                core = new PropertyDumpCore((Visual)DumpRoot);
            }
            else
            {
                FindDumpRoot(visualId, DumpRoot);
            }
        }

        /// <summary>
        /// Compares XML files and logs result.
        /// </summary>
        /// <returns></returns>
        public bool CompareLogShow(Arguments arguments)
        {
            bool comparison = false;

            if (File.Exists(arguments.FilterPath))
            {
                this.Core.Filter.XmlFilterPath = arguments.FilterPath;
            }
            else if (File.Exists(arguments.DefaultFilterPath))
            {
                this.Core.Filter.XmlFilterPath = arguments.DefaultFilterPath;
            }
            else
            {                
                GlobalLog.LogEvidence(new FileNotFoundException("Could not find "+ arguments.FilterFile));
                return false;
            }

            this.Core.SaveXmlDump(arguments.RenderedPath);            

            GlobalLog.LogStatus(string.Format("Searching for {0}.....", arguments.MasterFile));

            arguments.MissingMaster = !SearchMaster(arguments);

            if (!arguments.MissingMaster)
            {
                comparison = this.Core.CompareXmlFiles(arguments.ComparePath, arguments.RenderedPath);

                if (comparison)
                {
                    GlobalLog.LogStatus("XML COMPARE SUCCEEDED.");
                }
                else
                {
                    GlobalLog.LogEvidence("XML COMPARE FAILED.");
                }
            }
            else
            {
                GlobalLog.LogEvidence("NO MASTER FOUND");
                arguments.MissingMaster = true;
                comparison = false;
            }

            if (!comparison)
            {
                DiffPackage failurePackage = new DiffPackage(arguments.RenderedPath, arguments.ComparePath, arguments.MasterSdPath);
                string packageLocation = failurePackage.Save(arguments);
                
                if (!arguments.MissingMaster)
                {
                    GlobalLog.LogEvidence(string.Format("FAILED MASTER : {0}", arguments.ComparePath));
                }

                GlobalLog.LogFile(packageLocation);
            }

            return comparison;
        }
       
        bool SearchMaster(Arguments arguments)
        {
            // /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //
            // VARIATIONS OF MASTER PATH TO LOOK FOR
            //
            // * CHANGES : Will now start most specific directory and work my way to generic path.  will only use actual args instead of defaults.
            //
            // STARTPATH : [ BINROOT ]\FEATURETESTS\[ AREA ]\MASTERS\PROPERTYDUMP
            //
            // 1.  [STARTPATH]\[PRODUCT]\[THEME]\[LANGUAGE]\[FILENAME]
            // 2.  [STARTPATH]\[PRODUCT]\[THEME]\[FILENAME]
            // 3.  [STARTPATH]\[PRODUCT]\[LANGUAGE]\[FILENAME]
            // 4.  [STARTPATH]\[PRODUCT]\[FILENAME]
            // 5.  [STARTPATH]\[THEME]\[FILENAME]
            // 6.  [STARTPATH]\[LANGUAGE]\[FILENAME]
            // 7.  [STARTPATH]\[FILENAME]
            //
            // /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            # region NEW SEARCH.  Look from most specific to general directory.

            // using File.Exists() to check for master files.  may be slow if masters are being searched for over 
            // network share.  need to investigate at later time.

            if (FindMaster(arguments, string.Format("{0}\\{1}\\{2}", arguments.OS, arguments.Theme, arguments.Language))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}\\{1}", arguments.OS, arguments.Theme))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}\\{1}", arguments.OS, arguments.Language))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}", arguments.OS))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}", arguments.Theme))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}", arguments.Language))) { return true; }
            else if (FindMaster(arguments, string.Format("{0}", string.Empty))) { return true; }
            else { return false; }

            #endregion
        }

        bool FindMaster(Arguments arguments, string searchArgs)
        {
            arguments.CurrentPathArgs = searchArgs;
            return File.Exists(arguments.ComparePath);
        }
    }
}