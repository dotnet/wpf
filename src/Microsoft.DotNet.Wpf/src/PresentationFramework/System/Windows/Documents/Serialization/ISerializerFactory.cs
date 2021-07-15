// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA
// 
//
// Description: Plug-in document serializers implement this interface
//
//              See spec at <Need to post existing spec>
// 
namespace System.Windows.Documents.Serialization
{
    using System;
    using System.IO;

    /// <summary>
    /// ISerializerFactory is implemented by an assembly containing a plug-in serializer and provides
    /// functionality to instantiate the associated serializer
    /// </summary>
    public interface ISerializerFactory
    {
        /// <summary>
        /// Create a SerializerWriter on the passed in stream
        /// </summary>
        SerializerWriter CreateSerializerWriter(Stream stream);
        /// <summary>
        /// Return the DisplayName of the serializer.
        /// </summary>
        string DisplayName
        {
            get;
        }
        /// <summary>
        /// Return the ManufacturerName of the serializer.
        /// </summary>
        string ManufacturerName
        {
            get;
        }
        /// <summary>
        /// Return the ManufacturerWebsite of the serializer.
        /// </summary>
        Uri ManufacturerWebsite
        {
            get;
        }
        /// <summary>
        /// Return the DefaultFileExtension of the serializer.
        /// </summary>
        string DefaultFileExtension
        {
            get;
        }
    }
}
#endif
