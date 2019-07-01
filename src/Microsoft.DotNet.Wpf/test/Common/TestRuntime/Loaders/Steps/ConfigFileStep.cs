// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Xml;
using Microsoft.Test.Loaders.UIHandlers;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Loaders.Steps 
{
    /// <summary>
    /// Loader Step that can be used to recursively run the steps of a different config file.
    /// </summary> 
    public class ConfigFileStep : LoaderStep 
    {
        #region Constructors

        /// <summary>
        /// Creates a new ConfigFileStep
        /// </summary>
        public ConfigFileStep() 
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Name of child ApplicationMonitor Config file to run
        /// </summary>
        public string FileName = "";

        #endregion

        #region Step Implementation

        /// <summary>
        /// Tries to parse the config file and run the steps parsed.  
        /// </summary>
        /// <returns>returns true if the rest of the steps should be executed, otherwise, false</returns>
        public override bool DoStep()
        {
            ApplicationMonitorConfig amc = new ApplicationMonitorConfig(FileName);

            return amc.RunSteps();
        }

        #endregion
    }
}