// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Certificate Status for Certs used to sign document
    /// </summary>
    internal enum CertificatePriorityStatus
    {
        /// <summary>
        /// Certificate Verificate returned with NoErrors or only ignorable errors.
        /// </summary>
        Ok,

        /// <summary>
        /// Certificate Verificate returned with a corrupted type error.
        /// </summary>
        Corrupted,

        /// <summary>
        /// Certificate Verificate returned with a cannot be verified error.
        /// </summary>
        CannotBeVerified,
        
        /// <summary>
        /// Certificate Verificate returned with a Issuer not trusted error.
        /// </summary>
        IssuerNotTrusted,

        /// <summary>
        /// Certificate Verificate returned with a Revoked error.
        /// </summary>
        Revoked,
        
        /// <summary>
        /// Certificate Verificate returned with an expired error.
        /// </summary>
        Expired,

        /// <summary>
        /// Case where no certificate exists.
        /// </summary>
        NoCertificate,

        /// <summary>
        /// Certificate is being verified.
        /// </summary>
        Verifying,

    }
}
