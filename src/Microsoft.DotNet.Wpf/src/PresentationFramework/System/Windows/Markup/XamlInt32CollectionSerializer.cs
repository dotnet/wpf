// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   XamlSerializer used to persist collections of integer indices in Baml
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
using System.Collections.Generic; 
using TypeConverterHelper = MS.Internal.Markup.TypeConverterHelper;

namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D; 
using MS.Internal.Media; 
using TypeConverterHelper = System.Windows.Markup.TypeConverterHelper;

namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     XamlInt32CollectionSerializer is used to persist collections of integer indices in Baml
    /// </summary>


    internal class XamlInt32CollectionSerializer : XamlSerializer
    {
        //
        // Internal only class.         
        //  We actually create a class here - to avoid jitting for use of struct/value type
        // 
#if PBTCOMPILER       
        internal class IntegerMarkup
        {
            internal IntegerMarkup( int value) 
            {
                _value = value; 
            }

            internal int Value
            {
                get
                {
                    return _value;
                }
            }

            int _value;
        }    
#endif

#region Construction

        /// <summary>
        ///     Constructor for XamlInt32CollectionSerializer
        /// </summary>
        public XamlInt32CollectionSerializer()
        {
        }

        
#endregion Construction

#region Conversions

        /// <summary>
        ///   Convert a string into a compact binary representation and write it out
        ///   to the passed BinaryWriter.
        /// </summary>
        public override bool ConvertStringToCustomBinary (
            BinaryWriter   writer,           // Writer into the baml stream
            string         stringValue)      // String to convert
        {
#if PBTCOMPILER            
            List<IntegerMarkup> ints = Parse( stringValue); 
#else
            Int32Collection ints = Int32Collection.Parse( stringValue ); 
#endif
            int cur, last , count,  max = 0 ; 

            count = ints.Count ; 
            
            // loop through the collection testing for 
            // if the numbers are consecutive, and what's the max. 

            bool consecutive = true; 
            bool allPositive = true ; 
            for ( int i = 1; i < count; i++)
            {
#if PBTCOMPILER 
                last = ints[i-1].Value; 
                cur = ints[i].Value; 
#else
                last = ints.Internal_GetItem(i-1); 
                cur = ints.Internal_GetItem(i); 
#endif
    
                if ( consecutive && ( last + 1 != cur )) 
                {
                    consecutive = false; 
                }

                // 
                // If any number is negative - we will just use Integer type. 
                //
                //  We could handle this by encoding the min/max and creating a different number of bit encoding. 
                //  For now - we're seeing enough gains with this change. 
                // 
                if ( cur < 0 ) 
                {
                    allPositive = false ; 
                }
                
                if ( cur > max )
                {
                    max = cur; 
                }                    
            }
            
            if ( consecutive ) 
            {
                writer.Write( (byte) IntegerCollectionType.Consecutive ); 
                writer.Write( count ); // Write the count 

                // Write the first number. 
#if PBTCOMPILER                
                writer.Write( ints[0].Value ); 
#else
                writer.Write( ints.Internal_GetItem(0)); 
#endif
            }
            else
            {
                IntegerCollectionType type; 
                
                if ( allPositive && max <= 255 )
                {
                    type = IntegerCollectionType.Byte; 
                }
                else if ( allPositive && max <= UInt16.MaxValue ) 
                {
                    type = IntegerCollectionType.UShort; 
                }
                else 
                {
                    type = IntegerCollectionType.Integer; 
                }

                writer.Write( (byte) type ); 
                writer.Write( count ); // Write the count 
                
                switch( type ) 
                {
                    case IntegerCollectionType.Byte: 
                    {
                        for( int i = 0; i < count; i++ ) 
                        {
                                writer.Write( (byte) 
#if PBTCOMPILER 
                                                ints[i].Value
#else
                                                ints.Internal_GetItem(i)
#endif 
                                              ) ; 
                        }                            
                    }
                    break; 
                        
                    case IntegerCollectionType.UShort: 
                    {
                        for( int i = 0; i < count; i++ ) 
                        {                        
                            writer.Write( (ushort) 
#if PBTCOMPILER 
                                            ints[i].Value 
#else
                                            ints.Internal_GetItem(i)
#endif 
                                         ); 
                        }    
                    }
                    break; 

                    case IntegerCollectionType.Integer:
                    {
                        for( int i = 0; i < count; i++ ) 
                        {   
                            writer.Write( 
#if PBTCOMPILER
                                            ints[i].Value
#else
                                            ints.Internal_GetItem(i)
#endif                                  
                                        );
                        }
                    }
                    break; 
                }                
            }

            return true;             
        }

#if PBTCOMPILER 
        public static List<IntegerMarkup> Parse(string source)
        {
            IFormatProvider formatProvider = TypeConverterHelper.InvariantEnglishUS;

            TokenizerHelper th = new TokenizerHelper(source, formatProvider);
            List<IntegerMarkup> resource = new List<IntegerMarkup>();

            int value;

            while (th.NextToken())
            {
                value = Convert.ToInt32(th.GetCurrentToken(), formatProvider);

                resource.Add( new IntegerMarkup(value) );

            }

            return resource;
        }
#endif 

#if !PBTCOMPILER
        
        /// <summary>
        ///   Convert a compact binary representation of a collection 
        ///     into a Point3DCollection into and instance
        /// </summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// </remarks>
        public override object ConvertCustomBinaryToObject(
            BinaryReader reader)
        {
            return DeserializeFrom( reader ); 
        }  

        /// <summary>
        ///   Convert a compact binary representation of a collection 
        ///     into a Point3DCollection into and instance
        /// </summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method. 
        /// </remarks>
        public static object StaticConvertCustomBinaryToObject(
            BinaryReader reader)
        {
            return DeserializeFrom( reader ); 
        }          


        private static Int32Collection DeserializeFrom( BinaryReader reader )
        {
            Int32Collection theCollection; 
            IntegerCollectionType type; 

            type = (IntegerCollectionType) reader.ReadByte(); 

            int count = reader.ReadInt32(); 


            if ( count < 0 ) 
            {
                throw new ArgumentException(SR.Get(SRID.IntegerCollectionLengthLessThanZero)); 
            }                
                    
            theCollection = new Int32Collection( count ); 
                                
            if ( type == IntegerCollectionType.Consecutive ) 
            {
                // Get the first integer 
                int first = reader.ReadInt32(); 

                for( int i = 0; i < count; i ++)
                {
                    theCollection.Add( first + i ); 
                }
            }
            else
            {
                switch( type ) 
                {
                    case IntegerCollectionType.Byte : 
                    {
                        for ( int i = 0; i < count; i++ ) 
                        {                        
                            theCollection.Add( (int) reader.ReadByte()); 
                        }
                    }
                    break; 
                        
                    case IntegerCollectionType.UShort : 
                    {
                        for ( int i = 0; i < count; i++ ) 
                        {                    
                            theCollection.Add( (int) reader.ReadUInt16()); 
                        }
                    }
                    break; 
                        
                    case IntegerCollectionType.Integer : 
                    {                    
                        for ( int i = 0; i < count; i++ ) 
                        {                    
                            int value = reader.ReadInt32(); 

                            theCollection.Add( value);
                        }                            
                    }                            
                    break; 
                    
                    default:
                        throw new ArgumentException(SR.Get(SRID.UnknownIndexType)); 
                }
            }

            return theCollection; 
        }
#endif

#endregion Conversions

        internal enum IntegerCollectionType : byte
        {
            Unknown = 0,
            Consecutive = 1,
            Byte = 2, 
            UShort= 3, 
            Integer=  4
        }
    }
}

