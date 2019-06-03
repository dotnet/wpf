// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// Description: DeploymentExceptionMapper class definition.
//

//---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Deployment.Application;
using System.Text.RegularExpressions;

namespace MS.Internal
{
    internal enum MissingDependencyType
    {
        Others = 0,
        WinFX = 1,
        CLR = 2
    }

    internal static class DeploymentExceptionMapper
    {
        // This is the hardcoded fwlink query parameters, the only dynamic data we pass to fwlink server
        // is the WinFX or CLR version we parse from the ClickOnce error message.
        // Product ID is always 11953 which is the WinFX Runtime Components and subproduct is always bootwinfx
        // The winfxsetup.exe is language neutral so we always specify 0x409 for languages.
        const string fwlinkPrefix = "http://go.microsoft.com/fwlink?prd=11953&sbp=Bootwinfx&pver=";
        const string fwlinkSuffix = "&plcid=0x409&clcid=0x409&";

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        #region Internal Methods
              
        // Check if the platform exception is due to missing WinFX or CLR dependency
        // Parse the exception message and find out the dependent WinFX version and create the 
        // corresponding fwlink Uri.
        static internal MissingDependencyType GetWinFXRequirement(Exception e,
                                                            InPlaceHostingManager hostingManager, 
                                                            out string version, 
                                                            out Uri fwlinkUri)
        {
            version = String.Empty;
            fwlinkUri = null;

            // ClickOnce detects whether it's running as part of the v3.5 "client" subset ("Arrowhead") and
            // if so blocks older applications that don't explicitly opt into the subset framework.
            // (Unfortunately, it does not block an application targeting the full 3.5 SP1 framework this way.)
            // The exception message has ".NET Framework 3.5 SP1" hard-coded in it (referring to the version
            // of the framework needed to run the application, which is not strictly right, but older versions
            // can't/shouldn't be installed on top of the "client" subset). 
            // To make this exception message parsing at least potentially somewhat future-proof, a regex is 
            // used that allows for some variability of syntax and version number. 
            // We don't include the "SP1" part in the fwlink query. This is for consistency with the detection 
            // via sentinel assemblies. The server can be updated to offer the latest release/SP compatible 
            // with the requested major.minor version.
            if (e is DependentPlatformMissingException)
            {
                Regex regex = new Regex(@".NET Framework (v\.?)?(?<version>\d{1,2}(\.\d{1,2})?)", 
                                        RegexOptions.ExplicitCapture|RegexOptions.IgnoreCase);
                string msg = e.Message;
                Match match = regex.Match(msg);
                if(match.Success)
                {
                    version = match.Groups[1].Value;
                    ConstructFwlinkUrl(version, out fwlinkUri);
                    return MissingDependencyType.WinFX;
                }
            }

            // Load the clickonce resource and use it to parse the exception message
            Assembly deploymentDll = Assembly.GetAssembly(hostingManager.GetType());

            if (deploymentDll == null)
            {
                return MissingDependencyType.Others;
            }

            ResourceManager resourceManager = new ResourceManager("System.Deployment", deploymentDll);
            
            if (resourceManager == null)
            {
                return MissingDependencyType.Others;
            }

            String clrProductName = resourceManager.GetString("PlatformMicrosoftCommonLanguageRuntime", CultureInfo.CurrentUICulture);
            String versionString = resourceManager.GetString("PlatformDependentAssemblyVersion", CultureInfo.CurrentUICulture);

            if ((clrProductName == null) || (versionString == null))
            {
                return MissingDependencyType.Others;
            }

            // Need to trim off the parameters in the ClickOnce strings:
            // "{0} Version {1}" -> "Version"
            // "Microsoft Common Language Runtime Version {0}" -> "Microsoft Common Language Runtime Version"
            clrProductName = clrProductName.Replace("{0}", "");
            versionString = versionString.Replace("{0}", "");
            versionString = versionString.Replace("{1}", "");

            string[] sentinelAssemblies = { 
                // The Original & "Only" 
                "WindowsBase",
                // A reference to System.Core is what makes an application target .NET v3.5. 
                // Because WindowsBase still has v3.0.0.0, it's not the one that fails the platform requirements 
                // test when a v3.5 app is run on the v3 runtime. (This additional check added for v3 SP1.)
                "System.Core", 
                // New sentinel assemblies for v3.5 SP1 (see the revision history)
                "Sentinel.v3.5Client", "System.Data.Entity" };

            // Parse the required version and trim it to major and minor only
            string excpMsg = e.Message;
            int index = excpMsg.IndexOf(versionString, StringComparison.Ordinal);

            if (index != -1)
            {
                // ClickOnce exception message is ErrorMessage_Platform* 
                // from clickonce/system.deployment.txt
                version = String.Copy(excpMsg.Substring(index + versionString.Length));
                int indexToFirstDot = version.IndexOf(".", StringComparison.Ordinal);
                int indexToSecondDot = version.IndexOf(".", indexToFirstDot+1, StringComparison.Ordinal);


                if (excpMsg.IndexOf(clrProductName, StringComparison.Ordinal) != -1)
                {
	                if (OperatingSystemVersionCheck.IsVersionOrLater(OperatingSystemVersion.Windows8))
	                {
		                // CLR version are Major.Minor.Revision
		                // Defense in depth here in case CLR changes the version scheme to major + minor only
		                // and we might never see the third dot
		                int indexToThirdDot = version.IndexOf(".", indexToSecondDot+1, StringComparison.Ordinal);
		                if (indexToThirdDot != -1)
		                {
		                    version = version.Substring(0, indexToThirdDot);
		                }

	                }
		            else if (indexToSecondDot != -1)
		            {
		                // Defense in depth here in case Avalon change the version scheme to major + minor only
		                // and we might never see the second dot
		                version = version.Substring(0, indexToSecondDot);
		            }

                    // prepend CLR to distinguish CLR version fwlink query 
                    // vs. WinFX version query. 
                    string clrVersion = String.Concat("CLR", version);
                    return (ConstructFwlinkUrl(clrVersion, out fwlinkUri) ? MissingDependencyType.CLR : MissingDependencyType.Others);
                }
                else
                {
		            if (indexToSecondDot != -1)
		            {
		                // Defense in depth here in case Avalon change the version scheme to major + minor only
		                // and we might never see the second dot
		                version = version.Substring(0, indexToSecondDot);
		            }

                    bool sentinelMissing = false;
                    foreach (string sentinelAssembly in sentinelAssemblies)
                    {
                        if (excpMsg.IndexOf(sentinelAssembly, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            sentinelMissing = true;
                            break;
                        }
                    }
                    if (!sentinelMissing)
                    {
                        version = String.Empty;
                    }
                }
            }

            return (ConstructFwlinkUrl(version, out fwlinkUri) ? MissingDependencyType.WinFX : MissingDependencyType.Others);
        }
        

        static internal void GetErrorTextFromException(Exception e, out string errorTitle, out string errorMessage)
        {
            errorTitle = String.Empty;
            errorMessage = String.Empty; 

            if (e == null)
            {
                errorTitle = SR.Get(SRID.CancelledTitle);
                errorMessage = SR.Get(SRID.CancelledText);
            }
            else if (e is DependentPlatformMissingException)
            {
                errorTitle = SR.Get(SRID.PlatformRequirementTitle);
                errorMessage = e.Message;
            }
            else if (e is InvalidDeploymentException)
            {
                errorTitle = SR.Get(SRID.InvalidDeployTitle);
                errorMessage = SR.Get(SRID.InvalidDeployText);
            }
            else if (e is TrustNotGrantedException)
            {
                errorTitle = SR.Get(SRID.TrustNotGrantedTitle);
                errorMessage = SR.Get(SRID.TrustNotGrantedText);
            }
            else if (e is DeploymentDownloadException)
            {
                errorTitle = SR.Get(SRID.DownloadTitle);
                errorMessage = SR.Get(SRID.DownloadText);
            }
            else if (e is DeploymentException)
            {
                errorTitle = SR.Get(SRID.DeployTitle);
                errorMessage = SR.Get(SRID.DeployText);
            }
            else
            {
                errorTitle = SR.Get(SRID.UnknownErrorTitle);
                errorMessage = SR.Get(SRID.UnknownErrorText) + "\n\n" + e.Message;
            }
        }

        static internal bool ConstructFwlinkUrl(string version, out Uri fwlinkUri)
        {
            string fwlink = String.Empty;
            fwlinkUri = null; 

            if (version != String.Empty)
            {
                fwlink = String.Copy(fwlinkPrefix);
                fwlink = String.Concat(fwlink, version);
                fwlink = String.Concat(fwlink, fwlinkSuffix);
                // Mitigate against proxy server caching, append today's day to the fwlink 
                // query. This matches the fwlink query from unmanaged bootwap functionality
                // in IE7.
                DateTime today = System.DateTime.Today;
                fwlink = String.Concat(fwlink, today.Year.ToString());
                fwlink = String.Concat(fwlink, today.Month.ToString());
                fwlink = String.Concat(fwlink, today.Day.ToString());
                fwlinkUri = new Uri(fwlink);
                return true;
            }

            return false;
        }

        #endregion
    }
}
