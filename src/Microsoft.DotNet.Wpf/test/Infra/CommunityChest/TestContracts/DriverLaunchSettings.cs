// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;

namespace Microsoft.Test
{
    /// <summary>
    /// Provides singular point for placement and retrieval of driver launch settings.
    /// Tests have no reason to use this.
    /// </summary>
    public static class DriverLaunchSettings
    {

        #region Public API For consumption by DriverState(InternalUtilities) and QualityVault

        /// <summary>
        /// Stores settings for consumption by Driver
        /// </summary>
        /// <param name="executionDirectory"></param>
        /// <param name="TestBinRoot"></param>
        public static void StoreSettings(string executionDirectory, string TestBinRoot)
        {
            ApplyInfraSetting(executionDirectoryKey, executionDirectory);
            ApplyInfraSetting(testBinRootKey, TestBinRoot);
        }

        /// <summary>
        /// Retrieves Execution directory path.
        /// </summary>
        /// <returns></returns>
        public static string GetExecutionDirectory()
        {
            return GetInfraSetting(executionDirectoryKey);
        }

        /// <summary>
        /// Retrieves Test Binaries root path.
        /// </summary>
        /// <returns></returns>
        public static string GetTestBinRoot()
        {
            return GetInfraSetting(testBinRootKey);
        }

        #endregion

        #region Private Settings

        private static readonly string executionDirectoryKey = "ExecutionDirectory";
        private static readonly string testBinRootKey = "TestBinRoot";
        private static readonly string infraRegistryPath = @"software\microsoft\WPFTestInfra";
        private static readonly string wow6432InfraRegistryPath = @"software\Wow6432Node\microsoft\WPFTestInfra";

        #endregion

        #region Private Implementation
        /// <summary>
        /// Sets the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        private static void ApplyInfraSetting(string name, string val)
        {            
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(infraRegistryPath))
            {
                key.SetValue(name, val, RegistryValueKind.String);
                key.Flush();
            }
            //This step creates a duplicate set of Infra settings in the Windows-On-Windows (Wow) 32 bit shadow registry 
            // present on a 64 bit machine
            // Tests running in explicit 32 bit mode will be redirected to those entries automatically by Windows.
            if (System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "AMD64")
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(wow6432InfraRegistryPath))
                {
                    key.SetValue(name, val, RegistryValueKind.String);
                    key.Flush();
                }
            }
        }

        /// <summary>
        /// Reports the requested key. Returns null if undefined.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetInfraSetting(string name)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(infraRegistryPath, false))
            {
                return (string)key.GetValue(name);
            }
        }
        #endregion
    }
}
