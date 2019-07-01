// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// Base class for all Snapshot verifier types. 
    /// This establishes a single method contract: Verify(Snapshot).
    /// </summary>
    public abstract class SnapshotVerifier
    {
        /// <summary>
        /// Verifies the specified Snapshot instance against the current settings of the SnapshotVerifier instance.
        /// </summary>
        /// <param name="image">The image to be verified.</param>
        /// <returns>The verification result based on the supplied image and the current settings of the SnapshotVerifier instance.</returns>
        public abstract VerificationResult Verify(Snapshot image);
    }
}
