// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Set of static methods implementing text range serialization
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Windows;
    using System.Globalization;
    using System.Windows.Media;

    /// <summary>
    /// An object implementing ITypeDescriptorContext intended to be used in serialization
    /// scenarios for checking whether a particular value can be converted to a string
    /// </summary>
    internal class DPTypeDescriptorContext : System.ComponentModel.ITypeDescriptorContext
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        private DPTypeDescriptorContext(DependencyProperty property, object propertyValue)
        {
            Invariant.Assert(property != null, "property == null");
            Invariant.Assert(propertyValue != null, "propertyValue == null");
            Invariant.Assert(property.IsValidValue(propertyValue), "propertyValue must be of suitable type for the given dependency property");

            _property = property;
            _propertyValue = propertyValue;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns non-null string if this value can be converted to a string,
        // null otherwise.
        internal static string GetStringValue(DependencyProperty property, object propertyValue)
        {
            string stringValue = null;

            // Special cases working around incorrectly implemented type converters
            if (property == UIElement.BitmapEffectProperty)
            {
                return null; // Always treat BitmapEffects as complex value
            }

            if (property == Inline.TextDecorationsProperty)
            {
                stringValue = TextDecorationsFixup((TextDecorationCollection)propertyValue);
            }
            else if (typeof(CultureInfo).IsAssignableFrom(property.PropertyType)) //NumberSubstitution.CultureOverrideProperty
            {
                stringValue = CultureInfoFixup(property, (CultureInfo)propertyValue);
            }

            if (stringValue == null)
            {
                DPTypeDescriptorContext context = new DPTypeDescriptorContext(property, propertyValue);

                System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(property.PropertyType);
                Invariant.Assert(typeConverter != null);
                if (typeConverter.CanConvertTo(context, typeof(string)))
                {
                    stringValue = (string)typeConverter.ConvertTo(
                        context, System.Globalization.CultureInfo.InvariantCulture, propertyValue, typeof(string));
                }
            }
            return stringValue;
        }

        #endregion Internal Methods
        #region Private Methods
        private static string TextDecorationsFixup(TextDecorationCollection textDecorations)
        {
            string stringValue = null;
            // Work around for incorrect serialization for TextDecorations property
            //  This code must be in TextDecorationCollectionConverter.cs type converter

            // Special case for TextDecorations serialization
            if (TextDecorations.Underline.ValueEquals(textDecorations))
            {
                stringValue = "Underline";
            }
            else if (TextDecorations.Strikethrough.ValueEquals(textDecorations))
            {
                stringValue = "Strikethrough";
            }
            else if (TextDecorations.OverLine.ValueEquals(textDecorations))
            {
                stringValue = "OverLine";
            }
            else if (TextDecorations.Baseline.ValueEquals(textDecorations))
            {
                stringValue = "Baseline";
            }
            else if (textDecorations.Count == 0)
            {
                stringValue = string.Empty;
            }

            return stringValue;
        }

        private static string CultureInfoFixup(DependencyProperty property, CultureInfo cultureInfo)
        {
            string stringValue = null;

            // Parser uses a specific type coverter for converting instances of other types to and from CultureInfo.
            // This class differs from System.ComponentModel.CultureInfoConverter, the default type converter 
            // for the CultureInfo class. 
            // It uses a string representation based on the IetfLanguageTag property rather than the Name property 
            // (i.e., RFC 3066 rather than RFC 1766). 
            // In order to guarantee roundtripability of serialized xaml, textrange serialization needs to use
            // this type coverter for CultureInfo types.

            DPTypeDescriptorContext context = new DPTypeDescriptorContext(property, cultureInfo);
            System.ComponentModel.TypeConverter typeConverter = new CultureInfoIetfLanguageTagConverter();

            if (typeConverter.CanConvertTo(context, typeof(string)))
            {
                stringValue = (string)typeConverter.ConvertTo(
                    context, System.Globalization.CultureInfo.InvariantCulture, cultureInfo, typeof(string));
            }
            return stringValue;
        }
        #endregion  Private Methods
        //------------------------------------------------------
        //
        //  Interface ITypeDescriptorContext
        //
        //------------------------------------------------------

        #region ITypeDescriptorContext Members

        System.ComponentModel.IContainer System.ComponentModel.ITypeDescriptorContext.Container
        {
            get { return null; }
        }

        // Returns a value of a property - to be detected for convertability to string in a type converter
        object System.ComponentModel.ITypeDescriptorContext.Instance
        {
            get 
            { 
                return _propertyValue; 
            }
        }

        void System.ComponentModel.ITypeDescriptorContext.OnComponentChanged()
        {
        }

        bool System.ComponentModel.ITypeDescriptorContext.OnComponentChanging()
        {
            return false;
        }

        System.ComponentModel.PropertyDescriptor System.ComponentModel.ITypeDescriptorContext.PropertyDescriptor
        {
            get { return null; }
        }

        #endregion

        #region IServiceProvider Members

        object IServiceProvider.GetService(Type serviceType)
        {
            return null;
        }

        #endregion

        #region Private Fields

        private DependencyProperty _property;
        private object _propertyValue;

        #endregion
    }
}
