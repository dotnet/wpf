// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:  Authentication Type enumeration is used to differentiate users.    
//
//
//
//

using System;

namespace System.Security.RightsManagement 
{
    /// <summary>
    /// Enumeration that describes various Authentication types, currently only Passport and Windows are supported.
    /// </summary>
    public enum AuthenticationType : int
    {
            /// <summary>
            /// Windows authentication used in corporate Domain environments. 
            /// </summary>
            Windows,

            /// <summary>
            /// Passport authentication, can be used outside of the Windows Domain environments. 
            /// </summary>
            Passport,

            /// <summary>
            /// WindowsPassport authentication, can be used in scenarios when the authentication type of the consumer 
            /// isn't known or important . Regardless of whether it is Passport or Windows, author wants to enable consumer to 
            /// decrypt the document.       
            /// </summary>
            WindowsPassport, 

            /// <summary>
            /// Internal authentication type can be used to identify users implicitly without using their IDs.
            /// Currently this option only supports "Anyone" persona. So that End Use License will be granted 
            /// to anyone who requests one, but it will be attached to the requesting user.  
            /// </summary>
            Internal
    }
}
 
