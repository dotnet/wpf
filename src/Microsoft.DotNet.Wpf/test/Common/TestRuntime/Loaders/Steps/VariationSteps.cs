// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections.Generic;

using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.StepsEngine
{
    /// <summary>
    /// Inputs that specify a sequence of steps that can be input to 
    /// generate xml files and build them.
    /// </summary>
    public class VariationSteps
    {
        List<VariationStep> _variationsteps = null;
        XmlElement _msbuilderrorwarnings = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public VariationSteps()
        {
            _variationsteps = new List<VariationStep>();
        }

        /// <summary>
        /// Read input file for Variation steps.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Read(string filename)
        {
            filename = Microsoft.Test.MSBuildEngine.MSBuildEngineCommonHelper.VerifyFileExists(filename);
            if (filename == null)
            {
                throw new NullReferenceException("Input file name is null");
            }

            XmlDocumentSW xmldoc = new XmlDocumentSW();
            xmldoc.Load(filename);

            if (xmldoc.DocumentElement == null)
            {
                throw new ArgumentException("The file " + filename + " does not have a document element");
            }

            return this.Read((XmlNode)xmldoc.DocumentElement);
        }

        /// <summary>
        /// Read Xml node to Parse Variation steps.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Read(XmlNode node)
        {
            if (node == null)
            {
                throw new NullReferenceException("Input node is null");
            }

            if (node.Name != Constants.VariationStepsRootElement)
            {
                throw new ArgumentException("The current document root is not a recognized element. \nThe recognized root element is " + Constants.VariationStepsRootElement + ".");
            }

            XmlNodeList nodelist = node.SelectNodes("./" + Constants.VariationStepElement);
            for (int i = 0; i < nodelist.Count; i++)
            {
                VariationStep step = new VariationStep(nodelist[i]);
                _variationsteps.Add(step);
            }

            nodelist = node.SelectNodes("./" + Constants.VariationStepsMSBuildErrors);
            if (nodelist.Count > 1)
            {
                throw new NotSupportedException("Please specify only one MSBuildErrors element in a Steps file");
            }

            _msbuilderrorwarnings = (XmlElement) nodelist[0];

            return true;
        }

        /// <summary>
        /// List of variation step
        /// </summary>
        /// <value></value>
        public List<VariationStep> VariationStepsList
        {
            get
            {
                return _variationsteps;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public XmlElement MSBuildErrors
        {
            get
            {
                return _msbuilderrorwarnings;
            }
        }
    }

}
