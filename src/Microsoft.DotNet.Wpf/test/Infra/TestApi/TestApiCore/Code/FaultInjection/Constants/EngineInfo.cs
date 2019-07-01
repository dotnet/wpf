// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.FaultInjection.Constants
{
    internal static class EngineInfo
    {
        // FaultSession namespace
        public const string NameSpace = "Microsoft.Test.FaultInjection";

        // CLSID and registry key for profiling callback COM component
        public const string Engine_CLSID = "{2EB6DCDB-3250-4D7F-AA42-41B1B84113ED}";
        public const string Engine_RegistryKey = @"CLSID\" + Engine_CLSID;

        // File name for profiling callback COM dll
        public const string FaultEngineFileName = "FaultInjectionEngine.dll";
    } 
}