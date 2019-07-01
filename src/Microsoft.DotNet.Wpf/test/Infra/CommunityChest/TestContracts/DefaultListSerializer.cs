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
    //Serializes a list of objects
    internal class DefaultListSerializer : IObjectSerializer
    {
        #region Private Data

        private string itemElementName;

        #endregion


        #region Constructors

        public DefaultListSerializer() : this(null)
        {
        }

        //itemElementName: overrides the name of the item elements
        public DefaultListSerializer(string itemElementName)
        {
            this.itemElementName = itemElementName;
        }

        #endregion


        #region IObjectSerializer Implementation

        public void Serialize(XmlTextWriter writer, object obj, bool writeElement)
        {
            IList list = (IList)obj;

            //Serialize the object and its public members
            //Generic types have a ` in the name so we have to strip it out
            if (writeElement)
            {
                writer.WriteStartElement(obj.GetType().Name.Replace("`", ""));
            }

            if (list.Count > 0)
            {
                //determine the converter for the type of the list if it is a generic
                //if it is not a generic then it will use a converter per type
                Type listType = GetGenericListType(list.GetType());
                TypeConverter listTypeConverter = null;
                if (listType != null)
                {
                    listTypeConverter = TypeDescriptor.GetConverter(listType);
                }
                if (!DefaultObjectSerializer.CanSerialize(listTypeConverter))
                {
                    listTypeConverter = null;
                }

                foreach (object listEntry in list)
                {
                    //If we have a item element name specified then use it otherwise use the name of the type
                    string elementName = (itemElementName != null) ? itemElementName : listEntry.GetType().Name;

                    if (listTypeConverter != null)
                    {
                        writer.WriteElementString(elementName, listTypeConverter.ConvertToString(null, DefaultObjectSerializer.InvariantSerializationCulture, listEntry));
                    }
                    else
                    {
                        writer.WriteStartElement(elementName);
                        ObjectSerializer.Serialize(writer, listEntry, false);
                        writer.WriteEndElement();
                    }
                }
            }

            if (writeElement)
            {
                writer.WriteEndElement(); //Object element
            }
        }


        public object Deserialize(XmlTextReader reader, Type type, object context)
        {
            //see if we need to add items to the context
            IList list = context as IList;

            //otherwise create the object
            if (list == null)
            {
                list = (IList)Activator.CreateInstance(type);
            }

            if (!reader.IsEmptyElement)
            {
                //If the list is a generic then predermine the type
                //otherwise it will try to use the element-type mapping
                Type listType = GetGenericListType(list.GetType());
                reader.Read();//first child element
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        object listEntry = ObjectSerializer.Deserialize(reader, listType, null);
                        list.Add(listEntry);
                    }
                    else
                    {
                        reader.Read(); // Ignore non-Element nodes.
                    }
                }
            }
            reader.Read(); //end element

            return list;
        }

        #endregion


        #region Private Members

        //determines the type of a generic list
        public static Type GetGenericListType(Type type)
        {
            Type interfaceType = type.GetInterface("IList`1");
            if (interfaceType != null)
            {
                return interfaceType.GetGenericArguments()[0];
            }
            return null;
        }

        #endregion

    }

}