// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.Diagnostics;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;
using Microsoft.Win32;

using System;
using System.Globalization;

namespace Microsoft.Test.Loaders.Steps
{
    /// <summary>
    /// Step that allows skipping the step if the specified OS, IE, or bit-flavor version is seen.
    /// </summary>
    public class MachineInfoStepDisabler : LoaderStep
    {
        #region Public Members

        public string NeedsMediaPlayer = "";

        public string DotNetVersion = "";

        public string IEVersion = "unspecified";

        public string IsServer = "";

        public string IsPersonalEdition = "";

        public string IsWow64Process = "";

        public int OSMajorVersion = -1;

        public int OSMinorVersion = -1;

        public CultureInfo UICulture = null;

        public string ProcessorArch = "";

        public string DoNotRunReason = "No explanation specified";

        #endregion

        #region Private Members
        bool shouldRunChildSteps = true;
        #endregion

        protected override bool BeginStep()
        {
            bool tempBool = false;

            if (DotNetVersion.Length > 0)
            {
                string[] version = DotNetVersion.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                try
                {
                    if ((Environment.Version.Major == int.Parse(version[0]) && (Environment.Version.Minor == int.Parse(version[1]))))
                    {
                        GlobalLog.LogEvidence("Test specified to not run on .NET Version " + DotNetVersion);
                        shouldRunChildSteps = false;
                    }
                }
                catch (Exception ex)
                {
                    GlobalLog.LogDebug("Hit exception trying to filter on .NET version:\n" + ex.Message + "\n" + ex.StackTrace);
                }
            }

            if (SystemInformation.Current.IEVersion.StartsWith(IEVersion))
            {
                GlobalLog.LogEvidence("Test specified to not run on IE Version " + IEVersion + " ( Detected : " + SystemInformation.Current.IEVersion + ")");
                shouldRunChildSteps = false;
            }

            if (bool.TryParse(IsServer, out tempBool))
            {
                if (tempBool == SystemInformation.Current.IsServer)
                {
                    GlobalLog.LogEvidence("Test specified to " + (tempBool ? "not" : "only") + " run on Server SKUs.");
                    shouldRunChildSteps = false;
                }
            }

            if (bool.TryParse(NeedsMediaPlayer, out tempBool))
            {
                if (tempBool)
                {
                    GlobalLog.LogEvidence("Test is specified to require Media Player to function properly.");
                    RegistryKey wmvClassOpenWith = Registry.ClassesRoot.OpenSubKey(".wmv\\OpenWithProgIds");
                    if ((wmvClassOpenWith != null) && (wmvClassOpenWith.ValueCount > 0))
                    {
                        GlobalLog.LogEvidence("WMV File handlers detected, media player appears to be present.  Will run child steps");
                    }
                    else
                    {
                        GlobalLog.LogEvidence("It looks like there aren't .wmv handlers on the system, so disabling child steps.");
                        GlobalLog.LogEvidence("(Many Server SKUs lack Windows Media Player by default)");
                        shouldRunChildSteps = false;
                    }
                }
            }

            if (bool.TryParse(IsPersonalEdition, out tempBool))
            {
                if (tempBool == SystemInformation.Current.IsPersonalEdition)
                {
                    GlobalLog.LogEvidence("Test specified to " + (tempBool ? "not" : "only") + " run on Home/Personal Edition SKUs ");
                    shouldRunChildSteps = false;
                }
            }

            if (bool.TryParse(IsWow64Process, out tempBool))
            {
                if (tempBool == SystemInformation.Current.IsWow64Process)
                {
                    GlobalLog.LogEvidence("Test specified to " + (tempBool ? "not" : "only") + " run in WOW64 process");
                    shouldRunChildSteps = false;
                }
            }

            if (SystemInformation.Current.MajorVersion == OSMajorVersion)
            {
                if ((OSMinorVersion == -1))
                {
                    GlobalLog.LogEvidence("Test specified to not run in OS version " + OSMajorVersion);
                    shouldRunChildSteps = false;
                }
                else if (SystemInformation.Current.MinorVersion == OSMinorVersion)
                {
                    GlobalLog.LogEvidence("Test specified to not run in OS version " + OSMajorVersion + "." + OSMinorVersion);
                    shouldRunChildSteps = false;
                }
            }

            if ((UICulture != null) && SystemInformation.Current.UICulture == UICulture)
            {
                GlobalLog.LogEvidence("Test specified to not run in UI Culture " + UICulture.ToString());
                shouldRunChildSteps = false;
            }

            if (ProcessorArch.ToLowerInvariant() == SystemInformation.Current.ProcessorArchitecture.ToString().ToLowerInvariant())
            {
                GlobalLog.LogEvidence("Test specified to not run on Processor Architecture " + ProcessorArch.ToString());
                shouldRunChildSteps = false;
            }

            if (shouldRunChildSteps)
            {
                return true;
            }
            else
            {
                GlobalLog.LogEvidence("Cannot execute test.  This test is blocked because: " + DoNotRunReason);
                if (TestLog.Current == null)
                {
                    TestLog log = new TestLog("Ignored Test: " + DoNotRunReason);
                    log.Result = TestResult.Ignore;
                    log.Close();
                    return false;
                }
                TestLog.Current.Result = TestResult.Ignore;
                return false;
            }
        }

        protected override bool EndStep()
        {
            return shouldRunChildSteps;
        }
    }
}