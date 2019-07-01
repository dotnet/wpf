// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// ModelAttribute adaptor.
    /// </summary>
	public class ModelAttributeAdaptor : TestAttributeAdaptor
    {
	    #region Constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ModelAttributeAdaptor()
        {
            // Change default attribute type so adaptor only finds ModelAttributes.
            this.AttributeType = typeof(ModelAttribute);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Create TestInfo from a TestAttribute, Type, default TestInfo, and XTC file.
        /// </summary>
        protected override ICollection<TestInfo> BuildTestInfo(TestAttribute testAttribute, Type ownerType, TestInfo defaultTestInfo)
        {
            ModelAttribute modelAttribute = (ModelAttribute)testAttribute;

            List<TestInfo> newTests = new List<TestInfo>();

            if (String.IsNullOrEmpty(modelAttribute.XtcFileName))
            {
                // No xtc file name, return empty list.
                Trace.TraceWarning("Xtc file name is null or empty. Aborting discovery on class " + ownerType.FullName + ".");
                return newTests;
            }
            
            if (modelAttribute.ModelStart > modelAttribute.ModelEnd)
            {
                // Invalid end index, return empty list.
                Trace.TraceWarning("The model end index cannot be greater than the start index. Aborting discovery on class " + ownerType.FullName + ".");
                return newTests;
            }
            
            // Build test info as we would for normal test attribute.
            // This should only return one test case.
            IEnumerable<TestInfo> baseTests = base.BuildTestInfo(modelAttribute, ownerType, defaultTestInfo);
            if (baseTests.Count() > 1)
            {
                // Too many tests, return empty list.
                Trace.TraceWarning("Parsing single ModelAttribute produced multiple test infos before reading XTC, aborting discovery on class " + ownerType.FullName + ".");
                return newTests;
            }

            if (baseTests.Count() == 0)
            {
                // Too few tests, return empty list.
                Trace.TraceWarning("Failure parsing ModelAttribute on class " + ownerType.FullName + " before reading XTC. Aborting discovery.");
                return newTests;
            }

            TestInfo baseTest = base.BuildTestInfo(modelAttribute, ownerType, defaultTestInfo).First();

            baseTest.DriverParameters["ModelClass"] = baseTest.DriverParameters["Class"];
            baseTest.DriverParameters["ModelAssembly"] = baseTest.DriverParameters["Assembly"];
            baseTest.DriverParameters["XtcFileName"] = modelAttribute.XtcFileName;
            TestSupportFile tsf = new TestSupportFile();
            tsf.Source = modelAttribute.XtcFileName;
            baseTest.SupportFiles.Add(tsf);

            int modelStart, modelEnd;
            try
            {
                GetStartEndTestCaseFromXtc(modelAttribute.XtcFileName, out modelStart, out modelEnd);
            }
            catch (ArgumentException e)
            {
                // Xtc file does not exist, return empty list.
                Trace.TraceWarning(e.Message + " Discovery aborted on class " + ownerType.FullName + ".");
                return newTests;
            }

            // Attribute range overrides that found in the xtc file.
            if (modelAttribute.ModelStart >= 0)
                modelStart = modelAttribute.ModelStart;
            if (modelAttribute.ModelEnd >= 0)
                modelEnd = modelAttribute.ModelEnd;

            if (modelAttribute.ExpandModelCases)
            {
                // Create new test info for each test in the xtc and pass TIndex to driver.
                for (int testIndex = modelStart; testIndex <= modelEnd; testIndex++)
                {
                    baseTest.DriverParameters["TIndex"] = testIndex.ToString(CultureInfo.InvariantCulture);
                    newTests.Add(baseTest.Clone());
                }
            }
            else
            {
                // Create a single test info for all the tests in the xtc and pass range to driver.
                baseTest.DriverParameters["ModelStart"] = modelStart.ToString(CultureInfo.InvariantCulture);
                baseTest.DriverParameters["ModelEnd"] = modelEnd.ToString(CultureInfo.InvariantCulture);
                newTests.Add(baseTest);
            }
        
            return newTests;
        }

        /// <summary>
        /// Gets the Start Test Case and the end Test Case from a XTC.
        /// </summary>
        protected static void GetStartEndTestCaseFromXtc(string fileName, out int modelStart, out int modelEnd)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("XTC file '" + fileName + "' does not exist.", "fileName");
            }

            // Load xtc file.
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            // Construct the XmlNamespaceManager used for xpath queries later.
            NameTable ntable = new NameTable();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(ntable);
            nsmgr.AddNamespace("x", xmlDoc.DocumentElement.NamespaceURI);

            // Query for all TEST nodes. Use the count for modelEnd.
            XmlNodeList testNodes = xmlDoc.SelectNodes("/x:XTC/x:TEST", nsmgr);

            modelStart = 1;
            modelEnd = testNodes.Count;
        }

        #endregion
    }
}
