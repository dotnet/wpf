// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Basic Provider-side interface for 1-level UI

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation.Provider
{


    /// <summary>
    /// Indicates the type of provider this is, for example, whether it is a client-side
    /// or server-side provider.
    /// </summary>
    [Flags]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum ProviderOptions
#else
    public enum ProviderOptions
#endif
    {
        /// <summary>Indicates that this is a client-side provider</summary>
        ClientSideProvider      = 0x0001,
        /// <summary>Indicates that this is a server-side provider</summary>
        ServerSideProvider      = 0x0002,
        /// <summary>Indicates that this is a non-client-area provider</summary>
        NonClientAreaProvider   = 0x0004,
        /// <summary>Indicates that this is an override provider</summary>
        OverrideProvider        = 0x0008,

        /// <summary>Indicates that this provider handles its own focus, and does not want
        /// UIA to set focus to the nearest HWND on its behalf when AutomationElement.SetFocus
        /// is used. This option is typically used by providers for HWNDs that appear to take
        /// focus without actually receiving actual Win32 focus, such as menus and dropdowns</summary>
        ProviderOwnsSetFocus    = 0x0010,

        /// <summary>Indicates that this provider expects to be called according to COM threading rules:
        /// if the provider is in a Single-Threaded Apartment, it will be called only on the apartment
        /// thread. Only Server-side providers can use this option.</summary>
        UseComThreading         = 0x0020

    }

    /// <summary>
    /// UIAutomation provider interface, implemented by providers that want to expose
    /// properties for a single element. To expose properties and structure for more than
    /// a single element, see the derived IRawElementProviderFragment interface
    /// </summary>
    [ComVisible(true)]
    [Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderSimple
#else
    public interface IRawElementProviderSimple
#endif
    {
        /// <summary>
        /// Indicates the type of provider this is, for example, whether it is a client-side
        /// or server-side provider.
        /// </summary>
        /// <remarks>
        /// Providers must specify at least either one of ProviderOptions.ClientSideProvider
        /// or ProviderOptions.ServerSideProvider.
        /// 
        /// UIAutomation treats different types of providers
        /// differently - for example, events from server-side provider are broadcast to all listening
        /// clients, whereas events from client-side providers remain in that client.
        /// </remarks>
        ProviderOptions ProviderOptions
        {
            get;
        }

        /// <summary>
        /// Get a pattern interface from this object
        /// </summary>
        /// <param name="patternId">Identifier indicating the interface to return - use
        /// AutomationPattern.LookupById() to convert to an AutomationPattern instance.</param>
        /// <returns>Returns the interface as an object, if supported; otherwise returns null/</returns>
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetPatternProvider(int patternId);

        /// <summary>
        /// Request value of specified property from an element.
        /// </summary>
        /// <param name="propertyId">Identifier indicating the property to return - use
        /// AutomationProperty.LookupById() to convert to an AutomationProperty instance.</param>
        /// <returns>Returns a ValInfo indicating whether the element supports this property, or has no value for it.</returns>
        object GetPropertyValue(int propertyId);

        // Should we try and remove this, or move it to root?
        // Only native impl roots need to return something for this,
        // proxies always return null (cause we already know their HWNDs)
        // If proxies create themselves when handling winvents events, then they
        // also need to implement this so we can determine the HWND. Still only
        // lives on a root, however.
        /// <summary>
        /// Returns a base provider for this element.
        ///
        /// Typically only used by elements that correspond directly to a Win32 Window Handle,
        /// in which case the implementation returns AutomationInteropProvider.BaseElementFromHandle( hwnd ).
        /// </summary>
        IRawElementProviderSimple HostRawElementProvider
        {
            get;
        }
    }
}
