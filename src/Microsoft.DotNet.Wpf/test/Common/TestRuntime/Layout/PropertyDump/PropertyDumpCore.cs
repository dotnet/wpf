// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Windows;
using System.Threading; 
using System.Windows.Threading;
using System.Windows.Documents;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Globalization;
using Microsoft.Test.Logging;
using Microsoft.Test.Compression;

namespace Microsoft.Test.Layout.PropertyDump
{
    /// <summary>
    /// Dump Properties, Field and Mehods values from a running App.
    /// </summary>
    public class PropertyDumpCore
    {
        private XmlTextWriter xmlWriter = null;
        private Visual visualRoot = null;

        /// <summary>
        /// </summary>
        public Filter Filter
        {
            get
            {
                return filter;
            }
        }
        private Filter filter = null;

        /// <summary>
        /// </summary>
        public static XmlDocument xmldoc = null;

        private PropertyDumpCore()
        {
            xmldoc = new XmlDocument();
            filter = new Filter();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visual"></param>
        public PropertyDumpCore(Visual visual): this()
        {
            visualRoot = visual;
        }

        /// <summary>
        /// Saves XML dump to disc
        /// </summary>
        /// <param name="newXmlPath"></param>
        public void SaveXmlDump(string newXmlPath)
        {
            XmlNode node = DumpXml();
            XmlTextWriter writer = null;

            try
            {
                writer = new XmlTextWriter(newXmlPath, System.Text.UTF8Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                node.WriteTo(writer);
            }
            catch (Exception e)
            {
                GlobalLog.LogEvidence(e);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
        }
       
        /// <summary>
        /// XML Dump
        /// </summary>
        /// <returns></returns>
        public XmlNode DumpXml()
        {
            GlobalLog.LogStatus("START XML DUMP");

            XmlNode xmldump = PropertyDumpCore.xmldoc.CreateElement("PropertyDump");
            MemoryStream memStream = new MemoryStream();

            xmlWriter = new XmlTextWriter((MemoryStream)memStream, UTF8Encoding.UTF8);

            InternalXmlDump(visualRoot, xmldump);

            GlobalLog.LogStatus("END XML DUMP");

            return xmldump;
        }

        /// <summary>
        /// Compares dump and master
        /// </summary>
        /// <param name="masterPath"></param>
        /// <param name="renderedPath"></param>
        /// <returns></returns>
        public bool CompareXmlFiles(string masterPath, string renderedPath)
        {
            bool result = false;

            GlobalLog.LogStatus("MASTER     : " + masterPath);
            GlobalLog.LogStatus("RENDERED   : " + renderedPath);

            // check if master exist, if not bail out.
            if (!File.Exists(masterPath))
            {
                GlobalLog.LogEvidence("MASTER NOT FOUND");
                result = false;
                return result;
            }

            // Load master xml
            XmlDocument masterDoc = new XmlDocument();
            masterDoc.PreserveWhitespace = false;
            masterDoc.Load(masterPath);

            // Load rendered xml
            XmlDocument renderedDoc = new XmlDocument();
            renderedDoc.PreserveWhitespace = false;
            renderedDoc.Load(renderedPath);

            // Compare master and rendered
            result = CompareXmlFiles(masterDoc.DocumentElement, renderedDoc.DocumentElement);

            return result;
        }

        /// <summary>
        /// Compares xmlnodes
        /// </summary>
        /// <param name="masterNode"></param>
        /// <param name="renderedNode"></param>
        /// <returns></returns>
        public bool CompareXmlFiles(XmlNode masterNode, XmlNode renderedNode)
        {
            // Simple comparison of XML should be enough, if not use XMLDiff.dll
            if (masterNode.OuterXml == renderedNode.OuterXml)
            {
                return true;
            }        
            //Following Else part add a verify tolerance of the ElementLayout tests.
            else
            {
                // verifiy the value of System.Windows.Size/Height
                XmlNodeList mnl = masterNode.OwnerDocument .SelectNodes(@"//System.Windows.Size/Height");
                XmlNodeList rnl = renderedNode .OwnerDocument .SelectNodes(@"//System.Windows.Size/Height");
                if (!CompareWindowSizeValue(mnl, rnl))
                    return false;
                // verify the value of System.Windows.Size/With
                mnl = masterNode.OwnerDocument.SelectNodes(@"//System.Windows.Size/Width");
                rnl = renderedNode .OwnerDocument .SelectNodes(@"//System.Windows.Size/Width");
                if (!CompareWindowSizeValue(mnl, rnl))
                    return false;
                // verify the value of Line/LayoutBox
                mnl = masterNode.OwnerDocument.SelectNodes(@"//Line/LayoutBox");
                rnl = renderedNode.OwnerDocument.SelectNodes(@"//Line/LayoutBox");
                if (!CompareLineValue(mnl,rnl)) 
                    return false;
                // verify the value of Line/TextRange
                mnl = masterNode.OwnerDocument.SelectNodes(@"//Line/TextRange");
                rnl = renderedNode.OwnerDocument.SelectNodes(@"//Line/TextRange");
                if (!CompareLineValue(mnl, rnl))
                    return false;
                //All above four verification passed, return true;
                return true;
            }

        }

        /// <summary>
        /// Used to compare System.Windows.Size value in [Master].lxml and [Render].lxml
        /// </summary>
        /// <param name="mnl">XML Node in [Master].lxml</param>
        /// <param name="rnl">XML Node in [Render].lxml</param>
        /// <returns></returns>
        private bool CompareWindowSizeValue(XmlNodeList mnl, XmlNodeList rnl)
        {
            if (mnl.Count > 0 && mnl.Count == rnl.Count)
            {
                for (int i = 0; i < mnl.Count; i++)
                {
                    if (Math.Abs(Convert.ToDecimal(mnl[i].Value) - Convert.ToDecimal(rnl[i].Value)) > Convert.ToDecimal(0.5))
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Used to compare Line Node value in [Master].lxml and [Render].lxml 
        /// </summary>
        /// <param name="mnl">XML Node in [Master].lxml</param>
        /// <param name="rnl">XML Node in [Render].lxml</param>
        /// <returns></returns>
        private bool CompareLineValue(XmlNodeList mnl, XmlNodeList rnl)
        {
            if (mnl.Count > 0 && mnl.Count == rnl.Count)
            {
                for (int i = 0; i < mnl.Count; i++)
                {
                    if (mnl[i].Attributes.Count != rnl[i].Attributes.Count)
                        return false;
                    for (int j = 0; j < mnl[i].Attributes.Count; j++)
                    {
                        if (Math.Abs(Convert.ToDecimal(mnl[i].Attributes[j].Value) - Convert.ToDecimal(rnl[i].Attributes[j].Value)) > Convert.ToDecimal("0.5"))
                            return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }

        }

        private DependencyObject FindAncestorWindow(DependencyObject visual)
        {
            DependencyObject _visual = null;

            if (visual is Window)
                _visual = visual;

            if (LogicalTreeHelper.GetParent(visual) is Window)
            {
                _visual = LogicalTreeHelper.GetParent(visual);
            }
            else
            {
                FindAncestorWindow(LogicalTreeHelper.GetParent(visual));
            }

            return _visual;
        }

        private Window AncestorWindow = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="visual"></param>
        /// <param name="parentNode"></param>
        public void InternalXmlDump(DependencyObject visual, XmlNode parentNode)
        {
            if (AncestorWindow == null)
                AncestorWindow = FindAncestorWindow(visual) as Window;

            //Add support for BlockUIContainer
            if (visual is BlockUIContainer)
            {
                visual = ((BlockUIContainer)visual).Child;
            }

            //Add support for InlineUIContainer
            if (visual is InlineUIContainer)
            {
                visual = ((InlineUIContainer)visual).Child;
            }
            
            // Dump only information for UIElement (or class deriving from UIElement)
            if (!(visual is UIElement))
            {
                return;
            }

            XmlNode ownerNode = PropertyDumpCore.xmldoc.CreateElement(visual.GetType().ToString());

            //Get the position of the visual with respect to rootvisual and dump it as properties of the tag.
            GeneralTransform gt = new System.Windows.Media.MatrixTransform();

            if (AncestorWindow != null && AncestorWindow is Window)
            {
                UIElement root = (UIElement)AncestorWindow.Content;
                gt = ((UIElement)visual).TransformToAncestor((Visual)root);
            }
            else
            {
                GlobalLog.LogEvidence(new Exception("Ancestor Window is null"));
            }
            
            Point origin = new Point(0, 0);
            Point myPoint;

            if (gt.TryTransform(origin, out  myPoint) == false)
            {
                //A point may not always be transformable
                GlobalLog.LogEvidence(new ApplicationException("//TODO: Handle GeneralTransform Case - introduced by Transforms Breaking Change"));
            }

            XmlAttribute attr1 = PropertyDumpCore.xmldoc.CreateAttribute("XPos");
            attr1.Value = myPoint.X.ToString("F", CultureInfo.InvariantCulture);
            ownerNode.Attributes.Append(attr1);
            
            XmlAttribute attr2 = PropertyDumpCore.xmldoc.CreateAttribute("YPos");
            attr2.Value = myPoint.Y.ToString("F", CultureInfo.InvariantCulture);
            ownerNode.Attributes.Append(attr2);

            foreach (Package package in Filter.AllProperties)
            {
                XmlNode node = PropertyDumpCore.xmldoc.CreateElement(package.name);

                DumpMethodProperty(visual, package, node);
                if (node.InnerText != string.Empty)
                {
                    ownerNode.AppendChild(node);
                }
            }
            
            FrameworkElements.Init(this);

            DumpCustomUIElement dumpElement = elementToDumpHandler[((UIElement)visual).GetType()] as DumpCustomUIElement;
            
            if (dumpElement != null)
            {
                dumpElement(ownerNode, (UIElement)visual);
            }
            
            int count = VisualTreeHelper.GetChildrenCount(visual);
            
            for (int i = 0; i < count; i++)
            {
                DependencyObject visualChild = VisualTreeHelper.GetChild(visual, i);
                if (visual is Viewbox)
                {
                    GlobalLog.LogStatus(string.Format("ViewBox.Child is {0}", (visualChild != null) ? visualChild.GetType().ToString() : "null"));
                }
                InternalXmlDump(visualChild, ownerNode);
            }
            
            if (ownerNode.InnerText != string.Empty)
            {
                parentNode.AppendChild(ownerNode);
            }
        }

        private void DumpMethodProperty(object objectToQuery, Package package, XmlNode parentNode)
        {
            Type type = null;
            object valueReturned = null;
            object[] outParam = null;
            ParameterInfo[] outParamInfo = null;

            if (package.queryInterface != string.Empty)
            {
                type = objectToQuery.GetType().GetInterface(package.queryInterface, true);
                if (type == null)
                {
                    return;
                }
            }
            else
            {
                type = objectToQuery.GetType();
            }

            // Query Properties
            PropertyInfo propInfo = type.GetProperty(package.name);

            if (propInfo != null)
            {
                valueReturned = type.InvokeMember(propInfo.Name, BindingFlags.GetProperty, null, objectToQuery, null);
            }

            // Query Fields
            FieldInfo fieldInfo = type.GetField(package.name);
            if (fieldInfo != null)
            {
                valueReturned = type.InvokeMember(fieldInfo.Name, BindingFlags.GetField, null, objectToQuery, null);
            }

            // Query Methods
            MethodInfo methodInfo = type.GetMethod(package.name);

            if (methodInfo != null)
            {
                outParamInfo = methodInfo.GetParameters();
                outParam = new object[outParamInfo.Length];
                valueReturned = type.InvokeMember(methodInfo.Name,/*BindingFlags.CreateInstance |*/ BindingFlags.InvokeMethod, null, objectToQuery, outParam);
            }

            if (valueReturned == null)
            {
                return;
            }

            if (package.children.Count == 0)
            {
                // Methods might require to dump out/ref param along with regular return value
                if (outParam != null && outParam.Length != 0)
                {
                    // Dump return value as child node
                    XmlNode retValNode = PropertyDumpCore.xmldoc.CreateElement("RetVal");

                    retValNode.InnerText = valueReturned.ToString();
                    if (retValNode.InnerText != string.Empty)
                    {
                        GlobalLog.LogStatus("Method '" + package.name + "' returned value : " + valueReturned.ToString());
                        parentNode.AppendChild(retValNode);
                    }

                    // Dump "out" params
                    XmlNode outNodes = PropertyDumpCore.xmldoc.CreateElement("OutArgs");

                    for (int t = 0; t < outParamInfo.Length; t++)
                    {
                        if (outParamInfo[t].IsOut)
                        {
                            XmlNode argNode = PropertyDumpCore.xmldoc.CreateElement("Arg_" + (t + 1));

                            GlobalLog.LogStatus("Method '" + package.name + "' ref Param value : " + outParam[t].ToString());

                            // BUGBUG : Do not dump empty string
                            // BUGBUG : Do not recurse on "out" param (type returned might be an object)
                            argNode.InnerText = outParam[t].ToString();
                            outNodes.AppendChild(argNode);
                        }
                    }

                    if (outNodes.InnerText != string.Empty)
                    {
                        parentNode.AppendChild(outNodes);
                    }
                }
                else
                {
                    // Dump returned value as node value (InnerText)
                    if (valueReturned is string)
                    {
                        if (valueReturned.ToString() != string.Empty)
                        {
                            GlobalLog.LogStatus("Property '" + package.name + "' Found on interface '" + package.queryInterface + "' for object '" + objectToQuery.ToString() + "'");
                            parentNode.InnerText = valueReturned.ToString();
                        }
                    }
                    else
                    {
                     //   GlobalLog.LogStatus("Property '" + package.name + "' Found on interface '" + package.queryInterface + "' for object '" + objectToQuery.ToString() + "'");
                        if (valueReturned is Double)
                            parentNode.InnerText = ((Double)valueReturned).ToString("F", CultureInfo.InvariantCulture);
                        else if (valueReturned is float)
                            parentNode.InnerText = ((float)valueReturned).ToString("F", CultureInfo.InvariantCulture);
                        else if (valueReturned is int)
                            parentNode.InnerText = ((int)valueReturned).ToString("F", CultureInfo.InvariantCulture);
                        else
                            parentNode.InnerText = valueReturned.ToString();
                    }
                }
            }
            else
            {
                XmlNode nodeValuRetType = PropertyDumpCore.xmldoc.CreateElement(valueReturned.GetType().ToString());
                foreach (Package childPackage in package.children)
                {
                    XmlNode node = PropertyDumpCore.xmldoc.CreateElement(childPackage.name);
                    DumpMethodProperty(valueReturned, childPackage, node);
                    if (node.InnerText != string.Empty)
                    {
                        nodeValuRetType.AppendChild(node);
                    }
                }
                if (nodeValuRetType.InnerText != string.Empty)
                {
                    parentNode.AppendChild(nodeValuRetType);
                }
            }
        }
        
        /// <summary>
        /// Mapping from UIElement type to dump methdod (DumpCustomUIElement). 
        /// </summary>
        private static Hashtable elementToDumpHandler = new Hashtable();
 
        /// <summary>
        /// Add new mapping from UIElement type to dump methdod (DumpCustomUIElement). 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="dumper"></param>
        public static void AddUIElementDumpHandler(Type type, DumpCustomUIElement dumper)
        {
            elementToDumpHandler.Add(type, dumper);
        }
       
        /// <summary>
        /// Dumper delegate for custom UIElements.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="element"></param>
        public delegate void DumpCustomUIElement(XmlNode writer, UIElement element);
    }
}

