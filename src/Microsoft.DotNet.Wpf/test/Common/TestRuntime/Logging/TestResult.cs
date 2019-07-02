// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// Test Result
    /// </summary>
    public enum TestResult
    {
        /// <summary>
        /// The result cannot be determined
        /// </summary>
        /// <remarks>
        /// You should use this to indicate that an unexpected failure has occured
        /// that prevents your test from determining the quality of the targeted functionality.
        /// </remarks>
        Unknown,

        /// <summary>
        /// The targeted functionality was verified successfully.
        /// </summary>
        Pass,

        /// <summary>
        /// The targeted functionality was verified as broken.
        /// </summary>
        Fail,

        /// <summary>
        /// The targeted functionality could not be tested and the result should be ignored
        /// </summary>
        Ignore,

        /// <summary>
        /// The test was distributed to a lab machine, but not run.
        /// </summary>
        NotRun
    }
}
