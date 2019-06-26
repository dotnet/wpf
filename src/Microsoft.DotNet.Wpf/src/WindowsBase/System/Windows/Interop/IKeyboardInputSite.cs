// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Windows.Input;

namespace System.Windows.Interop
{
    /// <summary>
    ///     Containers provide a unique IKeyboardInputSite instance for each
    ///     component they contain.
    /// </summary> 
    public interface IKeyboardInputSite
    {
        /// <summary>
        ///     Unregisters a child KeyboardInputSink from this sink.
        /// </summary>
        /// <remarks>
        ///     This API requires unrestricted UI Window permission.
        /// </remarks>
        void Unregister();

        /// <summary>
        ///     Returns the sink associated with this site (the "child", not
        ///     the "parent" sink that owns the site).  There's no way of
        ///     getting from the site to the parent sink.
        /// </summary> 
        IKeyboardInputSink Sink {get;}

        /// <summary>
        ///     Components call this when they want to move focus ("tab") but
        ///     have nowhere further to tab within their own component.  Return
        ///     value is true if the site moved focus, false if the calling
        ///     component still has focus and should wrap around.
        /// </summary> 
        bool OnNoMoreTabStops(TraversalRequest request);
}
}

