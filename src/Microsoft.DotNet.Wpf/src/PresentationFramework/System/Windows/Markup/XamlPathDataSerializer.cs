// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
//   XamlSerializer used to persist path data into Baml. 
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
    ///     XamlPathDataSerializer is used to persist collections of integer indices in Baml
    /// </summary>


    internal class XamlPathDataSerializer : XamlSerializer
    {
#region Construction

        /// <summary>
        ///     Constructor for XamlPathDataSerializer
        /// </summary>
        public XamlPathDataSerializer()
        {
        }

        
#endregion Construction

        /// <summary>
        ///   Convert a string into a compact binary representation and write it out
        ///   to the passed BinaryWriter.
        /// </summary>
        public override bool ConvertStringToCustomBinary (
            BinaryWriter   writer,           // Writer into the baml stream
            string         stringValue)      // String to convert
        {
            Parsers.PathMinilanguageToBinary( writer, stringValue ) ;
            
            return true;             
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
            return Parsers.DeserializeStreamGeometry( reader );             
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
            return Parsers.DeserializeStreamGeometry( reader );             
        }          
#endif 

    }
}


