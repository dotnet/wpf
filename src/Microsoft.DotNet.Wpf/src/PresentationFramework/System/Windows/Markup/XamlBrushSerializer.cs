// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   XamlSerializer used to persist Brush objects in Baml
//

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using MS.Utility;
using MS.Internal;

#if PBTCOMPILER
using System.Reflection;

namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Media;


namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     XamlBrushSerializer is used to persist a Brush in Baml files
    /// </summary>
    /// <remarks>
    ///  The brush serializer currently only handles solid color brushes.  Other types of
    ///  are not persisted in a custom binary format.
    /// </remarks>
    internal class XamlBrushSerializer : XamlSerializer
    {
#region Construction

        /// <summary>
        ///     Constructor for XamlBrushSerializer
        /// </summary>
        /// <remarks>
        ///     This constructor will be used under 
        ///     the following two scenarios
        ///     1. Convert a string to a custom binary representation stored in BAML
        ///     2. Convert a custom binary representation back into a Brush
        /// </remarks>
        public XamlBrushSerializer()
        {
        }
        
#endregion Construction

#region Conversions

        /// <summary>
        ///   Convert a string into a compact binary representation and write it out
        ///   to the passed BinaryWriter.
        /// </summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// This currently only works for SolidColorBrushes that are identified
        /// by a known color name (eg - "Green" )
        /// </remarks>
        public override bool ConvertStringToCustomBinary (
            BinaryWriter   writer,           // Writer into the baml stream
            string         stringValue)      // String to convert
        {
#if !PBTCOMPILER
            return SolidColorBrush.SerializeOn(writer, stringValue.Trim());
#else
            return SerializeOn(writer, stringValue.Trim());
#endif
        }

#if !PBTCOMPILER
        
        /// <summary>
        ///   Convert a compact binary representation of a Brush into and instance
        ///   of Brush.  The reader must be left pointing immediately after the object 
        ///   data in the underlying stream.
        /// </summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// </remarks>
        public override object ConvertCustomBinaryToObject(
            BinaryReader reader)
        {
            // ********* VERY IMPORTANT NOTE *****************
            // If this method is changed, then BamlPropertyCustomRecord.GetCustomValue() needs
            // to be correspondingly changed as well
            // ********* VERY IMPORTANT NOTE *****************
            return SolidColorBrush.DeserializeFrom(reader);
        }
#else
        private static bool SerializeOn(BinaryWriter writer, string stringValue)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            KnownColor knownColor = KnownColors.ColorStringToKnownColor(stringValue);
#if !PBTCOMPILER
            // ***************** NOTE *****************
            // This section under #if !PBTCOMPILER is not needed in XamlBrushSerializer.cs
            // because XamlBrushSerializer.SerializeOn() is only compiled when PBTCOMPILER is set. 
            // If this code were tried to be compiled in XamlBrushSerializer.cs, it wouldn't compile
            // becuase of missing definition of s_knownSolidColorBrushStringCache. 
            // This code is added in XamlBrushSerializer.cs nevertheless for maintaining consistency in the codebase
            // between XamlBrushSerializer.SerializeOn() and SolidColorBrush.SerializeOn().
            // ***************** NOTE *****************
            lock (s_knownSolidColorBrushStringCache)
            {
                SolidColorBrush scb; 
                if (s_knownSolidColorBrushStringCache.ContainsValue(stringValue))
                {
                    knownColor = KnownColors.ArgbStringToKnownColor(stringValue);
                }
            }
#endif 
            if (knownColor != KnownColor.UnknownColor)
            {
                // Serialize values of the type "Red", "Blue" and other names
                writer.Write((byte)SerializationBrushType.KnownSolidColor);
                writer.Write((uint)knownColor);
                return true;
            }
            else
            {
                // Serialize values of the type "#F00", "#0000FF" and other hex color values.
                // We don't have a good way to check if this is valid without running the 
                // converter at this point, so just store the string if it has at least a
                // minimum length of 4.
                stringValue = stringValue.Trim();
                if (stringValue.Length > 3)
                {
                    writer.Write((byte)SerializationBrushType.OtherColor);
                    writer.Write(stringValue);
                    return true;
                }
            }

            return false;
        }

        // This enum is used to identify brush types for deserialization in the 
        // ConvertCustomBinaryToObject method.
        internal enum SerializationBrushType : byte
        {
            Unknown = 0,
            KnownSolidColor = 1,
            OtherColor = 2,
        }
#endif

#endregion Conversions

    }
}
