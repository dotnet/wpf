// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// API Contract for all ActionSequencers
    /// </summary>
    public interface IActionSequencer
    {
        /// <summary>
        /// Assembles an Action Sequence. Returns null in event of unsatisfied pre-requisites.
        /// </summary>
        /// <param name="state">State data to populate action sequence from</param>
        /// <param name="random">Random data feed</param>
        /// <returns></returns>
        Sequence GetNext(IState state, DeterministicRandom random);
    }
}
