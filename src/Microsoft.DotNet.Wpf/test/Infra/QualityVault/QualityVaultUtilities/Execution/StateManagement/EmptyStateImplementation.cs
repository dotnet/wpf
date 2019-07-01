// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// Provides a dummy State implementation for inoperational state mgmt types.
    /// </summary>
    internal class EmptyStateImplementation : IStateImplementation
    {
        #region IStateImplementation Members

        public void ApplyState(StateModule settings)
        {            
        }

        public void RecordPreviousState(StateModule settings)
        {            
        }

        public void RollbackState(StateModule settings)
        {            
        }

        #endregion
    }
}