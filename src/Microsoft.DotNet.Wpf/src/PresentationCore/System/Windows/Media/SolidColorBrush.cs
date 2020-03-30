// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of SolidColorBrush.
//              The SolidColorBrush is the simplest of the Brushes. consisting
//              as it does of just a color.
//
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media 
{
    /// <summary>
    /// SolidColorBrush
    /// The SolidColorBrush is the simplest of the Brushes.  It can be used to 
    /// fill an area with a solid color, which can be animate.
    /// </summary>
    public sealed partial class SolidColorBrush : Brush
    {
        #region Constructors
        
        /// <summary>
        /// Default constructor for SolidColorBrush.
        /// </summary>
        public SolidColorBrush()
        {
        }

        /// <summary>
        /// SolidColorBrush - The constructor accepts the color of the brush
        /// </summary>
        /// <param name="color"> The color value. </param>
        public SolidColorBrush(Color color)
        {
            Color = color;
        }

        #endregion Constructors

        #region Serialization

        // This enum is used to identify brush types for deserialization in the 
        // ConvertCustomBinaryToObject method.  If we support more types of brushes,
        // then we may have to expose this publically and add more enum values.
        internal enum SerializationBrushType : byte
        {
            Unknown = 0,
            KnownSolidColor = 1,
            OtherColor = 2,
        }
        
        ///<summary>
        /// Serialize this object using the passed writer in compact BAML binary format.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method.  
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if "writer" is null.
        /// </exception>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static bool SerializeOn(BinaryWriter writer, string stringValue)
        {
            // ********* VERY IMPORTANT NOTE *****************
            // If this method is changed, then XamlBrushSerilaizer.SerializeOn() needs
            // to be correspondingly changed as well. That code is linked into PBT.dll
            // and duplicates the code below to avoid pulling in SCB & base classes as well.
            // ********* VERY IMPORTANT NOTE *****************

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

        ///<summary>
        /// Deserialize this object using the passed reader.  Throw an exception if
        /// the format is not a solid color brush.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method.  
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if "reader" is null.
        /// </exception>
        public static object DeserializeFrom(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            return DeserializeFrom(reader, null);
        }

        internal static object DeserializeFrom(BinaryReader reader, ITypeDescriptorContext context)
        {
            SerializationBrushType brushType = (SerializationBrushType)reader.ReadByte();

            if (brushType == SerializationBrushType.KnownSolidColor)
            {
                uint knownColorUint = reader.ReadUInt32();
                SolidColorBrush scp = KnownColors.SolidColorBrushFromUint(knownColorUint);
#if !PBTCOMPILER
                lock (s_knownSolidColorBrushStringCache)
                {
                    if (!s_knownSolidColorBrushStringCache.ContainsKey(scp))
                    {
                        string strColor = scp.Color.ConvertToString(null, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS);
                        s_knownSolidColorBrushStringCache[scp] = strColor;
                    }
                }
#endif 
                return scp;
            }
            else if (brushType == SerializationBrushType.OtherColor)
            {
                string colorValue = reader.ReadString();
                BrushConverter converter = new BrushConverter();
                return converter.ConvertFromInvariantString(context, colorValue);
            }
            else
            {
                throw new Exception(SR.Get(SRID.BrushUnknownBamlType));
            }
        }

        #endregion Serialization

        #region ToString
        
        /// <summary>
        /// CanSerializeToString - an internal helper method which determines whether this object
        /// can fully serialize to a string with no data loss.
        /// </summary>
        /// <returns>
        /// bool - true if full fidelity serialization is possible, false if not.
        /// </returns>
        internal override bool CanSerializeToString()
        {
            if (HasAnimatedProperties
                || HasAnyExpression()
                || !Transform.IsIdentity
                || !DoubleUtil.AreClose(Opacity, Brush.c_Opacity))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            string strBrush = Color.ConvertToString(format, provider);

#if !PBTCOMPILER 
            // We maintain a cache of strings representing well-known SolidColorBrush objects corresponding to 
            // each of the KnownColors. This cache is primarily intended to be passed to the localizer which 
            // maintains SolidColorBrush values as a string. When the localizer passes
            // this string back to SolidColorBrush.SerializeOn(), we'd know which ones to 
            // serialize as SerializationBrushType.KnownSolidColor vs. which ones to write as 
            // SerializationBrushType.OtherColor. 
            // 
            // The BamlReader.GetPropertyCustomRecordId() calls ConvertToString() with format=null && 
            // provider = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS, so we use that
            // to decide which string objects to cache.  
            if ((format == null) && (provider == System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS) && KnownColors.IsKnownSolidColorBrush(this))
            {
                lock (s_knownSolidColorBrushStringCache)
                {
                    string strCachedBrush = null; 
                    if (s_knownSolidColorBrushStringCache.TryGetValue(this, out strCachedBrush))
                    {
                        strBrush = strCachedBrush;
                    }
                    else
                    {
                        s_knownSolidColorBrushStringCache[this] = strBrush;
                    }
                }
            }
#endif 
            return strBrush;
        }

        #endregion

#if !PBTCOMPILER
        // A simple Two-way Dictionary implementation - only a few useful methods are
        // defined. This class is used to maintain a cache of well-known SolidColorBrush 
        // string values, and allow for fast lookup of those string values by reference. 
        internal class TwoWayDictionary<TKey,TValue>
        {
            public TwoWayDictionary(): this(null, null)
            {
                // Forward to TwoWayDictionary(IEqualityComparer<TKey>, IEqualityComparer<TValue>)
            }

            public TwoWayDictionary(IEqualityComparer<TKey> keyComparer): this (keyComparer, null)
            {
                // Forward to TwoWayDictionary(IEqualityComparer<TKey>, IEqualityComparer<TValue>)
            }

            public TwoWayDictionary(IEqualityComparer<TValue> valueComparer) : this (null, valueComparer)
            {
                // Forward to TwoWayDictionary(IEqualityComparer<TKey>, IEqualityComparer<TValue>)
            }

            public TwoWayDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                fwdDictionary = new Dictionary<TKey, TValue>(keyComparer);
                revDictionary = new Dictionary<TValue, List<TKey>>(valueComparer); 
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                return fwdDictionary.TryGetValue(key, out value);
            }

            public bool TryGetKeys(TValue value, out List<TKey> keys)
            {
                return revDictionary.TryGetValue(value, out keys); 
            }

            public bool ContainsValue(TValue value)
            {
                return revDictionary.ContainsKey(value); 
            }

            public bool ContainsKey(TKey key)
            {
                return fwdDictionary.ContainsKey(key); 
            }

            public TValue this[TKey key]
            {
                get
                {
                    return fwdDictionary[key];
                }

                set
                {
                    fwdDictionary[key] = value;

                    List<TKey> keys; 
                    if (!revDictionary.TryGetValue(value, out keys))
                    {
                        keys = new List<TKey>();
                        revDictionary[value] = keys; 
                    }
                    keys.Add(key); 
                }
            }

            public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
            {
                return fwdDictionary.GetEnumerator();
            }

            // We define the reverse dictionary this way using a List because
            // the forward dictionary would have unique keys but potentially
            // repeating values for different keys. 
            private Dictionary<TKey, TValue> fwdDictionary;
            private Dictionary<TValue, List<TKey>> revDictionary; 
        }

        private static TwoWayDictionary<SolidColorBrush, string> s_knownSolidColorBrushStringCache = new TwoWayDictionary<SolidColorBrush, string>(keyComparer: ReferenceEqualityComparer.Instance);
#endif 
    }
}
