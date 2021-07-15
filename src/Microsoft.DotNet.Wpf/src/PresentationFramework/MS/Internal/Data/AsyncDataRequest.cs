// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Defines a request to the async data system.
//
// Specs:       Asynchronous Data Model.mht
//

using System;

namespace MS.Internal.Data
{
    /// <summary> Type for the work and completion delegates of an AsyncDataRequest </summary>
    internal delegate object AsyncRequestCallback(AsyncDataRequest request);

    /// <summary> Status of an async data request. </summary>
    internal enum AsyncRequestStatus
    {
        /// <summary> Request has not been started </summary>
        Waiting,
        /// <summary> Request is in progress </summary>
        Working,
        /// <summary> Request has been completed </summary>
        Completed,
        /// <summary> Request was cancelled </summary>
        Cancelled,
        /// <summary> Request failed </summary>
        Failed
    }

    /// <summary> A request to the async data system. </summary>
    internal class AsyncDataRequest
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor </summary>
        internal AsyncDataRequest(object bindingState,
                                    AsyncRequestCallback workCallback,
                                    AsyncRequestCallback completedCallback,
                                    params object[] args
                                    )
        {
            _bindingState = bindingState;
            _workCallback = workCallback;
            _completedCallback = completedCallback;
            _args = args;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /* unused by default scheduler.  Restore for custom schedulers.
        /// <summary> The "user data" from the binding that issued the request. </summary>
        public object BindingState { get { return _bindingState; } }
        */

        /// <summary> The result of the request (valid when request is completed). </summary>
        public object Result { get { return _result; } }

        /// <summary> The status of the request. </summary>
        public AsyncRequestStatus Status { get { return _status; } }

        /// <summary> The exception (for a failed request). </summary>
        public Exception Exception { get { return _exception; } }


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary> Run the request's work delegate and return the result. </summary>
        /// <remarks>
        /// This method should be called synchronously on a worker thread, as it
        /// calls the work delegate, which potentially takes a long time.  The
        /// method sets the status to "Working".  It is normally followed by a
        /// call to Complete.
        ///
        /// If the request has already been run or has been abandoned, this method
        /// returns null.
        /// </remarks>
        public object DoWork()
        {
            if (DoBeginWork() && _workCallback != null)
                return _workCallback(this);
            else
                return null;
        }


        /// <summary>If the request is in the "Waiting" state, return true and
        /// set its status to "Working".  Otherwise return false.
        /// </summary>
        /// <remarks>
        /// This method is thread-safe and works atomically.  Therefore only
        /// one thread will be permitted to run the request.
        /// </remarks>
        public bool DoBeginWork()
        {
            return ChangeStatus(AsyncRequestStatus.Working);
        }


        /// <summary> Set the request's status to "Completed", save the result,
        /// and call the completed delegate. </summary>
        /// <remarks>
        /// This method should be called on any thread, after
        /// either calling DoWork or performing the work for a request in some
        /// other way.
        ///
        /// If the request has already been run or has been abandoned, this method
        /// does nothing.
        /// </remarks>
        public void Complete(object result)
        {
            if (ChangeStatus(AsyncRequestStatus.Completed))
            {
                _result = result;
                if (_completedCallback != null)
                    _completedCallback(this);
            }
        }


        /// <summary> Cancel the request.</summary>
        /// <remarks> This method can be called from any thread.
        /// <p>Calling Cancel does not actually terminate the work being
        /// done on behalf of the request, but merely causes the result
        /// of that work to be ignored.</p>
        /// </remarks>
        public void Cancel()
        {
            ChangeStatus(AsyncRequestStatus.Cancelled);
        }


        /// <summary> Fail the request because of an exception.</summary>
        /// <remarks> This method can be called from any thread. </remarks>
        public void Fail(Exception exception)
        {
            if (ChangeStatus(AsyncRequestStatus.Failed))
            {
                _exception = exception;
                if (_completedCallback != null)
                    _completedCallback(this);
            }
        }


        //------------------------------------------------------
        //
        //  Internal properties
        //
        //------------------------------------------------------

        /// <summary> The caller-defined arguments. </summary>
        internal object[] Args { get { return _args; } }

        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------

        // Change the status to the new status.  Return true if this is allowed.
        // Do it all atomically.
        bool ChangeStatus(AsyncRequestStatus newStatus)
        {
            bool allowChange = false;

            lock (SyncRoot)
            {
                switch (newStatus)
                {
                    case AsyncRequestStatus.Working:
                        allowChange = (_status == AsyncRequestStatus.Waiting);
                        break;
                    case AsyncRequestStatus.Completed:
                        allowChange = (_status == AsyncRequestStatus.Working);
                        break;
                    case AsyncRequestStatus.Cancelled:
                        allowChange = (_status == AsyncRequestStatus.Waiting) ||
                                        (_status == AsyncRequestStatus.Working);
                        break;
                    case AsyncRequestStatus.Failed:
                        allowChange = (_status == AsyncRequestStatus.Working);
                        break;
                }

                if (allowChange)
                    _status = newStatus;
            }

            return allowChange;
        }

        //------------------------------------------------------
        //
        //  Private data
        //
        //------------------------------------------------------

        AsyncRequestStatus _status;
        object _result;
        object _bindingState;
        object[] _args;
        Exception _exception;

        AsyncRequestCallback _workCallback;
        AsyncRequestCallback _completedCallback;

        object SyncRoot = new object();     // for synchronization
    }


    /// <summary> Async request to get the value of a property on an item. </summary>
    internal class AsyncGetValueRequest : AsyncDataRequest
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor. </summary>
        internal AsyncGetValueRequest(object item,
                            string propertyName,
                            object bindingState,
                            AsyncRequestCallback workCallback,
                            AsyncRequestCallback completedCallback,
                            params object[] args
                            )
            : base(bindingState, workCallback, completedCallback, args)
        {
            _item = item;
            _propertyName = propertyName;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> The item whose property is being requested </summary>
        public object SourceItem { get { return _item; } }

        /* unused by default scheduler.  Restore for custom schedulers.
        /// <summary> The name of the property being requested </summary>
        public string PropertyName { get { return _propertyName; } }
        */

        //------------------------------------------------------
        //
        //  Private data
        //
        //------------------------------------------------------

        object _item;
        string _propertyName;
    }


    /// <summary> Async request to set the value of a property on an item. </summary>
    internal class AsyncSetValueRequest : AsyncDataRequest
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary> Constructor. </summary>
        internal AsyncSetValueRequest(object item,
                            string propertyName,
                            object value,
                            object bindingState,
                            AsyncRequestCallback workCallback,
                            AsyncRequestCallback completedCallback,
                            params object[] args
                            )
            : base(bindingState, workCallback, completedCallback, args)
        {
            _item = item;
            _propertyName = propertyName;
            _value = value;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> The item whose property is being set </summary>
        public object TargetItem { get { return _item; } }

        /* unused by default scheduler.  Restore for custom schedulers.
        /// <summary> The name of the property being set </summary>
        public string PropertyName { get { return _propertyName; } }
        */

        /// <summary> The new value for the property </summary>
        public object Value { get { return _value; } }

        //------------------------------------------------------
        //
        //  Private data
        //
        //------------------------------------------------------

        object _item;
        string _propertyName;
        object _value;
    }
}

