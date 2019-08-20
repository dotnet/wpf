// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Utilities for converting types to custom Binary format
//

using System;
using System.IO; 
using System.Collections; 
using System.Globalization; // CultureInfo 

#if PRESENTATION_CORE
using MS.Internal.PresentationCore;     // FriendAccessAllowed
#elif PRESENTATIONFRAMEWORK
using MS.Internal.PresentationFramework; 
#endif 

#if PBTCOMPILER
using System.Collections.Generic; 
using TypeConverterHelper = MS.Internal.Markup;

namespace MS.Internal.Markup
#else
using System.Windows; 
using System.Windows.Media ; 
using System.Windows.Media.Media3D; 
using TypeConverterHelper = System.Windows.Markup;

namespace MS.Internal.Media
#endif
{
#if PBTCOMPILER     

    
    //
    // Internal class used during serialization of Point3D, or Vectors. 
    //
    //  We define this struct so that we can create a collection of types during serialization. 
    //  If we used defined avalon types, like Point & Vector, we'd have to bring these into PBT
    //  with a compiler change everytime these types are changed. 
    // 
    //  The type is called "ThreeDoubles" to make clear what it is and what it's for. 
    //
    //  If either Vector3D or Point3D changes - we will need to change this code. 
    // 
    internal class ThreeDoublesMarkup
    {

        internal ThreeDoublesMarkup( double X, double Y, double Z) 
        {
            _x = X; 
            _y = Y; 
            _z = Z; 
        }

        internal double X
        {
            get
            {
                return _x;
            }
        }

        internal double Y
        {
            get
            {
                return _y;
            }
        }

        internal double Z 
        {
            get
            {
                return _z;
            }
        }
        
        double _x ;
        double _y ; 
        double _z ; 
            
    }

    internal struct Point
    {
        internal Point(double x, double y)
        {
            _x = x;
            _y = y;
        }

        internal double X
        {
            set
            {
                _x = value; 
            }
            get
            {
                return _x;
            }
        }

        internal double Y
        {
            set
            {
                _y = value; 
            }
            get
            {
                return _y;
            }
        }

        private double _x;
        private double _y;                 
    }

    internal struct Size
    {
        internal Size(double width, double height)
        {
            _width = width;
            _height = height;
        }

        internal double Width
        {
            set
            {
                _width = value; 
            }
            get
            {
                return _width; 
            }
        }
        
        internal double Height            
        {
            set
            {
               _height = value; 
            }
            get
            {
                return _height; 
            }
        }

        private double _width;
        private double _height;                 
    }
 

#endif 

    internal static class XamlSerializationHelper 
    {
        // =====================================================
        // 
        // All PBT specific types and methods go here. 
        // 
        // ======================================================

        

        internal enum SerializationFloatType : byte
        {
            Unknown = 0,
            Zero = 1,
            One = 2, 
            MinusOne = 3, 
            ScaledInteger = 4,
            Double = 5, 
            Other
        }


        ///<summary>
        /// Serialize this object using the passed writer in compact BAML binary format.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method.  
        /// </remarks>
#if !PBTCOMPILER        
        [FriendAccessAllowed] // Built into Core, also used by Framework.
#endif        
        internal static bool SerializePoint3D(BinaryWriter writer, string stringValues)
        {
#if PBTCOMPILER            
            List<ThreeDoublesMarkup> point3Ds = ParseThreeDoublesCollection(stringValues, TypeConverterHelper.InvariantEnglishUS); 
            ThreeDoublesMarkup curPoint; 
#else
            Point3DCollection point3Ds = Point3DCollection.Parse( stringValues ) ; 
            Point3D curPoint ; 
#endif

            // Write out the size.
            writer.Write( ( uint ) point3Ds.Count  ) ; 
            
            // Write out the doubles. 
            for ( int i = 0; i < point3Ds.Count  ; i ++ ) 
            {
                curPoint = point3Ds[i] ;                 

                WriteDouble( writer, curPoint.X); 
                WriteDouble( writer, curPoint.Y); 
                WriteDouble( writer, curPoint.Z); 
            }

            return true ; 
        }

        ///<summary>
        /// Serialize this object using the passed writer in compact BAML binary format.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method.  
        /// </remarks>
#if !PBTCOMPILER        
        [FriendAccessAllowed] // Built into Core, also used by Framework.
#endif        
        internal static bool SerializeVector3D(BinaryWriter writer, string stringValues)
        {
#if PBTCOMPILER            
            List<ThreeDoublesMarkup> points = ParseThreeDoublesCollection(stringValues, TypeConverterHelper.InvariantEnglishUS); 
            ThreeDoublesMarkup curPoint; 
#else
            Vector3DCollection points = Vector3DCollection.Parse( stringValues ) ;             
            Vector3D curPoint ; 
#endif

            // Write out the size.         
            writer.Write( ( uint ) points.Count  ) ; 
            
            // Write out the doubles. 
            for ( int i = 0; i < points.Count ; i ++ ) 
            {
                curPoint = points[ i ] ;                 

                WriteDouble( writer, curPoint.X); 
                WriteDouble( writer, curPoint.Y); 
                WriteDouble( writer, curPoint.Z); 
            }

            return true ; 
        }
        
        ///<summary>
        /// Serialize this object using the passed writer in compact BAML binary format.
        ///</summary>
        /// <remarks>
        /// This is called ONLY from the Parser and is not a general public method.  
        /// </remarks>
#if !PBTCOMPILER        
        [FriendAccessAllowed] // Built into Core, also used by Framework.
#endif        
        internal static bool SerializePoint(BinaryWriter writer, string stringValue)
        {
#if PBTCOMPILER            
            List<Point> points = ParsePointCollection(stringValue, TypeConverterHelper.InvariantEnglishUS); 
            Point curPoint; 
#else
            PointCollection points = PointCollection.Parse( stringValue ) ; 
            Point curPoint ; 
#endif

            // Write out the size.          
            writer.Write( ( uint ) points.Count  ) ; 
            
            // Write out the doubles. 
            for ( int i = 0; i < points.Count ; i ++ ) 
            {
                curPoint = points[ i ] ;                 

                WriteDouble( writer, curPoint.X); 
                WriteDouble( writer, curPoint.Y); 
            }

            return true ; 
        }

        private const double scaleFactor = 1000000 ; // approx ==  2^20 
        private const double inverseScaleFactor = 0.000001 ; // approx = 1 / 2^20 
        
        //
        // Write a double into our internal binary format
        //
        //
        //  The format is : 
        //          <Byte indicating enum type> ( <4 bytes for scaledinteger> | < 8 bytes for double> )
        //
        internal static void WriteDouble( BinaryWriter writer, Double value  ) 
        {
            if ( value == 0.0 ) 
            {
                writer.Write( (byte) SerializationFloatType.Zero ) ; 
            }
            else if ( value == 1.0 ) 
            {
                writer.Write( (byte) SerializationFloatType.One ) ; 
            }
            else if ( value == -1.0 ) 
            {
                writer.Write( (byte) SerializationFloatType.MinusOne ) ; 
            }            
            else
            {
                int intValue = 0 ; 
                
                if (  CanConvertToInteger( value, ref intValue ) ) 
                {
                    writer.Write( (byte) SerializationFloatType.ScaledInteger ) ; 
                    writer.Write( intValue ) ; 
                }
                else
                {
                    writer.Write( (byte) SerializationFloatType.Double ) ; 
                    writer.Write( value ) ; 
                }
            }                
        }

#if !PBTCOMPILER
        //
        // Read a double from our internal binary format. 
        //      We assume that the binary reader is at the start of a byte. 
        //          
        internal static double ReadDouble( BinaryReader reader ) 
        {
            SerializationFloatType type = ( SerializationFloatType ) reader.ReadByte(); 

            switch( type ) 
            {
                case SerializationFloatType.Zero :
                    return 0.0 ; 

                case SerializationFloatType.One :
                    return 1.0 ; 

                case SerializationFloatType.MinusOne :
                    return -1.0 ; 

                case SerializationFloatType.ScaledInteger : 
                    return ReadScaledInteger( reader ); 

                case SerializationFloatType.Double :
                    return reader.ReadDouble(); 

                default: 
                    throw new ArgumentException(SR.Get(SRID.FloatUnknownBamlType));                 
            }
}

        internal static double ReadScaledInteger(BinaryReader reader )
        {
            double value = (double) reader.ReadInt32(); 
            value = value * inverseScaleFactor ; 

            return value ; 
        }
        
#endif                     

#if PBTCOMPILER

        
        /// <summary>
        /// Parse - returns an instance converted from the provided string.
        /// <param name="source"> string with Point3DCollection data </param>
        /// <param name="formatProvider">IFormatprovider for processing string</param>
        /// </summary>
        private static List<ThreeDoublesMarkup> ParseThreeDoublesCollection(string source, IFormatProvider formatProvider)
        {
            TokenizerHelper th = new TokenizerHelper(source, formatProvider);

            
            List<ThreeDoublesMarkup> resource = new List<ThreeDoublesMarkup>( source.Length/ 8 ) ;  // SWAG the length of the collection. 

            ThreeDoublesMarkup value;

            while (th.NextToken())
            {
                value = new ThreeDoublesMarkup(
                    Convert.ToDouble(th.GetCurrentToken(), formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider));

                resource.Add(value);
            }

            return resource;
        }        
        
        /// <summary>
        /// Parse - returns an instance converted from the provided string.
        /// <param name="source"> string with Point3DCollection data </param>
        /// <param name="formatProvider">IFormatprovider for processing string</param>
        /// </summary>
        private static List<Point> ParsePointCollection(string source, IFormatProvider formatProvider)
        {
            TokenizerHelper th = new TokenizerHelper(source, formatProvider);
            
            List<Point> resource = new List<Point>(source.Length/ 8 ); // SWAG the length of the collection. 

            Point value;

            while (th.NextToken())
            {
                value = new Point(
                    Convert.ToDouble(th.GetCurrentToken(), formatProvider),
                    Convert.ToDouble(th.NextTokenRequired(), formatProvider) );

                resource.Add(value);
            }

            return resource;
        }     
#endif

        //
        // Can we convert this double to a "scaled integer"
        //
        // We multiply by approx 2^20 - see if the result is either 
        //              - greater than maxInt 
        //              - if there is a non-numeric integer remaining 
        //
        //      as a result this routine will convert doubles with six-digits precision  between +/- 2048 
        // 
        internal static bool CanConvertToInteger( Double doubleValue , ref int intValue ) 
        {
            double scaledValue ; 
            double scaledInteger ; 

            scaledValue = doubleValue * scaleFactor ; 
            scaledInteger = Math.Floor( scaledValue ) ; 
        
            if ( !( scaledInteger <= Int32.MaxValue )  // equivalent to scaledInteger > MaxValue, but take care of NaN. 
                    || 
                  !( scaledInteger >= Int32.MinValue ) ) // equivalent to scaledInteger < Minvalue but take care of NaN.
            {
                return false ; 
            }
            else if ( ( scaledValue - scaledInteger ) > Double.Epsilon )  
            {
                return false ;
            }
            else
            {
                intValue = (int) scaledValue ; 
                
                return true ; 
            }                     
        }
}
}
