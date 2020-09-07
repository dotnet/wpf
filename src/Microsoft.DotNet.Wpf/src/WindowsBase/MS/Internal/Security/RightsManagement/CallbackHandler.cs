// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  These are the internal helpers that used to process callbacks from RM SDK 
//
//
//
//

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security;

// Enable presharp pragma warning suppress directives.
#pragma warning disable 1634, 1691

namespace MS.Internal.Security.RightsManagement
{
    internal delegate int CallbackDelegate(StatusMessage status, 
                                                            int hr, 
                                                            IntPtr pvParam,         // in the unmanaged SDK these 2 declared as void *
                                                            IntPtr pvContext);     // so the IntPtr is the right equivalent for both 64 and 32 bits

    // No need to synchronize access to these methods because they are only called on the user thread
    // This object is re-used during the same session which is why the event must be reset after the wait.
    // As a result of calling GC.SuppressFinalize(this) we also need to seal the class. As there is a danger 
    // of subclass introducing it's own Finalizer which will not be called.     
    internal sealed class CallbackHandler : IDisposable
    {
        internal CallbackHandler()
        {
            _resetEvent = new AutoResetEvent(false); // initialized to a false non-signaled state
            _callbackDelegate = new CallbackDelegate(OnStatus);
        }

        internal CallbackDelegate CallbackDelegate 
        {
            get
            {
                return _callbackDelegate;
            }
        }

        // this property is not to be accessed until WaitForCompletion returns
        internal string CallbackData
        {
            get
            {
                return _callbackData;
            }
        }

        // this is called from the user thread
        internal void WaitForCompletion()
        {
            _resetEvent.WaitOne(); // return to the reset state after unblocking current transaction (as it is an "Auto"ResetEvent)

            // second process possible managed exception from the other thread 
            if (_exception != null)
            {
                // rethrow exception that was cought from a worker thread 
                throw _exception;
            }

            Errors.ThrowOnErrorCode(_hr);             
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
 
        // this is the callback from the RM thread
        private int OnStatus(StatusMessage status, int hr, IntPtr pvParam, IntPtr pvContext)        
        {
            if (hr == S_DRM_COMPLETED || hr < 0)
            {
                _exception = null; // we are resetting this variable, so that the instance of this class will be reusable even after exception 
                
                try
                {
                    _hr = hr; // we are assigning hr first as the next command can potentially throw, and we would like to have hr value preserved 
                    
                    if (pvParam != IntPtr.Zero)
                    {
                        _callbackData = Marshal.PtrToStringUni(pvParam);
                    }
                }
// disabling PreSharp false positive. In this case we are actually re-throwing the same exception 
// on the application thread inside WaitForCompletion() function
#pragma warning disable 56500                  
                catch (Exception e)
                {
                    // We catch all exceptions of the second worker thread (created by the unmanaged RM SDK)
                    // preserve them , and then rethrow later  on the main thread from the WaitForCompletion method
                    _exception = e;
                }
#pragma warning restore 56500
                finally
                {
                    // After this signal, OnStatus will NOT be invoked until we instigate another "transaction"
                    // from the user thread.
                    _resetEvent.Set();
                }
            }
            return 0;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {   
                if (_resetEvent != null)
                {
                    _resetEvent.Set();
                    ((IDisposable)_resetEvent).Dispose();
                }
            }
        }


        private CallbackDelegate _callbackDelegate;

        private AutoResetEvent _resetEvent;
        private string _callbackData;
        private int _hr;
        private Exception _exception;

        private const uint S_DRM_COMPLETED                 = 0x0004CF04;  //success code 
    }
}
