// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements COM DLL installation service
    /// </summary>
    internal class ComStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void ApplyState(StateModule settings)
        {
            ProcessUtilities.Run("regsvr32.exe", "/s " + Path.Combine(settings.TestBinariesDirectory.FullName, settings.Path));
        }

        public void RecordPreviousState(StateModule settings)
        {
          //No suitable query known at this time.
        }

        public void RollbackState(StateModule settings)
        {
            ProcessUtilities.Run("regsvr32.exe", "/s /u " + Path.Combine(settings.TestBinariesDirectory.FullName, settings.Path));
        }

        #endregion
    }
}