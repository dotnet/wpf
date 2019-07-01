// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test
{    
    /// <summary>
    /// Generic XML serializer that follows the same convention as the System.Xml.Serialization
    ///The value of this is that it is interoperable with our own serialization and we can customize
    ///it as needed.
    /// </summary>
    internal class DefaultObjectSerializer : IObjectSerializer
    {
        #region Private Members

        internal static readonly CultureInfo InvariantSerializationCulture = InitializeCultureInfo();

        #endregion

        #region IObjectSerializer Implementation
        
        /// <summary/>        
        public void Serialize(XmlTextWriter writer, object obj, bool writeElement)
        {
            //Use the IXmlSerializable implementation if needed
            IXmlSerializable serializableObject = obj as IXmlSerializable;
            if (serializableObject != null)
            {
                serializableObject.WriteXml(writer);
                return;
            }

            //If the type can be converted directly to a string then serialize it
            //as an element with text content (int, string, etc)
            TypeConverter conv = TypeDescriptor.GetConverter(obj);
            if (CanSerialize(conv))
            {
                writer.WriteElementString(obj.GetType().Name.ToUpperInvariant(), conv.ConvertToInvariantString(obj));
                return;
            }

            //Serialize the object and its public members
            if (writeElement)
            {
                writer.WriteStartElement(obj.GetType().Name);
            }

            //Get the list of properties with the ones that want to be serialized as attributed listed first
            List<PropertyDescriptor> props = SortAttributedProperties(obj);

            //Serialize the Properties
            foreach (PropertyDescriptor prop in props)
            {
                //Get the property value
                object value = prop.GetValue(obj);
                if (value == null)
                {
                    continue;
                }

                if (prop.IsReadOnly)
                {
                    //if the property is read-only and is a list then serialize the content
                    IList list = value as IList;
                    if (list != null && list.Count > 0)
                    {
                        writer.WriteStartElement(prop.Name);

                        string elementName = prop.Name.Substring(0, prop.Name.Length - 1);
                        DefaultListSerializer serializer = new DefaultListSerializer(elementName);
                        serializer.Serialize(writer, list, false);

                        writer.WriteEndElement();
                    }
                }
                else
                {
                    //Get a converter and Serialize it to a string
                    TypeConverter converter = prop.Converter;
                    if (converter == null || !converter.CanConvertTo(typeof(string)) || !converter.CanConvertFrom(typeof(string)))
                    {
                        writer.WriteStartElement(prop.Name);
                        ObjectSerializer.Serialize(writer, value, false);
                        writer.WriteEndElement();
                        continue;
                    }

                    string strValue = converter.ConvertToString(null, InvariantSerializationCulture, value);

                    if (prop.Attributes.Contains(new XmlAttributeAttribute()))
                    {
                        writer.WriteAttributeString(prop.Name, strValue);
                    }
                    else
                    {
                        writer.WriteElementString(prop.Name, strValue);
                    }
                }
            }

            if (writeElement)
            {
                writer.WriteEndElement(); //Object element
            }
        }

        /// <summary/>     
        public object Deserialize(XmlTextReader reader, Type type, object context)
        {

            //If the object has a type converter then just read the attribute string
            TypeConverter conv = TypeDescriptor.GetConverter(type);
            if (CanSerialize(conv))
            {
                string strValue = reader.ReadElementString();                
                return conv.ConvertFromInvariantString(strValue);
            }

            //Create an instance of the type
            Object obj = Activator.CreateInstance(type);

            //Use the IXmlSerializable implementation if needed
            IXmlSerializable serializableObject = obj as IXmlSerializable;
            if (serializableObject != null)
            {
                serializableObject.ReadXml(reader);
                return obj;
            }

            //parse all the attributes as properties
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);
            bool hasChildren = !reader.IsEmptyElement;
            if (reader.MoveToFirstAttribute())
            {
                do
                {
                    PropertyDescriptor prop = properties[reader.Name];
                    if (prop == null)
                    {
                        throw new InvalidOperationException("the property " + reader.Name + " does not exist on the object " + type.Name);
                    }

                    reader.ReadAttributeValue();
                    TypeConverter converter = prop.Converter;
                    object value = converter.ConvertFromInvariantString(reader.Value);
                    prop.SetValue(obj, value);
                }
                while (reader.MoveToNextAttribute());
            }
            reader.Read();

            if (hasChildren)
            {
                //Parse the children as properties
                while (true)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        //If the property is read-only and a list then add the children to the list members
                        PropertyDescriptor prop = properties[reader.Name];
                        if (prop == null)
                        {
                            throw new InvalidOperationException("The property " + reader.Name + " does not exist on the object " + type.Name + ". Line " + reader.LineNumber + ", Position " + reader.LinePosition + ".");
                        }

                        if (prop.IsReadOnly)
                        {
                            IList list = prop.GetValue(obj) as IList;
                            if (list != null)
                            {
                                DefaultListSerializer serializer = new DefaultListSerializer();
                                serializer.Deserialize(reader, list.GetType(), list);
                            }
                        }
                        else
                        {
                            //parse the value and set the property
                            object value = ObjectSerializer.Deserialize(reader, prop.PropertyType, null);
                            prop.SetValue(obj, value);
                        }
                    }
                    // If end element then we are done parsing children.
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        reader.Read(); //end element
                        break;
                    }
                    else if (reader.EOF)
                    {
                        break;  //end of document
                    }
                    // If we don't explicitly handle this kind of node then ignore it.
                    else
                    {
                        reader.Read();
                    }
                }
            }
            return obj;
        }

        #endregion


        #region Protected Members

        /// <summary/>     
        protected virtual List<PropertyDescriptor> SortAttributedProperties(object value)
        {
            //Gets all the properties for an object and sorts them by
            //which properties want to be serialized as an XMLAttribute
            //and then alphabetically by name
            //Note: This implementation is incredibly inefficient.

            //HACK: we have to use a List<T> because the Sort method on the
            //      PropertyDescriptorCollection doesn't respect the comparer
            List<PropertyDescriptor> props = new List<PropertyDescriptor>();
            PropertyDescriptorCollection oldProps = TypeDescriptor.GetProperties(value);
            foreach (PropertyDescriptor prop in oldProps)
            {
                props.Add(prop);
            }

            props.Sort(delegate(PropertyDescriptor x, PropertyDescriptor y)
            {
                bool xAttribute = x.Attributes.Contains(new XmlAttributeAttribute());
                bool yAttribute = y.Attributes.Contains(new XmlAttributeAttribute());
                if (xAttribute == yAttribute)
                {
                    return StringComparer.InvariantCulture.Compare(x.Name, y.Name); //sort by name
                }
                else if (xAttribute && !yAttribute)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            });
            return props;
        }

        #endregion


        #region Static Members

        //a converter can serialize if it supports convertion to and from a string
        internal static bool CanSerialize(TypeConverter converter1)
        {
            return converter1 != null && converter1.CanConvertTo(typeof(string)) && converter1.CanConvertFrom(typeof(string));
        }

        internal static CultureInfo InitializeCultureInfo()
        {
            return CultureInfo.InvariantCulture; 
        }


        #endregion


    }

}
