// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      A rendering element which binds to the strokes data
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Threading;
using System.Windows.Input;
using MS.Internal.Ink;
using System.Windows.Automation.Peers;

namespace System.Windows.Controls
{
    /// <summary>
    /// Renders the specified StrokeCollection data.
    /// </summary>
    public class InkPresenter : Decorator
    {                                        
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// The constructor of InkPresenter
        /// </summary>
        public InkPresenter()
        {        
            // Create the internal Renderer object.
            _renderer = new Ink.Renderer();

            SetStrokesChangedHandlers(this.Strokes, null);

            _contrastCallback = new InkPresenterHighContrastCallback(this);

            // Register rti high contrast callback. Then check whether we are under the high contrast already.
            HighContrastHelper.RegisterHighContrastCallback(_contrastCallback);
            if ( SystemParameters.HighContrast )
            {
                _contrastCallback.TurnHighContrastOn(SystemColors.WindowTextColor);
            }

            _constraintSize = Size.Empty;
        }

        #endregion Constructors

        //-------------------------------------------------------------------------------
        //
        // Public Methods
        //
        //-------------------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// AttachVisual method
        /// </summary>
        /// <param name="visual">The stroke visual which needs to be attached</param>
        /// <param name="drawingAttributes">The DrawingAttributes of the stroke</param>
        public void AttachVisuals(Visual visual, DrawingAttributes drawingAttributes)
        {
            VerifyAccess();

            EnsureRootVisual();
            _renderer.AttachIncrementalRendering(visual, drawingAttributes);
        }

        /// <summary>
        /// DetachVisual method
        /// </summary>
        /// <param name="visual">The stroke visual which needs to be detached</param>
        public void DetachVisuals(Visual visual)
        {
            VerifyAccess();

            EnsureRootVisual();
            _renderer.DetachIncrementalRendering(visual);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The DependencyProperty for the Strokes property.
        /// </summary>
        public static readonly DependencyProperty StrokesProperty =
                DependencyProperty.Register(
                        "Strokes",
                        typeof(StrokeCollection),
                        typeof(InkPresenter),
                        new FrameworkPropertyMetadata(
                                new StrokeCollectionDefaultValueFactory(),
                                new PropertyChangedCallback(OnStrokesChanged)),
                        (ValidateValueCallback)delegate(object value)
                            { return value != null; });

        /// <summary>
        /// Gets/Sets the Strokes property.
        /// </summary>
        public StrokeCollection Strokes
        {
            get { return (StrokeCollection)GetValue(StrokesProperty); }
            set { SetValue(StrokesProperty, value); }
        }

        private static void OnStrokesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InkPresenter inkPresenter = (InkPresenter)d;

            StrokeCollection oldValue = (StrokeCollection)e.OldValue;
            StrokeCollection newValue = (StrokeCollection)e.NewValue;

            inkPresenter.SetStrokesChangedHandlers(newValue, oldValue);
            inkPresenter.OnStrokeChanged(inkPresenter, EventArgs.Empty);
        }


        #endregion Public Properties

        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override of <seealso cref="FrameworkElement.MeasureOverride" />
        /// </summary>
        /// <param name="constraint">Constraint size.</param>
        /// <returns>Computed desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // No need to call VerifyAccess since we call the method on the base here.

            StrokeCollection strokes = Strokes;

            // Measure the child first
            Size newSize = base.MeasureOverride(constraint);

            // If there are strokes in IP, we need to combine the size to final size.
            if ( strokes != null && strokes.Count != 0 )
            {
                // Get the bounds of the stroks
                Rect boundingRect = StrokesBounds;

                // If we have an empty bounding box or the Right/Bottom value is negative,
                // an empty size will be returned.
                if ( !boundingRect.IsEmpty && boundingRect.Right > 0.0 && boundingRect.Bottom > 0.0 )
                {
                    // The new size needs to contain the right boundary and bottom boundary.
                    Size sizeStrokes = new Size(boundingRect.Right, boundingRect.Bottom);

                    newSize.Width = Math.Max(newSize.Width, sizeStrokes.Width);
                    newSize.Height = Math.Max(newSize.Height, sizeStrokes.Height);
                }
            }

            if ( Child != null )
            {
                _constraintSize = constraint;
            }
            else
            {
                _constraintSize = Size.Empty;
            }

            return newSize;
        }

        /// <summary>
        /// Override of <seealso cref="FrameworkElement.ArrangeOverride" />.
        /// </summary>
        /// <param name="arrangeSize">Size that element should use to arrange itself and its children.</param>
        /// <returns>The InkPresenter's desired size.</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            VerifyAccess();

            EnsureRootVisual();

            // 
            // When we arrange the child, we shouldn't count in the strokes' bounds. 
            // We only use the constraint size for the child.
            Size availableSize = arrangeSize;
            if ( !_constraintSize.IsEmpty )
            {
                availableSize = new Size(Math.Min(arrangeSize.Width, _constraintSize.Width), 
                                                Math.Min(arrangeSize.Height, _constraintSize.Height));
            }

            // We arrange our child as what Decorator does 
            // exceopt we are using the available size computed from our cached measure size.
            UIElement child = Child;
            if ( child != null )
            {
                child.Arrange(new Rect(availableSize));
            } 
            
            return arrangeSize;
        }

        /// <summary>
        /// The overridden GetLayoutClip method
        /// </summary>
        /// <returns>Geometry to use as additional clip if ClipToBounds=true</returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            // 
            // By default an FE will clip its content if the ink size exceeds the layout size (the final arrange size).
            // Since we are auto growing, the ink size is same as the desired size. So it ends up the strokes will be clipped
            // regardless ClipToBounds is set or not. 
            // We override the GetLayoutClip method so that we can bypass the default layout clip if ClipToBounds is set to false.
            // So we allow the ink to be drown anywhere when no clip is set.
            if ( ClipToBounds )
            {
                return base.GetLayoutClip(layoutSlotSize);
            }
            else
                return null;
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            int count = VisualChildrenCount;
                
            if(count == 2)
            {
                switch (index)
                {
                    case 0: 
                        return  base.Child;

                    case 1:
                        return _renderer.RootVisual;

                    default:
                        throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }
            }
            else if (index == 0 && count == 1)
            {
                if ( _hasAddedRoot )
                {
                    return _renderer.RootVisual;
                }
                else if(base.Child != null)
                {
                    return base.Child;
                }
            }
            throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
        }

        
        /// <summary>
        ///  Derived classes override this property to enable the Visual code to enumerate 
        ///  the Visual children. Derived classes need to return the number of children
        ///  from this method.
        ///
        ///    By default a Visual does not have any children.
        ///
        ///  Remark: During this virtual method the Visual tree must not be modified.
        /// </summary>        
        protected override int VisualChildrenCount
        {           
            get 
            {
                // we can have 4 states:-
                // 1. no children
                // 2. only base.Child
                // 3. only _renderer.RootVisual as the child
                // 4. both base.Child and  _renderer.RootVisual
                
                if(base.Child != null)
                {
                    if ( _hasAddedRoot )
                    {
                       return 2;
                    }
                    else 
                    {
                        return 1;
                    }
                }
                else if ( _hasAddedRoot )
                {
                    return 1;
                }
                
                return 0;
            }            
        }

        /// <summary>
        /// UIAutomation support
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InkPresenterAutomationPeer(this);
        }


        #endregion Protected Methods

        #region Internal Methods

        /// <summary>
        /// Internal helper used to indicate if a visual was previously attached 
        /// via a call to AttachIncrementalRendering
        /// </summary>
        internal bool ContainsAttachedVisual(Visual visual)
        {
            VerifyAccess();
            return _renderer.ContainsAttachedIncrementalRenderingVisual(visual);
        }


        /// <summary>
        /// Internal helper used to determine if a visual is in the right spot in the visual tree
        /// </summary>
        internal bool AttachedVisualIsPositionedCorrectly(Visual visual, DrawingAttributes drawingAttributes)
        {
            VerifyAccess();
            return _renderer.AttachedVisualIsPositionedCorrectly(visual, drawingAttributes);
        }

        #endregion Internal Methods
        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// A helper class for the high contrast support
        /// </summary>
        private class InkPresenterHighContrastCallback : HighContrastCallback
        {
            //------------------------------------------------------
            //
            //  Cnostructors
            //
            //------------------------------------------------------

            #region Constructors

            internal InkPresenterHighContrastCallback(InkPresenter inkPresenter)
            {
                _thisInkPresenter = inkPresenter;
            }

            private InkPresenterHighContrastCallback() { }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            /// <summary>
            /// TurnHighContrastOn
            /// </summary>
            /// <param name="highContrastColor"></param>
            internal override void TurnHighContrastOn(Color highContrastColor)
            {
                _thisInkPresenter._renderer.TurnHighContrastOn(highContrastColor);
                _thisInkPresenter.OnStrokeChanged();
            }

            /// <summary>
            /// TurnHighContrastOff
            /// </summary>
            internal override void TurnHighContrastOff()
            {
                _thisInkPresenter._renderer.TurnHighContrastOff();
                _thisInkPresenter.OnStrokeChanged();
            }

            #endregion Internal Methods

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------

            #region Internal Properties

            /// <summary>
            /// Returns the dispatcher if the object is associated to a UIContext.
            /// </summary>
            internal override Dispatcher Dispatcher
            {
                get
                {
                    return _thisInkPresenter.Dispatcher;
                }
            }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private InkPresenter _thisInkPresenter;

            #endregion Private Fields
        }

        #endregion Private Classes

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        private void SetStrokesChangedHandlers(StrokeCollection newStrokes, StrokeCollection oldStrokes)
        {
            Debug.Assert(newStrokes != null, "Cannot set a null to InkPresenter");

            // Remove the event handlers from the old stroke collection
            if ( null != oldStrokes )
            {
                // Stop listening on events from the stroke collection.
                oldStrokes.StrokesChanged -= new StrokeCollectionChangedEventHandler(OnStrokesChanged);
            }

            // Start listening on events from the stroke collection.
            newStrokes.StrokesChanged += new StrokeCollectionChangedEventHandler(OnStrokesChanged);

            // Replace the renderer stroke collection.
            _renderer.Strokes = newStrokes;

            SetStrokeChangedHandlers(newStrokes, oldStrokes);
        }

        /// <summary>
        /// StrokeCollectionChanged event handler
        /// </summary>
        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs eventArgs)
        {
            System.Diagnostics.Debug.Assert(sender == this.Strokes);

            SetStrokeChangedHandlers(eventArgs.Added, eventArgs.Removed);
            OnStrokeChanged(this, EventArgs.Empty);
        }

        private void SetStrokeChangedHandlers(StrokeCollection addedStrokes, StrokeCollection removedStrokes)
        {
            Debug.Assert(addedStrokes != null, "The added StrokeCollection cannot be null.");
            int count, i;

            if ( removedStrokes != null )
            {
                // Deal with removed strokes first
                count = removedStrokes.Count;
                for ( i = 0; i < count; i++ )
                {
                    StopListeningOnStrokeEvents(removedStrokes[i]);
                }
            }

            // Add new strokes
            count = addedStrokes.Count;
            for ( i = 0; i < count; i++ )
            {
                StartListeningOnStrokeEvents(addedStrokes[i]);
            }
        }

        private void OnStrokeChanged(object sender, EventArgs e)
        {
            OnStrokeChanged();
        }

        /// <summary>
        /// The method is called from Stroke setter, OnStrokesChanged, DrawingAttributesChanged and OnPacketsChanged
        /// </summary>
        private void OnStrokeChanged()
        {
            // Recalculate the bound of the StrokeCollection
            _cachedBounds = null;
            // Invalidate the current measure when any change happens on the stroke or strokes.
            InvalidateMeasure();
        }

        /// <summary>
        /// Attaches event handlers to stroke events
        /// </summary>
        private void StartListeningOnStrokeEvents(Stroke stroke)
        {
            System.Diagnostics.Debug.Assert(stroke != null);
            stroke.Invalidated += new EventHandler(OnStrokeChanged);
        }

        /// <summary>
        /// Detaches event handlers from stroke
        /// </summary>
        private void StopListeningOnStrokeEvents(Stroke stroke)
        {
            System.Diagnostics.Debug.Assert(stroke != null);
            stroke.Invalidated -= new EventHandler(OnStrokeChanged);
        }

        /// <summary>
        /// Ensure the renderer root to be connected. The method is called from
        ///     AttachVisuals
        ///     DetachVisuals
        ///     ArrangeOverride
        /// </summary>
        private void EnsureRootVisual()
        {
            if ( !_hasAddedRoot )
            {
                // Ideally we should set _hasAddedRoot to true before calling AddVisualChild.
                // So that VisualDiagnostics.OnVisualChildChanged can get correct child index.
                // However it is not clear what can regress. For now we'll temporary set
                // _parentIndex property. We'll use _parentIndex.
                // Note that there is a comment in Visual.cs stating that _parentIndex should
                // be set to -1 in DEBUG builds when child is removed. We are not going to 
                // honor it. There is no _parentIndex == -1 validation is performed anywhere.
                _renderer.RootVisual._parentIndex = 0;

                AddVisualChild(_renderer.RootVisual);
                _hasAddedRoot = true;
            }
        }

        #endregion Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------------------

        #region Private Properties

        private Rect StrokesBounds
        {
            get
            {
                if ( _cachedBounds == null )
                {
                    _cachedBounds = Strokes.GetBounds();
                }

                return _cachedBounds.Value;
            }
        }

        #endregion Private Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields
        private Ink.Renderer    _renderer;
        private Nullable<Rect>  _cachedBounds = null;
        private bool            _hasAddedRoot;
        //
        // HighContrast support
        //
        private InkPresenterHighContrastCallback    _contrastCallback;

        private Size                                _constraintSize;

        #endregion Private Fields
    }
}


