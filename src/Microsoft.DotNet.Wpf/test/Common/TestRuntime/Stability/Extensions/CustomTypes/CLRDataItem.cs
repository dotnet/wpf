// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.Test.Stability.Extensions.CustomTypes
{
    /// <summary>
    /// This class supply data source for binding
    /// </summary>
    public class CLRDataItem
    {
        #region Constructor

        public CLRDataItem()
        {
            PropertyChanged += new PropertyChangedEventHandler(CLRDataItemPropertyChanged);
        }

        #endregion

        #region Private Data

        private string stringValue;
        private Int32 intValue;
        private bool boolValue;
        private double doubleValue;
        private float floatValue;
        private static Type[] supportedTypes = new Type[] { typeof(string), typeof(int), typeof(bool), typeof(double), typeof(float) };
        private XmlDocument xmlDocument = new XmlDocument();

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChangedEvent(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region Public Properties

        public string StringValue
        {
            get { return stringValue; }
            set
            {
                stringValue = value;
                RaisePropertyChangedEvent("StringValue");
            }
        }

        public int IntegerValue
        {
            get { return intValue; }
            set
            {
                intValue = value;
                RaisePropertyChangedEvent("IntegerValue");
            }
        }

        public bool BooleanValue
        {
            get { return boolValue; }
            set
            {
                boolValue = value;
                RaisePropertyChangedEvent("BooleanValue");
            }
        }

        public double DoubleValue
        {
            get { return doubleValue; }
            set
            {
                doubleValue = value;
                RaisePropertyChangedEvent("DoubleValue");
            }
        }

        public float FloatValue
        {
            get { return floatValue; }
            set
            {
                floatValue = value;
                RaisePropertyChangedEvent("FloatValue");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create an XmlDocument representation of this Library.
        /// </summary>
        /// <returns>Library in XmlDocument format.</returns>
        public XmlDocument ToXmlDocument()
        {
            UpdateXmlDocument();

            return xmlDocument;
        }

        public void ModifyData(string newStringValue, int newIntValue, bool newBoolValue, double newDoubleValue, float newFloatValue)
        {
            stringValue = newStringValue;
            intValue = newIntValue;
            boolValue = newBoolValue;
            doubleValue = newDoubleValue;
            floatValue = newFloatValue;

            RaisePropertyChangedEvent("StringValue");
            RaisePropertyChangedEvent("IntegerValue");
            RaisePropertyChangedEvent("BooleanValue");
            RaisePropertyChangedEvent("DoubleValue");
            RaisePropertyChangedEvent("FloatValue");

            UpdateXmlDocument();
        }

        public static bool IsSupported(Type type)
        {
            for (int i = 0; i < supportedTypes.Length; i++)
            {
                if (type == supportedTypes[i])
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Members

        private void CLRDataItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Trace.WriteLine(string.Format("{0} property changed.", e.PropertyName));
        }

        private void UpdateXmlDocument()
        {
            xmlDocument.RemoveAll();

            XmlNode root = xmlDocument.CreateElement(typeof(CLRDataItem).Name);
            xmlDocument.AppendChild(root);

            XmlElement stringProperty = xmlDocument.CreateElement("StringValue");
            stringProperty.InnerText = StringValue;
            root.AppendChild(stringProperty);

            XmlElement intProperty = xmlDocument.CreateElement("IntegerValue");
            intProperty.InnerText = IntegerValue.ToString();
            root.AppendChild(intProperty);

            XmlAttribute boolAttribute = xmlDocument.CreateAttribute("BooleanValue");
            boolAttribute.Value = BooleanValue.ToString();
            root.Attributes.Append(boolAttribute);

            XmlAttribute doubleAttribute = xmlDocument.CreateAttribute("DoubleValue");
            doubleAttribute.Value = DoubleValue.ToString();
            root.Attributes.Append(doubleAttribute);

            XmlAttribute floatAttribute = xmlDocument.CreateAttribute("FloatValue");
            floatAttribute.Value = FloatValue.ToString();
            root.Attributes.Append(floatAttribute);
        }

        #endregion
    }
}
