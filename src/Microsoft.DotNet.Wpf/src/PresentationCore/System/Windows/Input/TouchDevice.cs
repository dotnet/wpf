// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusWisp;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.Security;
using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.PresentationCore;
using MS.Utility;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;
using System.Windows.Input.Tracing;

namespace System.Windows.Input
{
    /// <summary>
    ///     Represents a touch device (i.e. a finger).
    /// </summary>
    public abstract class TouchDevice : InputDevice, IManipulator
    {
        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        /// <param name="deviceId">
        ///     The ID of this device.
        ///     For a particular subclass of TouchDevice, ID should be unique.
        ///     Note: This is not validated to be unique.
        /// </param>
        protected TouchDevice(int deviceId)
            : base()
        {
            _deviceId = deviceId;
            _inputManager = InputManager.UnsecureCurrent;

            
            // If this is instantiated and the derived type is not a StylusTouchDevice then it is a 3rd party
            // custom touch device.
            StylusLogic stylusLogic = StylusLogic.CurrentStylusLogic;

            if (stylusLogic != null && !(this is StylusTouchDeviceBase))
            {
                stylusLogic.Statistics.FeaturesUsed |= StylusTraceLogger.FeatureFlags.CustomTouchDeviceUsed;
            }
        }

        private void AttachTouchDevice()
        {
            _inputManager.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
            _inputManager.HitTestInvalidatedAsync += new EventHandler(OnHitTestInvalidatedAsync);
        }

        private void DetachTouchDevice()
        {
            _inputManager.PostProcessInput -= new ProcessInputEventHandler(PostProcessInput);
            _inputManager.HitTestInvalidatedAsync -= new EventHandler(OnHitTestInvalidatedAsync);
        }

        /// <summary>
        ///     The ID of this device.
        ///     For a particular subclass of TouchDevice, ID should be unique.
        /// </summary>
        public int Id
        {
            get { return _deviceId; }
        }

        /// <summary>
        ///     This event will be raised whenever the device gets activated
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        ///     This event will be raised whenever the device gets deactivated
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        ///     IsActive boolean
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
        }


        #region InputDevice

        /// <summary>
        ///     Returns the element that input from this device is sent to.
        /// </summary>
        /// <remarks>
        ///     Always the same value as DirectlyOver.
        /// </remarks>
        public sealed override IInputElement Target
        {
            get { return _directlyOver; }
        }

        /// <summary>
        ///     Returns the PresentationSource that is reporting input for this device.
        /// </summary>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        ///     
        ///     Subclasses should use SetActiveSource to set this property.
        /// </remarks>
        public sealed override PresentationSource ActiveSource
        {
            get
            {
                return _activeSource;
            }
        }

        protected void SetActiveSource(PresentationSource activeSource)
        {
            _activeSource = activeSource;
        }

        #endregion

        #region Location

        /// <summary>
        ///     Returns the element that this device is over.
        /// </summary>
        public IInputElement DirectlyOver
        {
            get { return _directlyOver; }
        }

        /// <summary>
        ///     Provides the current position.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space.</param>
        /// <returns>The current position in the coordinate space of relativeTo.</returns>
        public abstract TouchPoint GetTouchPoint(IInputElement relativeTo);

        /// <summary>
        ///     Provides all of the known points the device hit since the last reported position update.
        /// </summary>
        /// <param name="relativeTo">Defines the coordinate space.</param>
        /// <returns>A list of points in the coordinate space of relativeTo.</returns>
        public abstract TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo);

        private IInputElement CriticalHitTest(Point point, bool isSynchronize)
        {
            IInputElement over = null;

            if (_activeSource != null)
            {
                switch (_captureMode)
                {
                    case CaptureMode.None:
                        // No capture, do a regular hit-test.
                        if (_isDown)
                        {
                            if (isSynchronize)
                            {
                                // In a synchronize call, we need to hit-test the window in addition to the element
                                over = GlobalHitTest(point, _activeSource);
                            }
                            else
                            {
                                // Just hit-test the element
                                over = LocalHitTest(point, _activeSource);
                            }

                            EnsureValid(ref over);
                        }
                        break;

                    case CaptureMode.Element:
                        // Capture is to a specific element, so the device will always be over that element.
                        over = _captured;
                        break;

                    case CaptureMode.SubTree:
                        // Capture is set to an entire subtree. Hit-test to determine the element (and window)
                        // the device is over. If the element is within the captured sub-tree (which can span 
                        // multiple windows), then the device is over that element. If the element is not within 
                        // the sub-tree, then the device is over the captured element.
                        {
                            IInputElement capture = InputElement.GetContainingInputElement(_captured as DependencyObject);
                            if (capture != null)
                            {
                                // We need to re-hit-test to get the "real" UIElement we are over.
                                // This allows us to have our capture-to-subtree span multiple windows.

                                // GlobalHitTest always returns an IInputElement, so we are sure to have one.
                                over = GlobalHitTest(point, _activeSource);
                            }

                            EnsureValid(ref over);

                            // Make sure that the element we hit is acutally underneath
                            // our captured element.  Because we did a global hit test, we
                            // could have hit an element in a completely different window.
                            //
                            // Note that we support the child being in a completely different window.
                            // So we use the GetUIParent method instead of just looking at
                            // visual/content parents.
                            if (over != null)
                            {
                                IInputElement ieTest = over;
                                while ((ieTest != null) && (ieTest != _captured))
                                {
                                    UIElement eTest = ieTest as UIElement;

                                    if (eTest != null)
                                    {
                                        ieTest = InputElement.GetContainingInputElement(eTest.GetUIParent(true));
                                    }
                                    else
                                    {
                                        ContentElement ceTest = ieTest as ContentElement;

                                        if (ceTest != null)
                                        {
                                            ieTest = InputElement.GetContainingInputElement(ceTest.GetUIParent(true));
                                        }
                                        else
                                        {
                                            UIElement3D e3DTest = (UIElement3D)ieTest;
                                            ieTest = InputElement.GetContainingInputElement(e3DTest.GetUIParent(true));
                                        }
                                    }
                                }

                                if (ieTest != _captured)
                                {
                                    // If we missed the capture point, consider the device over the capture point.
                                    over = _captured;
                                }
                            }
                            else
                            {
                                // If we didn't hit anything, consider the device over the capture point.
                                over = _captured;
                            }
                        }
                        break;
                }
            }

            return over;
        }

        private static void EnsureValid(ref IInputElement element)
        {
            // We understand UIElements and ContentElements.
            // If we are over something else (like a raw visual) find the containing element.
            if ((element != null) && !InputElement.IsValid(element))
            {
                element = InputElement.GetContainingInputElement(element as DependencyObject);
            }
        }

        private static IInputElement GlobalHitTest(Point pt, PresentationSource inputSource)
        {
            return MouseDevice.GlobalHitTest(false, pt, inputSource);
        }

        private static IInputElement LocalHitTest(Point pt, PresentationSource inputSource)
        {
            return MouseDevice.LocalHitTest(false, pt, inputSource);
        }

        #endregion

        #region Capture

        /// <summary>
        ///     The element this device is currently captured to.
        /// </summary>
        /// <remarks>
        ///     This value affects hit-testing to determine DirectlyOver.
        /// </remarks>
        public IInputElement Captured
        {
            get { return _captured; }
        }

        /// <summary>
        ///     The type of capture being used.
        /// </summary>
        /// <remarks>
        ///     This value affects hit-testing to determine DirectlyOver.
        /// </remarks>
        public CaptureMode CaptureMode
        {
            get { return _captureMode; }
        }

        /// <summary>
        ///     Captures this device to a particular element using CaptureMode.Element.
        /// </summary>
        /// <param name="element">The element this device will be captured to.</param>
        /// <returns>true if capture was changed, false otherwise.</returns>
        public bool Capture(IInputElement element)
        {
            return Capture(element, CaptureMode.Element);
        }

        /// <summary>
        ///     Captures this device to a particular element.
        /// </summary>
        /// <param name="element">The element this device will be captured to.</param>
        /// <param name="captureMode">The type of capture to use.</param>
        /// <returns>true if capture was changed, false otherwise.</returns>
        public bool Capture(IInputElement element, CaptureMode captureMode)
        {
            VerifyAccess();

            // If the element is null or captureMode is None, ensure
            // that the other parameter is consistent.
            if ((element == null) || (captureMode == CaptureMode.None))
            {
                element = null;
                captureMode = CaptureMode.None;
            }

            UIElement uiElement;
            ContentElement contentElement;
            UIElement3D uiElement3D;
            CastInputElement(element, out uiElement, out contentElement, out uiElement3D);

            if ((element != null) && (uiElement == null) && (contentElement == null) && (uiElement3D == null))
            {
                throw new ArgumentException(SR.Get(SRID.Invalid_IInputElement, element.GetType()), "element");
            }

            if (_captured != element)
            {
                // Ensure that the new element is visible and enabled
                if ((element == null) ||
                    (((uiElement != null) && uiElement.IsVisible && uiElement.IsEnabled) ||
                    ((contentElement != null) && contentElement.IsEnabled) ||
                    ((uiElement3D != null) && uiElement3D.IsVisible && uiElement3D.IsEnabled)))
                {
                    IInputElement oldCapture = _captured;
                    _captured = element;
                    _captureMode = captureMode;

                    UIElement oldUIElement;
                    ContentElement oldContentElement;
                    UIElement3D oldUIElement3D;
                    CastInputElement(oldCapture, out oldUIElement, out oldContentElement, out oldUIElement3D);

                    if (oldUIElement != null)
                    {
                        oldUIElement.IsEnabledChanged -= OnReevaluateCapture;
                        oldUIElement.IsVisibleChanged -= OnReevaluateCapture;
                        oldUIElement.IsHitTestVisibleChanged -= OnReevaluateCapture;
                    }
                    else if (oldContentElement != null)
                    {
                        oldContentElement.IsEnabledChanged -= OnReevaluateCapture;
                    }
                    else if (oldUIElement3D != null)
                    {
                        oldUIElement3D.IsEnabledChanged -= OnReevaluateCapture;
                        oldUIElement3D.IsVisibleChanged -= OnReevaluateCapture;
                        oldUIElement3D.IsHitTestVisibleChanged -= OnReevaluateCapture;
                    }
                    if (uiElement != null)
                    {
                        uiElement.IsEnabledChanged += OnReevaluateCapture;
                        uiElement.IsVisibleChanged += OnReevaluateCapture;
                        uiElement.IsHitTestVisibleChanged += OnReevaluateCapture;
                    }
                    else if (contentElement != null)
                    {
                        contentElement.IsEnabledChanged += OnReevaluateCapture;
                    }
                    else if (uiElement3D != null)
                    {
                        uiElement3D.IsEnabledChanged += OnReevaluateCapture;
                        uiElement3D.IsVisibleChanged += OnReevaluateCapture;
                        uiElement3D.IsHitTestVisibleChanged += OnReevaluateCapture;
                    }

                    UpdateReverseInheritedProperty(/* capture = */ true, oldCapture, _captured);

                    if (oldCapture != null)
                    {
                        DependencyObject o = oldCapture as DependencyObject;
                        o.SetValue(UIElement.AreAnyTouchesCapturedPropertyKey,
                            BooleanBoxes.Box(AreAnyTouchesCapturedOrDirectlyOver(oldCapture, /* isCapture = */ true)));
                    }
                    if (_captured != null)
                    {
                        DependencyObject o = _captured as DependencyObject;
                        o.SetValue(UIElement.AreAnyTouchesCapturedPropertyKey, BooleanBoxes.TrueBox);
                    }

                    if (oldCapture != null)
                    {
                        RaiseLostCapture(oldCapture);
                    }
                    if (_captured != null)
                    {
                        RaiseGotCapture(_captured);
                    }

                    // Capture successfully moved, notify the subclass.
                    OnCapture(element, captureMode);

                    Synchronize();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void UpdateReverseInheritedProperty(bool capture, IInputElement oldElement, IInputElement newElement)
        {
            // consdier caching the array
            List<DependencyObject> others = null;
            int count = (_activeDevices != null) ? _activeDevices.Count : 0;
            if (count > 0)
            {
                others = new List<DependencyObject>(count);
            }
            for (int i = 0; i < count; i++)
            {
                TouchDevice touchDevice = _activeDevices[i];
                if (touchDevice != this)
                {
                    DependencyObject other = capture ? (touchDevice._captured as DependencyObject) : (touchDevice._directlyOver as DependencyObject);
                    if (other != null)
                    {
                        others.Add(other);
                    }
                }
            }

            ReverseInheritProperty property = capture ? (ReverseInheritProperty)UIElement.TouchesCapturedWithinProperty : (ReverseInheritProperty)UIElement.TouchesOverProperty;
            DeferredElementTreeState treeState = capture ? _capturedWithinTreeState : _directlyOverTreeState;
            Action<DependencyObject, bool> originChangedAction = capture ? null : RaiseTouchEnterOrLeaveAction;

            property.OnOriginValueChanged(oldElement as DependencyObject, newElement as DependencyObject, others, ref treeState, originChangedAction);

            if (capture)
            {
                _capturedWithinTreeState = treeState;
            }
            else
            {
                _directlyOverTreeState = treeState;
            }
        }

        internal static void ReevaluateCapturedWithin(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            int count = _activeDevices != null ? _activeDevices.Count : 0;
            for (int i = 0; i < count; i++)
            {
                TouchDevice touchDevice = _activeDevices[i];
                touchDevice.ReevaluateCapturedWithinAsync(element, oldParent, isCoreParent);
            }
        }

        private void ReevaluateCapturedWithinAsync(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            if (element != null)
            {
                if (_capturedWithinTreeState == null)
                {
                    _capturedWithinTreeState = new DeferredElementTreeState();
                }

                if (isCoreParent)
                {
                    _capturedWithinTreeState.SetCoreParent(element, oldParent);
                }
                else
                {
                    _capturedWithinTreeState.SetLogicalParent(element, oldParent);
                }
            }

            if (_reevaluateCapture == null)
            {
                _reevaluateCapture = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    (DispatcherOperationCallback)delegate(object args)
                    {
                        _reevaluateCapture = null;
                        OnReevaluateCapturedWithinAsync();
                        return null;
                    }, null);
            }
        }

        private void OnReevaluateCapturedWithinAsync()
        {
            if (_captured == null)
            {
                return;
            }

            bool killCapture = false;

            //
            // First, check things like IsEnabled, IsVisible, etc. on a
            // UIElement vs. ContentElement basis.
            //
            UIElement uiElement;
            ContentElement contentElement;
            UIElement3D uiElement3D;
            CastInputElement(_captured, out uiElement, out contentElement, out uiElement3D);
            if (uiElement != null)
            {
                killCapture = !uiElement.IsEnabled || !uiElement.IsVisible || !uiElement.IsHitTestVisible;
            }
            else if (contentElement != null)
            {
                killCapture = !contentElement.IsEnabled;
            }
            else if (uiElement3D != null)
            {
                killCapture = !uiElement3D.IsEnabled || !uiElement3D.IsVisible || !uiElement3D.IsHitTestVisible;
            }
            else
            {
                killCapture = true;
            }

            //
            // Second, if we still haven't thought of a reason to kill capture, validate
            // it on a Visual basis for things like still being in the right tree.
            //
            if (killCapture == false)
            {
                DependencyObject containingVisual = InputElement.GetContainingVisual(_captured as DependencyObject);
                killCapture = !ValidateVisualForCapture(containingVisual);
            }

            //
            // Lastly, if we found any reason above, kill capture.
            //
            if (killCapture)
            {
                Capture(null);
            }

            // Refresh AreAnyTouchCapturesWithinProperty so that ReverseInherited flags are updated.
            if ((_capturedWithinTreeState != null) && !_capturedWithinTreeState.IsEmpty)
            {
                UpdateReverseInheritedProperty(/* capture = */ true, _captured, _captured);
            }
        }

        private bool ValidateVisualForCapture(DependencyObject visual)
        {
            if (visual == null)
                return false;

            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(visual);

            return ((presentationSource != null) && (presentationSource == _activeSource));
        }

        private void OnReevaluateCapture(object sender, DependencyPropertyChangedEventArgs e)
        {
            // IsEnabled, IsVisible, and/or IsHitTestVisible became false
            if (!(bool)e.NewValue)
            {
                if (_reevaluateCapture == null)
                {
                    _reevaluateCapture = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                        (DispatcherOperationCallback)delegate(object args)
                        {
                            _reevaluateCapture = null;
                            Capture(null);
                            return null;
                        }, null);
                }
            }
        }

        private static void CastInputElement(IInputElement element, out UIElement uiElement, out ContentElement contentElement, out UIElement3D uiElement3D)
        {
            uiElement = element as UIElement;
            contentElement = (uiElement == null) ? element as ContentElement : null;
            uiElement3D = ((uiElement == null) && (contentElement == null)) ? element as UIElement3D : null;
        }

        private void RaiseLostCapture(IInputElement oldCapture)
        {
            Debug.Assert(oldCapture != null, "oldCapture should be non-null.");

            TouchEventArgs e = CreateEventArgs(Touch.LostTouchCaptureEvent);
            e.Source = oldCapture;
            _inputManager.ProcessInput(e);
        }

        private void RaiseGotCapture(IInputElement captured)
        {
            Debug.Assert(captured != null, "captured should be non-null.");

            TouchEventArgs e = CreateEventArgs(Touch.GotTouchCaptureEvent);
            e.Source = captured;
            _inputManager.ProcessInput(e);
        }

        /// <summary>
        ///     Notifies subclasses that capture changed.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="captureMode"></param>
        protected virtual void OnCapture(IInputElement element, CaptureMode captureMode)
        {
        }

        #endregion

        #region Updating State

        protected bool ReportDown()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.TouchDownReported, _deviceId);

            _isDown = true;
            UpdateDirectlyOver(/* isSynchronize = */ false);
            bool handled = RaiseTouchDown();
            OnUpdated();

            Touch.ReportFrame();
            return handled;
        }

        protected bool ReportMove()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.TouchMoveReported, _deviceId);

            UpdateDirectlyOver(/* isSynchronize = */ false);
            bool handled = RaiseTouchMove();
            OnUpdated();

            Touch.ReportFrame();
            return handled;
        }

        protected bool ReportUp()
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordInput | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, EventTrace.Event.TouchUpReported, _deviceId);

            // DevDiv: 971187
            // If there is a hit test pending on the dispatcher queue for this touch device
            // we need to be sure that it is evaluated before we send a touch up.  Otherwise,
            // there may be visual tree changes that have invalidated the over property for
            // this device (such as removing the control we are over from the tree) that will
            // invalidate any touch ups and lead to missing events and other erroneous behavior.
            // This is safe to do as the next touch events for any correct touch device should
            // be constrained to a down or hover/move (if the device is not deactivated).  In 
            // those cases another hit test will occur removing the over selected by this test.
            // Any "old" hit tests still on the queue will run either on deactivated device
            // (which is no issue), or mid-stream with other touch messages.  Since the messages
            // available would be downs or moves/hovers, this should be no issue (as they update
            // the hit test as well).  Hit testing is stateless (without capture) so the results 
            // will also be the same as another hit test run on the same visual tree.  In cases
            // with capture the hit test is triggered already, so this will not change anything.
            if (_reevaluateOver != null)
            {
                _reevaluateOver = null;

                OnHitTestInvalidatedAsync(this, EventArgs.Empty);
            }

            bool handled = RaiseTouchUp();
            _isDown = false;
            UpdateDirectlyOver(/* isSynchronize = */ false);
            OnUpdated();

            Touch.ReportFrame();
            return handled;
        }

        protected void Activate()
        {
            if (_isActive)
            {
                throw new InvalidOperationException(SR.Get(SRID.Touch_DeviceAlreadyActivated));
            }

            PromotingToManipulation = false;
            AddActiveDevice(this);
            AttachTouchDevice();
            Synchronize();

            if (_activeDevices.Count == 1)
            {
                _isPrimary = true;
            }

            _isActive = true;

            if (Activated != null)
            {
                Activated(this, EventArgs.Empty);
            }
        }

        protected void Deactivate()
        {
            if (!_isActive)
            {
                throw new InvalidOperationException(SR.Get(SRID.Touch_DeviceNotActivated));
            }

            Capture(null);

            DetachTouchDevice();
            RemoveActiveDevice(this);

            _isActive = false;
            _manipulatingElement = null;

            if (Deactivated != null)
            {
                Deactivated(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///     Forces the TouchDevice to resynchronize.
        /// </summary>
        public void Synchronize()
        {
            if (_activeSource != null &&
                _activeSource.CompositionTarget != null &&
                !_activeSource.CompositionTarget.IsDisposed)
            {
                if (UpdateDirectlyOver(/* isSynchronize = */ true))
                {
                    OnUpdated();
                    Touch.ReportFrame();
                }
            }
        }

        protected virtual void OnManipulationEnded(bool cancel)
        {
            UIElement manipulatableElement = GetManipulatableElement();
            if (manipulatableElement != null && PromotingToManipulation)
            {
                Capture(null);
            }
        }

        protected virtual void OnManipulationStarted()
        {
        }

        private void OnHitTestInvalidatedAsync(object sender, EventArgs e)
        {
            // The hit-test result may have changed.
            Synchronize();

            if ((_directlyOverTreeState != null) && !_directlyOverTreeState.IsEmpty)
            {
                UpdateReverseInheritedProperty(/* capture = */ false, _directlyOver, _directlyOver);
            }
        }

        private bool UpdateDirectlyOver(bool isSynchronize)
        {
            IInputElement newDirectlyOver = null;

            TouchPoint touchPoint = GetTouchPoint(null);
            if (touchPoint != null)
            {
                Point position = touchPoint.Position;
                newDirectlyOver = CriticalHitTest(position, isSynchronize);
            }

            if (newDirectlyOver != _directlyOver)
            {
                ChangeDirectlyOver(newDirectlyOver);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnReevaluateDirectlyOver(object sender, DependencyPropertyChangedEventArgs e)
        {
            ReevaluateDirectlyOverAsync(null, null, true);
        }

        internal static void ReevaluateDirectlyOver(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            int count = _activeDevices != null ? _activeDevices.Count : 0;
            for (int i = 0; i < count; i++)
            {
                TouchDevice touchDevice = _activeDevices[i];
                touchDevice.ReevaluateDirectlyOverAsync(element, oldParent, isCoreParent);
            }
        }

        private void ReevaluateDirectlyOverAsync(DependencyObject element, DependencyObject oldParent, bool isCoreParent)
        {
            if (element != null)
            {
                if (_directlyOverTreeState == null)
                {
                    _directlyOverTreeState = new DeferredElementTreeState();
                }

                if (isCoreParent)
                {
                    _directlyOverTreeState.SetCoreParent(element, oldParent);
                }
                else
                {
                    _directlyOverTreeState.SetLogicalParent(element, oldParent);
                }
            }

            if (_reevaluateOver == null)
            {
                _reevaluateOver = Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    (DispatcherOperationCallback)delegate(object args)
                    {
                        _reevaluateOver = null;
                        OnHitTestInvalidatedAsync(this, EventArgs.Empty);
                        return null;
                    }, null);
            }
        }

        private void ChangeDirectlyOver(IInputElement newDirectlyOver)
        {
            Debug.Assert(newDirectlyOver != _directlyOver, "ChangeDirectlyOver called when newDirectlyOver is the same as _directlyOver.");

            IInputElement oldDirectlyOver = _directlyOver;
            _directlyOver = newDirectlyOver;

            UIElement oldUIElement;
            ContentElement oldContentElement;
            UIElement3D oldUIElement3D;
            CastInputElement(oldDirectlyOver, out oldUIElement, out oldContentElement, out oldUIElement3D);
            UIElement newUIElement;
            ContentElement newContentElement;
            UIElement3D newUIElement3D;
            CastInputElement(newDirectlyOver, out newUIElement, out newContentElement, out newUIElement3D);

            if (oldUIElement != null)
            {
                oldUIElement.IsEnabledChanged -= OnReevaluateDirectlyOver;
                oldUIElement.IsVisibleChanged -= OnReevaluateDirectlyOver;
                oldUIElement.IsHitTestVisibleChanged -= OnReevaluateDirectlyOver;
            }
            else if (oldContentElement != null)
            {
                oldContentElement.IsEnabledChanged -= OnReevaluateDirectlyOver;
            }
            else if (oldUIElement3D != null)
            {
                oldUIElement3D.IsEnabledChanged -= OnReevaluateDirectlyOver;
                oldUIElement3D.IsVisibleChanged -= OnReevaluateDirectlyOver;
                oldUIElement3D.IsHitTestVisibleChanged -= OnReevaluateDirectlyOver;
            }
            if (newUIElement != null)
            {
                newUIElement.IsEnabledChanged += OnReevaluateDirectlyOver;
                newUIElement.IsVisibleChanged += OnReevaluateDirectlyOver;
                newUIElement.IsHitTestVisibleChanged += OnReevaluateDirectlyOver;
            }
            else if (newContentElement != null)
            {
                newContentElement.IsEnabledChanged += OnReevaluateDirectlyOver;
            }
            else if (newUIElement3D != null)
            {
                newUIElement3D.IsEnabledChanged += OnReevaluateDirectlyOver;
                newUIElement3D.IsVisibleChanged += OnReevaluateDirectlyOver;
                newUIElement3D.IsHitTestVisibleChanged += OnReevaluateDirectlyOver;
            }

            UpdateReverseInheritedProperty(/* capture = */ false, oldDirectlyOver, newDirectlyOver);

            if (oldDirectlyOver != null)
            {
                DependencyObject o = oldDirectlyOver as DependencyObject;
                o.SetValue(UIElement.AreAnyTouchesDirectlyOverPropertyKey,
                            BooleanBoxes.Box(AreAnyTouchesCapturedOrDirectlyOver(oldDirectlyOver, /* isCapture = */ false)));
            }
            if (newDirectlyOver != null)
            {
                DependencyObject o = newDirectlyOver as DependencyObject;
                o.SetValue(UIElement.AreAnyTouchesDirectlyOverPropertyKey, BooleanBoxes.TrueBox);
            }
        }

        /// <summary>
        ///     Action to raise the TouchEnter/TouchLeave events.
        ///     This will be executed by TouchesOver RevereInheritance
        ///     property class on the entire parent chain affected by the
        ///     changes in TouchDevice's DirectlyOver.
        /// </summary>
        private Action<DependencyObject, bool> RaiseTouchEnterOrLeaveAction
        {
            get
            {
                if (_raiseTouchEnterOrLeaveAction == null)
                {
                    _raiseTouchEnterOrLeaveAction = new Action<DependencyObject, bool>(RaiseTouchEnterOrLeave);
                }
                return _raiseTouchEnterOrLeaveAction;
            }
        }

        private void RaiseTouchEnterOrLeave(DependencyObject element, bool isLeave)
        {
            Debug.Assert(element != null);
            TouchEventArgs touchEventArgs = CreateEventArgs(isLeave ? Touch.TouchLeaveEvent : Touch.TouchEnterEvent);
            touchEventArgs.Source = element;
            _inputManager.ProcessInput(touchEventArgs);
        }

        private TouchEventArgs CreateEventArgs(RoutedEvent routedEvent)
        {
            // review timestamps
            TouchEventArgs touchEventArgs = new TouchEventArgs(this, Environment.TickCount);
            touchEventArgs.RoutedEvent = routedEvent;
            return touchEventArgs;
        }

        private bool RaiseTouchDown()
        {
            TouchEventArgs e = CreateEventArgs(Touch.PreviewTouchDownEvent);
            _lastDownHandled = false;
            _inputManager.ProcessInput(e);

            // We want to return true if either of PreviewTouchDown or TouchDown
            // events are handled. Hence we cannot use e.Handled which is only
            // for preview.
            return _lastDownHandled;
        }

        private bool RaiseTouchMove()
        {
            TouchEventArgs e = CreateEventArgs(Touch.PreviewTouchMoveEvent);
            _lastMoveHandled = false;
            _inputManager.ProcessInput(e);

            // We want to return true if either of PreviewTouchMove or TouchMove
            // events are handled. Hence we cannot use e.Handled which is only
            // for preview.
            return _lastMoveHandled;
        }

        private bool RaiseTouchUp()
        {
            TouchEventArgs e = CreateEventArgs(Touch.PreviewTouchUpEvent);
            _lastUpHandled = false;
            _inputManager.ProcessInput(e);

            // We want to return true if either of PreviewTouchUp or TouchUp
            // events are handled. Hence we cannot use e.Handled which is only
            // for preview.
            return _lastUpHandled;
        }

        #endregion

        #region Input Processing and Promotion

        private void PostProcessInput(object sender, ProcessInputEventArgs e)
        {
            InputEventArgs inputEventArgs = e.StagingItem.Input;
            if ((inputEventArgs != null) && (inputEventArgs.Device == this))
            {
                if (inputEventArgs.Handled)
                {
                    RoutedEvent routedEvent = inputEventArgs.RoutedEvent;

                    if (routedEvent == Touch.PreviewTouchMoveEvent ||
                        routedEvent == Touch.TouchMoveEvent)
                    {
                        _lastMoveHandled = true;
                    }
                    else if (routedEvent == Touch.PreviewTouchDownEvent ||
                        routedEvent == Touch.TouchDownEvent)
                    {
                        _lastDownHandled = true;
                    }
                    else if (routedEvent == Touch.PreviewTouchUpEvent ||
                        routedEvent == Touch.TouchUpEvent)
                    {
                        _lastUpHandled = true;
                    }
                }
                else
                {
                    bool forManipulation;
                    RoutedEvent promotedTouchEvent = PromotePreviewToMain(inputEventArgs.RoutedEvent, out forManipulation);
                    if (promotedTouchEvent != null)
                    {
                        TouchEventArgs promotedTouchEventArgs = CreateEventArgs(promotedTouchEvent);
                        e.PushInput(promotedTouchEventArgs, e.StagingItem);
                    }
                    else if (forManipulation)
                    {
                        UIElement manipulatableElement = GetManipulatableElement();
                        if (manipulatableElement != null)
                        {
                            PromoteMainToManipulation(manipulatableElement, (TouchEventArgs)inputEventArgs);
                        }
                    }
                }
            }
        }

        private RoutedEvent PromotePreviewToMain(RoutedEvent routedEvent, out bool forManipulation)
        {
            forManipulation = false;
            if (routedEvent == Touch.PreviewTouchMoveEvent)
            {
                return Touch.TouchMoveEvent;
            }
            else if (routedEvent == Touch.PreviewTouchDownEvent)
            {
                return Touch.TouchDownEvent;
            }
            else if (routedEvent == Touch.PreviewTouchUpEvent)
            {
                return Touch.TouchUpEvent;
            }

            forManipulation = (routedEvent == Touch.TouchMoveEvent) ||
                (routedEvent == Touch.TouchDownEvent) ||
                (routedEvent == Touch.TouchUpEvent) ||
                (routedEvent == Touch.GotTouchCaptureEvent) ||
                (routedEvent == Touch.LostTouchCaptureEvent);
            return null;
        }

        private UIElement GetManipulatableElement()
        {
            UIElement element = InputElement.GetContainingUIElement(_directlyOver as DependencyObject) as UIElement;
            if (element != null)
            {
                element = Manipulation.FindManipulationParent(element);
            }

            return element;
        }

        private void PromoteMainToManipulation(UIElement manipulatableElement, TouchEventArgs touchEventArgs)
        {
            RoutedEvent routedEvent = touchEventArgs.RoutedEvent;
            if (routedEvent == Touch.TouchDownEvent)
            {
                // When touch goes down or if we're in the middle of a move, capture so that we can
                // start manipulation. We could be in the middle of a move if a device delayed 
                // promotion, such as the StylusTouchDevice, due to other gesture detection.
                Capture(manipulatableElement);
            }
            else if ((routedEvent == Touch.TouchUpEvent) && PromotingToManipulation)
            {
                // When touch goes up, release capture so that we can stop manipulation.
                Capture(null);
            }
            else if ((routedEvent == Touch.GotTouchCaptureEvent) && !PromotingToManipulation)
            {
                UIElement element = _captured as UIElement;
                if (element != null && element.IsManipulationEnabled)
                {
                    // When touch gets capture and if the captured element
                    // is manipulable, then add it as a manipulator to
                    // the captured element.
                    _manipulatingElement = new WeakReference(element);
                    Manipulation.AddManipulator(element, this);
                    PromotingToManipulation = true;
                    OnManipulationStarted();
                }
            }
            else if ((routedEvent == Touch.LostTouchCaptureEvent) && PromotingToManipulation && _manipulatingElement != null)
            {
                UIElement element = _manipulatingElement.Target as UIElement;
                _manipulatingElement = null;
                if (element != null)
                {
                    // When touch loses capture, remove it as a manipulator.
                    Manipulation.TryRemoveManipulator(element, this);
                    PromotingToManipulation = false;
                }
            }
        }

        /// <summary>
        ///     Whether this device should promote to manipulation.
        /// </summary>
        internal bool PromotingToManipulation
        {
            get;
            private set;
        }

        #endregion

        #region Active Devices

        private static void AddActiveDevice(TouchDevice device)
        {
            if (_activeDevices == null)
            {
                _activeDevices = new List<TouchDevice>(2);
            }

            _activeDevices.Add(device);
        }

        private static void RemoveActiveDevice(TouchDevice device)
        {
            if (_activeDevices != null)
            {
                _activeDevices.Remove(device);
            }
        }

        internal static TouchPointCollection GetTouchPoints(IInputElement relativeTo)
        {
            TouchPointCollection points = new TouchPointCollection();
            if (_activeDevices != null)
            {
                int count = _activeDevices.Count;
                for (int i = 0; i < count; i++)
                {
                    TouchDevice device = _activeDevices[i];
                    points.Add(device.GetTouchPoint(relativeTo));
                }
            }

            return points;
        }

        internal static TouchPoint GetPrimaryTouchPoint(IInputElement relativeTo)
        {
            if ((_activeDevices != null) && (_activeDevices.Count > 0))
            {
                TouchDevice device = _activeDevices[0];
                if (device._isPrimary)
                {
                    return device.GetTouchPoint(relativeTo);
                }
            }

            return null;
        }

        internal static void ReleaseAllCaptures(IInputElement element)
        {
            if (_activeDevices != null)
            {
                int count = _activeDevices.Count;
                for (int i = 0; i < count; i++)
                {
                    TouchDevice device = _activeDevices[i];
                    if (device.Captured == element)
                    {
                        device.Capture(null);
                    }
                }
            }
        }

        internal static IEnumerable<TouchDevice> GetCapturedTouches(IInputElement element, bool includeWithin)
        {
            return GetCapturedOrOverTouches(element, includeWithin, /* isCapture = */ true);
        }

        internal static IEnumerable<TouchDevice> GetTouchesOver(IInputElement element, bool includeWithin)
        {
            return GetCapturedOrOverTouches(element, includeWithin, /* isCapture = */ false);
        }

        private static bool IsWithin(IInputElement parent, IInputElement child)
        {
            // We are assuming parent and child are Visual, Visual3D, or ContentElement
            DependencyObject currentChild = child as DependencyObject;
            while ((currentChild != null) && (currentChild != parent))
            {
                if (currentChild is Visual || currentChild is Visual3D)
                {
                    currentChild = VisualTreeHelper.GetParent(currentChild);
                }
                else
                {
                    currentChild = ((ContentElement)currentChild).Parent;
                }
            }

            return (currentChild == parent);
        }

        private static IEnumerable<TouchDevice> GetCapturedOrOverTouches(IInputElement element, bool includeWithin, bool isCapture)
        {
            List<TouchDevice> touches = new List<TouchDevice>();
            if (_activeDevices != null)
            {
                int count = _activeDevices.Count;
                for (int i = 0; i < count; i++)
                {
                    TouchDevice device = _activeDevices[i];
                    IInputElement touchElement = isCapture ? device.Captured : device.DirectlyOver;
                    if ((touchElement != null) &&
                        ((touchElement == element) ||
                        (includeWithin && IsWithin(element, touchElement))))
                    {
                        touches.Add(device);
                    }
                }
            }
            return touches;
        }

        private static bool AreAnyTouchesCapturedOrDirectlyOver(IInputElement element, bool isCapture)
        {
            if (_activeDevices != null)
            {
                int count = _activeDevices.Count;
                for (int i = 0; i < count; i++)
                {
                    TouchDevice device = _activeDevices[i];
                    IInputElement touchElement = isCapture ? device.Captured : device.DirectlyOver;
                    if (touchElement != null &&
                        touchElement == element)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region IManipulator

        int IManipulator.Id
        {
            get
            {
                return Id;
            }
        }

        Point IManipulator.GetPosition(IInputElement relativeTo)
        {
            return GetTouchPoint(relativeTo).Position;
        }

        public event EventHandler Updated;

        private void OnUpdated()
        {
            if (Updated != null)
            {
                Updated(this, EventArgs.Empty);
            }
        }

        void IManipulator.ManipulationEnded(bool cancel)
        {
            this.OnManipulationEnded(cancel);
        }

        #endregion

        #region Data

        private int _deviceId;
        private IInputElement _directlyOver;
        private IInputElement _captured;
        private CaptureMode _captureMode;
        private bool _isDown;
        private DispatcherOperation _reevaluateCapture;
        private DispatcherOperation _reevaluateOver;
        private DeferredElementTreeState _directlyOverTreeState;
        private DeferredElementTreeState _capturedWithinTreeState;
        private bool _isPrimary;
        private bool _isActive;
        private Action<DependencyObject, bool> _raiseTouchEnterOrLeaveAction;

        // Technically only one flag is needed as per current code. But if
        // one of the derived touch devices raise a synchronizing TouchMove event,
        // while raising TouchUp, things would be messed up. Hence using three
        // different flags.
        private bool _lastDownHandled;
        private bool _lastUpHandled;
        private bool _lastMoveHandled;

        private PresentationSource _activeSource;

        private InputManager _inputManager;

        private WeakReference _manipulatingElement;

        [ThreadStatic]
        private static List<TouchDevice> _activeDevices;

        #endregion
    }
}
