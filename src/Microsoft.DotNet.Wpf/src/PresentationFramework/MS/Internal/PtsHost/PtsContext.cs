// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Context used to communicate with PTS component.
//

using System;                                   // IntPtr, IDisposable, ...
using System.Collections;                       // ArrayList
using System.Collections.Generic;               // List<T>
using System.Security;                          // SecurityCritical, SecurityTreatAsSafe
using System.Threading;                         // Interlocked
using System.Windows.Media.TextFormatting;      // TextFormatter
using System.Windows.Threading;                 // DispatcherObject
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS
using System.Windows.Media;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Context used to communicate with PTS component.
    /// Context keeps track of all unmanaged resources created by PTS.
    /// It also maps an instance of Object into an identity that can be used
    /// in unmanaged world. This identity can by easily mapped back into
    /// original instance of Object.
    /// </summary>
    internal sealed class PtsContext : DispatcherObject, IDisposable
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
        /// <remarks>
        /// The array of entries initially can store up to 16 entries. Upon
        /// adding elements the capacity increased in multiples of two as
        /// required. The first element always contains index to the next
        /// free entry. All free entries are forming a linked list.
        /// </remarks>
        internal PtsContext(bool isOptimalParagraphEnabled, TextFormattingMode textFormattingMode)
        {
            _pages = new ArrayList(1);
            _pageBreakRecords = new ArrayList(1);
            _unmanagedHandles = new HandleIndex[_defaultHandlesCapacity]; // Limit initial size
            _isOptimalParagraphEnabled = isOptimalParagraphEnabled;

            BuildFreeList(1); // 1 is the first free index in UnmanagedHandles array

            // Acquire PTS Context
            _ptsHost = PtsCache.AcquireContext(this, textFormattingMode);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Destroy all unmanaged resources associated with the PtsContext.
        /// </summary>
        public void Dispose()
        {
            int index;

            // Do actual dispose only once.
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                // Destroy all page break records. The collection is allocated during creation
                // of the context, and can be only destroyed during dispose process.
                // It is necessary to enter PTS Context when executing any PTS methods.
                try
                {
                    Enter();
                    for (index = 0; index < _pageBreakRecords.Count; index++)
                    {
                        Invariant.Assert(((IntPtr)_pageBreakRecords[index]) != IntPtr.Zero, "Invalid break record object");
                        PTS.Validate(PTS.FsDestroyPageBreakRecord(_ptsHost.Context, (IntPtr)_pageBreakRecords[index]));
                    }
                }
                finally
                {
                    Leave();
                    _pageBreakRecords = null;
                }

                // Destroy all pages. The collection is allocated during creation
                // of the context, and can be only destroyed during dispose process.
                // It is necessary to enter PTS Context when executing any PTS methods.
                try
                {
                    Enter();
                    for (index = 0; index < _pages.Count; index++)
                    {
                        Invariant.Assert(((IntPtr)_pages[index]) != IntPtr.Zero, "Invalid break record object");
                        PTS.Validate(PTS.FsDestroyPage(_ptsHost.Context, (IntPtr)_pages[index]));
                    }
                }
                finally
                {
                    Leave();
                    _pages = null;
                }

                if (Invariant.Strict && _unmanagedHandles != null)
                {
                    // Verify that PtsContext does not contain any reference to objects.
                    // Because order of finalizers is not deterministic, only objects
                    // that can be part of the NameTable are allowed here.
                    for (index = 0; index < _unmanagedHandles.Length; ++index)
                    {
                        Object obj = _unmanagedHandles[index].Obj;
                        if (obj != null)
                        {
                            Invariant.Assert(
                                obj is BaseParagraph ||
                                obj is Section ||
                                obj is MS.Internal.PtsHost.LineBreakRecord,  // Suppress line break record leak, looks like a PTS issue but we cannot 
                                                                             // get a firm repro for now. Workaround for bug #1294210.
                                "One of PTS Client objects is not properly disposed.");

#if DEBUG
                            // Make sure that FigureParagraphs are only used by TextParagraph
                            if (obj is FigureParagraph || obj is FloaterParagraph)
                            {
                                bool found = false;
                                for (int i = 0; i < _unmanagedHandles.Length; ++i)
                                {
                                    Object objDbg = _unmanagedHandles[i].Obj;
                                    if (objDbg is TextParagraph)
                                    {
                                        List<AttachedObject> attachedObjects = ((TextParagraph)objDbg).AttachedObjectDbg;
                                        if (attachedObjects != null)
                                        {
                                            foreach (AttachedObject attachedObject in attachedObjects)
                                            {
                                                if (attachedObject.Para == obj)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (found) { break; }
                                    }
                                }
                                Invariant.Assert(found, "FigureParagraph is not properly disposed.");
                            }
#endif
                        }
                    }
                }
                _ptsHost = null;
                _unmanagedHandles = null;
                _callbackException = null;
                _disposeCompleted = true;
            }
        }

        /// <summary>
        /// Inserts the object into reference array and returns its handle.
        /// </summary>
        /// <param name="obj">Object to be mapped to an unmanaged handle.</param>
        /// <returns>Unmanaged handle associated with the object.</returns>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal IntPtr CreateHandle(object obj)
        {
            Invariant.Assert(obj != null, "Cannot create handle for non-existing object.");
            Invariant.Assert(!this.Disposed, "PtsContext is already disposed.");

            // Ensure the size of handle array. The first item of the
            // array contains index of the next free position. If this
            // index is 0, it means that the array is full and needs to
            // be resized.
            if (_unmanagedHandles[0].Index == 0)
            {
                Resize();
            }

            // Assign a handle to the Object and adjust free index.
            long handle = _unmanagedHandles[0].Index;
            _unmanagedHandles[0].Index = _unmanagedHandles[handle].Index;
            _unmanagedHandles[handle].Obj = obj;
            _unmanagedHandles[handle].Index = 0;

            return (IntPtr)handle;
        }

        /// <summary>
        /// Removes reference to the object pointed by handle and release 
        /// the entry associated with it.
        /// </summary>
        /// <param name="handle">Handle of an Object being removed.</param>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal void ReleaseHandle(IntPtr handle)
        {
            long handleLong = (long)handle;
            Invariant.Assert(!_disposeCompleted, "PtsContext is already disposed."); // May be called from Dispose.
            Invariant.Assert(handleLong > 0 && handleLong < _unmanagedHandles.Length, "Invalid object handle.");
            Invariant.Assert(_unmanagedHandles[handleLong].IsHandle(), "Handle has been already released.");
            _unmanagedHandles[handleLong].Obj = null;
            _unmanagedHandles[handleLong].Index = _unmanagedHandles[0].Index;
            _unmanagedHandles[0].Index = handleLong;
        }

        /// <summary>
        /// Returns true if IntPtr is a handle
        /// </summary>
        /// <param name="handle">Handle of an Object.</param>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal bool IsValidHandle(IntPtr handle)
        {
            long handleLong = (long)handle;
            Invariant.Assert(!_disposeCompleted, "PtsContext is already disposed."); // May be called from Dispose.
            if (handleLong < 0 || handleLong >= _unmanagedHandles.Length)
            {
                return false;
            }
            return _unmanagedHandles[handleLong].IsHandle();
        }

        /// <summary>
        /// Maps handle to an Object.
        /// </summary>
        /// <param name="handle">Handle of an Object.</param>
        /// <returns>Reference to an Object.</returns>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal object HandleToObject(IntPtr handle)
        {
            long handleLong = (long)handle;
            Invariant.Assert(!_disposeCompleted, "PtsContext is already disposed."); // May be called from Dispose.
            Invariant.Assert(handleLong > 0 && handleLong < _unmanagedHandles.Length, "Invalid object handle.");
            Invariant.Assert(_unmanagedHandles[handleLong].IsHandle(), "Handle has been already released.");
            return _unmanagedHandles[handleLong].Obj;
        }

        /// <summary>
        /// Enters the PTS context. Called before executing PTS methods.
        /// </summary>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal void Enter()
        {
            Invariant.Assert(!_disposeCompleted, "PtsContext is already disposed."); // May be called from Dispose.
            _ptsHost.EnterContext(this);
        }

        /// <summary>
        /// Leaves the PTS context. Called after executing PTS methods.
        /// </summary>
        /// <remarks>
        /// Thread safe, see facts for the class description.
        /// </remarks>
        internal void Leave()
        {
            Invariant.Assert(!_disposeCompleted, "PtsContext is already disposed."); // May be called from Dispose.
            _ptsHost.LeaveContext(this);
        }

        /// <summary>
        /// Keeps track of created pages (unmanaged resource).
        /// When page is created, add it to the list.
        /// </summary>
        /// <param name="ptsPage">PTS Page object that was just created.</param>
        internal void OnPageCreated(SecurityCriticalDataForSet<IntPtr> ptsPage)
        {
            Invariant.Assert(ptsPage.Value != IntPtr.Zero, "Invalid page object.");
            Invariant.Assert(!this.Disposed, "PtsContext is already disposed.");
            Invariant.Assert(!_pages.Contains(ptsPage.Value), "Page already exists.");

            _pages.Add(ptsPage.Value);
        }

        /// <summary>
        /// Destroys PTS page.
        /// </summary>
        /// <param name="ptsPage">Pointer to PTS Page object that should be destroyed.</param>
        /// <param name="disposing">Whether dispose is caused by explicit call to Dispose.</param>
        /// <param name="enterContext">Whether needs to enter PtsContext or not (during layout it is not needed).</param>
        internal void OnPageDisposed(SecurityCriticalDataForSet<IntPtr> ptsPage, bool disposing, bool enterContext)
        {
            Invariant.Assert(ptsPage.Value != IntPtr.Zero, "Invalid page object.");

            // If explicitly disposing (not called during finalization), synchronously
            // destroy the page.
            if (disposing)
            {
                OnDestroyPage(ptsPage, enterContext);
            }
            else
            {
                // If PtsContext has been already disposed, ignore this call.
                if (!this.Disposed && !this.Dispatcher.HasShutdownStarted)
                {
                    // Schedule background operation to destroy the page.
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnDestroyPage), ptsPage);
                }
            }
        }

        /// <summary>
        /// Keeps track of created page BreakRecords (unmanaged resource).
        /// When PageBreakRecord is created, add it to the list.
        /// </summary>
        /// <param name="br">PTS Page BR object that was just created.</param>
        internal void OnPageBreakRecordCreated(SecurityCriticalDataForSet<IntPtr> br)
        {
            Invariant.Assert(br.Value != IntPtr.Zero, "Invalid break record object.");
            Invariant.Assert(!this.Disposed, "PtsContext is already disposed.");
            Invariant.Assert(!_pageBreakRecords.Contains(br.Value), "Break record already exists.");

            _pageBreakRecords.Add(br.Value);
        }

        /// <summary>
        /// Destroys PTS break record.
        /// </summary>
        /// <param name="br">Pointer to PTS Page BR object that should be destroyed.</param>
        /// <param name="disposing">Whether dispose is caused by explicit call to Dispose.</param>
        internal void OnPageBreakRecordDisposed(SecurityCriticalDataForSet<IntPtr> br, bool disposing)
        {
            Invariant.Assert(br.Value != IntPtr.Zero, "Invalid break record object.");

            // If explicitly disposing (not called during finalization), synchronously
            // destroy the page break record.
            if (disposing)
            {
                OnDestroyBreakRecord((object)br);
            }
            else
            {
                // If PtsContext has been already disposed, ignore this call.
                if (!this.Disposed && !this.Dispatcher.HasShutdownStarted)
                {
                    // Schedule background operation to destroy the page.
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnDestroyBreakRecord), br);
                }
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Whether object is already disposed.
        /// </summary>
        internal bool Disposed
        {
            get { return (_disposed != 0); }
        }

        /// <summary>
        /// Context Id used to communicate with PTS.
        /// </summary>
        internal IntPtr Context
        {
            get { return _ptsHost.Context; }
        }

        /// <summary>
        /// Whether optimal paragraph is enabled
        /// </summary>
        internal bool IsOptimalParagraphEnabled
        {
            get { return _isOptimalParagraphEnabled; }
        }

        /// <summary>
        /// Text formatter context for this pts context
        /// </summary>
        internal TextFormatter TextFormatter
        {
            get { return _textFormatter; }
            set { _textFormatter = value; }
        }

        /// <summary>
        /// Exception caught during callback execution. Those exceptions are
        /// converted into error codes and passed to PTS to provide appropriate
        /// clenaup. Later this exception is re-thrown.
        /// </summary>
        internal Exception CallbackException
        {
            get { return _callbackException; }
            set { _callbackException = value; }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Rebuilds list of free entries starting from specified index.
        /// </summary>
        /// <param name="freeIndex">Index to start from.</param>
        private void BuildFreeList(int freeIndex)
        {
            // Point to the first empty slot
            _unmanagedHandles[0].Index = freeIndex;
            // Link all entries starting from freeIndex
            while (freeIndex < _unmanagedHandles.Length)
            {
                _unmanagedHandles[freeIndex].Index = ++freeIndex;
            }
            // End of free entries list
            _unmanagedHandles[freeIndex - 1].Index = 0;
        }

        /// <summary>
        /// Increases the capacity of the handle array.
        ///      new size = current size * 2
        /// </summary>
        private void Resize()
        {
            int freeIndex = _unmanagedHandles.Length;

            // Allocate new array and copy all existing entries into it
            HandleIndex[] newItems = new HandleIndex[_unmanagedHandles.Length * 2];
            Array.Copy(_unmanagedHandles, newItems, _unmanagedHandles.Length);
            _unmanagedHandles = newItems;

            // Build list of free entries
            BuildFreeList(freeIndex);
        }

        /// <summary>
        /// Destroys PTS page.
        /// </summary>
        /// <param name="args">Pointer to PTS Page object that should be destroyed.</param>
        private object OnDestroyPage(object args)
        {
            SecurityCriticalDataForSet<IntPtr> ptsPage = (SecurityCriticalDataForSet<IntPtr>)args;
            OnDestroyPage(ptsPage, true);
            return null;
        }

        /// <summary>
        /// Destroys PTS page.
        /// </summary>
        /// <param name="ptsPage">Pointer to PTS Page object that should be destroyed.</param>
        /// <param name="enterContext">Whether needs to enter PTS Context.</param>
        private void OnDestroyPage(SecurityCriticalDataForSet<IntPtr> ptsPage, bool enterContext)
        {
            Invariant.Assert(ptsPage.Value != IntPtr.Zero, "Invalid page object.");

            // Dispatcher may invoke this operation when PtsContext is already explicitly
            // disposed.
            if (!this.Disposed)
            {
                Invariant.Assert(_pages != null, "Collection of pages does not exist.");
                Invariant.Assert(_pages.Contains(ptsPage.Value), "Page does not exist.");

                // Destroy given page.
                // It is necessary to enter PTS Context when executing any PTS methods.
                try
                {
                    if (enterContext)
                    {
                        Enter();
                    }
                    PTS.Validate(PTS.FsDestroyPage(_ptsHost.Context, ptsPage.Value));
                }
                finally
                {
                    if (enterContext)
                    {
                        Leave();
                    }
                    _pages.Remove(ptsPage.Value);
                }
            }
        }

        /// <summary>
        /// Destroys PTS page break record.
        /// </summary>
        /// <param name="args">Pointer to PTS Page BreakRecord object that should be destroyed.</param>
        private object OnDestroyBreakRecord(object args)
        {
            SecurityCriticalDataForSet<IntPtr> br = (SecurityCriticalDataForSet<IntPtr>)args;
            Invariant.Assert(br.Value != IntPtr.Zero, "Invalid break record object.");

            // Dispatcher may invoke this operation when PtsContext is already explicitly
            // disposed.
            if (!this.Disposed)
            {
                Invariant.Assert(_pageBreakRecords != null, "Collection of break records does not exist.");
                Invariant.Assert(_pageBreakRecords.Contains(br.Value), "Break record does not exist.");

                // Destroy given page break record.
                // It is necessary to enter PTS Context when executing any PTS methods.
                try
                {
                    Enter();
                    PTS.Validate(PTS.FsDestroyPageBreakRecord(_ptsHost.Context, br.Value));
                }
                finally
                {
                    Leave();
                    _pageBreakRecords.Remove(br.Value);
                }
            }
            return null;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Array of HandleIndex. This array stores 2 kind of information:
        /// {1) reference to Object; array index is a handle of the Object,
        /// (2) linked list of free entries; the first element (0) is always used
        ///     to point to the next free entry.
        /// </summary>
        /// <remarks>
        /// See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
        /// According to this article the entire reachable graph from 
        /// a finalizable object is promoted, and it is safe to access its 
        /// members if they do not have their own finalizers.
        /// Hence it is OK to access this array during finalization.
        /// </remarks>
        private HandleIndex[] _unmanagedHandles;

        /// <summary>
        /// List of created PTS pages. Those are unmanaged resources and
        /// have to be disposed.
        /// </summary>
        /// <remarks>
        /// See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
        /// According to this article the entire reachable graph from 
        /// a finalizable object is promoted, and it is safe to access its 
        /// members if they do not have their own finalizers.
        /// Hence it is OK to access this array during finalization.
        /// </remarks>
        private ArrayList _pages;

        /// <summary>
        /// List of created PTS BreakRecords. Those are unmanaged resources and
        /// have to be disposed.
        /// </summary>
        /// <remarks>
        /// See: http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx
        /// According to this article the entire reachable graph from 
        /// a finalizable object is promoted, and it is safe to access its 
        /// members if they do not have their own finalizers.
        /// Hence it is OK to access this array during finalization.
        /// </remarks>
        private ArrayList _pageBreakRecords;

        /// <summary>
        /// Exception caught during callback execution. Those exceptions are
        /// converted into error codes and passed to PTS to provide appropriate
        /// clenaup. Later this exception is re-thrown.
        /// </summary>
        private Exception _callbackException;

        /// <summary>
        /// PTS Host: all PTS callbacks are defined here.
        /// </summary>
        private PtsHost _ptsHost;

        /// <summary>
        /// Whether optimal paragraph is enabled for this ptscontext
        /// </summary>
        private bool _isOptimalParagraphEnabled;

        /// <summary>
        /// TextFormatter - Used only in optimal mode
        /// </summary>
        private TextFormatter _textFormatter;

        /// <summary>
        /// Whether object is already disposed.
        /// </summary>
        private int _disposed;

        /// <summary>
        /// Whether Dispose has been completed. It may be set to 'false' even when
        /// _disposed is set to 'true'. It may happen during Dispose execution.
        /// This flag is used for verification only.
        /// </summary>
        private bool _disposeCompleted;

        /// <summary>
        /// Default capacity of the UnmanagedHandles array. The array capacity 
        /// is always increased in multiples of two as required: 16*(2^N).
        /// </summary>
        private const int _defaultHandlesCapacity = 16;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// HandleIndex can store one of following information:
        /// {1) reference to Object
        /// (2) index of the next free entry
        /// </summary>
        private struct HandleIndex
        {
            internal long Index;
            internal object Obj;
            internal bool IsHandle()
            {
                return (Obj != null && Index == 0);
            }
        }

        #endregion Private Types
    }
}
