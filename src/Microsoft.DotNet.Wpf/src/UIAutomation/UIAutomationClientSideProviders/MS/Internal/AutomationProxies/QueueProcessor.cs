// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//      Class to create a queue on the client context.  
//      WinEventHooks must be processed in the same thread that created them.
//      A seperate thread is created by the win32 proxy to manage the hooks.
//              

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MS.Win32;
using Microsoft.Win32.SafeHandles;

namespace MS.Internal.AutomationProxies
{
    class QueueProcessor
    {
        // ------------------------------------------------------
        //
        // Constructors
        //
        // ------------------------------------------------------

        #region Constructors

        // Usage is to create QueueProcessor object and call StartOnThread
        internal QueueProcessor(int initCapacity)
        {
            _ev = new AutoResetEvent(false);
            _q = Queue.Synchronized(new Queue(initCapacity));
        }

        // Default constructor.
        // Create a Queue of jobs with 4 elements
        internal QueueProcessor() : this(4) 
        {
        }

        #endregion
         
        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Create and start a background thread for this worker window to 
        // run on (background threads will exit if the main and foreground 
        // threads exit)
        internal void StartOnThread ()
        {
            ThreadStart threadStart = new ThreadStart(WaitForWork);
            Thread thread = new Thread(threadStart);
            thread.IsBackground = true;
            thread.Start();
        }

        // Post a work item and wait for it to be processed.
        // The processing of the item is done on the client thread
        internal bool PostSyncWorkItem (QueueItem workItem)
        {
            QueueItem.SyncQueueItem syncItem = new QueueItem.SyncQueueItem (workItem);

            // save the data
            _q.Enqueue (syncItem);

            // Start the processing on a different thread
            _ev.Set ();

            // Wait for the processing to be completed
            return syncItem._ev.WaitOne (2000, false);
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Methods
        //
        // ------------------------------------------------------
    
        #region Private Methods

        // Infinite loop. Wait for queue items to be processed.
        private void WaitForWork()
        {
            SafeWaitHandle handle = _ev.SafeWaitHandle;
            NativeMethods.MSG msg = new NativeMethods.MSG();

            while (true)
            {
                try
                {
                    // pump any messages
                    while (UnsafeNativeMethods.PeekMessage (ref msg, IntPtr.Zero, 0, 0, NativeMethods.PM_REMOVE))
                    {
                        if (msg.message == NativeMethods.WM_QUIT)
                        {
                            break;
                        }
                        Misc.DispatchMessage(ref msg);
                    }

                    // do any work items in the queue
                    // It's possible items could be enqueued between when we check for the count
                    // and dequeue but the event is set then and we'll come back into DrainQueue.
                    // (note: don't use a for loop here because as the counter is incremented
                    // Count is decremented and we'll only process half the queue)
                    while (_q.Count > 0)
                    {
                        // pull an item off the queue, process, then clear it
                        QueueItem item = (QueueItem) _q.Dequeue ();

                        item.Process ();
                    }

                    int result = Misc.MsgWaitForMultipleObjects(handle, false, NativeMethods.INFINITE, NativeMethods.QS_ALLINPUT);
                    if (result == NativeMethods.WAIT_FAILED || result == NativeMethods.WAIT_TIMEOUT)
                    {
                        Debug.Assert(false, "MsgWaitForMultipleObjects failed while WaitForWork");
                        break;
                    }
                }
                catch( Exception e )
                {
                    if (Misc.IsCriticalException(e))
                        throw;

                    // Might happen when if the hwnd goes away between the peek and the dispatch
                }
            }
        }

        #endregion

        // ------------------------------------------------------
        //
        // Private Fields
        //
        // ------------------------------------------------------
    
        #region Private Fields

        // A synchronized queue.
        private Queue _q;

        // Notifies when new queue items show up.
        private AutoResetEvent _ev;

        #endregion
    }

    // ------------------------------------------------------
    //
    //  QueueItem abstract class
    //
    //------------------------------------------------------

    #region QueueItem Abstract Class

    // Abstract class for worker objects queued to the QueueProcessor class
    abstract class QueueItem
    {
        // ------------------------------------------------------
        //
        // Internal Methods
        //
        // ------------------------------------------------------

        #region Internal Methods

        // Process an item in a different thread
        internal abstract void Process ();

        #endregion

        // ------------------------------------------------------
        //
        // SyncQueueItem Private Class
        //
        // ------------------------------------------------------

        #region SyncQueueItem

        // Not many of these will be created at a time so having the
        // event in the class is OK and easier than having just one
        // that manages any sync method call.
        internal class SyncQueueItem : QueueItem
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            // Queue an item to be processed in a different thread.
            internal SyncQueueItem (QueueItem qItem)
            {
                _ev = new AutoResetEvent (false);
                _qItem = qItem;
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Methods
            //
            // ------------------------------------------------------

            #region Internal Methods

            // Process an item from the queue
            internal override void Process ()
            {
                // Calls the overloaded version of Process
                _qItem.Process ();
                _ev.Set ();
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Fields
            //
            // ------------------------------------------------------

            #region Internal Fields

            internal AutoResetEvent _ev;

            #endregion

            // ------------------------------------------------------
            //
            // Private Fields
            //
            // ------------------------------------------------------

            #region Private Fields

            private QueueItem _qItem;

            #endregion
        }

        #endregion

        // ------------------------------------------------------
        //
        // WinEventItem Private Class
        //
        // ------------------------------------------------------

        #region WinEventItem

        // Worker class used to handle WinEvents
        internal class WinEventItem : QueueItem
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal WinEventItem (ref WinEventTracker.EventHookParams hp, WinEventTracker.StartStopDelegate ssd)
            {
                _hp = hp;
                _ssd = ssd;
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Methods
            //
            // ------------------------------------------------------

            #region Internal Methods

            internal override void Process ()
            {
                _ssd (ref _hp);
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Methods
            //
            // ------------------------------------------------------

            #region Private Field

            // WinEvent Hook parameters
            private WinEventTracker.EventHookParams _hp;

            // Delegate to Start/Stop the WinEvent thread on the client context
            private WinEventTracker.StartStopDelegate _ssd;

            #endregion
        }

        #endregion


        // ------------------------------------------------------
        //
        // MSAAWinEventItem Private Class
        //
        // ------------------------------------------------------

        #region MSAAWinEventItem

        // Worker class used to handle WinEvents
        internal class MSAAWinEventItem : QueueItem
        {
            // ------------------------------------------------------
            //
            // Constructors
            //
            // ------------------------------------------------------

            #region Constructors

            internal MSAAWinEventItem(MSAAWinEventWrap.StartStopDelegate ssd)
            {
                _ssd = ssd;
            }

            #endregion

            // ------------------------------------------------------
            //
            // Internal Methods
            //
            // ------------------------------------------------------

            #region Internal Methods

            internal override void Process()
            {
                _ssd();
            }

            #endregion

            // ------------------------------------------------------
            //
            // Private Methods
            //
            // ------------------------------------------------------

            #region Private Field

            // Delegate to Start/Stop the WinEvent thread on the client context
            private MSAAWinEventWrap.StartStopDelegate _ssd;

            #endregion
        }

        #endregion
    }

    #endregion
}
