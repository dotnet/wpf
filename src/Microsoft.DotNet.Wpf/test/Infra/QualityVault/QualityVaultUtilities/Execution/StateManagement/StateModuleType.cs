// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.StateManagement
{
    /// <summary>
    /// State Module Enum allows user to select the appropriate state management Modules. 
    /// </summary>
    public enum StateModuleType
    {
        /// <summary>
        /// Performs Windows Registry Manipulations - Placeholder.
        /// </summary>
        WindowsRegistry = 0,

        /// <summary>
        /// Registers COM dlls
        /// </summary>
        ComRegistration = 1,

        /// <summary>
        /// Registers Managed Dll's to Global Assembly Cache (GAC)
        /// </summary>
        GlobalAssemblyCache = 2,

        /// <summary>
        /// Registers Windows Color Space Profiles
        /// </summary>
        ColorProfile = 3,

        /// <summary>
        /// Sets IE or FireFox as the default web browser
        /// </summary>
        DefaultWebBrowser = 4,

        /// <summary>
        /// Uninstall Keyboard Layouts
        /// </summary>
        KeyboardLayout = 5,

        /// <summary>
        /// Manage System themes
        /// </summary>
        Theme = 6,

        /// <summary>
        /// Switch away from Modern Shell to Desktop
        /// </summary>
        Mosh = 7,
    }
}      