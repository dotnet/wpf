// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   XamlSerializer is used to persist an object instance to xaml markup
//

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

using MS.Utility;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    ///     XamlSerializer is used to persist an 
    ///     object instance to xaml markup.
    /// </summary>
    internal class XamlSerializer
    {
        #region Construction

        /// <summary>
        ///     Constructor for XamlSerializer
        /// </summary>
        /// <remarks>
        ///     This constructor will be used under 
        ///     the following three scenarios
        ///     1. Convert .. XamlToBaml
        ///     2. Convert .. XamlToObject
        ///     3. Convert .. BamlToObject
        /// </remarks>
        public XamlSerializer()
        {
        }
        
        #endregion Construction

        #region OtherConversions
        
        /// <summary>
        ///   Convert from Xaml read by a token reader into baml being written
        ///   out by a record writer.  The context gives mapping information.
        /// </summary>
#if !PBTCOMPILER
#endif        
        internal virtual void ConvertXamlToBaml (
            XamlReaderHelper          tokenReader,
            ParserContext       context,
            XamlNode            xamlNode,
            BamlRecordWriter    bamlWriter)
        {
            throw new InvalidOperationException(SR.Get(SRID.InvalidDeSerialize));
        }

#if !PBTCOMPILER

        /// <summary>
        ///   Convert from Xaml read by a token reader into a live
        ///   object tree.  The context gives mapping information.
        /// </summary>
        internal virtual void ConvertXamlToObject (
            XamlReaderHelper             tokenReader,
            ReadWriteStreamManager streamManager,
            ParserContext          context,
            XamlNode               xamlNode,
            BamlRecordReader       reader)
        {
            throw new InvalidOperationException(SR.Get(SRID.InvalidDeSerialize));
        }

        /// <summary>
        ///   Convert from Baml read by a baml reader into an object tree.
        ///   The context gives mapping information.  Return the number of
        ///   baml records processed.
        /// </summary>
        internal virtual void ConvertBamlToObject (
            BamlRecordReader    reader,       // Current reader that is processing records
            BamlRecord          bamlRecord,   // Record read in that triggered serializer
            ParserContext       context)      // Context
        {
            throw new InvalidOperationException(SR.Get(SRID.InvalidDeSerialize));
        }

#endif

        /// <summary>
        ///   Convert a string into a compact binary representation and write it out
        ///   to the passed BinaryWriter.
        /// </summary>
        public virtual bool ConvertStringToCustomBinary (
            BinaryWriter   writer,           // Writer into the baml stream
            string         stringValue)      // String to convert
        {
            throw new InvalidOperationException(SR.Get(SRID.InvalidCustomSerialize));
        }
        
        /// <summary>
        ///   Convert a compact binary representation of a certain object into and instance
        ///   of that object.  The reader must be left pointing immediately after the object 
        ///   data in the underlying stream.
        /// </summary>
        public virtual object ConvertCustomBinaryToObject(
            BinaryReader reader)
        {
            throw new InvalidOperationException(SR.Get(SRID.InvalidCustomSerialize));
        }            

        /// <summary>
        ///   If the object created by this serializer is stored in a dictionary, this
        ///   method will extract the key used for this dictionary from the passed 
        ///   collection of baml records.  How the key is determined is up to the
        ///   individual serializer.  By default, there is no key retrieved.
        /// </summary>
#if !PBTCOMPILER
#endif        
        internal virtual object GetDictionaryKey(
            BamlRecord    bamlRecord, 
            ParserContext parserContext)
        {
            return null;
        }


        #endregion OtherConversions
        #region Data
        internal const string DefNamespacePrefix = "x"; // Used to emit Definitions namespace prefix
        internal const string DefNamespace = "http://schemas.microsoft.com/winfx/2006/xaml"; // Used to emit Definitions namespace
        internal const string ArrayTag = "Array"; // Used to emit the x:Array tag
        internal const string ArrayTagTypeAttribute = "Type"; // Used to emit the x:Type attribute for Array
        #endregion Data

    }
}
