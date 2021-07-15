// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DefaultValueFactory for DrawingAttributes
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;

namespace MS.Internal.Ink
{
    internal class DrawingAttributesDefaultValueFactory : DefaultValueFactory
    {
        internal DrawingAttributesDefaultValueFactory()
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
                return new DrawingAttributes();
            }
        }

        /// <summary>
        ///     Creates a mutable default value
        /// </summary>
        internal override object CreateDefaultValue(DependencyObject owner, DependencyProperty property)
        {
            // Instantiate our default value instance.
            DrawingAttributes defaultValue = new DrawingAttributes();

            // Add event handlers for tracking the changes on the default value instance.
            DrawingAttributesDefaultPromoter promoter = new DrawingAttributesDefaultPromoter((InkCanvas)owner);
            
            defaultValue.AttributeChanged += new PropertyDataChangedEventHandler(promoter.OnDrawingAttributesChanged);
            defaultValue.PropertyDataChanged += new PropertyDataChangedEventHandler(promoter.OnDrawingAttributesChanged);

            return defaultValue;
        }

        /// <summary>
        /// A tracking class which monitors the sub-property changes on DrawingAttributes
        /// </summary>
        private class DrawingAttributesDefaultPromoter
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner"></param>
            internal DrawingAttributesDefaultPromoter(InkCanvas owner)
            {
                _owner = owner;
            }

            /// <summary>
            /// A handler for both AttributeChanged and PropertyDataChanged.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            internal void OnDrawingAttributesChanged(object sender, PropertyDataChangedEventArgs e)
            {
                DrawingAttributes value = (DrawingAttributes)sender;

                // The current instance will be promoted to the local value other than the default value.
                // Then we could just remove our handlers to stop tracking.
                value.AttributeChanged -= new PropertyDataChangedEventHandler(OnDrawingAttributesChanged);
                value.PropertyDataChanged -= new PropertyDataChangedEventHandler(OnDrawingAttributesChanged);

                // 
                // We only promote the value when there is no local value set yet.
                if (_owner.ReadLocalValue(InkCanvas.DefaultDrawingAttributesProperty) == DependencyProperty.UnsetValue)
                {
                    // Promote the instance to the local value.
                    _owner.SetValue(InkCanvas.DefaultDrawingAttributesProperty, value);
                }

                // Remove this value from the DefaultValue cache so we stop
                // handing it out as the default value now that it has changed.
                PropertyMetadata metadata = InkCanvas.DefaultDrawingAttributesProperty.GetMetadata(_owner.DependencyObjectType);
                metadata.ClearCachedDefaultValue(_owner, InkCanvas.DefaultDrawingAttributesProperty);
            }

            private readonly InkCanvas _owner;
        }
    }
}
