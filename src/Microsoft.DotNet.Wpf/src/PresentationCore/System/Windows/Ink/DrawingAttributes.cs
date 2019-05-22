// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Utility;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using MS.Internal;
using MS.Internal.Ink;
using MS.Internal.Ink.InkSerializedFormat;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    /// <summary>
    /// DrawingAttributes is the list of attributes applied to an ink stroke
    /// when it is drawn. The DrawingAttributes controls stroke color, width,
    /// transparency, and more.
    /// </summary>
    /// <remarks>
    /// Note that when saving the DrawingAttributes, the V1 AntiAlias attribute
    /// is always set, and on load the AntiAlias property is ignored.
    /// </remarks>
    public class DrawingAttributes : INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        /// Creates a DrawingAttributes with default values
        /// </summary>
        public DrawingAttributes()
        {
            _extendedProperties = new ExtendedPropertyCollection();

            Initialize();
        }

        /// <summary>
        /// Internal only constructor that initializes a DA with an EPC
        /// </summary>
        /// <param name="extendedProperties"></param>
        internal DrawingAttributes(ExtendedPropertyCollection extendedProperties)
        {
            System.Diagnostics.Debug.Assert(extendedProperties != null);
            _extendedProperties = extendedProperties;

            Initialize();
        }

        /// <summary>
        /// Common constructor call, also called by Clone
        /// </summary>
        private void Initialize()
        {
            System.Diagnostics.Debug.Assert(_extendedProperties != null);
            _extendedProperties.Changed +=
                new ExtendedPropertiesChangedEventHandler(this.ExtendedPropertiesChanged_EventForwarder);
        }

        #endregion Constructors

        #region Public Properties

        /// <summary>
        /// The color of the Stroke
        /// </summary>
        public Color Color
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.Color))
                {
                    Debug.Assert(Colors.Black == (Color)GetDefaultDrawingAttributeValue(KnownIds.Color));
                    return Colors.Black;
                }
                return (Color)GetExtendedPropertyBackedProperty(KnownIds.Color);
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                // Validation of value is done in EPC
                SetExtendedPropertyBackedProperty(KnownIds.Color, value);
            }
        }

        /// <summary>
        /// The StylusTip used to draw the stroke
        /// </summary>
        public StylusTip StylusTip
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.StylusTip))
                {
                    Debug.Assert(StylusTip.Ellipse == (StylusTip)GetDefaultDrawingAttributeValue(KnownIds.StylusTip));
                    return StylusTip.Ellipse;
                }
                else
                {
                    //if we ever add to StylusTip enumeration, we need to just return GetExtendedPropertyBackedProperty
                    Debug.Assert(StylusTip.Rectangle == (StylusTip)GetExtendedPropertyBackedProperty(KnownIds.StylusTip));
                    return StylusTip.Rectangle;
                }
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                // Validation of value is done in EPC
                SetExtendedPropertyBackedProperty(KnownIds.StylusTip, value);
            }
        }

        /// <summary>
        /// The StylusTip used to draw the stroke
        /// </summary>
        public Matrix StylusTipTransform
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.StylusTipTransform))
                {
                    Debug.Assert(Matrix.Identity == (Matrix)GetDefaultDrawingAttributeValue(KnownIds.StylusTipTransform));
                    return Matrix.Identity;
                }
                return (Matrix)GetExtendedPropertyBackedProperty(KnownIds.StylusTipTransform);
            }
            set
            {
                Matrix m = (Matrix) value;
                if (m.OffsetX != 0 || m.OffsetY != 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidSttValue), "value");
                }
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                // Validation of value is done in EPC
                SetExtendedPropertyBackedProperty(KnownIds.StylusTipTransform, value);
            }
        }

        /// <summary>
        /// The height of the StylusTip
        /// </summary>
        public double Height
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.StylusHeight))
                {
                    Debug.Assert(DrawingAttributes.DefaultHeight == (double)GetDefaultDrawingAttributeValue(KnownIds.StylusHeight));
                    return DrawingAttributes.DefaultHeight;
                }
                return (double)GetExtendedPropertyBackedProperty(KnownIds.StylusHeight);
            }
            set
            {
                if (double.IsNaN(value) || value < MinHeight || value > MaxHeight)
                {
                    throw new ArgumentOutOfRangeException("Height", SR.Get(SRID.InvalidDrawingAttributesHeight));
                }
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                SetExtendedPropertyBackedProperty(KnownIds.StylusHeight, value);
            }
        }

        /// <summary>
        /// The width of the StylusTip
        /// </summary>
        public double Width
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.StylusWidth))
                {
                    Debug.Assert(DrawingAttributes.DefaultWidth == (double)GetDefaultDrawingAttributeValue(KnownIds.StylusWidth));
                    return DrawingAttributes.DefaultWidth;
                }
                return (double)GetExtendedPropertyBackedProperty(KnownIds.StylusWidth);
            }
            set
            {
                if (double.IsNaN(value) || value < MinWidth || value > MaxWidth)
                {
                    throw new ArgumentOutOfRangeException("Width", SR.Get(SRID.InvalidDrawingAttributesWidth));
                }
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                SetExtendedPropertyBackedProperty(KnownIds.StylusWidth, value);
            }
        }

        /// <summary>
        /// When true, ink will be rendered as a series of curves instead of as
        /// lines between Stylus sample points. This is useful for smoothing the ink, especially
        /// when the person writing the ink has jerky or shaky writing.
        /// This value is TRUE by default in the Avalon implementation
        /// </summary>
        public bool FitToCurve
        {
            get
            {
                DrawingFlags flags = (DrawingFlags)GetExtendedPropertyBackedProperty(KnownIds.DrawingFlags);
                return (0 != (flags & DrawingFlags.FitToCurve));
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                DrawingFlags flags = (DrawingFlags)GetExtendedPropertyBackedProperty(KnownIds.DrawingFlags);
                if (value)
                {
                    //turn on the bit
                    flags |= DrawingFlags.FitToCurve;
                }
                else
                {
                    //turn off the bit
                    flags &= ~DrawingFlags.FitToCurve;
                }
                SetExtendedPropertyBackedProperty(KnownIds.DrawingFlags, flags);
            }
        }

        /// <summary>
        /// When true, ink will be rendered with any available pressure information
        /// taken into account
        /// </summary>
        public bool IgnorePressure
        {
            get
            {
                DrawingFlags flags = (DrawingFlags)GetExtendedPropertyBackedProperty(KnownIds.DrawingFlags);
                return (0 != (flags & DrawingFlags.IgnorePressure));
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                DrawingFlags flags = (DrawingFlags)GetExtendedPropertyBackedProperty(KnownIds.DrawingFlags);
                if (value)
                {
                    //turn on the bit
                    flags |= DrawingFlags.IgnorePressure;
                }
                else
                {
                    //turn off the bit
                    flags &= ~DrawingFlags.IgnorePressure;
                }
                SetExtendedPropertyBackedProperty(KnownIds.DrawingFlags, flags);
            }
        }

        /// <summary>
        /// Determines if the stroke should be treated as a highlighter
        /// </summary>
        public bool IsHighlighter
        {
            get
            {
                //prevent boxing / unboxing if possible
                if (!_extendedProperties.Contains(KnownIds.IsHighlighter))
                {
                    Debug.Assert(false == (bool)GetDefaultDrawingAttributeValue(KnownIds.IsHighlighter));
                    return false;
                }
                else
                {
                    Debug.Assert(true == (bool)GetExtendedPropertyBackedProperty(KnownIds.IsHighlighter));
                    return true;
                }
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                SetExtendedPropertyBackedProperty(KnownIds.IsHighlighter, value);

                //
                // set RasterOp for V1 interop
                //
                if (value)
                {
                    _v1RasterOperation = DrawingAttributeSerializer.RasterOperationMaskPen;
                }
                else
                {
                    _v1RasterOperation = DrawingAttributeSerializer.RasterOperationDefaultV1;
                }
            }
        }

        #region Extended Properties
        /// <summary>
        /// Allows addition of objects to the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        /// <param name="propertyData"></param>
        public void AddPropertyData(Guid propertyDataId, object propertyData)
        {
            DrawingAttributes.ValidateStylusTipTransform(propertyDataId, propertyData);
            SetExtendedPropertyBackedProperty(propertyDataId, propertyData);
        }

        /// <summary>
        /// Allows removal of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public void RemovePropertyData(Guid propertyDataId)
        {
            this.ExtendedProperties.Remove(propertyDataId);
        }

        /// <summary>
        /// Allows retrieval of objects from the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public object GetPropertyData(Guid propertyDataId)
        {
            return GetExtendedPropertyBackedProperty(propertyDataId);
        }

        /// <summary>
        /// Allows retrieval of a Array of guids that are contained in the EPC
        /// </summary>
        public Guid[] GetPropertyDataIds()
        {
            return this.ExtendedProperties.GetGuidArray();
        }

        /// <summary>
        /// Allows check of containment of objects to the EPC
        /// </summary>
        /// <param name="propertyDataId"></param>
        public bool ContainsPropertyData(Guid propertyDataId)
        {
            return this.ExtendedProperties.Contains(propertyDataId);
        }

        /// <summary>
        /// ExtendedProperties
        /// </summary>
        internal ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                return _extendedProperties;
            }
        }


        /// <summary>
        /// Returns a copy of the EPC
        /// </summary>
        internal ExtendedPropertyCollection CopyPropertyData()
        {
            return this.ExtendedProperties.Clone();
        }


        #endregion



        #endregion

        #region Internal Properties

        /// <summary>
        /// StylusShape
        /// </summary>
        internal StylusShape StylusShape
        {
            get
            {
                StylusShape s;
                if (this.StylusTip == StylusTip.Rectangle)
                {
                    s =  new RectangleStylusShape(this.Width, this.Height);
                }
                else
                {
                    s = new EllipseStylusShape(this.Width, this.Height);
                }

                s.Transform = StylusTipTransform;
                return s;
            }
        }

        /// <summary>
        /// Sets the Fitting error for this drawing attributes
        /// </summary>
        internal int FittingError
        {
            get
            {
                if (!_extendedProperties.Contains(KnownIds.CurveFittingError))
                {
                    return 0;
                }
                else
                {
                    return (int)_extendedProperties[KnownIds.CurveFittingError];
                }
            }
            set
            {
                _extendedProperties[KnownIds.CurveFittingError] = value;
            }
        }

        /// <summary>
        /// Sets the Fitting error for this drawing attributes
        /// </summary>
        internal DrawingFlags DrawingFlags
        {
            get
            {
                return (DrawingFlags)GetExtendedPropertyBackedProperty(KnownIds.DrawingFlags);
            }
            set
            {
                //no need to raise change events, they will bubble up from the EPC
                //underneath us
                SetExtendedPropertyBackedProperty(KnownIds.DrawingFlags, value);
            }
        }


        /// <summary>
        /// we need to preserve this for round tripping
        /// </summary>
        /// <value></value>
        internal uint RasterOperation
        {
            get
            {
                return _v1RasterOperation;
            }
            set
            {
                _v1RasterOperation = value;
            }
        }

        /// <summary>
        /// When we load ISF from V1 if width is set and height is not
        /// and PenTip is Circle, we need to set height to the same as width
        /// or else we'll render different as an Ellipse.  We use this flag to 
        /// preserve state for round tripping.
        /// </summary>
        internal bool HeightChangedForCompatabity
        {
            get { return _heightChangedForCompatabity; }
            set { _heightChangedForCompatabity = value; }
        }

        #endregion

        //------------------------------------------------------
        //
        //  INotifyPropertyChanged Interface
        //
        //------------------------------------------------------

        #region INotifyPropertyChanged Interface

        /// <summary>
        /// INotifyPropertyChanged.PropertyChanged event
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        #endregion INotifyPropertyChanged Interface

        #region Methods

        #region Object overrides

        // What should ExtendedPropertyCollection.GetHashCode return?
        /// <summary>Retrieve an integer-based value for using ExtendedPropertyCollection
        /// objects in a hash table as keys</summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>Overload of the Equals method which determines if two DrawingAttributes
        /// objects contain the same drawing attributes</summary>
        public override bool Equals(object o)
        {
            if (o == null || o.GetType() != this.GetType())
            {
                return false;
            }

            //use as and check for null instead of casting to DA to make presharp happy
            DrawingAttributes that = o as DrawingAttributes;
            if (that == null)
            {
                return false; 
            }

            return (this._extendedProperties == that._extendedProperties);
        }

        /// <summary>Overload of the equality operator which determines
        /// if two DrawingAttributes are equal</summary>
        public static bool operator ==(DrawingAttributes first, DrawingAttributes second)
        {
            // compare the GC ptrs for the obvious reference equality
            if (((object)first == null && (object)second == null) ||
                ((object)first == (object)second))
            {
                return true;
            }
                // otherwise, if one of the ptrs are null, but not the other then return false
            else if ((object)first == null || (object)second == null)
            {
                return false;
            }
            // finally use the full `blown value-style comparison against the collection contents
            return first.Equals(second);
        }

        /// <summary>Overload of the not equals operator to determine if two
        /// DrawingAttributes are different</summary>
        public static bool operator !=(DrawingAttributes first, DrawingAttributes second)
        {
            return !(first == second);
        }
        #endregion

        /// <summary>
        /// Copies the DrawingAttributes
        /// </summary>
        /// <returns>Deep copy of the DrawingAttributes</returns>
        /// <remarks></remarks>
        public virtual DrawingAttributes Clone()
        {
            //
            // use MemberwiseClone, which will instance the most derived type
            // We use this instead of Activator.CreateInstance because it does not 
            // require ReflectionPermission.  One thing to note, all references 
            // are shared, including event delegates, so we need to set those to null
            //
            DrawingAttributes clone = (DrawingAttributes)this.MemberwiseClone();
            
            //
            // null the delegates in the cloned DrawingAttributes
            //
            clone.AttributeChanged = null;
            clone.PropertyDataChanged = null;

            //make a copy of the epc , set up listeners
            clone._extendedProperties = _extendedProperties.Clone();
            clone.Initialize();

            //don't need to clone these, it is a value type 
            //and is copied by MemberwiseClone
            //_v1RasterOperation
            //_heightChangedForCompatabity
            return clone;
        }
        #endregion

        #region Events

        /// <summary>
        /// Event fired whenever a DrawingAttribute is modified
        /// </summary>
        public event PropertyDataChangedEventHandler AttributeChanged;

        /// <summary>
        /// Method called when a change occurs to any DrawingAttribute
        /// </summary>
        /// <param name="e">The change information for the DrawingAttribute that was modified</param>
        protected virtual void OnAttributeChanged(PropertyDataChangedEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            try
            {
                PrivateNotifyPropertyChanged(e);
            }
            finally
            {
                if ( this.AttributeChanged != null )
                {
                    this.AttributeChanged(this, e);
                }
            }
        }

         /// <summary>
        /// Event fired whenever a DrawingAttribute is modified
        /// </summary>
        public event PropertyDataChangedEventHandler PropertyDataChanged;

        /// <summary>
        /// Method called when a change occurs to any PropertyData
        /// </summary>
        /// <param name="e">The change information for the PropertyData that was modified</param>
        protected virtual void OnPropertyDataChanged(PropertyDataChangedEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e", SR.Get(SRID.EventArgIsNull));
            }

            if (this.PropertyDataChanged != null)
            {
                this.PropertyDataChanged(this, e);
            }
        }


        #endregion

        #region Protected Methods

        /// <summary>
        /// Method called when a property change occurs to DrawingAttribute
        /// </summary>
        /// <param name="e">The EventArgs specifying the name of the changed property.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if ( _propertyChanged != null )
            {
                _propertyChanged(this, e);
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Simple helper method used to determine if a guid
        /// from an ExtendedProperty is used as the backing store
        /// of a DrawingAttribute
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static object GetDefaultDrawingAttributeValue(Guid id)
        {
            if (KnownIds.Color == id)
            {
                return Colors.Black;
            }
            if (KnownIds.StylusWidth == id)
            {
                return DrawingAttributes.DefaultWidth;
            }
            if (KnownIds.StylusTip == id)
            {
                return StylusTip.Ellipse;
            }
            if (KnownIds.DrawingFlags == id)
            {
                //note that in this implementation, FitToCurve is false by default
                return DrawingFlags.AntiAliased;
            }
            if (KnownIds.StylusHeight == id)
            {
                return DrawingAttributes.DefaultHeight;
            }
            if (KnownIds.StylusTipTransform == id)
            {
                return Matrix.Identity;
            }
            if (KnownIds.IsHighlighter == id)
            {
                return false;
            }
            // this is a valid case
            // as this helper method is used not only to
            // get the default value, but also to see if
            // the Guid is a drawing attribute value
            return null;
        }

        internal static void ValidateStylusTipTransform(Guid propertyDataId, object propertyData)
        {
            // 
            // Calling AddPropertyData(KnownIds.StylusTipTransform, "d") does not throw an ArgumentException.
            //  ExtendedPropertySerializer.Validate take a string as a valid type since StylusTipTransform
            //  gets serialized as a String, but at runtime is a Matrix
            if (propertyData == null)
            {
                throw new ArgumentNullException("propertyData");
            }
            else if (propertyDataId == KnownIds.StylusTipTransform)
            {
                // StylusTipTransform gets serialized as a String, but at runtime is a Matrix
                Type t = propertyData.GetType();
                if (t == typeof(String))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(Matrix)), "propertyData");
                }
            }
        }

        /// <summary>
        /// Simple helper method used to determine if a guid
        /// needs to be removed from the ExtendedPropertyCollection in ISF
        /// before serializing
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal static bool RemoveIdFromExtendedProperties(Guid id)
        {
            if (KnownIds.Color == id ||
                KnownIds.Transparency == id ||
                KnownIds.StylusWidth == id ||
                KnownIds.DrawingFlags == id ||
                KnownIds.StylusHeight == id ||
                KnownIds.CurveFittingError == id )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if two DrawingAttributes lead to the same PathGeometry.
        /// </summary>
        internal static bool GeometricallyEqual(DrawingAttributes left, DrawingAttributes right)
        {
            // Optimization case:
            // must correspond to the same path geometry if they refer to the same instance.
            if (Object.ReferenceEquals(left, right))
            {
                return true;
            }

            if (left.StylusTip == right.StylusTip &&
                left.StylusTipTransform == right.StylusTipTransform &&
                DoubleUtil.AreClose(left.Width, right.Width) &&
                DoubleUtil.AreClose(left.Height, right.Height) &&
                left.DrawingFlags == right.DrawingFlags /*contains IgnorePressure / FitToCurve*/)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the guid passed in has impact on geometry of the stroke
        /// </summary>
        internal static bool IsGeometricalDaGuid(Guid guid)
        {
            // Assert it is a DA guid
            System.Diagnostics.Debug.Assert(null != DrawingAttributes.GetDefaultDrawingAttributeValue(guid));

            if (guid == KnownIds.StylusHeight || guid == KnownIds.StylusWidth ||
                guid == KnownIds.StylusTipTransform || guid == KnownIds.StylusTip ||
                guid == KnownIds.DrawingFlags)
            {
                return true;
            }

            return false;
        }



        /// <summary>
        /// Whenever the base class fires the generic ExtendedPropertiesChanged
        /// event, we need to fire the DrawingAttributesChanged event also.
        /// </summary>
        /// <param name="sender">Should be 'this' object</param>
        /// <param name="args">The custom attributes that changed</param>
        private void ExtendedPropertiesChanged_EventForwarder(object sender, ExtendedPropertiesChangedEventArgs args)
        {
            System.Diagnostics.Debug.Assert(sender != null);
            System.Diagnostics.Debug.Assert(args != null);

            //see if the EP that changed is a drawingattribute
            if (args.NewProperty == null)
            {
                //a property was removed, see if it is a drawing attribute property
                object defaultValueIfDrawingAttribute
                    = DrawingAttributes.GetDefaultDrawingAttributeValue(args.OldProperty.Id);
                if (defaultValueIfDrawingAttribute != null)
                {
                    ExtendedProperty newProperty =
                        new ExtendedProperty(   args.OldProperty.Id,
                                                defaultValueIfDrawingAttribute);
                    //this is a da guid
                    PropertyDataChangedEventArgs dargs =
                        new PropertyDataChangedEventArgs(  args.OldProperty.Id,
                                                                newProperty.Value,      //the property
                                                                args.OldProperty.Value);//previous value

                    this.OnAttributeChanged(dargs);
                }
                else
                {
                    PropertyDataChangedEventArgs dargs =
                        new PropertyDataChangedEventArgs(  args.OldProperty.Id,
                                                                null,      //the property
                                                                args.OldProperty.Value);//previous value

                    this.OnPropertyDataChanged(dargs);
}
            }
            else if (args.OldProperty == null)
            {
                //a property was added, see if it is a drawing attribute property
                object defaultValueIfDrawingAttribute
                    = DrawingAttributes.GetDefaultDrawingAttributeValue(args.NewProperty.Id);
                if (defaultValueIfDrawingAttribute != null)
                {
                    if (!defaultValueIfDrawingAttribute.Equals(args.NewProperty.Value))
                    {
                        //this is a da guid
                        PropertyDataChangedEventArgs dargs =
                            new PropertyDataChangedEventArgs(  args.NewProperty.Id,
                                                                    args.NewProperty.Value,   //the property
                                                                    defaultValueIfDrawingAttribute);     //previous value

                        this.OnAttributeChanged(dargs);
                    }
                }
                else
                {
                    PropertyDataChangedEventArgs dargs =
                        new PropertyDataChangedEventArgs(args.NewProperty.Id,
                                                         args.NewProperty.Value,   //the property
                                                         null);     //previous value
                    this.OnPropertyDataChanged(dargs);
}
            }
            else
            {
                //something was modified, see if it is a drawing attribute property
                object defaultValueIfDrawingAttribute
                    = DrawingAttributes.GetDefaultDrawingAttributeValue(args.NewProperty.Id);
                if (defaultValueIfDrawingAttribute != null)
                {
                    //
                    // we only raise DA changed when the value actually changes
                    //
                    if (!args.NewProperty.Value.Equals(args.OldProperty.Value))
                    {
                        //this is a da guid
                        PropertyDataChangedEventArgs dargs =
                            new PropertyDataChangedEventArgs(  args.NewProperty.Id,
                                                                    args.NewProperty.Value,       //the da
                                                                    args.OldProperty.Value);//old value

                        this.OnAttributeChanged(dargs);
                    }
                }
                else
                {
                    if (!args.NewProperty.Value.Equals(args.OldProperty.Value))
                    {
                        PropertyDataChangedEventArgs dargs =
                            new PropertyDataChangedEventArgs(  args.NewProperty.Id,
                                                                    args.NewProperty.Value,
                                                                    args.OldProperty.Value);//old value

                        this.OnPropertyDataChanged(dargs);
                    }
                }
            }
        }

        /// <summary>
        /// All DrawingAttributes are backed by an ExtendedProperty
        /// this is a simple helper to set a property
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="value">value</param>
        private void SetExtendedPropertyBackedProperty(Guid id, object value)
        {
            if (_extendedProperties.Contains(id))
            {
                //
                // check to see if we're setting the property back
                // to a default value.  If we are we should remove it from
                // the EPC
                //
                object defaultValue = DrawingAttributes.GetDefaultDrawingAttributeValue(id);
                if (defaultValue != null)
                {
                    if (defaultValue.Equals(value))
                    {
                        _extendedProperties.Remove(id);
                        return;
                    }
                }
                //
                // we're setting a non-default value on a EP that
                // already exists, check for equality before we do
                // so we don't raise unnecessary EPC changed events
                //
                object o = GetExtendedPropertyBackedProperty(id);
                if (!o.Equals(value))
                {
                    _extendedProperties[id] = value;
                }
            }
            else
            {
                //
                // make sure we're not setting a default value of the guid
                // there is no need to do this
                //
                object defaultValue = DrawingAttributes.GetDefaultDrawingAttributeValue(id);
                if (defaultValue == null || !defaultValue.Equals(value))
                {
                    _extendedProperties[id] = value;
                }
            }
        }

        /// <summary>
        /// All DrawingAttributes are backed by an ExtendedProperty
        /// this is a simple helper to set a property
        /// </summary>
        /// <param name="id">id</param>
        /// <returns></returns>
        private object GetExtendedPropertyBackedProperty(Guid id)
        {
            if (!_extendedProperties.Contains(id))
            {
                if (null != DrawingAttributes.GetDefaultDrawingAttributeValue(id))
                {
                    return DrawingAttributes.GetDefaultDrawingAttributeValue(id);
                }
                throw new ArgumentException(SR.Get(SRID.EPGuidNotFound), "id");
            }
            else
            {
                return _extendedProperties[id];
            }
        }

        /// <summary>
        /// A help method which fires INotifyPropertyChanged.PropertyChanged event
        /// </summary>
        /// <param name="e"></param>
        private void PrivateNotifyPropertyChanged(PropertyDataChangedEventArgs e)
        {
            if ( e.PropertyGuid == KnownIds.Color)
            {
                OnPropertyChanged("Color");
            }
            else if ( e.PropertyGuid == KnownIds.StylusTip)
            {
                OnPropertyChanged("StylusTip");
            }
            else if ( e.PropertyGuid == KnownIds.StylusTipTransform)
            {
                OnPropertyChanged("StylusTipTransform");
            }
            else if ( e.PropertyGuid == KnownIds.StylusHeight)
            {
                OnPropertyChanged("Height");
            }
            else if ( e.PropertyGuid == KnownIds.StylusWidth)
            {
                OnPropertyChanged("Width");
            }
            else if ( e.PropertyGuid == KnownIds.IsHighlighter)
            {
                OnPropertyChanged("IsHighlighter");
            }
            else if ( e.PropertyGuid == KnownIds.DrawingFlags )
            {
                DrawingFlags changedBits = ( ( (DrawingFlags)e.PreviousValue ) ^ ( (DrawingFlags)e.NewValue ) );

                // NOTICE-2006/01/20-WAYNEZEN,
                // If someone changes FitToCurve and IgnorePressure simultaneously via AddPropertyData/RemovePropertyData,
                // we will fire both OnPropertyChangeds in advance the order of the values.
                if ( (changedBits & DrawingFlags.FitToCurve) != 0 )
                {
                    OnPropertyChanged("FitToCurve");
                }

                if ( (changedBits & DrawingFlags.IgnorePressure) != 0 )
                {
                    OnPropertyChanged("IgnorePressure");
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Private Fields

        // The private PropertyChanged event
        private PropertyChangedEventHandler             _propertyChanged;

        private ExtendedPropertyCollection              _extendedProperties;
        private uint                                    _v1RasterOperation = DrawingAttributeSerializer.RasterOperationDefaultV1;
        private bool                                    _heightChangedForCompatabity = false;

        /// <summary>
        /// Statics
        /// </summary>
        internal static readonly float StylusPrecision = 1000.0f;
        internal static readonly double DefaultWidth = 2.0031496062992127;
        internal static readonly double DefaultHeight = 2.0031496062992127;


        #endregion

        /// <summary>
        /// Mininum acceptable stylus tip height, corresponds to 0.001 in V1
        /// </summary>
        /// <remarks>corresponds to 0.001 in V1  (0.001 / (2540/96))</remarks>
        public static readonly double MinHeight = 0.00003779527559055120;

        /// <summary>
        /// Minimum acceptable stylus tip width
        /// </summary>
        /// <remarks>corresponds to 0.001 in V1  (0.001 / (2540/96))</remarks>
        public static readonly double MinWidth =  0.00003779527559055120;

        /// <summary>
        /// Maximum acceptable stylus tip height.
        /// </summary>
        /// <remarks>corresponds to 4294967 in V1 (4294967 / (2540/96))</remarks>
        public static readonly double MaxHeight = 162329.4614173230;
                          

        /// <summary>
        /// Maximum acceptable stylus tip width.
        /// </summary>
        /// <remarks>corresponds to 4294967 in V1 (4294967 / (2540/96))</remarks>
        public static readonly double MaxWidth = 162329.4614173230;
    }
}
