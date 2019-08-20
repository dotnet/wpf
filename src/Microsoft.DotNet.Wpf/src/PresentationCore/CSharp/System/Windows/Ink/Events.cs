// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Ink
{
    #region Public APIs

    // ===========================================================================================
    /// <summary>
    /// delegate used for event handlers that are called when a stroke was was added, removed, or modified inside of a Stroke collection
    /// </summary>
    public delegate void StrokeCollectionChangedEventHandler(object sender, StrokeCollectionChangedEventArgs e);

    /// <summary>
    /// Event arg used when delegate a stroke is was added, removed, or modified inside of a Stroke collection
    /// </summary>
    public class StrokeCollectionChangedEventArgs : EventArgs
    {
        private StrokeCollection.ReadOnlyStrokeCollection _added;
        private StrokeCollection.ReadOnlyStrokeCollection _removed;
        private int _index = -1;

        /// <summary>Constructor</summary>
        internal StrokeCollectionChangedEventArgs(StrokeCollection added, StrokeCollection removed, int index) :
            this(added, removed)
        {
            _index = index;
        }

        /// <summary>Constructor</summary>
        public StrokeCollectionChangedEventArgs(StrokeCollection added, StrokeCollection removed)
        {
            if ( added == null && removed == null )
            {
                throw new ArgumentException(SR.Get(SRID.CannotBothBeNull, "added", "removed"));
            }
            _added = ( added == null ) ? null : new StrokeCollection.ReadOnlyStrokeCollection(added);
            _removed = ( removed == null ) ? null : new StrokeCollection.ReadOnlyStrokeCollection(removed);
        }

        /// <summary>Set of strokes that where added, result may be an empty collection</summary>
        public StrokeCollection Added
        {
            get
            {
                if ( _added == null )
                {
                    _added = new StrokeCollection.ReadOnlyStrokeCollection(new StrokeCollection());
                }
                return _added;
            }
        }

        /// <summary>Set of strokes that where removed, result may be an empty collection</summary>
        public StrokeCollection Removed
        {
            get
            {
                if ( _removed == null )
                {
                    _removed = new StrokeCollection.ReadOnlyStrokeCollection(new StrokeCollection());
                }
                return _removed;
            }
        }

        /// <summary>
        /// The zero based starting index that was affected
        /// </summary>
        internal int Index
        {
            get
            {
                return _index;
            }
        }
    }

    // ===========================================================================================
    /// <summary>
    /// delegate used for event handlers that are called when a change to the drawing attributes associated with one or more strokes has occurred.
    /// </summary>
    public delegate void PropertyDataChangedEventHandler(object sender, PropertyDataChangedEventArgs e);

    /// <summary>
    /// Event arg used a change to the drawing attributes associated with one or more strokes has occurred.
    /// </summary>
    public class PropertyDataChangedEventArgs : EventArgs
    {
        private Guid _propertyGuid;
        private object _newValue;
        private object _previousValue;

        /// <summary>Constructor</summary>
        public PropertyDataChangedEventArgs(Guid propertyGuid,
                                            object newValue,
                                            object previousValue)
        {
            if ( newValue == null && previousValue == null )
            {
                throw new ArgumentException(SR.Get(SRID.CannotBothBeNull, "newValue", "previousValue"));
            }

            _propertyGuid = propertyGuid;
            _newValue = newValue;
            _previousValue = previousValue;
        }

        /// <summary>
        /// Gets the property guid that represents the DrawingAttribute that changed
        /// </summary>
        public Guid PropertyGuid
        {
            get { return _propertyGuid; }
        }

        /// <summary>
        /// Gets the new value of the DrawingAttribute
        /// </summary>
        public object NewValue
        {
            get { return _newValue; }
        }

        /// <summary>
        /// Gets the previous value of the DrawingAttribute
        /// </summary>
        public object PreviousValue
        {
            get { return _previousValue; }
        }
    }



    // ===========================================================================================
    /// <summary>
    /// delegate used for event handlers that are called when the Custom attributes associated with an object have changed.
    /// </summary>
    internal delegate void ExtendedPropertiesChangedEventHandler(object sender, ExtendedPropertiesChangedEventArgs e);

    /// <summary>
    /// Event Arg used when the Custom attributes associated with an object have changed.
    /// </summary>
    internal class ExtendedPropertiesChangedEventArgs : EventArgs
    {
        private ExtendedProperty _oldProperty;
        private ExtendedProperty _newProperty;

        /// <summary>Constructor</summary>
        internal ExtendedPropertiesChangedEventArgs(ExtendedProperty oldProperty,
                                                    ExtendedProperty newProperty)
        {
            if ( oldProperty == null && newProperty == null )
            {
                throw new ArgumentNullException("oldProperty");
            }
            _oldProperty = oldProperty;
            _newProperty = newProperty;
        }

        /// <summary>
        /// The value of the previous property.  If the Changed event was caused
        /// by an ExtendedProperty being added, this value is null
        /// </summary>
        internal ExtendedProperty OldProperty
        {
            get { return _oldProperty; }
        }

        /// <summary>
        /// The value of the new property.  If the Changed event was caused by 
        /// an ExtendedProperty being removed, this value is null
        /// </summary>
        internal ExtendedProperty NewProperty
        {
            get { return _newProperty; }
        }
    }

    /// <summary>
    /// The delegate to use for the DefaultDrawingAttributesReplaced event
    /// </summary>
    public delegate void DrawingAttributesReplacedEventHandler(object sender, DrawingAttributesReplacedEventArgs e);

    /// <summary>
    ///    DrawingAttributesReplacedEventArgs
    /// </summary>
    public class DrawingAttributesReplacedEventArgs : EventArgs
    {
        /// <summary>
        /// DrawingAttributesReplacedEventArgs
        /// </summary>
        /// <remarks>
        /// This must be public so InkCanvas can instance it
        /// </remarks>
        public DrawingAttributesReplacedEventArgs(DrawingAttributes newDrawingAttributes, DrawingAttributes previousDrawingAttributes)
        {
            if ( newDrawingAttributes == null )
            {
                throw new ArgumentNullException("newDrawingAttributes");
            }
            if ( previousDrawingAttributes == null )
            {
                throw new ArgumentNullException("previousDrawingAttributes");
            }
            _newDrawingAttributes = newDrawingAttributes;
            _previousDrawingAttributes = previousDrawingAttributes;
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public DrawingAttributes NewDrawingAttributes
        {
            get { return _newDrawingAttributes; }
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public DrawingAttributes PreviousDrawingAttributes
        {
            get { return _previousDrawingAttributes; }
        }

        private DrawingAttributes _newDrawingAttributes;
        private DrawingAttributes _previousDrawingAttributes;
}

    /// <summary>
    /// The delegate to use for the StylusPointsReplaced event
    /// </summary>
    public delegate void StylusPointsReplacedEventHandler(object sender, StylusPointsReplacedEventArgs e);

    /// <summary>
    ///    StylusPointsReplacedEventArgs
    /// </summary>
    public class StylusPointsReplacedEventArgs : EventArgs
    {
        /// <summary>
        /// StylusPointsReplacedEventArgs
        /// </summary>
        /// <remarks>
        /// This must be public so InkCanvas can instance it
        /// </remarks>
        public StylusPointsReplacedEventArgs(StylusPointCollection newStylusPoints, StylusPointCollection previousStylusPoints)
        {
            if ( newStylusPoints == null )
            {
                throw new ArgumentNullException("newStylusPoints");
            }
            if ( previousStylusPoints == null )
            {
                throw new ArgumentNullException("previousStylusPoints");
            }
            _newStylusPoints = newStylusPoints;
            _previousStylusPoints = previousStylusPoints;
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public StylusPointCollection NewStylusPoints
        {
            get { return _newStylusPoints; }
        }

        /// <summary>
        /// [TBS]
        /// </summary>
        public StylusPointCollection PreviousStylusPoints
        {
            get { return _previousStylusPoints; }
        }

        private StylusPointCollection _newStylusPoints;
        private StylusPointCollection _previousStylusPoints;
}

    #endregion
}
