// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;
using Microsoft.Test.Diagnostics;
using System.Security.Principal;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Microsoft.Test.CrossProcess;
using Microsoft.Win32;

namespace Microsoft.Test.Loaders.Steps 
{

    /// <summary>
    /// Calls an API that sets the third-party browser state on the machine.
    /// Currently only FireFox is supported.
    /// </summary>    
    public class BrowserStateStep : LoaderStep
    {
        #region Public Members

        /// <summary>
        /// Third party browser to be installed / uninstalled
        /// </summary>
        public BrowserIdentifier ThirdPartyBrowser =  BrowserIdentifier.None;

        /// <summary>
        /// Gets or sets whether the browser specified by ThirdPartyBrowser value is installed.
        /// Ignored if ThirdPartyBrowser=None.
        /// </summary>
        public bool Installed = false;

        /// <summary>
        /// Gets or sets whether the browser specified by ThirdPartyBrowser value is the default browser.
        /// This is ignored if ThirdPartyBrowser=None.
        /// </summary>
        public bool DefaultBrowser = false;

        #endregion

        #region Step Implementation
        /// <summary>
        /// Sets third party browser state on the machine.  Currently only supports FireFox.
        /// </summary>
        /// <returns>true</returns>
        public override bool DoStep() 
        {
            GlobalLog.LogStatus("In BrowserStateStep, but not calling state change APIs; just setting previously set property values and ensuring plugin and extension are properly installed.");

            // Starting with v4.0 and v3.5 Sp1+ (as shipped w/ Win7) the Firefox extension isn't available
            // until Firefox has been run at least once.  Do so before returning... 
            string mozillaExePath = Environment.GetEnvironmentVariable("ProgramFiles");
            if (Environment.GetEnvironmentVariable("ProgramFiles(X86)") != null)
            {
                mozillaExePath = Environment.GetEnvironmentVariable("ProgramFiles(X86)");
            }
            mozillaExePath += "\\Mozilla Firefox\\firefox.exe";

            
            // Hard code 3.X path since there's no simple way to get the 3.5 framework dir...
            string firefoxExtensionPath = Microsoft.Test.Diagnostics.SystemInformation.Current.FrameworkWpfPath;
            if (firefoxExtensionPath.ToLowerInvariant().Contains("v3.0"))
            {
                firefoxExtensionPath = Environment.GetEnvironmentVariable("WINDIR") + @"\Microsoft.NET\Framework\v3.5\Windows Presentation Foundation\";
            }
            // "-install-global-extension " is not available starting w/ FF 3.6, so use the universal "registry" version:
            firefoxExtensionPath += "DotNetAssistantExtension\\";

            // Being 32-bit, the plugin is only ever placed in the 32-bit framework path
            firefoxExtensionPath = firefoxExtensionPath.Replace("Framework64", "Framework");

            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Mozilla\Firefox\Extensions", "{20a82645-c095-46ed-80e3-08825760534b}", firefoxExtensionPath, RegistryValueKind.String);

            if (File.Exists(mozillaExePath) && Directory.Exists(firefoxExtensionPath))
            {
                try
                {
                    // No arguments needed... just want it to start up so it can show the "just installed your Addin" dialog and go away.
                    Process ffProc = Process.Start(mozillaExePath, "about:robots");
                    ffProc.WaitForExit(10000);
                    ffProc.Refresh();    
                    ffProc.CloseMainWindow();
                    Thread.Sleep(1000);
                
                    if (!ffProc.HasExited)
                    {
                        try
                        {                            
                            ffProc.Kill();
                        }
                        catch (System.InvalidOperationException)
                        { 
                            // Do nothing... happens when process manages to exit between the if and the kill...
                        }
                    }
                }
                catch (Exception e)
                {
                    GlobalLog.LogStatus("Hit exception trying to pre-launch firefox (needed for Extension update)\n " + e.Message + "\n" + e.StackTrace);
                }
            }
            else
            {
                GlobalLog.LogStatus("Error: Either can't find Mozilla exe at " + mozillaExePath + " or extension at " + firefoxExtensionPath + ".  Attempting to continue...");
            }
            GlobalLog.LogStatus("Cleaning up any leftover FireFox.exe processes from previous runs (can break tests)");
            foreach (Process firefoxProcess in Process.GetProcessesByName("firefox"))
            {
                try
                {
                    firefoxProcess.Kill();
                }
                catch (System.InvalidOperationException) { } // do nothing, happens a couple % of the time...
            }
            return true;            
        }
        #endregion
    }
}