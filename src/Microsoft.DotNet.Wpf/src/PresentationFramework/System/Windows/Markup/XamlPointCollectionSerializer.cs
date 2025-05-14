// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//   XamlSerializer used to persist collections of 3D points in Baml
//

using System.IO;

#if PBTCOMPILER
using System.Reflection;

namespace MS.Internal.Markup
#else

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using MS.Internal.Media;

namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     XamlPointCollectionSerializer is used to persist collections of 3D vectors in Baml
    /// </summary>


    internal class XamlPointCollectionSerializer : XamlSerializer
    {
#region Construction

        /// <summary>
        ///     Constructor for XamlPointCollectionSerializer
        /// </summary>
        /// <remarks>
        ///     This constructor will be used under 
        ///     the following two scenarios
        ///     1. Convert a string to a custom binary representation stored in BAML
        ///     2. Convert a custom binary representation back into a Brush
        /// </remarks>
        public XamlPointCollectionSerializer()
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
            return XamlSerializationHelper.SerializePoint( writer, stringValue ) ; 
        }

     

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
            return PointCollection.DeserializeFrom( reader ) ; 
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
            return PointCollection.DeserializeFrom( reader ) ; 
        }          
#endif

#endregion Conversions

    }
}

