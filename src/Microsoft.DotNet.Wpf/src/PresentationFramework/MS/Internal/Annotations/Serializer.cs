// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     Serializer performs the boot-strapping to call the public implementations
//     of IXmlSerializable for the Annotation object model.  This would normally
//     be done by XmlSerializer but its slow and causes an assembly to be generated
//     at runtime.  API-wise third-parties can still use XmlSerializer but we
//     choose not to for our purposes.
//

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Annotations.Storage;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MS.Internal;

namespace MS.Internal.Annotations
{
    /// <summary>
    ///     Serializer class for Annotation object model.  All entities
    ///     in the object model implement IXmlSerializable (or are
    ///     contained and serialized by an entity that does).  This class
    ///     does the simple boot-strapping for serializing/deserializing
    ///     of the object model.  This lets us get by without paying the
    ///     cost of XmlSerializer (which generates a run-time assembly).
    /// </summary>
    internal class Serializer
    {
        /// <summary>
        ///     Creates an instance of the serializer for the specified type.
        ///     We use the type to get the default constructor and the type's
        ///     element name and namespace.  This constructor expects the 
        ///     type to be attributed with XmlRootAttribute (as all serializable
        ///     classes in the object model are).
        /// </summary>
        /// <param name="type">the type to be serialized by this instance</param>
        public Serializer(Type type)
        {
            Invariant.Assert(type != null);

            // Find the XmlRootAttribute for the type
            object[] attributes = type.GetCustomAttributes(false);
            foreach (object obj in attributes)
            {
                _attribute = obj as XmlRootAttribute;
                if (_attribute != null)
                    break;
            }

            Invariant.Assert(_attribute != null, "Internal Serializer used for a type with no XmlRootAttribute.");

            // Get the default constructor for the type
            _ctor = type.GetConstructor(Array.Empty<Type>());
        }

        /// <summary>
        ///     Serializes the object to the specified XmlWriter.
        /// </summary>
        /// <param name="writer">writer to serialize to</param>
        /// <param name="obj">object to serialize</param>
        public void Serialize(XmlWriter writer, object obj)
        {
            Invariant.Assert(writer != null && obj != null);

            IXmlSerializable serializable = obj as IXmlSerializable;
            Invariant.Assert(serializable != null, "Internal Serializer used for a type that isn't IXmlSerializable.");

            writer.WriteStartElement(_attribute.ElementName, _attribute.Namespace);
            serializable.WriteXml(writer);
            writer.WriteEndElement();
        }

        /// <summary>
        ///     Deserializes the next object from the reader.  The 
        ///     reader is expected to be positioned on a node that
        ///     can be deserialized into the type this serializer
        ///     was instantiated for.
        /// </summary>
        /// <param name="reader">reader to deserialize from</param>
        /// <returns>an instance of the type this serializer was instanted
        /// for with values retrieved from the reader</returns>
        public object Deserialize(XmlReader reader)
        {
            Invariant.Assert(reader != null);

            IXmlSerializable serializable = (IXmlSerializable)_ctor.Invoke(Array.Empty<object>());

            // If this is a brand-new stream we need to jump into it
            if (reader.ReadState == ReadState.Initial)
            {
                reader.Read();
            }

            serializable.ReadXml(reader);

            return serializable;
        }

        // XmlRootAttribute - specifies the ElementName and Namespace for 
        // the node to read/write
        private XmlRootAttribute _attribute;
        // Constructor used to create instances when deserializing
        private ConstructorInfo _ctor;
    }
}
