// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.FaultInjection.Constants
{
    internal static class EnvironmentVariable
    {
        // Environment variables we used to pass informations
        public const string EnableProfiling = "COR_ENABLE_PROFILING";
        public const string Proflier = "COR_PROFILER";
        public const string RuleRepository = "FAULT_INJECTION_RULE_REPOSITORY";
        public const string MethodFilter = "FAULT_INJECTION_METHOD_FILTER";
        public const string LogDirectory = "FAULT_INJECTION_LOG_DIR";
        public const string LogVerboseLevel = "FAULT_INJECTION_LOG_LEVEL";

        // This flag is necessary to enable code injection in CLR4 binaries
        public const string CLR4Compatibility = "COMPLUS_ProfAPI_ProfilerCompatibilitySetting";
    }
}