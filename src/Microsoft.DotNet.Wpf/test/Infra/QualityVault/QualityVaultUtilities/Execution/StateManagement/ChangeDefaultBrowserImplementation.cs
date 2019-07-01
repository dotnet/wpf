// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Test.Execution.StateManagement.Browser;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Changes the default browser; only works if FireFox is already installed (otherwise does nothing)
    /// </summary>
    internal class ChangeDefaultBrowserImplementation : IStateImplementation
    {
        private DefaultWebBrowser previousBrowser = DefaultWebBrowser.InternetExplorer;

        #region IStateImplementation Members

        public void RecordPreviousState(StateModule settings)
        {
            previousBrowser = ChangeDefaultBrowserUtilities.CurrentDefaultWebBrowser;
        }

        public void ApplyState(StateModule settings)
        {
            DefaultWebBrowser requested = (DefaultWebBrowser) Enum.Parse(typeof(DefaultWebBrowser), settings.Path);
            ChangeDefaultBrowserUtilities.SetDefaultBrowserInRegistry(requested);
        }

        public void RollbackState(StateModule settings)
        {
            ChangeDefaultBrowserUtilities.SetDefaultBrowserInRegistry(previousBrowser);
        }

        #endregion
    }
}