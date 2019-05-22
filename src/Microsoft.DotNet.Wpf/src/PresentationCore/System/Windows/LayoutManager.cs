// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
*  Defines a top-level ContextLayoutManager - a layout dirtiness tracking/clearing system.
*
*
\***************************************************************************/

using System;
using System.Windows.Threading;
using System.Collections;

using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Automation.Peers;

using MS.Internal;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    /// Top-level ContextLayoutManager object. Manages the layout update and layout dirty state.
    /// </summary>
    internal sealed class ContextLayoutManager : DispatcherObject
    {
        internal ContextLayoutManager()
        {
            _shutdownHandler = new EventHandler(this.OnDispatcherShutdown);
            Dispatcher.ShutdownFinished += _shutdownHandler;
        }

        void OnDispatcherShutdown(object sender, EventArgs e)
        {
            if(_shutdownHandler != null)
                Dispatcher.ShutdownFinished -= _shutdownHandler;

            _shutdownHandler = null;
            _layoutEvents = null;
            _measureQueue = null;
            _arrangeQueue = null;
            _sizeChangedChain = null;
            _isDead = true;
        }


        /// <summary>
        /// The way to obtain ContextLayoutManager associated with particular Dispatcher.
        /// </summary>
        /// <param name="dispatcher">A dispatcher for which ContextLayoutManager is queried.
        /// There is only one ContextLayoutManager associuated with all elements in a single context</param>
        /// <returns>ContextLayoutManager</returns>
        internal static ContextLayoutManager From(Dispatcher dispatcher)
        {
            ContextLayoutManager lm = dispatcher.Reserved3 as ContextLayoutManager;
            if(lm == null)
            {
                if(Dispatcher.CurrentDispatcher != dispatcher)
                {
                    throw new InvalidOperationException();
                }

                lm = new ContextLayoutManager();
                dispatcher.Reserved3 = lm;
            }
            return lm;
        }

        private void setForceLayout(UIElement e)
        {
            _forceLayoutElement = e;
        }

        private void markTreeDirty(UIElement e)
        {
            //walk up until we are the topmost UIElement in the tree.
            while(true)
            {
                UIElement p = e.GetUIParentNo3DTraversal() as UIElement;
                if(p == null) break;
                e = p;
            }

            markTreeDirtyHelper(e);
            MeasureQueue.Add(e);
            ArrangeQueue.Add(e);
        }

        private void markTreeDirtyHelper(Visual v)
        {
            //now walk down and mark all UIElements dirty
            if(v != null)
            {
                if(v.CheckFlagsAnd(VisualFlags.IsUIElement))
                {
                    UIElement uie = ((UIElement)v);
                    uie.InvalidateMeasureInternal();
                    uie.InvalidateArrangeInternal();
                }

                //walk children doing the same, don't stop if they are already dirty since there can
                //be insulated dirty islands below
                int cnt = v.InternalVisualChildrenCount;

                for(int i=0; i<cnt; i++)
                {
                    Visual child = v.InternalGetVisualChild(i);
                    if (child != null) markTreeDirtyHelper(child);
                }
            }
        }

        // posts a layout update
        private void NeedsRecalc()
        {
            if(!_layoutRequestPosted && !_isUpdating)
            {
                MediaContext.From(Dispatcher).BeginInvokeOnRender(_updateCallback, this);
                _layoutRequestPosted = true;
            }
        }

        private static object UpdateLayoutBackground(object arg)
        {
            ((ContextLayoutManager)arg).NeedsRecalc();
            return null;
        }

        private bool hasDirtiness
        {
            get
            {
                return (!MeasureQueue.IsEmpty) || (!ArrangeQueue.IsEmpty);
            }
        }

        internal void EnterMeasure()
        {
            Dispatcher._disableProcessingCount++;
            _lastExceptionElement = null;
            _measuresOnStack++;
            if(_measuresOnStack > s_LayoutRecursionLimit)
                throw new InvalidOperationException(SR.Get(SRID.LayoutManager_DeepRecursion, s_LayoutRecursionLimit));

            _firePostLayoutEvents = true;
        }

        internal void ExitMeasure()
        {
            _measuresOnStack--;
            Dispatcher._disableProcessingCount--;
        }

        internal void EnterArrange()
        {
            Dispatcher._disableProcessingCount++;
            _lastExceptionElement = null;
            _arrangesOnStack++;
            if(_arrangesOnStack > s_LayoutRecursionLimit)
                throw new InvalidOperationException(SR.Get(SRID.LayoutManager_DeepRecursion, s_LayoutRecursionLimit));

            _firePostLayoutEvents = true;
        }

        internal void ExitArrange()
        {
            _arrangesOnStack--;
            Dispatcher._disableProcessingCount--;
        }

        /// <summary>
        /// Tells ContextLayoutManager to finalize possibly async update.
        /// Used before accessing services off Visual.
        /// </summary>
        internal void UpdateLayout()
        {
            VerifyAccess();

            //make UpdateLayout to be a NOP if called during UpdateLayout.
            if (   _isInUpdateLayout
                || _measuresOnStack > 0
                || _arrangesOnStack > 0
                || _isDead) return;

#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;

            // Start over with the Measure and arrange counters for this layout pass
            int measureCLRPass = 0;
            int arrangeCLRPass = 0;

            if (CLRProfilerControl.ProcessIsUnderCLRProfiler)
            {
                clrTracingEnabled = true;
                if (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance)
                {
                    ++_layoutCLRPass;
                    CLRProfilerControl.CLRLogWriteLine("Begin_Layout_{0}", _layoutCLRPass);
                }
            }
#endif // DEBUG_CLR_MEM

            bool etwTracingEnabled = false;
            long perfElementID = 0;
            const EventTrace.Keyword etwKeywords = EventTrace.Keyword.KeywordLayout | EventTrace.Keyword.KeywordPerf;
            if (!_isUpdating && EventTrace.IsEnabled(etwKeywords, EventTrace.Level.Info))
            {
                etwTracingEnabled = true;
                perfElementID = PerfService.GetPerfElementID(this);
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutBegin, etwKeywords, EventTrace.Level.Info,
                        perfElementID, EventTrace.LayoutSource.LayoutManager);
            }

            int cnt = 0;
            bool gotException = true;
            UIElement currentElement = null;

            try
            {
                invalidateTreeIfRecovering();


                while(hasDirtiness || _firePostLayoutEvents)
                {
                    if(++cnt > 153)
                    {
                        //loop detected. Lets go over to background to let input/user to correct the situation.
                        //most frequently, we get such a loop as a result of input detecting a mouse in the "bad spot"
                        //and some event handler oscillating a layout-affecting property depending on hittest result
                        //of the mouse. Going over to background will not break the loopp but will allow user to
                        //move the mouse so that it goes out of the "bad spot".
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, _updateLayoutBackground, this);
                        currentElement = null;
                        gotException = false;
                        if (etwTracingEnabled)
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutAbort, etwKeywords, EventTrace.Level.Info, 0, cnt);
                        }
                        return;
                    }


                    //this flag stops posting update requests to MediaContext - we are already in one
                    //note that _isInUpdateLayout is close but different - _isInUpdateLayout is reset
                    //before firing LayoutUpdated so that event handlers could call UpdateLayout but
                    //still could not cause posting of MediaContext work item. Posting MediaContext workitem
                    //causes infinite loop in MediaContext.
                    _isUpdating = true;
                    _isInUpdateLayout = true;

#if DEBUG_CLR_MEM
                    if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Verbose))
                    {
                        ++measureCLRPass;
                        CLRProfilerControl.CLRLogWriteLine("Begin_Measure_{0}_{1}", _layoutCLRPass, measureCLRPass);
                    }
#endif // DEBUG_CLR_MEM

                    if (etwTracingEnabled)
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureBegin, etwKeywords, EventTrace.Level.Info, perfElementID);
                    }

                    // Disable processing of the queue during blocking operations to prevent unrelated reentrancy.
                    using(Dispatcher.DisableProcessing())
                    {
                        //loop for Measure
                        //We limit the number of loops here by time - normally, all layout
                        //calculations should be done by this time, this limit is here for
                        //emergency, "infinite loop" scenarios - yielding in this case will
                        //provide user with ability to continue to interact with the app, even though
                        //it will be sluggish. If we don't yield here, the loop is goign to be a deadly one
                        //and it will be impossible to save results or even close the window.
                        int loopCounter = 0;
                        DateTime loopStartTime = new DateTime(0);
                        while(true)
                        {
                            if(++loopCounter > 153)
                            {
                                loopCounter = 0;
                                //first bunch of iterations is free, then we start count time
                                //this way, we don't call DateTime.Now in most layout updates
                                if(loopStartTime.Ticks == 0)
                                {
                                    loopStartTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    TimeSpan loopDuration = (DateTime.UtcNow - loopStartTime);
                                    if(loopDuration.Milliseconds > 153*2) // 153*2 = magic*science
                                    {
                                        //loop detected. Lets go over to background to let input work.
                                        Dispatcher.BeginInvoke(DispatcherPriority.Background, _updateLayoutBackground, this);
                                        currentElement = null;
                                        gotException = false;
                                        if (etwTracingEnabled)
                                        {
                                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureAbort, etwKeywords, EventTrace.Level.Info,
                                                   loopDuration.Milliseconds, loopCounter);
                                        }
                                        return;
                                    }
                                }
                            }

                            currentElement = MeasureQueue.GetTopMost();

                            if(currentElement == null) break; //exit if no more Measure candidates
                            
                            currentElement.Measure(currentElement.PreviousConstraint);
							//not clear why this is needed, remove for now
							//if the parent was just computed, the chidlren should be clean. If they are not clean and in the queue
							//that means that there is cross-tree dependency and they most likely shodul be updated by themselves.
							//                            MeasureQueue.RemoveOrphans(currentElement);
                        }

                        if (etwTracingEnabled)
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientMeasureEnd, etwKeywords, EventTrace.Level.Info, loopCounter);
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeBegin, etwKeywords, EventTrace.Level.Info, perfElementID);
                        }


#if DEBUG_CLR_MEM
                        if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Verbose))
                        {
                            CLRProfilerControl.CLRLogWriteLine("End_Measure_{0}_{1}", _layoutCLRPass, measureCLRPass);
                            ++arrangeCLRPass;
                            CLRProfilerControl.CLRLogWriteLine("Begin_Arrange_{0}_{1}", _layoutCLRPass, arrangeCLRPass);
                        }
#endif // DEBUG_CLR_MEM

                        //loop for Arrange
                        //if Arrange dirtied the tree go clean it again

                        //We limit the number of loops here by time - normally, all layout
                        //calculations should be done by this time, this limit is here for
                        //emergency, "infinite loop" scenarios - yielding in this case will
                        //provide user with ability to continue to interact with the app, even though
                        //it will be sluggish. If we don't yield here, the loop is goign to be a deadly one
                        //and it will be impossible to save results or even close the window.
                        loopCounter = 0;
                        loopStartTime = new DateTime(0);
                        while(MeasureQueue.IsEmpty)
                        {
                            if(++loopCounter > 153)
                            {
                                loopCounter = 0;
                                //first bunch of iterations is free, then we start count time
                                //this way, we don't call DateTime.Now in most layout updates
                                if(loopStartTime.Ticks == 0)
                                {
                                    loopStartTime = DateTime.UtcNow;
                                }
                                else
                                {
                                    TimeSpan loopDuration = (DateTime.UtcNow - loopStartTime);
                                    if(loopDuration.Milliseconds > 153*2) // 153*2 = magic*science
                                    {
                                        //loop detected. Lets go over to background to let input work.
                                        Dispatcher.BeginInvoke(DispatcherPriority.Background, _updateLayoutBackground, this);
                                        currentElement = null;
                                        gotException = false;
                                        if (etwTracingEnabled)
                                        {
                                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeAbort, etwKeywords, EventTrace.Level.Info,
                                                   loopDuration.Milliseconds, loopCounter);
                                        }
                                        return;
                                    }
                                }
                            }

                            currentElement = ArrangeQueue.GetTopMost();

                            if(currentElement == null) break; //exit if no more Measure candidates

                            Rect finalRect = getProperArrangeRect(currentElement);

                            currentElement.Arrange(finalRect);
							//not clear why this is needed, remove for now
							//if the parent was just computed, the chidlren should be clean. If they are not clean and in the queue
							//that means that there is cross-tree dependency and they most likely shodul be updated by themselves.
							//                            ArrangeQueue.RemoveOrphans(currentElement);
                        }

                        if (etwTracingEnabled)
                        {
                            EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientArrangeEnd, etwKeywords, EventTrace.Level.Info, loopCounter);
                        }

#if DEBUG_CLR_MEM
                        if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Verbose))
                        {
                            CLRProfilerControl.CLRLogWriteLine("End_Arrange_{0}_{1}", _layoutCLRPass, arrangeCLRPass);
                        }
#endif // DEBUG_CLR_MEM

                        //if Arrange dirtied the tree go clean it again
                        //it is not neccesary to check ArrangeQueue sicnce we just exited from Arrange loop
                        if(!MeasureQueue.IsEmpty) continue;

                        //let LayoutUpdated handlers to call UpdateLayout
                        //note that it means we can get reentrancy into UpdateLayout past this point,
                        //if any of event handlers call UpdateLayout sync. Need to protect from reentrancy
                        //in the firing methods below.
                        _isInUpdateLayout = false;
}

                    fireSizeChangedEvents();
                    if(hasDirtiness) continue;
                    fireLayoutUpdateEvent();
                    if(hasDirtiness) continue;
                    fireAutomationEvents();
                    if(hasDirtiness) continue;
                    fireSizeChangedEvents(); // if nothing is dirty, one last chance for any size changes to announce.
                }

                currentElement = null;
                gotException = false;
            }
            finally
            {
                _isUpdating = false;
                _layoutRequestPosted = false;
                _isInUpdateLayout = false;

                if(gotException)
                {
                    if (etwTracingEnabled)
                    {
                        EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutException, etwKeywords, EventTrace.Level.Info, PerfService.GetPerfElementID(currentElement));
                    }

                    //set indicator
                    _gotException = true;
                    _forceLayoutElement = currentElement;

                    //make attempt to request the subsequent layout calc
                    //some exception handler schemas use Idle priorities to
                    //wait until dust settles. Then they correct the issue noted in the exception handler.
                    //We don't want to attempt to re-do the operation on the priority higher then that.
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, _updateLayoutBackground, this);
                }
            }

            MS.Internal.Text.TextInterface.Font.ResetFontFaceCache();
            MS.Internal.FontCache.BufferCache.Reset();

            if (etwTracingEnabled)
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientLayoutEnd, etwKeywords, EventTrace.Level.Info);
            }

#if DEBUG_CLR_MEM
            if (clrTracingEnabled && (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                CLRProfilerControl.CLRLogWriteLine("End_Layout_{0}", _layoutCLRPass);
            }
#endif // DEBUG_CLR_MEM
        }

        private Rect getProperArrangeRect(UIElement element)
        {
            Rect arrangeRect = element.PreviousArrangeRect;

            // ELements without a parent (top level) get Arrange at DesiredSize
            // if they were measured "to content" (as infinity indicates).
            // If we arrange the element that is temporarily disconnected
            // so it is not a top-level one, the assumption is that it will be
            // layout-invalidated and/or recomputed by the parent when reconnected.
            if (element.GetUIParentNo3DTraversal() == null)
            {
                arrangeRect.X = arrangeRect.Y = 0;

                if (double.IsPositiveInfinity(element.PreviousConstraint.Width))
                    arrangeRect.Width = element.DesiredSize.Width;

                if (double.IsPositiveInfinity(element.PreviousConstraint.Height))
                    arrangeRect.Height = element.DesiredSize.Height;
            }

            return arrangeRect;
        }

        private void invalidateTreeIfRecovering()
        {
            if((_forceLayoutElement != null) || _gotException)
            {
                if(_forceLayoutElement != null)
                {
                    markTreeDirty(_forceLayoutElement);
                }

                _forceLayoutElement = null;
                _gotException = false;
            }
        }

        internal LayoutQueue MeasureQueue
        {
            get
            {
                if(_measureQueue == null)
                    _measureQueue = new InternalMeasureQueue();
                return _measureQueue;
            }
        }

        internal LayoutQueue ArrangeQueue
        {
            get
            {
                if(_arrangeQueue == null)
                    _arrangeQueue = new InternalArrangeQueue();
                return _arrangeQueue;
            }
        }

        internal class InternalMeasureQueue: LayoutQueue
        {
            internal override void setRequest(UIElement e, Request r)
            {
                e.MeasureRequest = r;
            }

            internal override Request getRequest(UIElement e)
            {
                return e.MeasureRequest;
            }

            internal override bool canRelyOnParentRecalc(UIElement parent)
            {
                return !parent.IsMeasureValid
                    && !parent.MeasureInProgress; //if parent's measure is in progress, we might have passed this child already
            }

            internal override void invalidate(UIElement e)
            {
                e.InvalidateMeasureInternal();
            }
}


        internal class InternalArrangeQueue: LayoutQueue
        {
            internal override void setRequest(UIElement e, Request r)
            {
                e.ArrangeRequest = r;
            }

            internal override Request getRequest(UIElement e)
            {
                return e.ArrangeRequest;
            }

            internal override bool canRelyOnParentRecalc(UIElement parent)
            {
                return !parent.IsArrangeValid
                    && !parent.ArrangeInProgress; //if parent's arrange is in progress, we might have passed this child already
            }

            internal override void invalidate(UIElement e)
            {
                e.InvalidateArrangeInternal();
            }
}

        // delegate for dispatcher - keep it static so we don't allocate new ones.
        private static DispatcherOperationCallback _updateCallback = new DispatcherOperationCallback(UpdateLayoutCallback);
        private static object UpdateLayoutCallback(object arg)
        {
            ContextLayoutManager ContextLayoutManager = arg as ContextLayoutManager;
            if(ContextLayoutManager != null)
                ContextLayoutManager.UpdateLayout();
            return null;
        }

        //walks the list, fires events to alive handlers and removes dead ones
        private void fireLayoutUpdateEvent()
        {
            //no reentrancy. It may happen if one of handlers calls UpdateLayout synchronously
            if(_inFireLayoutUpdated) return;

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireLayoutUpdatedBegin);
            try
            {
                _inFireLayoutUpdated = true;

                LayoutEventList.ListItem [] copy = LayoutEvents.CopyToArray();

                for(int i=0; i<copy.Length; i++)
                {
                    LayoutEventList.ListItem item = copy[i];
                    //store handler here in case if thread gets pre-empted between check for IsAlive and invocation
                    //and GC can run making something that was alive not callable.
                    EventHandler e = null;
                    try
                    {
                        // this will return null if element is already GC'ed
                        e = (EventHandler)(item.Target);
                    }
                    catch(InvalidOperationException) //this will happen if element is being resurrected after finalization
                    {
                        e = null;
                    }

                    if(e != null)
                    {
                        e(null, EventArgs.Empty);
                        // if handler dirtied the tree, go clean it again before calling other handlers
                        if(hasDirtiness) break;
                    }
                    else
                    {
                        LayoutEvents.Remove(item);
                    }
                }
             }
            finally
            {
                _inFireLayoutUpdated = false;
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireLayoutUpdatedEnd);
            }
        }


        private LayoutEventList _layoutEvents;

        internal LayoutEventList LayoutEvents
        {
            get
            {
                if(_layoutEvents == null)
                    _layoutEvents = new LayoutEventList();
                return _layoutEvents;
            }
        }

        internal void AddToSizeChangedChain(SizeChangedInfo info)
        {
            //this typically will cause firing of SizeChanged from top to down. However, this order is not
            //specified for any users and is subject to change without notice.
            info.Next = _sizeChangedChain;
            _sizeChangedChain = info;
        }




        private void fireSizeChangedEvents()
        {
            //no reentrancy. It may happen if one of handlers calls UpdateLayout synchronously
            if(_inFireSizeChanged) return;

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireSizeChangedBegin);
            try
            {
                _inFireSizeChanged = true;

                //loop for SizeChanged
                while(_sizeChangedChain != null)
                {
                    SizeChangedInfo info = _sizeChangedChain;
                    _sizeChangedChain = info.Next;

                    info.Element.sizeChangedInfo = null;

                    info.Element.OnRenderSizeChanged(info);

                    //if callout dirtified the tree, return to cleaning
                    if(hasDirtiness) break;
                }
            }
            finally
            {
                _inFireSizeChanged = false;
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireSizeChangedEnd);
            }
        }

        private void fireAutomationEvents()
        {
            //no reentrancy. It may happen if one of handlers calls UpdateLayout synchronously
            if(_inFireAutomationEvents) return;

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireAutomationEventsBegin);
            try
            {
                _inFireAutomationEvents = true;
                _firePostLayoutEvents = false;

                LayoutEventList.ListItem [] copy = AutomationEvents.CopyToArray();

                for(int i=0; i<copy.Length; i++)
                {
                    LayoutEventList.ListItem item = copy[i];
                    //store peer here in case if thread gets pre-empted between check for IsAlive and invocation
                    //and GC can run making something that was alive not callable.
                    AutomationPeer peer = null;
                    try
                    {
                        // this will return null if element is already GC'ed
                        peer = (AutomationPeer)(item.Target);
                    }
                    catch(InvalidOperationException) //this will happen if element is being resurrected after finalization
                    {
                        peer = null;
                    }

                    if(peer != null)
                    {
                        peer.FireAutomationEvents();
                        // if handler dirtied the tree, go clean it again before calling other handlers
                        if(hasDirtiness) break;
                    }
                    else
                    {
                        AutomationEvents.Remove(item);
                    }
                }
            }
            finally
            {
                _inFireAutomationEvents = false;
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordLayout, EventTrace.Level.Verbose, EventTrace.Event.WClientLayoutFireAutomationEventsEnd);
            }
        }

        private LayoutEventList _automationEvents;

        internal LayoutEventList AutomationEvents
        {
            get
            {
                if(_automationEvents == null)
                    _automationEvents = new LayoutEventList();
                return _automationEvents;
            }
        }

        internal AutomationPeer[] GetAutomationRoots()
        {
            LayoutEventList.ListItem [] copy = AutomationEvents.CopyToArray();

            AutomationPeer[] peers = new AutomationPeer[copy.Length];
            int freeSlot = 0;

            for(int i=0; i<copy.Length; i++)
            {
                LayoutEventList.ListItem item = copy[i];
                //store peer here in case if thread gets pre-empted between check for IsAlive and invocation
                //and GC can run making something that was alive not callable.
                AutomationPeer peer = null;
                try
                {
                    // this will return null if element is already GC'ed
                    peer = (AutomationPeer)(item.Target);
                }
                catch(InvalidOperationException) //this will happen if element is being resurrected after finalization
                {
                    peer = null;
                }

                if(peer != null)
                {
                    peers[freeSlot++] = peer;
                }
            }

            return peers;
        }

        //this is used to prevent using automation roots in AutomationPeer when there are
        //sync updates of AutomationPeers on the stack. It is here because LayoutManager is
        //a Dispatcher-wide object and sync updates are per-dispatcher. Basically,
        //it is here to avoid creating AutomationManager to track Dispatcher scope.
        internal int AutomationSyncUpdateCounter
        {
            get
            {
                return _automationSyncUpdateCounter;
            }
            set
            {
                _automationSyncUpdateCounter = value;
            }
        }

        //Debuggability support - see LayoutInformation class in Framework
        internal UIElement GetLastExceptionElement()
        {
            return _lastExceptionElement;
        }

        internal void SetLastExceptionElement(UIElement e)
        {
            _lastExceptionElement = e;
        }

       ///// DATA //////

        private UIElement _forceLayoutElement; //set in extreme situations, forces the update of the whole tree containing the element
        private UIElement _lastExceptionElement; //set on exception in Measure or Arrange.

        private InternalMeasureQueue _measureQueue;
        private InternalArrangeQueue _arrangeQueue;
        private SizeChangedInfo      _sizeChangedChain;

        private static DispatcherOperationCallback _updateLayoutBackground = new DispatcherOperationCallback(UpdateLayoutBackground);
        private EventHandler _shutdownHandler;

        internal static int s_LayoutRecursionLimit = UIElement.MAX_ELEMENTS_IN_ROUTE; //to keep these two constants in sync
        private int _arrangesOnStack;
        private int _measuresOnStack;
        private int _automationSyncUpdateCounter;

        private bool      _isDead;
        private bool      _isUpdating;
        private bool      _isInUpdateLayout;
        private bool      _gotException; //true if UpdateLayout exited with exception
        private bool      _layoutRequestPosted;
        private bool      _inFireLayoutUpdated;
        private bool      _inFireSizeChanged;
        private bool      _firePostLayoutEvents;
        private bool      _inFireAutomationEvents;


#if DEBUG_CLR_MEM
        // Used for CLRProfiler comments
        private static int _layoutCLRPass = 0;
#endif

        internal abstract class LayoutQueue
        {
            //size of the pre-allocated free list
            private const int PocketCapacity = 153;
            //when this many elements remain in the free list,
            //queue will switch to invalidating up and adding only the root
            private const int PocketReserve = 8;

            internal abstract Request getRequest(UIElement e);
            internal abstract void setRequest(UIElement e, Request r);
            internal abstract bool canRelyOnParentRecalc(UIElement parent);
            internal abstract void invalidate(UIElement e);

            internal class Request
            {
                internal UIElement Target;
                internal Request Next;
                internal Request Prev;
            }

            internal LayoutQueue()
            {
                Request r;
                for(int i=0; i<PocketCapacity; i++)
                {
                    r = new Request();
                    r.Next = _pocket;
                    _pocket = r;
                }
                _pocketSize = PocketCapacity;
            }

            private void _addRequest(UIElement e)
            {
                Request r = _getNewRequest(e);

                if(r != null)
                {
                    r.Next = _head;
                    if(_head != null) _head.Prev = r;
                    _head = r;

                    setRequest(e, r);
                }
            }

            internal void Add(UIElement e)
            {
                if(getRequest(e) != null) return;
                if(e.CheckFlagsAnd(VisualFlags.IsLayoutSuspended)) return;

                RemoveOrphans(e);

                UIElement parent = e.GetUIParentWithinLayoutIsland();
                if(parent != null && canRelyOnParentRecalc(parent)) return;

                ContextLayoutManager layoutManager = ContextLayoutManager.From(e.Dispatcher);

                if(layoutManager._isDead) return;

                //10 is arbitrary number here, simply indicates the queue is
                //about to be filled. If not queue is not almost full, simply add
                //the element to it. If it is almost full, start conserve entries
                //by escalating invalidation to all the ancestors until the top of
                //the visual tree, and only add root of visula tree to the queue.
                if(_pocketSize > PocketReserve)
                {
                    _addRequest(e);
                }
                else
                {
                    //walk up until we are the topmost UIElement in the tree.
                    //on each step, mark the parent dirty and remove it from the queues
                    //only leave a single node in the queue - the root of visual tree
                    while(e != null)
                    {
                        UIElement p = e.GetUIParentWithinLayoutIsland();

                        invalidate(e); //invalidate in any case

                        if (p != null && p.Visibility != Visibility.Collapsed) //not yet a root or a collapsed node
                        {
                            Remove(e);
                        }
                        else //root of visual tree or a collapsed node
                        {
                            if (getRequest(e) == null)
                            {
                                RemoveOrphans(e);
                                _addRequest(e);
                            }
                        }
                        e = p;
                    }
                }

                layoutManager.NeedsRecalc();
            }

            internal void Remove(UIElement e)
            {
                Request r = getRequest(e);
                if(r == null) return;
                _removeRequest(r);
                setRequest(e, null);
            }

            internal void RemoveOrphans(UIElement parent)
            {
                Request r = _head;
                while(r != null)
                {
                    UIElement child = r.Target;
                    Request next = r.Next;
                    ulong parentTreeLevel = parent.TreeLevel;

                    if(   (child.TreeLevel == parentTreeLevel + 1)
                       && (child.GetUIParentWithinLayoutIsland() == parent))
                    {
                        _removeRequest(getRequest(child));
                        setRequest(child, null);
                    }

                    r = next;
                }
            }

            internal bool IsEmpty { get { return (_head == null); }}

            internal UIElement GetTopMost()
            {
                UIElement found = null;
                ulong treeLevel = ulong.MaxValue;

                for(Request r = _head; r != null; r = r.Next)
                {
                    UIElement t = r.Target;
                    ulong l = t.TreeLevel;

                    if(l < treeLevel)
                    {
                        treeLevel = l;
                        found = r.Target;
                    }
                }

                return found;
            }

            private void _removeRequest(Request entry)
            {
                if(entry.Prev == null) _head = entry.Next;
                else entry.Prev.Next = entry.Next;

                if(entry.Next != null) entry.Next.Prev = entry.Prev;

                ReuseRequest(entry);
            }

            private Request _getNewRequest(UIElement e)
            {
                Request r;
                if(_pocket != null)
                {
                    r = _pocket;
                    _pocket = r.Next;
                    _pocketSize--;
                    r.Next = r.Prev = null;
                }
                else
                {
                    ContextLayoutManager lm = ContextLayoutManager.From(e.Dispatcher);
                    try
                    {
                        r = new Request();
                    }
                    catch(System.OutOfMemoryException ex)
                    {
                        if(lm != null)
                            lm.setForceLayout(e);
                        throw ex;
                    }
                }

                r.Target = e;
                return r;
            }

            private void ReuseRequest(Request r)
            {
                r.Target = null; //let target die

                if (_pocketSize < PocketCapacity)
                {
                    r.Next = _pocket;
                    _pocket = r;
                    _pocketSize++;
                }
            }

            private Request _head;
            private Request _pocket;
            private int     _pocketSize;
        }
    }

    internal class LayoutEventList
    {
        //size of the pre-allocated free list
        private const int PocketCapacity = 153;

        internal class ListItem: WeakReference
        {
            internal ListItem() : base(null) {}
            internal ListItem Next;
            internal ListItem Prev;
            internal bool     InUse;
        }

        internal LayoutEventList()
        {
            ListItem t;
            for(int i=0; i<PocketCapacity; i++)
            {
                t = new ListItem();
                t.Next = _pocket;
                _pocket = t;
            }
            _pocketSize = PocketCapacity;
        }

        internal ListItem Add(object target)
        {
            ListItem t = getNewListItem(target);

            t.Next = _head;
            if(_head != null) _head.Prev = t;
            _head = t;

           _count++;
            return t;
        }

        internal void Remove(ListItem t)
        {
            //already removed item can be passed again
            //(once removed by handler and then by firing code)
            if(!t.InUse) return;

            if(t.Prev == null) _head = t.Next;
            else t.Prev.Next = t.Next;

            if(t.Next != null) t.Next.Prev = t.Prev;

            reuseListItem(t);
            _count--;
        }

        private ListItem getNewListItem(object target)
        {
            ListItem t;
            if(_pocket != null)
            {
                t = _pocket;
                _pocket = t.Next;
                _pocketSize--;
                t.Next = t.Prev = null;
            }
            else
            {
                t = new ListItem();
            }

            t.Target = target;
            t.InUse = true;
            return t;
        }

        private void reuseListItem(ListItem t)
        {
            t.Target = null; //let target die
            t.Next = t.Prev = null;
            t.InUse = false;

            if (_pocketSize < PocketCapacity)
            {
                t.Next = _pocket;
                _pocket = t;
                _pocketSize++;
            }
        }

        internal ListItem[] CopyToArray()
        {
            ListItem [] copy = new ListItem[_count];
            ListItem t = _head;
            int cnt = 0;
            while(t != null)
            {
                copy[cnt++] = t;
                t = t.Next;
            }
            return copy;
        }

        internal int Count
        {
            get
            {
                return _count;
            }
        }

        private ListItem _head;
        private ListItem _pocket;
        private int      _pocketSize;
        private int      _count;
    }
}

