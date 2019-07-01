// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Globalization;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    /// The XtcAdaptor discovers test cases by searching for all XTC files
    /// in or under a given directory and parsing testcase metadata contained
    /// therein.
    /// </summary>
    public class XtcAdaptor : DiscoveryAdaptor
    {
        #region Override Members

        /// <summary>
        /// Discover a set of test cases from an xtc file.
        /// </summary>
        /// <param name="testManifestPath">Xtc to examine for tests.</param>
        /// <param name="defaultTestInfo">The default values for a TestInfo.</param>
        /// <returns>Collection of discovered tests.</returns>
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            if (testManifestPath == null)
            { 
                throw new ArgumentNullException("testManifestPath");
            }
            if (!testManifestPath.Exists)
            {
                throw new FileNotFoundException("testManifestPath", testManifestPath.FullName);
            }
            if (!String.Equals(testManifestPath.Extension, ".xtc", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Adaptor only supports xtc files.");
            }
            if (defaultTestInfo == null)
            {
                throw new ArgumentNullException("defaultTestInfo");
            }

            List<TestInfo> tests = ParseXtc(testManifestPath, defaultTestInfo);

            return tests;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Parse an XTC file for metadata describing testcase(s).
        /// </summary>
        /// <param name="testManifestPath">Xtc file.</param>
        /// <param name="defaultTestInfo">The default values for a TestInfo.</param>
        /// <returns>List of TestInfo objects representing the testcase(s).</returns>
        private static List<TestInfo> ParseXtc(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            List<TestInfo> tests = new List<TestInfo>();

            // TODO: Create xsd to describe the schema, use version of Create
            // that takes an xsd for validation. This will also enable
            // Intellisense in VS.

            XmlTextReader xmlTextReader = new SkipDataReader(testManifestPath.FullName);

            TestInfo xtcTestInfo = defaultTestInfo.Clone();

            // Add the name of the xtc file as a driver parameter.
            if (xtcTestInfo.DriverParameters == null)
            {
                xtcTestInfo.DriverParameters = new ContentPropertyBag();
            }
            xtcTestInfo.DriverParameters["XtcFileName"] = testManifestPath.Name;

            int xtcTestIndex = 1;

            // Process the text reader until we hit EOF. We are interested
            // in Elements named DefaultTestInfo or Test, in which cases we
            // deserialize into a TestInfo via ObjectSerializer. If the
            // current text reader node was an element but of a different
            // name, or not an element at all, we just Read() past it.
            while (!xmlTextReader.EOF)
            {
                if (xmlTextReader.NodeType == XmlNodeType.Element)
                {
                    if (xmlTextReader.Name == "DEFAULTTESTINFO")
                    {
                        TestInfo deserializedDefaultTestInfo = (TestInfo)ObjectSerializer.Deserialize(xmlTextReader, typeof(TestInfo), null);
                        xtcTestInfo.Merge(deserializedDefaultTestInfo);
                    }
                    else if (xmlTextReader.Name == "TEST")
                    {
                        TestInfo deserializedTest = (TestInfo)ObjectSerializer.Deserialize(xmlTextReader, typeof(TestInfo), null);
                        TestInfo test = xtcTestInfo.Clone();
                        test.Merge(deserializedTest);
                        test.DriverParameters["XtcTestIndex"] = xtcTestIndex.ToString(CultureInfo.InvariantCulture);
                        xtcTestIndex++;
                        tests.Add(test);
                    }
                    else
                    {
                        xmlTextReader.Read();
                    }
                }
                else
                {
                    xmlTextReader.Read();
                }
            }

            return tests;
        }

        /// <summary>
        /// XmlTextReader that skips any 'DATA' elements.
        /// </summary>
        private class SkipDataReader : XmlTextReader
        {
            /// <summary>
            /// Since this a private class we only need this one constructor.
            /// </summary>
            /// <param name="url">File path.</param>
            public SkipDataReader(string url)
                : base(url)
            {
            }

            /// <summary>
            /// Mimic XmlTextReader.Read(), save for skipping 'DATA' elements.
            /// </summary>
            /// <returns></returns>
            public override bool Read()
            {
                bool readResult = base.Read();

                if (this.NodeType == XmlNodeType.Element && this.Name == "DATA")
                {
                    this.ReadOuterXml();
                }

                return readResult;
            }
        }

        #endregion
    }
}