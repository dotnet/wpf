// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Conditions
{
    [Serializable()]
    internal sealed class TriggerOnNthCall : ICondition
    {
        public TriggerOnNthCall(int nth)
        {
            if (nth <= 0)
            {
                throw new ArgumentException("The parameter of TriggerOnNthCall(int) should be a postive number");
            }
            this.n = nth;
        }

        public bool Trigger(IRuntimeContext context)
        {
            if (context.CalledTimes == n)
            {
                return true;
            }
            return false;
        }
        private int n;
    } 
}