// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.StateManagement
{
    internal interface IStateImplementation
    {
        void RollbackState(StateModule settings);

        void ApplyState(StateModule settings);

        void RecordPreviousState(StateModule settings);
    }
}