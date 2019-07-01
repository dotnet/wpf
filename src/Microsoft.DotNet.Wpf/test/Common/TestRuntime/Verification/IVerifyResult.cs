// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using Microsoft.Test.Logging;

namespace Microsoft.Test.Verification
{
    /// <summary>
    /// Result for generic verifier (IVerifier).
    /// </summary>
    public interface IVerifyResult
    {
        /// <summary>
        /// Message (error or not) associated with the result of the verification.
        /// </summary>
        string Message { get; set; }

        /// <summary>
        /// The result (typically pass or fail) of the verification.
        /// </summary>
        TestResult Result { get; set; }
    }
}
    
