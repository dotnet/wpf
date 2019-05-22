// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Async content loaded state flags

using System;
using System.Runtime.InteropServices;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Used to represent state when content is asynchronously loading into an element.
    /// </summary>
    [ComVisible(true)]
    [Guid("d8e55844-7043-4edc-979d-593cc6b4775e")]
#if (INTERNAL_COMPILE)
    internal enum AsyncContentLoadedState
#else
    public enum AsyncContentLoadedState
#endif
    {
        /// <summary>Content is beginning to load asynchronously.</summary>
        Beginning,
        /// <summary>Content is continuing to load asynchronously.</summary>
        Progress,
        /// <summary>Content has finished loading or has been stopped.</summary>
        Completed,
    }
}
