// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Collections;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Layout.PropertyDump
{
    /// <summary>
    /// Summary description for Filter.
    /// </summary>
    public class Filter
    {
        private XmlDocument xmldoc = null;
        private string      xmlpath = null;
        private ArrayList   extraProperties = null;

        private Package     packagesFromXml;

        internal Package[] AllProperties
        {
            get
            {
                Package[] retVal = new Package[packagesFromXml.children.Count + extraProperties.Count];

                packagesFromXml.children.CopyTo(retVal);
                extraProperties.CopyTo(retVal, packagesFromXml.children.Count);
                return  retVal;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string XmlFilterPath
        {
            get
            {
                return xmlpath;
            }
            set
            {
                if (xmlpath == value)
                {
                    return;
                }

                packagesFromXml = new Package(string.Empty, string.Empty);

                if (value == null || value.Trim() == string.Empty)
                {
                    // filter can be null, if no xml filter is needed
                    xmlpath = null;
                }
                
                if (!File.Exists(value))
                {
                    GlobalLog.LogEvidence(new FileNotFoundException("The specified file does not exist (Network issues ?)", value));
                }

                // Get Xml defined properties						
                XmlDocument xmlDoc = new XmlDocument();
                
                xmlDoc.Load(value);

                XmlElement xmlElement = xmlDoc.DocumentElement;

                BuildPackageList(xmlElement.FirstChild, packagesFromXml);

                xmlpath = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ArrayList ExtraProperties
        {
            get
            {
                return extraProperties;
            }
        }

        internal Filter()
        {
            extraProperties    = new ArrayList();
            xmldoc             = new XmlDocument();
            packagesFromXml    = new Package(string.Empty, string.Empty);
        }

        private void BuildPackageList(XmlNode node, Package parent)
        {
            string interfaceValue   = string.Empty;
            string nameValue        = string.Empty;

            XmlAttributeCollection attributes = node.Attributes;

            if (attributes["interface"] != null)
            {
                interfaceValue = attributes["interface"].Value;
            }

            if (attributes["name"] != null)
            {
                nameValue = attributes["name"].Value;
            }

            Package package = new Package(interfaceValue, nameValue);

            parent.children.Add(package);

            XmlNode child = node.FirstChild;

            if (child != null)
            {
                BuildPackageList(child, package);
            }

            XmlNode next = node.NextSibling;

            if (next != null)
            {
                BuildPackageList(next, parent);
            }
        }
    }
}
