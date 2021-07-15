// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows
{
    /// <summary>
    ///     Routing Strategy can be either of 
    ///     Tunnel or Bubble
    /// </summary>
    /// <ExternalAPI/>
    public enum RoutingStrategy
    {
        /// <summary>
        ///     Tunnel 
        /// </summary>
        /// <remarks>
        ///     Route the event starting at the root of 
        ///     the visual tree and ending with the source
        /// </remarks>
        Tunnel,
        
        /// <summary>
        ///     Bubble 
        /// </summary>
        /// <remarks>
        ///     Route the event starting at the source 
        ///     and ending with the root of the visual tree
        /// </remarks>
        Bubble,

        /// <summary>
        ///     Direct 
        /// </summary>
        /// <remarks>
        ///     Raise the event at the source only.
        /// </remarks>
        Direct
    }
}

