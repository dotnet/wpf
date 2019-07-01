// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Collections;
using System.Windows.Markup;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;

namespace Microsoft.Test.Markup
{

    /// <summary>
    /// 
    /// </summary>
    internal class ModifyXaml
    {
        static ModifyXaml()
        {
            string assemblyPath = Path.Combine(Environment.CurrentDirectory, "TestRuntimeUntrusted.dll");
            Assembly customTypesAssembly = Assembly.LoadFile(assemblyPath);

            _extraAssemblies = new Assembly[] { customTypesAssembly };
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        enum ResourceLookupTypes
        {
            /// <summary>
            /// 
            /// </summary>
            UnSet = 0,
            /// <summary>
            /// 
            /// </summary>
            Dynamic,
            /// <summary>
            /// 
            /// </summary>
            Static,
            /// <summary>
            /// 
            /// </summary>
            None,
        }

        /// <summary>
        /// 
        /// </summary>
        public static string PostProcess(string xmlContent, SetupXamlInformation ppParams)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            
            CleanupXamlFile(xmlDoc, ppParams);
            ApplyResourceKeysInXaml(xmlDoc);     

            return xmlDoc.OuterXml;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void CleanupXamlFile(XmlDocument xmlDoc, SetupXamlInformation ppParams)
        {
            CleaningResources(xmlDoc.DocumentElement, ppParams);            
        }


        private static void CleaningResources(XmlNode node, SetupXamlInformation ppParams)
        {
            if (WPFXamlGenerator.IsResourceDictionaryNode(node))
            {

                foreach(XmlNode childResourceNode in node.ChildNodes)
                {

                    XmlElement childResource = childResourceNode as XmlElement;

                    if (childResource == null)
                    {
                        continue;
                    }

                    if (!GetXSharedXmlNodeValue(childResource))
                    {
                        // x:Shared is value is already False;
                        
                        continue;
                    }

                    Type resourceType = FindType(childResource);


                    if (typeof(Visual).IsAssignableFrom(resourceType) || typeof(ContentElement).IsAssignableFrom(resourceType))
                    {
                        if (!ppParams.EnableXamlCompilationFeatures)
                        {
                            node.RemoveChild(childResourceNode);
                        }
                        else
                        {
                            WPFXamlGenerator.SetXSharedValue(childResource, "false");
                        }            
                    }
                    
                }
            }
            else
            {
                
                foreach(XmlNode n in node.ChildNodes)
                {
                    CleaningResources(n,ppParams);
                }                    
            }
            
        }



       
        /// <summary>
        /// 
        /// </summary>
        private static void ApplyResourceKeysInXaml(XmlDocument xmlDoc)
        {
            List<ResourceSourceNode> listAll = new List<ResourceSourceNode>();
            _listAll = listAll;

            ScanForResources(xmlDoc, listAll);

            ApplyResources(xmlDoc, listAll);

        }


        static bool GetXSharedXmlNodeValue(XmlElement node)
        {
            string sharedValue = node.GetAttribute(SharedValue, XamlURI);

            bool value = true;

            if (!String.IsNullOrEmpty(sharedValue))
            {
                value = Convert.ToBoolean(sharedValue);
            }

            return value;


        }


        static void SetShareableState(ResourceSourceNode rsNode, XmlElement resourceElement)
        {
            rsNode.XShared = GetXSharedXmlNodeValue(resourceElement);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="listAll"></param>
        static void ScanForResources(XmlDocument xmlDoc, List<ResourceSourceNode> listAll)
        {
            XmlElement xmlRoot = xmlDoc.DocumentElement;

            NameTable table = new NameTable();

            XmlNamespaceManager mngr = new XmlNamespaceManager(table);
            mngr.AddNamespace("x", XamlURI);
            XmlNodeList listResourceNodex = xmlDoc.SelectNodes("//*[@x:Key]", mngr);

            foreach (XmlNode resourceNode in listResourceNodex)
            {
                if (WPFXamlGenerator.ShouldThrottle(1,2))
                {
                    continue;
                }

                XmlElement resourceElement = (XmlElement)resourceNode;

                string keyName = resourceElement.GetAttribute(KeyName, XamlURI);
                ResourceSourceNode bag = new ResourceSourceNode(resourceElement);
                bag.Key = keyName;

                SetShareableState(bag,resourceElement);
                
                // Validates that the parent on the resource XML Node
                // is Foo.Resources collection.

                if (resourceElement.ParentNode.Name.IndexOf(".Resource") != -1)
                {
                    // Getting the grandparent for the assumption that of we are under Foo.Resources
                    // and properties can be applied to the GrandParent node.
                    //  <TextBox>                               <=== GrandParent
                    //      <TextBox.Resources>
                    //          <SolidColorBrush Color="Red" /> <=== ResourceElement
                    //      </TextBox.Resources>
                    //      <Button />                          <=== TargetNode
                    //  </TextBox>
                    //
                    
                    XmlElement gradparentNode = resourceElement.ParentNode.ParentNode as XmlElement;
                    XmlNodeList descendatNodesList = gradparentNode.SelectNodes("descendant::*");
                    
                    // First scan the grandparent node to find properties.                    
                    ScanSingleInstance(bag, gradparentNode, resourceElement);

                    // Now we scan all the descendents of the grandparent node.
                    foreach (XmlNode node in descendatNodesList)
                    {
                        XmlElement targetNode = node as XmlElement;
                        ScanSingleInstance(bag, targetNode, resourceElement);
                    }
                }

                if (bag.Properties.Count > 0)
                {
                    listAll.Add(bag);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void ScanSingleInstance(ResourceSourceNode resourceSourceNode, XmlElement targetNode, XmlElement resourceElement)
        {            
            XmlElement gradparentNode = resourceElement.ParentNode.ParentNode as XmlElement;

            if (IsInvalid(gradparentNode, resourceElement, targetNode))
            {
                return;
            }

            PropertyInfo[] properties = null;

            properties = GetPropertiesWherePossibleToApply(resourceElement, targetNode);
             
            if (properties != null && properties.Length > 0)
            {
                TargetNodeToApply toApply = new TargetNodeToApply(targetNode, properties);

                resourceSourceNode.Properties.Add(toApply);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        static bool IsDP(Type type, PropertyInfo propertyInfo)
        {
            PropertyDescriptorCollection collection = TypeDescriptor.GetProperties(type);

            foreach (PropertyDescriptor dp in collection)
            {
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(dp);

                if (dpd == null)
                {
                    continue;
                }

                if (dpd.Name == propertyInfo.Name)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        static List<int> CreateXmlElementPositionIndex(XmlElement node)
        {
            Stack<int> positions = new Stack<int>();

            do
            {
                XmlNode parentNode = node.ParentNode as XmlNode;

                for (int i = 0; i < parentNode.ChildNodes.Count; i++)
                {
                    if (node == parentNode.ChildNodes[i])
                    {
                        positions.Push(i);
                        break;
                    }
                }
                node = node.ParentNode as XmlElement;
            }
            while (node != null);

            return new List<int>(positions.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <param name="listAll"></param>
        /// <returns></returns>
        static void ApplyResources(XmlDocument xmlDoc, List<ResourceSourceNode> listAll)
        {
            List<XmlElement> unshareableResources = new List<XmlElement>();
            List<TargetNodeToApply> targetNodeList = new List<TargetNodeToApply>();

            foreach (ResourceSourceNode infoBag in listAll)
            {
                XmlElement sourceNode = infoBag.SourceNode;
                string keyValue = sourceNode.GetAttribute(KeyName, XamlURI);

                // Check that we don't apply an unshareable resource twice.
                if (unshareableResources.Contains(sourceNode))
                {
                    throw new ApplicationException("ResourceSourceNode duplicate found in list.");
                }

                // Skip this resource if it can't be set on any property or we want to throttle
                // the number of references.
                if (infoBag.Properties.Count == 0 || WPFXamlGenerator.ShouldThrottle(1, 4))
                {
                    continue;
                }

                int amountApply = 1;

                if (!infoBag.XShared && infoBag.Properties.Count > 1)
                {
                   amountApply= GetRandomNumber(infoBag.Properties.Count);
                   targetNodeList.AddRange(infoBag.Properties.ToArray());
                }
                else
                {
                    targetNodeList.Add(infoBag.Properties[GetRandomNumber(infoBag.Properties.Count)]);
                }
                                                                  
                for (int i=0; i < amountApply; i++)
                {
                    int index = GetRandomNumber(targetNodeList.Count);

                    TargetNodeToApply pApply = targetNodeList[index];

                    targetNodeList.RemoveAt(index);

                    XmlElement targetNode = pApply.NodeToApply;
                    PropertyInfo targetProperty = pApply.Properties[GetRandomNumber(pApply.Properties.Length)];

                    ApplyResourceToXaml( xmlDoc,
                                         sourceNode,
                                         keyValue,
                                         pApply,
                                         targetNode,
                                         targetProperty);                            
                    
                }

                targetNodeList.Clear();
                                
                // 
                // If a reference to an unshareable resource was set on a property, 
                // keep track of it so we don't set it again.
                //
                if (DoesMatch(infoBag.ResourceType, _unshareableTypes))
                {
                    unshareableResources.Add(sourceNode);
                }
            }

        }


        static void ApplyResourceToXaml( 
                                                XmlDocument xmlDoc,
                                                XmlElement sourceNode,
                                                string keyValue, 
                                                TargetNodeToApply pApply, 
                                                XmlElement targetNode,
                                                PropertyInfo targetProperty)
        {
                object xmlTarget = null;
                string cpa = null;
                bool isPropertySet = IsPropertySet(targetNode, targetProperty, out xmlTarget, out cpa);

                //
                // Set a resource reference on a property using attribute or 
                // property element syntax.
                // 

                if (IsIList(targetProperty.PropertyType))
                {
                    // TODO
                    return;
                }
                else
                {
                    if (isPropertySet)
                    {
                        if (xmlTarget is XmlAttribute)
                        {
                            XmlAttribute attribute = (XmlAttribute)xmlTarget;

                            SetXmlAttributeValue(attribute, keyValue, targetNode, sourceNode, targetProperty);                            
                        }
                        else
                        {
                            //TODO
                            return;
                        }
                    }
                    else
                    {
                        if (WPFXamlGenerator.ShouldThrottle(1,2))
                        {
                            XmlAttribute attribute = xmlDoc.CreateAttribute(targetProperty.Name);

                            bool value = SetXmlAttributeValue(attribute, keyValue, targetNode, sourceNode, targetProperty);
                            
                            if (!value)
                            {
                                return;
                            }

                            targetNode.SetAttributeNode(attribute);
                        }
                        else
                        {
                            // <ParentName.PropertyName>

                            ResourceLookupTypes lookupType = FindResourceLookupType(targetNode, sourceNode, targetProperty);

                            string lookupString = ChooseResourceLookupType(lookupType);

                            if (lookupString == String.Empty)
                            {
                                return;
                            }

                            string tagName = targetNode.Name + "." + targetProperty.Name;
                            XmlElement e = (XmlElement)xmlDoc.CreateElement(tagName, targetNode.NamespaceURI);
                            targetNode.PrependChild(e);


                            string typeDSResource = lookupString;

                            XmlElement e2 = (XmlElement)xmlDoc.CreateElement(typeDSResource, AvalonURI);
                            e2.SetAttribute("ResourceKey", keyValue);
                            e.AppendChild(e2);

                            // </ParentName.PropertyName>

                        }

                    }
                }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="keyValue"></param>
        /// <param name="targetNode"></param>
        /// <param name="sourceNode"></param>
        /// <param name="targetProperty"></param>
        /// <returns></returns>
        static bool SetXmlAttributeValue(XmlAttribute attribute, string keyValue, XmlElement targetNode, XmlElement sourceNode, PropertyInfo targetProperty)
        {
            ResourceLookupTypes lookupType = FindResourceLookupType(targetNode, sourceNode, targetProperty);

            string lookupString = ChooseResourceLookupType(lookupType);

            if (lookupString == String.Empty)
            {
                return false;
            }

            attribute.Value = "{" + lookupString + " " + keyValue + "}";
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lookupTypes"></param>
        /// <returns></returns>
        static string ChooseResourceLookupType(ResourceLookupTypes lookupTypes)
        {
            if (ResourceLookupTypes.None == lookupTypes)
            {
                return String.Empty;
            }

            if (((lookupTypes & ResourceLookupTypes.Dynamic) == ResourceLookupTypes.Dynamic) &&
                ((lookupTypes & ResourceLookupTypes.Static) == ResourceLookupTypes.Static))
            {
                if (WPFXamlGenerator.ShouldThrottle(1,2))
                {
                    return "StaticResource";
                }
                else
                {
                    return "DynamicResource";
                }
            }

            if ((lookupTypes & ResourceLookupTypes.Dynamic) == ResourceLookupTypes.Dynamic)
            {
                return "DynamicResource";
            }

            if ((lookupTypes & ResourceLookupTypes.Static) == ResourceLookupTypes.Static)
            {
                return "StaticResource";
            }

            throw new Exception("TestException");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetNode"></param>
        /// <param name="sourceNode"></param>
        /// <param name="targetProperty"></param>
        /// <returns></returns>
        static ResourceLookupTypes FindResourceLookupType(XmlElement targetNode, XmlElement sourceNode, PropertyInfo targetProperty)
        {
            bool canStatic = CanApplyStaticResourceExtension(targetNode, sourceNode);
            Type type = FindType(targetNode.Name);
            bool canDynamic = IsDP(type, targetProperty);


            ResourceLookupTypes value = ResourceLookupTypes.UnSet;

            if (!canStatic && !canDynamic)
            {
                return ResourceLookupTypes.None;
            }

            if (canStatic)
            {
                value |= ResourceLookupTypes.Static;
            }

            if (canDynamic)
            {
                value |= ResourceLookupTypes.Dynamic;
            }

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="max"></param>
        /// <returns></returns>
        static int GetRandomNumber(int max)
        {
            return DefaultRandomGenerator.GetGlobalRandomGenerator().Next(max);
        }

        //static Random r = new Random();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetNode"></param>
        /// <param name="sourceNode"></param>
        /// <returns></returns>
        static bool CanApplyStaticResourceExtension(XmlElement targetNode, XmlElement sourceNode)
        {
            XmlElement aux = (XmlElement)sourceNode.ParentNode;

            while (aux != null)
            {
                if (aux == targetNode)
                {
                    return false;
                }

                aux = aux.ParentNode as XmlElement;
            }



            List<int> sourceNodeIndex = CreateXmlElementPositionIndex(sourceNode);
            List<int> targetNodeIndex = CreateXmlElementPositionIndex(targetNode);

            for (int i = 0; i < sourceNodeIndex.Count; i++)
            {
                if (targetNodeIndex.Count > i)
                {
                    if (sourceNodeIndex[i] > targetNodeIndex[i])
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="grandParent"></param>
        /// <param name="resourceElement"></param>
        /// <param name="targetNode"></param>
        /// <returns></returns>
        static bool IsInvalid(XmlElement grandParent, XmlElement resourceElement, XmlElement targetNode)
        {

            if (grandParent == null)
            {
                throw new InvalidOperationException("");
            }

            if (targetNode.Name.Contains(".") ||  // This is only because this algorithm doesn't support.                
                targetNode == resourceElement) // Do not it add on the same resource.
            {
                return true;
            }

            // First Validation for no nesting resources.
            XmlElement movingElement = targetNode;
            while (movingElement != grandParent)
            {
                if (movingElement == resourceElement)
                {
                    return true;
                }

                movingElement = movingElement.ParentNode as XmlElement;

                if (movingElement == null)
                {
                    return true;
                }
            }
            
            // Second validation for Style

            Type resourceType = FindType(resourceElement.Name);

            if (!(resourceType == typeof(Style) || resourceType == typeof(ControlTemplate)))
            {
                return false;
            }

            Type targetNodeType = FindType(targetNode);
            
            if (resourceType == typeof(ControlTemplate))
            {
                // Don't support to look for the correct TemplatePart.
                
                object[] oArray = targetNodeType.GetCustomAttributes(typeof(TemplatePartAttribute), false);

                if (oArray != null && oArray.Length > 0)
                {
                    return true;
                }
            }
            
            string targetTypeName = RetrieveTargetType(resourceElement);

            if (targetTypeName == String.Empty)
            {
                return true;
            }
            
            Type targetTypeType = FindType(targetTypeName);

            if (targetTypeType.IsAssignableFrom(targetNodeType))
            {
                return false;
            }

            return true;
        }

        static string RetrieveTargetType(XmlElement element)
        {
            string targetTypeName = element.GetAttribute("TargetType");

            if (targetTypeName.Trim()[0] != '{')
            {
                // It is not a markup extension.
                return targetTypeName;
            }
            
            string typeName = "";
            string args = "";
            GetMarkupExtensionTypeAndArgs(ref targetTypeName, out typeName, out args);

            if (typeName.Trim().IndexOf("Type") != -1)
            {
                args = args.Trim();
                
                if (args[args.Length-1] == '}')
                {
                    args = args.Substring(0, args.Length - 1);
                }
                return args;
            }

            return string.Empty;

        }


        /// <summary>
        /// Parse the attrValue string into a typename and arguments.  Return true if
        /// they parse successfully.
        /// </summary>
        static bool GetMarkupExtensionTypeAndArgs(
            ref string attrValue,
            out string typeName,
            out string args)
        {
            int length = attrValue.Length;
            typeName = string.Empty;
            args = string.Empty;

            // MarkupExtensions MUST have '{' as the first character
            if (length < 1 || attrValue[0] != '{')
            {
                return false;
            }

            bool gotEscape = false;
            StringBuilder stringBuilder = null;
            int i = 1;

            for (; i < length; i++)
            {
                // Skip all whitespace unless we are collecting characters for the 
                // type name. 
                if (Char.IsWhiteSpace(attrValue[i]))
                {
                    if (stringBuilder != null)
                    {
                        break;
                    }
                }
                else
                {
                    // If there is no string builder, then we haven't encountered the 
                    // first non-whitespace character after '{'.
                    if (stringBuilder == null)
                    {
                        // Always escape the first '\'
                        if (!gotEscape && attrValue[i] == '\\')
                        {
                            gotEscape = true;
                        }
                        // We have the first non-whitespace character after '{'
                        else if (attrValue[i] == '}')
                        {
                            // Found the closing '}', so we're done.  If this is the
                            // second character, we have an empty MarkupExtension, which
                            // is a way to escape a string.  In that case, trim off
                            // the first two characters from attrValue so that the caller
                            // will get the unescaped string.
                            if (i == 1)
                            {
                                attrValue = attrValue.Substring(2);
                                return false;
                            }
                        }
                        else
                        {
                            stringBuilder = new StringBuilder(length - i);
                            stringBuilder.Append(attrValue[i]);
                            gotEscape = false;
                        }
                    }
                    else
                    {
                        // Always escape the first '\'
                        if (!gotEscape && attrValue[i] == '\\')
                        {
                            gotEscape = true;
                        }
                        else if (attrValue[i] == '}')
                        {
                            // Found the closing '}', so we're done.
                            break;
                        }
                        else
                        {
                            // Collect characters that make up the type name
                            stringBuilder.Append(attrValue[i]);
                            gotEscape = false;
                        }
                    }
                }
            }

            // Set typeName and arguments.  Note that both may be empty, but having
            // an empyt typeName will generate an error later on.
            if (stringBuilder != null)
            {
                typeName = stringBuilder.ToString();
            }

            if (i < length - 1)
            {
                args = attrValue.Substring(i, length - i);
            }
            else if (attrValue[length - 1] == '}')
            {
                args = "}";
            }

            return true;
        }

        static string GetAttributeValue(XmlElement element, string attributeName)
        {
            string s = String.Empty;

            s = element.GetAttribute(attributeName, AvalonURI);

            return s;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        static string GetCPA(XmlElement element)
        {
            Type type = FindType(element.Name);

            object[] attributes = type.GetCustomAttributes(typeof(ContentPropertyAttribute), true);

            if (attributes != null && attributes.Length > 0)
            {
                ContentPropertyAttribute cpa = (ContentPropertyAttribute)attributes[0];
                return cpa.Name;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="property"></param>
        /// <param name="xmlTarget"></param>
        /// <param name="cpa"></param>
        /// <returns></returns>
        static bool IsPropertySet(XmlElement element, PropertyInfo property, out object xmlTarget, out string cpa)
        {
            cpa = GetCPA(element);

            //Sweep all attributes first.
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (IsAttributeNameEqual(property.Name, attribute.Name))
                {
                    xmlTarget = attribute;
                    return true;
                }
            }
            
            //Sweep all property element syntax
            foreach (XmlNode childNode in element.ChildNodes)
            {
                xmlTarget = childNode;
                if (cpa != null && cpa == property.Name && childNode.Name.IndexOf(".") == -1)
                {
                    return true;
                }

                if (IsAttributeNameEqual(property.Name, childNode.Name))
                {
                    return true;
                }
            }

            xmlTarget = null;
            return false;
        }


        static bool IsAttributeNameEqual(string propertyName, string attributeName)
        {
            int indexOfPoint = attributeName.IndexOf(".");

            if (indexOfPoint != -1)
            {
                attributeName = attributeName.Substring(indexOfPoint + 1);
            }

            return String.Compare(attributeName, propertyName, true) == 0;
        }


        /// <summary>
        /// 
        /// </summary>
        static PropertyInfo[] GetPropertiesWherePossibleToApply(XmlElement xmlSettableType, XmlElement xmlTargetTypeName)
        {
            Type targetType = FindType(xmlTargetTypeName.Name);
            Type settableType = FindType(xmlSettableType.Name);

            return GetPropertiesWherePossibleToApply(settableType, targetType);
        }

        /// <summary>
        /// 
        /// </summary>
        static PropertyInfo[] GetPropertiesWherePossibleToApply(Type settableType, Type targetType)
        {
            List<PropertyInfo> propertyList = new List<PropertyInfo>();

            // First check that we aren't in unsupported types, such as Trigger or Setter.
            if (DoesMatch(targetType, _unsupportedTargetTypes))
            {
                return new PropertyInfo[0];
            }

            PropertyInfo[] propertyInfos = targetType.GetProperties();

            for (int i = 0; i < propertyInfos.Length; i++)
            {
                PropertyInfo targetProperty = propertyInfos[i];

                if (IsIList(targetProperty.PropertyType) && !targetProperty.PropertyType.IsGenericType)
                {
                    int index = targetProperty.PropertyType.Name.IndexOf("Collection");
                    if (index > 1)
                    {
                        string typeNameHelper = targetProperty.PropertyType.Name.Substring(0, index);
                        Type t = FindType(typeNameHelper);

                        if (t != null && t.IsAssignableFrom(settableType))
                        {
                            propertyList.Add(targetProperty);
                        }
                    }
                    else
                    {
                        propertyList.Add(targetProperty);
                    }
                }
                else if (IsIList(targetProperty.PropertyType) && targetProperty.PropertyType.IsGenericType)
                {
                    Type[] types = targetProperty.PropertyType.GetGenericArguments();

                    if (types[0].IsAssignableFrom(settableType))
                    {
                        propertyList.Add(targetProperty);
                    }
                }


                if (!targetProperty.CanWrite)
                {
                    continue;
                }

                if (targetProperty.PropertyType.IsAssignableFrom(settableType))
                {
                    propertyList.Add(targetProperty);
                }
            }

            return propertyList.ToArray();
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static bool IsIList(Type type)
        {
            if (type.GetInterface("System.Collections.IList", true) != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether or not a type is one of a set of types.
        /// </summary>
        public static bool DoesMatch(Type type, Type[] baseTypes)
        {
            bool doesMatch = false;

            foreach (Type baseType in baseTypes)
            {
                if (baseType.IsAssignableFrom(type))
                {
                    doesMatch = true;
                    break;
                }
            }

            return doesMatch;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        static Type FindType(string typeName)
        {
            List<Assembly> assemblies = new List<Assembly>();
            assemblies.AddRange(_avalonAssemblies);
            assemblies.AddRange(_extraAssemblies);

            return FindTypeInternal(typeName, assemblies.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        static Type FindType(XmlElement typeName)
        {
            Assembly[] assemblies = _extraAssemblies;

            if (typeName.NamespaceURI == AvalonURI)
            {
                assemblies = _avalonAssemblies;
            }
            else
            {
                List<Assembly> list = new List<Assembly>();
                
                list.AddRange(_avalonAssemblies);
                list.AddRange(_extraAssemblies);
                assemblies = list.ToArray();
            }

            return FindTypeInternal(typeName.Name, assemblies);
        }
        
        /// <summary>
        /// 
        /// </summary>
        static Type FindTypeInternal(string typeName, Assembly[] assemblies)
        {
            string typeNameHelper = typeName;

            if (typeNameHelper.IndexOf(":") != -1)
            {
                string[] array = typeName.Split(':');
                typeNameHelper = array[1];            
            }
            
            
            lock (_cachingTypes)
            {
                if (_cachingTypes.ContainsKey(typeNameHelper))
                {
                    return (Type)_cachingTypes[typeNameHelper];
                }
            }

            Type foundType = null;

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];

                Type[] typeArray = asm.GetTypes();

                for (int j = 0; j < typeArray.Length; j++)
                {
                    if (typeArray[j].Name == typeNameHelper)
                    {
                        foundType = typeArray[j];
                        break;
                    }
                }

                if (foundType != null)
                {
                    break;
                }
            }

            lock (_cachingTypes)
            {
                if (!_cachingTypes.ContainsKey(typeNameHelper))
                {
                    _cachingTypes.Add(typeNameHelper, foundType);
                }
            }

            if (foundType == null)
            {
                throw new Exception("Type , " + typeNameHelper + ", was not found");
            }

            return foundType;
        }

        [ThreadStatic]
        static List<ResourceSourceNode> _listAll = null;

        static Hashtable _cachingTypes = new Hashtable();
        const string SharedValue = "Shared";
        const string KeyName = "Key";
        const string XamlURI = "http://schemas.microsoft.com/winfx/2006/xaml";
        const string AvalonURI = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        static Assembly[] _avalonAssemblies = { 
                typeof(FrameworkElement).Assembly, 
                typeof(UIElement).Assembly,  
                typeof(DispatcherObject).Assembly, 
                typeof(double).Assembly};

        static Assembly[] _extraAssemblies = { };

        static Type[] _unshareableTypes = new Type[] { typeof(Visual), typeof(ContentElement) };
        static Type[] _unsupportedTargetTypes = new Type[] { typeof(Trigger), typeof(Setter) };
        
        /// <summary>
        /// 
        /// </summary>
        internal struct ResourceSourceNode //PropertyInfoBag
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="targetXml"></param>
            public ResourceSourceNode(XmlElement targetXml)
            {
                Key = "";
                SourceNode = targetXml;
                Properties = new List<TargetNodeToApply>();
                _resourceType = null;
                XShared = true;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="property"></param>
            public void AddProperty(TargetNodeToApply property)
            {

                for (int i = 0; i < Properties.Count; i++)
                {
                    if (Properties[i].NodeToApply == property.NodeToApply)
                    {
                        return;
                    }
                }
                Properties.Add(property);
            }
            /// <summary>
            /// 
            /// </summary>
            public string Key;

            /// <summary>
            /// 
            /// </summary>
            public XmlElement SourceNode;


            /// <summary>
            /// 
            /// </summary>
            public bool XShared;

            /// <summary>
            /// 
            /// </summary>
            public List<TargetNodeToApply> Properties;

            /// <summary>
            /// Returns the Type of the resource.
            /// </summary>
            public Type ResourceType
            {
                get
                {
                    if (_resourceType == null)
                    {
                        _resourceType = FindType(SourceNode.Name);
                    }

                    return _resourceType;
                }
            }

            private Type _resourceType;
        }

        /// <summary>
        /// 
        /// </summary>
        internal struct TargetNodeToApply// PropertyToApply
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="nodeToApply"></param>
            /// <param name="propertyToApply"></param>
            public TargetNodeToApply(XmlElement nodeToApply, PropertyInfo[] propertyToApply)
            {
                NodeToApply = nodeToApply;
                Properties = propertyToApply;
            }

            /// <summary>
            /// 
            /// </summary>
            public XmlElement NodeToApply;
            
            /// <summary>
            /// 
            /// </summary>
            public PropertyInfo[] Properties;

        }
    }
}



