// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.Test
{
    /// <summary>
    /// Defines a serializable property bag. A null value implies the property is not in the bag, whereas an
    /// empty string implies it is defined but not set.
    /// </summary>
    // TODO: Figure out why we don't just make a generic dictionary class that implements IXmlSerializable.
    //       We're also sealing PropertyBag for the moment because we want to see if we can get rid of ContentPropertyBag.
    //       If we need ContentPropertyBag we'll have to unseal it, and then there will be issues with Expoisng some of the
    //       IXmlSerializable members to derived classes.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), Serializable()]
    public class PropertyBag : IXmlSerializable, ICloneable, IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        #region Private Data

        //NOTE: Changing any field implies you must update Clone, GetHashCode and Equal methods.
        private Dictionary<string, string> propertyBag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Constructor

        /// <summary/>
        public PropertyBag()
        {

        }

        #endregion

        #region Public Members

        /// <summary/>
        public string this[string property]
        {
            get
            {
                if (!propertyBag.ContainsKey(property))
                {
                    return null;
                }
                return propertyBag[property];
            }
            set
            {
                if (value == null && propertyBag.ContainsKey(property))
                {
                    propertyBag.Remove(property);
                }

                AddProperty(property, value);
            }
        }

        /// <summary/>
        public bool ContainsProperty(string property)
        {
            return propertyBag.ContainsKey(property);
        }

        /// <summary>
        /// Enumerator
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string>.Enumerator GetEnumerator()
        {
            return propertyBag.GetEnumerator();
        }

        #endregion

        #region Override Members

        /// <summary/>
        public override bool Equals(object obj)
        {
            PropertyBag other = obj as PropertyBag;
            if (other == null)
            {
                return false;
            }

            return propertyBag.SequenceEqual(other);
        }

        /// <summary/>
        public override int GetHashCode()
        {
            return propertyBag.Count;
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(PropertyBag first, PropertyBag second)
        {
            if (object.Equals(first, null))
            {
                return object.Equals(second, null);
            }
            else
            {
                return first.Equals(second);
            }
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator !=(PropertyBag first, PropertyBag second)
        {
            if (object.Equals(first, null))
            {
                return !object.Equals(second, null);
            }
            else
            {
                return !first.Equals(second);
            }
        }

        #endregion

        #region Private Members

        private void AddProperty(string property, string value)
        {
            if (String.IsNullOrEmpty(property))
            {
                throw new ArgumentNullException("property");
            }

            //Ensure the key used can be serialized as an attribute
            XmlConvert.VerifyName(property);
            propertyBag[property] = value;
        }

        #endregion

        #region Protected Members

        /// <summary/>
        // The current version does nothing, but there could be complexity
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        protected XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary/>
        protected void ReadXml(XmlReader reader, bool finishRead)
        {
            //Clear properties
            string elementName = reader.Name;
            propertyBag.Clear();

            while (reader.MoveToNextAttribute())
                propertyBag.Add(reader.Name, reader.Value);

            //Move back to the element
            reader.MoveToElement();

            if (finishRead) //Move to an end element
            {
                reader.Read();
            }
        }

        /// <summary/>
        protected void WriteXml(XmlWriter writer)
        {
            foreach (KeyValuePair<string, string> property in propertyBag)
            {
                writer.WriteAttributeString(property.Key, property.Value);
            }
        }

        #endregion

        #region IXmlSerializable Members

        /// <summary/>
        XmlSchema IXmlSerializable.GetSchema()
        {
            return GetSchema();
        }

        /// <summary/>
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            ReadXml(reader, true);
        }

        /// <summary/>
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            WriteXml(writer);
        }

        #endregion

        #region ICloneable Members

        /// <summary/>
        public object Clone()
        {
            PropertyBag clone = new PropertyBag();

            foreach (KeyValuePair<string, string> propertyValue in this.propertyBag)
            {
                clone.propertyBag.Add(propertyValue.Key, propertyValue.Value);
            }

            return clone;
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,string>> Members

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return propertyBag.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return propertyBag.GetEnumerator();
        }

        #endregion
    }    
}
