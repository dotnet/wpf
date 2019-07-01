// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System;
using System.Globalization;

namespace Microsoft.Test.Filtering
{
    /// <summary>
    /// A serializable container for describing constraints on machine configuration.
    /// Any fields left unspecified are treated as unconstrained.
    /// All specified constraint fields must be satisfied in order for the mix to be satisfied.
    /// </summary>
    internal class Configuration
    {
        /// <summary>
        /// Specifies what Operating Systems are supported in this configuration.
        /// </summary>
        public Collection<string> OperatingSystems { get; set; }

        /// <summary>
        /// Specifies what CPU architectures are supported in this configuration.
        /// </summary>
        public Collection<string> Architectures { get; set; }

        /// <summary>
        /// Specifies if the configuration requires Multi-Monitor support.
        /// </summary>
        public string MultiMonitor { get; set; }

        public string Culture { get; set; }

        /// <summary>
        /// Reports if this set of configuration mix constraints can be satisfied on the specified
        /// machine. If no machine is specified, active machine is queried.
        /// </summary>
        /// <returns></returns>
        internal bool IsSatisfied(MachineRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            //Note - The query operators may be worth caching... Need to see how that plays out.
            if (!HasMatch(OperatingSystems, record.OperatingSystem))
            {
                Explanation = "Configuration OSes: " + OperatingSystems.ToCommaSeparatedList() + " did not match " + record.OperatingSystem;
                return false;
            }
            else if (!HasMatch(Architectures, record.Architecture))
            {
                Explanation = "Configuration Architectures: " + Architectures.ToCommaSeparatedList() + " did not match: " + record.Architecture;
                return false;
            }
            else if (Culture != null && !Culture.Equals(record.Culture, StringComparison.OrdinalIgnoreCase))
            {
                Explanation = "Configuration Culture: " + Culture + " did not match:" + record.Culture;
                return false;
            }
            else if (!String.IsNullOrEmpty(MultiMonitor) 
                && (String.Compare(MultiMonitor, "True", StringComparison.InvariantCultureIgnoreCase) == 0)
                && (record.MonitorCount < 2))
            {
                Explanation = "Configuration MultiMonitor did not match. Test need multiple monitors, actual monitors enabled: " + record.MonitorCount;
                return false;
            }
            else
            {
                return true;
            }
        }

        internal string Explanation { get; set; }

        internal static MachineRecord QueryMachine()
        {
            MachineRecord record = new MachineRecord();
            record.Architecture = QueryArchitecture();
            record.Culture = QueryCulture();
            record.MonitorCount = QueryMonitorCount();
            record.Name = QueryName();
            record.OperatingSystem = QueryOperatingSystem();
            return record;
        }

        private static string QueryArchitecture()
        {
            return Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        }

        private static string QueryCulture()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        private static int QueryMonitorCount()
        {
            return MonitorInfo.MonitorCount;
        }

        private static string QueryName()
        {
            return System.Environment.MachineName;
        }

        /// <summary>
        /// Definitive data on mapping OS'es here: http://msdn.microsoft.com/en-us/library/ms724834(VS.85).aspx
        /// </summary>
        /// <returns></returns>
        private static string QueryOperatingSystem()
        {
            Version os = Environment.OSVersion.Version;
            string currentOS = "Other-[Not Currently Supported]";

            if (os.Major == 5) //5 covers post NT4/Pre Vista OS'es
            {
                if (os.Minor == 0)
                {
                    currentOS = "Windows 2000";
                }
                else if (os.Minor == 1)
                {
                    currentOS = "Windows XP";
                }
                else if (os.Minor == 2)
                {
                    if (OSVersionInfo.ProductType == OSProductType.Workstation)
                    {
                        currentOS = "Windows XP";    //Yes, this shows up in two places.
                    }
                    else
                    {
                        currentOS = "Windows Server 2003";
                    }
                }
            }
            else if (os.Major == 6)
            {
                //Server 2008 is only non-workstation 6 gen OS right now                
                if (OSVersionInfo.ProductType == OSProductType.Workstation)
                {
                    switch (os.Minor)
                    {
                        case 0:
                            currentOS = "Windows Vista";
                            break;
                        case 1:
                            currentOS = "Windows 7";
                            break;
                        case 2:
                            currentOS = "Windows 8";
                            break;
                        case 3:
                            currentOS = "Windows 8.1";
                            break;
                    }
                }
                else
                {
                    switch (os.Minor)
                    {
                        case 0:
                            currentOS = "Windows Server 2008";
                            break;
                        case 1:
                            currentOS = "Windows Server 2008 R2";
                            break;
                        case 2:
                            currentOS = "Windows Server 2012";
                            break;
                        case 3:
                            currentOS = "Windows Server 2012 R2";
                            break;
                    }
                }
            }
            else if (os.Major == 10)
            {
                if (OSVersionInfo.ProductType == OSProductType.Workstation)
                {
                    if (os.Minor == 0)
                    {
                        currentOS = "Windows 10";
                    }
                }
                else
                {
                    if (os.Minor == 0)
                    {
                        currentOS = "Windows Server 2016";
                    }
                }
            }

            return currentOS;
        }

        private bool HasMatch(Collection<string> collection, string element)
        {
            if (collection == null)
            {
                return true;
            }
            foreach (string s in collection)
            {
                if (s.Equals(element, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static Configuration LoadConfigurationMix(string mixFileName, DirectoryInfo testBinariesDirectory)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(testBinariesDirectory.FullName, mixFileName));
            Configuration configurationMix = null;
            using (XmlTextReader textReader = new XmlTextReader(fileInfo.OpenRead()))
            {
                configurationMix = (Configuration)ObjectSerializer.Deserialize(textReader, typeof(Configuration), null);
            }
            return configurationMix;
        }
    }
}