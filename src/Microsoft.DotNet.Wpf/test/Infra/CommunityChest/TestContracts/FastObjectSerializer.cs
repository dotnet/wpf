// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// A faster implementation of DefaulObjectSerializer with a focus on performance over generated xml readability/consistency.
    /// </summary>
    internal class FastObjectSerializer : DefaultObjectSerializer
    {
        #region Protected Members

        /// <summary/>       
        protected override List<PropertyDescriptor> SortAttributedProperties(object obj)
        {
            //Gets all the properties for an object and sorts them by
            //which properties want to be serialized as an XMLAttribute
            //This is required since attribute must be serialized before elements

            List<PropertyDescriptor> props = new List<PropertyDescriptor>();
            List<PropertyDescriptor> elementProps = new List<PropertyDescriptor>();
            PropertyDescriptorCollection oldProps = TypeDescriptor.GetProperties(obj);
            XmlAttributeAttribute xmlAttr = new XmlAttributeAttribute();  //Creating this allows us to avoid calling the contructor for every iteration
            foreach (PropertyDescriptor prop in oldProps)
            {
                if (prop.Attributes.Contains(xmlAttr))  //Add the PropertyDescriptors that contain XmlAttributes
                {
                    props.Add(prop);
                }
                else
                {
                    elementProps.Add(prop);
                }
            }
            props.AddRange(elementProps);   //Add the element based properties
            return props;
        }

        #endregion

    }
}