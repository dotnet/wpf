// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Test.Execution.StateManagement.GacUtilities;
using Microsoft.Test.Execution.Logging;

// This class should be removed, however since deployments in general are being used by QV to group tests,
// and removing the mechanism causes issues, we should analyze more in depth the consequences of removing all of this, but for now commenting this out 
// enables grouping to work correctly.

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements GAC DLL registration service
    /// Requires StateModule.Path to be set to the location of dll within the TestBinariesDirectory
    /// </summary>
    internal class GacStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        /// <summary>
        /// No-op - we simply remove the assembly afterwards
        /// </summary>
        /// <param name="settings"></param>
        public void RecordPreviousState(StateModule settings)
        {
        }

        /// <summary>
        /// Installs specified dll to the GAC. Nothing more is promised.
        /// </summary>        
        public void ApplyState(StateModule settings)
        {
            LoggingMediator.LogEvent(string.Format("QualityVault: GAC: {0}", settings.Path));
        }

        /// <summary>
        /// Removes specified dll from the GAC.
        /// </summary>
        /// <param name="settings"></param>
        public void RollbackState(StateModule settings)
        {
            LoggingMediator.LogEvent(string.Format("QualityVault: UN-GAC: {0}", settings.Path)); 
        }

        #endregion

        private string filePath;
        private string previousPath;
    }
}


