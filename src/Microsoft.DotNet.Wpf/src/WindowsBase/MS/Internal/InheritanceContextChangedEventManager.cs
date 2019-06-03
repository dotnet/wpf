// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manager for the InheritanceContextChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.Diagnostics;
using System.Windows;       // WeakEventManager
using MS.Internal.WindowsBase;  // [FriendAccessAllowed]

namespace MS.Internal
{
    /// <summary>
    /// Manager for the DependencyObject.InheritanceContextChanged event.
    /// </summary>
    [FriendAccessAllowed]
    internal class InheritanceContextChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private InheritanceContextChangedEventManager()
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
        public static void AddListener(DependencyObject source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            // Freezable.Freeze() relies on the assumption that a frozen Freezable
            // has no listeners.  This is because Freeze() fails if the Freezable
            // has any Expressions on it, and only Expressions ever listen
            // to the InheritanceContextChanged event.
            Debug.Assert(listener is Expression);

            CurrentManager.ProtectedAddListener(source, listener);
        }

        /// <summary>
        /// Remove a listener to the given source's event.
        /// </summary>
        public static void RemoveListener(DependencyObject source, IWeakEventListener listener)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (listener == null)
                throw new ArgumentNullException("listener");

            CurrentManager.ProtectedRemoveListener(source, listener);
        }

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(DependencyObject source, EventHandler<EventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(DependencyObject source, EventHandler<EventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedRemoveHandler(source, handler);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Return a new list to hold listeners to the event.
        /// </summary>
        protected override ListenerList NewListenerList()
        {
            return new ListenerList<EventArgs>();
        }

        //
        //  Protected Methods
        //

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            DependencyObject typedSource = (DependencyObject)source;
            typedSource.InheritanceContextChanged += new EventHandler(OnInheritanceContextChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            DependencyObject typedSource = (DependencyObject)source;
            typedSource.InheritanceContextChanged -= new EventHandler(OnInheritanceContextChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static InheritanceContextChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(InheritanceContextChangedEventManager);
                InheritanceContextChangedEventManager manager = (InheritanceContextChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new InheritanceContextChangedEventManager();
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

        // event handler for InheritanceContextChanged event
        private void OnInheritanceContextChanged(object sender, EventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}

