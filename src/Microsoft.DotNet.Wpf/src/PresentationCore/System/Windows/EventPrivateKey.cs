// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows
{
    /// <summary>
    ///     This class is meant to provide identification 
    ///     for Clr events whose handlers are stored 
    ///     into EventHandlersStore
    /// </summary>
    /// <remarks>
    ///     This type has been specifically added so that it 
    ///     is easy to enforce via fxcop rules or such that 
    ///     event keys of this type must be private static 
    ///     fields on the declaring class.
    /// </remarks>
    public class EventPrivateKey
    {
        /// <summary>
        ///     Constructor for EventPrivateKey
        /// </summary>
        public EventPrivateKey()
        {
            _globalIndex = GlobalEventManager.GetNextAvailableGlobalIndex(this);
        }

        internal int GlobalIndex
        {
            get { return _globalIndex; }
        }
        
        private int _globalIndex;
    }
}

