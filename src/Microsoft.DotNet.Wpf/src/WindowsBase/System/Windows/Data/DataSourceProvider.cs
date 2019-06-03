// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: common base class and contract for data source provider objects
//
// Specs:       http://avalon/connecteddata/Specs/Avalon%20DataProviders.mht
//

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;     // Dispatcher*

using MS.Internal;  // Invariant

namespace System.Windows.Data
{
    /// <summary>
    /// Common base class and contract for data source providers.
    /// A DataProvider in Avalon is the factory that executes some query
    /// to produce a single object or a list of objects that can be used
    /// as sources for Avalon data bindings.
    /// It is a convenience wrapper around existing data model, it does not replace any data model.
    /// A data provider does not attempt to condense the complexity and versatility of a data model
    /// like ADO into one single object with a few properties.
    /// </summary>
    /// <remarks>
    /// DataSourceProvider is an abstract class and cannot directly be used as a data provider.
    /// Use one of the derived concrete provider, e.g. XmlDataProvider, ObjectDataProvider.
    /// The DataProvider aware of Avalon's threading and dispatcher model. The data provider assumes
    /// the thread at creation time to be the UI thread. Events will get marshalled from a worker thread
    /// to the app's UI thread.
    /// </remarks>
    public abstract class DataSourceProvider : INotifyPropertyChanged, ISupportInitialize
    {
        /// <summary>
        /// constructor captures the Dispatcher associated with the current thread
        /// </summary>
        protected DataSourceProvider()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Start the initial query to the underlying data model.
        /// The result will be returned on the Data property.
        /// This method is typically called by the binding engine when
        /// dependent data bindings are activated.
        /// Set IsInitialLoadEnabled = false to prevent or delay the automatic loading of data.
        /// </summary>
        /// <remarks>
        /// The InitialLoad method can be called multiple times.
        /// The provider is expected to ignore subsequent calls once the provider
        /// is busy executing the initial query, i.e. the provider shall not restart
        /// an already running query when InitialLoad is called again.
        /// When the query finishes successfully, any InitialLoad call will still not re-query data.
        /// The InitialLoad operation is typically asynchronous, a DataChanged event will
        /// be raised when the Data property assumed a new value.
        /// The application should call Refresh to cause a refresh of data.
        /// </remarks>
        public void InitialLoad()
        {
            // ignore call if IsInitialLoadEnabled == false or already started initialization
            if (!IsInitialLoadEnabled || _initialLoadCalled)
                return;

            _initialLoadCalled = true;
            BeginQuery();
        }

        /// <summary>
        /// Initiates a Refresh Operation to the underlying data model.
        /// The result will be returned on the Data property.
        /// </summary>
        /// <remarks>
        /// A refresh operation is typically asynchronous, a DataChanged event will
        /// be raised when the Data property assumed a new value.
        /// If the refresh operation fails, the Data property will be set to null;
        /// the Error property will be set with the error exception.
        /// The app can call Refresh while a previous refresh is still underway.
        /// Calling Refresh twice will cause the DataChanged event to raise twice.
        /// </remarks>
        public void Refresh()
        {
            _initialLoadCalled = true;
            BeginQuery();
        }

        /// <summary>
        /// Set IsInitialLoadEnabled = false to prevent or delay the automatic loading of data.
        /// </summary>
        [DefaultValue(true)]
        public bool IsInitialLoadEnabled
        {
            get { return _isInitialLoadEnabled; }
            set
            {
                _isInitialLoadEnabled = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsInitialLoadEnabled"));
            }
        }

        /// <summary>
        /// Get the underlying data object.
        /// This is the resulting data source the data provider
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public object Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Raise this event when a new data object becomes available
        /// on the Data property.
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Return the error of the last query operation.
        /// To indicate there was no error, it will return null
        /// </summary>
        public Exception Error
        {
            get { return _error; }
        }


        /// <summary>
        /// Enter a Defer Cycle.
        /// Defer cycles are used to coalesce property changes, any automatic
        /// Refresh is delayed until the Defer Cycle is exited.
        /// </summary>
        /// <remarks>
        /// most typical usage is with a using block to set multiple proporties
        /// without the automatic Refresh to occur
        /// <code>
        ///     XmlDataProvider xdv = new XmlDataProvider();
        ///     using(xdv.DeferRefresh()) {
        ///         xdv.Source = "http://foo.com/bar.xml";
        ///         xdv.XPath = "/Bla/Baz[@Boo='xyz']";
        ///     }
        /// </code>
        /// </remarks>
        public virtual IDisposable DeferRefresh()
        {
            ++_deferLevel;
            return new DeferHelper(this);
        }


        #region ISupportInitialize
        /// <summary>
        ///     Initialization of this element is about to begin
        /// </summary>
        void ISupportInitialize.BeginInit()
        {
            BeginInit();
        }

        /// <summary>
        ///     Initialization of this element has completed
        /// </summary>
        void ISupportInitialize.EndInit()
        {
            EndInit();
        }
        #endregion


        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }
            remove
            {
                PropertyChanged -= value;
            }
        }


        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// IsRefreshDeferred returns true if there is still an
        /// outstanding DeferRefresh in use.  To get the best use
        /// out of refresh deferral, derived classes should try
        /// not to call Refresh when IsRefreshDeferred is true.
        /// </summary>
        protected bool IsRefreshDeferred
        {
            get
            {
                return (  (_deferLevel > 0)
                        || (!IsInitialLoadEnabled && !_initialLoadCalled));
            }
        }

        /// <summary>
        /// The current Dispatcher to the Avalon UI thread to use.
        /// </summary>
        /// <remarks>
        /// By default, this is the Dispatcher associated with the thread
        /// on which this DataProvider instance was created.
        /// </remarks>
        protected Dispatcher Dispatcher
        {
            get { return _dispatcher; }
            set
            {
                if (_dispatcher != value)
                {
                    _dispatcher = value;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Overridden by concrete data provider class.
        /// the base class will call this method when InitialLoad or Refresh
        /// has been called and will delay this call if refresh is deferred ot
        /// initial load is disabled.
        /// </summary>
        /// <remarks>
        /// The implementor can choose to execute the query on the same thread or
        /// on a background thread or using asynchronous API.
        /// When the query is complete, call OnQueryFinished to have the public properties updated.
        /// </remarks>
        protected virtual void BeginQuery()
        {
        }

        /// <summary>
        /// A concrete data provider will call this method
        /// to indicate that a query has finished.
        /// </summary>
        /// <remarks>
        /// This callback can be called from any thread, this implementation
        /// will marshal back the result to the UI thread
        /// before setting any of the public properties and before raising any events.
        /// <param name="newData">resulting data from query</param>
        /// </remarks>
        protected void OnQueryFinished(object newData)
        {
             OnQueryFinished(newData, null, null, null);
        }

        /// <summary>
        /// A concrete data provider will call this method
        /// to indicate that a query has finished.
        /// </summary>
        /// <remarks>
        /// This callback can be called from any thread, this implementation
        /// will marshal back the result to the UI thread
        /// before setting any of the public properties and before raising any events.
        /// <param name="newData">resulting data from query</param>
        /// <param name="error">error that occured while running query; null signals no error</param>
        /// <param name="completionWork">optional delegate to execute completion work on UI thread, e.g. setting additional properties</param>
        /// <param name="callbackArguments">optional arguments to send as parameter with the completionWork delegate</param>
        /// </remarks>
        protected virtual void OnQueryFinished(object newData, Exception error,
                                                DispatcherOperationCallback completionWork, object callbackArguments)
        {
            Invariant.Assert(Dispatcher != null);
            // check if we're already on the dispatcher thread
            if (Dispatcher.CheckAccess())
            {
                // already on UI thread
                UpdateWithNewResult(error, newData, completionWork, callbackArguments);
            }
            else
            {
                // marshal the result back to the main thread
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal, UpdateWithNewResultCallback,
                    new object[] { this, error, newData, completionWork, callbackArguments });
            }
        }

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises a PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        /// <summary>
        ///     Initialization of this element is about to begin;
        ///     no implicit Refresh occurs until the matched EndInit is called
        /// </summary>
        protected virtual void BeginInit()
        {
            ++_deferLevel;
        }

        /// <summary>
        ///     Initialization of this element has completed;
        ///     this causes a Refresh if no other deferred refresh is outstanding
        /// </summary>
        protected virtual void EndInit()
        {
            EndDefer();
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void EndDefer()
        {
            Debug.Assert(_deferLevel > 0);

            --_deferLevel;

            if (_deferLevel == 0)
            {
                Refresh();
            }
        }

        private static object UpdateWithNewResult(object arg)
        {
            object[] args = (object[]) arg;
            Invariant.Assert(args.Length == 5);
            DataSourceProvider provider = (DataSourceProvider) args[0];
            Exception error = (Exception) args[1];
            object newData = args[2];
            DispatcherOperationCallback completionWork
                = (DispatcherOperationCallback) args[3];
            object callbackArgs = args[4];

            provider.UpdateWithNewResult(error, newData, completionWork, callbackArgs);
            return null;
        }

        private void UpdateWithNewResult(Exception error, object newData, DispatcherOperationCallback completionWork, object callbackArgs)
        {
            bool errorChanged = (_error != error);
            _error = error;
            if (error != null)
            {
                newData = null;
                _initialLoadCalled = false; // allow again InitialLoad after an error
            }

            _data = newData;

            if (completionWork != null)
                completionWork(callbackArgs);

            // notify any listeners
            OnPropertyChanged(new PropertyChangedEventArgs("Data"));
            if (DataChanged != null)
            {
                DataChanged(this, EventArgs.Empty);
            }
            if (errorChanged)
                OnPropertyChanged(new PropertyChangedEventArgs("Error"));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------


        #region Private Types

        private class DeferHelper : IDisposable
        {
            public DeferHelper(DataSourceProvider provider)
            {
                _provider = provider;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                if (_provider != null)
                {
                    _provider.EndDefer();
                    _provider = null;
                }
            }

            private DataSourceProvider _provider;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private bool _isInitialLoadEnabled = true;
        private bool _initialLoadCalled;
        private int _deferLevel;
        private object _data;
        private Exception _error;
        private Dispatcher _dispatcher;

        static readonly DispatcherOperationCallback UpdateWithNewResultCallback = new DispatcherOperationCallback(UpdateWithNewResult);
}
}



