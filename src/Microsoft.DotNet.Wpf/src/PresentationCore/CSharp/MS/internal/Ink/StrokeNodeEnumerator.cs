// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using MS.Utility;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Input;
using MS.Internal.Ink;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink
{
    /// <summary>
    /// This class serves as a unified tool for enumerating through stroke nodes
    /// for all kinds of rendering and/or hit-testing that uses stroke contours.
    /// It provides static API for static (atomic) rendering, and it needs to be
    /// instantiated for dynamic (incremental) rendering. It generates stroke nodes
    /// from Stroke objects with or w/o overriden drawing attributes, as well as from
    /// a arrays of points (for a given StylusShape), and from raw stylus packets.
    /// In either case, the output collection of nodes is represented by a disposable
    /// iterator (i.e. good for a single enumeration only).
    /// </summary>
    internal class StrokeNodeIterator
    {
        /// <summary>
        /// Helper wrapper
        /// </summary>
        internal static StrokeNodeIterator GetIterator(Stroke stroke, DrawingAttributes drawingAttributes)
        {
            if (stroke == null)
            {
                throw new System.ArgumentNullException("stroke");
            }
            if (drawingAttributes == null)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            StylusPointCollection stylusPoints =
                drawingAttributes.FitToCurve ? stroke.GetBezierStylusPoints() : stroke.StylusPoints;

            return GetIterator(stylusPoints, drawingAttributes);
        }
        /// <summary>
        /// Creates a default enumerator for a given stroke
        /// If using the strokes drawing attributes, pass stroke.DrawingAttributes for the second
        /// argument.  If using an overridden DA, use that instance.
        /// </summary>
        internal static StrokeNodeIterator GetIterator(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes)
        {
            if (stylusPoints == null)
            {
                throw new System.ArgumentNullException("stylusPoints");
            }
            if (drawingAttributes == null)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            StrokeNodeOperations operations =
                StrokeNodeOperations.CreateInstance(drawingAttributes.StylusShape);

            bool usePressure = !drawingAttributes.IgnorePressure;

            return new StrokeNodeIterator(stylusPoints, operations, usePressure);
        }


        /// <summary>
        /// GetNormalizedPressureFactor
        /// </summary>
        private static float GetNormalizedPressureFactor(float stylusPointPressureFactor)
        {
            //
            // create a compatible pressure value that maps 0-1 to 0.25 - 1.75
            //
            return (1.5f * stylusPointPressureFactor) + 0.25f;
        }

        /// <summary>
        /// Constructor for an incremental node enumerator that builds nodes
        /// from array(s) of points and a given stylus shape.
        /// </summary>
        /// <param name="nodeShape">a shape that defines the stroke contour</param>
        internal StrokeNodeIterator(StylusShape nodeShape) 
            : this( null,   //stylusPoints
                    StrokeNodeOperations.CreateInstance(nodeShape),
                    false)  //usePressure)
        {
        }

        /// <summary>
        /// Constructor for an incremental node enumerator that builds nodes
        /// from StylusPointCollections
        /// called by the IncrementalRenderer
        /// </summary>
        /// <param name="drawingAttributes">drawing attributes</param>
        internal StrokeNodeIterator(DrawingAttributes drawingAttributes)
            : this( null,   //stylusPoints
                    StrokeNodeOperations.CreateInstance((drawingAttributes == null ? null : drawingAttributes.StylusShape)),
                    (drawingAttributes == null ? false : !drawingAttributes.IgnorePressure))  //usePressure
        {
        }

        /// <summary>
        /// Private ctor
        /// </summary>
        /// <param name="stylusPoints"></param>
        /// <param name="operations"></param>
        /// <param name="usePressure"></param>
        internal StrokeNodeIterator(StylusPointCollection stylusPoints,
                                    StrokeNodeOperations operations,
                                    bool usePressure)
        {
            //Note, StylusPointCollection can be null
            _stylusPoints = stylusPoints;
            if (operations == null)
            {
                throw new ArgumentNullException("operations");
            }
            _operations = operations;
            _usePressure = usePressure;
        }

        /// <summary>
        /// Generates (enumerates) StrokeNode objects for a stroke increment
        /// represented by an StylusPointCollection.  Called from IncrementalRenderer
        /// </summary>
        /// <param name="stylusPoints">StylusPointCollection</param>
        /// <returns>yields StrokeNode objects one by one</returns>
        internal StrokeNodeIterator GetIteratorForNextSegment(StylusPointCollection stylusPoints)
        {
            if (stylusPoints == null)
            {
                throw new System.ArgumentNullException("stylusPoints");
            }

            if (_stylusPoints != null && _stylusPoints.Count > 0 && stylusPoints.Count > 0)
            {
                //insert the previous last point, but we need insert a compatible
                //previous point.  The easiest way to do this is to clone a point
                //(since StylusPoint is a struct, we get get one out to get a copy
                StylusPoint sp = stylusPoints[0];
                StylusPoint lastStylusPoint = _stylusPoints[_stylusPoints.Count - 1];
                sp.X = lastStylusPoint.X;
                sp.Y = lastStylusPoint.Y;
                sp.PressureFactor = lastStylusPoint.PressureFactor;
                stylusPoints.Insert(0, sp);
            }

            return new StrokeNodeIterator(  stylusPoints,
                                            _operations,
                                            _usePressure);
        }

        /// <summary>
        /// Generates (enumerates) StrokeNode objects for a stroke increment
        /// represented by an array of points. This method is supposed to be used only
        /// on objects created via the c-tor with a StylusShape parameter.
        /// </summary>
        /// <param name="points">an array of points representing a stroke increment</param>
        /// <returns>yields StrokeNode objects one by one</returns>
        internal StrokeNodeIterator GetIteratorForNextSegment(Point[] points)
        {   
            if (points == null)
            {
                throw new System.ArgumentNullException("points");
            }
            StylusPointCollection newStylusPoints = new StylusPointCollection(points);
            if (_stylusPoints != null && _stylusPoints.Count > 0)
            {
                //insert the previous last point
                newStylusPoints.Insert(0, _stylusPoints[_stylusPoints.Count - 1]);
            }

            return new StrokeNodeIterator(  newStylusPoints,
                                            _operations,
                                            _usePressure);
        }

        /// <summary>
        /// The count of strokenodes that can be iterated across
        /// </summary>
        internal int Count
        {
            get
            {
                if (_stylusPoints == null)
                {
                    return 0;
                }
                return _stylusPoints.Count;
            }
        }

        /// <summary>
        /// Gets a StrokeNode at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal StrokeNode this[int index]
        {
            get
            {
                return this[index, (index == 0 ? -1 : index - 1)];
            }
        }

        /// <summary>
        /// Gets a StrokeNode at the specified index that connects to a stroke at the previousIndex
        /// previousIndex can be -1 to signify it should be empty (first strokeNode)
        /// </summary>
        /// <returns></returns>
        internal StrokeNode this[int index, int previousIndex]
        {
            get
            {
                if (_stylusPoints == null || index < 0 || index >= _stylusPoints.Count || previousIndex < -1 || previousIndex >= index)
                {
                    throw new IndexOutOfRangeException();
                }

                StylusPoint stylusPoint = _stylusPoints[index];
                StylusPoint previousStylusPoint = (previousIndex == -1 ? new StylusPoint() : _stylusPoints[previousIndex]);
                float pressureFactor = 1.0f;
                float previousPressureFactor = 1.0f;
                if (_usePressure)
                {
                    pressureFactor = StrokeNodeIterator.GetNormalizedPressureFactor(stylusPoint.PressureFactor);
                    previousPressureFactor = StrokeNodeIterator.GetNormalizedPressureFactor(previousStylusPoint.PressureFactor);
                }

                StrokeNodeData nodeData = new StrokeNodeData((Point)stylusPoint, pressureFactor);
                StrokeNodeData lastNodeData = StrokeNodeData.Empty;
                if (previousIndex != -1)
                {
                    lastNodeData = new StrokeNodeData((Point)previousStylusPoint, previousPressureFactor);
                }

                //we use previousIndex+1 because index can skip ahead
                return new StrokeNode(_operations, previousIndex + 1, nodeData, lastNodeData, index == _stylusPoints.Count - 1 /*Is this the last node?*/);
            }
        }

        private bool                    _usePressure;
        private StrokeNodeOperations    _operations;
        private StylusPointCollection   _stylusPoints;
    }
}
