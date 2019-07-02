// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.IO;
using Microsoft.Win32;

namespace Microsoft.Test.Deployment
{
    /// <summary>
    /// Helper methods for managing Runtime deployments, such as TestCenter or ExecutionService
    /// </summary>
    public static class RuntimeDeploymentHelper
    {
        #region Public Members
        /// <summary>
        /// Gets the path to the installed deployment, as contained in the deployment's ARP entry.
        /// Returns null if the specified deployment is not installed.
        /// </summary>
        /// <param name="ApplicationId">String ID of the runtime deployment</param>
        /// <returns>Path to the requested runtime deployment</returns>
        public static string GetDeploymentInstallLocation(string ApplicationId)
        {
            string alInstallSubKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + ApplicationId;
            string deploymentManifest = (string) Registry.GetValue(alInstallSubKey, "DeploymentManifest", "");
            
            if (!string.IsNullOrEmpty(deploymentManifest))
            {
                return Path.GetDirectoryName(deploymentManifest);
            }
            
            return null;
        }

        #endregion
    }
}
