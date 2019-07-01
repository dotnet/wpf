// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Test.Execution.StateManagement.ModernShell;
using Microsoft.Test.Input;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements Modern shell state management to switch away from Modern Shell UI to Desktop
    /// </summary>
    internal class ModernShellStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void RecordPreviousState(StateModule settings)
        {
        }

        public void ApplyState(StateModule settings)
        {
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
            {
                if (ModernShellUtilities.IsImmersiveWindowOpen()) ModernShellUtilities.EnsureDesktop();
            }
        }

        public void RollbackState(StateModule settings)
        {
        }

        #endregion
    }
}