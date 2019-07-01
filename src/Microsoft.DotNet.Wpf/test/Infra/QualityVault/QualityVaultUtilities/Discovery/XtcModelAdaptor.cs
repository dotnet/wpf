// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Discovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// The XmlModelAdaptor is an extension of the XtcAdaptor.
    /// If a test discovered by the XtcAdaptor contains a Model,
    /// XmlModelAdaptor will expand it and a new TestInfo will
    /// be created for each variation.
    /// </summary>
    public class XtcModelAdaptor : XtcAdaptor
    {
        /// <summary>
        /// Expand XTC base tests then expand model variations if they exist.  
        /// If no model exists, XTC discovered base tests will be returned.
        /// </summary>
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            // Discover base tests
            List<TestInfo> baseTests = base.Discover(testManifestPath, defaultTestInfo).ToList();

            // Expanded tests
            List<TestInfo> tests = new List<TestInfo>();

            XmlDocument xml = new XmlDocument();
            xml.Load(testManifestPath.FullName);

            foreach (TestInfo test in baseTests)
            {
                // select model node for test matching test.Name
                XmlNode modelXml = xml["XTC"].SelectSingleNode(string.Format("TEST[@Name='{0}']/DATA/Model", test.Name));

                if (modelXml == null)
                {
                    // no model defined, add base test without expansion
                    tests.Add(test);
                    continue;
                }

                // Load and expand model.
                List<string> variations = ExpandModel(Model.Load(modelXml));

                int index = 0;

                foreach (string variation in variations)
                {
                    TestInfo newTest = test.Clone();
                    newTest.Name = string.Format("{0}_VAR{1:000}", test.Name, ++index);
                    newTest.DriverParameters["variation"] = variation;
                    tests.Add(newTest);
                }
            }
            return tests;
        }

        /// <summary>
        /// Expand all variations in Model
        /// </summary>
        private List<string> ExpandModel(Model model)
        {
            List<VariationGeneration.Parameter<XmlNode>> vgParameters = new List<VariationGeneration.Parameter<XmlNode>>();

            int index = 0;

            foreach (Parameter parameter in model.Parameters)
            {
                VariationGeneration.Parameter<XmlNode> vgParameter = new VariationGeneration.Parameter<XmlNode>(string.Format("param{0}", ++index));
                foreach (XmlNode value in parameter.Values)
                {
                    vgParameter.Add(value);
                }
                vgParameters.Add(vgParameter);
            }

            List<string> variations = new List<string>();

            // Need to create a new list of VariationGeneration.ParameterBase and populate 
            // it with our parameters because we cannot cast a List<Parameter<XmlNode>> to 
            // IEnumerable<VariationGeneration.ParameterBase> in .net 3.5
            List<VariationGeneration.ParameterBase> vgParameterList = new List<VariationGeneration.ParameterBase>();
            foreach (var vgParam in vgParameters)
            {
                vgParameterList.Add(vgParam);
            }

            VariationGeneration.Model vgModel = new VariationGeneration.Model(vgParameterList);
            
            // Right now we call Generate Variations with the maximum parameter count
            // This will generate all possible variations
            // TODO : Add support for order & strength for a more controlled model
            foreach (VariationGeneration.Variation variation in vgModel.GenerateVariations(model.Parameters.Count))
            {
                // Variation is a Dictionary<string,object>, so
                // here we select the value from each KeyValuePair (kvp)
                // in each Variation and them as a list into CreateXmlVariation
                variations.Add(CreateXmlVariation(model.Type, variation.Select(kvp => (XmlNode)kvp.Value).ToList()));
            }

            return variations;
        }

        /// <summary>
        /// Create xml string of variartion that will be passed to test framework
        /// </summary>
        private string CreateXmlVariation(string type, List<XmlNode> variations)
        {
            string xmlVariation = string.Empty;
            using (StringWriter writer = new StringWriter())
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(writer))
                {
                    xmlWriter.WriteStartElement(type);
                    foreach (XmlNode variation in variations)
                    {
                        xmlWriter.WriteRaw(variation.OuterXml);
                    }
                    xmlWriter.WriteEndElement();
                }
                xmlVariation = writer.ToString();
            }
            return xmlVariation;
        }

        /// <summary>
        /// Represents an Xml Model defined in XTC filed
        /// </summary>
        private class Model
        {
            /// <summary />
            public Model() { Parameters = new List<Parameter>(); }

            /// <summary />
            public static Model Load(XmlNode xml)
            {
                if (xml == null)
                {
                    throw new ArgumentNullException("xml");
                }

                bool hasType = xml.Attributes["Type"] != null;

                Model model = new Model { Type = hasType ? xml.Attributes["Type"].Value : default(string) };

                // select parameter nodes
                foreach (XmlNode param in xml.SelectNodes("Parameter"))
                {
                    Parameter parameter = new Parameter();
                    // if any child nodes exist add as parameter value
                    foreach (XmlNode value in param.ChildNodes)
                    {
                        parameter.Values.Add(value);
                    }
                    model.Parameters.Add(parameter);
                }
                return model;
            }

            /// <summary>
            /// Type of object being modeled
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Collection of Parameters for model
            /// </summary>
            public List<Parameter> Parameters { get; set; }
        }

        /// <summary>
        /// Collection of XmlNodes that represent parameter values.
        /// </summary>
        private class Parameter
        {
            /// <summary>ctor</summary>
            public Parameter() { this.Values = new List<XmlNode>(); }

            /// <summary>
            /// List parameter values or variations
            /// </summary>
            public List<XmlNode> Values { get; set; }
        }
    }
}