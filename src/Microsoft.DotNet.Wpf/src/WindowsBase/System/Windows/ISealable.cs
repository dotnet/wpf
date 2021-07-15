// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.WindowsBase;

namespace System.Windows
{
    /// <summary>
    /// This interface is shared across Freezable, Style and Template and 
    /// is a performance optimization to avoid multiple type casts.
    ///
    /// A Sealed ISealable  is free-threaded; this implies the implementation
    /// of Seal() should call DetachFromDispatcher().  
    /// </summary>
    [FriendAccessAllowed] // Built into Base, also used by Framework.
    internal interface ISealable
    {
        /// <summary>
        /// Can the current instance be sealed
        /// </summary>
        bool CanSeal
        {
            get;
        }

        /// <summary>
        /// Seal the current instance by detaching from the dispatcher
        /// </summary>
        void Seal();

        /// <summary>
        /// Is the current instance sealed
        /// </summary>
        bool IsSealed
        {
            get;
        }
    }
}

