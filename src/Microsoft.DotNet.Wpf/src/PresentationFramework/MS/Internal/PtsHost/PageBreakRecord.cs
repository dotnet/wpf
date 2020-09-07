// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Break records for PTS pages.
//

using System;                                   // IntPtr, IDisposable, ...
using System.Threading;                         // Interlocked
using System.Security;                          // SecurityCritical
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// PageBreakRecord is used to represent a break record for top level PTS 
    /// page. Points to break record structure of PTS page.
    /// </summary>
    internal sealed class PageBreakRecord : IDisposable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ptsContext">Current PTS Context.</param>
        /// <param name="br">PTS page break record.</param>
        /// <param name="pageNumber">Page number.</param>
        internal PageBreakRecord(PtsContext ptsContext, SecurityCriticalDataForSet<IntPtr> br, int pageNumber)
        {
            Invariant.Assert(ptsContext != null, "Invalid PtsContext object.");
            Invariant.Assert(br.Value != IntPtr.Zero, "Invalid break record object.");

            _br = br;
            _pageNumber = pageNumber;

            // In the finalizer we may need to reference an object instance
            // to do the right cleanup. For this reason store a WeakReference
            // to such object.
            _ptsContext = new WeakReference(ptsContext);

            // BreakRecord contains unmanaged resources that need to be released when
            // BreakRecord is destroyed or Dispatcher is closing. For this reason keep 
            // track of this BreakRecord in PtsContext. 
            ptsContext.OnPageBreakRecordCreated(_br);
        }

        /// <summary>
        /// Finalizer - releases unmanaged resources.
        /// </summary>
        ~PageBreakRecord()
        {
            Dispose(false);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Dispose allocated resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// PTS page break record.
        /// </summary>
        internal IntPtr BreakRecord
        {
            get { return _br.Value; }
        }

        /// <summary>
        /// Page number of the page starting at the break position. 
        /// </summary>
        internal int PageNumber
        {
            get { return _pageNumber; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Destroys all unmanaged resources.
        /// </summary>
        /// <param name="disposing">Whether dispose is caused by explicit call to Dispose.</param>
        /// <remarks>
        /// Finalizer needs to follow rules below:
        ///     a) Your Finalize method must tolerate partially constructed instances.
        ///     b) Your Finalize method must consider the consequence of failure.
        ///     c) Your object is callable after Finalization.
        ///     d) Your object is callable during Finalization.
        ///     e) Your Finalizer could be called multiple times.
        ///     f) Your Finalizer runs in a delicate security context.
        /// See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
        /// </remarks>
        private void Dispose(bool disposing)
        {
            PtsContext ptsContext = null;

            // Do actual dispose only once.
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                // Dispose PTS break record.
                // According to following article the entire reachable graph from 
                // a finalizable object is promoted, and it is safe to access its 
                // members if they do not have their own finalizers.
                // Hence it is OK to access _ptsContext during finalization.
                // See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
                ptsContext = _ptsContext.Target as PtsContext;
                if (ptsContext != null && !ptsContext.Disposed)
                {
                    // Notify PtsContext that BreakRecord is not used anymore, so all
                    // associated resources can by destroyed.
                    ptsContext.OnPageBreakRecordDisposed(_br, disposing);
                }

                // Cleanup the state.
                _br.Value = IntPtr.Zero;
                _ptsContext = null;
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// PTS page break record.
        /// </summary>
        private SecurityCriticalDataForSet<IntPtr> _br;

        /// <summary>
        /// Page number of the page starting at the break position.
        /// </summary>
        private readonly int _pageNumber;

        /// <summary>
        /// In the finalizer we may need to reference an instance of PtsContext
        /// to do the right cleanup. For this reason store a WeakReference
        /// to it.
        /// </summary>
        private WeakReference _ptsContext;

        /// <summary>
        /// Whether object is already disposed.
        /// </summary>
        private int _disposed;

        #endregion Private Fields
    }
}
