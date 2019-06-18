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
using System.Security.Permissions;
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
        /// <SecurityNote>
        /// Critical - should only be write-once in the constructor
        /// </SecurityNote>
        readonly PermissionSet _userInitiated;

        /// <summary>
        /// Creates a new secure command, requiring the specified permissions. Used to delay initialization of Text and InputGestureCollection to time of first use.
        /// </summary>
        /// <param name="userInitiated">PermissionSet to associate with this command</param>
        /// <param name="name">Name of the Command Property/Field for Serialization</param>
        /// <param name="ownerType">Type that is registering the property</param>
        /// <param name="commandId">Idenfier assigned by the owning type.</param>
        /// <SecurityNote>
        ///     Critical -    assigns to the permission set, a protected resource
        ///     TreatAsSafe - KeyBinding (through InputBinding) will demand this permission before 
        ///                   binding this command to any key.
        /// </SecurityNote>
        internal SecureUICommand(PermissionSet userInitiated, string name, Type ownerType, byte commandId)
            : base(name, ownerType, commandId)
        {
            _userInitiated = userInitiated;
        }


        /// <summary>
        /// Permission required to modify bindings for this
        /// command, and the permission to assert when
        /// the command is invoked in a user interactive
        /// (trusted) fashion.
        /// </summary>
        /// <SecurityNote>
        /// Critical - access the permission set, a protected resource
        /// TreatAsSafe - get only access is safe
        /// </SecurityNote>
        public PermissionSet UserInitiatedPermission
        {
            get
            {
                return _userInitiated;
            }
        }
    }
 }
