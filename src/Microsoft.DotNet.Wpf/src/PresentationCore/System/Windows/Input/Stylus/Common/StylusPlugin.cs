// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// #define TRACE

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.Windows.Threading;

using System.Windows;
using System.Windows.Input;

using System.Windows.Media;
using MS.Win32; // for *NativeMethods

namespace System.Windows.Input.StylusPlugIns
{
    /////////////////////////////////////////////////////////////////////
    /// <summary>
    /// [TBS]
    /// </summary>
    public abstract class StylusPlugIn
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on Dispatcher
        /// </summary>
        protected StylusPlugIn()
        {
        }

        /////////////////////////////////////////////////////////////////////
        // (in Dispatcher)
        internal void Added(StylusPlugInCollection plugInCollection)
        {
            _pic = plugInCollection;
            OnAdded();
            InvalidateIsActiveForInput(); // Make sure we fire OnIsActivateForInputChanged.
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on Dispatcher
        /// </summary>
        protected virtual void OnAdded()
        {
        }

        /////////////////////////////////////////////////////////////////////
        // (in Dispatcher)
        internal void Removed()
        {
            // Make sure we fire OnIsActivateForInputChanged if we need to.
            if (_activeForInput)
            {
                InvalidateIsActiveForInput();
            }
            OnRemoved();
            _pic = null;
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on Dispatcher
        /// </summary>
        protected virtual void OnRemoved()
        {
        }

        /////////////////////////////////////////////////////////////////////
        // (in RTI Dispatcher)
        internal void StylusEnterLeave(bool isEnter, RawStylusInput rawStylusInput, bool confirmed)
        {
            // Only fire if plugin is enabled and hooked up to plugincollection.
            if (__enabled && _pic != null)
            {
                if (isEnter)
                    OnStylusEnter(rawStylusInput, confirmed);
                else
                    OnStylusLeave(rawStylusInput, confirmed);
            }
        }

        /////////////////////////////////////////////////////////////////////
        // (in RTI Dispatcher)
        internal void RawStylusInput(RawStylusInput rawStylusInput)
        {
            // Only fire if plugin is enabled and hooked up to plugincollection.
            if (__enabled && _pic != null)
            {
                try
                {
                    switch (rawStylusInput.Report.Actions)
                    {
                        case RawStylusActions.Down:
                            OnStylusDown(rawStylusInput);
                            break;
                        case RawStylusActions.Move:
                            OnStylusMove(rawStylusInput);
                            break;
                        case RawStylusActions.Up:
                            OnStylusUp(rawStylusInput);
                            break;
                    }
                }
                catch (Exception e)
                {
                    // This code is running on the Stylus Input thread 
                    // and has the chance to call out into app code. 
                    // If the app code throws an exception that is not caught, 
                    // then the app will crash 
                    // and the application will stop responding the touch. 
                    // We don't want to ignore all exceptions, 
                    // so we catch the exception and 
                    // dispatch it to the main thread, 
                    // allowing developers to handle it inside the handler 
                    // for the `Application.DispatcherUnhandledException` event.
                    _pic.Element.Dispatcher.InvokeAsync(() =>
                    {
                        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e).Throw();
                    },
                        // Why we should use `Send` priority? 
                        // Maybe the main thread is busy 
                        // that the developer can not find the exception timely
                        DispatcherPriority.Send);
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on RTI Dispatcher
        /// </summary>
        protected virtual void OnStylusEnter(RawStylusInput rawStylusInput, bool confirmed)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on RTI Dispatcher
        /// </summary>
        protected virtual void OnStylusLeave(RawStylusInput rawStylusInput, bool confirmed)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on RTI Dispatcher
        /// </summary>
        protected virtual void OnStylusDown(RawStylusInput rawStylusInput)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on RTI Dispatcher
        /// </summary>
        protected virtual void OnStylusMove(RawStylusInput rawStylusInput)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on RTI Dispatcher
        /// </summary>
        protected virtual void OnStylusUp(RawStylusInput rawStylusInput)
        {
        }

        /////////////////////////////////////////////////////////////////////
        // (on app Dispatcher)
        internal void FireCustomData(object callbackData,
                                                            RawStylusActions action,
                                                            bool targetVerified)
        {
            // Only fire if plugin is enabled and hooked up to plugincollection.
            if (__enabled && _pic != null)
            {
                switch (action)
                {
                    case RawStylusActions.Down:
                        OnStylusDownProcessed(callbackData, targetVerified);
                        break;
                    case RawStylusActions.Move:
                        OnStylusMoveProcessed(callbackData, targetVerified);
                        break;
                    case RawStylusActions.Up:
                        OnStylusUpProcessed(callbackData, targetVerified);
                        break;
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on app Dispatcher
        /// </summary>
        protected virtual void OnStylusDownProcessed(object callbackData, bool targetVerified)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on app Dispatcher
        /// </summary>
        protected virtual void OnStylusMoveProcessed(object callbackData, bool targetVerified)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on app Dispatcher
        /// </summary>
        protected virtual void OnStylusUpProcessed(object callbackData, bool targetVerified)
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - both Dispatchers
        /// </summary>
        public UIElement Element
        {
            get
            {
                return (_pic != null) ? _pic.Element : null;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - both Dispatchers
        /// </summary>
        public Rect ElementBounds
        {
            get
            {
                return (_pic != null) ? _pic.Rect : new Rect();
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - get - both Dispatchers, set Dispatcher
        /// </summary>
        public bool Enabled
        {
            get // both Dispatchers
            {
                return __enabled;
            }
            set // on Dispatcher
            {
                // Verify we are on the proper thread.
                if (_pic != null)
                {
                    _pic.Element.VerifyAccess();
                }

                if (value != __enabled)
                {
                    // If we are currently active for input we need to lock before input before
                    // changing so we don't get input coming in before event is fired.
                    if (_pic != null && _pic.IsActiveForInput)
                    {
                        // Make sure lock() doesn't cause reentrancy.
                        using (_pic.Element.Dispatcher.DisableProcessing())
                        {
                            _pic.ExecuteWithPotentialLock(() =>
                            {
                                // Make sure we fire the OnEnabledChanged event in the proper order
                                // depending on whether we are going active or inactive so you don't
                                // get input events after going inactive or before going active.
                                __enabled = value;
                                if (value == false)
                                {
                                    // Make sure we fire OnIsActivateForInputChanged if we need to.
                                    InvalidateIsActiveForInput();
                                    OnEnabledChanged();
                                }
                                else
                                {
                                    OnEnabledChanged();
                                    InvalidateIsActiveForInput();
                                }
                            });
                        }
                    }
                    else
                    {
                        __enabled = value;
                        if (value == false)
                        {
                            // Make sure we fire OnIsActivateForInputChanged if we need to.
                            InvalidateIsActiveForInput();
                            OnEnabledChanged();
                        }
                        else
                        {
                            OnEnabledChanged();
                            InvalidateIsActiveForInput();
                        }
                    }
                }
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on app Dispatcher
        /// </summary>
        protected virtual void OnEnabledChanged()
        {
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - app Dispatcher
        /// </summary>
        internal void InvalidateIsActiveForInput()
        {
            bool newIsActive = (_pic != null) ? (Enabled && _pic.Contains(this) &&
                _pic.IsActiveForInput) : false;

            if (newIsActive != _activeForInput)
            {
                _activeForInput = newIsActive;
                OnIsActiveForInputChanged();
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - get - both Dispatchers
        /// </summary>
        public bool IsActiveForInput
        {
            get // both Dispatchers
            {
                return _activeForInput;
            }
        }

        /////////////////////////////////////////////////////////////////////
        /// <summary>
        /// [TBS] - on Dispatcher
        /// </summary>
        protected virtual void OnIsActiveForInputChanged()
        {
        }

        // Enabled state is local to this plugin so we just use volatile versus creating a lock 
        // around it since we just read it from multiple thread and write from one.
        volatile bool __enabled = true;
        bool _activeForInput = false;
        StylusPlugInCollection _pic = null;
}
}
