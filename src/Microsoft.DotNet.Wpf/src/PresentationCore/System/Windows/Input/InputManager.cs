// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Security;
using MS.Win32;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using System;
using System.Diagnostics;
using System.Windows.Automation;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    ///     The InputManager class is responsible for coordinating all of the
    ///     input system in Avalon.
    /// </summary>
    public sealed class InputManager : DispatcherObject
    {
        /// <summary>
        ///     A routed event indicating that an input report arrived.
        /// </summary>
        internal static readonly RoutedEvent PreviewInputReportEvent = GlobalEventManager.RegisterRoutedEvent("PreviewInputReport", RoutingStrategy.Tunnel, typeof(InputReportEventHandler), typeof(InputManager));

        /// <summary>
        ///     A routed event indicating that an input report arrived.
        /// </summary>
        [FriendAccessAllowed]
        internal static readonly RoutedEvent InputReportEvent = GlobalEventManager.RegisterRoutedEvent("InputReport", RoutingStrategy.Bubble, typeof(InputReportEventHandler), typeof(InputManager));

        /// <summary>
        ///     Return the input manager associated with the current context.
        /// </summary>
        public static InputManager Current
        {
            get
            {
                return GetCurrentInputManagerImpl();
            }
        }

        ///<summary>
        ///     Internal implementation of InputManager.Current.
        ///     Critical but not TAS - for internal's to use.
        ///     Only exists for perf. The link demand check was causing perf in some XAF scenarios.
        ///</summary>
        internal static InputManager UnsecureCurrent
        {
            [FriendAccessAllowed]
            get
            {
                return GetCurrentInputManagerImpl();
            }
        }

        ///<summary>
        /// When true indicates input processing is synchronized.   
        ///</summary> 
        internal static bool IsSynchronizedInput
        {
            get
            {
                return _isSynchronizedInput;
            }
        }

        ///<summary>
        /// Synchronized input event type. 
        ///</summary> 
        internal static RoutedEvent[] SynchronizedInputEvents
        {
            get
            {
                return _synchronizedInputEvents;
            }
        }

        ///<summary>
        /// Complementary pair of Synchronized input events. 
        ///</summary> 
        internal static RoutedEvent[] PairedSynchronizedInputEvents
        {
            get
            {
                return _pairedSynchronizedInputEvents;
            }
        }

        ///<summary>
        /// Synchronized input type, set by the client. 
        ///</summary> 
        internal static SynchronizedInputType SynchronizeInputType
        {
            get
            {
                return _synchronizedInputType;
            }
        }

        ///<summary>
        /// Element on which StartListening was called.
        ///</summary> 
        internal static DependencyObject ListeningElement
        {
            get
            {
                return _listeningElement;
            }
        }

        ///<summary>
        /// Indicates state of the event during synchronized processing.
        ///</summary> 
        internal static SynchronizedInputStates SynchronizedInputState
        {
            get { return _synchronizedInputState; }
            set { _synchronizedInputState = value; }
        }

        ///<summary>
        ///     Implementation of InputManager.Current
        ///</summary>
        private static InputManager GetCurrentInputManagerImpl()
        {
            InputManager inputManager = null;

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            inputManager = dispatcher.InputManager as InputManager;

            if (inputManager == null)
            {
                inputManager = new InputManager();
                dispatcher.InputManager = inputManager;
            }

            return inputManager;
        }

        private InputManager()
        {
            // STA Requirement
            //
            // Avalon doesn't necessarily require STA, but many components do.  Examples
            // include Cicero, OLE, COM, etc.  So we throw an exception here if the
            // thread is not STA.
            if(Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new InvalidOperationException(SR.Get(SRID.RequiresSTA));
            }

            _stagingArea = new Stack();

            _primaryKeyboardDevice = new Win32KeyboardDevice(this);
            _primaryMouseDevice = new Win32MouseDevice(this);
            _primaryCommandDevice = new CommandDevice(this);

            _continueProcessingStagingAreaCallback = new DispatcherOperationCallback(ContinueProcessingStagingArea);

            _hitTestInvalidatedAsyncOperation = null;
            _hitTestInvalidatedAsyncCallback = new DispatcherOperationCallback(HitTestInvalidatedAsyncCallback);

            _layoutUpdatedCallback = new EventHandler(OnLayoutUpdated); //need to cache it, LM only keeps weak ref
            ContextLayoutManager.From(Dispatcher).LayoutEvents.Add(_layoutUpdatedCallback);

            // Timer used to synchronize the input devices periodically
            _inputTimer = new DispatcherTimer(DispatcherPriority.Background);
            _inputTimer.Tick += new EventHandler(ValidateInputDevices);
            _inputTimer.Interval = TimeSpan.FromMilliseconds(125);
        }

        public event PreProcessInputEventHandler PreProcessInput
        {
            add
            {
                _preProcessInput += value;
            }
            remove
            {
                _preProcessInput -= value;
            }
        }

        public event NotifyInputEventHandler PreNotifyInput
        {
            add
            {
                _preNotifyInput += value;
            }
            remove
            {
                _preNotifyInput -= value;
            }
}
        public event NotifyInputEventHandler PostNotifyInput
        {
            add
            {
                _postNotifyInput += value;
            }
            remove
            {
                _postNotifyInput -= value;
}
        }

        public event ProcessInputEventHandler PostProcessInput
        {
            add
            {
                _postProcessInput += value;
            }
            remove
            {
                _postProcessInput -= value;
            }
        }

        /// <summary>
        /// This event is raised by the HwndSource.CriticalTranslateAccelerator
        /// on descendent HwndSource instances. The only subscriber to this event
        /// is KeyboardNavigation.
        /// </summary>
        internal event KeyEventHandler TranslateAccelerator
        {
            [FriendAccessAllowed] // Used by KeyboardNavigation.cs in Framework
            add
            {
                _translateAccelerator += value;
            }
            [FriendAccessAllowed] // Used by KeyboardNavigation.cs in Framework
            remove
            {
                _translateAccelerator -= value;
            }
        }

        /// <summary>
        /// Raises the TranslateAccelerator event
        /// </summary>
        internal void RaiseTranslateAccelerator(KeyEventArgs e)
        {
            if (_translateAccelerator != null)
            {
                _translateAccelerator(this, e);
            }
        }

        /// <summary>
        ///     Registers an input provider with the input manager.
        /// </summary>
        /// <param name="inputProvider">
        ///     The input provider to register.
        /// </param>
        internal InputProviderSite RegisterInputProvider(IInputProvider inputProvider)
        {
//             VerifyAccess();


            // Create a site for this provider, and keep track of it.
            InputProviderSite site = new InputProviderSite(this, inputProvider);
            _inputProviders[inputProvider] = site;

            return site;
        }

        internal void UnregisterInputProvider(IInputProvider inputProvider)
        {
            _inputProviders.Remove(inputProvider);
        }

        /// <summary>
        ///     Returns a collection of input providers registered with the input manager.
        /// </summary>
        public ICollection InputProviders
        {
            get
            {
                return UnsecureInputProviders;
            }
        }


        /// <summary>
        ///     Returns a collection of input providers registered with the input manager.
        /// </summary>
        internal ICollection UnsecureInputProviders
        {
            get
            {
                return _inputProviders.Keys;
            }
        }
        /// <summary>
        ///     Read-only access to the primary keyboard device.
        /// </summary>
        public KeyboardDevice PrimaryKeyboardDevice
        {
            // 
            get {return _primaryKeyboardDevice;}
        }

        /// <summary>
        ///     Read-only access to the primary mouse device.
        /// </summary>
        public MouseDevice PrimaryMouseDevice
        {
            // 
            get {return _primaryMouseDevice;}
        }

        /// <summary>
        /// This property exists only due to the use of the private reflection hack as
        /// shown in the MSDN article located 
        /// <see href="https://msdn.microsoft.com/library/dd901337(v=vs.90).aspx">here</see>.
        /// Once this is no longer officially supported, this can be removed.
        /// </summary>
        internal StylusLogic StylusLogic
        {
            get { return StylusLogic.CurrentStylusLogic; }
        }

        /// <summary>
        ///     Read-only access to the primary keyboard device.
        /// </summary>
        internal CommandDevice PrimaryCommandDevice
        {
            get {return _primaryCommandDevice;}
        }

        /// <summary>
        ///     The InDragDrop property represents whether we are currently inside
        ///     a OLE DragDrop operation.
        /// </summary>
        internal bool InDragDrop
        {
            get { return _inDragDrop; }
            set { _inDragDrop = value; }
        }

        /// <summary>
        ///     The MostRecentInputDevice represents the last input device to
        ///     report an "interesting" user action.  What exactly constitutes
        ///     such an action is up to each device to implement.
        /// </summary>
        public InputDevice MostRecentInputDevice
        {
            get { return _mostRecentInputDevice; }
            internal set { _mostRecentInputDevice = value; }
        }

        /// <summary>
        ///     Controls call this to enter menu mode.
        ///</summary>
        public void PushMenuMode(PresentationSource menuSite)
        {
            if (menuSite == null)
            {
                throw new ArgumentNullException("menuSite");
            }
            menuSite.VerifyAccess();

            menuSite.PushMenuMode();
            _menuModeCount += 1;

            if (1 == _menuModeCount)
            {
                EventHandler enterMenuMode = EnterMenuMode;
                if (null != enterMenuMode)
                {
                    enterMenuMode(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        ///     Controls call this to leave menu mode.
        ///</summary>
        public void PopMenuMode(PresentationSource menuSite)
        {
            if (menuSite == null)
            {
                throw new ArgumentNullException("menuSite");
            }
            menuSite.VerifyAccess();

            if (_menuModeCount <= 0)
            {
                throw new InvalidOperationException();
            }

            menuSite.PopMenuMode();
            _menuModeCount -= 1;

            if (0 == _menuModeCount)
            {
                EventHandler leaveMenuMode = LeaveMenuMode;
                if (null != leaveMenuMode)
                {
                    leaveMenuMode(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        ///     Returns whether or not the input manager is in menu mode.
        ///</summary>
        public bool IsInMenuMode
        {
            get
            {
                return (_menuModeCount > 0);
            }
        }

        /// <summary>
        ///     This event notifies when the input manager enters menu mode.
        ///</summary>
        public event EventHandler EnterMenuMode;

        /// <summary>
        ///     This event notifies when the input manager leaves menu mode.
        ///</summary>
        public event EventHandler LeaveMenuMode;

        private int _menuModeCount;

        /// <summary>
        ///     An event that is raised whenever the result of a hit-test may
        ///     have changed.
        /// </summary>
        public event EventHandler HitTestInvalidatedAsync;

        internal void NotifyHitTestInvalidated()
        {
            // The HitTest result may have changed for someone somewhere.
            // Raise the HitTestInvalidatedAsync event after the next layout.
            if(_hitTestInvalidatedAsyncOperation == null)
            {
                // It would be best to re-evaluate anything dependent on the hit-test results
                // immediately after layout & rendering are complete.  Unfortunately this can
                // lead to an infinite loop.  Consider the following scenario:
                //
                // If the mouse is over an element, hide it.
                //
                // This never resolves to a "correct" state.  When the mouse moves over the
                // element, the element is hidden, so the mouse is no longer over it, so the
                // element is shown, but that means the mouse is over it again.  Repeat.
                //
                // We push our re-evaluation to a priority lower than input processing so that
                // the user can change the input device to avoid the infinite loops, or close
                // the app if nothing else works.
                //
                _hitTestInvalidatedAsyncOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                                                                        _hitTestInvalidatedAsyncCallback,
                                                                        null);
            }
            else if (_hitTestInvalidatedAsyncOperation.Priority == DispatcherPriority.Inactive)
            {
                // This means that we are currently waiting for the timer to expire so
                // that we can promote the current queue item to Input prority. Since
                // we are now being told that we need to re-hittest, we simply stop the
                // timer and promote the queue item right now instead of waiting for expiry.

                ValidateInputDevices(this, EventArgs.Empty);
            }
        }


        internal static void SafeCurrentNotifyHitTestInvalidated()
        {
            UnsecureCurrent.NotifyHitTestInvalidated();
        }

        private object HitTestInvalidatedAsyncCallback(object arg)
        {
            _hitTestInvalidatedAsyncOperation = null;
            if (HitTestInvalidatedAsync != null)
            {
                HitTestInvalidatedAsync(this, EventArgs.Empty);
            }

            return null;
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            NotifyHitTestInvalidated();
        }

        /// <summary>
        /// Start the timer that will kick off synchronize
        /// operation on all the input devices upon expiry
        /// </summary>
        internal void InvalidateInputDevices()
        {
            // If there is no pending ansyc hittest operation

            if (_hitTestInvalidatedAsyncOperation == null)
            {
                // Post an inactive item to the queue. When the timer expires
                // we will promote this queue item to Input priority.

                _hitTestInvalidatedAsyncOperation = Dispatcher.BeginInvoke(DispatcherPriority.Inactive,
                                                                        _hitTestInvalidatedAsyncCallback,
                                                                        null);

                // Start the input timer

                _inputTimer.IsEnabled = true;
            }
        }

        /// <summary>
        /// Synchronize the  input devices
        /// </summary>
        private void ValidateInputDevices(object sender, EventArgs e)
        {
            // It turns out that somehow we get here after the DispatcherOperation has been dispatched and we 
            // need to no-op on that.
            if (_hitTestInvalidatedAsyncOperation != null)
            {
                // Promote the pending DispatcherOperation to Input Priority

                _hitTestInvalidatedAsyncOperation.Priority = DispatcherPriority.Input;
}

            // Stop the input timer

            _inputTimer.IsEnabled = false;
        }

        /// <summary>
        ///     Synchronously processes the specified input.
        /// </summary>
        /// <remarks>
        ///     The specified input is processed by all of the filters and
        ///     monitors, and is finally dispatched to the appropriate
        ///     element as an input event.
        /// </remarks>
        /// <returns>
        ///     Whether or not any event generated as a consequence of this
        ///     event was handled.
        /// </returns>
        public bool ProcessInput(InputEventArgs input)
        {
//             VerifyAccess();

            if(input == null)
            {
                throw new ArgumentNullException("input");
            }

            // Push a marker indicating the portion of the staging area
            // that needs to be processed.
            PushMarker();

            // Push the input to be processed onto the staging area.
            PushInput(input, null);

            // Post a work item to continue processing the staging area
            // in case someone pushes a dispatcher frame in the middle
            // of input processing.
            RequestContinueProcessingStagingArea();

            // Now drain the staging area up to the marker we pushed.
            bool handled = ProcessStagingArea();
            return handled;
        }

        internal StagingAreaInputItem PushInput(StagingAreaInputItem inputItem)
        {
            _stagingArea.Push(inputItem);
            return inputItem;
        }

        internal StagingAreaInputItem PushInput(InputEventArgs input, StagingAreaInputItem promote)
        {
            StagingAreaInputItem item = new StagingAreaInputItem(false);
            item.Reset(input, promote);

            return PushInput(item);
        }

        internal StagingAreaInputItem PushMarker()
        {
            StagingAreaInputItem item = new StagingAreaInputItem(true);

            return PushInput(item);
        }

        internal StagingAreaInputItem PopInput()
        {
            object input = null;

            if(_stagingArea.Count > 0)
            {
                input = _stagingArea.Pop();
            }

            return input as StagingAreaInputItem;
        }


        internal StagingAreaInputItem PeekInput()
        {
            object input = null;

            if(_stagingArea.Count > 0)
            {
                input = _stagingArea.Peek();
            }

            return input as StagingAreaInputItem;
        }

        internal object ContinueProcessingStagingArea(object unused)
        {
            _continueProcessingStagingArea = false;

            // It is possible that we can be re-entered by a nested
            // dispatcher frame.  Continue processing the staging
            // area if we need to.
            if(_stagingArea.Count > 0)
            {
                // Before we actually start to drain the staging area, we need
                // to post a work item to process more input.  This enables us
                // to process more input if we enter a nested pump.
                RequestContinueProcessingStagingArea();

                // Now synchronously drain the staging area.
                ProcessStagingArea();
            }

            return null;
        }

        // When called, InputManager will get into synchronized input processing mode.
        internal static bool StartListeningSynchronizedInput(DependencyObject d, SynchronizedInputType inputType)
        {
            lock (_synchronizedInputLock)
            {
                if (_isSynchronizedInput)
                {
                    return false;
                }
                else
                {
                    _isSynchronizedInput = true;
                    _synchronizedInputState = SynchronizedInputStates.NoOpportunity;
                    _listeningElement = d;
                    _synchronizedInputType = inputType;
                    _synchronizedInputEvents = SynchronizedInputHelper.MapInputTypeToRoutedEvents(inputType);
                    _pairedSynchronizedInputEvents = SynchronizedInputHelper.MapInputTypeToRoutedEvents(SynchronizedInputHelper.GetPairedInputType(inputType));
                    return true;
                }
            }
        }

        // This method is used to cancel synchronized input processing.
        internal static void CancelSynchronizedInput()
        {
            lock (_synchronizedInputLock)
            {
                _isSynchronizedInput = false;
                _synchronizedInputState = SynchronizedInputStates.NoOpportunity;
                _listeningElement = null;
                _synchronizedInputEvents = null;
                _pairedSynchronizedInputEvents = null;

                if (_synchronizedInputAsyncClearOperation != null)
                {
                    _synchronizedInputAsyncClearOperation.Abort();
                    _synchronizedInputAsyncClearOperation = null;
                }
            }
        }


        private bool ProcessStagingArea()
        {
            bool handled = false;

            // For performance reasons, try to reuse the input event args.
            // If we are reentrered, we have to start over with fresh event
            // args, so we clear the member variables before continuing.
            // Also, we cannot simply make an single instance of the
            // PreProcessedInputEventArgs and cast it to NotifyInputEventArgs
            // or ProcessInputEventArgs because a malicious user could upcast
            // the object and call inappropriate methods.
            NotifyInputEventArgs notifyInputEventArgs = (_notifyInputEventArgs != null) ? _notifyInputEventArgs : new NotifyInputEventArgs();
            ProcessInputEventArgs processInputEventArgs = (_processInputEventArgs != null) ? _processInputEventArgs : new ProcessInputEventArgs();
            PreProcessInputEventArgs preProcessInputEventArgs = (_preProcessInputEventArgs != null) ? _preProcessInputEventArgs : new PreProcessInputEventArgs();
            _notifyInputEventArgs = null;
            _processInputEventArgs = null;
            _preProcessInputEventArgs = null;

            // Because we can be reentered, we can't just enumerate over the
            // staging area - that could throw an exception if the queue
            // changes underneath us.  Instead, just loop until we find a
            // frame marker or until the staging area is empty.
            StagingAreaInputItem item = null;
            while((item = PopInput()) != null)
            {
                // If we found a marker, we have reached the end of a
                // "section" of the staging area.  We just return from
                // the synchronous processing of the staging area.
                // If a dispatcher frame has been pushed by someone, this
                // will not return to the original ProcessInput.  Instead
                // it will unwind to the dispatcher and since we have
                // already pushed a work item to continue processing the
                // input, it will simply call back into us to do more
                // processing.  At which point we will continue to drain
                // the staging area.  This could cause strage behavior,
                // but it is deemed more acceptable than stalling input
                // processing.

                // In the future, in ordre to 
                // make sure we all agree on this.  We could also
                // just delay the rest of the staging area until
                // the dispatcher frame finishes.  Unfortunately,
                // this means one could receive an input event for
                // something that happened a long time ago.
                if (item.IsMarker)
                {
                    break;
                }

                // Pre-Process the input.  This could modify the staging
                // area, and it could cancel the processing of this
                // input event.
                //
                // Because we use multi-cast delegates, we always have to
                // create a new multi-cast delegate when we add or remove
                // a handler.  This means we can just call the current
                // multi-cast delegate instance, and it is safe to iterate
                // over, even if we get reentered.
                if (_preProcessInput != null)
                {
                    preProcessInputEventArgs.Reset(item, this);

                    // Invoke the handlers in reverse order so that handlers that
                    // users add are invoked before handlers in the system.
                    Delegate[] handlers = _preProcessInput.GetInvocationList();
                    for(int i = (handlers.Length - 1); i >= 0; i--)
                    {
                        PreProcessInputEventHandler handler = (PreProcessInputEventHandler) handlers[i];
                        handler(this, preProcessInputEventArgs);
                    }
                }

                if(!preProcessInputEventArgs.Canceled)
                {
                    // Pre-Notify the input.
                    //
                    // Because we use multi-cast delegates, we always have to
                    // create a new multi-cast delegate when we add or remove
                    // a handler.  This means we can just call the current
                    // multi-cast delegate instance, and it is safe to iterate
                    // over, even if we get reentered.
                    if(_preNotifyInput != null)
                    {
                        notifyInputEventArgs.Reset(item, this);

                        // Invoke the handlers in reverse order so that handlers that
                        // users add are invoked before handlers in the system.
                        Delegate[] handlers = _preNotifyInput.GetInvocationList();
                        for(int i = (handlers.Length - 1); i >= 0; i--)
                        {
                            NotifyInputEventHandler handler = (NotifyInputEventHandler) handlers[i];
                            handler(this, notifyInputEventArgs);
                        }
                    }

                    // Raise the input event being processed.
                    InputEventArgs input = item.Input;

                    // Some input events are explicitly associated with
                    // an element.  Those that are not are associated with
                    // the target of the input device for this event.
                    DependencyObject eventSource = input.Source as DependencyObject;
                    if(eventSource == null || !InputElement.IsValid(eventSource as IInputElement))
                    {
                        if (input.Device != null)
                        {
                            eventSource = input.Device.Target as DependencyObject;
                        }
                    }

                    // During synchronized input processing, event should be discarded if not listening for this input type.
                    if (_isSynchronizedInput &&
                        SynchronizedInputHelper.IsMappedEvent(input) &&
                        Array.IndexOf(SynchronizedInputEvents, input.RoutedEvent) < 0 &&
                        Array.IndexOf(PairedSynchronizedInputEvents, input.RoutedEvent) < 0)
                    {
                        if (!SynchronizedInputHelper.ShouldContinueListening(input))
                        {
                            // Discard the event
                            _synchronizedInputState = SynchronizedInputStates.Discarded;
                            SynchronizedInputHelper.RaiseAutomationEvents();
                            CancelSynchronizedInput();
                        }
                        else
                        {
                            _synchronizedInputAsyncClearOperation = Dispatcher.BeginInvoke((Action)delegate
                                {
                                    // Discard the event
                                    _synchronizedInputState = SynchronizedInputStates.Discarded;
                                    SynchronizedInputHelper.RaiseAutomationEvents();
                                    CancelSynchronizedInput();
                                }, 
                                DispatcherPriority.Background);
                        }
                    }
                    else
                    {
                        if (eventSource != null)
                        {
                            if (InputElement.IsUIElement(eventSource))
                            {
                                UIElement e = (UIElement)eventSource;

                                e.RaiseEvent(input, true); // Call the "trusted" flavor of RaiseEvent. 
                            }
                            else if (InputElement.IsContentElement(eventSource))
                            {
                                ContentElement ce = (ContentElement)eventSource;

                                ce.RaiseEvent(input, true);// Call the "trusted" flavor of RaiseEvent.
                            }
                            else if (InputElement.IsUIElement3D(eventSource))
                            {
                                UIElement3D e3D = (UIElement3D)eventSource;

                                e3D.RaiseEvent(input, true); // Call the "trusted" flavor of RaiseEvent
                            }

                            // If synchronized input raise appropriate automation event.
                            
                            if (_isSynchronizedInput && SynchronizedInputHelper.IsListening(_listeningElement, input))
                            {
                                if (!SynchronizedInputHelper.ShouldContinueListening(input))
                                {
                                    SynchronizedInputHelper.RaiseAutomationEvents();
                                    CancelSynchronizedInput();
                                }
                                else
                                {
                                    _synchronizedInputAsyncClearOperation = Dispatcher.BeginInvoke((Action)delegate
                                        {
                                            SynchronizedInputHelper.RaiseAutomationEvents();
                                            CancelSynchronizedInput();
                                        }, 
                                        DispatcherPriority.Background);
                                }
                            }
                        }
                    }

                    // Post-Notify the input.
                    //
                    // Because we use multi-cast delegates, we always have to
                    // create a new multi-cast delegate when we add or remove
                    // a handler.  This means we can just call the current
                    // multi-cast delegate instance, and it is safe to iterate
                    // over, even if we get reentered.
                    if(_postNotifyInput != null)
                    {
                        notifyInputEventArgs.Reset(item, this);

                        // Invoke the handlers in reverse order so that handlers that
                        // users add are invoked before handlers in the system.
                        Delegate[] handlers = _postNotifyInput.GetInvocationList();
                        for(int i = (handlers.Length - 1); i >= 0; i--)
                        {
                            NotifyInputEventHandler handler = (NotifyInputEventHandler) handlers[i];
                            handler(this, notifyInputEventArgs);
                        }
                    }

                    // Post-Process the input.  This could modify the staging
                    // area.
                    //
                    // Because we use multi-cast delegates, we always have to
                    // create a new multi-cast delegate when we add or remove
                    // a handler.  This means we can just call the current
                    // multi-cast delegate instance, and it is safe to iterate
                    // over, even if we get reentered.
                    if(_postProcessInput != null)
                    {
                        processInputEventArgs.Reset(item, this);

                        RaiseProcessInputEventHandlers(_postProcessInput, processInputEventArgs); 

                        // PreviewInputReport --> InputReport
                        if(item.Input.RoutedEvent == InputManager.PreviewInputReportEvent)
                        {
                            if(!item.Input.Handled)
                            {
                                InputReportEventArgs previewInputReport = (InputReportEventArgs) item.Input;

                                InputReportEventArgs inputReport = new InputReportEventArgs(previewInputReport.Device, previewInputReport.Report);
                                inputReport.RoutedEvent=InputManager.InputReportEvent;
                                PushInput(inputReport, item);
                            }
                        }
                    }

                    if(input.Handled)
                    {
                        handled = true;
                    }
                }
            }

            // Store our input event args so that we can use them again, and
            // avoid having to allocate more.
            _notifyInputEventArgs = notifyInputEventArgs;
            _processInputEventArgs = processInputEventArgs;
            _preProcessInputEventArgs = preProcessInputEventArgs;

            // Make sure to throw away the contents of the event args so
            // we don't keep refs around to things we don't mean to.
            _notifyInputEventArgs.Reset(null, null);
            _processInputEventArgs.Reset(null, null);
            _preProcessInputEventArgs.Reset(null, null);

            return handled;
        }

        private void RaiseProcessInputEventHandlers(ProcessInputEventHandler postProcessInput, ProcessInputEventArgs processInputEventArgs)
        {
            processInputEventArgs.StagingItem.Input.MarkAsUserInitiated();

            try
            {
                // Invoke the handlers in reverse order so that handlers that
                // users add are invoked before handlers in the system.
                Delegate[] handlers = postProcessInput.GetInvocationList();
                for(int i = (handlers.Length - 1); i >= 0; i--)
                {
                    ProcessInputEventHandler handler = (ProcessInputEventHandler) handlers[i];
                    handler(this, processInputEventArgs);
                }
            }
            finally // we do this in a finally block in case of exceptions
            {
                processInputEventArgs.StagingItem.Input.ClearUserInitiated();
            }
        }
        
                     
        private void RequestContinueProcessingStagingArea()
        {
            if(!_continueProcessingStagingArea)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, _continueProcessingStagingAreaCallback, null);
                _continueProcessingStagingArea = true;
            }
        }

        private DispatcherOperationCallback _continueProcessingStagingAreaCallback;
        private bool _continueProcessingStagingArea;

        private NotifyInputEventArgs _notifyInputEventArgs;
        private ProcessInputEventArgs _processInputEventArgs;
        private PreProcessInputEventArgs _preProcessInputEventArgs;

        //these four events introduced for secutiy purposes
        private event PreProcessInputEventHandler _preProcessInput;

        private event NotifyInputEventHandler _preNotifyInput;

        private event NotifyInputEventHandler _postNotifyInput;

        private event ProcessInputEventHandler _postProcessInput;

        private event KeyEventHandler _translateAccelerator;

        private Hashtable _inputProviders = new Hashtable();

        private KeyboardDevice _primaryKeyboardDevice;
        private MouseDevice    _primaryMouseDevice;
        private CommandDevice  _primaryCommandDevice;

        private bool            _inDragDrop;

        private DispatcherOperationCallback _hitTestInvalidatedAsyncCallback;
        private DispatcherOperation _hitTestInvalidatedAsyncOperation;
        private EventHandler _layoutUpdatedCallback;

        private Stack _stagingArea;

        private InputDevice _mostRecentInputDevice;

        // Timer used to synchronize the input devices periodically
        private DispatcherTimer _inputTimer;
        
        // Synchronized input automation related fields

        // Used to indicate whether any element is currently listening for synchronized input. 
        private static bool _isSynchronizedInput;

        // Element listening for synchronized input.
        private static DependencyObject _listeningElement;

        // Input event the element is listening on.
        private static RoutedEvent[] _synchronizedInputEvents;

        // Complementary pair of input event the element is listening on.
        private static RoutedEvent[] _pairedSynchronizedInputEvents;

        // Input type the element is listening on.
        private static SynchronizedInputType _synchronizedInputType;

        // Used to track state of synchronized input.
        private static SynchronizedInputStates _synchronizedInputState = SynchronizedInputStates.NoOpportunity;

        // Used to store the DispatcherOperation that waits until KeyDowns are translated to fire the corresponding AutomationEvent.
        private static DispatcherOperation _synchronizedInputAsyncClearOperation;

        // Lock used to serialize access to synchronized input related static fields.
        private static object _synchronizedInputLock = new object();
}
}

