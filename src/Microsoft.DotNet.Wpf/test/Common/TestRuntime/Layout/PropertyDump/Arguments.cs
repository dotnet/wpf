// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//Description:Arguments class provides the functionality of reading information from
//the piper for any lab specific environment variables and storing that information

using System;
using System.IO;
using Microsoft.Test.Diagnostics;
using Microsoft.Test.Display;
using Microsoft.Test.Discovery;

namespace Microsoft.Test.Layout.PropertyDump
{
    /// <summary>
    /// Summary description for Arguments.
    /// </summary>
    public class Arguments
    {
        #region Properties
        private object currentTestCase = null;
        private string actualArea = string.Empty;
        private string currentPathArgs = string.Empty;
        private string name = string.Empty;
        private bool missingMaster = false;

        /// <summary>
        /// Test Case Name.
        /// </summary>
        public string Name
        {            
            get 
            {
                if (name == string.Empty)
                { 
                    name = DriverState.TestName;                   
                }
                return name;
            }
            set { name = value; }
        }              
        
        /// <summary>
        /// Test case priority.
        /// </summary>
        public string Priority
        {
            get { return FindAttribute("pri"); }
        }

        /// <summary>
        /// BinPlace root. Try to use support files instead.
        /// </summary>
        public string BinRoot
        {
            get
            {
                return DriverState.TestBinRoot;
            }
        }

        /// <summary>
        /// String that actual \platform\theme\language in search and master paths
        /// </summary>
        public string ActualPathArgs
        {
            get { return Path.Combine(Path.Combine(OS, Theme), Language); }
        }

        /// <summary>
        /// String that represents the current \platform\theme\language in search and master paths
        /// </summary>
        public string CurrentPathArgs
        {
            get { return currentPathArgs; }
            set { currentPathArgs = value; }
        }

        /// <summary>
        /// Path of Master found under BinRoot.
        /// </summary>
        public string ComparePath
        {
            get
            {
                return string.Format("{0}\\FeatureTests\\{1}\\Masters\\PropertyDump\\{2}\\{3}", 
                    BinRoot, 
                    DriverState.DriverParameters["MasterFileArea"], 
                    CurrentPathArgs, 
                    MasterFile);
            }
        }

        /// <summary>
        /// Name of master file.
        /// </summary>
        public string MasterFile
        {
            get { return string.Format("{0}.lxml", Name); }
        }

        /// <summary>
        /// FINAL Source Depot Path location of file.
        /// Unsupported!! test binaries don't have TFS access.
        /// </summary>
        public string MasterSdPath
        {
            get 
            { 
                return string.Format("%sdxroot%\\wpf\\test\\{0}\\data\\masters\\propertydump\\{1}\\{2}", 
                    DriverState.DriverParameters["MasterFileArea"], 
                    CurrentPathArgs,
                    MasterFile); }
        }

        /// <summary>
        /// True if there is a master. Default False.
        /// </summary>
        public bool MissingMaster
        {
            get { return missingMaster; }
            set { missingMaster = value; }
        }

        /// <summary>
        /// Path to rendered file.
        /// </summary>
        public string RenderedFile
        {
            get { return string.Format("{0}_rendered.lxml", Name); }
        }

        /// <summary>
        /// Path to rendered file.
        /// </summary>
        public string RenderedPath
        {
            get { return Path.Combine(WorkingDirectory, RenderedFile); }
        }

        /// <summary>
        /// Name of Filter file.
        /// </summary>
        public string FilterFile
        {
            get { return "filter.xml"; }
        }

        /// <summary>
        /// Full Path to Filter file.
        /// </summary>
        public string FilterPath
        {
            get { return Path.Combine(WorkingDirectory, FilterFile); }
        }

        /// <summary>
        /// DEFAULT Full Path to Filter file.
        /// </summary>
        public string DefaultFilterPath
        {
            get { return Path.Combine(BinRoot, @"Common\filter.xml"); }
        }

        /// <summary>
        /// Current Working directory.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                //The driver is invoked with Current Directory set. 
                //Talk to pantal if you are hitting issues.
                return System.Environment.CurrentDirectory;
            }
        }

        /// <summary>
        /// OS Product
        /// </summary>
        public string OS
        {
            get { return SetSystemInfo("product", SystemInformation.Current.Product.ToString()); }
        }

        /// <summary>
        /// OS Language.
        /// </summary>
        public string Language
        {
            get { return SystemInformation.Current.OSCulture.ThreeLetterWindowsLanguageName; }
        }

        /// <summary>
        /// Current OS Theme.
        /// </summary>
        public string Theme
        {
            get { return SetSystemInfo("theme", DisplayConfiguration.GetTheme()); }
        }

        /// <summary>
        /// Current Test object
        /// </summary>
        public object CurrentTestCase
        {
            get { return currentTestCase; }
            set { currentTestCase = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Contructor.
        /// </summary>
        public Arguments(object testcase)
        {
            CurrentTestCase = testcase;
        }

        #endregion

        #region  Private Methods

        /// <summary>
        /// Gets usable values from system information.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="currentvalue"></param>
        /// <returns></returns>
        private string SetSystemInfo(string value, string currentvalue)
        {
            string returnValue = string.Empty;

            switch (value)
            {
                case "product":
                    //trim windows of of propduct name
                    string remove = "windows";
                    returnValue = currentvalue.ToLower().TrimStart(remove.ToCharArray());
                    break;
                case "theme":
                    //if 'windows clasic' remove 'windows'
                    char[] split_here = { ' ' };
                    string[] split = currentvalue.Split(split_here);
                    if (split.Length > 1)
                        returnValue = split[1];
                    else
                        returnValue = currentvalue;
                    break;
                default: break;
            }
            return returnValue.ToUpper();
        }

        /// <summary>
        /// Searches TestAttribute for values.
        /// </summary>
        private string FindAttribute(string _value)
        {
            //TODO: This should be refactored. This algorithm makes way too many internal reflection assumptions.
            if (currentTestCase != null)
            {
                object[] attributes = currentTestCase.GetType().GetCustomAttributes(typeof(TestAttribute), false);

                string value = string.Empty;

                foreach (object o in attributes)
                {
                    if (o.GetType().Name == "TestAttribute")
                    {
                        switch (_value.ToLower())
                        {
                            case "area":
                                //Variables Property is obsolete
                                #pragma warning disable 0618
                                if (((TestAttribute)o).Variables != null)
                                {
                                    char[] firstSplitter = new char[] { '/' };
                                    string[] variables = ((TestAttribute)o).Variables.Split(firstSplitter);
                                    if (variables != null && variables.Length > 0)
                                    {
                                        foreach (string variable in variables)
                                        {
                                            char[] secondSplitter = new char[] { '=' };
                                            string[] values = variable.Split(secondSplitter);
                                            if (values != null && values.Length > 1)
                                            {
                                                if (values[0].ToLower() == "area")
                                                {
                                                    value = values[1];
                                                }
                                                else { value = string.Empty; }
                                            }
                                            else { value = string.Empty; }
                                        }
                                    }
                                    else { value = string.Empty; }
                                }
                                else { value = string.Empty; }
                                #pragma warning restore 0618
                                break;
                            case "subarea":
                                if (((TestAttribute)o).SubArea != null)
                                {
                                    value = ((TestAttribute)o).SubArea;
                                }
                                else { value = string.Empty; }
                                break;
                            case "supportfiles":
                                if (((TestAttribute)o).SupportFiles != null)
                                {
                                    value = ((TestAttribute)o).SupportFiles;
                                }
                                else { value = string.Empty; }
                                break;
                            case "pri":
                                value = ((TestAttribute)o).Priority.ToString();
                                break;
                            default:
                                break;
                        }
                    }
                }

                return value;
            }
            else
            {
                throw new Exception("Arguments.CurrentTestCase == null");
            }
        }

        #endregion
    }
}
