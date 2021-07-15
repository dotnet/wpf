// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The Command class is used by the developer to define the intent of the User Action
//              This also serves the purpose of identifying commands or to compare identities of 
//              InputBindings and CommandBindings
// 

using System;
using System.Security;
using System.Windows;
using System.ComponentModel;
using System.Collections;
using System.Windows.Input;
using MS.Internal.PresentationCore;

namespace System.Windows.Input 
{
    /// <summary>
    /// Command
    /// </summary>
    [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
    internal class SecureUICommand : RoutedUICommand, ISecureCommand
    {
        /// <summary>
        /// Creates a new secure command, requiring the specified permissions. Used to delay initialization of Text and InputGestureCollection to time of first use.
        /// </summary>
        /// <param name="name">Name of the Command Property/Field for Serialization</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="commandId">Idenfier assigned by the owning type.</param>
        internal SecureUICommand(string name, Type ownerType, byte commandId)
            : base(name, ownerType, commandId)
        {
        }
    }
 }
