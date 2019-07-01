// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection.Conditions
{
    [Serializable()]
    internal sealed class TriggerOnNthCallBy : ICondition
    {
        public TriggerOnNthCallBy(int nth, String aTargetCaller)
        {
            calledTimes = 0;
            n = nth;
            if (nth <= 0)
            {
                throw new ArgumentException("The first parameter of TriggerOnNthCallBy(int, string) should be a postive number");
            }
            targetCaller = Signature.ConvertSignature(aTargetCaller);
        }

        public bool Trigger(IRuntimeContext context)
        {
            if (context.Caller == targetCaller)
            {
                if ((++calledTimes) == n)
                {
                    return true;
                }
            }
            return false;
        }
        private int calledTimes;
        private int n;
        private String targetCaller;
    }
}