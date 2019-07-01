// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// IAction defines the calling interface for Actions.
    /// Producers of actions are free to operate on proprietary interfaces for populating actions.
    /// </summary>
    public interface IAction
    {
        /// <summary>
        /// Verifies that the Action can be performed
        /// </summary>
        /// <returns>Returns true if you can call perform</returns> 
        bool CanPerform();

        /// <summary>
        /// Performs an action in the current state
        /// </summary>
        void Perform(DeterministicRandom random);
    }
}
