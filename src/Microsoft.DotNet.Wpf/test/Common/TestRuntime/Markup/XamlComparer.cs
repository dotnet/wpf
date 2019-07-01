// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using Microsoft.Test.Logging;
using Microsoft.Test.Serialization;

namespace Microsoft.Test.Markup
{
    /// <summary>
    /// Static class XamlComparer provides static methods to compare Xml
    /// </summary>
    public static class XamlComparer
    {
        /// <summary>
        /// Compares two Xml files
        /// </summary>
        /// <param name="fileName1">File name for the first Xml</param>
        /// <param name="fileName2">File name for the second Xml</param>
        /// <returns>
        ///     XmlCompareResult
        /// </returns>
        public static XmlCompareResult CompareFiles(string fileName1, string fileName2)
        {
            return CompareFiles(fileName1, fileName2, null);
        }

        /// <summary>
        /// Compares two Xml files
        /// </summary>
        /// <param name="fileName1">File name for the first Xaml</param>
        /// <param name="fileName2">File name for the second Xaml</param>
        /// <param name="nodesShouldIgnoreChildrenSequence">
        /// Array of name of xml nodes whose children
        /// sequence should be ignored
        /// </param>
        /// <returns> XmlCompareResult</returns>
        public static XmlCompareResult CompareFiles(string fileName1, string fileName2, string[] nodesShouldIgnoreChildrenSequence)
        {
            _nodesShouldIgnoreChildrenSequence = nodesShouldIgnoreChildrenSequence;

            XmlCompareResult result;

            if (null == fileName1)
            {
                throw new ArgumentNullException("fileName1");
            }

            if (null == fileName2)
            {
                throw new ArgumentNullException("fileName2");
            }

            Stream stream1 = null, stream2 = null;
            try
            {
                // change the files to streams
                stream1 = File.OpenRead(fileName1);
                stream2 = File.OpenRead(fileName2);

                //call the method for streams
                result = Compare(stream1, stream2, nodesShouldIgnoreChildrenSequence);
            }
            finally
            {
                //clean up
                if (null != stream1)
                    stream1.Close();

                if (null != stream2)
                    stream2.Close();
            }
            return result;
        }
        /// <summary>
        /// Compare Two Xmls
        /// </summary>
        /// <param name="string1">first Xml</param>
        /// <param name="string2">second Xml</param>
        /// <returns>
        ///     XmlCompareResult
        /// </returns>
        public static XmlCompareResult Compare(string string1, string string2)
        {
            return Compare(string1, string2, null);
        }

        /// <summary>
        /// Compare Two Xmls
        /// </summary>
        /// <param name="string1">The first Xml</param>
        /// <param name="string2">The second Xml</param>
        /// <param name="nodesShouldIgnoreChildrenSequence">
        /// Array of name of xml nodes whose children
        /// sequence should be ignored
        /// </param>
        /// <returns>
        ///     XmlCompareResult
        /// </returns>
        public static XmlCompareResult Compare(string string1, string string2, string[] nodesShouldIgnoreChildrenSequence)
        {
            _nodesShouldIgnoreChildrenSequence = nodesShouldIgnoreChildrenSequence;

            XmlCompareResult result;

            // Parameter Validation

            if (null == string1)
            {
                throw new ArgumentNullException("string1");
            }

            if (null == string2)
            {
                throw new ArgumentNullException("string2");
            }

            Stream stream1 = null, stream2 = null;

            try
            {
                //change the content to streams
                stream1 = IOHelper.ConvertTextToStream(string1);
                stream2 = IOHelper.ConvertTextToStream(string2);

                //call the method for streams
                result = Compare(stream1, stream2, nodesShouldIgnoreChildrenSequence);
            }
            finally
            {
                //clean up
                if (null != stream1)
                    stream1.Close();

                if (null != stream2)
                    stream2.Close();
            }
            return result;
        }



        /// <summary>
        /// Compares two Xmls
        /// </summary>
        /// <param name="xml1">stream for the first Xml</param>
        /// <param name="xml2">stream for the second Xml</param>
        /// <returns>
        ///     XmlCompareResult
        /// </returns>
        public static XmlCompareResult Compare(Stream xml1, Stream xml2)
        {
            return Compare(xml1, xml2, null);
        }

        /// <summary>
        /// Compares two Xmls
        /// </summary>
        /// <param name="xml1">The stream for the first Xml</param>
        /// <param name="xml2">The stream for the second Xml</param>
        /// <param name="nodesShouldIgnoreChildrenSequence">
        /// Array of name of xml nodes whose children
        /// sequence should be ignored
        /// </param>
        /// <returns>
        /// XmlCompareResult
        /// </returns>
        public static XmlCompareResult Compare(Stream xml1, Stream xml2, string[] nodesShouldIgnoreChildrenSequence)
        {
            _nodesShouldIgnoreChildrenSequence = nodesShouldIgnoreChildrenSequence;

            //Validate parameters
            if (null == xml1)
            {
                throw new ArgumentNullException("xml1");
            }

            if (null == xml2)
            {
                throw new ArgumentNullException("xml2");
            }

            XmlDocument doc1 = new XmlDocument();
            XmlDocument doc2 = new XmlDocument();
            //load Xml to be XmlDocument
            doc1.Load(xml1);
            doc2.Load(xml2);
            //Go Compare XmlNodes
            return Compare((XmlNode)doc1, (XmlNode)doc2, nodesShouldIgnoreChildrenSequence);
        }
        /// <summary>
        /// Compares two Xmls
        /// </summary>
        /// <param name="node1">Root XmlNode for the first Xml</param>
        /// <param name="node2">Root XmlNode for the second Xml</param>
        /// <returns>
        ///     XmlCompareResult
        /// </returns>
        public static XmlCompareResult Compare(XmlNode node1, XmlNode node2)
        {
            return Compare(node1, node2, null);
        }

        /// <summary>
        /// Compares two Xmls
        /// </summary>
        /// <param name="node1">The root XmlNode for the first Xml</param>
        /// <param name="node2">The root XmlNode for the second Xml</param>
        /// <param name="nodesShouldIgnoreChildrenSequence">
        /// Array of name of xml nodes whose children
        /// sequence should be ignored
        /// </param>
        /// <returns>XmlCompareResult</returns>
        public static XmlCompareResult Compare(XmlNode node1, XmlNode node2, string[] nodesShouldIgnoreChildrenSequence)
        {
            _nodesShouldIgnoreChildrenSequence = nodesShouldIgnoreChildrenSequence;

            XmlCompareResult result = new XmlCompareResult();

            _mappingTables[0] = new Hashtable();
            _mappingTables[1] = new Hashtable();
            _xmlnsTables[0] = new Hashtable();
            _xmlnsTables[1] = new Hashtable();

            if (CompareXmlNode(node1, node2) && CompareMappings())
            {
                result.Result = CompareResult.Equivalent;
            }
            else
            {
                result.Result = CompareResult.Different;
            }

            return result;
        }

        /// <summary>
        /// Compare two XmlNode
        /// </summary>
        /// <param name="node1">First XmlNode</param>
        /// <param name="node2">Second XmlNode</param>
        /// <returns>
        /// true, if them are equivalent
        /// false, otherwise
        /// </returns>
        private static bool CompareXmlNode(XmlNode node1, XmlNode node2)
        {
            // Both are null
            if (null == node1 && null == node2)
            {
                return true;
            }

            //Only one node is null, the other is not
            if (null == node1 || null == node2)
            {
                XmlNode node = (null == node1 ? node2 : node1);
                SendCompareMessage("One node is null, but other one is: \n"
                    + node.OuterXml);
                return false;
            }

            // Compare Name
            if (!CompareName(node1, node2))
            {
                SendCompareMessage("Node names do not match.");
                return false;
            }

            // Compare Attributes
            if (!CompareAttributes(node1, node2))
            {
                SendCompareMessage("Attribute collections do not match. First node is '" + node1.Name + "' Second node is '" + node2.Name + "'.");
                return false;
            }

            //Compare Children collection
            if (!CompareChildren(node1, node2))
            {
                SendCompareMessage("Children collections do not match for node " + node1.Name + ".");
                return false;
            }

            // all are the same
            return true;
        }

        private static bool CompareMappings()
        {
            if (_mappingTables[0].Count != _mappingTables[1].Count)
            {
                SendCompareMessage("Total Mapping PIs (processing instructions) do not match. First node has " + _mappingTables[0].Count.ToString() + ". Second node has " + _mappingTables[0].Count.ToString() + ".");
                return false;
            }

            foreach (string mapping in _mappingTables[0].Values)
            {
                if (!_mappingTables[1].ContainsValue(mapping))
                {
                    SendCompareMessage("Cannot find Mapping '" + mapping + "' in the second node.");
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        ///  Compare the names of the XmlNodes
        /// </summary>
        /// <param name="node1">First Node</param>
        /// <param name="node2">Second Node</param>
        /// <returns>
        ///  true, if Names are the same, 
        ///  false, otherwise
        /// </returns>  
        private static bool CompareName(XmlNode node1, XmlNode node2)
        {
            // Compare Name
            if (0 != String.Compare(node1.LocalName, node2.LocalName))
            {
                SendCompareMessage("Local names do not match: " + node1.LocalName + " vs. " + node2.LocalName + ".");
                return false;
            }

            string namespaceUri1 = node1.NamespaceURI;
            string namespaceUri2 = node2.NamespaceURI;

            //No namespaceuri for both
            if (null == namespaceUri1 && null == namespaceUri2)
                return true;

            //Only one cannot be found
            if (null == namespaceUri1 || null == namespaceUri2)
            {
                SendCompareMessage("One of the name space not found.");
                return false;
            }
            //Different clr namespace or assembly
            if (0 != String.Compare((string)(_mappingTables[0][namespaceUri1]),
                (string)(_mappingTables[1][namespaceUri2])))
            {
                SendCompareMessage("Namespaces do not match: " + _mappingTables[0][namespaceUri1] + " vs. " + _mappingTables[1][namespaceUri2] + ".");
                return false;
            }

            //same value
            return true;
        }

        /// <summary>
        /// Compare all attributes of two nodes. 
        /// </summary>
        /// <param name="node1">First node</param>
        /// <param name="node2">Second Node</param>
        /// <returns>
        ///          true, if all attributes have the same value for these two node
        ///          false, otherwise
        /// </returns>
        private static bool CompareAttributes(XmlNode node1, XmlNode node2)
        {
            XmlAttributeCollection attributes1 = node1.Attributes;
            XmlAttributeCollection attributes2 = node2.Attributes;

            if (null == attributes1 ^ null == attributes2)
            {
                SendCompareMessage("One one and only one of the attributes collection is null for node " + node1.Name + ".");
                return false;
            }
            else if (null == attributes1)
            {
                //both are null
                return true;
            }

            // Filter out and store separately all xmlns prefixes - first node.
            for (int i = 0; i < attributes1.Count; i++)
            {
                XmlAttribute attribute1 = attributes1[i];

                if (attribute1.Name.StartsWith("xmlns:"))
                {
                    AddToXmlnsTables(attribute1.Name.Substring(6), attribute1.Value, 0);
                }
            }

            // Filter out and store separately all xmlns prefixes - second node.
            for (int i = 0; i < attributes2.Count; i++)
            {
                XmlAttribute attribute2 = attributes2[i];

                if (attribute2.Name.StartsWith("xmlns:"))
                {
                    AddToXmlnsTables(attribute2.Name.Substring(6), attribute2.Value, 1);
                }
            }

            // Convert suffix of name of attributes for the second node to string containing clr namespace and assembly.

            Hashtable attributesTable2 = new Hashtable();
            for (int i = 0; i < attributes2.Count; i++)
            {
                XmlAttribute attribute2 = attributes2[i];

                if (!attribute2.Name.StartsWith("xmlns:") && !String.Equals(attribute2.Name, "xmlns", StringComparison.InvariantCulture))
                {
                    attributesTable2.Add(TransformXmlnsPrefixes(attribute2.Name, 1), attribute2);
                }
            }

            //Record how many attibutes that is not xmlns and remains uncompared. 
            int activeNonXmlnsAttributeCount = attributesTable2.Count;

            // Verify each attribute in the first node matches one in the second
            // node in both name and value.
            for (int i = 0; i < attributes1.Count; i++)
            {
                XmlAttribute attribute1 = attributes1[i];

                if (attribute1.Name.StartsWith("xmlns:") || String.Equals(attribute1.Name, "xmlns", StringComparison.InvariantCulture))
                    continue;

                string attributeName = TransformXmlnsPrefixes(attribute1.Name, 0);

                XmlAttribute attribute2 = (XmlAttribute)attributesTable2[attributeName];

                if (null == attribute2)
                {
                    SendCompareMessage("Didn't find attribute " + attribute1.Name + " in the second node.");
                    return false;
                }

                // Replace any xmlns prefixes in the values with their corresponding 
                // Mapping strings.  This is necessary because the prefixes are
                // never guaranteed to match.  Only the namespaces must match.
                string val1 = TransformXmlnsPrefixes(attribute1.Value, 0);
                string val2 = TransformXmlnsPrefixes(attribute2.Value, 1);

                if (!String.Equals(val1, val2, StringComparison.InvariantCulture))
                {
                    SendCompareMessage("Different values for attribute " + attribute1.Name + ": >" + val1 + "< vs. >" + val2 + "<.");
                    return false;
                }
                activeNonXmlnsAttributeCount--;
            }

            //in the case node 2 has more attribute, fails.
            if (0 != activeNonXmlnsAttributeCount)
            {
                SendCompareMessage("The first and second nodes have different attribute counts. Second node has " + activeNonXmlnsAttributeCount + " more.");
                return false;
            }

            // same
            return true;
        }

        /// <summary>
        /// Recursively compare children. 
        /// Put children for the second node in an ArrayList. 
        /// For every child for the first node, find equivalent node in that ArrayList. It
        /// found, delete the found node. After all children of first node is found, that 
        /// ArrayList should be empty
        /// </summary>
        /// <param name="node1">First Node</param>
        /// <param name="node2">Second Node</param>
        /// <returns>
        /// true, if Children are all equivalent
        /// false, otherwise
        /// </returns>
        private static bool CompareChildren(XmlNode node1, XmlNode node2)
        {
            XmlNodeList children1 = node1.ChildNodes;
            XmlNodeList children2 = node2.ChildNodes;

            if (null == children1 ^ null == children2)
            {
                SendCompareMessage("One one and only one of the Children collections is null for node " + node1.Name + ".");
                return false;
            }
            else if (null == children1)
            {
                //both are null
                return true;
            }

            // Verify the count of children is the same.
            if (children1.Count != children2.Count)
            {
                SendCompareMessage("The first and second nodes have different child counts. First: " + children1.Count + " Second: " + children2.Count + ".");
                return false;
            }

            ArrayList childrenArray1 = SeperateMappingOut(children1, 0);
            ArrayList childrenArray2 = SeperateMappingOut(children2, 1);

            if (ShouldConsiderSequence(node1.LocalName))
            {
                ArrayList cProperties1 = GetComplexPropertiesOut(childrenArray1);
                ArrayList cProperties2 = GetComplexPropertiesOut(childrenArray2);

                return CompareChildrenConsideringSequence(childrenArray1, childrenArray2)
                    && CompareChildrenNoSequence(cProperties1, cProperties2);
            }
            else
            {
                return CompareChildrenNoSequence(childrenArray1, childrenArray2);
            }
        }

        private static ArrayList GetComplexPropertiesOut(ArrayList children)
        {
            ArrayList properties = new ArrayList();

            //Get all properties, whose name contains a ".".
            foreach (XmlNode node in children)
            {
                if (node.Name.Contains("."))
                {
                    properties.Add(node);
                }
            }

            //Remove preperties from children collection.
            foreach (XmlNode node in properties)
                children.Remove(node);

            return properties;
        }

        private static ArrayList SeperateMappingOut(XmlNodeList list, int index)
        {
            ArrayList listWithoutMapping = new ArrayList();

            foreach (XmlNode node in list)
            {
                if ("Mapping" == node.LocalName)
                {
                    AddToMappingTable(node, index);
                }
                else
                    listWithoutMapping.Add(node);
            }

            return listWithoutMapping;
        }

        private static void AddToXmlnsTables(string prefix, string xmlNamespace, int index)
        {
            if (_mappingTables[index].Contains(xmlNamespace))
            {
                string mapping = (string)_mappingTables[index][xmlNamespace];

                _xmlnsTables[index][prefix] = mapping;
            }
            else
            {
                _xmlnsTables[index][prefix] = xmlNamespace;
            }
        }

        private static string TransformXmlnsPrefixes(string original, int index)
        {
            string newstr = original;

            foreach (object key in _xmlnsTables[index].Keys)
            {
                string prefix = (string)key;
                string mapping = (string)_xmlnsTables[index][key];
                newstr = newstr.Replace(prefix + ":", "[[" + mapping + "]]:");
            }

            return newstr;
        }

        private static void AddToMappingTable(XmlNode node, int index)
        {
            if (null == node)
            {
                throw new Exception("One node is null.");
            }

            string strValue = node.Value;

            if (null == strValue)
            {
                throw new Exception("Mapping Value is null.");
            }

            //seperate the Value into two string, first is namespace URI, 
            //second string contains other information
            string[] mappingValues = strValue.Split(new char[] { ' ', '\"', '=' }, 4, StringSplitOptions.RemoveEmptyEntries);

            _mappingTables[index][mappingValues[1]] = mappingValues[3];
        }

        /// <summary>
        /// When compare the children of a node, should we consider the 
        /// sequence?
        /// </summary>
        /// <param name="name">name of the node</param>
        /// <returns>
        /// true, if answer is yes
        /// false, otherwise
        /// </returns>
        private static bool ShouldConsiderSequence(string name)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (0 == String.Compare(name, "ResourceDictionary"))
            {
                return false;
            }

            if (null == _nodesShouldIgnoreChildrenSequence)
                return true;

            foreach (string nodeShouldIgnoreChildrenSequence in _nodesShouldIgnoreChildrenSequence)
            {
                if (null == nodeShouldIgnoreChildrenSequence) continue;
                if (0 == String.Compare(name, nodeShouldIgnoreChildrenSequence))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compare two XmlNodeLists. Return false even only the sequence of 
        /// items is different.
        /// </summary>
        /// <param name="children1">The first XmlNodeList</param>
        /// <param name="children2">The second XmlNodeList</param>
        /// <returns>
        /// true, if two list are equivalent
        /// false, otherwise
        /// </returns>
        private static bool CompareChildrenConsideringSequence(ArrayList children1, ArrayList children2)
        {
            for (int i = 0; i < children1.Count; i++)
            {
                XmlNode childNode1 = children1[i] as XmlNode;
                XmlNode childNode2 = children2[i] as XmlNode;
                bool equivalent = CompareXmlNode(childNode1, childNode2);

                if (!equivalent)
                {
                    SendCompareMessage("Child nodes at index " + i.ToString() + " do not match.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare two XmlNodeLists. If only sequence inside the list 
        /// is different the two lists are consider equivalent
        /// </summary>
        /// <param name="children1">The first XmlNodeList</param>
        /// <param name="children2">The second XmlNodeList</param>
        /// <returns>
        /// true, if two list are equivalent
        /// false, otherwise
        /// </returns>      
        private static bool CompareChildrenNoSequence(ArrayList children1, ArrayList children2)
        {
            ArrayList childrenArray = new ArrayList();
            for (int i = 0; i < children2.Count; i++)
            {
                childrenArray.Add(children2[i]);
            }

            for (int i = 0; i < children1.Count; i++)
            {
                bool findIt = false;

                for (int j = 0; j < children2.Count; j++)
                {
                    findIt = CompareXmlNode((XmlNode)children1[i], (XmlNode)children2[j]);
                    if (findIt)
                    {
                        childrenArray.Remove(children2[j]);
                        break;
                    }
                }

                if (false == findIt)
                {
                    SendCompareMessage("Didn't find child node " + ((XmlNode)children1[i]).Name + " in the second node.");
                    return false;
                }
            }

            return true;
        }

        #region Variables

        //Hashtables to hold the Mapping PIs and xmlns definitions
        static Hashtable[] _mappingTables = new Hashtable[2];
        static Hashtable[] _xmlnsTables = new Hashtable[2];

        static string[] _nodesShouldIgnoreChildrenSequence;
        #endregion Variables
        #region XamlComparerStatus

        /// <summary>
        /// Calls handlers of XamlComparerStatus of XamlComparer
        /// </summary>
        private static void SendCompareMessage(string message)
        {
            // Log To Console
            GlobalLog.LogStatus(message);
        }

        #endregion XamlComparerStatus
    }

    #region CompareResult struct
    /// <summary>
    /// Struct containing result.
    /// </summary>
    public struct XmlCompareResult
    {
        /// <summary>
        /// Enum CompareResult representing the compared result
        /// </summary>
        /// <value></value>
        public CompareResult Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
            }
        }

        CompareResult _result;
    }

    /// <summary>
    /// Enum representing compared result
    /// </summary>
    public enum CompareResult
    {
        /// <summary>
        /// Two xmls are equivalent
        /// </summary>
        Equivalent,
        /// <summary>
        /// Two xmls are not equivalent
        /// </summary>
        Different
    }
    #endregion CompareResult struct


}
