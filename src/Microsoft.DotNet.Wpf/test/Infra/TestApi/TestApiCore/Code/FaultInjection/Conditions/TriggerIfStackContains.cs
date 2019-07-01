// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection.Conditions
{
    [Serializable()] 
    internal sealed class TriggerIfStackContains : ICondition
    {
        public TriggerIfStackContains(String aTargetFunction)
        {
            targetFunction = Signature.ConvertSignature(aTargetFunction);
        }

        public bool Trigger(IRuntimeContext context)
        {
            for (int i = 0; i < context.CallStack.FrameCount; ++i)
            {
                if (context.CallStack[i] == null)
                {
                    return false;
                }
                else if (context.CallStack[i] == targetFunction)
                {
                    return true;
                }
            }
            return false;
        }
        private String targetFunction;
    }  
}