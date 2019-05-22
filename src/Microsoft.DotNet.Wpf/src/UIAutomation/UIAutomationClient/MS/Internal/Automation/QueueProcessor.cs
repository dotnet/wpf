// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Class to create a queue on its own thread.

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using MS.Internal.Automation;
using MS.Win32;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MS.Internal.Automation
{
    // Abstract class for worker objects queued to the QueueProcessor class
    internal abstract class QueueItem
    {
        internal abstract void Process();
    }


    // Class to create a queue on its own thread.  Used  on the PAW client-side 
    // to queue client callbacks.  This prevents a deadlock situation when the
    // client calls back into PAW from w/in a callback.  All events are queued
    // through this class.
    internal class QueueProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Usage is create QueueProcessor object and call StartOnThread
        internal QueueProcessor(int initCapacity)
        {
            _ev = new AutoResetEvent(false);
            _q = Queue.Synchronized(new Queue(initCapacity));
        }

        internal QueueProcessor()
            : this(4)
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal void StartOnThread()
        {
            _quitting = false;

            // create and start a background thread for this worker window to run on
            // (background threads will exit if the main and foreground threads exit)
            ThreadStart threadStart = new ThreadStart(WaitForWork);
            _thread = new Thread(threadStart);
            _thread.IsBackground = true;
            _thread.Start();
        }

        // Post a work item to the queue (from another thread)
        internal bool PostWorkItem(QueueItem workItem)
        {
            Debug.Assert(!_quitting, "Can't add items to queue when quitting");
            _q.Enqueue(workItem);
            _ev.Set();
            return true;
        }

        // Post a work item and wait for it to be processed
        // Return true if the item was processed w/in 2 sec else false
        internal bool PostSyncWorkItem(QueueItem workItem)
        {
            Debug.Assert(!_quitting, "Can't add items to queue when quitting");
            SyncQueueItem syncItem = new SyncQueueItem(workItem);
            _q.Enqueue(syncItem);
            _ev.Set();
            return syncItem._ev.WaitOne(2000, false);
        }

        // Stop queuing and clean up (from another thread)
        internal void PostQuit()
        {
            _quitting = true;
            _ev.Set();
       }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // The loop the thread uses to wait for queue items to process
        private void WaitForWork()
        {
            SafeWaitHandle handle = _ev.SafeWaitHandle;
            UnsafeNativeMethods.MSG msg = new UnsafeNativeMethods.MSG();
            while (!_quitting)
            {
                // pump any messages
                while (UnsafeNativeMethods.PeekMessage(ref msg, NativeMethods.HWND.NULL, 0, 0, UnsafeNativeMethods.PM_REMOVE))
                {
                    if (msg.message == UnsafeNativeMethods.WM_QUIT)
                    {
                        break;
                    }
                    // From the Windows SDK documentation:
                    // The return value specifies the value returned by the window procedure.
                    // Although its meaning depends on the message being dispatched, the return
                    // value generally is ignored.
#pragma warning suppress 6031, 6523
                    UnsafeNativeMethods.DispatchMessage(ref msg);
                }

                // do any work items in the queue
                DrainQueue();

                int lastWin32Error = 0;
                int result = Misc.TryMsgWaitForMultipleObjects(handle, false, UnsafeNativeMethods.INFINITE, UnsafeNativeMethods.QS_ALLINPUT, ref lastWin32Error);
                if (result == UnsafeNativeMethods.WAIT_FAILED || result == UnsafeNativeMethods.WAIT_TIMEOUT)
                {
                    DrainQueue();
                    Debug.Assert(_quitting, "MsgWaitForMultipleObjects failed while WaitForWork");
                    break;
                }
            }
            DrainQueue();
        }

        private void DrainQueue()
        {
            // It's possible items could be enqueued between when we check for the count
            // and dequeue but the event is set then and we'll come back into DrainQueue.
            // (note: don't use a for loop here because as the counter is incremented
            // Count is decremented and we'll only process half the queue)
            while (_q.Count > 0)
            {
                // pull an item off the queue, process, then clear it
                QueueItem item = (QueueItem)_q.Dequeue();
                if (! _quitting)
                {
                    try
                    {
                        item.Process();
                    }
                    catch (Exception e)
                    {
                        if (Misc.IsCriticalException(e)) 
                            throw; 
                        // Eat it.
                        // There's no place to let this exception percolate out
                        // to so we'll stop it here.
                    }
                }
            }
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        Thread _thread;              // the thread on which QueueItem's are processed
        private Queue _q;            // a synchronized queue
        private AutoResetEvent _ev;  // notifies when new queue items show up
        private bool _quitting;      // true if need to stop queueing

        #endregion Private Fields
    }


    // Class is used only by QueueProcessor class itself - wraps
    // another QueueItem, and fires an event when it is complete.
    internal class SyncQueueItem : QueueItem
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal SyncQueueItem(QueueItem qItem)
        {
            _ev = new AutoResetEvent(false);
            _qItem = qItem;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal override void Process()
        {
            _qItem.Process();
            _ev.Set();
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // Not many of these will be created at a time so having the
        // event in the class is OK and easier than having just one
        // that manages any sync method call.
        internal AutoResetEvent _ev;
        private QueueItem _qItem;

        #endregion Private Fields
    }
}
