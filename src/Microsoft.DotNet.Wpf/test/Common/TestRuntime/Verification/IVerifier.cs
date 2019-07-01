// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;

namespace Microsoft.Test.Verification
{
    /// <summary>
    /// Generic verifier.
    /// </summary>
    public interface IVerifier
    {
        /// <summary>
        /// This method is used as a generic verifier. 
        /// </summary>
        /// <param name="ExpectedState">Expected values used in verification.</param>
        /// <returns>The result of the verification.</returns>
        IVerifyResult Verify(params object[] ExpectedState);
    }

}
