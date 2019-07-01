// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Markup;
using Microsoft.Test.Logging;


namespace Microsoft.Test.Serialization
{
    /// <summary>
    /// Contains convenience methods for handling BAML files.
    /// 
    /// BamlReaderWrapper and BamlWriterWrapper used in this class are lightweight wrappers
    /// around BamlReader and BamlWriter (which are internal) in System.Windows.Markup namespace.
    /// The wrappers mirror the API of the BamlReader/Writer publicly, by calling the corresponding 
    /// internal methods using Reflection.
    /// </summary>
    public class BamlHelper
    {
        #region Public Methods
        /// <summary>
        /// Loads baml from a file using the Avalon internal API.
        /// </summary>
        /// <param name="filePath">Path to baml file.</param>
        /// <returns>Tree root.</returns>
        public static object LoadBaml(string filePath)
        {
            FileStream stream = File.OpenRead(filePath);
            string path = Path.GetDirectoryName(filePath);

            return LoadBaml(stream, path);
        }
        /// <summary>
        /// Loads baml from a stream using the Avalon internal API.
        /// </summary>
        /// <param name="stream">Stream of baml.</param>
        /// <param name="streamDirectory">Base directory of the baml.</param>
        /// <returns>Tree root.</returns>
        public static object LoadBaml(Stream stream, string streamDirectory)
        {
            ParserContext pc = new ParserContext();
            string baseUri;

            if (streamDirectory == null)
                baseUri = Environment.CurrentDirectory;
            else if (streamDirectory.EndsWith(@"\"))
                baseUri = streamDirectory;
            else
                baseUri = streamDirectory + @"\";

            pc.BaseUri = new Uri(baseUri, UriKind.RelativeOrAbsolute);

            Type parserType = typeof(System.Windows.Markup.XamlReader);

            return parserType.InvokeMember("LoadBaml",
                                            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                                            null,
                                            null,
                                            new object[] { stream, pc, null, false });
        }

        /// <summary>
        /// Reads an existing BAML using BamlReaderWrapper. After reading each node, it calls the given 
        /// callback function, providing the function with the data in the node just read.
        /// </summary>
        /// <param name="bamlIn">Path of the BAML to read</param>
        /// <param name="callback">Function to be called after reading each node</param>
        public static void ReadBaml(String bamlIn, BamlNodeCallback callback)
        {
            // Since we want to reuse the EditBaml function, we need to have an output BAML
            // file. So we create a temporary file, use the EditBaml function, and then
            // discard the temporary file.
            String tempFile = Path.GetTempFileName();
            EditBaml(bamlIn, tempFile, callback);
            File.Delete(tempFile);
        }

        /// <summary>
        /// Reads an existing BAML using BamlReaderWrapper and writes the same contents to a new BAML
        /// using BamlWriterWrapper
        /// </summary>
        /// <param name="bamlIn">Path of the Original BAML</param>
        /// <param name="bamlOut">Path of the BAML to be written</param>
        public static void CopyBaml(String bamlIn, String bamlOut)
        {
            EditBaml(bamlIn, bamlOut, null);
        }


        /// <summary>
        /// Reads an existing BAML using BamlReaderWrapper and writes the same contents to a new BAML
        /// using BamlWriterWrapper
        /// </summary>
        /// <param name="bamlIn">Path of the Original BAML</param>
        /// <param name="bamlOut">Path of the BAML to be written</param>
        public static void CopyBaml(Stream bamlIn, Stream bamlOut)
        {
            EditBaml(bamlIn, bamlOut, null);
        }


        /// <summary>
        /// Reads an existing BAML using BamlReaderWrapper, allows editing of the contents
        /// and writes the (possibly modified) contents to a new BAML using BamlWriterWrapper.
        ///
        /// After each node is read from the BamlReaderWrapper, it calls the given callback function,
        /// passing the data in the node just read. The callback function can then modify the
        /// data. The callback function is also passed the BamlWriterWrapper, so that it can add
        /// nodes, if it wants. 
        /// 
        /// The callback function is supposed to return a BamlNodeAction, specifiying 
        /// whether the current BAML node should be written to the output BAML file.
        /// By returning BamlNodeAction.Skip, the callback function can specify to delete nodes.
        /// 
        /// If the callback function returns BamlNodeAction.Continue, then the (possibly modified)
        /// contents will be written into the new BAML.
        /// </summary>
        /// <param name="bamlIn">Path of the Original BAML</param>
        /// <param name="bamlOut">Path of the BAML to be written</param>
        /// <param name="callback">EditBamlNode callback. Can be null.</param>
        public static void EditBaml(string bamlIn, string bamlOut, BamlNodeCallback callback)
        {
            Stream bamlInStream = File.OpenRead(bamlIn);
            Stream bamlOutStream = File.OpenWrite(bamlOut);

            try
            {
                EditBaml(bamlInStream, bamlOutStream, callback);
            }
            finally
            {
                bamlInStream.Close();
                bamlOutStream.Close();
            }
        }


        /// <summary>
        /// Reads an existing BAML using BamlReaderWrapper, allows editing of the contents
        /// and writes the (possibly modified) contents to a new BAML using BamlWriterWrapper.
        ///
        /// After each node is read from the BamlReaderWrapper, it calls the given callback function,
        /// passing the data in the node just read. The callback function can then modify the
        /// data. The callback function is also passed the BamlWriterWrapper, so that it can add
        /// nodes, if it wants. 
        /// 
        /// The callback function is supposed to return a BamlNodeAction, specifiying 
        /// whether the current BAML node should be written to the output BAML file.
        /// By returning BamlNodeAction.Skip, the callback function can specify to delete nodes.
        /// 
        /// If the callback function returns BamlNodeAction.Continue, then the (possibly modified)
        /// contents will be written into the new BAML.
        /// </summary>
        /// <param name="bamlIn">Path of the Original BAML</param>
        /// <param name="bamlOut">Path of the BAML to be written</param>
        /// <param name="callback">EditBamlNode callback. Can be null.</param>
        public static void EditBaml(Stream bamlIn, Stream bamlOut, BamlNodeCallback callback)
        {
            BamlReaderWrapper reader = new BamlReaderWrapper(bamlIn);
            BamlWriterWrapper writer = new BamlWriterWrapper(bamlOut);

            // Stack for verifying matching BAML nodes
            Stack matchingStack = new Stack();
            BamlNodeData expectedData;

            // Default action, in case there is no callback specified.
            BamlNodeAction action = BamlNodeAction.Continue;

            // Go through the input BAML, reading different types of records and 
            // writing them to the output BAML
            while (reader.Read())
            {
                // Copy the significant fields from BamlReaderWrapper to BamlNodeData
                // This nodeData can be then passed to a BamlNodeCallback delegate
                BamlNodeData nodeData = new BamlNodeData();
                PopulateBamlNodeData(reader, nodeData);

                // Make another copy for pushing to stack, in case of those nodes
                // which we are checking for node matching
                // We cannot work with the previous copy, since that may be modified by the 
                // callback.
                BamlNodeData stackData = new BamlNodeData();
                PopulateBamlNodeData(reader, stackData);

                switch (reader.NodeType)
                {
                    case "StartDocument":
                        matchingStack.Push(stackData);
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteStartDocument();
                        break;

                    case "EndDocument":
                        // Test for matching BAML nodes.
                        // Try to match the data with StartDocument's data
                        if (matchingStack.Count <= 0)
                            throw new Exception("Unmatched BAML nodes found");
                        expectedData = matchingStack.Pop() as BamlNodeData;
                        if (expectedData.NodeType != "StartDocument")
                            throw new Exception("Unmatched BAML nodes found");
                        CompareBamlNodes(stackData, expectedData);

                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteEndDocument();
                        break;

                    case "StartElement":
                        matchingStack.Push(stackData);
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteStartElement(nodeData.AssemblyName, nodeData.Name, nodeData.IsInjected, nodeData.CreateUsingTypeConverter);
                        break;

                    case "EndElement":
                        // Test for matching BAML nodes.
                        // Try to match the data with StartElement's data
                        if (matchingStack.Count <= 0)
                            throw new Exception("Unmatched BAML nodes found");
                        expectedData = matchingStack.Pop() as BamlNodeData;
                        if (expectedData.NodeType != "StartElement")
                            throw new Exception("Unmatched BAML nodes found");
                        CompareBamlNodes(stackData, expectedData);

                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteEndElement();
                        break;

                    case "StartConstructor":
                        matchingStack.Push(stackData);
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteStartConstructor();
                        break;

                    case "EndConstructor":
                        // Test for matching BAML nodes.
                        // Try to match the data with StartConstructor's data
                        if (matchingStack.Count <= 0)
                            throw new Exception("Unmatched BAML nodes found");
                        expectedData = matchingStack.Pop() as BamlNodeData;
                        if (expectedData.NodeType != "StartConstructor")
                            throw new Exception("Unmatched BAML nodes found");
                        CompareBamlNodes(stackData, expectedData);

                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteEndConstructor();
                        break;

                    case "StartComplexProperty":
                        matchingStack.Push(stackData);
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteStartComplexProperty(nodeData.AssemblyName, nodeData.Name.Substring(0, nodeData.Name.LastIndexOf('.')), nodeData.LocalName);
                        break;

                    case "EndComplexProperty":
                        // Test for matching BAML nodes.
                        // Try to match the data with StartComplexProperty's data
                        if (matchingStack.Count <= 0)
                            throw new Exception("Unmatched BAML nodes found");
                        expectedData = matchingStack.Pop() as BamlNodeData;
                        if (expectedData.NodeType != "StartComplexProperty")
                            throw new Exception("Unmatched BAML nodes found");
                        CompareBamlNodes(stackData, expectedData);

                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteEndComplexProperty();
                        break;

                    case "LiteralContent":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteLiteralContent(nodeData.Value);
                        break;

                    case "Text":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteText(nodeData.Value, nodeData.TypeConverterAssemblyName, nodeData.TypeConverterName);
                        break;

                    case "RoutedEvent":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteRoutedEvent(nodeData.AssemblyName, nodeData.Name.Substring(0, nodeData.Name.LastIndexOf('.')), nodeData.LocalName, nodeData.Value);
                        break;

                    case "Event":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WriteEvent(nodeData.LocalName, nodeData.Value);
                        break;

                    case "IncludeReference":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        break;

                    case "PIMapping":
                        if (callback != null)
                            action = callback(nodeData, writer);
                        if (BamlNodeAction.Continue == action)
                            writer.WritePIMapping(nodeData.XmlNamespace, nodeData.ClrNamespace, nodeData.AssemblyName);
                        break;

                    default:
                        throw new Exception("Unexpected NodeType read from BamlReaderWrapper");
                }

                // Property nodes are not provided by BamlReaderWrapper when Read() is called
                // We need to go through them separately.
                if (reader.HasProperties)
                {
                    reader.MoveToFirstProperty();
                    do
                    {
                        // This has to be present in this loop, because reader values
                        // keep changing as we move thru the properties
                        PopulateBamlNodeData(reader, nodeData);

                        if (reader.NodeType == "Property")
                        {
                            if (callback != null)
                                action = callback(nodeData, writer);
                            if (BamlNodeAction.Continue == action)
                                writer.WriteProperty(nodeData.AssemblyName, nodeData.Name.Substring(0, nodeData.Name.LastIndexOf('.')), nodeData.LocalName, nodeData.Value, nodeData.AttributeUsage);
                        }
                        else if (reader.NodeType == "ContentProperty")
                        {
                            if (callback != null)
                                action = callback(nodeData, writer);
                            if (BamlNodeAction.Continue == action)
                                writer.WriteContentProperty(nodeData.AssemblyName, nodeData.Name.Substring(0, nodeData.Name.LastIndexOf('.')), nodeData.LocalName);
                        }
                        else if (reader.NodeType == "DefAttribute")
                        {
                            if (callback != null)
                                action = callback(nodeData, writer);
                            if (BamlNodeAction.Continue == action)
                                writer.WriteDefAttribute(nodeData.Name, nodeData.Value);
                        }
                        else if (reader.NodeType == "XmlnsProperty")
                        {
                            if (callback != null)
                                action = callback(nodeData, writer);
                            if (BamlNodeAction.Continue == action)
                                writer.WriteXmlnsProperty(nodeData.LocalName, nodeData.Value);
                        }
                        else if (reader.NodeType == "ConnectionId")
                        {
                            if (callback != null)
                                action = callback(nodeData, writer);
                            if (BamlNodeAction.Continue == action)
                                writer.WriteConnectionId(Int32.Parse(nodeData.Value));
                        }
                        else
                        {
                            throw new Exception("Unexpected NodeType read from BamlReaderWrapper while trying to read properties on an element");
                        }
                    } while (reader.MoveToNextProperty());
                }
            }

            // Check that matching stack is empty
            if (matchingStack.Count != 0)
                throw new Exception("Unmatched BAML nodes found");


        }

        #region CompareBamlFiles
        /// <summary>
        /// Compares 2 Baml files node by node.
        /// </summary>
        /// <param name="sourcefile"></param>
        /// <param name="targetfile"></param>
        /// <returns></returns>
        public static ArrayList CompareBamlFiles(string sourcefile, string targetfile)
        {
            return CompareBamlFiles(sourcefile, targetfile, false);
        }

        /// <summary>
        /// </summary>
        static public bool BreakOnError = false;

        /// <summary>
        /// </summary>
        private static void Break()
        {
            if (BreakOnError)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Compares 2 Baml files node by node.
        /// </summary>
        public static ArrayList CompareBamlFiles(string sourcefile, string targetfile, bool verbose)
        {
            ArrayList dif = new ArrayList();
            BamlReaderWrapper Reader1;
            BamlReaderWrapper Reader2;
            Stream fs1 = File.OpenRead(sourcefile);
            Stream fs2 = File.OpenRead(targetfile);
            Reader1 = new BamlReaderWrapper(fs1);
            Reader2 = new BamlReaderWrapper(fs2);
#if (!STRESS_RUNTIME)
            GlobalLog.LogStatus("Comparing Baml files node by node.");
#endif
            while ((Reader1.Read()) & (Reader2.Read()))
            {
                if (Reader1.NodeType.ToString() == Reader2.NodeType.ToString())
                    LogIfVerbose(Reader2.NodeType.ToString(), verbose);
                else
                {
                    LogError("source " + Reader1.NodeType.ToString() + " is different from target " + Reader2.NodeType.ToString());

                    dif.Add(Reader1.NodeType.ToString());
                    dif.Add(Reader2.NodeType.ToString());
                    Break();
                }
                switch (Reader1.NodeType)
                {
                    case "LiteralContent":
                    case "Text":
                        if (Reader1.Value == Reader2.Value)
                            LogIfVerbose(Reader2.Value, verbose);
                        else
                        {
                            LogError("source value " + Reader1.Value + " is different from target " + Reader2.Value);
                            dif.Add(Reader1.Value);
                            dif.Add(Reader2.Value);
                            Break();
                        }
                        break;


                    case "PIMapping":

                        if (Reader1.XmlNamespace == Reader2.XmlNamespace)
                            LogIfVerbose(Reader2.XmlNamespace, verbose);
                        else
                        {
                            LogError("source value " + Reader1.XmlNamespace + " is different from target " + Reader2.XmlNamespace);
                            dif.Add(Reader1.XmlNamespace);
                            dif.Add(Reader2.XmlNamespace);
                            Break();
                        }

                        if (Reader1.ClrNamespace == Reader2.ClrNamespace)
                            LogIfVerbose(Reader2.ClrNamespace, verbose);
                        else
                        {
                            LogError("source value " + Reader1.ClrNamespace + " is different from target " + Reader2.ClrNamespace);
                            dif.Add(Reader1.ClrNamespace);
                            dif.Add(Reader2.ClrNamespace);
                            Break();
                        }

                        if (Reader1.AssemblyName == Reader2.AssemblyName)
                            LogIfVerbose(Reader2.AssemblyName, verbose);
                        else
                        {
                            LogError("source value " + Reader1.AssemblyName + " is different from target " + Reader2.AssemblyName);
                            dif.Add(Reader1.AssemblyName);
                            dif.Add(Reader2.AssemblyName);
                            Break();
                        }
                        break;

                    default:
                        break;

                }

                if (Reader1.HasProperties && Reader2.HasProperties)
                {
                    Reader1.MoveToFirstProperty();
                    Reader2.MoveToFirstProperty();
                    do
                    {
                        if (Reader1.NodeType.ToString() == Reader2.NodeType.ToString())
                            LogIfVerbose(Reader2.NodeType.ToString(), verbose);
                        else
                        {
                            LogError("source " + Reader1.NodeType.ToString() + " is different from target " + Reader2.NodeType.ToString());
                            dif.Add(Reader1.NodeType.ToString());
                            dif.Add(Reader2.NodeType.ToString());
                            Break();
                        }

                        if (Reader1.Name == Reader2.Name)
                            LogIfVerbose(Reader2.Name, verbose);
                        else
                        {
                            LogError("source property name " + Reader1.Name + " is different from target property name " + Reader2.Name);
                            dif.Add(Reader1.Name);
                            dif.Add(Reader2.Name);
                            Break();
                        }

                        if (Reader1.Value == Reader2.Value)
                            LogIfVerbose(Reader2.Value, verbose);
                        else
                        {
                            LogError("source property value " + Reader1.Value + " is different from target property Value " + Reader2.Value);
                            dif.Add(Reader1.Value);
                            dif.Add(Reader2.Value);
                            Break();
                        }
                    }
                    while (Reader1.MoveToNextProperty() & Reader2.MoveToNextProperty());
                }
                else
                {
                    // If both don't have properties, it's fine. 
                    // If only one of them has properties, it's an error.
                    if (Reader1.HasProperties || Reader2.HasProperties)
                    {
                        if (Reader1.HasProperties)
                            LogError("Source file has properties here and Target file does NOT");
                        else
                            LogError("Source file does NOT have properties here and Target file does");

                        dif.Add(Reader1.HasProperties);
                        dif.Add(Reader2.HasProperties);
                        Break();
                    }
                }
            }

            Reader1.Close();
            Reader2.Close();

            return dif;
        }

        /// <summary>
        /// Log an error
        /// </summary>
        /// <param name="log"></param>
        private static void LogError(string log)
        {
            GlobalLog.LogEvidence(log);
        }

        /// <summary>
        /// Logs only if verbose parameter is true.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="verbose"></param>
        private static void LogIfVerbose(string log, bool verbose)
        {
            if (verbose)
            {
                GlobalLog.LogStatus(log);
            }
        }
        #endregion CompareBamlFiles

        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Prints a set of fields from a BamlReaderWrapper
        /// </summary>
        /// <param name="reader">BamlReaderWrapper to print</param>
        private void PrintBamlNode(BamlReaderWrapper reader)
        {
            Console.WriteLine("***** Start BAML record *****");
            Console.WriteLine("NodeType: " + reader.NodeType);
            Console.WriteLine("Name: " + reader.Name);
            Console.WriteLine("LocalName: " + reader.LocalName);
            Console.WriteLine("Prefix: " + reader.Prefix);
            Console.WriteLine("Value: " + reader.Value);
            Console.WriteLine("AssemblyName: " + reader.AssemblyName);
            Console.WriteLine("ClrNamespace: " + reader.ClrNamespace);
            Console.WriteLine("XmlNamespace: " + reader.XmlNamespace);
            Console.WriteLine("***** End BAML record *****");
            Console.WriteLine(" ");
        }

        /// <summary>
        /// Copy fields from BamlReaderWrapper to the corresponding fields of BamlNodeData.
        /// </summary>
        /// <param name="reader">Source BamlReaderWrapper</param>
        /// <param name="nodeData">Destination nodeData where fields are to be copied.</param>
        private static void PopulateBamlNodeData(BamlReaderWrapper reader, BamlNodeData nodeData)
        {
            nodeData.NodeType = reader.NodeType;
            nodeData.AssemblyName = reader.AssemblyName;
            nodeData.Prefix = reader.Prefix;
            nodeData.XmlNamespace = reader.XmlNamespace;
            nodeData.ClrNamespace = reader.ClrNamespace;
            nodeData.Name = reader.Name;
            nodeData.LocalName = reader.LocalName;
            nodeData.Value = reader.Value;
            nodeData.IsInjected = reader.IsInjected;
            nodeData.CreateUsingTypeConverter = reader.CreateUsingTypeConverter;
            nodeData.TypeConverterAssemblyName = reader.TypeConverterAssemblyName;
            nodeData.TypeConverterName = reader.TypeConverterName;
            nodeData.AttributeUsage = reader.AttributeUsage;
        }

        /// <summary>
        /// A function that compares the actual field values of a BamlNodeData with those expected,
        /// and throws an exception at the first mis-match.
        /// </summary>
        /// <param name="actualData">Actual data</param>
        /// <param name="expectedData">Expected data</param>
        private static void CompareBamlNodes(BamlNodeData actualData, BamlNodeData expectedData)
        {
            // Compare Name
            if (expectedData.Name != actualData.Name)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " Name (expected): " + expectedData.Name + "\n" + " Name (found): " + actualData.Name);
            }

            // Compare LocalName
            if (expectedData.LocalName != actualData.LocalName)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " LocalName (expected): " + expectedData.LocalName + "\n" + " LocalName (found): " + actualData.LocalName);
            }

            // Compare ClrNamespace
            if (expectedData.ClrNamespace != actualData.ClrNamespace)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " ClrNamespace (expected): " + expectedData.ClrNamespace + "\n" + " ClrNamespace (found): " + actualData.ClrNamespace);
            }

            // Compare XmlNamespace
            if (expectedData.XmlNamespace != actualData.XmlNamespace)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " XmlNamespace (expected): " + expectedData.XmlNamespace + "\n" + " XmlNamespace (found): " + actualData.XmlNamespace);
            }

            // Compare AssemblyName
            if (expectedData.AssemblyName != actualData.AssemblyName)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " AssemblyName (expected): " + expectedData.AssemblyName + "\n" + " AssemblyName (found): " + actualData.AssemblyName);
            }

            // Compare Prefix
            if (expectedData.Prefix != actualData.Prefix)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " Prefix (expected): " + expectedData.Prefix + "\n" + " Prefix (found): " + actualData.Prefix);
            }

            // Compare Value
            if (expectedData.Value != actualData.Value)
            {
                throw new Exception("Field different from expected in Baml node type " + actualData.NodeType + "\n" + " Value (expected): " + expectedData.Value + "\n" + " Value (found): " + actualData.Value);
            }
        }
        #endregion Private Methods

        #region Delegates
        /// <summary>
        /// Callback delegate used by methods like ReadBaml, EditBaml, etc. in this class.
        /// Please see the documentation of these methods for further information.
        /// </summary>
        public delegate BamlNodeAction BamlNodeCallback(BamlNodeData nodeData, BamlWriterWrapper writer);
        #endregion Delegates
    }

    #region Enum BamlNodeAction
    /// <summary>
    /// Action to take on the current Baml node 
    /// </summary>
    public enum BamlNodeAction : byte
    {
        /// <summary>
        /// Write the current Baml node to output
        /// </summary>
        Continue = 0,

        /// <summary>
        /// Skip the current Baml node
        /// </summary>
        Skip
    }
    #endregion Enum BamlNodeAction

    #region Class BamlNodeData
    /// <summary>
    /// This class holds a subset of BamlReaderWrapper fields, but they are read-write here,
    /// as opposed to BamlReaderWrapper.
    /// </summary>
    public class BamlNodeData
    {
        /// <summary>
        /// The type of node, be it element, property, etc.
        /// </summary>
        public string NodeType;

        /// <summary>
        /// AssemblyName
        /// </summary>
        public string AssemblyName;

        /// <summary>
        /// Prefix
        /// </summary>
        public string Prefix;

        /// <summary>
        /// XmlNamespace
        /// </summary>
        public string XmlNamespace;

        /// <summary>
        /// ClrNamespace
        /// </summary>
        public string ClrNamespace;

        /// <summary>
        /// Name
        /// </summary>
        public string Name;

        /// <summary>
        /// LocalName
        /// </summary>
        public string LocalName;

        /// <summary>
        /// Value
        /// </summary>
        public string Value;

        /// <summary>
        /// IsInjected
        /// </summary>
        public bool IsInjected;

        /// <summary>
        /// CreateUsingTypeConverter
        /// </summary>
        public bool CreateUsingTypeConverter;

        /// <summary>
        /// CreateUsingTypeConverter
        /// </summary>
        public string TypeConverterAssemblyName;

        /// <summary>
        /// CreateUsingTypeConverter
        /// </summary>
        public string TypeConverterName;

        /// <summary>
        /// AttributeUsage
        /// </summary>
        public object AttributeUsage;
    }
    #endregion Class BamlNodeData
}
