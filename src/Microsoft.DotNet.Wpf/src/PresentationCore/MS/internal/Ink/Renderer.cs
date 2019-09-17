// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.Windows;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using MS.Internal.Ink;
using MS.Internal;
using MS.Internal.PresentationCore;

namespace System.Windows.Ink
{
    /// <summary>
    /// The Renderer class is used to render a stroke collection.
    /// This class listens to stroke added and removed events on the
    /// stroke collection and updates the internal visual tree.
    /// It also listens to the Invalidated event on Stroke (fired when
    /// DrawingAttributes changes, packet data changes, DrawingAttributes
    /// replaced, as well as Stroke developer calls Stroke.OnInvalidated)
    /// and updates the visual state as necessary.
    /// </summary>
    ///
    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal class Renderer
    {
        #region StrokeVisual

        /// <summary>
        /// A retained visual for rendering a single stroke in a stroke collection view
        /// </summary>
        private class StrokeVisual : System.Windows.Media.DrawingVisual
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="stroke">a stroke to render into this visual</param>
            /// <param name="renderer">a renderer associated to this visual</param>
            internal StrokeVisual(Stroke stroke, Renderer renderer) : base()
            {
                Debug.Assert(renderer != null);

                if (stroke == null)
                {
                    throw new System.ArgumentNullException("stroke");
                }

                _stroke = stroke;
                _renderer = renderer;

                // The original value of the color and IsHighlighter are cached so
                // when Stroke.Invalidated is fired, Renderer knows whether re-arranging
                // the visual tree is needed.
                _cachedColor = stroke.DrawingAttributes.Color;
                _cachedIsHighlighter = stroke.DrawingAttributes.IsHighlighter;

                // Update the visual contents
                Update();
            }

            /// <summary>
            /// The Stroke rendedered into this visual.
            /// </summary>
            internal Stroke Stroke
            {
                get { return _stroke; }
            }
            
            /// <summary>
            /// Updates the contents of the visual.
            /// </summary>
            internal void Update()
            {
                using (DrawingContext drawingContext = RenderOpen())
                {
                    bool highContrast = _renderer.IsHighContrast();

                    if (highContrast == true && _stroke.DrawingAttributes.IsHighlighter)
                    {
                        // we don't render highlighters in high contrast
                        return;
                    }

                    DrawingAttributes da;
                    if (highContrast)
                    {
                        da = _stroke.DrawingAttributes.Clone();
                        da.Color = _renderer.GetHighContrastColor();
                    }
                    else if (_stroke.DrawingAttributes.IsHighlighter == true)
                    {
                        // Get the drawing attributes to use for a highlighter stroke. This can be a copied DA with color.A
                        // overridden if color.A != 255.
                        da = StrokeRenderer.GetHighlighterAttributes(_stroke, _stroke.DrawingAttributes);
                    }
                    else
                    {
                        // Otherwise, usethe DA on this stroke
                        da = _stroke.DrawingAttributes;
                    }

                    // Draw selected stroke as hollow
                    _stroke.DrawInternal (drawingContext, da, _stroke.IsSelected );
                }
            }

            /// <summary>
            /// StrokeVisual should not be hittestable as it interferes with event routing
            /// </summary>
            protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParams)
            {
                return null;
            }

            /// <summary>
            /// The previous value of IsHighlighter
            /// </summary>
            internal bool CachedIsHighlighter
            {
                get {return _cachedIsHighlighter;}
                set {_cachedIsHighlighter = value;}
            }

            /// <summary>
            /// The previous value of Color
            /// </summary>
            internal Color CachedColor
            {
                get {return _cachedColor;}
                set {_cachedColor = value;}
            }

            private Stroke                      _stroke;
            private bool                        _cachedIsHighlighter;
            private Color                       _cachedColor;
            private Renderer                    _renderer;
}

        /// <summary>
        /// Private helper that helps reverse map the highlighter dictionary
        /// </summary>
        private class HighlighterContainerVisual : ContainerVisual
        {
            internal HighlighterContainerVisual(Color color)
            {
                _color = color;
            }

            /// <summary>
            /// The Color of the strokes in this highlighter container visual
            /// </summary>
            internal Color Color
            {
                get { return _color; }
            }
            private Color _color;
        }

        #endregion

        #region Internal interface

        /// <summary>
        /// Public Constructor
        /// </summary>
        internal Renderer()
        {
            // Initialize the data members.
            // We intentionally don't use lazy initialization for the core members to avoid
            // hidden bug situations when one thing has been created while another not.
            // If the user is looking for lazy initialization, she should do that on her
            // own and create Renderer only when there's a need for it.

            // Create visuals that'll be the containers for all other visuals
            // created by the Renderer. This visuals are created once and are
            // not supposed to be replaced nor destroyed while the Renderer is alive.
            _rootVisual = new ContainerVisual();
            _highlightersRoot = new ContainerVisual();
            _regularInkVisuals = new ContainerVisual();
            _incrementalRenderingVisuals = new ContainerVisual();

            // Highlighters go to the bottom, then regular ink, and regular
            // ink' incremental rendering in on top.
            VisualCollection rootChildren = _rootVisual.Children;
            rootChildren.Add(_highlightersRoot);
            rootChildren.Add(_regularInkVisuals);
            rootChildren.Add(_incrementalRenderingVisuals);

            // Set the default value of highcontrast to be false.
            _highContrast = false;

            // Create a stroke-visual dictionary
            _visuals = new Dictionary<Stroke, StrokeVisual>();
        }

        /// <summary>
        /// Returns a reference to a visual tree that can be used to render the ink.
        /// This property may be either a single visual or a container visual with
        /// children. The element uses this visual as a child visual and arranges
        /// it with respects to the other siblings.
        /// Note: No visuals are actually generated until the application gets
        /// this property for the first time. If no strokes are set then an empty
        /// visual is returned.
        /// </summary>
        internal Visual RootVisual { get { return _rootVisual; } }

        /// <summary>
        /// Set the strokes property to the collection of strokes to be rendered.
        /// The Renderer will then listen to changes to the StrokeCollection
        /// and update its state to reflect the changes.
        /// </summary>
        internal StrokeCollection Strokes
        {
            get
            {
                // We should never return a null value.
                if ( _strokes == null )
                {
                    _strokes = new StrokeCollection();

                    // Start listening on events from the stroke collection.
                    _strokes.StrokesChangedInternal += new StrokeCollectionChangedEventHandler(OnStrokesChanged);
                }

                return _strokes;
            }
            set
            {
                if (value == null)
                {
                    throw new System.ArgumentNullException("value");
                }
                if (value == _strokes)
                {
                    return;
                }

                // Detach the current stroke collection
                if (null != _strokes)
                {
                    // Stop listening on events from the stroke collection.
                    _strokes.StrokesChangedInternal -= new StrokeCollectionChangedEventHandler(OnStrokesChanged);

                    foreach (StrokeVisual visual in _visuals.Values)
                    {
                        StopListeningOnStrokeEvents(visual.Stroke);
                        // Detach the visual from the tree
                        DetachVisual(visual);
                    }
                    _visuals.Clear();
                }

                // Set it.
                _strokes = value;

                // Create visuals
                foreach (Stroke stroke in _strokes)
                {
                    // Create a visual per stroke
                    StrokeVisual visual = new StrokeVisual(stroke, this);

                    // Store the stroke-visual pair in the dictionary
                    _visuals.Add(stroke, visual);

                    StartListeningOnStrokeEvents(visual.Stroke);

                    // Attach it to the visual tree
                    AttachVisual(visual, true/*buildingStrokeCollection*/);
                }

                // Start listening on events from the stroke collection.
                _strokes.StrokesChangedInternal += new StrokeCollectionChangedEventHandler(OnStrokesChanged);
            }
        }

        /// <summary>
        /// User supposed to use this method to attach IncrementalRenderer's root visual
        /// to the visual tree of a stroke collection view.
        /// </summary>
        /// <param name="visual">visual to attach</param>
        /// <param name="drawingAttributes">drawing attributes that used in the incremental rendering</param>
        internal void AttachIncrementalRendering(Visual visual, DrawingAttributes drawingAttributes)
        {
            // Check the input parameters
            if (visual == null)
            {
                throw new System.ArgumentNullException("visual");
            }
            if (drawingAttributes == null)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            //harden against eaten exceptions
            bool exceptionRaised = false;

            // Verify that the visual hasn't been attached already
            if (_attachedVisuals != null)
            {
                foreach(Visual alreadyAttachedVisual in _attachedVisuals)
                {
                    if (visual == alreadyAttachedVisual)
                    {
                        exceptionRaised = true;
                        throw new System.InvalidOperationException(SR.Get(SRID.CannotAttachVisualTwice));
                    }
                }
            }
            else
            {
                // Create the list to register attached visuals in
                _attachedVisuals = new List<Visual>();
            }


            if (!exceptionRaised)
            {
                // The position of the visual in the tree depends on the drawingAttributes
                // Find the appropriate parent visual to attach this visual to.
                ContainerVisual parent = drawingAttributes.IsHighlighter ? GetContainerVisual(drawingAttributes) : _incrementalRenderingVisuals;

                // Attach the visual to the tree
                parent.Children.Add(visual);

                // Put the visual into the list of visuals attached via this method
                _attachedVisuals.Add(visual);
            }
        }

        /// <summary>
        /// Detaches a visual previously attached via AttachIncrementalRendering
        /// </summary>
        /// <param name="visual">the visual to detach</param>
        internal void DetachIncrementalRendering(Visual visual)
        {
            if (visual == null)
            {
                throw new System.ArgumentNullException("visual");
            }

            // Remove the visual in the list of attached via AttachIncrementalRendering
            if ((_attachedVisuals == null) || (_attachedVisuals.Remove(visual) == false))
            {
                throw new System.InvalidOperationException(SR.Get(SRID.VisualCannotBeDetached));
            }

            // Detach it from the tree
            DetachVisual(visual);
        }

        /// <summary>
        /// Internal helper used to indicate if a visual was previously attached 
        /// via a call to AttachIncrementalRendering
        /// </summary>
        internal bool ContainsAttachedIncrementalRenderingVisual(Visual visual)
        {
            if (visual == null || _attachedVisuals == null)
            {
                return false;
            }

            return _attachedVisuals.Contains(visual);
        }

        /// <summary>
        /// Internal helper used to determine if a visual is in the right spot in the visual tree
        /// </summary>
        internal bool AttachedVisualIsPositionedCorrectly(Visual visual, DrawingAttributes drawingAttributes)
        {
            if (visual == null || drawingAttributes == null || _attachedVisuals == null || !_attachedVisuals.Contains(visual))
            {
                return false;
            }

            ContainerVisual correctParent 
                = drawingAttributes.IsHighlighter ? GetContainerVisual(drawingAttributes) : _incrementalRenderingVisuals;

            ContainerVisual currentParent 
                = VisualTreeHelper.GetParent(visual) as ContainerVisual;

            if (currentParent == null || correctParent != currentParent)
            {
                return false;
            }
            return true;
        }



        /// <summary>
        /// TurnOnHighContrast turns on the HighContrast rendering mode
        /// </summary>
        /// <param name="strokeColor">The stroke color under high contrast</param>
        internal void TurnHighContrastOn(Color strokeColor)
        {
            if ( !_highContrast || strokeColor != _highContrastColor )
            {
                _highContrast = true;
                _highContrastColor = strokeColor;
                UpdateStrokeVisuals();
            }
        }

        /// <summary>
        /// ResetHighContrast turns off the HighContrast mode
        /// </summary>
        internal void TurnHighContrastOff()
        {
            if ( _highContrast )
            {
                _highContrast = false;
                UpdateStrokeVisuals();
            }
        }

        /// <summary>
        /// Indicates whether the renderer is in high contrast mode.
        /// </summary>
        /// <returns></returns>
        internal bool IsHighContrast()
        {
            return _highContrast;
        }

        /// <summary>
        /// returns the stroke color for the high contrast rendering.
        /// </summary>
        /// <returns></returns>
        public Color GetHighContrastColor()
        {
            return _highContrastColor;
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// StrokeCollectionChanged event handler
        /// </summary>
        private void OnStrokesChanged(object sender, StrokeCollectionChangedEventArgs eventArgs)
        {
            System.Diagnostics.Debug.Assert(sender == _strokes);

            // Read the args
            StrokeCollection added = eventArgs.Added;
            StrokeCollection removed = eventArgs.Removed;

            // Add new strokes
            foreach (Stroke stroke in added)
            {
                // Verify that it's not a dupe
                if (_visuals.ContainsKey(stroke))
                {
                    throw new System.ArgumentException(SR.Get(SRID.DuplicateStrokeAdded));
                }

                // Create a visual for the new stroke and add it to the dictionary
                StrokeVisual visual = new StrokeVisual(stroke, this);
                _visuals.Add(stroke, visual);

                // Start listening on the stroke events
                StartListeningOnStrokeEvents(visual.Stroke);

                // Attach it to the visual tree
                AttachVisual(visual, false/*buildingStrokeCollection*/);
            }

            // Deal with removed strokes first
            foreach (Stroke stroke in removed)
            {
                // Verify that the event is in sync with the view
                StrokeVisual visual = null;
                if (_visuals.TryGetValue(stroke, out visual))
                {
                    // get rid of both the visual and the stroke
                    DetachVisual(visual);
                    StopListeningOnStrokeEvents(visual.Stroke);
                    _visuals.Remove(stroke);
                }
                else
                {
                    throw new System.ArgumentException(SR.Get(SRID.UnknownStroke3));
                }
            }
        }

        /// <summary>
        /// Stroke Invalidated event handler
        /// </summary>
        private void OnStrokeInvalidated(object sender, EventArgs eventArgs)
        {
            System.Diagnostics.Debug.Assert(_strokes.IndexOf(sender as Stroke) != -1);

            // Find the visual associated with the changed stroke.
            StrokeVisual visual;
            Stroke stroke = (Stroke)sender;
            if (_visuals.TryGetValue(stroke, out visual) == false)
            {
                throw new System.ArgumentException(SR.Get(SRID.UnknownStroke1));
            }

            // The original value of IsHighligher and Color are cached in StrokeVisual.
            // if (IsHighlighter value changed or (IsHighlighter == true and not changed and color changed)
            // detach and re-attach the corresponding visual;
            // otherwise Invalidate the corresponding StrokeVisual
            if (visual.CachedIsHighlighter != stroke.DrawingAttributes.IsHighlighter ||
                (stroke.DrawingAttributes.IsHighlighter &&
                    StrokeRenderer.GetHighlighterColor(visual.CachedColor) != StrokeRenderer.GetHighlighterColor(stroke.DrawingAttributes.Color)))
            {
                // The change requires reparenting the visual in the tree.
                DetachVisual(visual);
                AttachVisual(visual, false/*buildingStrokeCollection*/);

                // Update the cached values
                visual.CachedIsHighlighter = stroke.DrawingAttributes.IsHighlighter;
                visual.CachedColor = stroke.DrawingAttributes.Color;
            }
            // Update the visual.
            visual.Update();
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Update the stroke visuals
        /// </summary>
        private void UpdateStrokeVisuals()
        {
            foreach ( StrokeVisual strokeVisual in _visuals.Values )
            {
                strokeVisual.Update();
            }
        }

        /// <summary>
        /// Attaches a stroke visual to the tree based on the stroke's
        /// drawing attributes and/or its z-order (index in the collection).
        /// </summary>
        private void AttachVisual(StrokeVisual visual, bool buildingStrokeCollection)
        {
            System.Diagnostics.Debug.Assert(_strokes != null);

            if (visual.Stroke.DrawingAttributes.IsHighlighter)
            {
                // Find or create a container visual for highlighter strokes of the color
                ContainerVisual parent = GetContainerVisual(visual.Stroke.DrawingAttributes);
                Debug.Assert(visual is StrokeVisual);

                //insert StrokeVisuals under any non-StrokeVisuals used for dynamic inking
                int i = 0;
                for (int j = parent.Children.Count - 1; j >= 0; j--)
                {
                    if (parent.Children[j] is StrokeVisual)
                    {
                        i = j + 1;
                        break;
                    }
                }
                parent.Children.Insert(i, visual);
            }
            else
            {
                // For regular ink we have to respect the z-order of the strokes.
                // The implementation below is not optimal in a generic case, but the
                // most simple and should work ok in most common scenarios.

                // Find the nearest non-highlighter stroke with a lower z-order
                // and insert the new visual right next to the visual of that stroke.
                StrokeVisual precedingVisual = null;
                int i = 0;
                if (buildingStrokeCollection)
                {
                    Stroke visualStroke = visual.Stroke;
                    //we're building up a stroke collection, no need to start at IndexOf,
                    i = Math.Min(_visuals.Count, _strokes.Count); //not -1, we're about to decrement
                    while (--i >= 0)
                    {
                        if (object.ReferenceEquals(_strokes[i], visualStroke))
                        {
                            break;
                        }
                    }
                }
                else
                {
                    i = _strokes.IndexOf(visual.Stroke);
                }
                while (--i >= 0)
                {
                    Stroke stroke = _strokes[i];
                    if ((stroke.DrawingAttributes.IsHighlighter == false)
                        && (_visuals.TryGetValue(stroke, out precedingVisual) == true)
                        && (VisualTreeHelper.GetParent(precedingVisual) != null))
                    {
                        VisualCollection children = ((ContainerVisual)(VisualTreeHelper.GetParent(precedingVisual))).Children;
                        int index = children.IndexOf(precedingVisual);
                        children.Insert(index + 1, visual);
                        break;
                    }
                }
                // If found no non-highlighter strokes with a lower z-order, insert
                // the stroke at the very bottom of the regular ink visual tree.
                if (i < 0)
                {
                    ContainerVisual parent = GetContainerVisual(visual.Stroke.DrawingAttributes);
                    parent.Children.Insert(0, visual);
                }
            }
        }

        /// <summary>
        /// Detaches a visual from the tree, also removes highligher parents if empty
        /// when true is passed
        /// </summary>
        private void DetachVisual(Visual visual)
        {
            ContainerVisual parent = (ContainerVisual)(VisualTreeHelper.GetParent(visual));
            if (parent != null)
            {
                VisualCollection children = parent.Children;
                children.Remove(visual);

                // If the parent is a childless highlighter, detach it too.
                HighlighterContainerVisual hcVisual = parent as HighlighterContainerVisual;
                if (hcVisual != null &&
                    hcVisual.Children.Count == 0 &&
                    _highlighters != null &&
                    _highlighters.ContainsValue(hcVisual))
                {
                    DetachVisual(hcVisual);
                    _highlighters.Remove(hcVisual.Color);
                }
            }
        }

        /// <summary>
        /// Attaches event handlers to stroke events
        /// </summary>
        private void StartListeningOnStrokeEvents(Stroke stroke)
        {
            System.Diagnostics.Debug.Assert(stroke != null);
            stroke.Invalidated += new EventHandler(OnStrokeInvalidated);
        }

        /// <summary>
        /// Detaches event handlers from stroke
        /// </summary>
        private void StopListeningOnStrokeEvents(Stroke stroke)
        {
            System.Diagnostics.Debug.Assert(stroke != null);
            stroke.Invalidated -= new EventHandler(OnStrokeInvalidated);
        }

        /// <summary>
        /// Finds a container for a new visual based on the drawing attributes
        /// of the stroke rendered into that visual.
        /// </summary>
        /// <param name="drawingAttributes">drawing attributes</param>
        /// <returns>visual</returns>
        private ContainerVisual GetContainerVisual(DrawingAttributes drawingAttributes)
        {
            System.Diagnostics.Debug.Assert(drawingAttributes != null);

            HighlighterContainerVisual hcVisual;
            if (drawingAttributes.IsHighlighter)
            {
                // For a highlighter stroke, the color.A is neglected.
                Color color = StrokeRenderer.GetHighlighterColor(drawingAttributes.Color);
                if ((_highlighters == null) || (_highlighters.TryGetValue(color, out hcVisual) == false))
                {
                    if (_highlighters == null)
                    {
                        _highlighters = new Dictionary<Color, HighlighterContainerVisual>();
                    }

                    hcVisual = new HighlighterContainerVisual(color);
                    hcVisual.Opacity = StrokeRenderer.HighlighterOpacity;
                    _highlightersRoot.Children.Add(hcVisual);

                    _highlighters.Add(color, hcVisual);
                }
                else if (VisualTreeHelper.GetParent(hcVisual) == null)
                {
                    _highlightersRoot.Children.Add(hcVisual);
                }
                return hcVisual;
            }
            else
            {
                return _regularInkVisuals;
            }
        }

        #endregion

        #region Fields

        // The renderer's top level container visuals
        private ContainerVisual _rootVisual;
        private ContainerVisual _highlightersRoot;
        private ContainerVisual _incrementalRenderingVisuals;
        private ContainerVisual _regularInkVisuals;

        // Stroke-to-visual map
        private Dictionary<Stroke, StrokeVisual> _visuals;

        // Color-to-visual map for highlighter ink container visuals
        private Dictionary<Color, HighlighterContainerVisual> _highlighters = null;

        // Collection of strokes this Renderer renders
        private StrokeCollection _strokes = null;

        // List of visuals attached via AttachIncrementalRendering
        private List<Visual> _attachedVisuals = null;

        // Whhen true, will render in high contrast mode
        private bool _highContrast;
        private Color _highContrastColor = Colors.White;

        #endregion
    }
}


