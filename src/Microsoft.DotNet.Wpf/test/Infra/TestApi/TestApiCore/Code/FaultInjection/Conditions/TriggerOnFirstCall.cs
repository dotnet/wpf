// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Conditions
{
    [Serializable()]
    internal sealed class TriggerOnFirstCall : ICondition
    {
        public bool Trigger(IRuntimeContext context)
        {
            if (triggered == false)
            {
                triggered = true;
                return true;
            }
            return false;
        }
        private bool triggered = false;
    }
}