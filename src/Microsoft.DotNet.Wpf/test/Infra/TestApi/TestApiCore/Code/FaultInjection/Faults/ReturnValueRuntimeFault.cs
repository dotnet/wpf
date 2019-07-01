// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.FaultInjection.SignatureParsing;

namespace Microsoft.Test.FaultInjection.Faults
{
    [Serializable(), RuntimeFault]
    internal sealed class ReturnValueRuntimeFault : IFault
    {
        public ReturnValueRuntimeFault(string returnValueExpression)
        {
            this.returnValueExpression = returnValueExpression;
        }
        public void Retrieve(IRuntimeContext rtx, out Exception exceptionValue, out object returnValue)
        {
            exceptionValue = null;
            returnValue = Expression.GeneralExpression(returnValueExpression);
        }
        private readonly string returnValueExpression;
    }
}