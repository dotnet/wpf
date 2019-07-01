// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Test.Diagnostics
{

    /// <summary>
    /// User login information
    /// </summary>
    public struct UserInfo
    {
        /// <summary>
        /// Username
        /// </summary>
        public string Username;
        /// <summary>
        /// Domain
        /// </summary>
        public string Domain;
        /// <summary>
        /// Password
        /// </summary>
        public string Password;
        /// <summary>
        /// Wtt LLU
        /// </summary>
        public string LLUName;
    }
}
