// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test
{
    /// <summary>
    /// Result for a test variation. Currently a pass or did not pass semantic.
    /// I.e. Fail simply means 'did not pass' - does not mean that
    /// we validated the type of failure.
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// The test variation passed.
        /// </summary>
        Pass,

        /// <summary>
        /// The test variation did not pass.
        /// </summary>
        Fail,

        /// <summary>
        /// This test variation should be ignored for the purposes of judging product quality.
        /// </summary>
        Ignore
    }
}
