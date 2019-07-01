// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test
{
    /// <summary>
    /// Specifies the possible UAC elevation settings
    /// </summary>
    public enum TestUacElevation
    {
        /// <summary>
        /// Run as a default restricted user
        /// </summary>
        Restricted = 0,
        /// <summary>
        /// Run using elevated administrator privileges
        /// </summary>
        Elevated = 1
    }
}