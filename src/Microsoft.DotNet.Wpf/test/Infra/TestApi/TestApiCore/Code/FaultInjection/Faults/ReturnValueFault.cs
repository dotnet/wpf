// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Faults
{
    [Serializable()]
    internal sealed class ReturnValueFault : IFault
    {
        public ReturnValueFault(object returnValue)
        {
            this.returnValue = returnValue;
        }
        public void Retrieve(IRuntimeContext rtx, out Exception exceptionValue, out object returnValue)
        {
            exceptionValue = null;
            returnValue = this.returnValue;
        }
        private readonly object returnValue;
    }
}