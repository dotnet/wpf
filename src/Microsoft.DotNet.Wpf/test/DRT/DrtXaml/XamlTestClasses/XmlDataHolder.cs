// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Markup;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace Test.Elements
{
    [ContentProperty("XmlData")]
    public class XmlDataHolderElement: Element
    {
        private TestXmlSerializer _xmlData;

        public XmlDataHolderElement()
        {
            _xmlData = new TestXmlSerializer();
        }

        public IXmlSerializable XmlData
        {
            get { return _xmlData; }
        }
    }


    public class TestXmlSerializer : IXmlSerializable
    {
#region IXmlSerializable Members
        XmlSchema  IXmlSerializable.GetSchema()
        {
 	        return null;
        }

        public void  ReadXml(XmlReader reader)
        {
        }

        public void  WriteXml(XmlWriter writer)
        {
        }
#endregion
    }
}
