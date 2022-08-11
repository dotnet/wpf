// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Generic weak event manager.
//

using System;
using System.Reflection;
using MS.Internal.WindowsBase;

namespace System.Windows
{
    public class WeakEventManager<TEventSource, TEventArgs> : WeakEventManager
        where TEventArgs : EventArgs
    {
        #region Constructors

        //
        //  Constructors
        //

        private WeakEventManager(string eventName)
        {
            _eventName = eventName;
            _eventInfo = typeof(TEventSource).GetEvent(_eventName);

            if (_eventInfo == null)
                throw new ArgumentException(SR.Format(SR.EventNotFound, typeof(TEventSource).FullName, eventName));

            _handler = Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, DeliverEventMethodInfo);
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(TEventSource source, string eventName, EventHandler<TEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager(eventName).ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(TEventSource source, string eventName, EventHandler<TEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager(eventName).ProtectedRemoveHandler(source, handler);
        }

        #endregion Public Methods

        #region Protected Methods

        //
        //  Protected Methods
        //

        /// <summary>
        /// Return a new list to hold listeners to the event.
        /// </summary>
        protected override ListenerList NewListenerList()
        {
            return new ListenerList<TEventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            _eventInfo.AddEventHandler(source, _handler);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            _eventInfo.RemoveEventHandler(source, _handler);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static WeakEventManager<TEventSource, TEventArgs> CurrentManager(string eventName)
        {
            Type managerType = typeof(WeakEventManager<TEventSource, TEventArgs>);
            WeakEventManager<TEventSource, TEventArgs> manager = (WeakEventManager<TEventSource, TEventArgs>)GetCurrentManager(typeof(TEventSource), eventName);

            // at first use, create and register a new manager
            if (manager == null)
            {
                manager = new WeakEventManager<TEventSource, TEventArgs>(eventName);
                SetCurrentManager(typeof(TEventSource), eventName, manager);
            }

            return manager;
        }

        #endregion Private Properties

        #region Private Data

        Delegate _handler;
        string _eventName;
        EventInfo _eventInfo;

        #endregion Private Data
    }
}
