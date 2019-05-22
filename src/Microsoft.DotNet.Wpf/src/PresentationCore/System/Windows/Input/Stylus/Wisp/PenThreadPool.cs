// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input.Tracing;
using System.Windows.Threading;
using System.Security;
using System.Security.Permissions;
using MS.Utility;
using MS.Win32.Penimc;

namespace System.Windows.Input
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 
    /// </summary>
    internal class PenThreadPool
    {
        /// <summary>
        /// Limits the number of times to attempt getting a PenThread after
        /// the selected one does not add the context successfully.
        /// 
        /// Each PenThread can hold 30 contexts so this limit allows for 300
        /// re-entrant context adds after the initial AddPenContext call.
        /// </summary>
        private const int MAX_PENTHREAD_RETRIES = 10;

        /// <SecurityNote>
        /// Critical -  Constructor for singleton of our PenThreadPool.
        ///             marking this critical to prevent inadvertant access by transparent code
        ///
        ///         Called by critical methods:
        ///             Instance (above)
        ///
        /// </SecurityNote>
        [SecurityCritical]
        static PenThreadPool()
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <SecurityNote>
        /// Critical -  marking this critical to prevent inadvertant 
        ///             access by transparent code             
        ///
        /// </SecurityNote>
        [SecurityCritical]
        [ThreadStatic]
        private static PenThreadPool _penThreadPool;

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// </summary>
        /// <SecurityNote>
        /// Critical -  Returns a PenThread (creates as needed).
        ///             marking this critical to prevent inadvertant access by transparent code
        ///
        ///         Called by critical methods:
        ///             PenContext.Dispose
        ///             PenContext.Enable
        ///             PenContext.Disable
        ///
        /// </SecurityNote>
        [SecurityCritical]
        internal static PenThread GetPenThreadForPenContext(PenContext penContext)
        {
            // Create the threadstatic DynamicRendererThreadManager as needed for calling thread.
            // It only creates one 
            if (_penThreadPool == null)
            {
                _penThreadPool = new PenThreadPool();
            }
            return _penThreadPool.GetPenThreadForPenContextHelper(penContext); // Adds to weak ref list if creating new one.
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <SecurityNote>
        /// Critical -  marking this critical to prevent inadvertant 
        ///             access by transparent code             
        ///
        /// </SecurityNote>
        [SecurityCritical]
        private List<WeakReference<PenThread>> _penThreadWeakRefList;

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// 
        /// </summary>
        /// <SecurityNote>
        /// Critical -  Initializes critical data: m_PenThreads  
        ///
        /// </SecurityNote>
        [SecurityCritical]
        internal PenThreadPool()
        {
            _penThreadWeakRefList = new List<WeakReference<PenThread>>();
        }

        /// <summary>
        /// DevDiv:1192272
        /// 
        /// This function has been changed to avoid re-entrancy issues.  Previously, the
        /// PenThread selection depended on calls to AddPenContext in the selection mechanism
        /// or when creating a new PenThread.  Since AddPenContext will wait on operations done
        /// on a PenThread, this would allow re-entrant calls to occur.  These calls had the 
        /// potential to generate redundant PenThreads that could cause performance and functional
        /// issues in touch-enabled applications.
        /// 
        /// By removing calls to AddPenContext from the selection loops, we can be certain that there is
        /// no re-entrancy possible during this part of the code.  The call to AddPenContext is now done 
        /// post thread selection/creation.  While this is still re-entrant, we handle the possible issues
        /// from that case by retrying thread selection for any failed AddPenContext calls, ignoring the
        /// specific thread that failed.  After MAX_PENTHREAD_RETRIES, we exit and log an error.
        /// </summary>
        /// <SecurityNote>
        /// Critical - Calls SecurityCritical code (PenThread constructor).
        ///             Called by BeginService.
        ///             TreatAsSafe boundry is Stylus.EnableCore, Stylus.RegisterHwndForInput
        ///                and HwndWrapperHook class (via HwndSource.InputFilterMessage).
        /// </SecurityNote>
        [SecurityCritical]
        private PenThread GetPenThreadForPenContextHelper(PenContext penContext)
        {
            // A list of full PenThreads that we should ignore when attempting to select a thread
            // for this context.
            List<PenThread> ignoredThreads = new List<PenThread>();

            PenThread selectedPenThread = null;

            // We have gone over the max retries, something is forcing a huge amount
            // of re-entrancy.  In this case, break the loop and exit even if we might
            // have some issues with missing touch contexts and bad touch behavior.
            while (ignoredThreads.Count < MAX_PENTHREAD_RETRIES)
            {
                // Scan existing penthreads to find one to add the context to
                // We scan back to front to enable list cleanup.
                for (int i = _penThreadWeakRefList.Count - 1; i >= 0; i--)
                {
                    PenThread candidatePenThread = null;

                    // Select a thread if it's a valid WeakReference and we're not ignoring it
                    // Allow selection to happen multiple times so we get the first valid candidate
                    // in forward order.
                    if (_penThreadWeakRefList[i].TryGetTarget(out candidatePenThread)
                        && !ignoredThreads.Contains(candidatePenThread))
                    {
                        selectedPenThread = candidatePenThread;
                    }
                    // This is an invalid WeakReference and should be removed
                    else if (candidatePenThread == null)
                    {
                        _penThreadWeakRefList.RemoveAt(i);
                    }
                }

                // If no valid thread was found, create a new one and add to the pool
                if (selectedPenThread == null)
                {
                    selectedPenThread = new PenThread();

                    _penThreadWeakRefList.Add(new WeakReference<PenThread>(selectedPenThread));
                }

                // If we have no context or we can successfully add to it, then end with this thread
                if (penContext == null || selectedPenThread.AddPenContext(penContext))
                {
                    break;
                }
                // If the add wasn't successful, this thread is full, so try again and ignore it
                else
                {
                    ignoredThreads.Add(selectedPenThread);

                    selectedPenThread = null;

                    // Log re-entrant calls
                    StylusTraceLogger.LogReentrancy();
                }
            }

            //  If we're here due to max retries, log errors appropriately
            if (selectedPenThread == null)
            {
                StylusTraceLogger.LogReentrancyRetryLimitReached();

                Debug.Assert(false, "Retry limit reached when acquiring PenThread");
            }

            return selectedPenThread;
        }
    }
}
