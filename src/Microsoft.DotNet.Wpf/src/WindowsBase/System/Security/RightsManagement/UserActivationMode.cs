// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.RightsManagement
{
    /// <summary>
    /// This enumeration is used to indicate whether we are going to request temporary or permanent User Certificate from RM server
    /// </summary>
    public enum UserActivationMode : int
    {
        /// <summary>
        /// Permanent User Certificate will be requested
        /// </summary>
        Permanent = 0,
        
        /// <summary>
        /// Temporary User Certificate will be requested
        /// </summary>
        Temporary = 1,
    }
}
 
