// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
//      Ink Collection Behavior
//
// Authors: alexz/samgeo/bkrantz
//


using System;
using System.Collections;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Interop;
using System.ComponentModel;
using System.Collections.Specialized;
using Swi = System.Windows.Ink;
using System.Windows.Documents;

namespace MS.Internal.Ink
{
    /// <summary>
    /// InkCollectionBehavior
    /// </summary>
    internal sealed class InkCollectionBehavior : StylusEditingBehavior
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        internal InkCollectionBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
            : base(editingCoordinator, inkCanvas)
        {
            _stylusPoints = null;
            _userInitiated = false;
        }

        #endregion Constructors

        /// <summary>
        /// A method which flags InkCollectionBehavior when it needs to reset the dynamic renderer.
        ///    Called By:
        ///         EditingCoordinator.OnInkCanvasDeviceDown
        /// </summary>
        internal void ResetDynamicRenderer()
        {
            _resetDynamicRenderer = true;
        }

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Overrides SwitchToMode 
        /// As the following expected results
        ///  1. From Ink To InkAndGesture
        ///     Packets between StylusDown and StylusUp are sent to the gesture reco. On StylusUp gesture event fires. If it’s not a gesture, StrokeCollected event fires.
        ///  2. From Ink To GestureOnly
        ///     Packets between StylusDown and StylusUp are send to the gesture reco. On StylusUp gesture event fires. Stroke gets removed on StylusUp even if it’s not a gesture.
        ///  3. From Ink To EraseByPoint
        ///     Stroke is discarded. PointErasing is performed after changing the mode.
        ///  4. From Ink To EraseByStroke
        ///      Stroke is discarded. StrokeErasing is performed after changing the mode.
        ///  5. From Ink To Select
        ///     Stroke is discarded. Lasso is drawn for all packets between StylusDown and StylusUp. Strokes/elements within the lasso will be selected.
        ///  6. From Ink To None
        ///     Stroke is discarded.
        ///  7. From InkAndGesture To Ink
        ///     Stroke is collected for all packets between StylusDown and StylusUp. Gesture event does not fire.
        ///  8. From InkAndGesture To GestureOnly
        ///     Packets between StylusDown and StylusUp are sent to the gesture reco. Stroke gets removed on StylusUp even if it’s not a gesture.
        ///  9. From InkAndGesture To EraseByPoint
        ///     Stroke is discarded. PointErasing is performed after changing the mode, gesture event does not fire.
        /// 10. From InkAndGesture To EraseByStroke
        ///     Stroke is discarded. StrokeErasing is performed after changing the mode, gesture event does not fire.
        /// 11. From InkAndGesture To Select
        ///     Lasso is drawn for all packets between StylusDown and StylusUp. Strokes/elements within the lasso will be selected.
        /// 12. From InkAndGesture To None
        ///     Stroke is discarded, no gesture is recognized.
        /// 13. From GestureOnly To InkAndGesture
        ///     Packets between StylusDown and StylusUp are sent to the gesture reco. On StylusUp gesture event fires. If it’s not a gesture, StrokeCollected event fires.
        /// 14. From GestureOnly To Ink
        ///     Stroke is collected. Gesture event does not fire.
        /// 15. From GestureOnly To EraseByPoint
        ///     Stroke is discarded PointErasing is performed after changing the mode, gesture event does not fire
        /// 16. From GestureOnly To EraseByStroke
        ///     Stroke is discarded. StrokeErasing is performed after changing the mode, gesture event does not fire.
        /// 17. From GestureOnly To Select
        ///     Lasso is drawn for all packets between StylusDown and StylusUp. Strokes/elements within the lasso will be selected.
        /// 18. From GestureOnly To None
        ///     Stroke is discarded. Gesture event does not fire.
        /// </summary>
        /// <param name="mode"></param>
        protected override void OnSwitchToMode(InkCanvasEditingMode mode)
        {
            Debug.Assert(EditingCoordinator.IsInMidStroke, "SwitchToMode should only be called in a mid-stroke");

            switch ( mode )
            {
                case InkCanvasEditingMode.Ink:
                case InkCanvasEditingMode.InkAndGesture:
                case InkCanvasEditingMode.GestureOnly:
                    {
                        // We are under one of those Ink modes now. Nothing to change here except raising the mode change event.
                        InkCanvas.RaiseActiveEditingModeChanged(new RoutedEventArgs(InkCanvas.ActiveEditingModeChangedEvent, InkCanvas));
                        break;
                    }
                case InkCanvasEditingMode.EraseByPoint:
                case InkCanvasEditingMode.EraseByStroke:
                    {
                        // Discard the collected ink.
                        Commit(false);

                        // Change the Erase mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                case InkCanvasEditingMode.Select:
                    {
                        // Make a copy of the current cached points.
                        StylusPointCollection cachedPoints = _stylusPoints != null ? 
                                                                _stylusPoints.Clone() : null;

                        // Discard the collected ink.
                        Commit(false);

                        // Change the Select mode
                        IStylusEditing newBehavior = EditingCoordinator.ChangeStylusEditingMode(this, mode);

                        if ( cachedPoints != null 
                            // NOTICE-2006/04/27-WAYNEZEN,
                            // EditingCoordinator.ChangeStylusEditingMode raises external event.
                            // The user code could take any arbitrary action for instance calling InkCanvas.ReleaseMouseCapture()
                            // So there is no guarantee that we could receive the newBehavior.
                            && newBehavior != null)
                        {
                            // Now add the previous points to the lasso behavior
                            // The SelectionBehavior doesn't check userInitiated, pass false
                            // even if our _userInitiated flag is true
                            newBehavior.AddStylusPoints(cachedPoints, false/*userInitiated*/);
                        }

                        break;
                    }
                case InkCanvasEditingMode.None:
                    {
                        // Discard the collected ink.
                        Commit(false);

                        // Change to the None mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                default:
                    Debug.Assert(false, "Unknown InkCanvasEditingMode!");
                    break;
            }
        }

        /// <summary>
        /// OnActivate
        /// </summary>
        protected override void OnActivate()
        {
            base.OnActivate();

            // Enable RealTimeInking.
            if (InkCanvas.InternalDynamicRenderer != null)
            {
                InkCanvas.InternalDynamicRenderer.Enabled = true;
                InkCanvas.UpdateDynamicRenderer(); // Kick DynamicRenderer to be hooked up to renderer.
            }

            //if we're activated in PreviewStylusDown or in mid-stroke, the DynamicRenderer will miss the down
            //and will not ink.  If that is the case, flag InkCollectionBehavior to reset RTI.
            _resetDynamicRenderer = EditingCoordinator.StylusOrMouseIsDown;
        }

        /// <summary>
        /// OnDeactivate
        /// </summary>
        protected override void OnDeactivate()
        {
            base.OnDeactivate();

            // Disable RealTimeInking.
            if (InkCanvas.InternalDynamicRenderer != null)
            {
                InkCanvas.InternalDynamicRenderer.Enabled = false;
                InkCanvas.UpdateDynamicRenderer();  // Kick DynamicRenderer to be removed from renderer.
            }
        }

        /// <summary>
        /// Get Pen Cursor
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetCurrentCursor()
        {
            if ( EditingCoordinator.UserIsEditing == true )
            {
                return Cursors.None;
            }
            else
            {
                return PenCursor;
            }
        }


        /// <summary>
        /// StylusInputBegin
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputBegin(StylusPointCollection stylusPoints, bool userInitiated)
        {
            _userInitiated = false;

            //we only initialize to true if the first stylusPoints were user initiated
            if (userInitiated)
            {
                _userInitiated = true;
            }

            _stylusPoints = new StylusPointCollection(stylusPoints.Description, 100);
            _stylusPoints.Add(stylusPoints);

            _strokeDrawingAttributes = this.InkCanvas.DefaultDrawingAttributes.Clone();

            // Reset the dynamic renderer if it's been flagged.
            if ( _resetDynamicRenderer )
            {
                InputDevice inputDevice = EditingCoordinator.GetInputDeviceForReset();
                if ( InkCanvas.InternalDynamicRenderer != null && inputDevice != null )
                {
                    StylusDevice stylusDevice = inputDevice as StylusDevice;
                    // If the input device is MouseDevice, null will be passed in Reset Method.
                    InkCanvas.InternalDynamicRenderer.Reset(stylusDevice, stylusPoints);
                }

                _resetDynamicRenderer = false;
            }

            // Call InvalidateBehaviorCursor at the end of the routine. The method will cause an external event fired.
            // So it should be invoked after we set up our states.
            EditingCoordinator.InvalidateBehaviorCursor(this);
        }

        /// <summary>
        /// StylusInputContinue
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputContinue(StylusPointCollection stylusPoints, bool userInitiated)
        {
            //we never set _userInitated to true after it is initialized, only to false
            if (!userInitiated)
            {
                _userInitiated = false;
            }

            _stylusPoints.Add(stylusPoints);
        }

        /// <summary>
        /// StylusInputEnd
        /// </summary>
        /// <param name="commit">commit</param>
        protected override void StylusInputEnd(bool commit)
        {
            // The follow code raises Gesture and/or StrokeCollected event
            // The out-side code could throw exception in the their handlers. We use try/finally block to protect our status.
            try
            {
                if ( commit )
                {
                    // 
                    // It's possible that the input may end up without any StylusPoint being collected since the behavior can be deactivated by
                    // the user code in the any event handler.
                    if ( _stylusPoints != null )
                    {
                        Debug.Assert(_strokeDrawingAttributes != null, "_strokeDrawingAttributes can not be null, did we not see a down?");

                        Stroke stroke =
                            new Stroke(_stylusPoints, _strokeDrawingAttributes);

                        //we don't add the stroke to the InkCanvas stroke collection until RaiseStrokeCollected
                        //since this might be a gesture and in some modes, gestures don't get added
                        InkCanvasStrokeCollectedEventArgs argsStroke = new InkCanvasStrokeCollectedEventArgs(stroke);
                        InkCanvas.RaiseGestureOrStrokeCollected(argsStroke, _userInitiated);
                    }
                }
            }
            finally
            {
                _stylusPoints = null;
                _strokeDrawingAttributes = null;
                _userInitiated = false;
                EditingCoordinator.InvalidateBehaviorCursor(this);
            }
        }

        /// <summary>
        /// ApplyTransformToCursor
        /// </summary>
        protected override void OnTransformChanged()
        {
            // Drop the cached pen cursor.
            _cachedPenCursor = null;
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        private Cursor PenCursor
        {
            get
            {
                // We only update our cache cursor when DefaultDrawingAttributes has changed or 
                // there are animated transforms being applied to InkCanvas.
                if ( _cachedPenCursor == null || _cursorDrawingAttributes != InkCanvas.DefaultDrawingAttributes )
                {
                    //adjust the DA for any Layout/Render transforms.
                    Matrix xf = GetElementTransformMatrix();

                    DrawingAttributes da = this.InkCanvas.DefaultDrawingAttributes;
                    if ( !xf.IsIdentity )
                    {
                        //scale the DA, zero the offsets.
                        xf *= da.StylusTipTransform;
                        xf.OffsetX = 0;
                        xf.OffsetY = 0;
                        if ( xf.HasInverse )
                        {
                            da = da.Clone();
                            da.StylusTipTransform = xf;
                        }
                    }

                    _cursorDrawingAttributes = InkCanvas.DefaultDrawingAttributes.Clone();
                    DpiScale dpi = this.InkCanvas.GetDpi();
                    _cachedPenCursor = PenCursorManager.GetPenCursor(da, false, (this.InkCanvas.FlowDirection == FlowDirection.RightToLeft), dpi.DpiScaleX, dpi.DpiScaleY);
                }

                return _cachedPenCursor;
            }
        }

        #endregion Private Methods


        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        // A flag indicates that the dynamic renderer needs to be reset when StylusInputs begins.
        private bool                                           _resetDynamicRenderer;

        private StylusPointCollection                           _stylusPoints;

        private bool                                            _userInitiated;

        /// <summary>
        /// We clone the InkCanvas.DefaultDrawingAttributes on down, in case they are
        /// changed mid-stroke
        /// </summary>
        private DrawingAttributes                               _strokeDrawingAttributes;

        /// <summary>
        /// The cached DrawingAttributes and Cursor instances for rendering the pen cursor.
        /// </summary>
        private DrawingAttributes                               _cursorDrawingAttributes;
        private Cursor                                          _cachedPenCursor;

        #endregion Private Fields
    }
}
