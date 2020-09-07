// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Threading 
{
    /// <summary>
    ///     Base class for all event arguments associated with a <see cref="Dispatcher"/>.
    /// </summary>
    /// <ExternalAPI/> 
    public class DispatcherEventArgs : EventArgs
    {
        /// <summary>
        ///     The <see cref="Dispatcher"/> associated with this event.
        /// </summary>
        /// <ExternalAPI/> 
        public Dispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        internal DispatcherEventArgs(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }
        
        private Dispatcher _dispatcher;
    }
}

