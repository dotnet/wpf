// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manager for the CurrentChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Windows;       // WeakEventManager

namespace System.ComponentModel
{
    /// <summary>
    /// Manager for the ICollectionView.CurrentChanged event.
    /// </summary>
    public class CurrentChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private CurrentChangedEventManager()
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
        public static void AddListener(ICollectionView source, IWeakEventListener listener)
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
        public static void RemoveListener(ICollectionView source, IWeakEventListener listener)
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
        public static void AddHandler(ICollectionView source, EventHandler<EventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(ICollectionView source, EventHandler<EventArgs> handler)
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
            ICollectionView typedSource = (ICollectionView)source;
            typedSource.CurrentChanged += new EventHandler(OnCurrentChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            ICollectionView typedSource = (ICollectionView)source;
            typedSource.CurrentChanged -= new EventHandler(OnCurrentChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static CurrentChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(CurrentChangedEventManager);
                CurrentChangedEventManager manager = (CurrentChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new CurrentChangedEventManager();
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

        // event handler for CurrentChanged event
        private void OnCurrentChanged(object sender, EventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}

