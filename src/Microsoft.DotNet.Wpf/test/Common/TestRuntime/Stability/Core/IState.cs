// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System;

namespace Microsoft.Test.Stability.Core
{    
    public interface IStatePayload
    {
        void Initialize(ContentPropertyBag arguments);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Pass in proprietary initialization arguments for known default state.
        /// Use of these arguments are proprietary to State implementations.
        /// </summary>
        void Initialize(IStatePayload arguments);

        /// <summary>
        /// Reset State to known defaults. This operation is called prior to use of State object.
        /// </summary>
        /// <param name="stateIndicator">An integer determinates how to reset.</param>
        void Reset(int stateIndicator);

        /// <summary>
        /// Allows State object to measure itself to indicate if it is exceeding resource constraints. 
        /// Exceeding constraints will cause state to be reset.
        /// </summary>
        /// <returns></returns>
        bool IsBeyondConstraints();
    }
}
