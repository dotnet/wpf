// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection
{

    /// <summary>
    /// Defines the contract for specifying when a fault will be triggered on a method.
    /// </summary>
    /// <remarks>
    /// If the fault condition is not triggered, the faulted method will execute its original code.
    /// For more information on how to use a condition, see the <see cref="FaultSession"/> class.
    /// </remarks>
    public interface ICondition
    {
        /// <summary>
        /// Determines whether a fault should be triggered.
        /// </summary>
        /// <param name="context">The runtime context information for this call and the faulted method.</param>
        /// <returns>Returns true if a fault should be triggered, otherwise returns false.</returns>
        bool Trigger(IRuntimeContext context);
    }
}
