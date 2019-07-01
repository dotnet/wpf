// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using TrustedEnvironment = System.Environment;
using System.Security.Permissions;


namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Environment Wrapper for neccessary environment interfaces
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public static class EnvironmentWrapper
    {
        /// <summary/>
        public static string CurrentDirectory
        {
            get { return TrustedEnvironment.CurrentDirectory; }
        }

        /// <summary/>
        public static int TickCount
        {
            get { return TrustedEnvironment.TickCount; }
        }

        /// <summary/>
        public static OperatingSystem OSVersion
        {
            get { return TrustedEnvironment.OSVersion; }
        }

        /// <summary/>
        public static string ExpandEnvironmentVariables(String name)
        {
            return TrustedEnvironment.ExpandEnvironmentVariables(name);
        }
        /// <summary/>
        public static string GetEnvironmentVariable(string variable)
        {
            return TrustedEnvironment.GetEnvironmentVariable(variable);
        }
    }
}