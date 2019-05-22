// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provider interface that allows for overriding HWND-based UI

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;


namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Implemented by providers which want to provide information about or want to
    /// reposition contained HWND-based elements.
    /// </summary>
    [ComVisible(true)]
    [Guid("1d5df27c-8947-4425-b8d9-79787bb460b8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface IRawElementProviderHwndOverride : IRawElementProviderSimple
#else
    public interface IRawElementProviderHwndOverride : IRawElementProviderSimple
#endif
    {
        /// <summary>
        /// Request a provider for the specified component. The returned provider can supply additional
        /// properties or override properties of the specified component.
        /// </summary>
        /// <param name="hwnd">The window handle of the component</param>
        /// <returns>Return the provider for the specified component, or null if the component is not being overridden</returns>
        IRawElementProviderSimple GetOverrideProviderForHwnd( IntPtr hwnd );
    }
}
