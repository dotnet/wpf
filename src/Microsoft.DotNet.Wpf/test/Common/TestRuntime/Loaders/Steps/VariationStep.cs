// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;

namespace Microsoft.Test.Utilities.StepsEngine
{
    /// <summary>
    /// 	<Step ID="1">
    ///	        <FileName>Sample.xvar</FileName>
    ///	        <Scenario>1</Scenario>
    ///	        <Variation>3,5,23,7</Variation>
    ///	        <ErrorCodes>1010</ErrorCodes>
    ///	        <NoBuild/>
    ///     </Step>
    /// </summary>
    public class VariationStep
    {
        string _filename = null;
        string _scenario = null;
        string[] _variation = null;
        string _outputdirectory = null;
        string _commandlineoptions = null;
        string _errorcodes = null;
        bool _build = true;
        char[] separator = { ',' };
        string _id = null;
        string[] _dependson = null;
        int _runmultipletimes = -1;
        bool _boutputperf = false;

        /// <summary>
        ///  Constructor for MSBUild test Variation Step
        /// </summary>
        public VariationStep()
        {
        }

        /// <summary>
        /// Read a Variation Step from Xml.
        /// </summary>
        /// <param name="node"></param>
        public VariationStep(XmlNode node)
        {
            if (node == null)
            {
                throw new ApplicationException("Cannot have a empty variation Step");
            }

            if (node.HasChildNodes == false)
            {
                throw new ApplicationException("Cannot have a empty variation Step");
            }

            if (node.Attributes == null)
            {
                throw new ApplicationException("Step should define a ID attribute");
            }

            if (node.Attributes.Count > 0)
            {
                if (node.Attributes["ID"] != null && String.IsNullOrEmpty(node.Attributes["ID"].Value) == false)
                {
                    _id = node.Attributes["ID"].Value;
                }
                else
                {
                    throw new ApplicationException("An ID for each step has to be specified.");
                }

                if (node.Attributes[Constants.VariationStepDependsOnAttribute] != null &&
                    String.IsNullOrEmpty(node.Attributes[Constants.VariationStepDependsOnAttribute].Value) == false)
                {
                    _dependson = node.Attributes[Constants.VariationStepDependsOnAttribute].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            XmlNodeList childlist = node.SelectNodes(Constants.VariationStepFileName);
            if (childlist.Count > 1 || childlist.Count == 0)
            {
                throw new Exception("Only one FileName can be specified per variation step.");
            }

            _filename = childlist[0].InnerText;

            childlist = node.SelectNodes(Constants.VariationStepScenario);
            if (childlist.Count > 1 || childlist.Count == 0)
            {
                throw new Exception("Only one Scenario can be applied per variation step.");
            }

            _scenario = childlist[0].InnerText; //.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            childlist = node.SelectNodes(Constants.VariationStepVariation);
            if (childlist.Count > 1)
            {
                throw new Exception("Cannot have more than one variation in a variation step.");
            }

            if (childlist.Count > 0)
            {
                _variation = childlist[0].InnerText.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                _variation = new string[1];
                _variation[0] = "all";
            }

            childlist = node.SelectNodes(Constants.VariationStepNoBuild);
            if (childlist.Count > 0)
            {
                _build = false;
            }

            childlist = node.SelectNodes(Constants.VariationStepOutputDirectory);
            if (childlist.Count > 0)
            {
                _outputdirectory = childlist[0].InnerText;
            }


            childlist = node.SelectNodes(Constants.VariationStepCommandLineArgs);
            if (childlist.Count > 0)
            {
                if (childlist.Count > 1)
                {
                    throw new NotSupportedException("Cannot have more than one " + Constants.VariationStepCommandLineArgs + " element");
                }

                _commandlineoptions = childlist[0].InnerText;
            }

            childlist = node.SelectNodes(Constants.VariationStepMultipleRunElement);
            if (childlist.Count > 0)
            {
                if (childlist.Count > 1)
                {
                    throw new NotSupportedException("Cannot have more than one " + Constants.VariationStepMultipleRunElement + " element");
                }

                _runmultipletimes = Convert.ToInt32(childlist[0].InnerText);
            }

            childlist = node.SelectNodes(Constants.VariationStepErrorCodesElement);
            if (childlist.Count > 0)
            {
                if (childlist.Count > 1)
                {
                    throw new NotSupportedException("Cannot have more than one " + Constants.VariationStepErrorCodesElement + " element");
                }

                _errorcodes = childlist[0].InnerText;
            }

            childlist = node.SelectNodes(Constants.VariationStepOutputPerf);
            if (childlist.Count > 0)
            {
                if (childlist.Count > 1)
                {
                    throw new NotSupportedException("Cannot have more than one" + Constants.VariationStepOutputPerf + " element");
                }

                if (String.IsNullOrEmpty(childlist[0].InnerText) == false)
                {
                    bool result;
                    if (Boolean.TryParse(childlist[0].InnerText, out result))
                    {
                        _boutputperf = result;
                    }
                }
                else
                {
                    _boutputperf = false;
                }
                
            }
        }

        /// <summary>
        /// File name to apply variation.
        /// </summary>
        /// <value></value>
        public string FileName
        {
            get { return _filename; }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new Exception("Variation step FileName cannot be null");
                }

                _filename = value;
            }
        }

        /// <summary>
        /// Scenario ID to apply on current File Name.
        /// </summary>
        /// <value></value>
        public string Scenario
        {
            get { return _scenario; }
            set
            {
                _scenario = value;

            }
        }

        /// <summary>
        /// Variation ID to apply on current File Name
        /// </summary>
        /// <value></value>
        public string[] Variation
        {
            get
            {
                if (_variation == null || _variation.Length == 0)
                {
                    _variation = new string[1];
                    _variation[0] = "all";
                }

                return _variation;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                if (value.Length == 0)
                {
                    _variation = new string[1];
                    _variation[0] = "all";
                }
                else
                {
                    _variation = value;
                }
            }
        }

        /// <summary>
        /// Flag to compile existing file.
        /// </summary>
        /// <value></value>
        public bool Build
        {
            get { return _build; }
            set
            {
                _build = value;
            }
        }

        /// <summary>
        ///  Boolean value for compilation perf data (Deprecated?)
        /// </summary>
        public bool OutputPerfData
        {
            get
            {
                return _boutputperf;
            }
        }
        /// <summary>
        /// Number of times to repeat test for performance aggregation
        /// </summary>
        public int RunMultipleTimes
        {
            set
            {
                _runmultipletimes = value;
            }
            get
            {
                return _runmultipletimes;
            }
        }

        /// <summary>
        ///  Directory for logging output to be written to
        /// </summary>
        public string OutputDirectory
        {
            set
            {
                _outputdirectory = value;
            }
            get
            {
                return _outputdirectory;
            }
        }

        /// <summary>
        /// ID of the VariationStep
        /// </summary>
        public string ID
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        ///  Command line options for the Variation Engine
        /// </summary>
        public string StepCommandLineOptions
        {
            get
            {
                return _commandlineoptions;
            }
        }

        /// <summary>
        ///  Codes for ignorable error codes
        /// </summary>
        public string StepExpectedErrorCodes
        {
            get
            {
                return _errorcodes;
            }
        }
        /// <summary>
        ///  String array of dependencies for the VariationStep.
        /// </summary>
        public string[] StepDependson
        {
            get
            {
                return _dependson;
            }
        }
    }
}
