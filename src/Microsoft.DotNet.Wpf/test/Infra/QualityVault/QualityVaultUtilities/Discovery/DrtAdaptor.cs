// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Collections.ObjectModel;

namespace Microsoft.Test.Discovery
{
    /// <summary>
    ///
    /// </summary>
	public class DrtAdaptor : DiscoveryAdaptor
    {
        #region Constructor

        /// <summary/>
        public DrtAdaptor()
        {
        }

        #endregion

        /// <summary>
        /// Discover DRTs from drt manifest.
        /// </summary>
        /// <param name="testManifestPath">Should be path to rundrtlist.txt</param>
        /// <param name="defaultTestInfo"></param>
        /// <returns>TestInfos for drts.</returns>
        //List<TestInfo> Discover(string testBinRootPath, string targetFilename, TestInfo defaultTestInfo)
        public override IEnumerable<TestInfo> Discover(FileInfo testManifestPath, TestInfo defaultTestInfo)
        {
            string targetFilename = testManifestPath.FullName;

            if (!File.Exists(targetFilename))
                throw new FileNotFoundException(Path.GetFullPath(targetFilename));

            // Deserialize Drt manifest file into a list of Drt objects.
            XmlTextReader reader = new XmlTextReader(targetFilename);
            List<Drt> drtDefinitions = (List<Drt>)ObjectSerializer.Deserialize(reader, typeof(List<Drt>), null);

            // Convert each Drt object into a TestInfo.
            List<TestInfo> drts = new List<TestInfo>();
            foreach (Drt drtDef in drtDefinitions)
            {
                // Initialize TestInfo.
                TestInfo drtTestInfo = defaultTestInfo.Clone();

                ContentPropertyBag driverArgs = drtTestInfo.DriverParameters;
                // Store Executable, Owner, and Args in the property bag for DrtDriver to consume.
                driverArgs["exe"] = drtDef.Executable;
                if (!String.IsNullOrEmpty(drtDef.Args))
                    driverArgs["args"] = drtDef.Args;
                driverArgs["owner"] = drtDef.Owner;

                drtTestInfo.Name = Path.GetFileNameWithoutExtension(driverArgs["exe"]) + "(" + drtDef.Args + ")";
                drtTestInfo.DriverParameters = driverArgs;
                drtTestInfo.Area = drtDef.Team;

                if (drtDef.Timeout > 0)
                {
                    drtTestInfo.Timeout = TimeSpan.FromSeconds(drtDef.Timeout);
                }

                SelectConfiguration(drtTestInfo, drtDef);

                // Convert drt support files to a source/destination pair for TestInfo.
                foreach (string file in drtDef.SupportFiles)
                {
                    //The path may be to a directory or a file.  If we think the path is to a directory
                    //then the destination and the source are the same
                    //otherwise the destination is the containing directory of the source file
                    //We assume people are not specifying files that have no extention
                    //and that * is used only in filenames
                    TestSupportFile supportFile = new TestSupportFile();
                    supportFile.Source = Path.Combine("DRT", file);
                    supportFile.Destination = string.IsNullOrEmpty(Path.GetExtension(supportFile.Source)) && !supportFile.Source.Contains("*") ? file : Path.GetDirectoryName(file);
                    drtTestInfo.SupportFiles.Add(supportFile);
                }

                // Make sure that exe name wasn't explicitly listed as a support file.
                //TODO: Investigate DrtAdaptor and see whether this code can be removed.
                //      In the new world all support files should be declared explicitly
                //      instead of implicitly.
                string exePath = Path.Combine("DRT", driverArgs["exe"]);
                if (!drtTestInfo.SupportFiles.Select(s => s.Source).Contains(exePath))
                {
                    drtTestInfo.SupportFiles.Add(new TestSupportFile() { Source = exePath });
                }

                // Append relative path to all deployments.
                foreach (string deployment in drtDef.Deployments)
                    drtTestInfo.Deployments.Add(Path.Combine("DRT", deployment));

                drts.Add(drtTestInfo);
            }
            return drts;
        }

        //Selects the appropriate configuration mix for DRTs
        private void SelectConfiguration(TestInfo drtTestInfo, Drt drtDefinition)
        {
            //Old infra supported possibilities via this scheme of enumeration which was incompatible with the configuration mix technology.
            //We'll support until we can move tests to use Configuration Filtering directly- ie (Win7/Server 2008, IA64/AMD64 opt out cases lead to combinatorial explosion in current design)

            //if user doesn't specify "all" or Vista, but does specify xp, we will filter for XP
            string oses = drtDefinition.OS.ToLowerInvariant();
            bool allOs = oses.Equals("all");
            bool xp = oses.Contains("xp");                  //Implicitly always support, like with old infra (Legacy consistent)
            bool vista = oses.Contains("vista");            //Legacy feature, Allow opt-out
            //No other choices at this point- Canon DRT platforms should be reviewed. ie- Win7 instead of Vista?

            //If user doesn't specify "all" or amd64, but does specify x86, they will only run x86 - IA64 goes along for the ride
            string architectures = drtDefinition.Architecture.ToLowerInvariant();
            bool allArchitectures = architectures.Equals("all");
            bool x86 = architectures.Contains("x86");       //Implicitly always support, like with old infra (Legacy consistent)

            bool amd64 = architectures.Contains("amd64");   //No direct control on this (Legacy Consistent)
            bool ia64 = architectures.Contains("ia64");     //No direct control on this (Legacy Consistent)


            drtTestInfo.Configurations = new Collection<string>();
            //Filter against incompatability w/Vista OS, and amd64 Platform
            if (!allOs && xp && !vista)
            {
                if (!allArchitectures && x86 && !amd64)
                {
                    drtTestInfo.Configurations.Add(@"Infra\Configurations\DRT_XP_X86.xml");
                }
                else
                {
                    drtTestInfo.Configurations.Add(@"Infra\Configurations\DRT_XP.xml");
                }
            }
            else if (!allArchitectures && x86 && !amd64)
            {
                drtTestInfo.Configurations.Add(@"Infra\Configurations\DRT_X86.xml");
            }
            //default configuration filter accepts all platforms.
        }
    }
}
