// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Element that represents InkCanvas selection
//


using MS.Utility;
using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Ink;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;

namespace MS.Internal.Ink
{
    /// <summary>
    /// InkCanvasSelection
    /// </summary>
    internal sealed class InkCanvasSelection
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// InkCanvasSelection has an internal constructor to prevent direct instantiation
        /// </summary>
        /// <param name="inkCanvas">inkCanvas</param>
        internal InkCanvasSelection(InkCanvas inkCanvas)
        {
            //validate
            if (inkCanvas == null)
            {
                throw new ArgumentNullException("inkCanvas");
            }
            _inkCanvas = inkCanvas;

            _inkCanvas.FeedbackAdorner.UpdateBounds(Rect.Empty);
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Internal Properties
        //
        //-------------------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns the collection of selected strokes
        /// </summary>
        internal StrokeCollection SelectedStrokes
        {
            get
            {
                if ( _selectedStrokes == null )
                {
                    _selectedStrokes = new StrokeCollection();
                    _areStrokesChanged = true;
                }

                return _selectedStrokes;
            }
        }

        /// <summary>
        /// Returns the collection of selected elements
        /// </summary>
        internal ReadOnlyCollection<UIElement> SelectedElements
        {
            get
            {
                if ( _selectedElements == null )
                {
                    _selectedElements = new List<UIElement>();
                }

                return new ReadOnlyCollection<UIElement>(_selectedElements);
            }
        }

        /// <summary>
        /// Indicates whether there is a selection in the current InkCanvas.
        /// </summary>
        internal bool HasSelection
        {
            get
            {
                return SelectedStrokes.Count != 0 || SelectedElements.Count != 0;
            }
        }

        /// <summary>
        /// Returns the selection bounds
        /// </summary>
        internal Rect SelectionBounds
        {
            get
            {
                return Rect.Union(GetStrokesBounds(), GetElementsUnionBounds());
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Start a feedback rubberband.
        /// This method is called from SelectionEditingBehavior.OnActivated
        /// </summary>
        /// <param name="feedbackRect"></param>
        /// <param name="activeSelectionHitResult"></param>
        internal void StartFeedbackAdorner(Rect feedbackRect, InkCanvasSelectionHitResult activeSelectionHitResult)
        {
            Debug.Assert( _inkCanvas.EditingCoordinator.UserIsEditing == true );
            Debug.Assert(activeSelectionHitResult != InkCanvasSelectionHitResult.None, "activeSelectionHitResult cannot be InkCanvasSelectionHitResult.None.");

            _activeSelectionHitResult = activeSelectionHitResult;

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_inkCanvas.InnerCanvas);

            // Until the next start, we are going to use this adorner regardless user will change feedback adorner or not during the editing.
            InkCanvasFeedbackAdorner feedbackAdorner = _inkCanvas.FeedbackAdorner;

            // The feedback adorner shouldn't have been connected to the adorner layer yet.
            Debug.Assert(VisualTreeHelper.GetParent(feedbackAdorner) == null,
                "feedbackAdorner shouldn't be added to tree.");

            // Now, attach the feedback adorner to the adorner layer. Then update its bounds
            adornerLayer.Add(feedbackAdorner);
            feedbackAdorner.UpdateBounds(feedbackRect);
        }

        /// <summary>
        /// This method updates the feedback rubberband.
        /// This method is called from SelectionEditingBehavior.OnMouseMove.
        /// </summary>
        /// <param name="feedbackRect"></param>
        internal void UpdateFeedbackAdorner(Rect feedbackRect)
        {
            Debug.Assert(VisualTreeHelper.GetParent(_inkCanvas.FeedbackAdorner)
                == AdornerLayer.GetAdornerLayer(_inkCanvas.InnerCanvas),
                "feedbackAdorner should have been added to tree.");

            // Update the feedback bounds
            _inkCanvas.FeedbackAdorner.UpdateBounds(feedbackRect);
        }

        /// <summary>
        /// Ends a feedback rubberband
        /// This method is called from SelectionEditingBehavior.OnMoveUp
        /// </summary>
        /// <param name="finalRectangle"></param>
        internal void EndFeedbackAdorner(Rect finalRectangle)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_inkCanvas.InnerCanvas);
            InkCanvasFeedbackAdorner feedbackAdorner = _inkCanvas.FeedbackAdorner;

            Debug.Assert(VisualTreeHelper.GetParent(feedbackAdorner) == adornerLayer,
                "feedbackAdorner should have been added to tree.");

            // Reset the feedback bounds and detach it from the adorner layer.
            feedbackAdorner.UpdateBounds(Rect.Empty);
            adornerLayer.Remove(feedbackAdorner);

            // Commit the new rectange of the selection.
            CommitChanges(finalRectangle, true);

            _activeSelectionHitResult = null;
}

        /// <summary>
        /// Set the new selection to this helper class.
        /// </summary>
        /// <param name="strokes"></param>
        /// <param name="elements"></param>
        /// <param name="raiseSelectionChanged"></param>
        internal void Select(StrokeCollection strokes, IList<UIElement> elements, bool raiseSelectionChanged)
        {
            // Can't be null.
            Debug.Assert(strokes != null && elements != null);

            //
            // check to see if we're different.  If not, there is no need to raise the changed event
            //
            bool strokesAreDifferent;
            bool elementsAreDifferent;
            int count = 0;
            SelectionIsDifferentThanCurrent(strokes, out strokesAreDifferent, elements, out elementsAreDifferent);

            // Besides the differences of both the stroks and the elements, there is a chance that the selected strokes and elements
            // have been removed from the InkCanvas. Meanwhile, we may still hold the reference of _inkCanvasSelection which is empty.
            // So, we should make sure to remove _inkCanvasSelection in this case.
            if ( strokesAreDifferent || elementsAreDifferent )
            {
                if ( strokesAreDifferent && SelectedStrokes.Count != 0 )
                {
                    // PERF-2006/05/02-WAYNEZEN,
                    // Unsubscribe the event fisrt so that reseting IsSelected won't call into our handler.
                    //
                    // quit listening to changes
                    //
                    QuitListeningToStrokeChanges();

                    count = SelectedStrokes.Count;
                    for ( int i = 0; i < count; i++ )
                    {
                        SelectedStrokes[i].IsSelected = false;
                    }
                }

                _selectedStrokes = strokes;
                _areStrokesChanged = true;
                _selectedElements = new List<UIElement>(elements);


                // PERF-2006/05/02-WAYNEZEN,
                // Update IsSelected property before adding the event handlers.
                // 
                // We don't have to render hollow strokes if the current mode isn't Select mode
                if ( _inkCanvas.ActiveEditingMode == InkCanvasEditingMode.Select )
                {
                    // 
                    // Updating the visuals of the hitted strokes.
                    count = strokes.Count;
                    for ( int i = 0; i < count; i++ )
                    {
                        strokes[i].IsSelected = true;
                    }
                }

                // Add a LayoutUpdated handler if it's necessary.
                UpdateCanvasLayoutUpdatedHandler();

                // Update our selection adorner manually
                UpdateSelectionAdorner();

                ListenToStrokeChanges();

                //
                // And finally... raise the SelectionChanged event
                //
                if ( raiseSelectionChanged )
                {
                    _inkCanvas.RaiseSelectionChanged(EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Our internal method to commit the current editing.
        /// Called by EndFeedbackAdorner or InkCanvas.PasteFromDataObject
        /// </summary>
        internal void CommitChanges(Rect finalRectangle, bool raiseEvent)
        {
            // The follow moving or resizing raises SelectionMove or SelectionResize event
            // The out-side code could throw exception in the their handlers. We use try/finally block to protect our status.
            Rect selectionBounds = SelectionBounds;

            // 
            // If the selection is cleared programmatically somehow during user interaction, we will get an empty source rect.
            // We should check if the source still valid here. If not, no selection needs to be changed.
            if ( selectionBounds.IsEmpty )
            {
                return;
            }

            try
            {
                QuitListeningToStrokeChanges( );

                if ( raiseEvent )
                {
                    //
                    // see if we resized
                    //
                    if ( !DoubleUtil.AreClose(finalRectangle.Height, selectionBounds.Height)
                        || !DoubleUtil.AreClose(finalRectangle.Width, selectionBounds.Width) )
                    {
                        //
                        // we resized
                        //
                        CommitResizeChange(finalRectangle);
                    }
                    else if ( !DoubleUtil.AreClose(finalRectangle.Top, selectionBounds.Top)
                        || !DoubleUtil.AreClose(finalRectangle.Left, selectionBounds.Left) )
                    {
                        //
                        // we moved
                        //
                        CommitMoveChange(finalRectangle);
                    }
                }
                else
                {
                    //
                    // Just perform the move without raising any events.
                    //
                    MoveSelection(selectionBounds, finalRectangle);
                }
            }
            finally
            {
                ListenToStrokeChanges( );
            }
        }

        /// <summary>
        /// Try to remove an element from the selected elements list.
        /// Called by:
        ///             OnCanvasLayoutUpdated
        /// </summary>
        /// <param name="removedElement"></param>
        internal void RemoveElement(UIElement removedElement)
        {
            // No selected element. Bail out
            if ( _selectedElements == null || _selectedElements.Count == 0 )
            {
                return;
            }

            if ( _selectedElements.Remove(removedElement) )
            {
                // The element existed and was removed successfully.
                // Now if there is no selected element, we should remove our LayoutUpdated handler.
                if ( _selectedElements.Count == 0 )
                {
                    UpdateCanvasLayoutUpdatedHandler();
                    UpdateSelectionAdorner();
                }
            }
        }

        /// <summary>
        /// UpdateElementBounds:
        ///     Called by InkCanvasSelection.MoveSelection
        /// </summary>
        /// <param name="element"></param>
        /// <param name="transform"></param>
        internal void UpdateElementBounds(UIElement element, Matrix transform)
        {
            UpdateElementBounds(element, element, transform);
        }

        /// <summary>
        /// UpdateElementBounds:
        ///     Called by   InkCanvasSelection.UpdateElementBounds
        ///                 ClipboardProcessor.CopySelectionInXAML
        /// </summary>
        /// <param name="originalElement"></param>
        /// <param name="updatedElement"></param>
        /// <param name="transform"></param>
        internal void UpdateElementBounds(UIElement originalElement, UIElement updatedElement, Matrix transform)
        {
            if ( originalElement.DependencyObjectType.Id == updatedElement.DependencyObjectType.Id )
            {
                // Get the transform from element to Canvas
                GeneralTransform elementToCanvas = originalElement.TransformToAncestor(_inkCanvas.InnerCanvas);

                //cast to a FrameworkElement, nothing inherits from UIElement besides it right now
                FrameworkElement frameworkElement = originalElement as FrameworkElement;
                Size size;
                Thickness thickness = new Thickness();
                if ( frameworkElement == null )
                {
                    // Get the element's render size.
                    size = originalElement.RenderSize;
                }
                else
                {
                    size = new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight);
                    thickness = frameworkElement.Margin;
                }

                Rect elementBounds = new Rect(0, 0, size.Width, size.Height);   // Rect in element space
                elementBounds = elementToCanvas.TransformBounds(elementBounds); // Rect in Canvas space

                // Now apply the matrix to the element bounds
                Rect newBounds = Rect.Transform(elementBounds, transform);

                if ( !DoubleUtil.AreClose(elementBounds.Width, newBounds.Width) )
                {
                    if ( frameworkElement == null )
                    {
                        Size newSize = originalElement.RenderSize;
                        newSize.Width = newBounds.Width;
                        updatedElement.RenderSize = newSize;
                    }
                    else
                    {
                        ((FrameworkElement)updatedElement).Width = newBounds.Width;
                    }
                }

                if ( !DoubleUtil.AreClose(elementBounds.Height, newBounds.Height) )
                {
                    if ( frameworkElement == null )
                    {
                        Size newSize = originalElement.RenderSize;
                        newSize.Height = newBounds.Height;
                        updatedElement.RenderSize = newSize;
                    }
                    else
                    {
                        ( (FrameworkElement)updatedElement ).Height = newBounds.Height;
                    }
                }

                double left = InkCanvas.GetLeft(originalElement);
                double top = InkCanvas.GetTop(originalElement);
                double right = InkCanvas.GetRight(originalElement);
                double bottom = InkCanvas.GetBottom(originalElement);

                Point originalPosition = new Point(); // Default as (0, 0)
                if ( !double.IsNaN(left) )
                {
                    originalPosition.X = left;
                }
                else if ( !double.IsNaN(right) )
                {
                    originalPosition.X = right;
                }

                if ( !double.IsNaN(top) )
                {
                    originalPosition.Y = top;
                }
                else if ( !double.IsNaN(bottom) )
                {
                    originalPosition.Y = bottom;
                }

                Point newPosition = originalPosition * transform;

                if ( !double.IsNaN(left) )
                {
                    InkCanvas.SetLeft(updatedElement, newPosition.X - thickness.Left);           // Left wasn't auto
                }
                else if ( !double.IsNaN(right) )
                {
                    // NOTICE-2005/05/05-WAYNEZEN
                    // Canvas.RightProperty means the distance between element right side and its parent Canvas
                    // right side. The same definition is applied to Canvas.BottomProperty
                    InkCanvas.SetRight(updatedElement, ( right - ( newPosition.X - originalPosition.X ) )); // Right wasn't not auto
                }
                else
                {
                    InkCanvas.SetLeft(updatedElement, newPosition.X - thickness.Left);           // Both Left and Right were aut. Modify Left by default.
                }

                if ( !double.IsNaN(top) )
                {
                    InkCanvas.SetTop(updatedElement, newPosition.Y - thickness.Top);           // Top wasn't auto
                }
                else if ( !double.IsNaN(bottom) )
                {
                    InkCanvas.SetBottom(updatedElement, ( bottom - ( newPosition.Y - originalPosition.Y ) ));      // Bottom wasn't not auto
                }
                else
                {
                    InkCanvas.SetTop(updatedElement, newPosition.Y - thickness.Top);           // Both Top and Bottom were aut. Modify Left by default.
                }
            }
            else
            {
                Debug.Assert(false, "The updatedElement has to be the same type as the originalElement.");
            }
        }

        internal void TransformStrokes(StrokeCollection strokes, Matrix matrix)
        {
            strokes.Transform(matrix, false /*Don't apply the transform to StylusTip*/);
        }

        internal InkCanvasSelectionHitResult HitTestSelection(Point pointOnInkCanvas)
        {
            // If Selection is moving or resizing now, we should returns the active hit result.
            if ( _activeSelectionHitResult.HasValue )
            {
                return _activeSelectionHitResult.Value;
            }

            // No selection at all. Just return None.
            if ( !HasSelection )
            {
                return InkCanvasSelectionHitResult.None;
            }

            // Get the transform from InkCanvas to SelectionAdorner
            GeneralTransform inkCanvasToSelectionAdorner = _inkCanvas.TransformToDescendant(_inkCanvas.SelectionAdorner);
            Point pointOnSelectionAdorner = inkCanvasToSelectionAdorner.Transform(pointOnInkCanvas);

            InkCanvasSelectionHitResult hitResult = _inkCanvas.SelectionAdorner.SelectionHandleHitTest(pointOnSelectionAdorner);

            // If the hit test returns Selection and there is one and only one element is selected.
            // We have to check whether we hit on inside the element.
            if ( hitResult == InkCanvasSelectionHitResult.Selection
                && SelectedElements.Count == 1
                && SelectedStrokes.Count == 0 )
            {
                GeneralTransform transformToInnerCanvas = _inkCanvas.TransformToDescendant(_inkCanvas.InnerCanvas);
                Point pointOnInnerCanvas = transformToInnerCanvas.Transform(pointOnInkCanvas);

                // Try to find out whether we hit the single selement. If so, just return None.
                if ( HasHitSingleSelectedElement(pointOnInnerCanvas) )
                {
                    hitResult = InkCanvasSelectionHitResult.None;
                }
            }

            return hitResult;
        }

        /// <summary>
        /// Private helper to used to determine if the current selection
        /// is different than the selection passed
        /// </summary>
        /// <param name="strokes">strokes</param>
        /// <param name="strokesAreDifferent">strokesAreDifferent</param>
        /// <param name="elements">elements</param>
        /// <param name="elementsAreDifferent">elementsAreDifferent</param>
        internal void SelectionIsDifferentThanCurrent(StrokeCollection strokes,
                                                    out bool strokesAreDifferent,
                                                    IList<UIElement> elements,
                                                    out bool elementsAreDifferent)
        {
            strokesAreDifferent = false;
            elementsAreDifferent = false;

            if ( SelectedStrokes.Count == 0 )
            {
                if ( strokes.Count > 0 )
                {
                    strokesAreDifferent = true;
                }
            }
            else if ( !InkCanvasSelection.StrokesAreEqual(SelectedStrokes, strokes) )
            {
                strokesAreDifferent = true;
            }

            if ( SelectedElements.Count == 0 )
            {
                if ( elements.Count > 0 )
                {
                    elementsAreDifferent = true;
                }
            }
            else if ( !InkCanvasSelection.FrameworkElementArraysAreEqual(elements, SelectedElements) )
            {
                elementsAreDifferent = true;
            }
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// HasHitSingleSelectedElement
        /// This method hits test against the current selection. If the point is inside the single selected element,
        /// it will return true. Otherwise, return false.
        /// </summary>
        /// <param name="pointOnInnerCanvas"></param>
        /// <returns></returns>
        private bool HasHitSingleSelectedElement(Point pointOnInnerCanvas)
        {
            bool hasHit = false;

            if ( SelectedElements.Count == 1 )
            {
                IEnumerator<Rect> enumerator = SelectedElementsBoundsEnumerator.GetEnumerator();
                if ( enumerator.MoveNext() )
                {
                    Rect elementRect = enumerator.Current;
                    hasHit = elementRect.Contains(pointOnInnerCanvas);
                }
                else
                {
                    Debug.Assert(false, "An unexpected single selected Element");
                }
            }

            return hasHit;
        }

        /// <summary>
        /// Private helper to stop listening to stroke changes
        /// </summary>
        private void QuitListeningToStrokeChanges()
        {
            if ( _inkCanvas.Strokes != null )
            {
                _inkCanvas.Strokes.StrokesChanged -= new StrokeCollectionChangedEventHandler(this.OnStrokeCollectionChanged);
            }

            foreach ( Stroke s in SelectedStrokes )
            {
                s.Invalidated -= new EventHandler(this.OnStrokeInvalidated);
            }
        }

        /// <summary>
        /// Private helper to listen to stroke changes
        /// </summary>
        private void ListenToStrokeChanges()
        {
            if ( _inkCanvas.Strokes != null )
            {
                _inkCanvas.Strokes.StrokesChanged += new StrokeCollectionChangedEventHandler(this.OnStrokeCollectionChanged);
            }

            foreach ( Stroke s in SelectedStrokes )
            {
                s.Invalidated += new EventHandler(this.OnStrokeInvalidated);
            }
        }

        /// <summary>
        /// Helper method that take a finalRectangle and raised the appropriate
        /// events on the InkCanvas.  Handles cancellation
        /// </summary>
        private void CommitMoveChange(Rect finalRectangle)
        {
            Rect selectionBounds = SelectionBounds;
            //
            //*** MOVED ***
            //
            InkCanvasSelectionEditingEventArgs args =
                new InkCanvasSelectionEditingEventArgs(selectionBounds, finalRectangle);

            _inkCanvas.RaiseSelectionMoving(args);

            if ( !args.Cancel )
            {
                if ( finalRectangle != args.NewRectangle )
                {
                    finalRectangle = args.NewRectangle;
                }

                //
                // perform the move
                //
                MoveSelection(selectionBounds, finalRectangle);

                //
                // raise the 'changed' event
                //
                _inkCanvas.RaiseSelectionMoved(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Helper method that take a finalRectangle and raised the appropriate
        /// events on the InkCanvas.  Handles cancellation
        /// </summary>
        private void CommitResizeChange(Rect finalRectangle)
        {
            Rect selectionBounds = SelectionBounds;
            //
            // *** RESIZED ***
            //
            InkCanvasSelectionEditingEventArgs args =
                new InkCanvasSelectionEditingEventArgs(selectionBounds, finalRectangle);

            _inkCanvas.RaiseSelectionResizing(args);

            if ( !args.Cancel )
            {
                if ( finalRectangle != args.NewRectangle )
                {
                    finalRectangle = args.NewRectangle;
                }

                //
                // perform the move
                //
                MoveSelection(selectionBounds, finalRectangle);

                //
                // raise the 'changed' event
                //
                _inkCanvas.RaiseSelectionResized(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Private helper that moves all selected elements from the previous location
        /// to the new one
        /// </summary>
        /// <param name="previousRect"></param>
        /// <param name="newRect"></param>
        private void MoveSelection(Rect previousRect, Rect newRect)
        {
            //
            // compute our transform, but first remove our artificial border
            //
            Matrix matrix = InkCanvasSelection.MapRectToRect(newRect, previousRect);

            //
            // transform the elements
            //
            int count = SelectedElements.Count;
            IList<UIElement> elements = SelectedElements;
            for ( int i = 0; i < count ; i++ )
            {
                UpdateElementBounds(elements[i], matrix);
            }

            //
            // transform the strokes
            //
            if ( SelectedStrokes.Count > 0 )
            {
                TransformStrokes(SelectedStrokes, matrix);
                // Flag the change
                _areStrokesChanged = true;
            }

            if ( SelectedElements.Count == 0 )
            {
                // If there is no element in the current selection, the inner canvas won't have layout changes.
                // So there is no LayoutUpdated event. We have to manually update the selection bounds.
                UpdateSelectionAdorner();
            }

            // 
            // Always bring the new selection rectangle into the current view when the InkCanvas is hosted inside a ScrollViewer.
            _inkCanvas.BringIntoView(newRect);
        }

        /// <summary>
        /// A handler which receives LayoutUpdated from the layout manager. Since the selected elements can be animated,
        /// moved, resized or even removed programmatically from the InkCanvas, we have to monitor
        /// the any layout changes caused by those actions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCanvasLayoutUpdated(object sender, EventArgs e)
        {
            Debug.Assert( SelectedElements.Count != 0, 
                "The handler only can be hooked up when there are elements being selected" );

            // Update the selection adorner when receive the layout changed
            UpdateSelectionAdorner();
        }

        /// <summary>
        /// Our own internal listener for packet changes
        /// </summary>
        private void OnStrokeInvalidated(object sender, EventArgs e)
        {
            OnStrokeCollectionChanged(sender, 
                new StrokeCollectionChangedEventArgs(new StrokeCollection(), new StrokeCollection()));
        }

        /// <summary>
        /// Our own internal listener for strokes changed.
        /// This is used so that if someone deletes or modifies a stroke
        /// we are currently displaying for selection, we can update our size
        /// </summary>
        private void OnStrokeCollectionChanged(object target, StrokeCollectionChangedEventArgs e)
        {
            // If the strokes only get added to the InkCanvas, we don't have to update our internal selected strokes.
            if ( e.Added.Count != 0 && e.Removed.Count == 0 )
            {
                return;
            }

            foreach (Stroke s in e.Removed)
            {
                if ( SelectedStrokes.Contains(s) )
                {
                    s.Invalidated -= new EventHandler(this.OnStrokeInvalidated);
                    s.IsSelected = false;

                    // Now remove the stroke from our private collection.
                    SelectedStrokes.Remove(s);
                }
            }

            // Mark the strokes change
            _areStrokesChanged = true;
            UpdateSelectionAdorner();
        }

        /// <summary>
        /// Get the cached bounding boxes to be updated.
        /// </summary>
        private Rect GetStrokesBounds()
        {
            // Update the strokes bounds if they are changed.
            if ( _areStrokesChanged )
            {
                _cachedStrokesBounds = SelectedStrokes.Count != 0 ? 
                                        SelectedStrokes.GetBounds( ) : Rect.Empty;
                _areStrokesChanged = false;
            }

            return _cachedStrokesBounds;
        }

        /// <summary>
        /// Get the bounding boxes of the selected elements.
        /// </summary>
        /// <returns></returns>
        private List<Rect> GetElementsBounds()
        {
            List<Rect> rects = new List<Rect>();

            if ( SelectedElements.Count != 0 )
            {
                // The private SelectedElementsBounds property will ensure the size is got after rendering's done.
                foreach ( Rect elementRect in SelectedElementsBoundsEnumerator )
                {
                    rects.Add(elementRect);
                }
            }

            return rects;
        }

        /// <summary>
        /// Get the union box of the selected elements.
        /// </summary>
        /// <returns></returns>
        private Rect GetElementsUnionBounds()
        {
            if ( SelectedElements.Count == 0 )
            {
                return Rect.Empty;
            }

            Rect elementsBounds = Rect.Empty;

            // The private SelectedElementsBounds property will ensure the size is got after rendering's done.
            foreach ( Rect elementRect in SelectedElementsBoundsEnumerator )
            {
                elementsBounds.Union(elementRect);
            }

            return elementsBounds;
        }

        /// <summary>
        /// Update the selection adorners state.
        /// The method is called by:
        ///         MoveSelection
        ///         OnCanvasLayoutUpdated
        ///         OnStrokeCollectionChanged
        ///         RemoveElement
        ///         Select
        /// </summary>
        private void UpdateSelectionAdorner()
        {
            // The Selection Adorner will be visible all the time unless the ActiveEditingMode is None.
            if ( _inkCanvas.ActiveEditingMode != InkCanvasEditingMode.None )
            {
                Debug.Assert(_inkCanvas.SelectionAdorner.Visibility == Visibility.Visible,
                    "SelectionAdorner should be always visible except under the None ActiveEditingMode");

                _inkCanvas.SelectionAdorner.UpdateSelectionWireFrame(GetStrokesBounds(), GetElementsBounds());
            }
            else
            {
                Debug.Assert(_inkCanvas.SelectionAdorner.Visibility == Visibility.Collapsed,
                    "SelectionAdorner should be collapsed except if the ActiveEditingMode is None");
            }
}

        /// <summary>
        /// Ensure the rendering is valid.
        /// Called by:
        ///         SelectedElementsBounds
        /// </summary>
        private void EnusreElementsBounds()
        {
            InkCanvasInnerCanvas innerCanvas = _inkCanvas.InnerCanvas;

            // Make sure the arrange is valid. If not, we invoke UpdateLayout now.
            if ( !innerCanvas.IsMeasureValid || !innerCanvas.IsArrangeValid )
            {
                innerCanvas.UpdateLayout();
            }
        }

        /// <summary>
        /// Utility for computing a transformation that maps one rect to another
        /// </summary>
        /// <param name="target">Destination Rectangle</param>
        /// <param name="source">Source Rectangle</param>
        /// <returns>Transform that maps source rectangle to destination rectangle</returns>
        private static Matrix MapRectToRect(Rect target, Rect source)
        {
            if(source.IsEmpty)
                throw new ArgumentOutOfRangeException("source", SR.Get(SRID.InvalidDiameter));
            /*
            In the horizontal direction:

                    M11*source.left  + Dx = target.left
                    M11*source.right + Dx = target.right

            Subtracting the equations yields:

                    M11*(source.right - source.left) = target.right - target.left
            hence:
                    M11 = (target.right - target.left) / (source.right - source.left)

            Having computed M11, solve the first equation for Dx:

                    Dx = target.left - M11*source.left
            */
            double m11 = target.Width / source.Width;
            double dx = target.Left - m11 * source.Left;

            // Now do the same thing in the vertical direction
            double m22 = target.Height / source.Height;
            double dy = target.Top - m22 * source.Top;
            /*
            We are looking for the transformation that takes one upright rectangle to
            another.  This involves neither rotation nor shear, so:
            */
            return new Matrix(m11, 0, 0, m22, dx, dy);
        }

        /// <summary>
        /// Adds/Removes the LayoutUpdated handler according to whether there is selected elements or not.
        /// </summary>
        private void UpdateCanvasLayoutUpdatedHandler()
        {
            // If there are selected elements, we should create a LayoutUpdated handler in order to monitor their arrange changes.
            // Otherwise, we don't have to listen the LayoutUpdated if there is no selected element at all.
            if ( SelectedElements.Count != 0 )
            {
                // Make sure we hook up the event handler.
                if ( _layoutUpdatedHandler == null )
                {
                    _layoutUpdatedHandler = new EventHandler(OnCanvasLayoutUpdated);
                    _inkCanvas.InnerCanvas.LayoutUpdated += _layoutUpdatedHandler;
                }
            }
            else
            {
                // Remove the handler if there is one.
                if ( _layoutUpdatedHandler != null )
                {
                    _inkCanvas.InnerCanvas.LayoutUpdated -= _layoutUpdatedHandler;
                    _layoutUpdatedHandler = null;
                }
            }
        }

        /// <summary>
        /// Private helper method used to determine if two stroke collection either
        /// 1) are both null
        /// or
        /// 2) both refer to the identical set of strokes
        /// </summary>
        private static bool StrokesAreEqual(StrokeCollection strokes1, StrokeCollection strokes2)
        {
            if ( strokes1 == null && strokes2 == null )
            {
                return true;
            }
            //
            // we know that one of the stroke collections is
            // not null.  If the other one is, they're not
            // equal
            //
            if ( strokes1 == null || strokes2 == null )
            {
                return false;
            }
            if ( strokes1.Count != strokes2.Count )
            {
                return false;
            }
            foreach ( Stroke s in strokes1 )
            {
                if ( !strokes2.Contains(s) )
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Private helper method used to determine if two collection either
        /// 1) are both null
        /// or
        /// 2) both refer to the identical set of elements
        /// </summary>
        private static bool FrameworkElementArraysAreEqual(IList<UIElement> elements1, IList<UIElement> elements2)
        {
            if ( elements1 == null && elements2 == null )
            {
                return true;
            }
            //
            // we know that one of the stroke collections is
            // not null.  If the other one is, they're not
            // equal
            //
            if ( elements1 == null || elements2 == null )
            {
                return false;
            }
            if ( elements1.Count != elements2.Count )
            {
                return false;
            }
            foreach ( UIElement e in elements1 )
            {
                if ( !elements2.Contains(e) )
                {
                    return false;
                }
            }
            return true;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Iterates the bounding boxes of the selected elements relevent to their parent (InnerCanvas)   
        /// </summary>
        private IEnumerable<Rect> SelectedElementsBoundsEnumerator
        {
            get
            {
                // Ensure the elements have been rendered completely.
                EnusreElementsBounds();

                InkCanvasInnerCanvas innerCanvas = _inkCanvas.InnerCanvas;
                foreach ( UIElement element in SelectedElements )
                {
                    // Get the transform from element to Canvas
                    GeneralTransform elementToCanvas = element.TransformToAncestor(innerCanvas);

                    // Get the element's render size.
                    Size size = element.RenderSize;

                    Rect rect = new Rect(0, 0, size.Width, size.Height);   // Rect in element space
                    rect = elementToCanvas.TransformBounds(rect);          // Rect in Canvas space

                    yield return rect;
                }
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Our logical parent.
        /// </summary>
        private InkCanvas                   _inkCanvas;

        /// <summary>
        /// The collection of strokes this selectedinkelement represents
        /// </summary>
        private StrokeCollection            _selectedStrokes;
        /// <summary>
        /// The last known rectangle before the current edit.  used to revert
        /// when events are cancelled and expose the value to the InkCanvas.GetSelectionBounds
        /// </summary>
        private Rect                        _cachedStrokesBounds;
        private bool                        _areStrokesChanged;

        /// <summary>
        /// The collection of elements this selectedinkelement represents
        /// </summary>
        private List<UIElement>             _selectedElements;
        private EventHandler                _layoutUpdatedHandler;

        private Nullable<InkCanvasSelectionHitResult> _activeSelectionHitResult;

        #endregion Private Fields
    }
}

