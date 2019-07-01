// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Constants
{
    internal static class ApiErrorMessages
    {
        // Error messages for class FaultRule
        public const string ConditionNull = "Condition object of a fault rule can't be null.";
        public const string FaultNull = "Fault object of a fault rule can't be null.";

        // Error messages for class FaultSession
        public const string FaultRulesNullOrEmpty = "Fault rules used to initialize FaultSession instance can't be null or empty.";
        public const string FaultRulesConflict = "Each method can attach only one FaultRule in one FaultSession.";
        public const string LauncherNull = "Launch delegate can't be null";
        public const string UnableToCreateFileReadWriteMutex = "Unable to create Mutex for file I/O with name \"{0}\".";
        public const string RegisterEngineFailed = "Register fault injection engine file \"{1}\" failed. Regsvr32 return error code 0x{0:X} ";
        public const string RegisterEngineAccessDenied = "Register fault injection engine file \"{1}\" failed. You should run fault injection tool as Administrator(e.g. Run cmd or Visual Studio as Administrator), error code 0x{0:X}.";
        public const string RegisterEngineFileNotFound = "Cannot find fault injection engine file \"{1}\". Please check if the file exists, error code 0x{0:X}.";
        
        public const string LogDirectoryNullOrEmpty = "Directory for log files can't be null or empty.";

        // Error messages for FaultRuleLoader
        public const string UnableToFindEnvironmentVariable = "Can't find environment variable \"{0}\"";

        // Error messages for faults
        public const string ExceptionNull = "The exception object used to initialize ThrowExceptionFault can't be null.";
        public const string ExpressionNullOrEmpty = "Expression to specify exception want to be thrown can't be null or empty.";

        // Error messages for Helpers
        public const string MethodSignatureNullOrEmpty = "Method signature can't be null or empty.";
        public const string InvalidMethodSignature = "Invalid method signature: {0}";
        public const string FaultRulesNullInSerialization = "Can't serialize null FaultRule[] instance";
    }
}