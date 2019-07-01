// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Test.Theming;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements Theme state management
    /// </summary>
    internal class ThemeStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void RecordPreviousState(StateModule settings)
        {
            previousTheme = Theme.GetCurrent();
        }

        public void ApplyState(StateModule settings)
        {
            Theme.SetCurrent(new FileInfo(System.Environment.ExpandEnvironmentVariables(settings.Path)));
        }

        public void RollbackState(StateModule settings)
        {
            Theme.SetCurrent(previousTheme);
        }

        #endregion

        private Theme previousTheme;
    }
}