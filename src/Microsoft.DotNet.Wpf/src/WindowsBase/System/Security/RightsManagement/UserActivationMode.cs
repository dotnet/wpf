// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  This enumeration is used as a parameter in SecureEnvironment.Create to  
//   indicate whether permanent or temporary certificate request needs to be issued.
//
//
//
//

using System;

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
 
