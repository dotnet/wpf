// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manager for the CollectionChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Windows;       // WeakEventManager

namespace System.Collections.Specialized
{
    /// <summary>
    /// Manager for the INotifyCollectionChanged.CollectionChanged event.
    /// </summary>
    public class CollectionChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private CollectionChangedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a listener to the given source's event.
        /// </summary>
        public static void AddListener(INotifyCollectionChanged source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedAddListener(source, listener);
        }

        /// <summary>
        /// Remove a listener to the given source's event.
        /// </summary>
        public static void RemoveListener(INotifyCollectionChanged source, IWeakEventListener listener)
        {
            /* for app-compat, allow RemoveListener(null, x) - it's a no-op 
            if (source == null)
                throw new ArgumentNullException("source");
            */
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedRemoveListener(source, listener);
        }

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(INotifyCollectionChanged source, EventHandler<NotifyCollectionChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(INotifyCollectionChanged source, EventHandler<NotifyCollectionChangedEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedRemoveHandler(source, handler);
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
            return new ListenerList<NotifyCollectionChangedEventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            INotifyCollectionChanged typedSource = (INotifyCollectionChanged)source;
            typedSource.CollectionChanged += new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            INotifyCollectionChanged typedSource = (INotifyCollectionChanged)source;
            typedSource.CollectionChanged -= new NotifyCollectionChangedEventHandler(OnCollectionChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static CollectionChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(CollectionChangedEventManager);
                CollectionChangedEventManager manager = (CollectionChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new CollectionChangedEventManager();
                    SetCurrentManager(managerType, manager);
                }

                return manager;
            }
        }

        #endregion Private Properties

        #region Private Methods

        //
        //  Private Methods
        //

        // event handler for CollectionChanged event
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}

