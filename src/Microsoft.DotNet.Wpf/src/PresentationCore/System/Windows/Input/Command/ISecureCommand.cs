// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: ISecureCommand enables a command to specify that calls
//              must have a specific permission to modify the bindings
//              associated with that command. That permission will
//              then be asserted when the command is invoked in a user
//              interactive (trusted) way.
//  
//

using System;
using System.ComponentModel;
using System.Security;
using MS.Internal.PresentationCore;

namespace System.Windows.Input
{
    ///<summary>
    /// ISecureCommand enables a command to specify that calls
    /// must have a specific permission to modify the bindings
    /// associated with that command. That permission will
    /// then be asserted when the command is invoked in a user
    /// interactive (trusted) way.
    ///</summary>
    [FriendAccessAllowed]
    [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    internal interface ISecureCommand : ICommand
    {
    }
}
