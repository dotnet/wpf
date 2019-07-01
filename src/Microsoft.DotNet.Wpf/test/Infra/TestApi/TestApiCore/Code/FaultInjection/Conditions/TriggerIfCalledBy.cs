// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection.Conditions
{
    [Serializable()] 
    internal sealed class TriggerIfCalledBy : ICondition
    {
        public TriggerIfCalledBy(String aTargetCaller)
        {
            targetCaller = Signature.ConvertSignature(aTargetCaller);
        }

        public bool Trigger(IRuntimeContext context)
        {
            if (context.Caller == targetCaller)
            {
                return true;
            }
            return false;
        }
        private String targetCaller;
    }   
}