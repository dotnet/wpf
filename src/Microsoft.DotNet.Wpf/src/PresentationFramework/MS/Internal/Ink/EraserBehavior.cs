// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description:
// ErasingBehavior for Ink
// Features:
//


using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;
using Swi = System.Windows.Ink;

namespace MS.Internal.Ink
{
    /// <summary>
    /// Eraser Behavior
    /// </summary>
    internal sealed class EraserBehavior : StylusEditingBehavior
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        internal EraserBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
            : base(editingCoordinator, inkCanvas)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Overrides SwitchToMode as the following expectations
        /// 19. From EraseByPoint To InkAndGesture
        ///     After mode change ink is being collected. On StylusUp gesture event fires. If it’s not a gesture, StrokeCollected event fires.
        /// 20. From EraseByPoint To GestureOnly
        ///     After mode change ink is being collected. On StylusUp gesture event fires. Stroke gets removed on StylusUp even if it’s not a gesture.
        /// 21. From EraseByPoint To Ink
        ///     Ink collection starts when changing the mode.
        /// 22. From EraseByPoint To EraseByStroke
        ///     After mode change StrokeErasing is performed.
        /// 23. From EraseByPoint To Select
        ///     Lasso is drawn for all packets between StylusDown and StylusUp. Strokes/elements within the lasso will be selected.
        /// 24. From EraseByPoint To None
        ///     No erasing is performed after mode change.
        /// 25. From EraseByStroke To InkAndGesture
        ///     After mode change ink is being collected. On StylusUp gesture event fires. If it’s not a gesture, StrokeCollected event fires.
        /// 26. From EraseByStroke To GestureOnly
        ///     After mode change ink is being collected. On StylusUp gesture event fires. Stroke gets removed on StylusUp even if it’s not a gesture.
        /// 27. From EraseByStroke To EraseByPoint
        ///     After mode change PointErasing is performed.
        /// 28. From EraseByStroke To Ink
        ///     Ink collection starts when changing the mode.
        /// 29. From EraseByStroke To Select
        ///     Lasso is drawn for all packets between StylusDown and StylusUp. Strokes/elements within the lasso will be selected
        /// 30. From EraseByStroke To None
        ///     No erasing is performed after mode change.
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
                        // Commit the current behavior
                        Commit(true);

                        // Change the mode. The dynamic renderer will be reset automatically.
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                case InkCanvasEditingMode.EraseByPoint:
                case InkCanvasEditingMode.EraseByStroke:
                    {
                        Debug.Assert(_cachedEraseMode != mode);

                        // Commit the current behavior
                        Commit(true);

                        // Change the mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);

                        break;
                    }
                case InkCanvasEditingMode.Select:
                    {
                        // Make a copy of the current cached points.
                        StylusPointCollection cachedPoints = _stylusPoints != null ? 
                                                                _stylusPoints.Clone() : null;

                        // Commit the current behavior.
                        Commit(true);

                        // Change the Select mode
                        IStylusEditing newBehavior = EditingCoordinator.ChangeStylusEditingMode(this, mode);

                        if ( cachedPoints != null
                            // NOTICE-2006/04/27-WAYNEZEN,
                            // EditingCoordinator.ChangeStylusEditingMode raises external event.
                            // The user code could take any arbitrary action for instance calling InkCanvas.ReleaseMouseCapture()
                            // So there is no guarantee that we could receive the newBehavior.
                            && newBehavior != null )
                        {
                            // Now add the previous points to the lasso behavior
                            newBehavior.AddStylusPoints(cachedPoints, false/*userInitiated*/);
                        }

                        break;
                    }
                case InkCanvasEditingMode.None:
                    {
                        // Discard the collected ink.
                        Commit(true);

                        // Change to the None mode
                        EditingCoordinator.ChangeStylusEditingMode(this, mode);
                        break;
                    }
                default:
                    Debug.Assert(false, "Unknown InkCanvasEditingMode!");
                    break;
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            InkCanvasEditingMode newEraseMode = EditingCoordinator.ActiveEditingMode;
            Debug.Assert(newEraseMode == InkCanvasEditingMode.EraseByPoint
                            || newEraseMode == InkCanvasEditingMode.EraseByStroke);

            // Check whether we have to update cursor.
            if ( _cachedEraseMode != newEraseMode )
            {
                // EraseMode is changed
                _cachedEraseMode = newEraseMode;
                EditingCoordinator.InvalidateBehaviorCursor(this);
            }
            else if ( newEraseMode == InkCanvasEditingMode.EraseByPoint )
            {
                // Invalidate the PointEraser if we don't have the cache yet.
                bool isPointEraserCursorValid = _cachedStylusShape != null;

                // 
                // If the cached EraserShape is different from the current EraserShape, we shoud just reset the cache.
                // The new cursor will be generated when it's needed later.
                if ( isPointEraserCursorValid 
                    && ( _cachedStylusShape.Width != InkCanvas.EraserShape.Width
                        || _cachedStylusShape.Height != InkCanvas.EraserShape.Height
                        || _cachedStylusShape.Rotation != InkCanvas.EraserShape.Rotation
                        || _cachedStylusShape.GetType() != InkCanvas.EraserShape.GetType()) )
                {
                    Debug.Assert(_cachedPointEraserCursor != null, "_cachedPointEraserCursor shouldn't be null.");
                    ResetCachedPointEraserCursor();
                    isPointEraserCursorValid = false;
                }

                if ( !isPointEraserCursorValid )
                {
                    // EraserShape is changed when the new mode is EraseByPoint
                    EditingCoordinator.InvalidateBehaviorCursor(this);
                }
            }
        }

        /// <summary>
        /// StylusInputBegin
        /// </summary> 
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputBegin(StylusPointCollection stylusPoints, bool userInitiated)
        {
            //
            // get a disposable dynamic hit-tester and add event handler
            //
            _incrementalStrokeHitTester =
                                this.InkCanvas.Strokes.GetIncrementalStrokeHitTester(this.InkCanvas.EraserShape);


            if ( InkCanvasEditingMode.EraseByPoint == _cachedEraseMode )
            {
                _incrementalStrokeHitTester.StrokeHit += new StrokeHitEventHandler(OnPointEraseResultChanged);
            }
            else
            {
                //we're in stroke hit test mode
                _incrementalStrokeHitTester.StrokeHit += new StrokeHitEventHandler(OnStrokeEraseResultChanged);
            }

            _stylusPoints = new StylusPointCollection(stylusPoints.Description, 100);
            _stylusPoints.Add(stylusPoints);

            //
            // start erasing
            //
            _incrementalStrokeHitTester.AddPoints(stylusPoints);

            // 
            // Since InkCanvas will ignore the animated tranforms when it receives the property changes.
            // So we should update our cursor when the stylus is down if there are animated transforms applied to InkCanvas.
            if ( InkCanvasEditingMode.EraseByPoint == _cachedEraseMode )
            {
                // Call InvalidateBehaviorCursor at the end of the routine. The method will cause an external event fired.
                // So it should be invoked after we set up our states.
                EditingCoordinator.InvalidateBehaviorCursor(this);
            }
        }

        /// <summary>
        /// StylusInputContinue
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        /// <param name="userInitiated">true if the source eventArgs.UserInitiated flag was set to true</param>
        protected override void StylusInputContinue(StylusPointCollection stylusPoints, bool userInitiated)
        {
            _stylusPoints.Add(stylusPoints);

            _incrementalStrokeHitTester.AddPoints(stylusPoints);
        }

        /// <summary>
        /// StylusInputEnd
        /// </summary>
        /// <param name="commit">commit</param>
        protected override void StylusInputEnd(bool commit)
        {
            if ( InkCanvasEditingMode.EraseByPoint == _cachedEraseMode )
            {
                _incrementalStrokeHitTester.StrokeHit-= new StrokeHitEventHandler(OnPointEraseResultChanged);
            }
            else
            {
                //we're in stroke hit test mode
                _incrementalStrokeHitTester.StrokeHit -= new StrokeHitEventHandler(OnStrokeEraseResultChanged);
            }

            _stylusPoints = null;

            // 
            // Don't forget ending the current incremental hit testing.
            _incrementalStrokeHitTester.EndHitTesting();
            _incrementalStrokeHitTester = null;
        }

        /// <summary>
        /// Get eraser cursor.
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetCurrentCursor()
        {
            if ( InkCanvasEditingMode.EraseByPoint == _cachedEraseMode )
            {
                if ( _cachedPointEraserCursor == null )
                {
                    _cachedStylusShape = InkCanvas.EraserShape;

                    // 
                    // The eraser cursor should respect the InkCanvas' Transform properties as the pen tip.
                    Matrix xf = GetElementTransformMatrix();
                    if ( !xf.IsIdentity )
                    {
                        // Zero the offsets if the element's transform in invertable. 
                        // Otherwise fallback the matrix to the identity.
                        if ( xf.HasInverse )
                        {
                            xf.OffsetX = 0;
                            xf.OffsetY = 0;
                        }
                        else
                        {
                            xf = Matrix.Identity;
                        }
                    }
                    DpiScale dpi = this.InkCanvas.GetDpi();
                    _cachedPointEraserCursor = PenCursorManager.GetPointEraserCursor(_cachedStylusShape, xf, dpi.DpiScaleX, dpi.DpiScaleY);
                }

                return _cachedPointEraserCursor;
            }
            else
            {
                return PenCursorManager.GetStrokeEraserCursor();
            }
        }

        /// <summary>
        /// ApplyTransformToCursor
        /// </summary>
        protected override void OnTransformChanged()
        {
            // Reset the cached point eraser cursor.
            ResetCachedPointEraserCursor();
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Reset the cached point eraser cursor.
        /// </summary>
        private void ResetCachedPointEraserCursor()
        {
            _cachedPointEraserCursor = null;
            _cachedStylusShape = null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStrokeEraseResultChanged(object sender, StrokeHitEventArgs e)
        {
            Debug.Assert(null != e.HitStroke);

            bool fSucceeded = false;

            // The below code calls out StrokeErasing or StrokeErased event.
            // The out-side code could throw exception. 
            // We use try/finally block to protect our status.
            try
            {
                InkCanvasStrokeErasingEventArgs args = new InkCanvasStrokeErasingEventArgs(e.HitStroke);
                this.InkCanvas.RaiseStrokeErasing(args);

                if ( !args.Cancel )
                {
                    // Erase only if the event wasn't cancelled
                    InkCanvas.Strokes.Remove(e.HitStroke);
                    this.InkCanvas.RaiseInkErased();
                }

                fSucceeded = true;
            }
            finally
            {
                if ( !fSucceeded )
                {
                    // Abort the editing.
                    Commit(false);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPointEraseResultChanged(object sender, StrokeHitEventArgs e)
        {
            Debug.Assert(null != e.HitStroke, "e.HitStroke cannot be null");

            bool fSucceeded = false;

            // The below code might call out StrokeErasing or StrokeErased event.
            // The out-side code could throw exception. 
            // We use try/finally block to protect our status.
            try
            {
                InkCanvasStrokeErasingEventArgs args = new InkCanvasStrokeErasingEventArgs(e.HitStroke);
                this.InkCanvas.RaiseStrokeErasing(args);

                if ( !args.Cancel )
                {
                    // Erase only if the event wasn't cancelled
                    StrokeCollection eraseResult = e.GetPointEraseResults();
                    Debug.Assert(eraseResult != null, "eraseResult cannot be null");

                    StrokeCollection strokesToReplace = new StrokeCollection();
                    strokesToReplace.Add(e.HitStroke);

                    try
                    {
                        // replace or remove the stroke
                        if (eraseResult.Count > 0)
                        {
                            this.InkCanvas.Strokes.Replace(strokesToReplace, eraseResult);
                        }
                        else
                        {
                            this.InkCanvas.Strokes.Remove(strokesToReplace);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        //this can happen if someone sits in an event handler 
                        //for StrokeErasing and removes the stroke.  
                        //this to harden against failure here.
                        if (!ex.Data.Contains("System.Windows.Ink.StrokeCollection"))
                        {
                            //System.Windows.Ink.StrokeCollection didn't throw this, 
                            //we need to just throw the original exception
                            throw;
                        }
                    }


                    //raise ink erased
                    this.InkCanvas.RaiseInkErased();
                }

                fSucceeded = true;
            }
            finally
            {
                if ( !fSucceeded )
                {
                    // Abort the editing.
                    Commit(false);
                }
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private InkCanvasEditingMode            _cachedEraseMode;
        private IncrementalStrokeHitTester      _incrementalStrokeHitTester;
        private Cursor                          _cachedPointEraserCursor;
        private StylusShape                     _cachedStylusShape;
        private StylusPointCollection           _stylusPoints = null;

        #endregion Private Fields
    }
}
