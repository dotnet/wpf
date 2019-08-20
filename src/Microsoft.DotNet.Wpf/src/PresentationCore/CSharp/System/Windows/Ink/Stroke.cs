// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using MS.Internal.Ink.InkSerializedFormat;
using MS.Internal;
using MS.Internal.Ink;
using System.Reflection;
using System.Windows.Input;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace System.Windows.Ink
{
    /// <summary>
    /// A Stroke object is the fundamental unit of ink data storage.
    /// </summary>
    public partial class Stroke : INotifyPropertyChanged
    {
        /// <summary>Create a stroke from a StylusPointCollection</summary>
        /// <remarks>
        /// </remarks>
        /// <param name="stylusPoints">StylusPointCollection that makes up the stroke</param>
        public Stroke(StylusPointCollection stylusPoints)
            : this (stylusPoints, new DrawingAttributes(), null)
        {
        }

        /// <summary>Create a stroke from a StylusPointCollection</summary>
        /// <remarks>
        /// </remarks>
        /// <param name="stylusPoints">StylusPointCollection that makes up the stroke</param>
        /// <param name="drawingAttributes">drawingAttributes</param>
        public Stroke(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes)
            : this(stylusPoints, drawingAttributes, null)
        {
        }

        /// <summary>Create a stroke from a StylusPointCollection</summary>
        /// <remarks>
        /// </remarks>
        /// <param name="stylusPoints">StylusPointCollection that makes up the stroke</param>
        /// <param name="drawingAttributes">drawingAttributes</param>
        /// <param name="extendedProperties">extendedProperties</param>
        internal Stroke(StylusPointCollection stylusPoints, DrawingAttributes drawingAttributes, ExtendedPropertyCollection extendedProperties)
        {
            if (stylusPoints == null)
            {
                throw new ArgumentNullException("stylusPoints");
            }
            if (stylusPoints.Count == 0)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidStylusPointCollectionZeroCount), "stylusPoints");
            }
            if (drawingAttributes == null)
            {
                throw new ArgumentNullException("drawingAttributes");
            }

            _drawingAttributes = drawingAttributes;
            _stylusPoints = stylusPoints;
            _extendedProperties = extendedProperties;

            Initialize();
        }

        /// <summary>
        /// Internal helper to set up listeners, called by ctor and by Clone
        /// </summary>
        private void Initialize()
        {
            _drawingAttributes.AttributeChanged += new PropertyDataChangedEventHandler(DrawingAttributes_Changed);
            _stylusPoints.Changed += new EventHandler(StylusPoints_Changed);
            _stylusPoints.CountGoingToZero += new CancelEventHandler(StylusPoints_CountGoingToZero);
        }

        /// <summary>Returns a new stroke that has a deep copy.</summary>
        /// <remarks>Deep copied data includes points, point description, drawing attributes, and transform</remarks>
        /// <returns>Deep copy of current stroke</returns>
        public virtual Stroke Clone()
        {
            //
            // use MemberwiseClone, which will instance the most derived type
            // We use this instead of Activator.CreateInstance because it does not 
            // require ReflectionPermission.  One thing to note, all references 
            // are shared, including event delegates, so we need to set those to null
            //
            Stroke clone = (Stroke)this.MemberwiseClone();

            //
            // null the delegates in the cloned strokes
            //
            clone.DrawingAttributesChanged = null;
            clone.DrawingAttributesReplaced = null;
            clone.StylusPointsReplaced = null;
            clone.StylusPointsChanged = null;
            clone.PropertyDataChanged = null;
            clone.Invalidated = null;
            clone._propertyChanged = null;

            //Clone is also called from Stroke.Copy internally for point 
            //erase.  In that case, we don't want to clone the StylusPoints
            //because they will be replaced after we call
            if (_cloneStylusPoints)
            {
                clone._stylusPoints = _stylusPoints.Clone();
            }
            clone._drawingAttributes = _drawingAttributes.Clone();
            if (_extendedProperties != null)
            {
                clone._extendedProperties = _extendedProperties.Clone();
            } 
            //set up listeners
            clone.Initialize();
            
            //
            // copy state
            //
            Debug.Assert(_cachedGeometry == null || _cachedGeometry.IsFrozen);
            //we don't need to cache if this is frozen
            //if (null != _cachedGeometry)
            //{
            //    clone._cachedGeometry = _cachedGeometry.Clone();
            //}
            //don't need to clone these, they are value types 
            //and are copied by MemberwiseClone
            //_isSelected
            //_drawAsHollow 
            //_cachedBounds

            //this need to be reset
            clone._cloneStylusPoints = true;

            return clone;
        }


        /// <summary>Transforms the ink and also changes the StylusTip</summary>
        /// <param name="transformMatrix">Matrix to transform the stroke by</param>
        /// <param name="applyToStylusTip">Boolean if true the transform matrix will be applied to StylusTip</param>
        public virtual void Transform(Matrix transformMatrix, bool applyToStylusTip)
        {
            if (transformMatrix.IsIdentity)
            {
                return;
            }

            if (!transformMatrix.HasInverse)
            {
                throw new ArgumentException(SR.Get(SRID.MatrixNotInvertible), "transformMatrix");
            }
            else if ( MatrixHelper.ContainsNaN(transformMatrix))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidMatrixContainsNaN), "transformMatrix");
            }
            else if ( MatrixHelper.ContainsInfinity(transformMatrix))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidMatrixContainsInfinity), "transformMatrix");
            }
            else
            {
                // we need to force a recaculation of the cached path geometry right after the
                // DrawingAttributes changed, beforet the events are raised.
                _cachedGeometry = null;
                // Set the cached bounds to empty, which will force a re-calculation of the _cachedBounds upon next GetBounds call.
                _cachedBounds = Rect.Empty;

                if (applyToStylusTip)
                {
                    //we use this flag to prevent this method from causing two
                    //invalidates, which causes a good deal of memory thrash when
                    //the strokes are being rendered
                    _delayRaiseInvalidated = true;
                }

                try
                {
                    _stylusPoints.Transform(new System.Windows.Media.MatrixTransform(transformMatrix));

                    if (applyToStylusTip)
                    {
                        Matrix newMatrix = _drawingAttributes.StylusTipTransform;
                        // Don't allow a Translation in the matrix
                        transformMatrix.OffsetX = 0;
                        transformMatrix.OffsetY = 0;
                        newMatrix *= transformMatrix;
                        //only persist the StylusTipTransform if there is an inverse.
                        //there are cases where two invertible xf's result in a non-invertible one
                        //we decided not to throw here because it is so unobvious
                        if (newMatrix.HasInverse)
                        {
                            _drawingAttributes.StylusTipTransform = newMatrix;
                        }
                    }
                    if (_delayRaiseInvalidated)
                    {
                        OnInvalidated(EventArgs.Empty);
                    }
                    //else OnInvalidated was already raised
                }
                finally
                {
                    //We do this in a finally block to reset
                    //our state in the event that an exception is thrown.
                    _delayRaiseInvalidated = false;
                }
            }
        }

        /// <summary>
        /// Returns a Bezier smoothed version of the StylusPoints
        /// </summary>
        /// <returns></returns>
        public StylusPointCollection GetBezierStylusPoints()
        {
            // Since we can't compute Bezier for single point stroke, we should return.
            if (_stylusPoints.Count < 2)
            {
                return _stylusPoints;
            }

            // Construct the Bezier approximation
            Bezier bezier = new Bezier();
            if (!bezier.ConstructBezierState(   _stylusPoints, 
                                                DrawingAttributes.FittingError))
            {
                //construction failed, return a clone of the original points
                return _stylusPoints.Clone();
            }

            double tolerance = 0.5;
            StylusShape stylusShape = this.DrawingAttributes.StylusShape;
            if (null != stylusShape)
            {
                Rect shapeBoundingBox = stylusShape.BoundingBox;
                double min = Math.Min(shapeBoundingBox.Width, shapeBoundingBox.Height);
                tolerance = Math.Log10(min + min);
                tolerance *= (StrokeCollectionSerializer.AvalonToHimetricMultiplier / 2);
                if (tolerance < 0.5)
                {
                    //don't allow tolerance to drop below .5 or we 
                    //can wind up with an huge amount of bezier points
                    tolerance = 0.5;
                }
            }

            List<Point> bezierPoints = bezier.Flatten(tolerance);
            return GetInterpolatedStylusPoints(bezierPoints);
        }

        /// <summary>
        /// Interpolate packet / pressure data from _stylusPoints
        /// </summary>
        private StylusPointCollection GetInterpolatedStylusPoints(List<Point> bezierPoints)
        {
            Debug.Assert(bezierPoints != null && bezierPoints.Count > 0);

            //new points need the same description
            StylusPointCollection bezierStylusPoints =
                new StylusPointCollection(_stylusPoints.Description, bezierPoints.Count);

            //
            // add the first point
            //
            AddInterpolatedBezierPoint( bezierStylusPoints, 
                                        bezierPoints[0], 
                                        _stylusPoints[0].GetAdditionalData(), 
                                        _stylusPoints[0].PressureFactor);

            if (bezierPoints.Count == 1)
            {
                return bezierStylusPoints;
            }

            //
            // this is a little tricky... Bezier points are not equidistant, so we have to 
            // use the length between the points instead of the indexes to interpolate pressure
            //
            //  Bezier points:   P0 ------------------------------ P1 ---------- P2 --------- P3
            //  Stylus points:   P0 -------- P1 ------------ P2 ------------- P3 ---------- P4
            //  
            //  Or in terms of lengths...
            //  Bezier lengths:  L1 ------------------------------
            //                   L2 ---------------------------------------------
            //                   L3 ---------------------------------------------------------
            //
            //  Stylus lengths   L1 --------
            //                   L2 ------------------------
            //                   L3 -----------------------------------------
            //                   L4 --------------------------------------------------------
            //                   
            //                      
            //
            double bezierLength = 0.0;
            double prevUnbezierLength = 0.0;
            double unbezierLength = GetDistanceBetweenPoints((Point)_stylusPoints[0], (Point)_stylusPoints[1]);

            int stylusPointsIndex = 1;
            int stylusPointsCount = _stylusPoints.Count;
            //skip the first and last point
            for (int x = 1; x < bezierPoints.Count - 1; x++)
            {
                bezierLength += GetDistanceBetweenPoints(bezierPoints[x - 1], bezierPoints[x]);
                while (stylusPointsCount > stylusPointsIndex)
                {
                    if (bezierLength >= prevUnbezierLength &&
                        bezierLength < unbezierLength)
                    {
                        Debug.Assert(stylusPointsCount > stylusPointsIndex);

                        StylusPoint prevStylusPoint = _stylusPoints[stylusPointsIndex - 1];
                        float percentFromPrev = 
                            ((float)bezierLength - (float)prevUnbezierLength) / 
                            ((float)unbezierLength - (float)prevUnbezierLength);
                        float pressureAtPrev = prevStylusPoint.PressureFactor;
                        float pressureDelta = _stylusPoints[stylusPointsIndex].PressureFactor - pressureAtPrev;
                        float interopolatedPressure = (percentFromPrev * pressureDelta) + pressureAtPrev;

                        AddInterpolatedBezierPoint(bezierStylusPoints,
                                                    bezierPoints[x],
                                                    prevStylusPoint.GetAdditionalData(),
                                                    interopolatedPressure);
                        break;
}
                    else
                    {
                        Debug.Assert(bezierLength >= prevUnbezierLength);
                        //
                        // move our unbezier lengths forward...
                        // 
                        stylusPointsIndex++;
                        if (stylusPointsCount > stylusPointsIndex)
                        {
                            prevUnbezierLength = unbezierLength;
                            unbezierLength +=
                                GetDistanceBetweenPoints((Point)_stylusPoints[stylusPointsIndex - 1],
                                                         (Point)_stylusPoints[stylusPointsIndex]);
                        } //else we'll break
                    }
                }
            }

            //
            // add the last point
            //
            AddInterpolatedBezierPoint( bezierStylusPoints,
                                        bezierPoints[bezierPoints.Count - 1],
                                        _stylusPoints[stylusPointsCount - 1].GetAdditionalData(),
                                        _stylusPoints[stylusPointsCount - 1].PressureFactor);
            
            return bezierStylusPoints;
        }

        /// <summary>
        /// Private helper used to get the length between two points
        /// </summary>
        private double GetDistanceBetweenPoints(Point p1, Point p2)
        {
            Vector spine = p2 - p1;
            return Math.Sqrt(spine.LengthSquared);
        }

        /// <summary>
        /// Private helper for adding a StylusPoint to the BezierStylusPoints
        /// </summary>
        private void AddInterpolatedBezierPoint(StylusPointCollection bezierStylusPoints, 
                                                Point bezierPoint, 
                                                int[] additionalData, 
                                                float pressure)
        {
            double xVal = bezierPoint.X > StylusPoint.MaxXY ?
                        StylusPoint.MaxXY :
                        (bezierPoint.X < StylusPoint.MinXY ? StylusPoint.MinXY : bezierPoint.X);

            double yVal = bezierPoint.Y > StylusPoint.MaxXY ?
                        StylusPoint.MaxXY :
                        (bezierPoint.Y < StylusPoint.MinXY ? StylusPoint.MinXY : bezierPoint.Y);


            StylusPoint newBezierPoint =
                new StylusPoint(xVal, yVal, pressure, bezierStylusPoints.Description, additionalData, false, false);


            bezierStylusPoints.Add(newBezierPoint);
        }

        /// <summary>
        /// Allows addition of objects to the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        /// <param name="propertyData"></param>
        public void AddPropertyData(Guid propertyDataId, object propertyData)
        {
            DrawingAttributes.ValidateStylusTipTransform(propertyDataId, propertyData);

            object oldValue = null;
            if (ContainsPropertyData(propertyDataId))
            {
                oldValue = GetPropertyData(propertyDataId);
                this.ExtendedProperties[propertyDataId] = propertyData;
            }
            else
            {
                this.ExtendedProperties.Add(propertyDataId, propertyData);
            }

            // fire notification
            OnPropertyDataChanged(new PropertyDataChangedEventArgs(propertyDataId, propertyData, oldValue));
        }


        /// <summary>
        /// Allows removal of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public void RemovePropertyData(Guid propertyDataId)
        {
            object propertyData = GetPropertyData(propertyDataId);
            this.ExtendedProperties.Remove(propertyDataId);
            // fire notification
            OnPropertyDataChanged(new PropertyDataChangedEventArgs(propertyDataId, null, propertyData));
        }

        /// <summary>
        /// Allows retrieval of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public object GetPropertyData(Guid propertyDataId)
        {
            return this.ExtendedProperties[propertyDataId];
        }

        /// <summary>
        /// Allows retrieval of a Array of guids that are contained in the EPC
        /// </summary>
        public Guid[] GetPropertyDataIds()
        {
            return this.ExtendedProperties.GetGuidArray();
        }

        /// <summary>
        /// Allows the checking of objects in the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public bool ContainsPropertyData(Guid propertyDataId)
        {
            return this.ExtendedProperties.Contains(propertyDataId);
        }


        /// <summary>
        /// Allows an application to configure the rendering state
        /// associated with this stroke (e.g. outline pen, brush, color,
        /// stylus tip, etc.)
        /// </summary>
        /// <remarks>
        /// If the stroke has been deleted, this will return null for 'get'.
        /// If the stroke has been deleted, the 'set' will no-op.
        /// </remarks>
        /// <value>The drawing attributes associated with the current stroke.</value>
        public DrawingAttributes DrawingAttributes
        {
            get
            {
                return _drawingAttributes;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _drawingAttributes.AttributeChanged -= new PropertyDataChangedEventHandler(DrawingAttributes_Changed);

                DrawingAttributesReplacedEventArgs e =
                    new DrawingAttributesReplacedEventArgs(value, _drawingAttributes);

                DrawingAttributes previousDa = _drawingAttributes;
                _drawingAttributes = value;


                // If the drawing attributes change involves Width, Height, StylusTipTransform, IgnorePressure, or FitToCurve,
                // we need to force a recaculation of the cached path geometry right after the
                // DrawingAttributes changed, beforet the events are raised.
                if (false == DrawingAttributes.GeometricallyEqual(previousDa, _drawingAttributes))
                {
                    _cachedGeometry = null;
                    // Set the cached bounds to empty, which will force a re-calculation of the _cachedBounds upon next GetBounds call.
                    _cachedBounds = Rect.Empty;
                }

                _drawingAttributes.AttributeChanged += new PropertyDataChangedEventHandler(DrawingAttributes_Changed);
                OnDrawingAttributesReplaced(e);
                OnInvalidated(EventArgs.Empty);
                OnPropertyChanged(DrawingAttributesName);
            }
        }

        /// <summary>
        /// StylusPoints
        /// </summary>
        public StylusPointCollection StylusPoints
        {
            get
            {
                return _stylusPoints;
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.Count == 0)
                {
                    //we don't allow this
                    throw new ArgumentException(SR.Get(SRID.InvalidStylusPointCollectionZeroCount));
                }

                // Force a recaculation of the cached path geometry
                _cachedGeometry = null;

                // Set the cached bounds to empty, which will force a re-calculation of the _cachedBounds upon next GetBounds call.
                _cachedBounds = Rect.Empty;

                StylusPointsReplacedEventArgs e =
                    new StylusPointsReplacedEventArgs(value, _stylusPoints);

                _stylusPoints.Changed -= new EventHandler(StylusPoints_Changed);
                _stylusPoints.CountGoingToZero -= new CancelEventHandler(StylusPoints_CountGoingToZero);

                _stylusPoints = value;

                _stylusPoints.Changed += new EventHandler(StylusPoints_Changed);
                _stylusPoints.CountGoingToZero += new CancelEventHandler(StylusPoints_CountGoingToZero);

                // fire notification
                OnStylusPointsReplaced(e);
                OnInvalidated(EventArgs.Empty);
                OnPropertyChanged(StylusPointsName);
            }
        }

        /// <summary>Event that is fired when a drawing attribute is changed.</summary>
        /// <value>The event listener to add or remove in the listener chain</value>
        public event PropertyDataChangedEventHandler DrawingAttributesChanged;

        /// <summary>
        /// Event that is fired when the DrawingAttributes have been replaced
        /// </summary>
        public event DrawingAttributesReplacedEventHandler DrawingAttributesReplaced;

        /// <summary>
        /// Notifies listeners whenever the StylusPoints have been replaced
        /// </summary>
        public event StylusPointsReplacedEventHandler StylusPointsReplaced;

        /// <summary>
        /// Notifies listeners whenever the StylusPoints have been changed
        /// </summary>
        public event EventHandler StylusPointsChanged;

        /// <summary>
        /// Notifies listeners whenever a change occurs in the propertyData
        /// </summary>
        /// <value>PropertyDataChangedEventHandler</value>
        public event PropertyDataChangedEventHandler PropertyDataChanged;


        /// <summary>
        /// Stroke would raise this event for PacketsChanged, DrawingAttributeChanged, or DrawingAttributeReplaced.
        /// Renderer would simply listen to this. Stroke developer can raise this event by calling OnInvalidated when
        /// he wants the renderer to repaint.
        /// </summary>
        public event EventHandler Invalidated;

        /// <summary>
        /// INotifyPropertyChanged.PropertyChanged event, explicitly implemented
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// Method called on derived classes whenever a drawing attribute
        /// is changed and event listeners must be notified.
        /// </summary>
        /// <param name="e">Information on the drawing attributes that changed</param>
        /// <remarks>Derived classes should call this method (their base class)
        /// to ensure that event listeners are notified</remarks>
        protected virtual void OnDrawingAttributesChanged(PropertyDataChangedEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (DrawingAttributesChanged != null)
            {
                DrawingAttributesChanged(this, e);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">DrawingAttributesReplacedEventArgs to raise the event with</param>
        protected virtual void OnDrawingAttributesReplaced(DrawingAttributesReplacedEventArgs e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            if (null != this.DrawingAttributesReplaced)
            {
                DrawingAttributesReplaced(this, e);
            }
        }

        /// <summary>
        /// Method called on derived classes whenever the StylusPoints are replaced
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected virtual void OnStylusPointsReplaced(StylusPointsReplacedEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (StylusPointsReplaced != null)
                StylusPointsReplaced(this, e);
        }

        /// <summary>
        /// Method called on derived classes whenever the StylusPoints are changed
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected virtual void OnStylusPointsChanged(EventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (StylusPointsChanged != null)
                StylusPointsChanged(this, e);
        }

        /// <summary>
        /// Method called on derived classes whenever a change occurs in
        /// the PropertyData.
        /// </summary>
        /// <remarks>Derived classes should call this method (their base class)
        /// to ensure that event listeners are notified</remarks>
        protected virtual void OnPropertyDataChanged(PropertyDataChangedEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (PropertyDataChanged != null)
            {
                PropertyDataChanged(this, e);
            }
        }


        /// <summary>
        /// Method called on derived classes whenever a stroke needs repaint. Developers who
        /// subclass Stroke and need a repaint could raise Invalidated through this protected virtual
        /// </summary>
        protected virtual void OnInvalidated(EventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (Invalidated != null)
            {
                Invalidated(this, e);
            }
        }

        /// <summary>
        /// Method called when a property change occurs to the Stroke
        /// </summary>
        /// <param name="e">The EventArgs specifying the name of the changed property.</param>
        /// <remarks>To follow the guidelines, this method should take a PropertyChangedEventArgs
        /// instance, but every other INotifyPropertyChanged implementation follows this pattern.</remarks>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, e);
            }
        }


        /// <summary>
        /// ExtendedProperties
        /// </summary>
        internal ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                if (_extendedProperties == null)
                {
                    _extendedProperties = new ExtendedPropertyCollection();
                }

                return _extendedProperties;
            }
        }


        /// <summary>
        /// Clip
        /// </summary>
        /// <param name="cutAt">Fragment markers for clipping</param>
        private StrokeCollection Clip(StrokeFIndices[] cutAt)
        {
            System.Diagnostics.Debug.Assert(cutAt != null);
            System.Diagnostics.Debug.Assert(cutAt.Length != 0);

#if DEBUG
            //
            // Assert there are  no overlaps between multiple StrokeFIndices
            //
            AssertSortedNoOverlap(cutAt);
#endif

            StrokeCollection leftovers = new StrokeCollection();
            if (cutAt.Length == 0)
            {
                return leftovers;
            }

            if ((cutAt.Length == 1) && cutAt[0].IsFull)
            {
                leftovers.Add(this.Clone()); //clip and erase always return clones
                return leftovers;
            }


            StylusPointCollection sourceStylusPoints = this.StylusPoints;
            if (this.DrawingAttributes.FitToCurve)
            {
                sourceStylusPoints = this.GetBezierStylusPoints();
            }

            //
            // Assert the findices are NOT out of range with the packets
            //
            System.Diagnostics.Debug.Assert(false == ((!DoubleUtil.AreClose(cutAt[cutAt.Length - 1].EndFIndex, StrokeFIndices.AfterLast)) &&
                                        Math.Ceiling(cutAt[cutAt.Length - 1].EndFIndex) > sourceStylusPoints.Count - 1));

            for (int i = 0; i < cutAt.Length; i++)
            {
                StrokeFIndices fragment = cutAt[i];
                if(DoubleUtil.GreaterThanOrClose(fragment.BeginFIndex, fragment.EndFIndex))
                {
                    // ISSUE-2004/06/26-vsmirnov - temporary workaround for bugs
                    // in point erasing: drop invalid fragments
                    System.Diagnostics.Debug.Assert(DoubleUtil.LessThan(fragment.BeginFIndex, fragment.EndFIndex));
                    continue;
                }

                Stroke stroke = Copy(sourceStylusPoints, fragment.BeginFIndex, fragment.EndFIndex);

                // Add the stroke to the output collection
                leftovers.Add(stroke);
            }

            return leftovers;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cutAt">Fragment markers for clipping</param>
        /// <returns>Survived fragments of current Stroke as a StrokeCollection</returns>
        private StrokeCollection Erase(StrokeFIndices[] cutAt)
        {
            System.Diagnostics.Debug.Assert(cutAt != null);
            System.Diagnostics.Debug.Assert(cutAt.Length != 0);

#if DEBUG
            //
            // Assert there are  no overlaps between multiple StrokeFIndices
            //
            AssertSortedNoOverlap(cutAt);
#endif

            StrokeCollection leftovers = new StrokeCollection();
            // Return an empty collection if the entire stroke it to erase
            if ((cutAt.Length == 0) || ((cutAt.Length == 1) && cutAt[0].IsFull))
            {
                return leftovers;
            }

            StylusPointCollection sourceStylusPoints = this.StylusPoints;
            if (this.DrawingAttributes.FitToCurve)
            {
                sourceStylusPoints = this.GetBezierStylusPoints();
            }

            //
            // Assert the findices are NOT out of range with the packets
            //
            System.Diagnostics.Debug.Assert(false == ((!DoubleUtil.AreClose(cutAt[cutAt.Length - 1].EndFIndex, StrokeFIndices.AfterLast)) &&
                                        Math.Ceiling(cutAt[cutAt.Length - 1].EndFIndex) > sourceStylusPoints.Count - 1));


            int i = 0;
            double beginFIndex = StrokeFIndices.BeforeFirst;
            if (cutAt[0].BeginFIndex == StrokeFIndices.BeforeFirst)
            {
                beginFIndex = cutAt[0].EndFIndex;
                i++;
            }
            for (; i < cutAt.Length; i++)
            {
                StrokeFIndices fragment = cutAt[i];
                if(DoubleUtil.GreaterThanOrClose(beginFIndex, fragment.BeginFIndex))
                {
                    // ISSUE-2004/06/26-vsmirnov - temporary workaround for bugs
                    // in point erasing: drop invalid fragments
                    System.Diagnostics.Debug.Assert(DoubleUtil.LessThan(beginFIndex, fragment.BeginFIndex));
                    continue;
                }


                Stroke stroke = Copy(sourceStylusPoints, beginFIndex, fragment.BeginFIndex);
                // Add the stroke to the output collection
                leftovers.Add(stroke);

                beginFIndex = fragment.EndFIndex;
            }

            if (beginFIndex != StrokeFIndices.AfterLast)
            {
                Stroke stroke = Copy(sourceStylusPoints, beginFIndex, StrokeFIndices.AfterLast);

                // Add the stroke to the output collection
                leftovers.Add(stroke);
            }

            return leftovers;
        }


        /// <summary>
        /// Creates a new stroke from a subset of the points
        /// </summary>
        private Stroke Copy(StylusPointCollection sourceStylusPoints, double beginFIndex, double endFIndex)
        {
            Debug.Assert(sourceStylusPoints != null);
            //
            // get the floor and ceiling to copy from, we'll adjust the ends below
            //
            int beginIndex =
                (DoubleUtil.AreClose(StrokeFIndices.BeforeFirst, beginFIndex))
                    ? 0 : (int)Math.Floor(beginFIndex);

            int endIndex =
                (DoubleUtil.AreClose(StrokeFIndices.AfterLast, endFIndex))
                    ? (sourceStylusPoints.Count - 1) : (int)Math.Ceiling(endFIndex);

            int pointCount = endIndex - beginIndex + 1;
            System.Diagnostics.Debug.Assert(pointCount >= 1);

            StylusPointCollection stylusPoints =
                new StylusPointCollection(this.StylusPoints.Description, pointCount);

            //
            // copy the data from the floor of beginIndex to the ceiling
            //
            for (int i = 0; i < pointCount; i++)
            {
                System.Diagnostics.Debug.Assert(sourceStylusPoints.Count > i + beginIndex);
                StylusPoint stylusPoint = sourceStylusPoints[i + beginIndex];
                stylusPoints.Add(stylusPoint);
            }
            System.Diagnostics.Debug.Assert(stylusPoints.Count == pointCount);

            //
            // at this point, the stroke has been reduced to one with n number of points
            // so we need to adjust the fIndices based on the new point data
            //
            // for example, in a stroke with 4 points:
            // 0, 1, 2, 3
            //
            // if the fIndexes passed 1.1 and 2.7
            // at this point beginIndex is 1 and endIndex is 3
            //
            // now that we've copied the stroke points 1, 2 and 3, we need to
            // adjust beginFIndex to .1 and endFIndex to 1.7
            //
            if (!DoubleUtil.AreClose(beginFIndex, StrokeFIndices.BeforeFirst))
            {
                beginFIndex = beginFIndex - beginIndex;
            }
            if (!DoubleUtil.AreClose(endFIndex, StrokeFIndices.AfterLast))
            {
                endFIndex = endFIndex - beginIndex;
            }

            if (stylusPoints.Count > 1)
            {
                Point begPoint = (Point)stylusPoints[0];
                Point endPoint = (Point)stylusPoints[stylusPoints.Count - 1];

                // Adjust the last point to fragment.EndFIndex.
                if ((!DoubleUtil.AreClose(endFIndex, StrokeFIndices.AfterLast)) && !DoubleUtil.AreClose(endIndex, endFIndex))
                {
                    //
                    // for 1.7, we need to get .3, because that is the distance
                    // we need to back up between the third point and the second
                    //
                    // so this would be .3 = 2 - 1.7
                    double ceiling = Math.Ceiling(endFIndex);
                    double fraction = ceiling - endFIndex;

                    endPoint = GetIntermediatePoint(stylusPoints[stylusPoints.Count - 1],
                                                    stylusPoints[stylusPoints.Count - 2],
                                                    fraction);
}

                // Adjust the first point to fragment.BeginFIndex.
                if ((!DoubleUtil.AreClose(beginFIndex, StrokeFIndices.BeforeFirst)) && !DoubleUtil.AreClose(beginIndex, beginFIndex))
                {
                    begPoint = GetIntermediatePoint(stylusPoints[0],
                                                    stylusPoints[1],
                                                    beginFIndex);
}

                //
                // now set the end points
                //
                StylusPoint tempEnd = stylusPoints[stylusPoints.Count - 1];
                tempEnd.X = endPoint.X;
                tempEnd.Y = endPoint.Y;
                stylusPoints[stylusPoints.Count - 1] = tempEnd;

                StylusPoint tempBegin = stylusPoints[0];
                tempBegin.X = begPoint.X;
                tempBegin.Y = begPoint.Y;
                stylusPoints[0] = tempBegin;
            }

            Stroke stroke = null;
            try
            {
                //
                // set a flag that tells clone not to clone the StylusPoints
                // we do this in a try finally so we alway reset our state
                // even if Clone (which is virtual) throws
                //
                _cloneStylusPoints = false;
                stroke = this.Clone();
                if (stroke.DrawingAttributes.FitToCurve)
                {
                    //
                    // we're using the beziered points for the new data,
                    // FitToCurve needs to be false to prevent re-bezier.
                    //
                    stroke.DrawingAttributes.FitToCurve = false;
                }

                //this will reset the cachedGeometry and cachedBounds
                stroke.StylusPoints = stylusPoints;
            }
            finally
            {
                _cloneStylusPoints = true;
            }

            return stroke;
        }

        /// <summary>
        /// Private helper that will generate a new point between two points at an findex
        /// </summary>
        private Point GetIntermediatePoint(StylusPoint p1, StylusPoint p2, double findex)
        {
            double xDistance = p2.X - p1.X;
            double yDistance = p2.Y - p1.Y;

            double xFDistance = xDistance * findex;
            double yFDistance = yDistance * findex;

            return new Point(p1.X + xFDistance, p1.Y + yFDistance);
        }


#if DEBUG
        /// <summary>
        /// Helper method used to validate that the strokefindices in the array
        /// are sorted and there are no overlaps
        /// </summary>
        /// <param name="fragments">fragments</param>
        private void AssertSortedNoOverlap(StrokeFIndices[] fragments)
        {
            if (fragments.Length == 0)
            {
                return;
            }
            if (fragments.Length == 1)
            {
                System.Diagnostics.Debug.Assert(IsValidStrokeFIndices(fragments[0]));
                return;
            }
            double current = StrokeFIndices.BeforeFirst;
            for (int x = 0; x < fragments.Length; x++)
            {
                if (fragments[x].BeginFIndex <= current)
                {
                    //
                    // when x == 0, we're just starting, any value is valid
                    //
                    System.Diagnostics.Debug.Assert(x == 0);
                }
                current = fragments[x].BeginFIndex;
                System.Diagnostics.Debug.Assert(IsValidStrokeFIndices(fragments[x]) && fragments[x].EndFIndex > current);
                current = fragments[x].EndFIndex;
            }
        }

        private bool IsValidStrokeFIndices(StrokeFIndices findex)
        {
            return (!double.IsNaN(findex.BeginFIndex) && !double.IsNaN(findex.EndFIndex) && findex.BeginFIndex < findex.EndFIndex);
        }

#endif


        /// <summary>
        /// Method called whenever the Stroke's drawing attributes are changed.
        /// This method will trigger an event for any listeners interested in
        /// drawing attributes.
        /// </summary>
        /// <param name="sender">The Drawing Attributes object that was changed</param>
        /// <param name="e">More data about the change that occurred</param>
        private void DrawingAttributes_Changed(object sender, PropertyDataChangedEventArgs e)
        {
            // set Geometry flag to be dirty if the DA change will cause change in geometry
            if (DrawingAttributes.IsGeometricalDaGuid(e.PropertyGuid) == true)
            {
                _cachedGeometry = null;
                // Set the cached bounds to empty, which will force a re-calculation of the _cachedBounds upon next GetBounds call.
                _cachedBounds = Rect.Empty;
            }

            OnDrawingAttributesChanged(e);
            if (!_delayRaiseInvalidated)
            {
                //when Stroke.Transform(Matrix, bool) is called, we don't raise invalidated from 
                //here, but rather from the Stroke.Transform method.
                OnInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Method called whenever the Stroke's StylusPoints are changed.
        /// This method will trigger an event for any listeners interested in
        /// Invalidate
        /// </summary>
        /// <param name="sender">The StylusPoints object that was changed</param>
        /// <param name="e">event args</param>
        private void StylusPoints_Changed(object sender, EventArgs e)
        {
            _cachedGeometry = null;
            _cachedBounds = Rect.Empty;

            OnStylusPointsChanged(EventArgs.Empty);
            if (!_delayRaiseInvalidated)
            {
                //when Stroke.Transform(Matrix, bool) is called, we don't raise invalidated from 
                //here, but rather from the Stroke.Transform method.
                OnInvalidated(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Private method called when StylusPoints are going to zero
        /// </summary>
        /// <param name="sender">The StylusPoints object that is about to go to zero count</param>
        /// <param name="e">event args</param>
        private void StylusPoints_CountGoingToZero(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            //StylusPoints will raise the exception
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

            // Custom attributes associated with this stroke
        private ExtendedPropertyCollection _extendedProperties = null;

            // Drawing attributes associated with this stroke
        private DrawingAttributes _drawingAttributes = null;

        private StylusPointCollection _stylusPoints = null;
}

        //internal helper to determine if a matix contains invalid values
    internal static class MatrixHelper
    {
        //returns true if any member is NaN
        internal static bool ContainsNaN(Matrix matrix)
        {
            if (Double.IsNaN(matrix.M11) ||
                Double.IsNaN(matrix.M12) ||
                Double.IsNaN(matrix.M21) ||
                Double.IsNaN(matrix.M22) ||
                Double.IsNaN(matrix.OffsetX) ||
                Double.IsNaN(matrix.OffsetY))
            {
                return true;
            }
            return false;
        }

        //returns true if any member is negative or positive infinity
        internal static bool ContainsInfinity(Matrix matrix)
        {
            if (Double.IsInfinity(matrix.M11) ||
                Double.IsInfinity(matrix.M12) ||
                Double.IsInfinity(matrix.M21) ||
                Double.IsInfinity(matrix.M22) ||
                Double.IsInfinity(matrix.OffsetX) ||
                Double.IsInfinity(matrix.OffsetY))
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Helper for dealing with IEnumerable of Points
    /// </summary>
    internal static class IEnumerablePointHelper
    {
        /// <summary>
        /// Returns the count of an IEumerable of Points by trying to cast
        /// to an ICollection of Points
        /// </summary>
        internal static int GetCount(IEnumerable<Point> ienum)
        {
            Debug.Assert(ienum != null);
            ICollection<Point> icol = ienum as ICollection<Point>;
            if (icol != null)
            {
                return icol.Count;
            }
            int count = 0;
            foreach (Point point in ienum)
            {
                count++;
            }
            return count;
        }

        /// <summary>
        /// Returns a Point[] for a given IEnumerable of Points.
        /// </summary>
        internal static Point[] GetPointArray(IEnumerable<Point> ienum)
        {
            Debug.Assert(ienum != null);
            Point[] points = ienum as Point[];
            if (points != null)
            {
                return points;
            }

            //
            // fall back to creating an array
            //
            points = new Point[GetCount(ienum)];
            int index = 0;
            foreach (Point point in ienum)
            {
                points[index++] = point;
            }
            return points;
        }
    }
}
