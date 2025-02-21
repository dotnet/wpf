// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Printing.Configuration
{
    /// <summary>
    /// From http://msdn.microsoft.com/en-us/library/dd183576(VS.85).aspx
    /// </summary>
    [Flags]
    internal enum DocumentPropertiesFlags : uint
    {
        None = 0,

        /// <summary>
        /// Input value
        /// </summary>
        DM_IN_BUFFER = 8,

        /// <summary>
        /// DM_OUT_BUFFER
        /// </summary>
        DM_OUT_BUFFER = 2
    }
}
