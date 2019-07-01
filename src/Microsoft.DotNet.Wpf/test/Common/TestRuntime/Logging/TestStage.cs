// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Logging
{ 

    /// <summary>
    /// Test Stage - unsupported feature.
    /// </summary>
    public enum TestStage
    {
        /// <summary>
        /// The Stage is unknown or not used by the test
        /// </summary>
        Unknown,
        /// <summary>
        /// The Test is preparing to exercise the targeted functionality.
        /// </summary>
        Initialize,
        /// <summary>
        /// The Test is exercising the targeted functionality
        /// </summary>
        Run,
        /// <summary>
        /// The Test has determined the quality of the targeted functionality and is cleaning up.
        /// </summary>
        Cleanup
    }
}
