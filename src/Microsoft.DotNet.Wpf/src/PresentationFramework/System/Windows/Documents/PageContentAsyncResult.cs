// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the PageContentAsyncResult
//

namespace System.Windows.Documents
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Threading;
    using System.Threading;
    using MS.Internal;
    using MS.Internal.AppModel;
    using MS.Internal.Utility;
    using MS.Internal.Navigation;
    using MS.Utility;
    using System.Reflection;
    using System.Windows.Controls;
    using System.Windows.Markup;
    using System.Net;
    using System.IO.Packaging;

    /// <summary>
    /// IAsyncResult for GetPageAsync. This item is passed around and queued up during various
    /// phase of async call. 
    /// </summary>
    internal sealed class PageContentAsyncResult : IAsyncResult
    {
        //--------------------------------------------------------------------
        //
        // Internal enum
        //
        //---------------------------------------------------------------------

        internal enum GetPageStatus
        {
            Loading,
            Cancelled,
            Finished
        }

        //--------------------------------------------------------------------
        //
        // Ctor
        //
        //---------------------------------------------------------------------
        #region Ctor
        internal PageContentAsyncResult(AsyncCallback callback, object state, Dispatcher dispatcher, Uri baseUri, Uri source, FixedPage child)
        {
            this._dispatcher = dispatcher;
            this._isCompleted = false;
            this._completedSynchronously = false;
            this._callback = callback;
            this._asyncState = state;
            this._getpageStatus = GetPageStatus.Loading;
            this._child  = child;
            this._baseUri = baseUri;
            Debug.Assert(source == null || source.IsAbsoluteUri);
            this._source = source;
        }
        #endregion Ctor


        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------


        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region IAsyncResult
    
        //---------------------------------------------------------------------
        /// <summary>
        /// Gets a user-defined object that contains information about 
        /// this GetPageAsync call
        /// </summary>
        public object AsyncState
        {
            get { return _asyncState; }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Gets a WaitHandle that is used to wait for the asynchrounus
        /// GetPageAsync to complete. We are not providing WaitHandle 
        /// since this can be called on the main UIThread.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get { Debug.Assert(false);  return null; }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Gets an indication of whether the asynchronous GetPage
        /// completed synchronously.
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return _completedSynchronously; }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Gets an indication whether the asynchronous GetPage has finished
        /// </summary>
        public bool IsCompleted
        {
            get { return _isCompleted; }
        }
        #endregion IAsyncResult

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------
        
        #region DispatcherOperationCallback
        
        //---------------------------------------------------------------------
        internal object Dispatch(object arg)
        {
            if (this._exception != null)
            {
                // force finish if there was any exception
                this._getpageStatus = GetPageStatus.Finished;
            }
            switch (this._getpageStatus)
            {
                case GetPageStatus.Loading:
                    try
                    {
                        if (this._child != null)
                        {
                            this._completedSynchronously = true;
                            this._result = this._child;
                            _getpageStatus = GetPageStatus.Finished;
                            goto case GetPageStatus.Finished;
                        }

                        //
                        // Note if _source == null, exception will 
                        // be thrown.
                        //
                        Stream responseStream;
                        PageContent._LoadPageImpl(this._baseUri, this._source, out _result, out responseStream);
            
                        if (_result == null || _result.IsInitialized)
                        {
                            responseStream.Close();
                        }
                        else
                        {
                            _pendingStream = responseStream; 
                            _result.Initialized += new EventHandler(_OnPaserFinished);
                        }
                        _getpageStatus = GetPageStatus.Finished;
                    }

                    catch (ApplicationException e)
                    {
                        this._exception = e;
                    }
                    goto case GetPageStatus.Finished;                    

                case GetPageStatus.Cancelled:
                    // do nothing
                    goto case GetPageStatus.Finished;

                case GetPageStatus.Finished:
                    _isCompleted = true;
                    if (_callback != null)
                    {
                        _callback(this);
                    }
                    break;
            }
            return null;
        }
        #endregion DispatcherOperationCallback

        //-----------------------------------------------------------------
        internal void Cancel()
        {
            _getpageStatus = GetPageStatus.Cancelled;
            // Need to cancel loader
        }

        internal void Wait()
        {
            _dispatcherOperation.Wait();
        }

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        //---------------------------------------------------------------------
        internal Exception Exception
        {
            get  { return _exception; }
        }

        //-----------------------------------------------------------------
        internal bool IsCancelled
        {
            get { return _getpageStatus == GetPageStatus.Cancelled; }
        }

        internal DispatcherOperation DispatcherOperation
        {
            set { _dispatcherOperation = value; }
        }

        //-----------------------------------------------------------------
        internal FixedPage Result
        {
            get { return _result; }
        }
        #endregion Internal Properties


        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        private void _OnPaserFinished(object sender, EventArgs args)
        {
            if (_pendingStream != null)
            {
                _pendingStream.Close();
                _pendingStream = null;
            }
        }
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private
        private object              _asyncState;
        private bool    _isCompleted;
        private bool    _completedSynchronously;
        private AsyncCallback  _callback;
        private Exception      _exception;
        private GetPageStatus _getpageStatus;
        private Uri _baseUri;
        private Uri _source;
        private FixedPage _child;
        private Dispatcher _dispatcher;
        private FixedPage _result;
        private Stream  _pendingStream;
        private DispatcherOperation _dispatcherOperation;
        #endregion Private
    }
}
