// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Manager for the ErrorsChanged event in the "weak event listener"
//              pattern.  See WeakEventTable.cs for an overview.
//

using System;
using System.ComponentModel;
using System.Windows;       // WeakEventManager

namespace System.ComponentModel
{
    /// <summary>
    /// Manager for the INotifyDataErrorInfo.ErrorsChanged event.
    /// </summary>
    public class ErrorsChangedEventManager : WeakEventManager
    {
        #region Constructors

        //
        //  Constructors
        //

        private ErrorsChangedEventManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        //
        //  Public Methods
        //

        /// <summary>
        /// Add a handler for the given source's event.
        /// </summary>
        public static void AddHandler(INotifyDataErrorInfo source, EventHandler<DataErrorsChangedEventArgs> handler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (handler == null)
                throw new ArgumentNullException("handler");

            CurrentManager.ProtectedAddHandler(source, handler);
        }

        /// <summary>
        /// Remove a handler for the given source's event.
        /// </summary>
        public static void RemoveHandler(INotifyDataErrorInfo source, EventHandler<DataErrorsChangedEventArgs> handler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
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
            return new ListenerList<DataErrorsChangedEventArgs>();
        }

        /// <summary>
        /// Listen to the given source for the event.
        /// </summary>
        protected override void StartListening(object source)
        {
            INotifyDataErrorInfo typedSource = (INotifyDataErrorInfo)source;
            typedSource.ErrorsChanged += new EventHandler<DataErrorsChangedEventArgs>(OnErrorsChanged);
        }

        /// <summary>
        /// Stop listening to the given source for the event.
        /// </summary>
        protected override void StopListening(object source)
        {
            INotifyDataErrorInfo typedSource = (INotifyDataErrorInfo)source;
            typedSource.ErrorsChanged -= new EventHandler<DataErrorsChangedEventArgs>(OnErrorsChanged);
        }

        #endregion Protected Methods

        #region Private Properties

        //
        //  Private Properties
        //

        // get the event manager for the current thread
        private static ErrorsChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(ErrorsChangedEventManager);
                ErrorsChangedEventManager manager = (ErrorsChangedEventManager)GetCurrentManager(managerType);

                // at first use, create and register a new manager
                if (manager == null)
                {
                    manager = new ErrorsChangedEventManager();
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

        // event handler for ErrorsChanged event
        private void OnErrorsChanged(object sender, DataErrorsChangedEventArgs args)
        {
            DeliverEvent(sender, args);
        }

        #endregion Private Methods
    }
}

