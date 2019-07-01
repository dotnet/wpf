// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Input
{
    /// <summary>
    /// Wrapper for InputReport class.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public class InputManagerWrapper
    {

        /// <summary>
        /// </summary>
        public static RoutedEvent GetInputReportEvent()
        {
            return (RoutedEvent)typeof(InputManager).InvokeMember("InputReportEvent", 
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Static,
                null,
                null, 
                null);
        }

    }
}
