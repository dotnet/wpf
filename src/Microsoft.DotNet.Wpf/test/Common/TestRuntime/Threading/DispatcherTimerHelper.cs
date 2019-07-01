// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Threading;

namespace Microsoft.Test.Threading
{
    /// <summary>
    /// This class contains multiples methods that abstract some functionality around
    /// DispatcherTimer.
    /// </summary>
    public class DispatcherTimerHelper
	{
        /// <summary>
        /// Creates a dispatcher timer with sometimespan, callback.  The last argument is 
        /// stored on the Tag property inside of the timer. The dispatcher is created on the current dispatcher and 
        /// with Normal Priority
        /// </summary>
        static public DispatcherTimer StartTimer(TimeSpan span, EventHandler callback, object o)
        {
            return StartTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher, span, callback, o);
        }

        /// <summary>
        /// Creates a dispatcher timer with some priority, timespan, callback.  The last argument is 
        /// stored on the Tag property inside of the timer. The dispatcher is created on the current dispatcher
        /// </summary>
        static public DispatcherTimer StartTimer(DispatcherPriority priority, TimeSpan span, EventHandler callback, object o)
        {
            return StartTimer(priority, Dispatcher.CurrentDispatcher, span, callback, o);
        }

        /// <summary>
        /// Creates a dispatcher timer with some priority, timespan, callback.  The last argument is 
        /// stored on the Tag property inside of the timer. The dispatcher is created on the current dispatcher
        /// </summary>
        static public DispatcherTimer StartTimer(DispatcherPriority priority, Dispatcher dispatcher, TimeSpan span, EventHandler callback, object o)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            if (callback == null)
                throw new ArgumentNullException("callback");

            DispatcherTimer dTimer = new DispatcherTimer(priority, dispatcher);
            dTimer.Tag = o;
            dTimer.Interval = span;
            dTimer.Tick += new EventHandler(callback);
            dTimer.Start();

            return dTimer;

        }
	}
}
