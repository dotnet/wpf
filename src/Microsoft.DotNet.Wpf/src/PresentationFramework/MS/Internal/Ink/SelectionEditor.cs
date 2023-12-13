// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Provides basic ink editing
// Features:
//

//#define TRACE_MAJOR         // DO NOT LEAVE ENABLED IN CHECKED IN CODE
//#define TRACE_ADDITIONAL    // DO NOT LEAVE ENABLED IN CHECKED IN CODE

using MS.Internal;
using MS.Internal.Controls;
using System;
using System.Security;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Threading;
using System.Windows.Ink;

namespace MS.Internal.Ink
{
    /// <summary>
    ///     SelectionEditor
    /// </summary>
    internal class SelectionEditor : EditingBehavior
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// SelectionEditor constructor
        /// </summary>
        internal SelectionEditor(EditingCoordinator editingCoordinator, InkCanvas inkCanvas) : base (editingCoordinator, inkCanvas)
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// InkCanvas' Selection is changed
        /// </summary>
        internal void OnInkCanvasSelectionChanged()
        {
            Point currentPosition = Mouse.PrimaryDevice.GetPosition(InkCanvas.SelectionAdorner);
            UpdateSelectionCursor(currentPosition);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Attach to new scope as defined by this element.
        /// </summary>
        protected override void OnActivate()
        {
            // Add mouse event handles to InkCanvas.SelectionAdorner
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseDownEvent, new MouseButtonEventHandler(OnAdornerMouseButtonDownEvent));
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnAdornerMouseMoveEvent));
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseEnterEvent, new MouseEventHandler(OnAdornerMouseMoveEvent));
            InkCanvas.SelectionAdorner.AddHandler(Mouse.MouseLeaveEvent, new MouseEventHandler(OnAdornerMouseLeaveEvent));

            Point currentPosition = Mouse.PrimaryDevice.GetPosition(InkCanvas.SelectionAdorner);
            UpdateSelectionCursor(currentPosition);
        }

        /// <summary>
        /// Called when the SelectionEditor is detached
        /// </summary>
        protected override void OnDeactivate()
        {
            // Remove all event hanlders.
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseDownEvent, new MouseButtonEventHandler(OnAdornerMouseButtonDownEvent));
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseMoveEvent, new MouseEventHandler(OnAdornerMouseMoveEvent));
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseEnterEvent, new MouseEventHandler(OnAdornerMouseMoveEvent));
            InkCanvas.SelectionAdorner.RemoveHandler(Mouse.MouseLeaveEvent, new MouseEventHandler(OnAdornerMouseLeaveEvent));
}

        /// <summary>
        /// OnCommit
        /// </summary>
        /// <param name="commit"></param>
        protected override void OnCommit(bool commit)
        {
            // Nothing to do.
        }

        /// <summary>
        /// Retrieve the cursor
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetCurrentCursor()
        {
            // 
            // We only show the selection related cursor when the mouse is over the SelectionAdorner.
            // If mouse is outside of the SelectionAdorner, we let the system to pick up the default cursor.
            if ( InkCanvas.SelectionAdorner.IsMouseOver )
            {
                return PenCursorManager.GetSelectionCursor(_hitResult,
                    (this.InkCanvas.FlowDirection == FlowDirection.RightToLeft));
            }
            else
            {
                return null;
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods


        /// <summary>
        /// SelectionAdorner MouseButtonDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdornerMouseButtonDownEvent(object sender, MouseButtonEventArgs args)
        {
            // If the ButtonDown is raised by RightMouse, we should just bail out.
            if ( (args.StylusDevice == null && args.LeftButton != MouseButtonState.Pressed) )
            {
                return;
            }

            Point pointOnSelectionAdorner = args.GetPosition(InkCanvas.SelectionAdorner);

            InkCanvasSelectionHitResult hitResult = InkCanvasSelectionHitResult.None;

            // Check if we should start resizing/moving
            hitResult = HitTestOnSelectionAdorner(pointOnSelectionAdorner);
            if ( hitResult != InkCanvasSelectionHitResult.None )
            {
                // We always use MouseDevice for the selection editing.
                EditingCoordinator.ActivateDynamicBehavior(EditingCoordinator.SelectionEditingBehavior, args.Device);
            }
            else
            {
                //
                //push selection and we're done
                //
                // If the current captured device is Stylus, we should activate the LassoSelectionBehavior with
                // the Stylus. Otherwise, use mouse.
                EditingCoordinator.ActivateDynamicBehavior(EditingCoordinator.LassoSelectionBehavior,
                    args.StylusDevice != null ? args.StylusDevice : args.Device);
            }
        }

        /// <summary>
        /// Selection Adorner MouseMove
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdornerMouseMoveEvent(object sender, MouseEventArgs args)
        {
            Point pointOnSelectionAdorner = args.GetPosition(InkCanvas.SelectionAdorner);
            UpdateSelectionCursor(pointOnSelectionAdorner);
        }

        /// <summary>
        /// Adorner MouseLeave Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnAdornerMouseLeaveEvent(object sender, MouseEventArgs args)
        {
            // We are leaving the adorner, update our cursor.
            EditingCoordinator.InvalidateBehaviorCursor(this);
        }

        /// <summary>
        /// Hit test for the grab handles
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private InkCanvasSelectionHitResult HitTestOnSelectionAdorner(Point position)
        {
            InkCanvasSelectionHitResult hitResult = InkCanvasSelectionHitResult.None;
            
            if ( InkCanvas.InkCanvasSelection.HasSelection )
            {
                // First, do hit test on the adorner
                hitResult = InkCanvas.SelectionAdorner.SelectionHandleHitTest(position);

                // Now, check if ResizeEnabled or MoveEnabled has been set.
                // If so, reset the grab handle.
                if ( hitResult >= InkCanvasSelectionHitResult.TopLeft && hitResult <= InkCanvasSelectionHitResult.Left )
                {
                    hitResult = InkCanvas.ResizeEnabled ? hitResult : InkCanvasSelectionHitResult.None;
                }
                else if ( hitResult == InkCanvasSelectionHitResult.Selection )
                {
                    hitResult = InkCanvas.MoveEnabled ? hitResult : InkCanvasSelectionHitResult.None;
                }
            }

            return hitResult;
        }

        /// <summary>
        /// This method updates the cursor when the mouse is hovering ont the selection adorner.
        /// It is called from
        ///  OnAdornerMouseLeaveEvent
        ///  OnAdornerMouseEvent
        /// </summary>
        /// <param name="hitPoint">the handle is being hit</param>
        private void UpdateSelectionCursor(Point hitPoint)
        {
            InkCanvasSelectionHitResult hitResult = HitTestOnSelectionAdorner(hitPoint);
            if ( _hitResult != hitResult )
            {
                // Keep the current handle
                _hitResult = hitResult;
                EditingCoordinator.InvalidateBehaviorCursor(this);
            }
        }


        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private InkCanvasSelectionHitResult _hitResult;

        #endregion Private Fields
    }
}

