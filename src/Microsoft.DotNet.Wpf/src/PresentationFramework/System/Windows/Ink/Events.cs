// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Defined our Delegates, EventHandlers and EventArgs
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Ink;
using Swi = System.Windows.Ink;
using MS.Utility;

namespace System.Windows.Controls
{
    /// <summary>
    /// The delegate to use for the StrokeCollected event
    /// </summary>
    public delegate void InkCanvasStrokeCollectedEventHandler(object sender, InkCanvasStrokeCollectedEventArgs e);

    /// <summary>
    ///    InkCanvasStrokeCollectedEventArgs
    /// </summary>
    public class InkCanvasStrokeCollectedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// [TBS]
        /// </summary>
        public InkCanvasStrokeCollectedEventArgs(Swi.Stroke stroke) : base(InkCanvas.StrokeCollectedEvent)
        {
            if (stroke == null)
            {
                throw new ArgumentNullException("stroke");
            }
            _stroke = stroke;
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public Swi.Stroke Stroke
        {
            get { return _stroke; }
        }

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            InkCanvasStrokeCollectedEventHandler handler = (InkCanvasStrokeCollectedEventHandler)genericHandler;
            handler(genericTarget, this);
        }

        private Swi.Stroke _stroke;
    }

    /// <summary>
    /// The delegate to use for the StrokesChanged event
    /// </summary>
    public delegate void InkCanvasStrokesReplacedEventHandler(object sender, InkCanvasStrokesReplacedEventArgs e);

    /// <summary>
    ///    InkCanvasStrokesChangedEventArgs
    /// </summary>
    public class InkCanvasStrokesReplacedEventArgs : EventArgs
    {
        /// <summary>
        /// InkCanvasStrokesReplacedEventArgs
        /// </summary>
        internal InkCanvasStrokesReplacedEventArgs(Swi.StrokeCollection newStrokes, Swi.StrokeCollection previousStrokes)
        {
            if (newStrokes == null)
            {
                throw new ArgumentNullException("newStrokes");
            }
            if (previousStrokes == null)
            {
                throw new ArgumentNullException("previousStrokes");
            }
            _newStrokes = newStrokes;
            _previousStrokes = previousStrokes;
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public Swi.StrokeCollection NewStrokes
        {
            get { return _newStrokes; }
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public Swi.StrokeCollection PreviousStrokes
        {
            get { return _previousStrokes; }
        }

        private Swi.StrokeCollection _newStrokes;
        private Swi.StrokeCollection _previousStrokes;
    }

    /// <summary>
    ///     The delegate to use for the SelectionChanging event
    ///     
    ///     This event is only thrown when you change the selection programmatically (through our APIs, not the SelectionService's
    ///     or through our lasso selection behavior
    /// </summary>
    public delegate void InkCanvasSelectionChangingEventHandler(object sender, InkCanvasSelectionChangingEventArgs e);

    /// <summary>
    /// Event arguments sent when the SelectionChanging event is raised.
    /// </summary>
    public class InkCanvasSelectionChangingEventArgs : CancelEventArgs
    {
        private StrokeCollection        _strokes;
        private List<UIElement>         _elements;
        private bool                    _strokesChanged;
        private bool                    _elementsChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        internal InkCanvasSelectionChangingEventArgs(StrokeCollection selectedStrokes, IEnumerable<UIElement> selectedElements)
        {
            if (selectedStrokes == null)
            {
                throw new ArgumentNullException("selectedStrokes");
            }
            if (selectedElements == null)
            {
                throw new ArgumentNullException("selectedElements");
            }
            _strokes = selectedStrokes;
            List<UIElement> elements =
                new List<UIElement>(selectedElements);
            _elements = elements;

            _strokesChanged = false;
            _elementsChanged = false;
        }
        
        /// <summary>
        /// An internal flag which indicates the Strokes has changed.
        /// </summary>
        internal bool StrokesChanged
        {
            get
            {
                return _strokesChanged;
            }
        }

        /// <summary>
        /// An internal flag which indicates the Elements has changed.
        /// </summary>
        internal bool ElementsChanged
        {
            get
            {
                return _elementsChanged;
            }
        }

        /// <summary>
        /// Set the selected elements
        /// </summary>
        /// <param name="selectedElements">The new selected elements</param>
        public void SetSelectedElements(IEnumerable<UIElement> selectedElements)
        {
            if ( selectedElements == null )
            {
                throw new ArgumentNullException("selectedElements");
            }

            List<UIElement> elements =
                new List<UIElement>(selectedElements);
            _elements = elements;
            _elementsChanged = true;
        }

        /// <summary>
        /// Get the selected elements
        /// </summary>
        /// <returns>The selected elements</returns>
        public ReadOnlyCollection<UIElement> GetSelectedElements()
        {
            return new ReadOnlyCollection<UIElement>(_elements);
        }

        /// <summary>
        /// Set the selected strokes
        /// </summary>
        /// <param name="selectedStrokes">The new selected strokes</param>
        public void SetSelectedStrokes(StrokeCollection selectedStrokes)
        {
            if ( selectedStrokes == null )
            {
                throw new ArgumentNullException("selectedStrokes");
            }

            _strokes = selectedStrokes;
            _strokesChanged = true;
        }

        /// <summary>
        /// Get the selected strokes
        /// </summary>
        /// <returns>The selected strokes</returns>
        public StrokeCollection GetSelectedStrokes()
        {
            //
            // make a copy of out internal collection.
            //
            StrokeCollection sc = new StrokeCollection();
            sc.Add(_strokes);
            return sc;
        }
    }

    
    /// <summary>
    ///     The delegate to use for the SelectionMoving, SelectionResizing events
    /// </summary>
    public delegate void  InkCanvasSelectionEditingEventHandler(object sender,  InkCanvasSelectionEditingEventArgs e);

    /// <summary>
    /// Event arguments sent when the SelectionChanging event is raised.
    /// </summary>
    public class  InkCanvasSelectionEditingEventArgs : CancelEventArgs
    {
        private Rect _oldRectangle;
        private Rect _newRectangle;
        /// <summary>
        /// Constructor
        /// </summary>
        internal  InkCanvasSelectionEditingEventArgs(Rect oldRectangle, Rect newRectangle) 
        {
            _oldRectangle = oldRectangle;
            _newRectangle = newRectangle;
        }

        /// <summary>
        /// Read access to the OldRectangle, from before the edit.
        /// </summary>
        public Rect OldRectangle
        {
            get { return _oldRectangle;}
        }

        /// <summary>
        /// Read access to the NewRectangle, resulting from this edit.
        /// </summary>
        public Rect NewRectangle
        {
            get { return _newRectangle;}
            set {_newRectangle = value;}
        }
    }

    /// <summary>
    ///     The delegate to use for the InkErasing event
    /// </summary>
    public delegate void InkCanvasStrokeErasingEventHandler(object sender, InkCanvasStrokeErasingEventArgs e);

    /// <summary>
    /// Event arguments sent when the SelectionChanging event is raised.
    /// </summary>
    public class InkCanvasStrokeErasingEventArgs : CancelEventArgs
    {
        private Swi.Stroke _stroke;
        /// <summary>
        /// Constructor
        /// </summary>
        internal InkCanvasStrokeErasingEventArgs(Swi.Stroke stroke) 
        {
            if (stroke == null)
            {
                throw new ArgumentNullException("stroke");
            }
            _stroke = stroke;
        }

        /// <summary>
        /// Read access to the stroke about to be deleted
        /// </summary>
        public Stroke Stroke
        {
            get { return _stroke;}
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    /// <param name="sender">sender</param>
    /// <param name="e">e</param>
    public delegate void InkCanvasGestureEventHandler(object sender, InkCanvasGestureEventArgs e);

    /// <summary>
    /// ApplicationGestureEventArgs
    /// </summary>
    public class InkCanvasGestureEventArgs : RoutedEventArgs
    {
        private StrokeCollection _strokes;
        private List<GestureRecognitionResult> _gestureRecognitionResults;
        private bool                _cancel;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="strokes">strokes</param>
        /// <param name="gestureRecognitionResults">gestureRecognitionResults</param>
        public InkCanvasGestureEventArgs(StrokeCollection strokes, IEnumerable<GestureRecognitionResult> gestureRecognitionResults)
            : base(InkCanvas.GestureEvent)
        {
            if (strokes == null)
            {
                throw new ArgumentNullException("strokes");
            }
            if (strokes.Count < 1)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidEmptyStrokeCollection), "strokes");
            }
            if (gestureRecognitionResults == null)
            {
                throw new ArgumentNullException("strokes");
            }
            List<GestureRecognitionResult> results = 
                new List<GestureRecognitionResult>(gestureRecognitionResults);
            if (results.Count == 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidEmptyArray), "gestureRecognitionResults");
            }
            _strokes = strokes;
            _gestureRecognitionResults = results;
        }

        /// <summary>
        /// TBD
        /// </summary>
        public StrokeCollection Strokes
        {
            get { return _strokes; }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<GestureRecognitionResult> GetGestureRecognitionResults()
        {
            return new ReadOnlyCollection<GestureRecognitionResult>(_gestureRecognitionResults);
        }

        /// <summary>
        /// Indicates whether the Gesture event needs to be cancelled.
        /// </summary>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value;
            }
        }

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            InkCanvasGestureEventHandler handler = (InkCanvasGestureEventHandler)genericHandler;
            handler(genericTarget, this);
        }
    }
}
