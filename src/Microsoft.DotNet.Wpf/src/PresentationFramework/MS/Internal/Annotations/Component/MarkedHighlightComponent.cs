// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Implements a Highlight component that has markers at the end
//              It also listens to TextSelection.Changed and GotFocus/LostFocus
//              events of the TextContainerParent and activates/deactivates
//
//              See spec at StickyNoteControlSpec.mht
//

using System;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Annotations;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Input;
using System.Diagnostics;                           // Assert
using MS.Internal.Annotations.Anchoring;
using MS.Utility;


namespace MS.Internal.Annotations.Component
{
    /// <summary>
    /// Displays a highlight anchor with markers at the end that gets
    /// activated when selected or when Focused property is set.
    /// Focused property is provided for synchronization with external components like
    /// Sticky Note
    /// </summary>
    internal sealed class MarkedHighlightComponent : Canvas, IAnnotationComponent
    {
        //------------------------------------------------------
        //
        //  ctors
        //
        //------------------------------------------------------
        #region ctors

        /// <summary>
        /// Creates a new component
        /// </summary>
        /// <param name="type">owning component type </param>
        /// <param name="host">Dependency object to host IsActive and IsMouseOverAnchor DepenedencyProperties
        /// if host is null - set them on MarkedHighlightComponent itself</param>
        public MarkedHighlightComponent(XmlQualifiedName type, DependencyObject host)
            : base()
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            _DPHost = host == null ? this : host;
            ClipToBounds = false;

            //create anchor highlight. The second parameter controls
            // if we want to render the entire anchor or only the Text content
            // This applies specificaly to tables, figures and floaters.
            // Currently implementation of GetTightBoundingGeometryForTextPointers does not
            // allow us to render the entire cell so we pass true (means content only).
            HighlightAnchor = new HighlightComponent(1, true, type);
            Children.Add(HighlightAnchor);

            //we will add the markers when the attached annotation is added
            //when we know the attachment level
            _leftMarker = null;
            _rightMarker = null;

            _state = 0;
            SetState();
        }
        #endregion ctors


        //------------------------------------------------------
        //
        //  IAnnotationComponent implementation
        //
        //------------------------------------------------------
        #region IAnnotationComponent implementation


        #region Public Properties

        /// <summary>
        /// Return a copy of the list of IAttachedAnnotations held by this component
        /// </summary>
        public IList AttachedAnnotations
        {
            get
            {
                ArrayList list = new ArrayList();
                if (_attachedAnnotation != null)
                {
                    list.Add(_attachedAnnotation);
                }
                return list;
            }
        }

        /// <summary>
        /// Sets and gets the context this annotation component is hosted in.
        /// </summary>
        /// <value>Context this annotation component is hosted in</value>
        public PresentationContext PresentationContext
        {
            get
            {
                return _presentationContext;
            }

            set
            {
                _presentationContext = value;
            }
        }

        /// <summary>
        /// Sets and gets the Z-order of this component. NOP -
        /// Highlight does not have Z-order
        /// </summary>
        /// <value>Context this annotation component is hosted in</value>
        public int ZOrder
        {
            get
            {
                return -1;
            }

            set
            {
            }
        }

        /// <summary>
        /// Returns the one element the annotation component is attached to.
        /// </summary>
        /// <value></value>
        public UIElement AnnotatedElement
        {
            get
            {
                return _attachedAnnotation != null ? (_attachedAnnotation.Parent as UIElement) : null;
            }
        }

        /// <summary>
        /// When the value is set to true - the AnnotatedElement content has changed -
        /// save the value and update geometry.
        /// The value is set to false when the geometry is synced with the content
        /// </summary>
        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }

            set
            {
                _isDirty = value;
                if (value)
                    UpdateGeometry();
            }
        }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// There is no common transformation that can be used for all the children.
        /// Here we adjust the individual rendering transformation of each child and
        /// return the input transfor as a result
        /// </summary>
        /// <param name="transform">Transform to the AnnotatedElement.</param>
        /// <returns>Transform to the annotation component</returns>
        public GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            //we need to recalculate RenderingTransformations of the markers
            //according to their current positions in the TextView
            //after the content reflow
            if (_attachedAnnotation == null)
                throw new InvalidOperationException(SR.Get(SRID.InvalidAttachedAnnotation));

            HighlightAnchor.GetDesiredTransform(transform);

            return transform;
        }

        /// <summary>
        /// Add an attached annotation to the component. The attached anchor will be used to add
        /// a highlight to the appropriate TextContainer
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be added to the component</param>
        public void AddAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (_attachedAnnotation != null)
            {
                throw new ArgumentException(SR.Get(SRID.MoreThanOneAttachedAnnotation));
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedMHBegin);

            //save the attached annotation
            _attachedAnnotation = attachedAnnotation;

            //create anchor markers
            if ((attachedAnnotation.AttachmentLevel & AttachmentLevel.StartPortion) != 0)
                _leftMarker = CreateMarker(GetMarkerGeometry());
            if ((attachedAnnotation.AttachmentLevel & AttachmentLevel.EndPortion) != 0)
                _rightMarker = CreateMarker(GetMarkerGeometry());

            //create highlight, markers and register for TextSelection.Changed and other events
            RegisterAnchor();

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AddAttachedMHEnd);
        }

        /// <summary>
        /// Remove an attached annotation from the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation to be removed from the component</param>
        public void RemoveAttachedAnnotation(IAttachedAnnotation attachedAnnotation)
        {
            if (attachedAnnotation == null)
            {
                throw new ArgumentNullException("attachedAnnotation");
            }

            if (attachedAnnotation != _attachedAnnotation)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidAttachedAnnotation), "attachedAnnotation");
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedMHBegin);

            //unregister markers from the adorner layer aND LISTENERS
            CleanUpAnchor();

            _attachedAnnotation = null;

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.RemoveAttachedMHEnd);
        }

        /// <summary>
        /// Modify an attached annotation that is held by the component
        /// </summary>
        /// <param name="attachedAnnotation">The attached annotation after modification</param>
        /// <param name="previousAttachedAnchor">The attached anchor previously associated with the attached annotation.</param>
        /// <param name="previousAttachmentLevel">The previous attachment level of the attached annotation.</param>
        public void ModifyAttachedAnnotation(IAttachedAnnotation attachedAnnotation, object previousAttachedAnchor, AttachmentLevel previousAttachmentLevel)
        {
            throw new NotSupportedException(SR.Get(SRID.NotSupported));
        }

        #endregion Public Methods

        #endregion IAnnotationComponent implementation

        //------------------------------------------------------
        //
        //  Additional Public Properties
        //
        //------------------------------------------------------
        #region Additional Public Properties

        /// <summary>
        /// Used to sync with another component focus (like StickyNote)
        /// </summary>
        public bool Focused
        {
            set
            {
                byte oldState = _state;

                if (value)
                    _state |= FocusFlag;
                else
                    _state &= FocusFlagComplement;

                if ((_state == 0) != (oldState == 0))
                {
                    SetState();
                }
            }
        }

        /// <summary>
        /// Anchor brackets color
        /// </summary>
        public Brush MarkerBrush
        {
            set
            {
                SetValue(MarkedHighlightComponent.MarkerBrushProperty, value);
            }
        }

        //those properies are exposed to allow external components like StickyNote to synchronize their color
        // with MarkedHighlight colors
        public static DependencyProperty MarkerBrushProperty = DependencyProperty.Register("MarkerBrushProperty", typeof(Brush), typeof(MarkedHighlightComponent));

        /// <summary>
        /// Anchor brackets thickness
        /// </summary>
        public double StrokeThickness
        {
            set
            {
                SetValue(MarkedHighlightComponent.StrokeThicknessProperty, value);
            }
        }

        //A DP for StrokeThickness to bind marker thickness to.
        public static DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThicknessProperty", typeof(double), typeof(MarkedHighlightComponent));

        #endregion Additional Public Properties

        #region Inrernal Methods

        //------------------------------------------------------
        //
        //  Inrernal Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Set the TabIndex for the hosting element (StickyNote or other)
        /// </summary>
        /// <param name="index">tab index to be set</param>
        internal void SetTabIndex(int index)
        {
            if (_DPHost != null)
                KeyboardNavigation.SetTabIndex(_DPHost, index);
        }
        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private methods

        /// <summary>
        /// Marker transformation is calculated based on anchor position
        /// </summary>
        /// <param name="marker">marker Path element</param>
        /// <param name="anchor">marker anchor</param>
        /// <param name="baseAnchor">if anchor is the end anchor, pass the start one too so we can check for
        /// relative transform. If anchor is the start anchor baseAnchor is null</param>
        /// <param name="xScaleFactor">x-scale parameter of the ScaleTransform multiplier. if -1 theis will get the right bracket</param>
        private void SetMarkerTransform(Path marker, ITextPointer anchor, ITextPointer baseAnchor, int xScaleFactor)
        {
            //check if this marker is visible at all
            if (marker == null)
                return;

            Debug.Assert(anchor != null, "undefined anchor");
            Debug.Assert(marker.Data != null || marker.Data == Geometry.Empty, "undefined geometry");

            //marker.Data must be a GeometryGroup with 3 children
            GeometryGroup geometry = marker.Data as GeometryGroup;
            Debug.Assert(geometry != null, "unexpected geometry type");
            Debug.Assert(geometry.Children.Count == 3, "unexpected geometry children count");


            Rect markerRect = TextSelectionHelper.GetAnchorRectangle(anchor);

            if (markerRect == Rect.Empty)
                return;

            //transform of the body of the bracket
            double desiredBodyHeight = markerRect.Height - MarkerVerticalSpace - _bottomTailHeight - _topTailHeight;
            double scale = 0.0;
            double yScaleFactor = 0;
            if (desiredBodyHeight > 0)
            {
                scale = desiredBodyHeight / _bodyHeight;
                yScaleFactor = 1;
            }
            ScaleTransform bodyScale = new ScaleTransform(1, scale);
            TranslateTransform bodyOffset = new TranslateTransform(markerRect.X, markerRect.Y + _topTailHeight + MarkerVerticalSpace);
            TransformGroup bodyTransform = new TransformGroup();
            bodyTransform.Children.Add(bodyScale);
            bodyTransform.Children.Add(bodyOffset);

            //scale of the tails of the bracket
            ScaleTransform tailScale = new ScaleTransform(xScaleFactor, yScaleFactor);

            //bottom tail offset
            TranslateTransform bottomOffset = new TranslateTransform(markerRect.X, markerRect.Bottom - _bottomTailHeight);
            //top tail offset
            TranslateTransform topOffset = new TranslateTransform(markerRect.X, markerRect.Top + MarkerVerticalSpace);

            //bottom tail transform
            TransformGroup bottomTailTransform = new TransformGroup();
            bottomTailTransform.Children.Add(tailScale);
            bottomTailTransform.Children.Add(bottomOffset);

            //top tail transform
            TransformGroup topTailTransform = new TransformGroup();
            topTailTransform.Children.Add(tailScale);
            topTailTransform.Children.Add(topOffset);

            if (geometry.Children[0] != null)
                geometry.Children[0].Transform = topTailTransform;

            if (geometry.Children[1] != null)
                geometry.Children[1].Transform = bodyTransform;
            if (geometry.Children[2] != null)
                geometry.Children[2].Transform = bottomTailTransform;

            //add transform to base anchor if needed
            if (baseAnchor != null)
            {
                ITextView startView = TextSelectionHelper.GetDocumentPageTextView(baseAnchor);
                ITextView endView = TextSelectionHelper.GetDocumentPageTextView(anchor);
                if (startView != endView && startView.RenderScope != null && endView.RenderScope != null)
                {
                    geometry.Transform = (Transform)endView.RenderScope.TransformToVisual(startView.RenderScope);
                }
            }
        }

        /// <summary>
        /// Handles selected/unselected changes
        /// </summary>
        /// <param name="selected">true - selected/false - unselected</param>
        private void SetSelected(bool selected)
        {
            Debug.Assert(_uiParent != null, "No selection container");
            byte oldState = _state;

            if (selected && _uiParent.IsFocused)
                _state |= SelectedFlag;
            else
                _state &= SelectedFlagComplement;

            if ((_state == 0) != (oldState == 0))
            {
                SetState();
            }
        }

        /// <summary>
        /// Removes markers
        /// </summary>
        private void RemoveHighlightMarkers()
        {
            if (_leftMarker != null)
                Children.Remove(_leftMarker);
            if (_rightMarker != null)
                Children.Remove(_rightMarker);
            _leftMarker = null;
            _rightMarker = null;
        }

        /// <summary>
        /// Creates a HighlightComponent + 2 Path elements that mark the ends of the highlight.
        /// Add everything to the Children.
        /// Register for TextSelection.Changed and container parent GotFocus/LostFocus events
        /// </summary>
        private void RegisterAnchor()
        {
            //register for caret events
            TextAnchor anchor = _attachedAnnotation.AttachedAnchor as TextAnchor;
            if (anchor == null)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidAttachedAnchor));
            }

            ITextContainer textContainer = anchor.Start.TextContainer;
            Debug.Assert(textContainer != null, "TextAnchor does not belong to a TextContainer");

            HighlightAnchor.AddAttachedAnnotation(_attachedAnnotation);

            //this will set  correct transformations on the markers
            UpdateGeometry();

            //host in the appropriate adorner layer
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(AnnotatedElement); // note, GetAdornerLayer requires UIElement
            if (layer == null) throw new InvalidOperationException(SR.Get(SRID.NoPresentationContextForGivenElement, AnnotatedElement));

            AdornerPresentationContext.HostComponent(layer, this, AnnotatedElement, false);

            //register for selection changed
            _selection = textContainer.TextSelection as ITextRange;
            if (_selection != null)
            {
                //find the container's parent as well so we can listen to mouseMove events
                _uiParent = PathNode.GetParent(textContainer.Parent) as UIElement;

                RegisterComponent();

                //register for container's parent GotFocus/LostFocus
                if (_uiParent != null)
                {
                    _uiParent.GotKeyboardFocus += new KeyboardFocusChangedEventHandler(OnContainerGotFocus);
                    _uiParent.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnContainerLostFocus);
                    //check if it is currently selected
                    if (HighlightAnchor.IsSelected(_selection))
                        SetSelected(true);
                }
            }
        }

        /// <summary>
        /// Unregisters the markers, listeners etc
        /// </summary>
        private void CleanUpAnchor()
        {
            //unregister for selection changed
            if (_selection != null)
            {
                UnregisterComponent();

                //unregister for container focus events
                if (_uiParent != null)
                {
                    _uiParent.GotKeyboardFocus -= new KeyboardFocusChangedEventHandler(OnContainerGotFocus);
                    _uiParent.LostKeyboardFocus -= new KeyboardFocusChangedEventHandler(OnContainerLostFocus);
                }
            }

            //remove from host
            _presentationContext.RemoveFromHost(this, false);

            //clear highlight if any
            if (HighlightAnchor != null)
            {
                HighlightAnchor.RemoveAttachedAnnotation(_attachedAnnotation);
                Children.Remove(HighlightAnchor);
                HighlightAnchor = null;
                //remove the Markers as well
                RemoveHighlightMarkers();
            }
            _attachedAnnotation = null;
        }

        /// <summary>
        /// If active/inactive state changes - change colors and Invalidate visuals
        /// </summary>
        private void SetState()
        {
            if (_state == 0)
            {
                if (_highlightAnchor != null)
                    _highlightAnchor.Activate(false);
                MarkerBrush = new SolidColorBrush(DefaultMarkerColor);
                StrokeThickness = MarkerStrokeThickness;
                _DPHost.SetValue(StickyNoteControl.IsActiveProperty, false);
            }
            else
            {
                if (_highlightAnchor != null)
                    _highlightAnchor.Activate(true);
                MarkerBrush = new SolidColorBrush(DefaultActiveMarkerColor);
                StrokeThickness = ActiveMarkerStrokeThickness;
                _DPHost.SetValue(StickyNoteControl.IsActiveProperty, true);
            }
        }

        /// <summary>
        /// Creates one marker and wraps it up in an adorner
        /// </summary>
        /// <param name="geometry">marker geometry</param>
        /// <returns>The MarkerComponent</returns>
        private Path CreateMarker(Geometry geometry)
        {
            Path marker = new Path();
            marker.Data = geometry;

            //set activation binding
            Binding markerStroke = new Binding("MarkerBrushProperty");
            markerStroke.Source = this;
            marker.SetBinding(Path.StrokeProperty, markerStroke);
            Binding markerStrokeThickness = new Binding("StrokeThicknessProperty");
            markerStrokeThickness.Source = this;
            marker.SetBinding(Path.StrokeThicknessProperty, markerStrokeThickness);
            marker.StrokeEndLineCap = PenLineCap.Round;
            marker.StrokeStartLineCap = PenLineCap.Round;
            Children.Add(marker);

            return marker;
        }

        /// <summary>
        /// Register a MarkedHighlightComponent with the TextSelection. In order to handle
        /// highlights z-order we must have only one EventHandler
        /// for each TextSelection so we keep all the state objects along with the EventHandler
        /// in one entry of a Hashtable where the key is the selection
        /// </summary>
        private void RegisterComponent()
        {
            ComponentsRegister componentsRegister = (ComponentsRegister)_documentHandlers[_selection];
            if (componentsRegister == null)
            {
                //create a new selection entry
                componentsRegister = new ComponentsRegister(new EventHandler(OnSelectionChanged), new MouseEventHandler(OnMouseMove));
                _documentHandlers.Add(_selection, componentsRegister);
                _selection.Changed += componentsRegister.SelectionHandler;

                //register with the container's mousemove event
                if (_uiParent != null)
                {
                    _uiParent.MouseMove += componentsRegister.MouseMoveHandler;
                }
            }

            componentsRegister.Add(this);
        }

        /// <summary>
        /// Unregister a MarkedHighlightComponent with the Changed event of the _selection.
        /// If this is the last MarkedHighlightComponent remove the entry in the
        /// Hashtable and EventHandler.
        /// </summary>
        private void UnregisterComponent()
        {
            ComponentsRegister componentsRegister = (ComponentsRegister)_documentHandlers[_selection];
            Debug.Assert(componentsRegister != null, "The selection is not registered");
            componentsRegister.Remove(this);
            if (componentsRegister.Components.Count == 0)
            {
                _documentHandlers.Remove(_selection);
                _selection.Changed -= componentsRegister.SelectionHandler;
                if (_uiParent != null)
                {
                    _uiParent.MouseMove -= componentsRegister.MouseMoveHandler;
                }
            }
        }

        /// <summary>
        /// Updates the geometry of the component - this includes
        /// setting IsDirty flag of the highlight to true, and recalculating marker
        /// transformations
        /// </summary>
        private void UpdateGeometry()
        {
            if ((HighlightAnchor == null) || !(HighlightAnchor is IHighlightRange))
            {
                throw new Exception(SR.Get(SRID.UndefinedHighlightAnchor));
            }

            TextAnchor anchor = ((IHighlightRange)HighlightAnchor).Range;
            Debug.Assert(anchor != null, "wrong attachedAnchor");

            ITextPointer start = anchor.Start.CreatePointer(LogicalDirection.Forward);
            ITextPointer end = anchor.End.CreatePointer(LogicalDirection.Backward);

            FlowDirection startFlowDirection = GetTextFlowDirection(start);
            FlowDirection endFlowDirection = GetTextFlowDirection(end);

            SetMarkerTransform(_leftMarker, start, null, startFlowDirection == FlowDirection.LeftToRight ? 1 : -1);
            SetMarkerTransform(_rightMarker, end, start, endFlowDirection == FlowDirection.LeftToRight ? -1 : 1);

            // Highlight anchor might have changed too - mark it Dirty.
            HighlightAnchor.IsDirty = true;

            IsDirty = false;
        }

        /// <summary>
        /// Loads a Path element that will be used as a marker at the end of SN anchor.
        /// The element is loaded from a embeded resource
        /// </summary>
        /// <returns>the Geometry</returns>
        private Geometry GetMarkerGeometry()
        {
            // Load the GeometryGroup from a resource - not working yet
            //The GeometryGroup must contain 3 geometries one for top part which is not transformed
            // one fo r the middle part which will strech with the font size and one for the bottom part which is not
            //transforemd either. Each geometry must have non-zero height.
            /*IResourceLoader loader = new ResourceLoader() as IResourceLoader;
            System.Windows.Shapes.Path path = loader.LoadResource(SNBConstants.c_MarkerPathResource) as System.Windows.Shapes.Path;
             */

            GeometryGroup markerGeometry = new GeometryGroup();
            markerGeometry.Children.Add(new LineGeometry(new Point(0, 1), new Point(1, 0)));
            markerGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 50)));
            markerGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(1, 1)));
            //set pixel height of geometry
            _bodyHeight = markerGeometry.Children[1].Bounds.Height;
            _topTailHeight = markerGeometry.Children[0].Bounds.Height;
            _bottomTailHeight = markerGeometry.Children[2].Bounds.Height;

            return markerGeometry;
        }

        /// <summary>
        /// Checks if the passed position is over the text range and sets the
        /// IsMouseOver state accordingly
        /// </summary>
        /// <param name="position">the position</param>
        private void CheckPosition(ITextPointer position)
        {
            IHighlightRange range = _highlightAnchor;
            bool newState = range.Range.Contains(position);
            bool currentState = (bool)_DPHost.GetValue(StickyNoteControl.IsMouseOverAnchorProperty);

            if (newState != currentState)
            {
                _DPHost.SetValue(StickyNoteControl.IsMouseOverAnchorPropertyKey, newState);
            }
        }
        /// <summary>
        /// We need to track TextContainer parent GotFocus
        /// in order to activate the SN that are currently spanned by the selection
        /// </summary>
        /// <param name="sender">TextContainer parent (not used)</param>
        /// <param name="args">not used</param>
        private void OnContainerGotFocus(Object sender, KeyboardFocusChangedEventArgs args)
        {
            //check if we need to activate the SN again
            if ((HighlightAnchor != null) && HighlightAnchor.IsSelected(_selection))
                SetSelected(true);
        }

        /// <summary>
        /// We need to track TextContainer parent LostFocus
        /// events so we can deactivate all SN in this container
        /// </summary>
        /// <param name="sender">TextContainer parent (not used)</param>
        /// <param name="args">not used</param>
        private void OnContainerLostFocus(object sender, KeyboardFocusChangedEventArgs args)
        {
            //deactivate SN
            SetSelected(false);
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properies
        //
        //------------------------------------------------------
        #region Private Properies

        /// <summary>
        /// The highlight component
        /// </summary>
        internal HighlightComponent HighlightAnchor
        {
            get
            {
                return _highlightAnchor;
            }

            set
            {
                _highlightAnchor = value;
                if (_highlightAnchor != null)
                {
                    //set the default colors
                    _highlightAnchor.DefaultBackground = DefaultAnchorBackground;
                    _highlightAnchor.DefaultActiveBackground = DefaultActiveAnchorBackground;
                }
            }
        }

        #endregion Private Properies

        //------------------------------------------------------
        //
        //  Static Methods
        //
        //------------------------------------------------------
        #region Static Methods

        /// <summary>
        /// Determine the flow direction of the character referred to by the TextPointer.
        /// If the context of the pointer is not text, we use the FlowDirection of the
        /// containing element.
        /// </summary>
        /// <param name="pointer">pointer to get flow direction for</param>
        /// <returns>positive value means LeftToRight, negative value means RightToLeft</returns>
        private static FlowDirection GetTextFlowDirection(ITextPointer pointer)
        {
            Invariant.Assert(pointer != null, "Null pointer passed.");
            Invariant.Assert(pointer.IsAtInsertionPosition, "Pointer is not an insertion position");

            int sign = 0;
            FlowDirection flowDirection;

            LogicalDirection direction = pointer.LogicalDirection;

            TextPointerContext currentContext = pointer.GetPointerContext(direction);
            if ((currentContext == TextPointerContext.ElementEnd || currentContext == TextPointerContext.ElementStart) &&
                !TextSchema.IsFormattingType(pointer.ParentType))
            {
                // If the current pointer (with its current direction) is at the start or end of a paragraph,
                // the next insertion position will be in a separate paragraph.  We can't determine direction
                // based on the pointer rects in that case so we use the direction of the Paragraph.
                flowDirection = (FlowDirection)pointer.GetValue(FlowDirectionProperty);
            }
            else
            {
                // We get the rects before and after the character following the current
                // pointer and determine that character's flow direction based on the x-values
                // of the rects.  Because we use direction when requesting a rect, we should
                // always get rects that are on the same line (except for the case above)

                // Forward gravity for leading edge
                Rect current = TextSelectionHelper.GetAnchorRectangle(pointer);

                // Get insertion position after the current pointer
                ITextPointer nextPointer = pointer.GetNextInsertionPosition(direction);

                // There may be no more insertion positions in this document
                if (nextPointer != null)
                {
                    // Backward gravity for trailing edge
                    nextPointer = nextPointer.CreatePointer(direction == LogicalDirection.Backward ? LogicalDirection.Forward : LogicalDirection.Backward);

                    // Special case - for pointers at the end of a paragraph
                    // Actually the pointer is the last insertion position in the paragraph and the next insertion position is the first
                    // in the next paragraph.  We handle this by detecting that the two pointers only have markup between them.  If the
                    // markup was only formatting, there would also be content (because insertion position would move past a character).
                    if (direction == LogicalDirection.Forward)
                    {
                        if (currentContext == TextPointerContext.ElementEnd && nextPointer.GetPointerContext(nextPointer.LogicalDirection) == TextPointerContext.ElementStart)
                        {
                            return (FlowDirection)pointer.GetValue(FlowDirectionProperty);
                        }
                    }
                    else
                    {
                        if (currentContext == TextPointerContext.ElementStart && nextPointer.GetPointerContext(nextPointer.LogicalDirection) == TextPointerContext.ElementEnd)
                        {
                            return (FlowDirection)pointer.GetValue(FlowDirectionProperty);
                        }
                    }


                    Rect next = TextSelectionHelper.GetAnchorRectangle(nextPointer);

                    // Calculate the difference in x-coordinate between the two rects
                    if (next != Rect.Empty && current != Rect.Empty)
                    {
                        sign = Math.Sign(next.Left - current.Left);

                        // If we are looking at the character before the current pointer,
                        // we swap the difference since "next" was actually before "current"
                        if (direction == LogicalDirection.Backward)
                        {
                            sign = -(sign);
                        }
                    }
                }

                // If the rects were at the same x-coordinate or we couldn't get rects
                // at all, we simply use the FlowDirection at that pointer.
                if (sign == 0)
                {
                    flowDirection = (FlowDirection)pointer.GetValue(FlowDirectionProperty);
                }
                else
                {
                    // Positive is left to right, negative is right to left
                    flowDirection = (sign > 0 ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
                }
            }

            return flowDirection;
        }

        /// <summary>
        /// We need to track selection changes in order to activate/deactivate
        /// MarkedHighlightComponent when its anchor is selected/unselected. In fact we register only one
        /// handler for all anchors on the same TextContainer because we need to process them together
        /// (see comments for _selectionStateHandlers Dictionary)
        /// </summary>
        /// <param name="sender">text selection</param>
        /// <param name="args">selection arguments</param>
        private static void OnSelectionChanged(object sender, EventArgs args)
        {
            ITextRange selection = sender as ITextRange;
            Debug.Assert(selection != null, "Unexpected sender of Changed event");

            //get all the anchors
            ComponentsRegister componentsRegister = (ComponentsRegister)_documentHandlers[selection];
            //Debug.Assert(stateHandler != null, "The selection is not registered");
            if (componentsRegister == null)
            {
                return;
            }
            List<MarkedHighlightComponent> components = componentsRegister.Components;
            Debug.Assert(components != null, "No SN registered for this selection");

            //get the state and deactivate. We do not want to activate now -
            //the activation should happen after all deactivation is done in order to
            //make sure that overlaping parts of active and unactive anchor remain active.
            bool[] active = new bool[components.Count];
            for (int i = 0; i < components.Count; i++)
            {
                Debug.Assert(components[i].HighlightAnchor != null, "Missing highlight anchor component");
                active[i] = components[i].HighlightAnchor.IsSelected(selection);
                if (!active[i])
                    components[i].SetSelected(false);
            }

            //now activate
            for (int i = 0; i < components.Count; i++)
            {
                if (active[i])
                    components[i].SetSelected(true);
            }
        }

        /// <summary>
        /// MouseMove event handler. Used to check if the mouse is over the textrange
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">mouse event params</param>
        private static void OnMouseMove(object sender, MouseEventArgs args)
        {
            Debug.Assert(sender != null, "undefined sender");

            //the sender must provide ITextView service in order to have this feature working
            IServiceProvider service = sender as IServiceProvider;
            if (service == null)
                return;

            ITextView textView = (ITextView)service.GetService(typeof(ITextView));
            if (textView == null || !textView.IsValid)
                return;

            //get mouse point
            Point currentPosition = Mouse.PrimaryDevice.GetPosition(textView.RenderScope);
            //now convert it to a text position
            ITextPointer pos = textView.GetTextPositionFromPoint(currentPosition, false);

            if (pos != null)
                CheckAllHighlightRanges(pos);
        }

        /// <summary>
        /// Scans through annotation components, registered with the TextContainer
        /// of this text pointer and checks if the mouse is over any of them
        /// </summary>
        /// <param name="pos">the text pointer to be checked</param>
        private static void CheckAllHighlightRanges(ITextPointer pos)
        {
            Debug.Assert(pos != null, "null text pointer");

            ITextContainer container = pos.TextContainer;
            ITextRange selection = container.TextSelection as ITextRange;
            if (selection == null)
                return;

            //now get the components registered with this selection
            ComponentsRegister registry = (ComponentsRegister)_documentHandlers[selection];

            if (registry == null)
                return;

            List<MarkedHighlightComponent> components = registry.Components;
            Debug.Assert((components != null) && (components.Count > 0), "invalid component registry");

            for (int i = 0; i < components.Count; i++)
            {
                components[i].CheckPosition(pos);
            }
        }

        #endregion Static Methods

        //-------------------------------------------------------------------------------
        //
        // Private Classes
        //
        //-------------------------------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// We need this to store the EventHandler along with the MarkedHighlightComponent array.
        /// We need to have the EventHandler so we can erase the same one no metter which
        /// MarkedHighlightComponent will be removed last
        /// </summary>
        private class ComponentsRegister
        {
            public ComponentsRegister(EventHandler selectionHandler, MouseEventHandler mouseMoveHandler)
            {
                Debug.Assert(selectionHandler != null, "SelectionHandler handler can not be null");
                Debug.Assert(mouseMoveHandler != null, "MouseMoveHandler handler can not be null");
                _components = new List<MarkedHighlightComponent>();
                _selectionHandler = selectionHandler;
                _mouseMoveHandler = mouseMoveHandler;
            }

            /// <summary>
            /// Adds the component to the list, sorted by the start point of the attachedAnchor.
            /// Sets its TabIndex and updates the TabIndexes of all components after this one.
            /// </summary>
            /// <param name="component">Component to be added</param>
            public void Add(MarkedHighlightComponent component)
            {
                Debug.Assert(component != null, "component is null");
                Debug.Assert(_components != null, "_components are null");

                //if this is the first component set KeyboardNavigation mode of the host
                if (_components.Count == 0)
                {
                    UIElement host = component.PresentationContext.Host;
                    if (host != null)
                    {
                        KeyboardNavigation.SetTabNavigation(host, KeyboardNavigationMode.Local);
                        KeyboardNavigation.SetControlTabNavigation(host, KeyboardNavigationMode.Local);
                    }
                }

                int i = 0;
                for (; i < _components.Count; i++)
                {
                    if (Compare(_components[i], component) > 0)
                        break;
                }

                _components.Insert(i, component);

                //update TabStopIndexes
                for (; i < _components.Count; i++)
                    _components[i].SetTabIndex(i);
            }

            /// <summary>
            /// Removes the component from the list and updates TabIndexes of the
            /// components after removed one.
            /// </summary>
            /// <param name="component">component to be removed</param>
            public void Remove(MarkedHighlightComponent component)
            {
                Debug.Assert(component != null, "component is null");
                Debug.Assert(_components != null, "_components are null");

                int ind = 0;
                for (; ind < _components.Count; ind++)
                {
                    if (_components[ind] == component)
                        break;
                }

                if (ind < _components.Count)
                {
                    _components.RemoveAt(ind);

                    //update TabIndexes
                    for (; ind < _components.Count; ind++)
                        _components[ind].SetTabIndex(ind);
                }
            }

            public List<MarkedHighlightComponent> Components
            {
                get
                {
                    return _components;
                }
            }

            public EventHandler SelectionHandler
            {
                get
                {
                    return _selectionHandler;
                }
            }

            public MouseEventHandler MouseMoveHandler
            {
                get
                {
                    return _mouseMoveHandler;
                }
            }

            /// <summary>
            /// Compares the anchors of two AnnotationComponents that have a ITextRange
            /// as attachedAnchor.
            /// </summary>
            /// <param name="first">first component</param>
            /// <param name="second">second component</param>
            /// <returns> less than 0 - first is before the second; == 0 - equal attached anchors; > 0 first is after the second</returns>
            /// <remarks>The comparison is based on the comparison of the Start TPs of the two AttachedAnchors
            /// If Start TPs are equal, the end TPs are compared. The result will be 0 only if
            /// both = Start and End TPs are equal. </remarks>
            private int Compare(IAnnotationComponent first, IAnnotationComponent second)
            {
                Debug.Assert(first != null, "first component is null");
                Debug.Assert((first.AttachedAnnotations != null) && (first.AttachedAnnotations.Count > 0), "first AttachedAnchor is null");
                Debug.Assert(second != null, "second component is null");
                Debug.Assert((second.AttachedAnnotations != null) && (second.AttachedAnnotations.Count > 0), "second AttachedAnchor is null");

                TextAnchor firstAnchor = ((IAttachedAnnotation)first.AttachedAnnotations[0]).FullyAttachedAnchor as TextAnchor;
                TextAnchor secondAnchor = ((IAttachedAnnotation)second.AttachedAnnotations[0]).FullyAttachedAnchor as TextAnchor;

                Debug.Assert(firstAnchor != null, " first TextAnchor is null");
                Debug.Assert(secondAnchor != null, " second TextAnchor is null");

                int res = firstAnchor.Start.CompareTo(secondAnchor.Start);
                if (res == 0)
                    res = firstAnchor.End.CompareTo(secondAnchor.End);

                return res;
            }

            private List<MarkedHighlightComponent> _components;
            private EventHandler _selectionHandler;
            private MouseEventHandler _mouseMoveHandler;
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Static Fields
        //
        //------------------------------------------------------
        #region Static fields

        //Colors of the MarkedHighlightComponent anchor
        internal static Color DefaultAnchorBackground = (Color)ColorConverter.ConvertFromString("#3380FF80");
        internal static Color DefaultMarkerColor = (Color)ColorConverter.ConvertFromString("#FF008000");
        internal static Color DefaultActiveAnchorBackground = (Color)ColorConverter.ConvertFromString("#3300FF00");
        internal static Color DefaultActiveMarkerColor = (Color)ColorConverter.ConvertFromString("#FF008000");
        internal static double MarkerStrokeThickness = 1;
        internal static double ActiveMarkerStrokeThickness = 2;
        internal static double MarkerVerticalSpace = 2;

        //We need to process all the anchors registered for the same selection
        // together, so we can ensure that we first deactivate every thing that needs to be
        // deactivated and then activate the segments to be activated. Otherwise it is possible
        // to live some of the overlaping segments drown as inactive even if they are active. This is used as
        // well for hadling MoseMove event in order to set IsMouseOverAnchor property
        // key is a TextSelection (which is unique per document, the value is of type ComponentRegister which contains an array of
        // MarkedHighlightComponent objects registered with this TextSelection along with the EventHandlers for the
        // TextSelection.Changed and document.MouseMove events
        private static Hashtable _documentHandlers = new Hashtable();

        #endregion Static fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private fields

        private byte _state;    //active or inactive
        private HighlightComponent _highlightAnchor = null;     // the HighlightComponent which visualizes the anchor
        private double _bodyHeight;                             // the height of the middle part of the anchor
        private double _bottomTailHeight;                       // the height of the bottom part of the bracket
        private double _topTailHeight;                          // the height of the top part of the bracket
        private Path _leftMarker;                               //The path element to visualize left bracket
        private Path _rightMarker;                              //The path element to visualize right bracket
        private DependencyObject _DPHost;                       //The DO that hosts IsActive and IsMouseOverAnchor DPs
        //defines the meaning of the bits in the _state member
        private const byte FocusFlag = 0x1;
        private const byte FocusFlagComplement = 0x7E;
        private const byte SelectedFlag = 0x2;
        private const byte SelectedFlagComplement = 0x7D;

        private IAttachedAnnotation _attachedAnnotation;
        private PresentationContext _presentationContext;
        private bool _isDirty = true; //shows if the annotation is in sync with the content of the AnnotatedElement


        //event sources
        private ITextRange _selection;          // we need to listen to selection.Changed event
        private UIElement _uiParent = null;     // the TextContainer parent. We need to handle GotFocus/LostFocus events

        #endregion Private fields
    }
}



