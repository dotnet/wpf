// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections;
using Microsoft.Test.Execution.StateManagement.KeyboardLayout;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements Keyboard Layout UnInstallation service
    /// </summary>
    internal class KeyboardLayoutStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void RecordPreviousState(StateModule settings)
        {
            previousEnabledKeyboardLayouts = KeyboardLayoutUtilities.QueryEnabledKeyboardLayouts();
        }

        public void ApplyState(StateModule settings)
        {
            // Keyboard layout is installed as part of the test, hence skipping this step.
        }

        public void RollbackState(StateModule settings)
        {
            KeyboardLayoutUtilities.UninstallKeyboardLayouts(previousEnabledKeyboardLayouts);
        }

        #endregion IStateImplementation Members

        private ArrayList previousEnabledKeyboardLayouts = new ArrayList();
    }
}