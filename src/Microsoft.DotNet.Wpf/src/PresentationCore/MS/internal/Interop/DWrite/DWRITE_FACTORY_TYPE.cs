// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MS.Internal.Interop.DWrite
{
    /// <summary>
    /// Specifies the type of DirectWrite factory object.
    /// DirectWrite factory contains internal state such as font loader registration and cached font data.
    /// In most cases it is recommended to use the shared factory object, because it allows multiple components
    /// that use DirectWrite to share internal DirectWrite state and reduce memory usage.
    /// However, there are cases when it is desirable to reduce the impact of a component,
    /// such as a plug-in from an untrusted source, on the rest of the process by sandboxing and isolating it
    /// from the rest of the process components. In such cases, it is recommended to use an isolated factory for the sandboxed
    /// component.
    /// </summary>
    internal enum DWRITE_FACTORY_TYPE
    {
        /// <summary>
        /// Shared factory allow for re-use of cached font data across multiple in process components.
        /// Such factories also take advantage of cross process font caching components for better performance.
        /// </summary>
        DWRITE_FACTORY_TYPE_SHARED,

        /// <summary>
        /// Objects created from the isolated factory do not interact with internal DirectWrite state from other components.
        /// </summary>
        DWRITE_FACTORY_TYPE_ISOLATED
    }
}
