// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* SSS_DROP_BEGIN */

/*************************************************************************
* 11/17/07 - bartde
*
* NOTICE: Code excluded from Developer Reference Sources.
*         Don't remove the SSS_DROP_BEGIN directive on top of the file.
*
* Reason for exclusion: obscure PTLS interface
*
**************************************************************************/


//
// Description: Definition of class responsible for PTS Context lifetime
//              management.
//

using System;                                   // IntPtr
using System.Collections.Generic;               // List<T>
using System.Security;                          // SecurityCritical, SecurityTreatAsSafe
using System.Threading;                         // Interlocked
using System.Windows;                           // WrapDirection
using System.Windows.Media.TextFormatting;      // TextFormatter
using System.Windows.Threading;                 // Dispatcher
using System.Windows.Media;
using MS.Internal.Text;                         // TextDpi
using MS.Internal.TextFormatting;               // UnsafeTextPenaltyModule
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// PtsCache class encapsulates lifetime management of PTS Context.
    /// Internally provides caching mechanism, which enables reusing PTS Context.
    /// </summary>
    /// <remarks>
    /// Instead of using static instance of PtsCache, instance of PtsCache
    /// could be stored in the current Dispatcher as context data. That
    /// would be much cleaner design. But there is one problem:
    /// Thread.GetData() is a static method that accesses the current
    /// thread's Dispatcher. Since DependencyObject may be created using
    /// different Dispatcher then the current one, incorrect PtsCache
    /// could be retrieved.
    /// For this reason there is only one PtsCache instance that stores
    /// mapping between Dispatcher and PTS context pool.
    /// </remarks>
    internal sealed class PtsCache
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Acquires new PTS Context and associates it with new owner.
        /// </summary>
        /// <param name="ptsContext">Context used to communicate with PTS component.</param>
        internal static PtsHost AcquireContext(PtsContext ptsContext, TextFormattingMode textFormattingMode)
        {
            PtsCache ptsCache = ptsContext.Dispatcher.PtsCache as PtsCache;
            if (ptsCache == null)
            {
                ptsCache = new PtsCache(ptsContext.Dispatcher);
                ptsContext.Dispatcher.PtsCache = ptsCache;
            }
            return ptsCache.AcquireContextCore(ptsContext, textFormattingMode);
        }

        /// <summary>
        /// Notifies PtsCache about destruction of a PtsContext.
        /// </summary>
        /// <param name="ptsContext">Context used to communicate with PTS component.</param>
        internal static void ReleaseContext(PtsContext ptsContext)
        {
            PtsCache ptsCache = ptsContext.Dispatcher.PtsCache as PtsCache;
            Invariant.Assert(ptsCache != null, "Cannot retrieve PtsCache from PtsContext object.");
            ptsCache.ReleaseContextCore(ptsContext);
        }

        /// <summary>
        /// Retrieves floater handler callbacks.
        /// </summary>
        /// <param name="ptsHost">Host of the PTS component.</param>
        /// <param name="pobjectinfo">Struct with callbacks to fill in.</param>
        internal static void GetFloaterHandlerInfo(PtsHost ptsHost, IntPtr pobjectinfo)
        {
            PtsCache ptsCache = Dispatcher.CurrentDispatcher.PtsCache as PtsCache;
            Invariant.Assert(ptsCache != null, "Cannot retrieve PtsCache from the current Dispatcher.");
            ptsCache.GetFloaterHandlerInfoCore(ptsHost, pobjectinfo);
        }

        /// <summary>
        /// Retrieves table handler callbacks.
        /// </summary>
        /// <param name="ptsHost">Host of the PTS component.</param>
        /// <param name="pobjectinfo">Struct with callbacks to fill in.</param>
        internal static void GetTableObjHandlerInfo(PtsHost ptsHost, IntPtr pobjectinfo)
        {
            PtsCache ptsCache = Dispatcher.CurrentDispatcher.PtsCache as PtsCache;
            Invariant.Assert(ptsCache != null, "Cannot retrieve PtsCache from the current Dispatcher.");
            ptsCache.GetTableObjHandlerInfoCore(ptsHost, pobjectinfo);
        }

        /// <summary>
        /// Checks whether PtsCache is already diposed.
        /// </summary>
        internal static bool IsDisposed()
        {
            bool disposed = true;
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            if (dispatcher != null)
            {
                PtsCache ptsCache = Dispatcher.CurrentDispatcher.PtsCache as PtsCache;
                if (ptsCache != null)
                {
                    disposed = (ptsCache._disposed == 1);
                }
            }
            return disposed;
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Constructor - private to protect agains initialization.
        /// </summary>
        /// <param name="dispatcher">Dispatcher associated with PtsCache.</param>
        private PtsCache(Dispatcher dispatcher)
        {
            // Initially allocate just one entry. The constructor gets called
            // when acquiring the first PTS Context, so it guarantees at least
            // one element in the collection.
            _contextPool = new List<ContextDesc>(1);

            // Register for ShutdownFinished event for the Dispatcher. When Dispatcher
            // finishes the shutdown process, all associated resources stored in its
            // PTS context pool need to be disposed as well.
            // NOTE: Cannot do this work during ShutdownStarted, because after this
            //       event is fired, layout process may still be executed.

            // Add an event handler for AppDomain unload. If Dispatcher is not running
            // we are not going to receive ShutdownFinished to do appropriate cleanup.
            // When an AppDomain is unloaded, we'll be called back on a worker thread.
            PtsCacheShutDownListener listener = new PtsCacheShutDownListener(this);
        }

        /// <summary>
        /// PtsCache finalizer.
        /// </summary>
        ~PtsCache()
        {
            // After shutdown is initiated, do not allow Finalizer thread to the cleanup.
            if (0 == Interlocked.CompareExchange(ref _disposed, 1, 0))
            {
                // Destroy all PTS contexts
                DestroyPTSContexts();
            }
        }

        /// <summary>
        /// Acquires new PTS Context and associate it with new owner.
        /// </summary>
        /// <param name="ptsContext">Context used to communicate with PTS component.</param>
        /// <returns>PtsHost associated with new owner.</returns>
        private PtsHost AcquireContextCore(PtsContext ptsContext, TextFormattingMode textFormattingMode)
        {
            int index;

            // Look for the first free PTS Context.
            for (index = 0; index < _contextPool.Count; index++)
            {
                if (!_contextPool[index].InUse &&
                    _contextPool[index].IsOptimalParagraphEnabled == ptsContext.IsOptimalParagraphEnabled)
                {
                    break;
                }
            }

            // Create new PTS Context, if cannot find free one.
            if (index == _contextPool.Count)
            {
                _contextPool.Add(new ContextDesc());
                _contextPool[index].IsOptimalParagraphEnabled = ptsContext.IsOptimalParagraphEnabled;
                _contextPool[index].PtsHost = new PtsHost();
                _contextPool[index].PtsHost.Context = CreatePTSContext(index, textFormattingMode);
            }

            // Initialize TextFormatter, if optimal paragraph is enabled.
            // Optimal paragraph requires new TextFormatter for every PTS Context.
            if (_contextPool[index].IsOptimalParagraphEnabled)
            {
                ptsContext.TextFormatter = _contextPool[index].TextFormatter;
            }

            // Assign PTS Context to new owner.
            _contextPool[index].InUse = true;
            _contextPool[index].Owner = new WeakReference(ptsContext);

            return _contextPool[index].PtsHost;
        }

        /// <summary>
        /// Notifies PtsCache about destruction of a PtsContext.
        /// </summary>
        /// <param name="ptsContext">Context used to communicate with PTS component.</param>
        private void ReleaseContextCore(PtsContext ptsContext)
        {
            // _releaseQueue may be accessed from Finalizer thread or Dispatcher thread.
            lock (_lock)
            {
                // After shutdown is initiated, do not allow Finalizer thread to add any
                // items to _releaseQueue.
                if (_disposed == 0)
                {
                    // Add PtsContext to collection of released PtsContexts.
                    // If the queue is empty, schedule Dispatcher time to dispose
                    // PtsContexts in the Dispatcher thread.
                    // If the queue is not empty, there is already pending Dispatcher request.
                    if (_releaseQueue == null)
                    {
                        _releaseQueue = new List<PtsContext>();
                        ptsContext.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(OnPtsContextReleased), null);
                    }
                    _releaseQueue.Add(ptsContext);
                }
            }
        }

        /// <summary>
        /// Retrieves floater handler callbacks.
        /// </summary>
        /// <param name="ptsHost">Host of the PTS component.</param>
        /// <param name="pobjectinfo">Struct with callbacks to fill in.</param>
        private void GetFloaterHandlerInfoCore(PtsHost ptsHost, IntPtr pobjectinfo)
        {
            int index;
            for (index = 0; index < _contextPool.Count; index++)
            {
                if (_contextPool[index].PtsHost == ptsHost)
                {
                    break;
                }
            }
            Invariant.Assert(index < _contextPool.Count, "Cannot find matching PtsHost in the Context pool.");
            PTS.Validate(PTS.GetFloaterHandlerInfo(ref _contextPool[index].FloaterInit, pobjectinfo));
        }

        /// <summary>
        /// Retrieves table handler callbacks.
        /// </summary>
        /// <param name="ptsHost">Host of the PTS component.</param>
        /// <param name="pobjectinfo">Struct with callbacks to fill in.</param>
        private void GetTableObjHandlerInfoCore(PtsHost ptsHost, IntPtr pobjectinfo)
        {
            int index;
            for (index = 0; index < _contextPool.Count; index++)
            {
                if (_contextPool[index].PtsHost == ptsHost)
                {
                    break;
                }
            }
            Invariant.Assert(index < _contextPool.Count, "Cannot find matching PtsHost in the context pool.");
            PTS.Validate(PTS.GetTableObjHandlerInfo(ref _contextPool[index].TableobjInit, pobjectinfo));
        }

        /// <summary>
        /// Delete all resources associated with PtsCache (PTS Contexts)
        /// </summary>
        private void Shutdown()
        {
            // WeakReference.Target is NULL when object is in the finalization queue.
            // Hence there is possibility to destroy PTS Context before all pages are
            // destroyed from the finalizer of PtsContext.
            // Workaround: wait for all finalizers to run before destroying any PTS contexts.
            GC.WaitForPendingFinalizers();

            // After shutdown is initiated, do not allow Finalizer thread to add any
            // items to _releaseQueue.
            if (0 == Interlocked.CompareExchange(ref _disposed, 1, 0))
            {
                // Dispose any pending PtsContexts stored in _releaseQueue
                OnPtsContextReleased(false);

                // Destroy all PTS contexts
                DestroyPTSContexts();
            }
        }

        /// <summary>
        /// Destroy all PTS contexts.
        /// </summary>
        private void DestroyPTSContexts()
        {
            // Destroy all unused PTS Contexts.
            int index = 0;
            while (index < _contextPool.Count)
            {
                PtsContext ptsContext = _contextPool[index].Owner.Target as PtsContext;
                if (ptsContext != null)
                {
                    Invariant.Assert(_contextPool[index].PtsHost.Context == ptsContext.Context, "PTS Context mismatch.");
                    _contextPool[index].Owner = new WeakReference(null);
                    _contextPool[index].InUse = false;

                    Invariant.Assert(!ptsContext.Disposed, "PtsContext has been already disposed.");
                    ptsContext.Dispose();
                }

                if (!_contextPool[index].InUse)
                {
                    // Ignore any errors during shutdown. Reason:
                    // * make sure that loop continues and all contexts have a chance to be destroyed.
                    // * this is shutdown case, so even if memory is not disposed, the system will reclaim it.
                    Invariant.Assert(_contextPool[index].PtsHost.Context != IntPtr.Zero, "PTS Context handle is not valid.");
                    PTS.IgnoreError(PTS.DestroyDocContext(_contextPool[index].PtsHost.Context));
                    Invariant.Assert(_contextPool[index].InstalledObjects != IntPtr.Zero, "Installed Objects handle is not valid.");
                    PTS.IgnoreError(PTS.DestroyInstalledObjectsInfo(_contextPool[index].InstalledObjects));
                    // Explicitly dispose the penalty module object to ensure proper destruction
                    // order of PTSContext  and the penalty module (PTS context must be destroyed first).
                    if (_contextPool[index].TextPenaltyModule != null)
                    {
                        _contextPool[index].TextPenaltyModule.Dispose();
                    }

                    _contextPool.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        /// <summary>
        /// Cleans up PTS Context pool.
        /// </summary>
        /// <param name="args">Not used.</param>
        private object OnPtsContextReleased(object args)
        {
            OnPtsContextReleased(true);
            return null;
        }

        /// <summary>
        /// Cleans up PTS Context pool.
        /// </summary>
        /// <param name="cleanContextPool">Whether needs to clean context pool.</param>
        private void OnPtsContextReleased(bool cleanContextPool)
        {
            int index;

            // _releaseQueue may be accessed from Finalizer thread or Dispatcher thread.
            lock (_lock)
            {
                // Dispose any pending PtsContexts
                if (_releaseQueue != null)
                {
                    foreach (PtsContext ptsContext in _releaseQueue)
                    {
                        // Find the PtsContext in the context pool and detach it from PtsHost.
                        for (index = 0; index < _contextPool.Count; index++)
                        {
                            if (_contextPool[index].PtsHost.Context == ptsContext.Context)
                            {
                                _contextPool[index].Owner = new WeakReference(null);
                                _contextPool[index].InUse = false;
                                break;
                            }
                        }
                        Invariant.Assert(index < _contextPool.Count, "PtsContext not found in the context pool.");

                        // Dispose PtsContext.
                        Invariant.Assert(!ptsContext.Disposed, "PtsContext has been already disposed.");
                        ptsContext.Dispose();
                    }
                    _releaseQueue = null;
                }
            }

            // Remove all unused PTS Contexts. Leave at least 4 entries for future use.
            if (cleanContextPool && _contextPool.Count > 4)
            {
                // Destroy all unused PTS Contexts.
                index = 4;
                while (index < _contextPool.Count)
                {
                    if (!_contextPool[index].InUse)
                    {
                        Invariant.Assert(_contextPool[index].PtsHost.Context != IntPtr.Zero, "PTS Context handle is not valid.");
                        PTS.Validate(PTS.DestroyDocContext(_contextPool[index].PtsHost.Context));
                        Invariant.Assert(_contextPool[index].InstalledObjects != IntPtr.Zero, "Installed Objects handle is not valid.");
                        PTS.Validate(PTS.DestroyInstalledObjectsInfo(_contextPool[index].InstalledObjects));
                        // Explicitly dispose the penalty module object to ensure proper destruction
                        // order of PTSContext  and the penalty module (PTS context must be destroyed first).
                        if (_contextPool[index].TextPenaltyModule != null)
                        {
                            _contextPool[index].TextPenaltyModule.Dispose();
                        }
                        _contextPool.RemoveAt(index);
                        continue;
                    }
                    index++;
                }
            }
        }

        /// <summary>
        /// Creates a new PTS context using PTSWrapper APIs.
        /// </summary>
        /// <param name="index">Index to free entry in the PTS Context pool.</param>
        /// <returns>PTS Context ID.</returns>
        private IntPtr CreatePTSContext(int index, TextFormattingMode textFormattingMode)
        {
            PtsHost ptsHost;
            IntPtr installedObjects;
            int installedObjectsCount;
            TextFormatterContext textFormatterContext;
            IntPtr context;

            ptsHost = _contextPool[index].PtsHost;
            Invariant.Assert(ptsHost != null);

            // Create installed object info.
            InitInstalledObjectsInfo(ptsHost, ref _contextPool[index].SubtrackParaInfo, ref _contextPool[index].SubpageParaInfo, out installedObjects, out installedObjectsCount);
            _contextPool[index].InstalledObjects = installedObjects;

            // Create generic callbacks info.
            InitGenericInfo(ptsHost, (IntPtr)(index + 1), installedObjects, installedObjectsCount, ref _contextPool[index].ContextInfo);

            // Preallocated floater and table info.
            InitFloaterObjInfo(ptsHost, ref _contextPool[index].FloaterInit);
            InitTableObjInfo(ptsHost, ref _contextPool[index].TableobjInit);

            // Setup for optimal paragraph
            if (_contextPool[index].IsOptimalParagraphEnabled)
            {
                textFormatterContext = new TextFormatterContext();
                TextPenaltyModule penaltyModule = textFormatterContext.GetTextPenaltyModule();
                IntPtr ptsPenaltyModule = penaltyModule.DangerousGetHandle();

                _contextPool[index].TextPenaltyModule = penaltyModule;
                _contextPool[index].ContextInfo.ptsPenaltyModule = ptsPenaltyModule;
                _contextPool[index].TextFormatter = TextFormatter.CreateFromContext(textFormatterContext, textFormattingMode);

                // Explicitly take the penalty module object out of finalization queue;
                // PTSCache must manage lifetime of the penalty module explicitly by calling
                // TextPenaltyModule.Dispose to ensure proper destruction order of PTSContext
                // and the penalty module (PTS context must be destroyed first).
                GC.SuppressFinalize(_contextPool[index].TextPenaltyModule);
            }

            // Create PTS Context
            PTS.Validate(PTS.CreateDocContext(ref _contextPool[index].ContextInfo, out context));

            return context;
        }

        /// <summary>
        /// Initializes generic PTS callbacks.
        /// </summary>
        /// <param name="ptsHost">PtsHost that defines all PTS callbacks.</param>
        /// <param name="clientData">Unique PTS Client ID.</param>
        /// <param name="installedObjects">PTS Installed objects.</param>
        /// <param name="installedObjectsCount">Count of PTS Installed objects.</param>
        /// <param name="contextInfo">PTS Context Info to be initialized.</param>
        private unsafe void InitGenericInfo(PtsHost ptsHost, IntPtr clientData, IntPtr installedObjects, int installedObjectsCount, ref PTS.FSCONTEXTINFO contextInfo)
        {
            // Validation
            Invariant.Assert(((int)PTS.FSKREF.fskrefPage) == 0);
            Invariant.Assert(((int)PTS.FSKREF.fskrefMargin) == 1);
            Invariant.Assert(((int)PTS.FSKREF.fskrefParagraph) == 2);
            Invariant.Assert(((int)PTS.FSKREF.fskrefChar) == 3);
            Invariant.Assert(((int)PTS.FSKALIGNFIG.fskalfMin) == 0);
            Invariant.Assert(((int)PTS.FSKALIGNFIG.fskalfCenter) == 1);
            Invariant.Assert(((int)PTS.FSKALIGNFIG.fskalfMax) == 2);
            Invariant.Assert(((int)PTS.FSKWRAP.fskwrNone) == ((int)WrapDirection.None));
            Invariant.Assert(((int)PTS.FSKWRAP.fskwrLeft) == ((int)WrapDirection.Left));
            Invariant.Assert(((int)PTS.FSKWRAP.fskwrRight) == ((int)WrapDirection.Right));
            Invariant.Assert(((int)PTS.FSKWRAP.fskwrBoth) == ((int)WrapDirection.Both));
            Invariant.Assert(((int)PTS.FSKWRAP.fskwrLargest) == 4);
            Invariant.Assert(((int)PTS.FSKCLEAR.fskclearNone) == 0);
            Invariant.Assert(((int)PTS.FSKCLEAR.fskclearLeft) == 1);
            Invariant.Assert(((int)PTS.FSKCLEAR.fskclearRight) == 2);
            Invariant.Assert(((int)PTS.FSKCLEAR.fskclearBoth) == 3);

            // Initialize context info
            contextInfo.version = 0;
            contextInfo.fsffi = PTS.fsffiUseTextQuickLoop
                // This flag is added toward the end of WPF V1 project.
                // PTS requires that break record is present in ReconstructLineVariant call,
                // unfortunately TextFormatter can't give them that as it breaks single-line
                // mode in Bidi scenario. Since break record is never a requirement before,
                // PTS agrees to address this issue in the next version. The current solution
                // for V1 is to temporary disable optimal formatting of the paragraph when
                // figire is fully embedded within the paragraph with text on both sides of the
                // figure. [Windows bug #1506821; WChao, 5/18/2006]
                | PTS.fsffiAvalonDisableOptimalInChains;
            contextInfo.drMinColumnBalancingStep = TextDpi.ToTextDpi(10.0);  // Assume 10px as minimal step
            contextInfo.cInstalledObjects = installedObjectsCount;
            contextInfo.pInstalledObjects = installedObjects;
            contextInfo.pfsclient = clientData;
            contextInfo.pfnAssertFailed = new PTS.AssertFailed(ptsHost.AssertFailed);
            // Initialize figure callbacks
            contextInfo.fscbk.cbkfig.pfnGetFigureProperties = new PTS.GetFigureProperties(ptsHost.GetFigureProperties);
            contextInfo.fscbk.cbkfig.pfnGetFigurePolygons = new PTS.GetFigurePolygons(ptsHost.GetFigurePolygons);
            contextInfo.fscbk.cbkfig.pfnCalcFigurePosition = new PTS.CalcFigurePosition(ptsHost.CalcFigurePosition);
            // Initialize generic callbacks
            contextInfo.fscbk.cbkgen.pfnFSkipPage = new PTS.FSkipPage(ptsHost.FSkipPage);
            contextInfo.fscbk.cbkgen.pfnGetPageDimensions = new PTS.GetPageDimensions(ptsHost.GetPageDimensions);
            contextInfo.fscbk.cbkgen.pfnGetNextSection = new PTS.GetNextSection(ptsHost.GetNextSection);
            contextInfo.fscbk.cbkgen.pfnGetSectionProperties = new PTS.GetSectionProperties(ptsHost.GetSectionProperties);
            contextInfo.fscbk.cbkgen.pfnGetJustificationProperties = new PTS.GetJustificationProperties(ptsHost.GetJustificationProperties);
            contextInfo.fscbk.cbkgen.pfnGetMainTextSegment = new PTS.GetMainTextSegment(ptsHost.GetMainTextSegment);
            contextInfo.fscbk.cbkgen.pfnGetHeaderSegment = new PTS.GetHeaderSegment(ptsHost.GetHeaderSegment);
            contextInfo.fscbk.cbkgen.pfnGetFooterSegment = new PTS.GetFooterSegment(ptsHost.GetFooterSegment);
            contextInfo.fscbk.cbkgen.pfnUpdGetSegmentChange = new PTS.UpdGetSegmentChange(ptsHost.UpdGetSegmentChange);
            contextInfo.fscbk.cbkgen.pfnGetSectionColumnInfo = new PTS.GetSectionColumnInfo(ptsHost.GetSectionColumnInfo);
            contextInfo.fscbk.cbkgen.pfnGetSegmentDefinedColumnSpanAreaInfo = new PTS.GetSegmentDefinedColumnSpanAreaInfo(ptsHost.GetSegmentDefinedColumnSpanAreaInfo);
            contextInfo.fscbk.cbkgen.pfnGetHeightDefinedColumnSpanAreaInfo = new PTS.GetHeightDefinedColumnSpanAreaInfo(ptsHost.GetHeightDefinedColumnSpanAreaInfo);
            contextInfo.fscbk.cbkgen.pfnGetFirstPara = new PTS.GetFirstPara(ptsHost.GetFirstPara);
            contextInfo.fscbk.cbkgen.pfnGetNextPara = new PTS.GetNextPara(ptsHost.GetNextPara);
            contextInfo.fscbk.cbkgen.pfnUpdGetFirstChangeInSegment = new PTS.UpdGetFirstChangeInSegment(ptsHost.UpdGetFirstChangeInSegment);
            contextInfo.fscbk.cbkgen.pfnUpdGetParaChange = new PTS.UpdGetParaChange(ptsHost.UpdGetParaChange);
            contextInfo.fscbk.cbkgen.pfnGetParaProperties = new PTS.GetParaProperties(ptsHost.GetParaProperties);
            contextInfo.fscbk.cbkgen.pfnCreateParaclient = new PTS.CreateParaclient(ptsHost.CreateParaclient);
            contextInfo.fscbk.cbkgen.pfnTransferDisplayInfo = new PTS.TransferDisplayInfo(ptsHost.TransferDisplayInfo);
            contextInfo.fscbk.cbkgen.pfnDestroyParaclient = new PTS.DestroyParaclient(ptsHost.DestroyParaclient);
            contextInfo.fscbk.cbkgen.pfnFInterruptFormattingAfterPara = new PTS.FInterruptFormattingAfterPara(ptsHost.FInterruptFormattingAfterPara);
            contextInfo.fscbk.cbkgen.pfnGetEndnoteSeparators = new PTS.GetEndnoteSeparators(ptsHost.GetEndnoteSeparators);
            contextInfo.fscbk.cbkgen.pfnGetEndnoteSegment = new PTS.GetEndnoteSegment(ptsHost.GetEndnoteSegment);
            contextInfo.fscbk.cbkgen.pfnGetNumberEndnoteColumns = new PTS.GetNumberEndnoteColumns(ptsHost.GetNumberEndnoteColumns);
            contextInfo.fscbk.cbkgen.pfnGetEndnoteColumnInfo = new PTS.GetEndnoteColumnInfo(ptsHost.GetEndnoteColumnInfo);
            contextInfo.fscbk.cbkgen.pfnGetFootnoteSeparators = new PTS.GetFootnoteSeparators(ptsHost.GetFootnoteSeparators);
            contextInfo.fscbk.cbkgen.pfnFFootnoteBeneathText = new PTS.FFootnoteBeneathText(ptsHost.FFootnoteBeneathText);
            contextInfo.fscbk.cbkgen.pfnGetNumberFootnoteColumns = new PTS.GetNumberFootnoteColumns(ptsHost.GetNumberFootnoteColumns);
            contextInfo.fscbk.cbkgen.pfnGetFootnoteColumnInfo = new PTS.GetFootnoteColumnInfo(ptsHost.GetFootnoteColumnInfo);
            contextInfo.fscbk.cbkgen.pfnGetFootnoteSegment = new PTS.GetFootnoteSegment(ptsHost.GetFootnoteSegment);
            contextInfo.fscbk.cbkgen.pfnGetFootnotePresentationAndRejectionOrder = new PTS.GetFootnotePresentationAndRejectionOrder(ptsHost.GetFootnotePresentationAndRejectionOrder);
            contextInfo.fscbk.cbkgen.pfnFAllowFootnoteSeparation = new PTS.FAllowFootnoteSeparation(ptsHost.FAllowFootnoteSeparation);
            //Initialize object callbacks
            //contextInfo.fscbk.cbkobj.pfnNewPtr                Handled by PTSWrapper
            //contextInfo.fscbk.cbkobj.pfnDisposePtr            Handled by PTSWrapper
            //contextInfo.fscbk.cbkobj.pfnReallocPtr            Handled by PTSWrapper
            contextInfo.fscbk.cbkobj.pfnDuplicateMcsclient = new PTS.DuplicateMcsclient(ptsHost.DuplicateMcsclient);
            contextInfo.fscbk.cbkobj.pfnDestroyMcsclient = new PTS.DestroyMcsclient(ptsHost.DestroyMcsclient);
            contextInfo.fscbk.cbkobj.pfnFEqualMcsclient = new PTS.FEqualMcsclient(ptsHost.FEqualMcsclient);
            contextInfo.fscbk.cbkobj.pfnConvertMcsclient = new PTS.ConvertMcsclient(ptsHost.ConvertMcsclient);
            contextInfo.fscbk.cbkobj.pfnGetObjectHandlerInfo = new PTS.GetObjectHandlerInfo(ptsHost.GetObjectHandlerInfo);
            // Initialize text callbacks
            contextInfo.fscbk.cbktxt.pfnCreateParaBreakingSession = new PTS.CreateParaBreakingSession(ptsHost.CreateParaBreakingSession);
            contextInfo.fscbk.cbktxt.pfnDestroyParaBreakingSession = new PTS.DestroyParaBreakingSession(ptsHost.DestroyParaBreakingSession);
            contextInfo.fscbk.cbktxt.pfnGetTextProperties = new PTS.GetTextProperties(ptsHost.GetTextProperties);
            contextInfo.fscbk.cbktxt.pfnGetNumberFootnotes = new PTS.GetNumberFootnotes(ptsHost.GetNumberFootnotes);
            contextInfo.fscbk.cbktxt.pfnGetFootnotes = new PTS.GetFootnotes(ptsHost.GetFootnotes);
            contextInfo.fscbk.cbktxt.pfnFormatDropCap = new PTS.FormatDropCap(ptsHost.FormatDropCap);
            contextInfo.fscbk.cbktxt.pfnGetDropCapPolygons = new PTS.GetDropCapPolygons(ptsHost.GetDropCapPolygons);
            contextInfo.fscbk.cbktxt.pfnDestroyDropCap = new PTS.DestroyDropCap(ptsHost.DestroyDropCap);
            contextInfo.fscbk.cbktxt.pfnFormatBottomText = new PTS.FormatBottomText(ptsHost.FormatBottomText);
            contextInfo.fscbk.cbktxt.pfnFormatLine = new PTS.FormatLine(ptsHost.FormatLine);
            contextInfo.fscbk.cbktxt.pfnFormatLineForced = new PTS.FormatLineForced(ptsHost.FormatLineForced);
            contextInfo.fscbk.cbktxt.pfnFormatLineVariants = new PTS.FormatLineVariants(ptsHost.FormatLineVariants);
            contextInfo.fscbk.cbktxt.pfnReconstructLineVariant = new PTS.ReconstructLineVariant(ptsHost.ReconstructLineVariant);
            contextInfo.fscbk.cbktxt.pfnDestroyLine = new PTS.DestroyLine(ptsHost.DestroyLine);
            contextInfo.fscbk.cbktxt.pfnDuplicateLineBreakRecord = new PTS.DuplicateLineBreakRecord(ptsHost.DuplicateLineBreakRecord);
            contextInfo.fscbk.cbktxt.pfnDestroyLineBreakRecord = new PTS.DestroyLineBreakRecord(ptsHost.DestroyLineBreakRecord);
            contextInfo.fscbk.cbktxt.pfnSnapGridVertical = new PTS.SnapGridVertical(ptsHost.SnapGridVertical);
            contextInfo.fscbk.cbktxt.pfnGetDvrSuppressibleBottomSpace = new PTS.GetDvrSuppressibleBottomSpace(ptsHost.GetDvrSuppressibleBottomSpace);
            contextInfo.fscbk.cbktxt.pfnGetDvrAdvance = new PTS.GetDvrAdvance(ptsHost.GetDvrAdvance);
            contextInfo.fscbk.cbktxt.pfnUpdGetChangeInText = new PTS.UpdGetChangeInText(ptsHost.UpdGetChangeInText);
            contextInfo.fscbk.cbktxt.pfnUpdGetDropCapChange = new PTS.UpdGetDropCapChange(ptsHost.UpdGetDropCapChange);
            contextInfo.fscbk.cbktxt.pfnFInterruptFormattingText = new PTS.FInterruptFormattingText(ptsHost.FInterruptFormattingText);
            contextInfo.fscbk.cbktxt.pfnGetTextParaCache = new PTS.GetTextParaCache(ptsHost.GetTextParaCache);
            contextInfo.fscbk.cbktxt.pfnSetTextParaCache = new PTS.SetTextParaCache(ptsHost.SetTextParaCache);
            contextInfo.fscbk.cbktxt.pfnGetOptimalLineDcpCache = new PTS.GetOptimalLineDcpCache(ptsHost.GetOptimalLineDcpCache);
            contextInfo.fscbk.cbktxt.pfnGetNumberAttachedObjectsBeforeTextLine = new PTS.GetNumberAttachedObjectsBeforeTextLine(ptsHost.GetNumberAttachedObjectsBeforeTextLine);
            contextInfo.fscbk.cbktxt.pfnGetAttachedObjectsBeforeTextLine = new PTS.GetAttachedObjectsBeforeTextLine(ptsHost.GetAttachedObjectsBeforeTextLine);
            contextInfo.fscbk.cbktxt.pfnGetNumberAttachedObjectsInTextLine = new PTS.GetNumberAttachedObjectsInTextLine(ptsHost.GetNumberAttachedObjectsInTextLine);
            contextInfo.fscbk.cbktxt.pfnGetAttachedObjectsInTextLine = new PTS.GetAttachedObjectsInTextLine(ptsHost.GetAttachedObjectsInTextLine);
            contextInfo.fscbk.cbktxt.pfnUpdGetAttachedObjectChange = new PTS.UpdGetAttachedObjectChange(ptsHost.UpdGetAttachedObjectChange);
            contextInfo.fscbk.cbktxt.pfnGetDurFigureAnchor = new PTS.GetDurFigureAnchor(ptsHost.GetDurFigureAnchor);
        }

        /// <summary>
        /// Initializes formatting callbacks for PTS Installed objects.
        /// </summary>
        /// <param name="ptsHost">PtsHost that defines all PTS callbacks.</param>
        /// <param name="subtrackParaInfo">Subtrack formatting callbacks.</param>
        /// <param name="subpageParaInfo">Subpage formatting callbacks.</param>
        /// <param name="installedObjects">PTS Installed objects.</param>
        /// <param name="installedObjectsCount">Count of PTS Installed objects.</param>
        private unsafe void InitInstalledObjectsInfo(PtsHost ptsHost, ref PTS.FSIMETHODS subtrackParaInfo, ref PTS.FSIMETHODS subpageParaInfo, out IntPtr installedObjects, out int installedObjectsCount)
        {
            // Initialize subtrack para info
            subtrackParaInfo.pfnCreateContext = new PTS.ObjCreateContext(ptsHost.SubtrackCreateContext);
            subtrackParaInfo.pfnDestroyContext = new PTS.ObjDestroyContext(ptsHost.SubtrackDestroyContext);
            subtrackParaInfo.pfnFormatParaFinite = new PTS.ObjFormatParaFinite(ptsHost.SubtrackFormatParaFinite);
            subtrackParaInfo.pfnFormatParaBottomless = new PTS.ObjFormatParaBottomless(ptsHost.SubtrackFormatParaBottomless);
            subtrackParaInfo.pfnUpdateBottomlessPara = new PTS.ObjUpdateBottomlessPara(ptsHost.SubtrackUpdateBottomlessPara);
            subtrackParaInfo.pfnSynchronizeBottomlessPara = new PTS.ObjSynchronizeBottomlessPara(ptsHost.SubtrackSynchronizeBottomlessPara);
            subtrackParaInfo.pfnComparePara = new PTS.ObjComparePara(ptsHost.SubtrackComparePara);
            subtrackParaInfo.pfnClearUpdateInfoInPara = new PTS.ObjClearUpdateInfoInPara(ptsHost.SubtrackClearUpdateInfoInPara);
            subtrackParaInfo.pfnDestroyPara = new PTS.ObjDestroyPara(ptsHost.SubtrackDestroyPara);
            subtrackParaInfo.pfnDuplicateBreakRecord = new PTS.ObjDuplicateBreakRecord(ptsHost.SubtrackDuplicateBreakRecord);
            subtrackParaInfo.pfnDestroyBreakRecord = new PTS.ObjDestroyBreakRecord(ptsHost.SubtrackDestroyBreakRecord);
            subtrackParaInfo.pfnGetColumnBalancingInfo = new PTS.ObjGetColumnBalancingInfo(ptsHost.SubtrackGetColumnBalancingInfo);
            subtrackParaInfo.pfnGetNumberFootnotes = new PTS.ObjGetNumberFootnotes(ptsHost.SubtrackGetNumberFootnotes);
            subtrackParaInfo.pfnGetFootnoteInfo = new PTS.ObjGetFootnoteInfo(ptsHost.SubtrackGetFootnoteInfo);
            subtrackParaInfo.pfnGetFootnoteInfoWord = IntPtr.Zero;
            subtrackParaInfo.pfnShiftVertical = new PTS.ObjShiftVertical(ptsHost.SubtrackShiftVertical);
            subtrackParaInfo.pfnTransferDisplayInfoPara = new PTS.ObjTransferDisplayInfoPara(ptsHost.SubtrackTransferDisplayInfoPara);

            // Initialize subpage para info
            subpageParaInfo.pfnCreateContext = new PTS.ObjCreateContext(ptsHost.SubpageCreateContext);
            subpageParaInfo.pfnDestroyContext = new PTS.ObjDestroyContext(ptsHost.SubpageDestroyContext);
            subpageParaInfo.pfnFormatParaFinite = new PTS.ObjFormatParaFinite(ptsHost.SubpageFormatParaFinite);
            subpageParaInfo.pfnFormatParaBottomless = new PTS.ObjFormatParaBottomless(ptsHost.SubpageFormatParaBottomless);
            subpageParaInfo.pfnUpdateBottomlessPara = new PTS.ObjUpdateBottomlessPara(ptsHost.SubpageUpdateBottomlessPara);
            subpageParaInfo.pfnSynchronizeBottomlessPara = new PTS.ObjSynchronizeBottomlessPara(ptsHost.SubpageSynchronizeBottomlessPara);
            subpageParaInfo.pfnComparePara = new PTS.ObjComparePara(ptsHost.SubpageComparePara);
            subpageParaInfo.pfnClearUpdateInfoInPara = new PTS.ObjClearUpdateInfoInPara(ptsHost.SubpageClearUpdateInfoInPara);
            subpageParaInfo.pfnDestroyPara = new PTS.ObjDestroyPara(ptsHost.SubpageDestroyPara);
            subpageParaInfo.pfnDuplicateBreakRecord = new PTS.ObjDuplicateBreakRecord(ptsHost.SubpageDuplicateBreakRecord);
            subpageParaInfo.pfnDestroyBreakRecord = new PTS.ObjDestroyBreakRecord(ptsHost.SubpageDestroyBreakRecord);
            subpageParaInfo.pfnGetColumnBalancingInfo = new PTS.ObjGetColumnBalancingInfo(ptsHost.SubpageGetColumnBalancingInfo);
            subpageParaInfo.pfnGetNumberFootnotes = new PTS.ObjGetNumberFootnotes(ptsHost.SubpageGetNumberFootnotes);
            subpageParaInfo.pfnGetFootnoteInfo = new PTS.ObjGetFootnoteInfo(ptsHost.SubpageGetFootnoteInfo);
            subpageParaInfo.pfnShiftVertical = new PTS.ObjShiftVertical(ptsHost.SubpageShiftVertical);
            subpageParaInfo.pfnTransferDisplayInfoPara = new PTS.ObjTransferDisplayInfoPara(ptsHost.SubpageTransferDisplayInfoPara);

            // Create installed objects info
            PTS.Validate(PTS.CreateInstalledObjectsInfo(ref subtrackParaInfo, ref subpageParaInfo, out installedObjects, out installedObjectsCount));
        }

        /// <summary>
        /// Initializes floater formatting callbacks.
        /// </summary>
        /// <param name="ptsHost">PtsHost that defines all PTS callbacks.</param>
        /// <param name="floaterInit">Floater formatting callbacks.</param>
        private unsafe void InitFloaterObjInfo(PtsHost ptsHost, ref PTS.FSFLOATERINIT floaterInit)
        {
            floaterInit.fsfloatercbk.pfnGetFloaterProperties = new PTS.GetFloaterProperties(ptsHost.GetFloaterProperties);
            floaterInit.fsfloatercbk.pfnFormatFloaterContentFinite = new PTS.FormatFloaterContentFinite(ptsHost.FormatFloaterContentFinite);
            floaterInit.fsfloatercbk.pfnFormatFloaterContentBottomless = new PTS.FormatFloaterContentBottomless(ptsHost.FormatFloaterContentBottomless);
            floaterInit.fsfloatercbk.pfnUpdateBottomlessFloaterContent = new PTS.UpdateBottomlessFloaterContent(ptsHost.UpdateBottomlessFloaterContent);
            floaterInit.fsfloatercbk.pfnGetFloaterPolygons = new PTS.GetFloaterPolygons(ptsHost.GetFloaterPolygons);
            floaterInit.fsfloatercbk.pfnClearUpdateInfoInFloaterContent = new PTS.ClearUpdateInfoInFloaterContent(ptsHost.ClearUpdateInfoInFloaterContent);
            floaterInit.fsfloatercbk.pfnCompareFloaterContents = new PTS.CompareFloaterContents(ptsHost.CompareFloaterContents);
            floaterInit.fsfloatercbk.pfnDestroyFloaterContent = new PTS.DestroyFloaterContent(ptsHost.DestroyFloaterContent);
            floaterInit.fsfloatercbk.pfnDuplicateFloaterContentBreakRecord = new PTS.DuplicateFloaterContentBreakRecord(ptsHost.DuplicateFloaterContentBreakRecord);
            floaterInit.fsfloatercbk.pfnDestroyFloaterContentBreakRecord = new PTS.DestroyFloaterContentBreakRecord(ptsHost.DestroyFloaterContentBreakRecord);
            floaterInit.fsfloatercbk.pfnGetFloaterContentColumnBalancingInfo = new PTS.GetFloaterContentColumnBalancingInfo(ptsHost.GetFloaterContentColumnBalancingInfo);
            floaterInit.fsfloatercbk.pfnGetFloaterContentNumberFootnotes = new PTS.GetFloaterContentNumberFootnotes(ptsHost.GetFloaterContentNumberFootnotes);
            floaterInit.fsfloatercbk.pfnGetFloaterContentFootnoteInfo = new PTS.GetFloaterContentFootnoteInfo(ptsHost.GetFloaterContentFootnoteInfo);
            floaterInit.fsfloatercbk.pfnTransferDisplayInfoInFloaterContent = new PTS.TransferDisplayInfoInFloaterContent(ptsHost.TransferDisplayInfoInFloaterContent);
            floaterInit.fsfloatercbk.pfnGetMCSClientAfterFloater = new PTS.GetMCSClientAfterFloater(ptsHost.GetMCSClientAfterFloater);
            floaterInit.fsfloatercbk.pfnGetDvrUsedForFloater = new PTS.GetDvrUsedForFloater(ptsHost.GetDvrUsedForFloater);
        }

        /// <summary>
        /// Initializes table formatting callbacks.
        /// </summary>
        /// <param name="ptsHost">PtsHost that defines all PTS callbacks.</param>
        /// <param name="tableobjInit">Table formatting callbacks.</param>
        private unsafe void InitTableObjInfo(PtsHost ptsHost, ref PTS.FSTABLEOBJINIT tableobjInit)
        {
            // FSTABLEOBJCBK
            tableobjInit.tableobjcbk.pfnGetTableProperties = new PTS.GetTableProperties(ptsHost.GetTableProperties);
            tableobjInit.tableobjcbk.pfnAutofitTable = new PTS.AutofitTable(ptsHost.AutofitTable);
            tableobjInit.tableobjcbk.pfnUpdAutofitTable = new PTS.UpdAutofitTable(ptsHost.UpdAutofitTable);
            tableobjInit.tableobjcbk.pfnGetMCSClientAfterTable = new PTS.GetMCSClientAfterTable(ptsHost.GetMCSClientAfterTable);
            tableobjInit.tableobjcbk.pfnGetDvrUsedForFloatTable = IntPtr.Zero;

            // FSTABLECBKFETCH
            tableobjInit.tablecbkfetch.pfnGetFirstHeaderRow = new PTS.GetFirstHeaderRow(ptsHost.GetFirstHeaderRow);
            tableobjInit.tablecbkfetch.pfnGetNextHeaderRow = new PTS.GetNextHeaderRow(ptsHost.GetNextHeaderRow);
            tableobjInit.tablecbkfetch.pfnGetFirstFooterRow = new PTS.GetFirstFooterRow(ptsHost.GetFirstFooterRow);
            tableobjInit.tablecbkfetch.pfnGetNextFooterRow = new PTS.GetNextFooterRow(ptsHost.GetNextFooterRow);
            tableobjInit.tablecbkfetch.pfnGetFirstRow = new PTS.GetFirstRow(ptsHost.GetFirstRow);
            tableobjInit.tablecbkfetch.pfnGetNextRow = new PTS.GetNextRow(ptsHost.GetNextRow);
            tableobjInit.tablecbkfetch.pfnUpdFChangeInHeaderFooter = new PTS.UpdFChangeInHeaderFooter(ptsHost.UpdFChangeInHeaderFooter);
            tableobjInit.tablecbkfetch.pfnUpdGetFirstChangeInTable = new PTS.UpdGetFirstChangeInTable(ptsHost.UpdGetFirstChangeInTable);
            tableobjInit.tablecbkfetch.pfnUpdGetRowChange = new PTS.UpdGetRowChange(ptsHost.UpdGetRowChange);
            tableobjInit.tablecbkfetch.pfnUpdGetCellChange = new PTS.UpdGetCellChange(ptsHost.UpdGetCellChange);
            tableobjInit.tablecbkfetch.pfnGetDistributionKind = new PTS.GetDistributionKind(ptsHost.GetDistributionKind);
            tableobjInit.tablecbkfetch.pfnGetRowProperties = new PTS.GetRowProperties(ptsHost.GetRowProperties);
            tableobjInit.tablecbkfetch.pfnGetCells = new PTS.GetCells(ptsHost.GetCells);
            tableobjInit.tablecbkfetch.pfnFInterruptFormattingTable = new PTS.FInterruptFormattingTable(ptsHost.FInterruptFormattingTable);
            tableobjInit.tablecbkfetch.pfnCalcHorizontalBBoxOfRow = new PTS.CalcHorizontalBBoxOfRow(ptsHost.CalcHorizontalBBoxOfRow);

            // FSTABLECBKCELL
            tableobjInit.tablecbkcell.pfnFormatCellFinite = new PTS.FormatCellFinite(ptsHost.FormatCellFinite);
            tableobjInit.tablecbkcell.pfnFormatCellBottomless = new PTS.FormatCellBottomless(ptsHost.FormatCellBottomless);
            tableobjInit.tablecbkcell.pfnUpdateBottomlessCell = new PTS.UpdateBottomlessCell(ptsHost.UpdateBottomlessCell);
            tableobjInit.tablecbkcell.pfnCompareCells = new PTS.CompareCells(ptsHost.CompareCells);
            tableobjInit.tablecbkcell.pfnClearUpdateInfoInCell = new PTS.ClearUpdateInfoInCell(ptsHost.ClearUpdateInfoInCell);
            tableobjInit.tablecbkcell.pfnSetCellHeight = new PTS.SetCellHeight(ptsHost.SetCellHeight);
            tableobjInit.tablecbkcell.pfnDestroyCell = new PTS.DestroyCell(ptsHost.DestroyCell);
            tableobjInit.tablecbkcell.pfnDuplicateCellBreakRecord = new PTS.DuplicateCellBreakRecord(ptsHost.DuplicateCellBreakRecord);
            tableobjInit.tablecbkcell.pfnDestroyCellBreakRecord = new PTS.DestroyCellBreakRecord(ptsHost.DestroyCellBreakRecord);
            tableobjInit.tablecbkcell.pfnGetCellNumberFootnotes = new PTS.GetCellNumberFootnotes(ptsHost.GetCellNumberFootnotes);
            tableobjInit.tablecbkcell.pfnGetCellFootnoteInfo = IntPtr.Zero;
            tableobjInit.tablecbkcell.pfnGetCellFootnoteInfoWord = IntPtr.Zero;
            tableobjInit.tablecbkcell.pfnGetCellMinColumnBalancingStep = new PTS.GetCellMinColumnBalancingStep(ptsHost.GetCellMinColumnBalancingStep);
            tableobjInit.tablecbkcell.pfnTransferDisplayInfoCell = new PTS.TransferDisplayInfoCell(ptsHost.TransferDisplayInfoCell);

            /*
            // FSTABLECBKFETCHWORD -- lepfnDuplicateCellBreakRecord;gacy =)
            tableobjInit.tablecbkfetchword.pfnGetTablePropertiesWord  = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnGetRowPropertiesWord    = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnGetNumberFiguresForTableRow = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnGetRowWidthWord = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnGetFiguresForTableRow   = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnFStopBeforeTableRowLr   = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnFIgnoreCollisionForTableRow = IntPtr.Zero;
            tableobjInit.tablecbkfetchword.pfnChangeRowHeightRestriction = IntPtr.Zero;
            */
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// For each Dispatcher separate PTS context pool is created.
        /// This enables usage from different Dispatchers at the same time.
        /// When Dispatcher is disposed, all associated resources stored in its
        /// PTS context pool are disposed as well.
        /// </summary>
        /// <remarks>
        /// PTS context pool is an array of PTS Context descriptors.
        /// PTS Context is very expensive to create, so once it is created, it
        /// is stored and might be reused. Particular PTS Context is in use,
        /// when WeakReference points to actual object. Otherwise it is free.
        /// </remarks>
        private List<ContextDesc> _contextPool;

        /// <summary>
        /// Collection of PtsContext ready for disposal.
        /// </summary>
        private List<PtsContext> _releaseQueue;

        /// <summary>
        /// Lock.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// Whether object is already disposed.
        /// </summary>
        private int _disposed;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        /// <summary>
        /// Single item in PTS Context array. It stores created PTS Context
        /// together with its current owner.
        /// PTS Context is in use, when Owner points to actual object.
        /// Otherwise it is free and may be reused.
        /// Created PTS Context is not destroyed, because creation process is
        /// expensive.
        /// </summary>
        private class ContextDesc
        {
            internal PtsHost PtsHost;
            internal PTS.FSCONTEXTINFO ContextInfo;
            internal PTS.FSIMETHODS SubtrackParaInfo;
            internal PTS.FSIMETHODS SubpageParaInfo;
            internal PTS.FSFLOATERINIT FloaterInit;
            internal PTS.FSTABLEOBJINIT TableobjInit;
            internal IntPtr InstalledObjects;
            internal TextFormatter TextFormatter;
            internal TextPenaltyModule TextPenaltyModule;
            internal bool IsOptimalParagraphEnabled;
            internal WeakReference Owner;
            internal bool InUse;
        }

        private sealed class PtsCacheShutDownListener : ShutDownListener
        {
            public PtsCacheShutDownListener(PtsCache target) : base(target)
            {
            }

            internal override void OnShutDown(object target, object sender, EventArgs e)
            {
                PtsCache ptsCache = (PtsCache)target;
                ptsCache.Shutdown();
            }
        }

        #endregion Private Types
    }
}
