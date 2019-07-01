// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.Test
{
    /// <summary>
    /// Serializer API for managing serialization and deserialzaion of objects to XML
    /// </summary>
    /// <remarks>
    /// individual implementations are created by implementing IObjectSerializer
    /// </remarks>
    public static class ObjectSerializer
    {
        #region Private Data

        private static DefaultObjectSerializer defaultSerializer = new DefaultObjectSerializer();
        private static DefaultListSerializer defaultListSerializer = new DefaultListSerializer();
        private static Dictionary<string, Type> elementTypeMapping = new Dictionary<string, Type>();

        #endregion

        #region Constructor

        // TODO: understand what RegisterTypeMapping is doing and why this is necessary. For now we silence the warning
        //       since it is not a field initialization.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static ObjectSerializer()
        {
            RegisterTypeMapping("string", typeof(string));
        }

        #endregion


        #region Public Members

        /// <summary>
        /// Serialize an object to XML
        /// </summary>
        /// <param name="value">object to serialize</param>
        /// <returns>a string containing the serialized object in xml</returns>
        public static string Serialize(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.Formatting = Formatting.Indented;
                Serialize(xmlWriter, value, true);
            }
            return stringWriter.ToString();
        }


        /// <summary>
        /// Serialize an object to the given Xml writer
        /// </summary>
        /// <param name="writer">xml writer to serialize to</param>
        /// <param name="value">object to serialize</param>
        /// <returns>a string containing the serialized object in xml</returns>
        public static void Serialize(XmlTextWriter writer, object value)
        {
            Serialize(writer, value, true);
        }


        /// <summary>
        /// Serialize an object to the given Xml writer
        /// </summary>
        /// <param name="writer">xml writer to serialize to</param>
        /// <param name="value">object to serialize</param>
        /// <param name="writeElement">true to write a new element for the object, otherwise, false</param>
        /// <returns>a string containing the serialized object in xml</returns>
        public static void Serialize(XmlTextWriter writer, object value, bool writeElement)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            IObjectSerializer serializer = GetSerializer(value.GetType());
            serializer.Serialize(writer, value, writeElement);
        }

        /// <summary>
        /// Deserialize the element at the current position in the reader using the given deserialization context
        /// </summary>
        /// <param name="reader">XmlTextReader to deserialize the object from</param>
        /// <param name="context">Context to use during deserialization</param>
        /// <returns>the deserialized object</returns>
        public static object Deserialize(XmlTextReader reader, object context)
        {

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (reader.NodeType != XmlNodeType.Element)
            {
                throw new InvalidOperationException("The Xml reader must be at an Element node to Deserialize and object");
            }
            if (!elementTypeMapping.ContainsKey(reader.Name))
            {
                throw new InvalidOperationException("There is no registered Category mapping for the Element " + reader.Name);
            }

            //look up the type to be deserialize based on the type element mapping table
            Type type = elementTypeMapping[reader.Name];

            //Deserailize the object to the Mapped type
            return Deserialize(reader, type, context);
        }

        /// <summary>
        /// Deserialize the node at the current position to the specified type in the reader using the given deserialization context
        /// </summary>
        /// <param name="reader">XmlTextReader to deserialize the object from</param>
        /// <param name="type">Category of the object to be deserialized</param>
        /// <param name="context">Context to use during deserialization</param>
        /// <returns>the deserialized object</returns>
        public static object Deserialize(XmlTextReader reader, Type type, object context)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            PrepareReader(reader);

            return GetSerializer(type).Deserialize(reader, type, context);
        }

        /// <summary>
        /// Moves the reader to the first element in the document and disables whitespace handling
        /// </summary>
        /// <param name="reader">the xml reader</param>
        public static void PrepareReader(XmlTextReader reader)
        {
            //Ensure we get to ignore whitespaces (this is to make it easier to implement Serializers)            
            reader.WhitespaceHandling = WhitespaceHandling.None;
            if (reader.NodeType == XmlNodeType.None)
            {
                do
                {
                    reader.Read();
                } while (reader.NodeType != XmlNodeType.Element && !reader.EOF);
            }
        }

        /// <summary>
        /// Register a Category that will be deserialzed for the given element Name
        /// </summary>
        /// <param name="elementName">name of the element to use the deserializer for</param>
        /// <param name="type">Category of the object that is deserialized</param>
        public static void RegisterTypeMapping(string elementName, Type type)
        {
            if (string.IsNullOrEmpty(elementName))
            {
                throw new ArgumentNullException("elementName");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            elementTypeMapping[elementName] = type;
        }

        #endregion


        #region Private members

        //Gets the serializer for the specified type
        private static IObjectSerializer GetSerializer(Type targetType)
        {
            //Get the serializer specified by the ObjectSerializerAttribute
            ObjectSerializerAttribute att = Attribute.GetCustomAttribute(targetType, typeof(ObjectSerializerAttribute), true) as ObjectSerializerAttribute;
            if (att == null)
            {
                //TODO: we may need to provide a more extensible way for default serializers other then attributes
                if (typeof(IList).IsAssignableFrom(targetType))
                {
                    return defaultListSerializer;
                }
                else
                {
                    return defaultSerializer;
                }
            }
            return att.Serializer;
        }

        #endregion

    }


    //Contract for ObjectSerializers
    internal interface IObjectSerializer
    {
        //write the object to the stream
        void Serialize(XmlTextWriter writer, object obj, bool writeElement);
        //Deserialize the specified Category from the current position in the reader using the given context
        //The reader should be returned in a position where the next read will move the reader to the next object in the reader
        object Deserialize(XmlTextReader reader, Type type, object context);
    }

    //Attribute for specifying a serializer to use for a Category
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class ObjectSerializerAttribute : Attribute
    {
        private IObjectSerializer serializer;

        public ObjectSerializerAttribute(Type serializerType)
        {
            if (serializerType == null)
            {
                throw new ArgumentNullException("serializerType");
            }

            //TODO: this may need to be optimized to not create a serializer per attribute
            serializer = (IObjectSerializer)Activator.CreateInstance(serializerType);
        }

        //gets the serializer for the attribute
        public IObjectSerializer Serializer
        {
            get { return serializer; }
        }
    }
}


