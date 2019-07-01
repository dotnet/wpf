// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Constants
{
    internal static class FaultDispatcherMessages
    {
        //info
        public const string FaultRuleFound = "Function Faulted, Condition & Trigger found";
        public const string ConditionTriggered = "Condition Triggered";
        public const string ConditionNotTriggered = "Condition Not Triggered";
        public const string NoCondition = "No Condition found";
        public const string NoFault = "No Fault found";
        public const string FaultType = "Fault Type: {0}";
        public const string ReturnTypeVoid = "Return Type is void, will directly return";

        //error
        public const string LoadFaultRuleError = "Fatal Error when Loading Fault Rules!";
        public const string NoExceptionAllowedInTriggerAndRetrieve = "Unexpected Exception thrown in ICondition.Trigger() & IFault.Retrieve()";
        public const string UnknownExceptionInTrap = "Exception occurred inside Trap(), refer to InnerException for more info";
        public const string ReturnValueTypeNullError = "Return type '{0}' is Value Type, its value cannot be 'null'";
        public const string ReturnTypeMismatchError = "Return type mismatch, Require '{0}', but get '{1}'";

        //accurate info
        public const string ReturnValueSet = "Return value of type '{0}' is successfully set to '{1}'";
        public const string ExceptionValueSet = "Exception value of type '{0}' is successfully created";

    }
}