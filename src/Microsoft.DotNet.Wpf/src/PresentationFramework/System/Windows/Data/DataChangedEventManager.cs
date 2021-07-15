// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Manager for the DataChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Windows;       // WeakEventManager

namespace System.Windows.Data
{
    /// <summary>
    /// Manager for the DataSourceProvider.DataChanged event.
    /// </summary>
    public class DataChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private DataChangedEventManager()
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
        public static void AddListener(DataSourceProvider source, IWeakEventListener listener)
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
        public static void RemoveListener(DataSourceProvider source, IWeakEventListener listener)
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
        public static void AddHandler(DataSourceProvider source, EventHandler<EventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(DataSourceProvider source, EventHandler<EventArgs> handler)
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
            return new ListenerList<EventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            DataSourceProvider typedSource = (DataSourceProvider)source;
            typedSource.DataChanged += new EventHandler(OnDataChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            DataSourceProvider typedSource = (DataSourceProvider)source;
            typedSource.DataChanged -= new EventHandler(OnDataChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static DataChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(DataChangedEventManager);
                DataChangedEventManager manager = (DataChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new DataChangedEventManager();
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

        // event handler for DataChanged event
        private void OnDataChanged(object sender, EventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}

