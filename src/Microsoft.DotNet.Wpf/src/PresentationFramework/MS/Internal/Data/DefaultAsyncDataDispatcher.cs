// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Default async scheduler for data operations.
//

using System.Collections;
using System.Threading;
using System.Windows;

namespace MS.Internal.Data
{
    internal class DefaultAsyncDataDispatcher : IAsyncDataDispatcher
    {
        //------------------------------------------------------
        //
        //  Interface: IAsyncDataDispatcher
        //
        //------------------------------------------------------

        /// <summary> Add a request to the dispatcher's queue </summary>
        void IAsyncDataDispatcher.AddRequest(AsyncDataRequest request)
        {
            lock (_list.SyncRoot)
            {
                _list.Add(request);
            }

            ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessRequest), request);
        }

        /// <summary> Cancel all requests in the dispatcher's queue </summary>
        void IAsyncDataDispatcher.CancelAllRequests()
        {
            lock (_list.SyncRoot)
            {
                for (int i = 0; i < _list.Count; ++i)
                {
                    AsyncDataRequest request = (AsyncDataRequest)_list[i];
                    request.Cancel();
                }

                _list.Clear();
            }
        }

        //------------------------------------------------------
        //
        //  Private methods
        //
        //------------------------------------------------------

        // Run a single request.  This method gets scheduled on a worker thread
        // from the process ThreadPool.
        private void ProcessRequest(object o)
        {
            AsyncDataRequest request = (AsyncDataRequest)o;

            // PreSharp complains about catching NullReference (and other) exceptions.
            // In this case, these are precisely the ones we want to catch the most,
            // so that a failure on a worker thread doesn't affect the main thread.

            // run the request - this may take a while
            try
            {
                request.Complete(request.DoWork());
            }

            // Catch all exceptions.  There is no app code on the stack,
            // so the exception isn't actionable by the app.
            // Yet we don't want to crash the app.
            catch (Exception ex)
            {
                if (CriticalExceptions.IsCriticalApplicationException(ex))
                    throw;

                request.Fail(ex);
            }
            catch // non CLS compliant exception
            {
                request.Fail(new InvalidOperationException(SR.Format(SR.NonCLSException, "processing an async data request")));
            }

            // remove the request from the list
            lock (_list.SyncRoot)
            {
                _list.Remove(request);
            }
        }

        //------------------------------------------------------
        //
        //  Private data
        //
        //------------------------------------------------------

        private ArrayList _list = new ArrayList();
    }
}
