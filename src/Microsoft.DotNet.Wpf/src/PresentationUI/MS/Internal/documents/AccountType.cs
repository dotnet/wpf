// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// The EnrollmentAccountType enum for the type of account to enroll.
    /// </summary>
    internal enum EnrollmentAccountType
    {
        /// <summary>
        /// No account type selected
        /// </summary>
        None,

        /// <summary>
        /// Represents a domain account
        /// </summary>
        Network,

        /// <summary>
        /// Represents a one-time use account
        /// </summary>
        Temporary,

        /// <summary>
        /// Represents a .NET Passport account
        /// </summary>
        NET
    }
}
