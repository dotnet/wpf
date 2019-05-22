// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DefaultValueFactory for StrokeCollection
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace MS.Internal.Ink
{
    internal class StrokeCollectionDefaultValueFactory : DefaultValueFactory
    {
        internal StrokeCollectionDefaultValueFactory()
        {
        }

        /// <summary>
        ///     Returns an "immutable" default value. Since we can't make the default value
        ///     read only we'll return a new one every time.
        /// </summary>
        internal override object DefaultValue 
        {
            get
            {
                return new StrokeCollection();
            }
        }

        /// <summary>
        ///     Creates a mutable default value
        /// </summary>
        internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            Debug.Assert(property.PropertyType == typeof(StrokeCollection), 
                string.Format(System.Globalization.CultureInfo.InvariantCulture, "The DependencyProperty {0} has to be type of StrokeCollection.", property));

            // Instantiate our default value instance.
            StrokeCollection defaultValue = new StrokeCollection();

            // Add event handlers for tracking the changes on the default value instance.
            StrokeCollectionDefaultPromoter promoter = new StrokeCollectionDefaultPromoter(owner, property);

            defaultValue.StrokesChanged +=
                new StrokeCollectionChangedEventHandler(promoter.OnStrokeCollectionChanged<StrokeCollectionChangedEventArgs>);
            defaultValue.PropertyDataChanged +=
                new PropertyDataChangedEventHandler(promoter.OnStrokeCollectionChanged<PropertyDataChangedEventArgs>);

            return defaultValue;
        }

        /// <summary>
        /// A tracking class which monitors any changes on StrokeCollection
        /// </summary>
        private class StrokeCollectionDefaultPromoter
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner"></param>
            /// <param name="property"></param>
            internal StrokeCollectionDefaultPromoter(DependencyObject owner, DependencyProperty property)
            {
                _owner = owner;
                _dependencyProperty = property;
            }

            /// <summary>
            /// A handler for both AttributeChanged and PropertyDataChanged.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            internal void OnStrokeCollectionChanged<TEventArgs>(object sender, TEventArgs e)
            {
                StrokeCollection value = (StrokeCollection)sender;

                // The current instance will be promoted to the local value other than the default value.
                // Then we could just remove our handlers to stop tracking.
                value.StrokesChanged -= 
                    new StrokeCollectionChangedEventHandler(OnStrokeCollectionChanged<StrokeCollectionChangedEventArgs>);
                value.PropertyDataChanged -=
                    new PropertyDataChangedEventHandler(OnStrokeCollectionChanged<PropertyDataChangedEventArgs>);

                // 
                // We only promote the value when there is no local value set yet.
                if ( _owner.ReadLocalValue(_dependencyProperty) == DependencyProperty.UnsetValue )
                {
                    // Promote the instance to the local value.
                    _owner.SetValue(_dependencyProperty, value);
                }

                // Remove this value from the DefaultValue cache so we stop
                // handing it out as the default value now that it has changed.
                PropertyMetadata metadata = _dependencyProperty.GetMetadata(_owner.DependencyObjectType);
                metadata.ClearCachedDefaultValue(_owner, _dependencyProperty);
            }

            private readonly DependencyObject   _owner;
            private readonly DependencyProperty _dependencyProperty;
        }
    }
}
