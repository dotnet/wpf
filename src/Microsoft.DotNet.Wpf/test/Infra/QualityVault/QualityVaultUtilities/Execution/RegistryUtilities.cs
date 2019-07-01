// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.Win32;

//Under execution namespace, as no one else modifies registry
namespace Microsoft.Test.Execution
{
    /// <summary>
    /// Provides common services for Registry changes.
    /// Contains policy for apply and rollback of settings.
    /// </summary>
    internal static class RegistryUtilities
    {
        private static readonly string backupSuffix = ".TestInfraBackup";
    
        /// <summary>
        /// Gets a Registry key - create one if it does not exist.
        /// </summary>
        /// <param name="registryPath"></param>
        /// <returns></returns>
        internal static RegistryKey ObtainKey(string registryPath)
        {
            RegistryKey keyPath = Registry.LocalMachine.OpenSubKey(registryPath, true);
            if (keyPath == null)
            {
                keyPath = Registry.LocalMachine.CreateSubKey(registryPath);
            }
            return keyPath;
        }

        /// <summary>
        /// Backs up an existing registry key to an infra backup path. 
        /// Only the first call has effect, to avoid overwriting previously backed up user settings.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="keyPath"></param>
        internal static void BackupKey(RegistryKey key, string keyPath)
        {
            string sourceValue = keyPath;
            string destValue = string.Concat(keyPath, backupSuffix);

            object value = key.GetValue(sourceValue);
            if (value != null && key.GetValue(destValue) == null)

            {
                key.SetValue(destValue, value);
                key.Flush();
            }
        }

        /// <summary>
        /// Rolls back pre-infra user settings
        /// </summary>
        /// <param name="key"></param>
        /// <param name="subKey"></param>
        internal static void RollbackKey(RegistryKey key, string subKey)
        {
            string sourceSubKey = string.Concat(subKey, backupSuffix);
            string destSubKey = subKey;

            // If the dest keys are non null then restore, otherwise delete the source keys.
            if (key.GetValue(sourceSubKey) != null)
            {
                MoveRegKey(key, sourceSubKey, destSubKey);
            }
            else if (key.GetValue(destSubKey) != null)
            {
                key.DeleteValue(destSubKey);
            }
        }

        /// <summary>
        /// Move registry value from one subkey to another using an open RegistryKey.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="sourceSubKey"></param>
        /// <param name="destSubKey"></param>
        internal static void MoveRegKey(RegistryKey key, string sourceSubKey, string destSubKey)
        {
            object value = key.GetValue(sourceSubKey);
            if (value != null)
            {
                key.SetValue(destSubKey, value);
                key.DeleteValue(sourceSubKey);
            }
            key.Flush();
        }
    }
}