// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   TypeConverter for SystemResourceKey and SystemThemeKey.
//
//

using System;
using System.ComponentModel;
using System.Globalization;
using System.ComponentModel.Design.Serialization;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace System.Windows.Markup
{
    /// <summary>
    ///     Common TypeConverter functionality SystemThemeKey and SystemResourceKey; each
    ///     is an internal type, so gets serialized as an {x:Static} reference.
    /// </summary>

    internal class SystemKeyConverter : TypeConverter
    {
        /// <summary>
        ///     TypeConverter method override.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="sourceType">
        ///     Type to convert from
        /// </param>
        /// <returns>
        ///     true if conversion is possible
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException("sourceType");
            }

            return base.CanConvertFrom(context, sourceType);
        }
    
        /// <summary>
        ///     TypeConverter method override.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="destinationType">
        ///     Type to convert to
        /// </param>
        /// <returns>
        ///     true if conversion is possible
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) 
        {
            // Validate Input Arguments
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            else if( destinationType == typeof(MarkupExtension) && context is IValueSerializerContext  )
            {
                return true;
            }
            
            return base.CanConvertTo(context, destinationType);
        }
        
        /// <summary>
        ///     TypeConverter method implementation.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="culture">
        ///     current culture (see CLR specs)
        /// </param>
        /// <param name="value">
        ///     value to convert from
        /// </param>
        /// <returns>
        ///     value that is result of conversion
        /// </returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///     TypeConverter method implementation.
        /// </summary>
        /// <param name="context">
        ///     ITypeDescriptorContext
        /// </param>
        /// <param name="culture">
        ///     current culture (see CLR specs)
        /// </param>
        /// <param name="value">
        ///     value to convert from
        /// </param>
        /// <param name="destinationType">
        ///     Type to convert to
        /// </param>
        /// <returns>
        ///     converted value
        /// </returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
                        
            // Input validation
            
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            
            if (destinationType == typeof(MarkupExtension)
                &&
                CanConvertTo(context, destinationType) )
            {
                SystemResourceKeyID keyId;

                // Get the SystemResourceKeyID
                
                if( value is SystemResourceKey )
                {
                    keyId = (value as SystemResourceKey).InternalKey;
                }
                else if( value is SystemThemeKey )
                {
                    keyId = (value as SystemThemeKey).InternalKey;
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "SystemResourceKey or SystemThemeKey")); 
                }

                // System resource keys can be converted into a MarkupExtension (StaticExtension)
                
                Type keyType = SystemKeyConverter.GetSystemClassType(keyId);

                // Get the value serialization context
                IValueSerializerContext valueSerializerContext = context as IValueSerializerContext;
                if( valueSerializerContext != null )
                {
                    // And from that get a System.Type serializer
                    ValueSerializer typeSerializer = valueSerializerContext.GetValueSerializerFor(typeof(Type));
                    if( typeSerializer != null )
                    {
                        // And use that to create the string-ized class name
                        string systemClassName = typeSerializer.ConvertToString(keyType, valueSerializerContext);

                        // And finally create the StaticExtension.
                        return new StaticExtension( systemClassName + "." + GetSystemKeyName(keyId) );
                    }
                }
            }
            
            return base.CanConvertTo(context, destinationType);
        }



        internal static Type GetSystemClassType(SystemResourceKeyID id)
        {
            if (((SystemResourceKeyID.InternalSystemColorsStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemColorsEnd))||
                ((SystemResourceKeyID.InternalSystemColorsExtendedStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemColorsExtendedEnd)))
            {
                return typeof(SystemColors);
            }
            else if ((SystemResourceKeyID.InternalSystemFontsStart < id) &&
                     (id < SystemResourceKeyID.InternalSystemFontsEnd))
            {
                return typeof(SystemFonts);
            }
            else if ((SystemResourceKeyID.InternalSystemParametersStart < id) &&
                     (id < SystemResourceKeyID.InternalSystemParametersEnd))
            {
                return typeof(SystemParameters);
            }
            else if (SystemResourceKeyID.MenuItemSeparatorStyle == id)
            {
                return typeof(MenuItem);
            }
            else if ((SystemResourceKeyID.ToolBarButtonStyle <= id) &&
                     (id <= SystemResourceKeyID.ToolBarMenuStyle))
            {
                return typeof(ToolBar);
            }
            else if (SystemResourceKeyID.StatusBarSeparatorStyle == id)
            {
                return typeof(StatusBar);
            }
            else if ((SystemResourceKeyID.GridViewScrollViewerStyle <= id) &&
                     (id <= SystemResourceKeyID.GridViewItemContainerStyle))
            {
                return typeof(GridView);
            }

            return null;
        }

        internal static string GetSystemClassName(SystemResourceKeyID id)
        {
            if (((SystemResourceKeyID.InternalSystemColorsStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemColorsEnd))||
                ((SystemResourceKeyID.InternalSystemColorsExtendedStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemColorsExtendedEnd)))
            {
                return "SystemColors";
            }
            else if ((SystemResourceKeyID.InternalSystemFontsStart < id) &&
                     (id < SystemResourceKeyID.InternalSystemFontsEnd))
            {
                return "SystemFonts";
            }
            else if ((SystemResourceKeyID.InternalSystemParametersStart < id) &&
                     (id < SystemResourceKeyID.InternalSystemParametersEnd))
            {
                return "SystemParameters";
            }
            else if (SystemResourceKeyID.MenuItemSeparatorStyle == id)
            {
                return "MenuItem";
            }
            else if ((SystemResourceKeyID.ToolBarButtonStyle <= id) &&
                     (id <= SystemResourceKeyID.ToolBarMenuStyle))
            {
                return "ToolBar";
            }
            else if (SystemResourceKeyID.StatusBarSeparatorStyle == id)
            {
                return "StatusBar";
            }
            else if ((SystemResourceKeyID.GridViewScrollViewerStyle <= id) &&
                     (id <= SystemResourceKeyID.GridViewItemContainerStyle))
            {
                return "GridView";
            }

            return String.Empty;
        }

        internal static string GetSystemKeyName(SystemResourceKeyID id)
        {
            if (((SystemResourceKeyID.InternalSystemColorsStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemParametersEnd))||
                ((SystemResourceKeyID.InternalSystemColorsExtendedStart < id) &&
                  (id < SystemResourceKeyID.InternalSystemColorsExtendedEnd))||
                ((SystemResourceKeyID.GridViewScrollViewerStyle <= id) &&
                 (id <= SystemResourceKeyID.GridViewItemContainerStyle)))
            {
                return Enum.GetName(typeof(SystemResourceKeyID), id) + "Key";
            }
            else if (SystemResourceKeyID.MenuItemSeparatorStyle == id ||
                     SystemResourceKeyID.StatusBarSeparatorStyle == id)
            {
                return "SeparatorStyleKey";
            }
            else if ((SystemResourceKeyID.ToolBarButtonStyle <= id) &&
                     (id <= SystemResourceKeyID.ToolBarMenuStyle))
            {
                string propName = Enum.GetName(typeof(SystemResourceKeyID), id) + "Key";
                return propName.Remove(0, 7); // Remove the "ToolBar" prefix
            }

            return String.Empty;
        }

        internal static string GetSystemPropertyName(SystemResourceKeyID id)
        {
            if ((SystemResourceKeyID.InternalSystemColorsStart < id) &&
                (id < SystemResourceKeyID.InternalSystemColorsExtendedEnd))
            {
                return Enum.GetName(typeof(SystemResourceKeyID), id);
            }

            return String.Empty;
        }


    }

}

