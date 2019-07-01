// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Test.Execution.StateManagement.Color;

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Implements Color Profile installation service
    /// </summary>
    internal class ColorProfileStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void RecordPreviousState(StateModule settings)
        {
            previousProfile = ColorProfileUtilities.GetActiveName();
        }

        public void ApplyState(StateModule settings)
        {
            ColorProfileUtilities.SetActiveName(settings.Path);
        }

        public void RollbackState(StateModule settings)
        {
            ColorProfileUtilities.SetActiveName(previousProfile);
        }

        #endregion

        private string previousProfile;
    }
}