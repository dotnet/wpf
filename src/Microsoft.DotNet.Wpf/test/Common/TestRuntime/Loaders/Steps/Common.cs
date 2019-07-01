// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities
{
    internal struct Constants
    {
        internal const string VariationStepsRootElement = "Steps";
        internal const string VariationStepElement = "Step";
        internal const string VariationStepFileName = "FileName";
        internal const string VariationStepScenario = "Scenario";
        internal const string VariationStepVariation = "Variation";
        internal const string VariationStepNoBuild = "NoBuild";
        internal const string VariationStepOutputDirectory = "OutputDirectory";
        internal const string VariationStepCommandLineArgs = "CommandLineArgs";
        internal const string VariationStepsMSBuildErrors = "MSBuildErrors";
        internal const string VariationStepErrorCodesElement = "ErrorCodes";
        internal const string VariationStepDependsOnAttribute = "DependsOn";
        internal const string VariationStepMultipleRunElement = "MultipleRuns";
        internal const string VariationStepOutputPerf = "IgnorePerf";

        internal const string MSBuildPerfDataFile = "msbuildperfdata.xml";
        internal const string TargetsListFile = "ErrorCodes.xml";
        internal const string PerfTargetsElement = "PerfTargets";
        internal const string PerfSubResultElement = "SUBRESULT";
        internal const string PerfSubResultUnit = "";
    }

    internal class Helper
    {
        public static string ConvertArrayToString(string[] stringarray)
        {
            if (stringarray == null)
            {
                return null;
            }

            string returnstring = null;

            for (int i = 0; i < stringarray.Length; i++)
            {
                if (i == 0)
                {
                    returnstring = stringarray[i];
                    continue;
                }

                if (String.IsNullOrEmpty(stringarray[i]) == false)
                {
                    returnstring += "," + stringarray[i];
                }
            }

            return returnstring;
        }

        public static string ConvertCommanlineArrayToString(string[] stringarray)
        {
            if (stringarray == null)
            {
                return null;
            }

            string returnstring = null;

            for (int i = 0; i < stringarray.Length; i++)
            {
                if (i == 0)
                {
                    returnstring = stringarray[i];
                    continue;
                }

                returnstring += " " + stringarray[i];
            }

            return returnstring;
        }
    }
}

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Structure to describe Project file specific information.
    /// </summary>
    public struct ProjFileInfo
    {
        /// <summary>
        /// Project file name.
        /// </summary>
        public string filename;

        /// <summary>
        /// Project commandline options to use.
        /// </summary>
        public string commandlineoptions;

        /// <summary>
        /// Project expected error id's.
        /// </summary>
        public string[] errorcode;

        /// <summary>
        /// Project expected warning id's.
        /// </summary>
        public string[] warningcode;
    }

    /// <summary>
    /// Summary description for Macros.
    /// </summary>
    internal struct Constants
    {
        internal const string ScenarioElement = "Scenario";
        internal const string ScenariosElement = "Scenarios";
        internal const string NodeVariationElement = "NodeVariation";
        internal const string RootNodeVariationElement = "RootNodeVariation";
        internal const string TemplateDataElement = "TemplateData";
        internal const string IncludeElement = "Include";
        internal const string TypeAttribute = "Type";
        internal const string CaseAttribute = "Case";
        internal const string IDAttribute = "ID";
        internal const string AttributeVariationElement = "AttributeVariation";
        internal const string ElementNameAttribute = "ElementName";
        internal const string AttributeNameAttribute = "AttributeName";
        internal const string AttributeValueAttribute = "AttributeValue";
        internal const string RemoveAttribute = "Remove";
        internal const string TextVariationElement = "TextVariation";
        internal const string CommandLineArgAttribute = "CommandLineArg";
        internal const string CommandLineArgsElement = "CommandLineArgs";
        internal const string ErrorCodeAttribute = "ErrorCode";
        internal const string WarningCodeAttribute = "WarningCode";
        internal const string ProjectNodeVariationElement = "ProjNodeVariation";
        internal const string ProjectAttributeVariationElement = "ProjAttribVariation";
        internal const string ProjectTextVariationElement = "ProjTextVariation";
        internal const string TemplateDataRootAttribute = "Root";
        internal const string XMLVariationTemplateElement = "XMLVariationTemplate";
    }

    /// <summary>
    /// Helper class 
    /// </summary>
    public static class CommonHelper
    {
        static bool filemodified = false;
        static string[] _searchpaths = null;

        /// <summary>
        /// Additional Directory search paths for file lookup.
        /// </summary>
        public static string AdditionalSearchPaths
        {
            set
            {
                if (String.IsNullOrEmpty(value) == false)
                {
                    _searchpaths = value.Split(new char[] { ',', ';' });
                }
            }
        }

        /// <summary>
        /// Helper method to check if file exists.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string VerifyFileExists(string filename)
        {
            if (filename == null)
            {
                return null;
            }

            string filepath = PathSW.GetFullPath(filename.ToLowerInvariant());
            if (FileSW.Exists(filepath))
            {
                return filepath;
            }

            if (PathSW.IsPathRooted(filename.ToLowerInvariant()) == true)
            {
                string rootpath = PathSW.GetPathRoot(filename);
                filepath = rootpath + PathSW.DirectorySeparatorChar + PathSW.GetFileName(filename);

                return filepath;
            }

            // Use additional search paths provided by user.
            for (int i = 0; _searchpaths != null && i < _searchpaths.Length; i++)
            {
                if (String.IsNullOrEmpty(_searchpaths[i]) == false)
                {
                    filepath = _searchpaths[i] + PathSW.DirectorySeparatorChar + filename;
                    if (FileSW.Exists(filepath))
                    {
                        return PathSW.GetFullPath(filepath);
                    }
                }
            }

            Console.WriteLine("{0} could not be found", filename);
            return null;
        }

        /// <summary>
        /// Processing for {data} format. supported Autodata or Culture.
        /// </summary>
        /// <param name="szspecialvalue"></param>
        /// <param name="varnode"></param>
        /// <returns></returns>
        static internal string DeriveSpecialValues(string szspecialvalue, XmlNode varnode)
        {
            if (IsSpecialValue(szspecialvalue) == false)
            {
                return null;
            }

            int index = szspecialvalue.IndexOf('{');

            string leftsidevalue = szspecialvalue.Substring(0, index);

            string rightsidevalue = szspecialvalue.Substring(szspecialvalue.IndexOf('}', index + 1) + 1);

            szspecialvalue = szspecialvalue.Substring(index + 1, szspecialvalue.IndexOf('}') - (index + 1));
            //szspecialvalue = szspecialvalue.Substring(1, szspecialvalue.Length - 2);

            index = szspecialvalue.IndexOf('[');
            if (index < 0)
            {
                index = szspecialvalue.IndexOf(':');
            }

            string loadtype = null;
            if (index >= 0)
            {
                loadtype = szspecialvalue.Substring(0, index);
                szspecialvalue = szspecialvalue.Substring(index);
            }
            else
            {
                loadtype = szspecialvalue;
                szspecialvalue = null;
            }

            switch (loadtype.ToLowerInvariant())
            {
                case "autodata":
                    AutoDataHelper adhautodatainfo = new AutoDataHelper();
                    loadtype = adhautodatainfo.DeriveAutoDataInformation(szspecialvalue, varnode);
                    FileModified = adhautodatainfo.Isdirty;
                    adhautodatainfo = null;
                    break;

                case "culture":
                    AutoDataHelper adhautoculture = new AutoDataHelper();
                    loadtype = adhautoculture.GetCurrentCultureName(szspecialvalue, varnode);
                    FileModified = adhautoculture.Isdirty;
                    adhautoculture = null;
                    break;

                case "differentculture":
                    AutoDataHelper adhdiffculture = new AutoDataHelper();
                    loadtype = adhdiffculture.GetDifferentCultureName();
                    FileModified = adhdiffculture.Isdirty;
                    adhdiffculture = null;
                    break;

                case "invalidculture":
                    AutoDataHelper adhinvalidculture = new AutoDataHelper();
                    loadtype = adhinvalidculture.GetInvalidCultureName();
                    FileModified = adhinvalidculture.Isdirty;
                    adhinvalidculture = null;
                    break;

                case "generatedfile":
                    GeneratedFilesHelper gfhfilename = new GeneratedFilesHelper();
                    loadtype = gfhfilename.GetGeneratedFileName(szspecialvalue, varnode);
                    gfhfilename = null;
                    break;

                case "generatedassembly":
                    GeneratedFilesHelper gfhassembly = new GeneratedFilesHelper();
                    loadtype = gfhassembly.GetGeneratedAssembly(szspecialvalue, varnode);
                    gfhassembly = null;
                    break;

                case "version":
                    VersionHelper vh = new VersionHelper();
                    loadtype = vh.GetVersionInfo(szspecialvalue);
                    vh = null;
                    break;

                default:
                    return null;
            }

            return leftsidevalue + loadtype + rightsidevalue;

            //innertext = AutoData.Extract.GetTestString( Convert.ToInt16(innertext), ScriptTypeEnum.Arabic, 20, false);

            //return true;
        }

        /// <summary>
        /// Flag indicates if AutoData has been used to generate data.
        /// </summary>
        public static bool FileModified
        {
            get
            {
                return filemodified;
            }
            set
            {
                if (filemodified == false)
                {
                    filemodified = value;
                }
            }
        }

        /// <summary>
        /// Check if string starts with { and ends with }
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        static internal bool IsSpecialValue(string text)
        {
            //if (text.StartsWith("{") && text.EndsWith("}"))
            //{
            //    return true;
            //}

            if (text.Contains("{"))
            {
                text = text.Substring(text.IndexOf('{'));
                if (text.Contains("}"))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
