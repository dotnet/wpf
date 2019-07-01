// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading; 
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using Microsoft.Win32;
using System.Xml;
using System.Reflection;
using Microsoft.Test;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using Microsoft.Test.CrossProcess;

namespace Microsoft.Test.TestTypes 
{
    /// <summary>
    /// ApplicationMonitorLoader functionality for consumption by generic STi loader.
    /// </summary>
    public class ApplicationMonitorTest
    {
        #region Public Members

        /// <summary>
        /// AppMonitorLoader Main Method for STi consumption, configuration File version
        /// </summary>
        /// <param name="filename">File name of application monitor config file</param>
        public static void RunConfigurationFile(string filename)
        {            
            DictionaryStore.StartServer();

            ApplicationMonitorConfig config = new ApplicationMonitorConfig(filename);
            config.RunSteps();

            CloseCurrentVariationIfOneExists();
        }

        /// <summary>
        /// AppMonitorLoader Main Method for STi consumption, direct-application-launching version
        /// </summary>
        /// <param name="commandline">Arguments to AppMonitor loader, either a config file or original format</param>
        public static void RunApplication(string commandline)
        {
            ProcessStartInfo pInfo = ProcessArgs(commandline);
            DictionaryStore.StartServer();

            ApplicationMonitor appMon = new ApplicationMonitor();

            // if we're launching a ClickOnce application, clean the cache
            // Since this method precludes remote deployment and our enlistment should build properly signed manifests, there's no need to update / resign the manifests.
            if (pInfo.FileName.ToLowerInvariant().EndsWith(ApplicationDeploymentHelper.STANDALONE_APPLICATION_EXTENSION) || pInfo.FileName.ToLowerInvariant().EndsWith(ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION))
            {
                ApplicationDeploymentHelper.CleanClickOnceCache();
            }
            // shell exec the app
            appMon.StartProcess(pInfo);

            // Some Xbap tests exit early unless we add PresentationHost.exe as a monitored process.  Has to happen after StartProcess.
            // Timing is not an issue, since this is simply adding a string to a List, so will execute orders of magnitude faster than actually starting any Xbap.
            if (pInfo.FileName.ToLowerInvariant().EndsWith(ApplicationDeploymentHelper.BROWSER_APPLICATION_EXTENSION))
            {
                appMon.MonitorProcess("PresentationHost.exe");
            }
            appMon.WaitForUIHandlerAbort();
            CloseCurrentVariationIfOneExists();
            appMon.Close();
        }

        #endregion
        
        #region Private Members

        static void CloseCurrentVariationIfOneExists()
        {
            // New behavior: frequently tests don't close their own Variation
            // In fact, child processes frequently CAN'T close their own variation.
            // If we get here and Variation.Current is non-null, it must be closed if the 
            // test is to have a chance to pass.
            if (Variation.Current != null)
            {
                Variation.Current.Close();
            }
        }

        static ProcessStartInfo ProcessArgs(string commandline) 
        {
            string[] args = commandline.Split(' ');
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = args[0];
            startInfo.UseShellExecute = true;

            if (args.Length > 1)
                startInfo.Arguments = String.Join(" ", args, 1, args.Length - 1);

            return startInfo;
        }
        #endregion        
    }
}
