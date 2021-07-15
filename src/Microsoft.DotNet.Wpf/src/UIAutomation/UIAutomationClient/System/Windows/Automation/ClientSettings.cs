// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Facade class that contains client configutation options (eg. proxies)
//

using System.Windows.Automation;
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using MS.Internal.Automation;
using MS.Win32;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class containing methods for configuring UIAutomation.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class ClientSettings
#else
    public static class ClientSettings
#endif
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #region Proxies / Client-side providers

        /// <summary>
        /// Load client-side providers from specified assembly
        /// </summary>
        /// <param name="assemblyName">
        /// Specifies the assembly to load client-side providers from.
        /// </param>
        public static void RegisterClientSideProviderAssembly(AssemblyName assemblyName)
        {
            Misc.ValidateArgumentNonNull( assemblyName, "assemblyName" );

            ProxyManager.RegisterProxyAssembly( assemblyName );
        } 
        
        /// <summary>
        /// Register client-side providers to use on HWND-based controls.
        /// </summary>
        /// <param name="clientSideProviderDescription">Array of ClientSideProviderDescription structs that specify window class names and factory delegate</param>
        public static void RegisterClientSideProviders(ClientSideProviderDescription[] clientSideProviderDescription)
        {
            Misc.ValidateArgumentNonNull(clientSideProviderDescription, "clientSideProviderDescription ");

            ProxyManager.RegisterWindowHandlers(clientSideProviderDescription);
        } 

        #endregion Proxies / Client-side providers

        #endregion Public Methods
    }
}
