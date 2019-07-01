// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Test.FaultInjection
{
    /// <summary>
    /// Defines the contract for information provided by the faulted method.
    /// </summary>
    public interface IRuntimeContext
    {
        /// <summary>
        /// The number of times the method is called.
        /// </summary>
        int CalledTimes
        {
            get;
        }

        /// <summary>
        /// The method's stack trace.
        /// </summary>
        StackTrace CallStackTrace
        {
            get;
        }

        /// <summary>
        /// An array of C#-style method signatures for each method on the call stack.
        /// </summary>
        CallStack CallStack
        {
            get;
        }

        /// <summary>
        /// The C#-style method signature of the caller of the faulted method.
        /// </summary>
        String Caller
        {
            get;
        }   
    }
}