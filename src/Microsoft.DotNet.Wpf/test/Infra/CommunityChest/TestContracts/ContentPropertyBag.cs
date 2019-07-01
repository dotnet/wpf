// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// Represents a property bag that can also hold arbitrary Xml content
    /// </summary>
    [Serializable()]
    [SuppressMessage("Microsoft.Naming", "CA1710")]
    public class ContentPropertyBag : PropertyBag, IXmlSerializable, ICloneable
    {
        #region SerializableXmlDocument Class

        /// <summary>
        /// This class is only used to work around a serialization limitation, where
        /// XmlDocument cannot be serialized directly. Instead, we serialize and deserialize its
        /// content manually while exposing XmlDocument as a property in the parent class
        /// </summary>
        [Serializable()]
        private class SerializableXmlDocument : ISerializable
        {
            #region Private Data

            //NOTE: Changing any field implies you must update Clone, GetHashCode and Equal methods.
            internal XmlDocument document;

            #endregion

            #region Constructors

            public SerializableXmlDocument()
            {
            }            

            protected SerializableXmlDocument(SerializationInfo info, StreamingContext context)
                : this()
            {
                string xml = info.GetString("XmlData");
                if (!string.IsNullOrEmpty(xml))
                    Document.LoadXml(xml);
            }

            #endregion

            #region Public Members

            public XmlDocument Document
            {
                get
                {
                    if (document == null)
                        document = new XmlDocument();
                    return document;
                }
                set { document = value; }
            }

            #endregion

            #region Override Members

            public override bool Equals(object obj)
            {
                SerializableXmlDocument other = obj as SerializableXmlDocument;

                if (other == null)
                    return false;

                StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
                Document.Save(writer);
                StringWriter otherWriter = new StringWriter(CultureInfo.InvariantCulture);
                other.Document.Save(otherWriter);

                return String.Equals(writer.ToString(), otherWriter.ToString());
            }

            public override int GetHashCode()
            {
                return Document.Name.GetHashCode() ^ Document.ChildNodes.Count;
            }

            /// <summary/>
            [Browsable(false)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static bool operator ==(SerializableXmlDocument x, SerializableXmlDocument y)
            {
                if (object.Equals(x, null))
                    return object.Equals(y, null);
                else
                    return x.Equals(y);
            }

            /// <summary/>
            [Browsable(false)]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public static bool operator !=(SerializableXmlDocument x, SerializableXmlDocument y)
            {
                if (object.Equals(x, null))
                    return !object.Equals(y, null);
                else
                    return !x.Equals(y);
            }

            #endregion

            #region ISerializable Members

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (document != null)
                    info.AddValue("XmlData", document.InnerXml);
                // HACK: SererializationInfo doesn't have a 'contains' method so we always put something so that we don't have to repeatedly try/catch exceptions.
                else
                    info.AddValue("XmlData", string.Empty);
            }

            #endregion
        }

        #endregion

        #region Private Data

        //NOTE: Changing any field implies you must update Clone, GetHashCode and Equal methods.
        private SerializableXmlDocument content = new SerializableXmlDocument();

        #endregion

        #region Constructors

        /// <summary/>
        public ContentPropertyBag()
            : base()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Holds Xml content
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1059")]
        public XmlDocument Content
        {
            get { return content.Document; }
            set { content.Document = value; }
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            ContentPropertyBag other = obj as ContentPropertyBag;

            if (other == null)
                return false;

            if (!base.Equals(other))
                return false;

            return content.Equals(other.content);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            int baseHash = base.GetHashCode();
            return baseHash ^ content.GetHashCode();
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(ContentPropertyBag first, ContentPropertyBag second)
        {
            if (object.Equals(first, null))
                return object.Equals(second, null);
            else
                return first.Equals(second);
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator !=(ContentPropertyBag first, ContentPropertyBag second)
        {
            if (object.Equals(first, null))
                return !object.Equals(second, null);
            else
                return !first.Equals(second);
        }

        #endregion

        #region IXmlSerializable Members

        /// <summary/>
        XmlSchema IXmlSerializable.GetSchema()
        {
            return base.GetSchema();
        }

        /// <summary/>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            content.document = null;
            string elementName = reader.Name;

            //Call the base class Read method to get the property bag
            base.ReadXml(reader, false);

            //Read content
            if (!reader.IsEmptyElement)
            {
                //ReadInnerXml moves the reader to the next element automatically
                string innerXml = reader.ReadInnerXml();
                content.document = new XmlDocument();
                content.document.LoadXml(innerXml);
            }
            else
                reader.Read();
        }

        /// <summary/>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            base.WriteXml(writer);

            if (content.document != null)
                content.document.Save(writer);
        }

        #endregion

        #region ICloneable Members

        object ICloneable.Clone()
        {
            ContentPropertyBag clone = new ContentPropertyBag();

            foreach (KeyValuePair<string, string> propertyValue in this)
            {
                clone[propertyValue.Key] = propertyValue.Value;
            }

            if (content.document != null)
                clone.content.document = (XmlDocument)content.document.Clone();

            return clone;
        }

        #endregion
    }

    /// <summary>
    /// List that can be serialized with custom fields. By default, the Xml Serializer
    /// ignores other public members when it detects ICollection or IEnumerable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    //TODO: This is no longer needed since we are not limited by XmlSerializer anymore.
    [Serializable()]
    [SuppressMessage("Microsoft.Naming", "CA1710")]
    public class AttributeList<T> : List<T>, IXmlSerializable
    {
        #region Constructor

        /// <summary/>
        public AttributeList()
        {
        }

        #endregion

        #region IXmlSerializable Members

        /// <summary/>
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        /// <summary/>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            //Deserializer assumes Xml Attributes are fields.
            string elementName = reader.Name;
            Type itemType = typeof(T);
            XmlSerializer xmlSerializer = new XmlSerializer(itemType);
            Clear();

            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute() && reader.HasValue)
                {
                    PropertyInfo property = this.GetType().GetProperty(reader.Name);
                    if (property != null)                    
                        property.SetValue(this, reader.Value, null);                    
                    else
                    {
                        FieldInfo field = this.GetType().GetField(reader.Name, BindingFlags.IgnoreCase);
                        field.SetValue(this, reader.Value);
                    }
                }
            }

            reader.MoveToElement();

            if (!reader.IsEmptyElement)
            {
                reader.Read(); //Move to child or end element

                //Xml Deserializer advances the reader to the next element
                while (reader.Name != elementName)
                    Add((T)xmlSerializer.Deserialize(reader));
            }

            reader.Read();
        }

        /// <summary/>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {            
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add(string.Empty, string.Empty);

            //TODO: Use a type converter instead of ToString() on the properties?
            FieldInfo[] fields = this.GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                object[] foundAttributes = field.GetCustomAttributes(typeof(XmlAttributeAttribute), false);
                object fieldValue = field.GetValue(this);
                if (foundAttributes.Length > 0 && fieldValue != null)
                    writer.WriteAttributeString(field.Name, fieldValue.ToString());
            }

            Attribute[] attributes = new Attribute[1] { new XmlAnyAttributeAttribute() };
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this, attributes);

            foreach (PropertyDescriptor property in properties)
                writer.WriteAttributeString(property.Name, property.GetValue(this).ToString());

            foreach (T item in this)
                xmlSerializer.Serialize(writer, item, xmlSerializerNamespaces);
        }

        #endregion
    }
}
