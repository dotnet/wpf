// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MS.Internal;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Win32; // VK translation.
using System.Windows.Automation.Peers;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Input
{
    /// <summary>
    ///     The KeyboardDevice class represents the mouse device to the
    ///     members of a context.
    /// </summary>
    public abstract class KeyboardDevice : InputDevice
    {
        protected KeyboardDevice(InputManager inputManager)
        {
            _inputManager = new SecurityCriticalDataClass<InputManager>(inputManager);
            _inputManager.Value.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
            _inputManager.Value.PreNotifyInput += new NotifyInputEventHandler(PreNotifyInput);
            _inputManager.Value.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);

            _isEnabledChangedEventHandler = new DependencyPropertyChangedEventHandler(OnIsEnabledChanged);
            _isVisibleChangedEventHandler = new DependencyPropertyChangedEventHandler(OnIsVisibleChanged);
            _focusableChangedEventHandler = new DependencyPropertyChangedEventHandler(OnFocusableChanged);

            _reevaluateFocusCallback = new DispatcherOperationCallback(ReevaluateFocusCallback);
            _reevaluateFocusOperation = null;

            // Consider moving this elsewhere
            // The TextServicesManager must be created before the TextCompositionManager
            // so that TIP/IME listeners get precedence.
            _TsfManager = new SecurityCriticalDataClass<TextServicesManager>(new TextServicesManager(inputManager));
            _textcompositionManager = new SecurityCriticalData<TextCompositionManager>(new TextCompositionManager(inputManager));
        }

        /// <summary>
        ///     Gets the current state of the specified key from the device from the underlying system
        /// </summary>
        /// <param name="key">
        ///     Key to get the state of
        /// </param>
        /// <returns>
        ///     The state of the specified key
        /// </returns>
        protected abstract KeyStates GetKeyStatesFromSystem( Key key );

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        public override IInputElement Target
        {
            get
            {
                //VerifyAccess();
                if(null != ForceTarget)
                    return ForceTarget;

                return FocusedElement;
            }
        }

        internal IInputElement ForceTarget
        {
            get
            {
                return (IInputElement) _forceTarget;
            }
            set
            {
                _forceTarget = value as DependencyObject;
            }
        }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        public override PresentationSource ActiveSource
        {
            get
            {

                //VerifyAccess();
                if (_activeSource != null)
                {
                    return _activeSource.Value;
                }

                return null;
            }
        }

        /// <summary>
        ///     The default mode for restoring focus.
        /// <summary>
        public RestoreFocusMode DefaultRestoreFocusMode {get; set;}

        /// <summary>
        ///     Returns the element that the keyboard is focused on.
        /// </summary>
        public IInputElement FocusedElement
        {
            get
            {
//                 VerifyAccess();
                return (IInputElement) _focus;
            }
        }

        /// <summary>
        ///     Clears focus.
        /// </summary>
        public void ClearFocus()
        {
            Focus(null, false, false, false);
        }

        /// <summary>
        ///     Focuses the keyboard on a particular element.
        /// </summary>
        /// <param name="element">
        ///     The element to focus the keyboard on.
        /// </param>
        public IInputElement Focus(IInputElement element)
        {
            DependencyObject oFocus = null;
            bool forceToNullIfFailed = false;

            // Validate that if elt is either a UIElement or a ContentElement.
            if(element != null)
            {
                if(!InputElement.IsValid(element))
                {
                    #pragma warning suppress 6506 // element is obviously not null
                    throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, element.GetType()));
                }

                oFocus = (DependencyObject) element;
            }

            // If no element is given for focus, use the root of the active source.
            if(oFocus == null && _activeSource != null)
            {
                oFocus = _activeSource.Value.RootVisual as DependencyObject;
                forceToNullIfFailed = true;
            }

            Focus(oFocus, true, true, forceToNullIfFailed);

            return (IInputElement) _focus;
        }
        private void Focus(DependencyObject focus, bool askOld, bool askNew, bool forceToNullIfFailed)
        {
            // Make sure that the element is valid for receiving focus.
            bool isValid = true;
            if(focus != null)
            {
                isValid = Keyboard.IsFocusable(focus);

                if(!isValid && forceToNullIfFailed)
                {
                    focus = null;
                    isValid = true;
                }
            }

            if(isValid)
            {
                // Get the keyboard input provider that provides input for the active source.
                IKeyboardInputProvider keyboardInputProvider = null;
                DependencyObject containingVisual = InputElement.GetContainingVisual(focus);
                if(containingVisual != null)
                {
                    PresentationSource source = PresentationSource.CriticalFromVisual(containingVisual);
                    if (source != null)
                    {
                        keyboardInputProvider = (IKeyboardInputProvider)source.GetInputProvider(typeof(KeyboardDevice));
                    }
                }

                // Start the focus-change operation.
                TryChangeFocus(focus, keyboardInputProvider, askOld, askNew, forceToNullIfFailed);
            }
        }

        /// <summary>
        ///     Returns the set of modifier keys currently pressed as determined by querying our keyboard state cache
        /// </summary>
        public ModifierKeys Modifiers
        {
            get
            {
//                 VerifyAccess();

                ModifierKeys modifiers = ModifierKeys.None;
                if(IsKeyDown_private(Key.LeftAlt) || IsKeyDown_private(Key.RightAlt))
                {
                    modifiers |= ModifierKeys.Alt;
                }
                if(IsKeyDown_private(Key.LeftCtrl) || IsKeyDown_private(Key.RightCtrl))
                {
                    modifiers |= ModifierKeys.Control;
                }
                if(IsKeyDown_private(Key.LeftShift) || IsKeyDown_private(Key.RightShift))
                {
                    modifiers |= ModifierKeys.Shift;
                }

                return modifiers;
            }
        }

        /// <summary>
        /// There is a proscription against using Enum.IsDefined().  (it is slow)
        /// so we write these PRIVATE validate routines instead.
        /// </summary>
        private void Validate_Key(Key key)
        {
            if( 256 <= (int)key || (int)key <= 0)
                throw new  System.ComponentModel.InvalidEnumArgumentException("key", (int)key, typeof(Key));
        }

        /// <summary>
        /// This is the core private method that returns whether or not the specified key
        ///  is down.  It does it without the extra argument validation and context checks.
        /// </summary>
        private bool IsKeyDown_private(Key key)
        {
            return ( ( GetKeyStatesFromSystem(key) & KeyStates.Down ) == KeyStates.Down );
        }

        /// <summary>
        ///     Returns whether or not the specified key is down.
        /// </summary>
        public bool IsKeyDown(Key key)
        {
//             VerifyAccess();
            Validate_Key(key);
            return IsKeyDown_private(key);
        }

        /// <summary>
        ///     Returns whether or not the specified key is up.
        /// </summary>
        public bool IsKeyUp(Key key)
        {
//             VerifyAccess();
            Validate_Key(key);
            return (!IsKeyDown_private(key));
        }

        /// <summary>
        ///     Returns whether or not the specified key is toggled.
        /// </summary>
        public bool IsKeyToggled(Key key)
        {
//             VerifyAccess();
            Validate_Key(key);
            return( ( GetKeyStatesFromSystem(key) & KeyStates.Toggled ) == KeyStates.Toggled );
        }

        /// <summary>
        ///     Returns the state of the specified key.
        /// </summary>
        public KeyStates GetKeyStates(Key key)
        {
//             VerifyAccess();
            Validate_Key(key);
            return GetKeyStatesFromSystem(key);
        }

        internal TextServicesManager TextServicesManager
        {
           get
           {
               return _TsfManager.Value;
           }
        }


       internal TextCompositionManager TextCompositionManager
        {
           get
           {
               return _textcompositionManager.Value;
           }
        }

        private void TryChangeFocus(DependencyObject newFocus, IKeyboardInputProvider keyboardInputProvider, bool askOld, bool askNew, bool forceToNullIfFailed)
        {
            bool changeFocus = true;
            int timeStamp = Environment.TickCount ;
            DependencyObject oldFocus = _focus; // This is required, used below to see if focus has been delegated

            if(newFocus != _focus)
            {
                // If requested, and there is currently something with focus
                // Send the PreviewLostKeyboardFocus event to see if the object losing focus want to cancel it.
                // - no need to check "changeFocus" here as it was just previously unconditionally set to true
                if(askOld && _focus != null)
                {
                    KeyboardFocusChangedEventArgs previewLostFocus = new KeyboardFocusChangedEventArgs(this, timeStamp, (IInputElement)_focus, (IInputElement)newFocus);
                    previewLostFocus.RoutedEvent=Keyboard.PreviewLostKeyboardFocusEvent;
                    previewLostFocus.Source= _focus;
                    if(_inputManager != null)
                        _inputManager.Value.ProcessInput(previewLostFocus);

                    // is handled the right indication of canceled?
                    if (previewLostFocus.Handled)
                    {
                        changeFocus = false;
                    }
                }
                // If requested, and there is an object to specified to take focus
                // Send the PreviewGotKeyboardFocus event to see if the object gaining focus want to cancel it.
                // - must also check "changeFocus", no point in checking if the "previewLostFocus" event
                //   above already cancelled it
                if(askNew && changeFocus && newFocus != null)
                {
                    KeyboardFocusChangedEventArgs previewGotFocus = new KeyboardFocusChangedEventArgs(this, timeStamp, (IInputElement)_focus, (IInputElement)newFocus);
                    previewGotFocus.RoutedEvent=Keyboard.PreviewGotKeyboardFocusEvent;
                    previewGotFocus.Source= newFocus;
                    if(_inputManager != null)
                        _inputManager.Value.ProcessInput(previewGotFocus);

                    // is handled the right indication of canceled?
                    if (previewGotFocus.Handled)
                    {
                        changeFocus = false;
                    }
                }

                // If we are setting the focus to an element, see if the InputProvider
                // can take focus for us.
                if(changeFocus && newFocus != null)
                {
                    if (keyboardInputProvider != null && Keyboard.IsFocusable(newFocus))
                    {
                        // Tell the element we are about to acquire focus through
                        // the input provider.  The element losing focus and the
                        // element receiving focus have all agreed to the
                        // transaction.  This is used by menus to configure the
                        // behavior of focus changes.
                        KeyboardInputProviderAcquireFocusEventArgs acquireFocus = new KeyboardInputProviderAcquireFocusEventArgs(this, timeStamp, changeFocus);
                        acquireFocus.RoutedEvent = Keyboard.PreviewKeyboardInputProviderAcquireFocusEvent;
                        acquireFocus.Source= newFocus;
                        if(_inputManager != null)
                            _inputManager.Value.ProcessInput(acquireFocus);

                        // Acquire focus through the input provider.
                        changeFocus = keyboardInputProvider.AcquireFocus(false);

                        // Tell the element whether or not we were able to
                        // acquire focus through the input provider.
                        acquireFocus = new KeyboardInputProviderAcquireFocusEventArgs(this, timeStamp, changeFocus);
                        acquireFocus.RoutedEvent = Keyboard.KeyboardInputProviderAcquireFocusEvent;
                        acquireFocus.Source= newFocus;
                        if(_inputManager != null)
                            _inputManager.Value.ProcessInput(acquireFocus);
                    }
                    else
                    {
                        changeFocus = false;
                    }
                }

                // If the ChangeFocus operation was cancelled or the AcquireFocus operation failed
                // and the "ForceToNullIfFailed" flag was set, we set focus to null
                if( !changeFocus && forceToNullIfFailed && oldFocus == _focus /* Focus is not delegated */ )
                {
                    // focus might be delegated (e.g. during PreviewGotKeyboardFocus)
                    // without actually changing, if it was already on the delegated
                    // element.  We can't test for this directly,
                    // but if focus is within the desired element we'll assume this
                    // is what happened.
                    IInputElement newFocusElement = newFocus as IInputElement;
                    if (newFocusElement == null || !newFocusElement.IsKeyboardFocusWithin)
                    {
                        newFocus = null;
                        changeFocus = true;
                    }
                }

                // If both the old and new focus elements allowed it, and the
                // InputProvider has acquired it, go ahead and change our internal
                // sense of focus to the desired element.
                if(changeFocus)
                {
                    ChangeFocus(newFocus, timeStamp);
                }
            }
        }
        private void ChangeFocus(DependencyObject focus, int timestamp)
        {
            DependencyObject o = null;

            if(focus != _focus)
            {
                // Update the critical pieces of data.
                DependencyObject oldFocus = _focus;
                _focus = focus;
                _focusRootVisual = InputElement.GetRootVisual(focus);

                using(Dispatcher.DisableProcessing()) // Disable reentrancy due to locks taken
                {
                    // Adjust the handlers we use to track everything.
                    if(oldFocus != null)
                    {
                        o = oldFocus;
                        if (InputElement.IsUIElement(o))
                        {
                            ((UIElement)o).IsEnabledChanged -= _isEnabledChangedEventHandler;
                            ((UIElement)o).IsVisibleChanged -= _isVisibleChangedEventHandler;
                            ((UIElement)o).FocusableChanged -= _focusableChangedEventHandler;
                        }
                        else if (InputElement.IsContentElement(o))
                        {
                            ((ContentElement)o).IsEnabledChanged -= _isEnabledChangedEventHandler;
                            // NOTE: there is no IsVisible property for ContentElements.
                            ((ContentElement)o).FocusableChanged -= _focusableChangedEventHandler;
                        }
                        else
                        {
                            ((UIElement3D)o).IsEnabledChanged -= _isEnabledChangedEventHandler;
                            ((UIElement3D)o).IsVisibleChanged -= _isVisibleChangedEventHandler;
                            ((UIElement3D)o).FocusableChanged -= _focusableChangedEventHandler;
                        }
                    }
                    if(_focus != null)
                    {
                        o = _focus;
                        if (InputElement.IsUIElement(o))
                        {
                            ((UIElement)o).IsEnabledChanged += _isEnabledChangedEventHandler;
                            ((UIElement)o).IsVisibleChanged += _isVisibleChangedEventHandler;
                            ((UIElement)o).FocusableChanged += _focusableChangedEventHandler;
                        }                        
                        else if (InputElement.IsContentElement(o))
                        {
                            ((ContentElement)o).IsEnabledChanged += _isEnabledChangedEventHandler;
                            // NOTE: there is no IsVisible property for ContentElements.
                            ((ContentElement)o).FocusableChanged += _focusableChangedEventHandler;
                        }
                        else
                        {
                            ((UIElement3D)o).IsEnabledChanged += _isEnabledChangedEventHandler;
                            ((UIElement3D)o).IsVisibleChanged += _isVisibleChangedEventHandler;
                            ((UIElement3D)o).FocusableChanged += _focusableChangedEventHandler;
                        }
                    }
                }

                // Oddly enough, update the FocusWithinProperty properties first.  This is
                // so any callbacks will see the more-common FocusWithinProperty properties
                // set correctly.
                UIElement.FocusWithinProperty.OnOriginValueChanged(oldFocus, _focus, ref _focusTreeState);

                // Invalidate the IsKeyboardFocused properties.
                if(oldFocus != null)
                {
                    o = oldFocus;
                    o.SetValue(UIElement.IsKeyboardFocusedPropertyKey, false); // Same property for ContentElements
                }
                if(_focus != null)
                {
                    // Invalidate the IsKeyboardFocused property.
                    o = _focus;
                    o.SetValue(UIElement.IsKeyboardFocusedPropertyKey, true); // Same property for ContentElements
                }

                // Call TestServicesManager change the focus of the InputMethod is enable/disabled accordingly
                // so it's ready befere the GotKeyboardFocusEvent handler is invoked.
                if (_TsfManager != null)
                    _TsfManager.Value.Focus(_focus);

                // InputLanguageManager checks the preferred input languages.
                // This should before GotEvent because the preferred input language
                // should be set at the event handler.
                InputLanguageManager.Current.Focus(_focus, oldFocus);

                // Send the LostKeyboardFocus and GotKeyboardFocus events.
                if(oldFocus != null)
                {
                    KeyboardFocusChangedEventArgs lostFocus = new KeyboardFocusChangedEventArgs(this, timestamp, (IInputElement) oldFocus, (IInputElement) focus);
                    lostFocus.RoutedEvent=Keyboard.LostKeyboardFocusEvent;
                    lostFocus.Source= oldFocus;
                    if(_inputManager != null)
                        _inputManager.Value.ProcessInput(lostFocus);
                }
                if(_focus != null)
                {
                    KeyboardFocusChangedEventArgs gotFocus = new KeyboardFocusChangedEventArgs(this, timestamp, (IInputElement) oldFocus, (IInputElement) _focus);
                    gotFocus.RoutedEvent=Keyboard.GotKeyboardFocusEvent;
                    gotFocus.Source= _focus;
                    if(_inputManager!=null)
                        _inputManager.Value.ProcessInput(gotFocus);
                }

                // InputMethod checks the preferred ime state.
                // The preferred input methods should be applied after Cicero TIP gots SetFocus callback.
                InputMethod.Current.GotKeyboardFocus(_focus);

                //Could be also built-in into IsKeyboardFocused_Changed static on UIElement and ContentElement
                //However the Automation likes to go immediately back on us so it would be better be last one...
                AutomationPeer.RaiseFocusChangedEventHelper((IInputElement)_focus);
            }
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element with focus just became disabled.
            //
            // We can't leave focus on a disabled element, so move it.
            ReevaluateFocusAsync(null, null, false);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element with focus just became non-visible (collapsed or hidden).
            //
            // We can't leave focus on a non-visible element, so move it.
            ReevaluateFocusAsync(null, null, false);
        }

        private void OnFocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The element with focus just became unfocusable.
            //
            // We can't leave focus on an unfocusable element, so move it.
            ReevaluateFocusAsync(null, null, false);
        }

        /// <summary>
        ///     Determines if we can remain focused on the element we think has focus
        /// </summary>
        /// <remarks>
        ///     Queues an invocation of ReevaluateFocusCallback to do the actual work.
        ///         - that way if the object that had focus has only been temporarily
        ///           removed, disable, etc. and will eventually be valid again, we
        ///           avoid needlessly killing focus.
        /// </remarks>
        internal void ReevaluateFocusAsync(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            if(element != null)
            {
                if(isCoreParent)
                {
                    FocusTreeState.SetCoreParent(element, oldParent);
                }
                else
                {
                    FocusTreeState.SetLogicalParent(element, oldParent);
                }
            }

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
            if(_reevaluateFocusOperation == null)
            {
                _reevaluateFocusOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input, _reevaluateFocusCallback, null);
            }
        }

        /// <summary>
        ///     Determines if we can remain focused on the element we think has focus
        /// </summary>
        /// <remarks>
        ///     Invoked asynchronously by ReevaluateFocusAsync.
        ///     Confirms that the element we think has focus is:
        ///         - still enabled
        ///         - still visible
        ///         - still in the tree
        /// </remarks>
        private object ReevaluateFocusCallback(object arg)
        {
            _reevaluateFocusOperation = null;

            if( _focus == null )
            {
                return null;
            }

            //
            // Reevaluate the eligability of the focused element to actually
            // have focus.  If that element is no longer focusable, then search
            // for an ancestor that is.
            //
            DependencyObject element = _focus;
            while(element != null)
            {
                if(Keyboard.IsFocusable(element))
                {
                    break;
                }

                // Walk the current tree structure.
                element = DeferredElementTreeState.GetCoreParent(element, null);
            }

            // Get the PresentationSource that contains the element to be focused.
            PresentationSource presentationSource = null;
            DependencyObject visualContainer = InputElement.GetContainingVisual(element);
            if(visualContainer != null)
            {
                presentationSource = PresentationSource.CriticalFromVisual(visualContainer);
            }
            
            // The default action is to reset focus to the root element
            // of the active presentation source.
            bool moveFocus = true;
            DependencyObject moveFocusTo = null;

            if(presentationSource != null)
            {
                IKeyboardInputProvider keyboardProvider = presentationSource.GetInputProvider(typeof(KeyboardDevice)) as IKeyboardInputProvider;
                if(keyboardProvider != null)
                {
                    // Confirm with the keyboard provider for this
                    // presentation source that it has acquired focus.
                    if(keyboardProvider.AcquireFocus(true))
                    {
                        if(element == _focus)
                        {
                            // The focus element is still good.
                            moveFocus = false;
                        }
                        else
                        {
                            // The focus element is no longer focusable, but we found
                            // an ancestor that is, so move focus there.
                            moveFocus = true;
                            moveFocusTo = element;
                        }
                    }
                }
            }

            if(moveFocus)
            {
                if(moveFocusTo == null && _activeSource != null)
                {
                    moveFocusTo = _activeSource.Value.RootVisual as DependencyObject;
                }

                
                Focus(moveFocusTo, /*askOld=*/ false, /*askNew=*/ true, /*forceToNullIfFailed=*/ true);
            }
            else
            {
                // Refresh FocusWithinProperty so that ReverseInherited Flags are updated.
                //
                // We only need to do this if there is any information about the old
                // tree structure.
                if(_focusTreeState != null && !_focusTreeState.IsEmpty)
                {
                    UIElement.FocusWithinProperty.OnOriginValueChanged(_focus, _focus, ref _focusTreeState);
                }
            }

            return null;
        }

        private void PreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            RawKeyboardInputReport keyboardInput = ExtractRawKeyboardInputReport(e, InputManager.PreviewInputReportEvent);
            if(keyboardInput != null)
            {
                // Claim the input for the keyboard.
                e.StagingItem.Input.Device = this;
            }
        }

        private void PreNotifyInput(object sender, NotifyInputEventArgs e)
        {
            RawKeyboardInputReport keyboardInput = ExtractRawKeyboardInputReport(e, InputManager.PreviewInputReportEvent);
            if(keyboardInput != null)
            {
                CheckForDisconnectedFocus();

                // Activation
                //
                // MITIGATION: KEYBOARD_STATE_OUT_OF_SYNC
                //
                // It is very important that we allow multiple activate events.
                // This is how we deal with the fact that Win32 sometimes sends
                // us a WM_SETFOCUS message BEFORE it has updated it's internal
                // internal keyboard state information.  When we get the
                // WM_SETFOCUS message, we activate the keyboard with the
                // keyboard state (even though it could be wrong).  Then when
                // we get the first "real" keyboard input event, we activate
                // the keyboard again, since Win32 will have updated the
                // keyboard state correctly by then.
                //
                if((keyboardInput.Actions & RawKeyboardActions.Activate) == RawKeyboardActions.Activate)
                {
                    //if active source is null, no need to do special-case handling
                    if(_activeSource == null)
                    {
                        // we are now active.
                        _activeSource = new SecurityCriticalDataClass<PresentationSource>(keyboardInput.InputSource);
                    }
                    else if(_activeSource.Value != keyboardInput.InputSource)
                    {
                        IKeyboardInputProvider toDeactivate = _activeSource.Value.GetInputProvider(typeof(KeyboardDevice)) as IKeyboardInputProvider;

                        // we are now active.
                        _activeSource = new SecurityCriticalDataClass<PresentationSource>(keyboardInput.InputSource);

                        if(toDeactivate != null)
                        {
                            toDeactivate.NotifyDeactivate();
                        }
                    }
                }

                // Generally, we need to check against redundant actions.
                // We never prevet the raw event from going through, but we
                // will only generate the high-level events for non-redundant
                // actions.  We store the set of non-redundant actions in
                // the dictionary of this event.

                // If the input is reporting a key down, the action is never
                // considered redundant.
                if((keyboardInput.Actions & RawKeyboardActions.KeyDown) == RawKeyboardActions.KeyDown)
                {
                    RawKeyboardActions actions = GetNonRedundantActions(e);
                    actions |= RawKeyboardActions.KeyDown;
                    e.StagingItem.SetData(_tagNonRedundantActions, actions);

                    // Pass along the key that was pressed, and update our state.
                    Key key = KeyInterop.KeyFromVirtualKey(keyboardInput.VirtualKey);
                    e.StagingItem.SetData(_tagKey, key);
                    e.StagingItem.SetData(_tagScanCode, new ScanCode(keyboardInput.ScanCode, keyboardInput.IsExtendedKey));

                    // Tell the InputManager that the MostRecentDevice is us.
                    if(_inputManager!=null)
                        _inputManager.Value.MostRecentInputDevice = this;
                }

                // We are missing detection for redundant ups
                if((keyboardInput.Actions & RawKeyboardActions.KeyUp) == RawKeyboardActions.KeyUp)
                {
                    RawKeyboardActions actions = GetNonRedundantActions(e);
                    actions |= RawKeyboardActions.KeyUp;
                    e.StagingItem.SetData(_tagNonRedundantActions, actions);

                    // Pass along the key that was pressed, and update our state.
                    Key key = KeyInterop.KeyFromVirtualKey(keyboardInput.VirtualKey);
                    e.StagingItem.SetData(_tagKey, key);
                    e.StagingItem.SetData(_tagScanCode, new ScanCode(keyboardInput.ScanCode, keyboardInput.IsExtendedKey));

                    // Tell the InputManager that the MostRecentDevice is us.
                    if(_inputManager!=null)
                        _inputManager.Value.MostRecentInputDevice = this;
                }
            }

            // On KeyDown, we might need to set the Repeat flag

            if(e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyDownEvent)
            {
                CheckForDisconnectedFocus();

                KeyEventArgs args = (KeyEventArgs) e.StagingItem.Input;

                // Is this the same as the previous key?  (Look at the real key, e.g. TextManager
                // might have changed args.Key it to Key.TextInput.)

                if (_previousKey == args.RealKey)
                {
                    // Yes, this is a repeat (we got the keydown for it twice, with no KeyUp in between)
                    args.SetRepeat(true);
                }

                // Otherwise, keep this key to check against next time.
                else
                {
                    _previousKey = args.RealKey;
                    args.SetRepeat(false);
                }
            }

            // On KeyUp, we clear Repeat flag
            else if(e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyUpEvent)
            {
                CheckForDisconnectedFocus();

                KeyEventArgs args = (KeyEventArgs) e.StagingItem.Input;
                args.SetRepeat(false);

                // Clear _previousKey, so that down/up/down/up doesn't look like a repeat
                _previousKey = Key.None;
            }
        }

        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            // PreviewKeyDown --> KeyDown
            if(e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyDownEvent)
            {
                CheckForDisconnectedFocus();

                if(!e.StagingItem.Input.Handled)
                {
                    KeyEventArgs previewKeyDown = (KeyEventArgs) e.StagingItem.Input;

                    // Dig out the real key.
                    bool isSystemKey = false;
                    bool isImeProcessed = false;
                    bool isDeadCharProcessed = false;
                    Key key = previewKeyDown.Key;
                    if (key == Key.System)
                    {
                        isSystemKey = true;
                        key = previewKeyDown.RealKey;
                    } 
                    else if (key == Key.ImeProcessed)
                    {
                        isImeProcessed = true;
                        key = previewKeyDown.RealKey;
                    }
                    else if (key == Key.DeadCharProcessed)
                    {
                        isDeadCharProcessed = true;
                        key = previewKeyDown.RealKey;
                    }

                    KeyEventArgs keyDown = new KeyEventArgs(this, previewKeyDown.UnsafeInputSource, previewKeyDown.Timestamp, key);
                    keyDown.SetRepeat( previewKeyDown.IsRepeat );

                    // Mark the new event as SystemKey as appropriate.
                    if (isSystemKey)
                    {
                        keyDown.MarkSystem();
                    }
                    else if (isImeProcessed)
                    {
                        // Mark the new event as ImeProcessed as appropriate.
                        keyDown.MarkImeProcessed();
                    }
                    else if (isDeadCharProcessed)
                    {
                        keyDown.MarkDeadCharProcessed();
                    }

                    keyDown.RoutedEvent=Keyboard.KeyDownEvent;
                    keyDown.ScanCode = previewKeyDown.ScanCode;
                    keyDown.IsExtendedKey = previewKeyDown.IsExtendedKey;
                    e.PushInput(keyDown, e.StagingItem);
                }
            }

            // PreviewKeyUp --> KeyUp
            if(e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyUpEvent)
            {
                CheckForDisconnectedFocus();

                if(!e.StagingItem.Input.Handled)
                {
                    KeyEventArgs previewKeyUp = (KeyEventArgs) e.StagingItem.Input;

                    // Dig out the real key.
                    bool isSystemKey = false;
                    bool isImeProcessed = false;
                    bool isDeadCharProcessed = false;
                    Key key = previewKeyUp.Key;
                    if (key == Key.System)
                    {
                        isSystemKey = true;
                        key = previewKeyUp.RealKey;
                    }
                    else if (key == Key.ImeProcessed)
                    {
                        isImeProcessed = true;
                        key = previewKeyUp.RealKey;
                    }
                    else if(key == Key.DeadCharProcessed)
                    {
                        isDeadCharProcessed = true;
                        key = previewKeyUp.RealKey;
                    }

                    KeyEventArgs keyUp = new KeyEventArgs(this, previewKeyUp.UnsafeInputSource, previewKeyUp.Timestamp, key);

                    // Mark the new event as SystemKey as appropriate.
                    if (isSystemKey)
                    {
                        keyUp.MarkSystem();
                    }
                    else if (isImeProcessed)
                    {
                        // Mark the new event as ImeProcessed as appropriate.
                        keyUp.MarkImeProcessed();
                    } 
                    else if (isDeadCharProcessed)
                    {
                        keyUp.MarkDeadCharProcessed();
                    }

                    keyUp.RoutedEvent=Keyboard.KeyUpEvent;
                    keyUp.ScanCode = previewKeyUp.ScanCode;
                    keyUp.IsExtendedKey = previewKeyUp.IsExtendedKey;
                    e.PushInput(keyUp, e.StagingItem);
                }
            }

            RawKeyboardInputReport keyboardInput = ExtractRawKeyboardInputReport(e, InputManager.InputReportEvent);
            if(keyboardInput != null)
            {
                CheckForDisconnectedFocus();

                if(!e.StagingItem.Input.Handled)
                {
                    // In general, this is where we promote the non-redundant
                    // reported actions to our premier events.
                    RawKeyboardActions actions = GetNonRedundantActions(e);

                    // Raw --> PreviewKeyDown
                    if((actions & RawKeyboardActions.KeyDown) == RawKeyboardActions.KeyDown)
                    {
                        Key key = (Key) e.StagingItem.GetData(_tagKey);
                        if(key != Key.None)
                        {
                            KeyEventArgs previewKeyDown = new KeyEventArgs(this, keyboardInput.InputSource, keyboardInput.Timestamp, key);
                            ScanCode scanCode = (ScanCode)e.StagingItem.GetData(_tagScanCode);
                            previewKeyDown.ScanCode = scanCode.Code;
                            previewKeyDown.IsExtendedKey = scanCode.IsExtended;
                            if (keyboardInput.IsSystemKey)
                            {
                                previewKeyDown.MarkSystem();
                            }
                            previewKeyDown.RoutedEvent=Keyboard.PreviewKeyDownEvent;
                            e.PushInput(previewKeyDown, e.StagingItem);
                        }
                    }

                    // Raw --> PreviewKeyUp
                    if((actions & RawKeyboardActions.KeyUp) == RawKeyboardActions.KeyUp)
                    {
                        Key key = (Key) e.StagingItem.GetData(_tagKey);
                        if(key != Key.None)
                        {
                            KeyEventArgs previewKeyUp = new KeyEventArgs(this, keyboardInput.InputSource, keyboardInput.Timestamp, key);
                            ScanCode scanCode = (ScanCode)e.StagingItem.GetData(_tagScanCode);
                            previewKeyUp.ScanCode = scanCode.Code;
                            previewKeyUp.IsExtendedKey = scanCode.IsExtended;
                            if (keyboardInput.IsSystemKey)
                            {
                                previewKeyUp.MarkSystem();
                            }
                            previewKeyUp.RoutedEvent=Keyboard.PreviewKeyUpEvent;
                            e.PushInput(previewKeyUp, e.StagingItem);
                        }
                    }
                }

                // Deactivate
                if((keyboardInput.Actions & RawKeyboardActions.Deactivate) == RawKeyboardActions.Deactivate)
                {
                    if(IsActive)
                    {
                        _activeSource = null;

                        // Even if handled, a keyboard deactivate results in a lost focus.
                        ChangeFocus(null, e.StagingItem.Input.Timestamp);
                    }
                }
            }
        }

        private RawKeyboardInputReport ExtractRawKeyboardInputReport(NotifyInputEventArgs e, RoutedEvent Event)
        {
            RawKeyboardInputReport keyboardInput = null;

            InputReportEventArgs input = e.StagingItem.Input as InputReportEventArgs;
            if(input != null)
            {
                if(input.Report.Type == InputType.Keyboard && input.RoutedEvent == Event)
                {
                    keyboardInput = input.Report as RawKeyboardInputReport;
                }
            }

            return keyboardInput;
        }

        private RawKeyboardActions GetNonRedundantActions(NotifyInputEventArgs e)
        {
            RawKeyboardActions actions;

            // The CLR throws a null-ref exception if it tries to unbox a
            // null.  So we have to special case that.
            object o = e.StagingItem.GetData(_tagNonRedundantActions);
            if(o != null)
            {
                actions = (RawKeyboardActions) o;
            }
            else
            {
                actions = new RawKeyboardActions();
            }

            return actions;
        }


        // at the moment we don't have a good way of detecting when an
        // element gets deleted from the tree (logical or visual).  The
        // best we can do right now is clear out the focus if we detect
        // that the tree containing the focus was disconnected.
        private bool CheckForDisconnectedFocus()
        {
            bool wasDisconnected = false;

            if(InputElement.GetRootVisual (_focus as DependencyObject) != _focusRootVisual)
            {
                wasDisconnected = true;
                Focus (null);
            }

            return wasDisconnected;
        }

        internal bool IsActive
        {
            get
            {
                return _activeSource != null && _activeSource.Value != null;
            }
        }

        private DeferredElementTreeState FocusTreeState
        {
            get
            {
                if (_focusTreeState == null)
                {
                    _focusTreeState = new DeferredElementTreeState();
                }

                return _focusTreeState;
            }
        }

        private SecurityCriticalDataClass<InputManager> _inputManager;
        private SecurityCriticalDataClass<PresentationSource> _activeSource;

        private DependencyObject _focus;
        private DeferredElementTreeState _focusTreeState;
        private DependencyObject _forceTarget;
        private DependencyObject _focusRootVisual;
        private Key _previousKey;

        private DependencyPropertyChangedEventHandler _isEnabledChangedEventHandler;
        private DependencyPropertyChangedEventHandler _isVisibleChangedEventHandler;
        private DependencyPropertyChangedEventHandler _focusableChangedEventHandler;
        private DispatcherOperationCallback _reevaluateFocusCallback;
        private DispatcherOperation _reevaluateFocusOperation;

        // Data tags for information we pass around the staging area.
        private object _tagNonRedundantActions = new object();
        private object _tagKey = new object();
        private object _tagScanCode = new object();

        private class ScanCode
        {
            internal ScanCode(int code, bool isExtended)
            {
                _code = code;
                _isExtended = isExtended;
            }

            internal int Code {get {return _code;}}
            internal bool IsExtended {get {return _isExtended;}}

            private readonly int _code;
            private readonly bool _isExtended;
        }

        // TextCompositionManager handles KeyDown -> Unicode conversion.
        private SecurityCriticalData<TextCompositionManager> _textcompositionManager;
        // TextServicesManager handles KeyDown -> IME composition conversion.
        private SecurityCriticalDataClass<TextServicesManager> _TsfManager;
}
}

