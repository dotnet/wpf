// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// Constants shared across multiple deployments.
    /// </summary>
    public static class TestRuntimeConstants
    {
        /// <summary>
        /// Full assembly name of TestRuntime which is used to load the dll from the GAC 
        /// </summary>
        public static string FullAssemblyName 
        { 
            get
            {
                return ("TestRuntime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=e1e6160fdd198bb1");
            }
        }
    }

    /// <summary>
    /// Methods marked with this attribute will be ignored when walking the stack
    /// to determine the calling method
    /// </summary>
    /// <remarks>
    /// If you have helper logging functions you can mark them with this attribute
    /// so that the trace is indicated as occuring from the caller.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
    public sealed class LoggingSupportFunctionAttribute : Attribute
    {
    }
}
