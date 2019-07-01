// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create InkCanvas.
    /// </summary>
    internal class InkCanvasFactory : DiscoverableFactory<InkCanvas>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set InkCanvas Background property.
        /// </summary>
        public Brush Background { get; set; }

        /// <summary>
        /// Gets or sets a list of UIElement to set InkCanvas Children property.
        /// </summary>
        public List<UIElement> UIElementCollection { get; set; }

        /// <summary>
        /// Gets or sets DefaultDrawingAttributes to set InkCanvas DefaultDrawingAttributes property.
        /// </summary>
        public DrawingAttributes DefaultDrawingAttributes { get; set; }

        /// <summary>
        /// Gets or sets a StylusPointDescription to set InkCanvas DefaultStylusPointDescription property.
        /// </summary>
        public StylusPointDescription DefaultStylusPointDescription { get; set; }

        /// <summary>
        /// Gets or sets a StylusShape to set InkCanvas EraserShape property.
        /// </summary>
        public StylusShape EraserShape { get; set; }

        /// <summary>
        /// Gets or sets a list of InkCanvasClipboardFormat to set InkCanvas PreferredPasteFormats property.
        /// </summary>
        public List<InkCanvasClipboardFormat> PreferredPasteFormats { get; set; }

        /// <summary>
        /// Gets or sets a StrokeCollection to set InkCanvas Strokes property.
        /// </summary>
        public StrokeCollection Strokes { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create a InkCanvas.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override InkCanvas Create(DeterministicRandom random)
        {
            InkCanvas inkCanvas = new InkCanvas();

            inkCanvas.Background = Background;
            HomelessTestHelpers.Merge(inkCanvas.Children, UIElementCollection);
            inkCanvas.DefaultDrawingAttributes = DefaultDrawingAttributes;
            inkCanvas.DefaultStylusPointDescription = DefaultStylusPointDescription;
            inkCanvas.EditingMode = random.NextEnum<InkCanvasEditingMode>();
            inkCanvas.EditingModeInverted = random.NextEnum<InkCanvasEditingMode>();
            inkCanvas.EraserShape = EraserShape;
            inkCanvas.MoveEnabled = random.NextBool();
            inkCanvas.PreferredPasteFormats = PreferredPasteFormats;
            inkCanvas.ResizeEnabled = random.NextBool();
            inkCanvas.Strokes = Strokes;
            inkCanvas.UseCustomCursor = random.NextBool();

            inkCanvas.ActiveEditingModeChanged += new RoutedEventHandler(ActiveEditingModeChanged);
            inkCanvas.DefaultDrawingAttributesReplaced += new DrawingAttributesReplacedEventHandler(DefaultDrawingAttributesReplaced);
            inkCanvas.EditingModeChanged += new RoutedEventHandler(EditingModeChanged);
            inkCanvas.EditingModeInvertedChanged += new RoutedEventHandler(EditingModeInvertedChanged);
            inkCanvas.Gesture += new InkCanvasGestureEventHandler(Gesture);
            inkCanvas.SelectionChanged += new EventHandler(SelectionChanged);
            inkCanvas.SelectionChanging += new InkCanvasSelectionChangingEventHandler(SelectionChanging);
            inkCanvas.SelectionMoved += new EventHandler(SelectionMoved);
            inkCanvas.SelectionMoving += new InkCanvasSelectionEditingEventHandler(SelectionMoving);
            inkCanvas.SelectionResized += new EventHandler(SelectionResized);
            inkCanvas.SelectionResizing += new InkCanvasSelectionEditingEventHandler(SelectionResizing);
            inkCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(StrokeCollected);
            inkCanvas.StrokeErased += new RoutedEventHandler(StrokeErased);
            inkCanvas.StrokeErasing += new InkCanvasStrokeErasingEventHandler(StrokeErasing);
            inkCanvas.StrokesReplaced += new InkCanvasStrokesReplacedEventHandler(StrokesReplaced);

            return inkCanvas;
        }

        #endregion

        #region Private Members

        private void ActiveEditingModeChanged(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("InkCanvas editing mode changed.");
        }

        private void DefaultDrawingAttributesReplaced(object sender, DrawingAttributesReplacedEventArgs e)
        {
            Trace.WriteLine("InkCanvas default drawing attributes replaced.");
        }

        private void EditingModeChanged(object sender, EventArgs e)
        {
            Trace.WriteLine("InkCanvas editing mode changed.");
        }

        private void EditingModeInvertedChanged(object sender, EventArgs e)
        {
            Trace.WriteLine("InkCanvas editing mode inverted changed.");
        }

        private void Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            Trace.WriteLine("InkCanvas gestrue.");
        }

        private void SelectionChanged(object sender, EventArgs e)
        {
            Trace.WriteLine("InkCanvas selection changed.");
        }

        private void SelectionChanging(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            Trace.WriteLine("InkCanvas selection changing.");
        }

        private void SelectionMoved(object sender, EventArgs e)
        {
            Trace.WriteLine("InkCanvas selection moved.");
        }

        private void SelectionMoving(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            Trace.WriteLine("InkCanvas selection moving.");
        }

        private void SelectionResized(object sender, EventArgs e)
        {
            Trace.WriteLine("InkCanvas selection resized.");
        }

        private void SelectionResizing(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            Trace.WriteLine("InkCanvas selection resizing.");
        }

        private void StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            Trace.WriteLine("InkCanvas stroke collected.");
        }

        private void StrokeErased(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("InkCanvas stroke erased.");
        }

        private void StrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            Trace.WriteLine("InkCanvas stroke erasing.");
        }

        private void StrokesReplaced(object sender, InkCanvasStrokesReplacedEventArgs e)
        {
            Trace.WriteLine("InkCanvas stroke replaced.");
        }

        #endregion
    }
}
