// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      ResizeEditingBehavior 
//



using System;
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Navigation;
using System.Windows.Media;
using System.Windows.Ink;

namespace MS.Internal.Ink
{
    /// <summary>
    /// SelectionEditingBehavior
    /// </summary>
    internal sealed class SelectionEditingBehavior : EditingBehavior
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// SelectionEditingBehavior constructor
        /// </summary>
        internal SelectionEditingBehavior(EditingCoordinator editingCoordinator, InkCanvas inkCanvas)
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
        ///     Attaching to the element, we get attached in StylusDown
        /// </summary>
        protected override void OnActivate()
        {
            _actionStarted = false;

            // Capture the mouse.
            InitializeCapture();

            // Hittest for the grab handle
            MouseDevice mouse = Mouse.PrimaryDevice;

            _hitResult = InkCanvas.SelectionAdorner.SelectionHandleHitTest(
                    mouse.GetPosition((IInputElement)(InkCanvas.SelectionAdorner)));

            Debug.Assert(_hitResult != InkCanvasSelectionHitResult.None);
            EditingCoordinator.InvalidateBehaviorCursor(this);

            // Get the current selection bounds.
            _selectionRect = InkCanvas.GetSelectionBounds( );

            // Set the initial tracking position and rectangle
            _previousLocation = mouse.GetPosition(InkCanvas.SelectionAdorner);
            _previousRect = _selectionRect;

            // Start the feedback rubber band.
            InkCanvas.InkCanvasSelection.StartFeedbackAdorner(_selectionRect, _hitResult);

            // Add handlers to the mouse events.
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp));
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnMouseMove));
            InkCanvas.SelectionAdorner.AddHandler(UIElement.LostMouseCaptureEvent,
                new MouseEventHandler(OnLostMouseCapture));
}

        /// <summary>
        /// Called when the ResizeEditingBehavior is detached
        /// </summary>
        protected override void OnDeactivate()
        {
            // Remove the mouse event handlers.
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp));
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnMouseMove));
            InkCanvas.SelectionAdorner.RemoveHandler(UIElement.LostMouseCaptureEvent,
                new MouseEventHandler(OnLostMouseCapture));
        }

        protected override void OnCommit(bool commit)
        {
            // Release the inputs.
            ReleaseCapture(true, commit);
        }

        protected override Cursor GetCurrentCursor()
        {
            return PenCursorManager.GetSelectionCursor(_hitResult,
                (this.InkCanvas.FlowDirection == FlowDirection.RightToLeft));
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// The handler of the MouseMove event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            // Get the current mouse location.
            Point curPoint = args.GetPosition(InkCanvas.SelectionAdorner);

            // Check if we have a mouse movement at all.
            if ( !DoubleUtil.AreClose(curPoint.X, _previousLocation.X)
                || !DoubleUtil.AreClose(curPoint.Y, _previousLocation.Y) )
            {
                // We won't start the move until we really see a movement.
                if ( !_actionStarted )
                {
                    _actionStarted = true;
                }

                // Get the new rectangle
                Rect newRect = ChangeFeedbackRectangle(curPoint);

                // Update the feedback rubber band and record the current tracking rectangle.
                InkCanvas.InkCanvasSelection.UpdateFeedbackAdorner(newRect);
                _previousRect = newRect;
            }
        }

        /// <summary>
        /// The handler of the MouseUp event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnMouseUp(object sender, MouseButtonEventArgs args)
        {
            // We won't start the move until we really see a movement.
            if ( _actionStarted )
            {
                _previousRect = ChangeFeedbackRectangle(args.GetPosition(InkCanvas.SelectionAdorner));
            }

            Commit(true);
        }

        /// <summary>
        /// InkCanvas.LostStylusCapture handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnLostMouseCapture(object sender, MouseEventArgs args)
        {
            // If user is editing, we have to commit the current operation and reset our state.
            if ( EditingCoordinator.UserIsEditing )
            {
                ReleaseCapture(false, true);
            }
        }


        /// <summary>
        /// Get the new feedback rectangle based on the location
        /// </summary>
        /// <param name="newPoint"></param>
        private Rect ChangeFeedbackRectangle(Point newPoint)
        {
            // First check to make sure that we are not going past the zero point on
            //  the original element in which we hit the resize handle.

            // If moving the left side -- don't go past the right side.
            if ( _hitResult == InkCanvasSelectionHitResult.TopLeft ||
                _hitResult == InkCanvasSelectionHitResult.BottomLeft ||
                _hitResult == InkCanvasSelectionHitResult.Left )
            {
                if ( newPoint.X > _selectionRect.Right - MinimumHeightWidthSize )
                {
                    newPoint.X = _selectionRect.Right - MinimumHeightWidthSize;
                }
            }

            // If moving the right side -- don't go past the left side.
            if ( _hitResult == InkCanvasSelectionHitResult.TopRight ||
                _hitResult == InkCanvasSelectionHitResult.BottomRight ||
                _hitResult == InkCanvasSelectionHitResult.Right )
            {
                if ( newPoint.X < _selectionRect.Left + MinimumHeightWidthSize )
                {
                    newPoint.X = _selectionRect.Left + MinimumHeightWidthSize;
                }
            }

            // If moving the top side -- don't go past the bottom side.
            if ( _hitResult == InkCanvasSelectionHitResult.TopLeft ||
                _hitResult == InkCanvasSelectionHitResult.TopRight ||
                _hitResult == InkCanvasSelectionHitResult.Top )
            {
                if ( newPoint.Y > _selectionRect.Bottom - MinimumHeightWidthSize )
                {
                    newPoint.Y = _selectionRect.Bottom - MinimumHeightWidthSize;
                }
            }

            // If moving the bottom side -- don't go past the top side.
            if ( _hitResult == InkCanvasSelectionHitResult.BottomLeft ||
                _hitResult == InkCanvasSelectionHitResult.BottomRight ||
                _hitResult == InkCanvasSelectionHitResult.Bottom )
            {
                if ( newPoint.Y < _selectionRect.Top + MinimumHeightWidthSize )
                {
                    newPoint.Y = _selectionRect.Top + MinimumHeightWidthSize;
                }
            }

            // Get the new boundary of the selection
            Rect newRect = CalculateRect(newPoint.X - _previousLocation.X, newPoint.Y - _previousLocation.Y);

            // Depends on the current grab handle, we record the tracking position accordingly.
            if ( _hitResult == InkCanvasSelectionHitResult.BottomRight ||
                _hitResult == InkCanvasSelectionHitResult.BottomLeft ||
                _hitResult == InkCanvasSelectionHitResult.TopRight ||
                _hitResult == InkCanvasSelectionHitResult.TopLeft ||
                _hitResult == InkCanvasSelectionHitResult.Selection )
            {
                _previousLocation.X = newPoint.X;
                _previousLocation.Y = newPoint.Y;
            }
            else if ( _hitResult == InkCanvasSelectionHitResult.Left ||
                _hitResult == InkCanvasSelectionHitResult.Right )
            {
                _previousLocation.X = newPoint.X;
            }
            else if ( _hitResult == InkCanvasSelectionHitResult.Top || 
                _hitResult == InkCanvasSelectionHitResult.Bottom )
            {
                _previousLocation.Y = newPoint.Y;
            }

            return newRect;
}

        /// <summary>
        ///     Resize a given element based on the grab handle
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private Rect CalculateRect(double x, double y)
        {
            Rect newRect = _previousRect;

            switch ( _hitResult )
            {
                case InkCanvasSelectionHitResult.BottomRight:
                    {
                        newRect = ExtendSelectionRight(newRect, x);
                        newRect = ExtendSelectionBottom(newRect, y);
                        break;
                    }
                case InkCanvasSelectionHitResult.Bottom:
                    {
                        newRect = ExtendSelectionBottom(newRect, y);
                        break;
                    }
                case InkCanvasSelectionHitResult.BottomLeft:
                    {
                        newRect = ExtendSelectionLeft(newRect, x);
                        newRect = ExtendSelectionBottom(newRect, y);
                        break;
                    }
                case InkCanvasSelectionHitResult.TopRight:
                    {
                        newRect = ExtendSelectionTop(newRect, y);
                        newRect = ExtendSelectionRight(newRect, x);
                        break;
                    }
                case InkCanvasSelectionHitResult.Top:
                    {
                        newRect = ExtendSelectionTop(newRect, y);
                        break;
                    }
                case InkCanvasSelectionHitResult.TopLeft:
                    {
                        newRect = ExtendSelectionTop(newRect, y);
                        newRect = ExtendSelectionLeft(newRect, x);
                        break;
                    }
                case InkCanvasSelectionHitResult.Left:
                    {
                        newRect = ExtendSelectionLeft(newRect, x);
                        break;
                    }
                case InkCanvasSelectionHitResult.Right:
                    {
                        newRect = ExtendSelectionRight(newRect, x);
                        break;
                    }
                case InkCanvasSelectionHitResult.Selection:
                    {
                        // Translate the rectangle
                        newRect.Offset(x, y);
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
                        break;
                    }
            }

            return newRect;
        }

        // ExtendSelectionLeft
        private static Rect ExtendSelectionLeft(Rect rect, double extendBy)
        {
            Rect newRect = rect;
            newRect.X += extendBy;
            newRect.Width -= extendBy;

            return newRect;
        }

        // ExtendSelectionTop
        private static Rect ExtendSelectionTop(Rect rect, double extendBy)
        {
            Rect newRect = rect;

            newRect.Y += extendBy;
            newRect.Height -= extendBy;

            return newRect;
        }

        // ExtendSelectionRight
        private static Rect ExtendSelectionRight(Rect rect, double extendBy)
        {
            Rect newRect = rect;

            newRect.Width += extendBy;

            return newRect;
        }

        // ExtendSelectionBottom
        private static Rect ExtendSelectionBottom(Rect rect, double extendBy)
        {
            Rect newRect = rect;

            newRect.Height += extendBy;

            return newRect;
        }

        /// <summary>
        /// Capture Mouse
        /// </summary>
        private void InitializeCapture()
        {
            Debug.Assert(EditingCoordinator.UserIsEditing == false, "Unexpect UserIsEditng state." );
            EditingCoordinator.UserIsEditing = true;
            InkCanvas.SelectionAdorner.CaptureMouse();
        }

        /// <summary>
        /// Release Mouse capture
        /// </summary>
        /// <param name="releaseDevice"></param>
        /// <param name="commit"></param>
        private void ReleaseCapture(bool releaseDevice, bool commit)
        {
            if ( EditingCoordinator.UserIsEditing )
            {
                EditingCoordinator.UserIsEditing = false;
                if ( releaseDevice )
                {
                    InkCanvas.SelectionAdorner.ReleaseMouseCapture();
                }

                SelfDeactivate();

                // End the rubber band. If the operation is committed, we set the selection to the tracking rectangle.
                // Otherwise, reset the selection to the original bounds.
                InkCanvas.InkCanvasSelection.EndFeedbackAdorner(commit ? _previousRect : _selectionRect);
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private const double MinimumHeightWidthSize = 16.0;
        private Point _previousLocation;
        private Rect _previousRect;
        private Rect _selectionRect;
        private InkCanvasSelectionHitResult _hitResult;
        private bool _actionStarted = false;

        #endregion Private Fields
    }
}
