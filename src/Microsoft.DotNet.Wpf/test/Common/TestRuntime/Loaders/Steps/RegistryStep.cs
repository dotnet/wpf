// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Loaders.Steps 
{

    using System;
    using System.Diagnostics;
    using Microsoft.Test.Loaders;
    using Microsoft.Test.Logging;

    /// <summary>
    /// Step for writing to a registry key and automatic 
    /// rollback.
    /// </summary>
    public class RegistryStep : LoaderStep
    {
        #region private Data
        private string keyName = string.Empty;
        private string valueName = string.Empty;
        private object valueData = string.Empty;
        #endregion // private Data

        #region Public Members
        /// <summary>
        /// Name of the registry key
        /// </summary>
        public string KeyName 
        {
            get { return keyName; }
            set {  keyName = value; }
        }

        /// <summary>
        /// Registry key value
        /// </summary>
        public string ValueName 
        {
            get { return valueName; }
            set { valueName = value; }
        }

        /// <summary>
        /// Value data in string form. The string can be qword, binary, ...
        /// </summary>
        public string ValueData
        {
            set 
            { 
                value = value.Trim();
                if(value.StartsWith("dword:",StringComparison.InvariantCulture))
                {
                    valueData = Int32.Parse(value.Substring(6));
                }
                else
                {
                    if(value.Contains("*")) { value = value.Replace("*", DriverState.TestBinRoot); }
                    valueData = System.Environment.ExpandEnvironmentVariables(value);
                }
            }
        }
        #endregion // Public Members

        #region Step Implementation
        /// <summary>
        /// Execute the specified command
        /// </summary>
        /// <returns>true</returns>
        protected override bool BeginStep() 
        {
            Microsoft.Test.Configuration.MachineStateManager.SetRegistryValue(keyName, valueName, valueData);
            return true;
        }
        #endregion

    }
}