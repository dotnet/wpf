// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using Microsoft.Test.Logging;
using Microsoft.Test.Markup;


namespace Microsoft.Test.Windows
{
    /// <summary>
    /// A class providing a static method to compare two Avalon logical trees
    /// by comparing node by node and property by property.
    /// </summary>
    public static class TreeComparer
    {
        #region Public Method
        /// <summary>
        /// Compare two object trees. If all the descendant logical nodes 
        /// are equivalent, return true, otherwise, return false.
        /// </summary>
        /// <param name="firstTree">The root for the first tree</param>
        /// <param name="secondTree">The root for the second tree</param>
        /// <remarks>
        /// Compares every event and property for each the node.
        /// </remarks>
        /// <returns>
        /// A structure containing result. If the returned variable name is result.
        /// result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        /// and CompareResult.Different otherwise.
        /// </returns>
        public static TreeCompareResult CompareLogical(
            Object firstTree,
            Object secondTree)
        {
            return CompareLogical(firstTree, secondTree, _skipPropertiesDefault);
        }
        /// <summary>
        /// Compare two object trees. If all the descendant logical nodes 
        /// are equivalent, return true, otherwise, return false.
        /// </summary>
        /// <param name="firstTree">The root for the first tree.</param>
        /// <param name="secondTree">The root for the second tree.</param>
        /// <param name="fileName">Custom list of properties to ignore.</param>
        /// <remarks>
        /// Compares every event and property for each the node.
        /// </remarks>
        /// <returns>
        /// A structure containing result. If the returned variable name is result.
        /// result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        /// and CompareResult.Different otherwise.
        /// </returns>
        public static TreeCompareResult CompareLogical(
            Object firstTree,
            Object secondTree,
            string fileName)
        {
            Dictionary<string, PropertyToIgnore> props = ReadSkipProperties(fileName);

            return CompareLogical(firstTree, secondTree, props);
        }
        /// <summary>
        /// Compare two object trees. If all the descendant logical nodes 
        /// are equivalent, return true, otherwise, return false.
        /// </summary>
        /// <param name="firstTree">The root for the first tree.</param>
        /// <param name="secondTree">The root for the second tree.</param>
        /// <param name="propertiesToIgnore">Custom list of properties to ignore.</param>
        /// <remarks>
        /// Compares every event and property for each the node.
        /// </remarks>
        /// <returns>
        /// A structure containing result. If the returned variable name is result.
        /// result.Result is CompareResult.Equivalent in the case two nodes are equivalent,
        /// and CompareResult.Different otherwise.
        /// </returns>
        public static TreeCompareResult CompareLogical(
            Object firstTree,
            Object secondTree,
            Dictionary<string, PropertyToIgnore> propertiesToIgnore)
        {
            if (propertiesToIgnore == null)
            {
                throw new ArgumentNullException("propertiesToIgnore", "Argument must be a non-null Dictionary.");
            }

            TreeCompareResult result = new TreeCompareResult();

            result.Result = CompareResult.Equivalent;

            // Validate parameters, both objects are null
            if (null == firstTree && null == secondTree)
            {
                return result;
            }

            result.Result = CompareResult.Different;

            // Validate parameters, only one object is null
            if (null == firstTree || null == secondTree)
            {
                return result;
            }

            // Compare the types 
            if (!firstTree.GetType().Equals(secondTree.GetType()))
            {
                SendCompareMessage("Two nodes have different types: '" + firstTree.GetType().FullName + "' vs. '" + secondTree.GetType().FullName + "'.");
                Break();
                return result;
            }

            bool same = false;
            lock (_objectsInTree)
            {
                // Create hashtables that will contain objects in the trees.
                // This is used to break loops.
                _objectsInTree[0] = new List<int>();
                _objectsInTree[1] = new List<int>();

                _skipProperties = propertiesToIgnore;

                // Include default skip properties if necessary.
                if (_skipProperties != _skipPropertiesDefault)
                {
                    _MergeDictionaries(_skipProperties, _skipPropertiesDefault);
                }

                try
                {
                    // cast roots to LogicalTreeNodes
                    DependencyObject firstLogicalTreeNode = firstTree as DependencyObject;
                    DependencyObject secondLogicalTreeNode = secondTree as DependencyObject;

                    //Both are not logical tree node
                    if ((null == firstLogicalTreeNode) && (null == secondLogicalTreeNode))
                    {
                        same = CompareObjects(firstTree, secondTree);
                    }
                    else
                    {
                        _objectsInTree[0].Add(firstTree.GetHashCode());
                        _objectsInTree[1].Add(secondTree.GetHashCode());

                        same = CompareLogicalTree(firstLogicalTreeNode, secondLogicalTreeNode);
                    }
                }
                finally
                {
                    _objectsInTree[0] = null;
                    _objectsInTree[1] = null;
                    _skipProperties = null;
                }
            }

            // Two trees are equivalent
            if (same)
            {
                result.Result = CompareResult.Equivalent;
            }

            return result;
        }
        #endregion Public Method

        #region CompareLogicalTree
        /// <summary>
        /// Recursively compare two object trees following logical tree structure
        /// </summary>
        /// <param name="firstTree">Root for first tree</param>
        /// <param name="secondTree">Root for second tree</param>
        /// <returns>
        ///   True, if two object tree are equivalent
        ///   False, otherwise
        /// </returns>
        private static bool CompareLogicalTree(
            object firstTree,
            object secondTree
            )
        {
            // cast roots to LogicalTreeNodes
            DependencyObject firstLogicalTreeNode = firstTree as DependencyObject;
            DependencyObject secondLogicalTreeNode = secondTree as DependencyObject;

            //Both are not logical tree node
            if ((null == firstLogicalTreeNode) && (null == secondLogicalTreeNode))
            {
                return CompareObjects(firstTree, secondTree);
            }

            // The first is null 
            if (null == firstLogicalTreeNode)
            {
                SendCompareMessage("Nodes are different: 'null' vs. '" + secondTree.GetType().FullName + "'.");
                Break();
                return false;
            }

            // The second is null
            if (null == secondLogicalTreeNode)
            {
                SendCompareMessage("Nodes are different: '" + firstTree.GetType().FullName + "' vs. 'null'.");
                Break();
                return false;
            }

            string nodeName = firstTree.GetType().FullName;

            // Compare children collection recursively

            if (!CompareChildren(firstLogicalTreeNode, secondLogicalTreeNode))
            {
                SendCompareMessage("Not all the children are the same for '" + nodeName + "'.");
                Break();
                return false;
            }

            //Compare Events ( we skip this because PresentationSource.SourceChanged)
            //  if (!CompareTreeEvents(firstTree, secondTree))
            //  {
            //      SendCompareMessage("Not all the events are the same for " + nodeName + ".");
            //      return false;
            //  }

            //Compare properties
            if (!CompareObjectProperties(firstTree, secondTree))
            {
                SendCompareMessage("Not all the properties are the same for '" + nodeName + "'.");
                Break();
                return false;
            }

            // All are equivalent
            return true;
        }
        #endregion CompareLogicalTree

        private static int _MergeDictionaries(Dictionary<string, PropertyToIgnore> dictionary1, Dictionary<string, PropertyToIgnore> dictionary2)
        {
            int cnt = 0;

            foreach (string propName in dictionary2.Keys)
            {
                if (!dictionary1.ContainsKey(propName))
                {
                    dictionary1.Add(propName, dictionary2[propName]);
                    cnt++;
                }
            }

            return cnt;
        }

        #region Compare Children
        private static bool CompareChildren(DependencyObject firstLogicalTreeNode, DependencyObject secondLogicalTreeNode)
        {
            IEnumerator childEnumerator1 = LogicalTreeHelper.GetChildren(firstLogicalTreeNode).GetEnumerator();
            IEnumerator childEnumerator2 = LogicalTreeHelper.GetChildren(secondLogicalTreeNode).GetEnumerator();

            // Both nodes have no child, skip
            if ((null == childEnumerator1) && (null == childEnumerator2))
            {
                return true;
            }

            // Only one node has children, the other doesn't
            if ((null == childEnumerator1) ^ (null == childEnumerator2))
            {
                SendCompareMessage("Child collection of the " + (null == childEnumerator1 ? "first" : "second") + " node is null while the child collection of the other node is not.");
                Break();
                return false;
            }

            // Find each child for first node in the second node's children collection
            while (childEnumerator1.MoveNext())
            {
                if (!childEnumerator2.MoveNext())
                {
                    SendCompareMessage("One of the first object's children '" + childEnumerator1.Current.ToString() + "' doesn't exist in the second child collection.");
                    Break();
                    return false;
                }

                if (!CompareLogicalTree(childEnumerator1.Current, childEnumerator2.Current))
                {
                    Break();
                    return false;
                }
            }

            // Second node has more children
            if (childEnumerator2.MoveNext())
            {
                SendCompareMessage("The second object has more children: ");
                do
                {
                    // Show their name one by one
                    SendCompareMessage("\n" + childEnumerator2.Current.ToString());
                }
                while (childEnumerator2.MoveNext());
                Break();

                return false;
            }

            return true;
        }
        #endregion Compare Children

        #region Compare Events

        /// <summary>
        /// Compare events for two nodes. If all the events  
        /// are the same, return true, otherwise, return false.
        /// Todo: right now, only compare the name. Should compare event handlers
        ///       as well. Anyway, delegates are not available yet
        /// </summary>
        /// <param name="firstNode">The first node</param>
        /// <param name="secondNode">The second node</param>
        private static bool CompareTreeEvents(
            Object firstNode,
            Object secondNode)
        {
            //Both are not IInputElement
            if (!(firstNode is IInputElement) && !(secondNode is IInputElement))
            {
                return true;
            }
            // Only one of the nodes is an IInputElement
            if (!(firstNode is IInputElement) || !(secondNode is IInputElement))
            {
                SendCompareMessage("Only one of the nodes is an IInputElement.");
                Break();
                return false;
            }


            RoutedEvent[] firstAttachedIDs = null; // ((IInputElement)(firstNode)).GetRoutedEventsWithHandlers();
            RoutedEvent[] secondAttachedIDs = null; // ((IInputElement)(secondNode)).GetRoutedEventsWithHandlers();

            //Both are null
            if (null == firstAttachedIDs && null == secondAttachedIDs)
                return true;

            //Only the second one is null
            if (null == secondAttachedIDs)
            {
                SendCompareMessage("The first object has events, the second one doesn't.");
                Break();
                return false;
            }

            //Only the first one is null
            if (null == firstAttachedIDs)
            {
                SendCompareMessage("The second object has events, the first one doesn't.");
                Break();
                return false;
            }


            //Compare the numbers of events
            if (firstAttachedIDs.Length != secondAttachedIDs.Length)
            {
                SendCompareMessage("The first node and second node have different numbers of events: "
                    + firstAttachedIDs.Length.ToString()
                                   + " vs. " + secondAttachedIDs.Length.ToString() + ".");
                Break();
                return false;
            }
            foreach (RoutedEvent rID in firstAttachedIDs)
            {
                if (!FindSameEvent(rID, firstNode, secondAttachedIDs, secondNode))
                {
                    SendCompareMessage("Cannot find the event with name: "
                        + rID.Name + " for the second node");
                    Break();
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Compare event. For a RoutedEventID, find whether the quivalent event exist in the
        /// Array of RoutedEventIDs. If one is find there, return true.
        /// Otherwise, return false
        /// </summary>
        /// <param name="toFind">The RoutedEventID event to find</param>
        /// <param name="owner1">owner for the event to find</param>
        /// <param name="findFrom">The RoutedEventID array to search from</param>
        /// <param name="owner2">The owner for the RoutedEventID array to search from</param>
        private static bool FindSameEvent(
            RoutedEvent toFind,
            object owner1,
            RoutedEvent[] findFrom,
            object owner2)
        {
            //todo: right now, only judge whether two objects have the same RoutedEventID collection
            //The event handlerswill be compared in the future. 
            String eventName1 = GetEventName(owner1, toFind);
            for (int i = 0; i < findFrom.Length; i++)
            {
                if (0 == GetEventName(owner2, (RoutedEvent)findFrom[i]).CompareTo(eventName1))
                {
                    if (CompareEventValue(toFind, owner1, findFrom[i], owner2))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
        ///todo: to compare the event handlers
        private static bool CompareEventValue(
            RoutedEvent toFind,
            Object owner1,
            RoutedEvent findFrom,
            Object owner2)
        {
            return true;
        }

        /// <summary>
        /// Get event name
        /// </summary>
        /// <param name="owner">owner</param>
        /// <param name="routedEventID">RoutedEventID</param>
        /// <returns>Name of Event</returns>
        private static String GetEventName(
            object owner,
            RoutedEvent routedEventID)
        {
            if (routedEventID.OwnerType.IsInstanceOfType(owner))
            {
                return routedEventID.Name;
            }
            else
                return routedEventID.OwnerType.AssemblyQualifiedName + "_" + routedEventID.Name;
        }

        #endregion Compare Events

        #region compare properties

        /// <summary>
        /// Compare Properties for two nodes. If all the properties for these two
        /// nodes have the same value, return true. Otherwise, return false.
        ///
        /// </summary>
        /// <param name="firstNode">The first node</param>
        /// <param name="secondNode">The second node</param>
        private static bool CompareObjectProperties(object firstNode, object secondNode)
        {
            //
            // Compare CLR properties.
            //
            Dictionary<string, PropertyDescriptor> clrProperties1 = GetClrProperties(firstNode);
            Dictionary<string, PropertyDescriptor> clrProperties2 = GetClrProperties(secondNode);

            if (!CompareClrPropertyCollection(firstNode, clrProperties1, secondNode, clrProperties2))
            {
                SendCompareMessage("The first node and the second node are different in one or more CLR properties.");
                Break();
                return false;
            }

            //
            // Compare local DPs.
            //
            if (firstNode is DependencyObject)
            {
                DependencyObject do1 = (DependencyObject)firstNode;
                DependencyObject do2 = (DependencyObject)secondNode;
                Dictionary<string, DependencyProperty> dependencyProperties1 = GetLocallySetDependencyProperties(do1);
                Dictionary<string, DependencyProperty> dependencyProperties2 = GetLocallySetDependencyProperties(do2);

                if (!CompareDependencyPropertyCollection(do1, dependencyProperties1, do2, dependencyProperties2))
                {
                    SendCompareMessage("The first node and the second node are different in one or more locally-set dependency properties.");
                    Break();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Compare a collection of DPs.
        /// </summary>
        /// <returns>
        /// true, if all properties are equivalent
        /// false, otherwise
        /// </returns>
        private static bool CompareDependencyPropertyCollection(
            DependencyObject firstNode,
            Dictionary<string, DependencyProperty> properties1,
            DependencyObject secondNode,
            Dictionary<string, DependencyProperty> properties2)
        {
            Dictionary<string, DependencyProperty> propertyGroup = null;

            IEnumerator<string> ie1 = properties1.Keys.GetEnumerator();

            while (ie1.MoveNext())
            {
                string propertyName = ie1.Current;

                // If property was in skip collection, ignore it
                if (!ShouldIgnoreProperty(propertyName, firstNode, IgnoreProperty.IgnoreValueOnly))
                {
                    // In the case dependency property is not locally set for second node, 
                    // get the property in the properties1 to compare.  In most cases, the 
                    // dependency properties are static.
                    propertyGroup = properties2.ContainsKey(propertyName) ? properties2 : properties1;

                    // Compare properties
                    if (!CompareDependencyProperty(
                            firstNode,
                            properties1[propertyName],
                            secondNode,
                            propertyGroup[propertyName]))
                    {
                        SendCompareMessage("Value of property '" + propertyName + "' is different.");
                        Break();
                        return false;
                    }
                }

                properties2.Remove(propertyName);
            }

            // Return true if there is not more properties for second node
            if (0 == properties2.Count)
                return true;

            // If second node has more locally set DPs, compare those with that in
            // the first node as well
            IEnumerator<string> ie2 = properties2.Keys.GetEnumerator();
            while (ie2.MoveNext())
            {
                string propertyName = ie2.Current;

                // If property was in skip collection, ignore it 
                if (!ShouldIgnoreProperty(propertyName, secondNode, IgnoreProperty.IgnoreValueOnly))
                {
                    if (!properties1.ContainsKey(propertyName))
                    {
                        SendCompareMessage("Property '" + propertyName + "' is not found for the first tree.");
                        return false;
                    }
                    if (!CompareDependencyProperty(
                            firstNode,
                            properties1[propertyName],
                            secondNode,
                            properties2[propertyName]))
                    {

                        SendCompareMessage("Value of property '" + propertyName + "' is different.");
                        Break();
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Compare a collection of clr properties.
        /// </summary>
        /// <returns>
        /// true, if all properties are equivalent
        /// false, otherwise
        /// </returns>
        private static bool CompareClrPropertyCollection(
            Object firstNode,
            Dictionary<string, PropertyDescriptor> properties1,
            Object secondNode,
            Dictionary<string, PropertyDescriptor> properties2)
        {
            IEnumerator<string> ie1 = properties1.Keys.GetEnumerator();

            while (ie1.MoveNext())
            {
                string propertyName = ie1.Current;

                // Check that the second tree contains the property.
                if (!properties2.ContainsKey(propertyName))
                {
                    SendCompareMessage("Property '" + propertyName + "' is not in second tree.");
                    Break();
                    return false;
                }

                // If property was in skip collection, ignore it
                if (!ShouldIgnoreProperty(propertyName, firstNode, IgnoreProperty.IgnoreValueOnly))
                {

                    // Compare properties
                    if (!CompareClrProperty(
                            firstNode,
                            properties1[propertyName],
                            secondNode,
                            properties2[propertyName]))
                    {
                        SendCompareMessage("Value of property '" + propertyName + "' is different.");
                        Break();
                        return false;
                    }
                }

                properties2.Remove(propertyName);
            }

            // Check that the second tree doesn't have more properties than the first tree.
            if (properties2.Count > 0)
            {
                IEnumerator<string> ie2 = properties2.Keys.GetEnumerator();
                ie2.MoveNext();

                SendCompareMessage("Property '" + properties2[ie2.Current].Name + "' is not in first tree.");
                Break();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get DPs
        /// </summary>
        /// <param name="owner">owner</param>
        private static Dictionary<string, DependencyProperty> GetLocallySetDependencyProperties(DependencyObject owner)
        {
            Dictionary<string, DependencyProperty> dependencyProperties = new Dictionary<string, DependencyProperty>();

            LocalValueEnumerator localValues = owner.GetLocalValueEnumerator();
            while (localValues.MoveNext())
            {
                DependencyProperty dp = (DependencyProperty)localValues.Current.Property;

                // skip properties
                if (ShouldIgnoreProperty(dp.Name, owner, IgnoreProperty.IgnoreNameAndValue))
                    continue;

                dependencyProperties.Add(dp.Name, dp);
            }

            return dependencyProperties;
        }

        /// <summary>
        /// Get clr properties
        /// </summary>
        /// <param name="owner">owner</param>
        private static Dictionary<string, PropertyDescriptor> GetClrProperties(object owner)
        {
            Dictionary<string, PropertyDescriptor> clrProperties = new Dictionary<string, PropertyDescriptor>();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(owner);

            foreach (PropertyDescriptor property in properties)
            {
                // skip properties
                if (ShouldIgnoreProperty(property.Name, owner, IgnoreProperty.IgnoreNameAndValue))
                    continue;

                clrProperties.Add(property.Name, property);
            }

            return clrProperties;
        }

        /// <summary>
        ///  Shoud ignore this property?
        /// </summary>
        /// <param name="propertyName">property name</param>
        /// <param name="owner">owner</param>
        /// <param name="whatToIgnore">Valueonly or Value and name to ignore?</param>
        /// <returns></returns>
        private static bool ShouldIgnoreProperty(string propertyName, object owner, IgnoreProperty whatToIgnore)
        {
            PropertyToIgnore property = null;
            foreach (string key in _skipProperties.Keys)
            {
                if (String.Equals(key, propertyName, StringComparison.InvariantCulture)
                    || key.StartsWith(propertyName + "___owner___"))
                {
                    property = _skipProperties[key];
                    if (whatToIgnore == property.WhatToIgnore && ((null == property.Owner) || _DoesTypeMatch(owner.GetType(), property.Owner)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool _DoesTypeMatch(Type ownerType, string typeName)
        {
            Type type = ownerType;
            bool isMatch = false;

            while (type != null && !isMatch)
            {
                if (0 == String.Compare(type.Name, typeName))
                {
                    isMatch = true;
                }

                type = type.BaseType;
            }

            return isMatch;
        }

        /// <summary>
        /// Read properties to skip from PropertiesToSkip.xml. If this file
        /// exists under the current working directory, use the one there. 
        /// Otherwise, use the file built in the ClientTestLibrary Assembly.
        /// </summary>
        /// <returns>Hashtable containing properties should be skiped</returns>
        private static Dictionary<string, PropertyToIgnore> ReadSkipProperties()
        {
            // File name for the properties to skip.
            return ReadSkipProperties("PropertiesToSkip.xml");
        }

        /// <summary>
        /// Read properties to skip from PropertiesToSkip.xml. If this file
        /// exists under the current working directory, use the one there. 
        /// Otherwise, use the file built in the ClientTestLibrary Assembly.
        /// </summary>
        /// <param name="fileName">Name of config file for specifying properties.</param>
        /// <returns>Hashtable containing properties should be skiped</returns>
        public static Dictionary<string, PropertyToIgnore> ReadSkipProperties(string fileName)
        {
            Dictionary<string, PropertyToIgnore> PropertiesToSkip = new Dictionary<string, PropertyToIgnore>();

            //
            // Load PropertiesToSkip.xml document from assembly resources.
            //
            XmlDocument doc = new XmlDocument();
            Stream xmlFileStream = null;
            if (File.Exists(fileName))
            {
                SendCompareMessage("Opening '" + fileName + "' from the current directory.");
                xmlFileStream = File.OpenRead(fileName);
            }
            else
            {
                SendCompareMessage("Opening '" + fileName + "' from the Assembly.");
                Assembly asm = Assembly.GetAssembly(typeof(TreeComparer));
                xmlFileStream = asm.GetManifestResourceStream(fileName);

                if (xmlFileStream == null)
                {
                    throw new ArgumentException("The file '" + fileName + "' cannot be loaded.", "fileName");
                }
            }

            try
            {
                StreamReader reader = new StreamReader(xmlFileStream);
                doc.LoadXml(reader.ReadToEnd());
            }
            finally
            {
                xmlFileStream.Close();
            }

            //
            // Store properties to skip in collection.
            //
            XmlNodeList properties = doc.GetElementsByTagName("PropertyToSkip");

            foreach (XmlNode property in properties)
            {
                string propertyName = GetAttributeValue(property, "PropertyName");
                string ignore = GetAttributeValue(property, "Ignore");
                string owner = GetAttributeValue(property, "Owner");

                IgnoreProperty whatToIgnore;

                if (null == ignore || 0 == String.Compare(ignore, "ValueOnly"))
                {
                    whatToIgnore = IgnoreProperty.IgnoreValueOnly;
                }
                else if (0 == String.Compare(ignore, "NameAndValue"))
                {
                    whatToIgnore = IgnoreProperty.IgnoreNameAndValue;
                }
                else
                {
                    throw new Exception("'Ignore' attribute value not recognized: " + ignore);
                }

                PropertyToIgnore newItem = new PropertyToIgnore();

                newItem.WhatToIgnore = whatToIgnore;

                if (!String.IsNullOrEmpty(owner))
                {
                    newItem.Owner = owner;
                }

                try
                {
                    if (PropertiesToSkip.ContainsKey(propertyName))
                    {
                        SendCompareMessage(propertyName);
                    }
                    PropertiesToSkip.Add(propertyName + "___owner___" + owner, newItem);

                }
                catch (Exception ex)
                {
                    SendCompareMessage(ex.Message);
                }
            }

            return PropertiesToSkip;
        }

        private static string GetAttributeValue(
            XmlNode node,
            string attributeName)
        {
            XmlAttributeCollection attributes = node.Attributes;
            XmlAttribute attribute = attributes[attributeName];

            if (null == attribute)
            {
                return null;
            }

            return attribute.Value;
        }

        // Checks if GetValue may be called on the given PropertyDescriptor.
        private static bool IsReadablePropertyDescriptor(PropertyDescriptor property)
        {
            return !(property.ComponentType is System.Reflection.MemberInfo)
                   || !IsGenericTypeMember(property.ComponentType, property.Name);
        }

        // Checks if the given type member is a generic-only member on a non-generic type.
        private static bool IsGenericTypeMember(Type type, string memberName)
        {
            return !type.IsGenericType
                    && (memberName == "GenericParameterPosition"
                    || memberName == "GenericParameterAttributes"
                    || memberName == "GetGenericArguments"
                    || memberName == "GetGenericParameterConstraints"
                    || memberName == "GetGenericTypeDefinition"
                    || memberName == "IsGenericTypeDefinition"
                    || memberName == "DeclaringMethod");
        }

        /// <summary>
        /// Compare two clr properties.
        /// </summary>
        /// <returns>
        /// true, if they are the same
        /// false, otherwise
        /// </returns>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Assert, Name = "FullTrust")]
        private static bool CompareClrProperty(
                object owner1,
                PropertyDescriptor property1,
                object owner2,
                PropertyDescriptor property2)
        {
            //both are simple property, convert them into string and compare
            object obj1;
            object obj2;
            //Show property to be compared.
            //SendCompareMessage("Compare Clr property '" + property1.Name + " owner: " + owner1.GetType().Name);

            try
            {

                if (IsReadablePropertyDescriptor(property1))
                {

                    obj1 = property1.GetValue(owner1);
                    obj2 = property2.GetValue(owner2);

                    bool same = CompareObjects(obj1, obj2);

                    if (!same)
                    {
                        SendCompareMessage("Clr property '" + property1.Name + "' is different.");
                        Break();

                    }

                    return same;
                }
                else
                {
                    return true;
                }
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                if (!(e.InnerException is NotSupportedException))
                {
                    throw;
                }
            }

            return true;

        }

        /// <summary>
        /// Compare two dependency properties
        /// </summary>
        /// <returns>
        /// true, if they are the same
        /// false, otherwise
        /// </returns>
        private static bool CompareDependencyProperty(
            DependencyObject owner1,
            DependencyProperty dp1,
            DependencyObject owner2,
            DependencyProperty dp2)
        {
            //Show property to be compared.
            //SendCompareMessage("compare DependencyProperty : " + dp1.ToString());

            object obj1 = owner1.GetValue(dp1);
            object obj2 = owner2.GetValue(dp2);

            bool same = CompareObjects(obj1, obj2);

            if (!same)
            {
                SendCompareMessage("Value of DependencyProperty '" + dp1.ToString() + "' is different.");
                Break();
            }

            return same;
        }
        /*
                /// <summary>
                /// Return value indicates whether this property has an animation associated. 
                /// </summary>
                /// <param name="owner">Owner</param>
                /// <param name="property">Property</param>
                /// <returns>Has animation</returns>

                static bool HasAnimation(object owner, DependencyProperty property)
                {
                    PropertyAnimationClockCollection clocks = null;
            
                    if(owner is UIElement)
                    {
                        if (((UIElement)owner).HasAnimatedProperties)
                            clocks = ((UIElement)owner).GetAnimationClocks(property);
                    }
                    else if (owner is ContentElement)
                    {
                        if (((ContentElement)owner).HasAnimatedProperties)
                            clocks = ((ContentElement)owner).GetAnimationClocks(property);
                    }
                    else
                    {
                        return false;
                    }

                    if (clocks != null && clocks.Count > 0)
                        return clocks[0].Timeline != null;
                    else
                        return false;
                }
        */
        /// <summary>
        /// Compare the value for of a property of LogicalTreeNode.
        /// If the value are not of the same type, return false.
        /// if the value can be convert to string and the result is not
        /// the same just return false.
        /// For logical tree nodes, call CompareLogicalTree to compare recursively.
        /// Otherwise, use CompareAsGenericObject
        /// to compare
        /// </summary>
        /// <param name="obj1">The first value</param>
        /// <param name="obj2">The second value</param>
        /// <returns>
        /// true, if value is regarded as the same
        /// false, otherwise use CompareAsGenericObject to compare
        /// </returns>
        private static bool CompareObjects(object obj1, object obj2)
        {
            bool same = false;

            //Both are null
            if (null == obj1 && null == obj2)
                return true;

            //Only one of them is null
            if (null == obj1)
            {

                SendCompareMessage("Values is different: 'null' vs. '" + obj2.ToString() + "'.");
                Break();
                return false;
            }

            if (null == obj2)
            {

                SendCompareMessage("Values are different: '" + obj1.ToString() + "' vs. 'null'.");
                Break();
                return false;
            }

            //Compare Type
            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();

            if (!type1.Equals(type2))
            {

                SendCompareMessage("Type of value is different: '" + type1.FullName + "' vs. '" + type2.FullName + "'.");
                Break();
                return false;
            }

            //They are value type
            if (type1.IsValueType)
            {
                same = CompareValueType(obj1, obj2);
                return same;
            }

            if (_objectsInTree[0].Contains(obj1.GetHashCode()) || _objectsInTree[1].Contains(obj2.GetHashCode()))
                return true;

            _objectsInTree[0].Add(obj1.GetHashCode());
            _objectsInTree[1].Add(obj2.GetHashCode());

            //They are logical node
            if (obj1 is DependencyObject)
            {
                if ((obj1 is ICollection) && (obj2 is ICollection))
                {
                    same = ComparePropertyAsICollection(obj1, obj2);

                }
                else
                {
                    same = CompareLogicalTree(obj1, obj2);
                }
            }
            else
            {

                //Compare generic objects
                same = CompareGenericObject(obj1, obj2);
            }

            return same;
        }

        /// <summary>
        /// Compare two value types
        /// </summary>
        /// <param name="obj1">The first value</param>
        /// <param name="obj2">The second value</param>
        /// <returns>
        /// true, if they are the same
        /// false, otherwise
        /// </returns>
        private static bool CompareValueType(object obj1, object obj2)
        {
            bool same = false;
            double ErrorAllowed = 0.000001;

            // for double or float comparison, certain error should be allowed. 
            if (obj1 is double)
            {
                double double1 = (double)obj1;
                double double2 = (double)obj2;
                if ((obj1.Equals(double.NaN) && obj2.Equals(double.NaN))
                    || (double.IsInfinity(double1) && double.IsInfinity(double2))
                )
                {
                    return true;
                }

                same = Math.Abs(double2) > ErrorAllowed ?
                    (double1 / double2) > (1 - ErrorAllowed) && (double1 / double2) < (1 + ErrorAllowed) :
                    Math.Abs(double1 - double2) < ErrorAllowed;
            }
            else if (obj1 is float)
            {
                float float1 = (float)obj1;
                float float2 = (float)obj2;
                if ((obj1.Equals(float.NaN) && obj2.Equals(float.NaN))
                    || (float.IsInfinity(float1) && float.IsInfinity(float2))
                )
                {
                    return true;
                }

                same = Math.Abs(float2) > ErrorAllowed ?
                    (float1 / float2) > (1 - ErrorAllowed) && (float1 / float2) < (1 + ErrorAllowed) :
                    Math.Abs(float1 - float2) < ErrorAllowed;
            }
            else
            {
                same = obj1.Equals(obj2);
            }

            if (!same)
            {
                SendCompareMessage("Values are different: '" + obj1.ToString() + "' vs. '" + obj2.ToString() + "'.");
                Break();
            }

            return same;
        }

        // For a generic object value, just compare their properties.
        private static bool CompareGenericObject(object obj1, object obj2)
        {
            //Compare properties
            if (!CompareObjectProperties(obj1, obj2))
            {
                SendCompareMessage("Not all the properties are the same for object '" + obj1.GetType().ToString() + "'.");
                Break();
                return false;
            }

            return true;
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
        /// Compare collections of properties
        /// </summary>
        /// <param name="properties1">The first property that is collection</param>
        /// <param name="properties2">The second property that is collection</param>
        /// <returns>
        /// true, if they are the same
        /// false, otherwise
        /// </returns>
        private static bool ComparePropertyAsICollection(
            object properties1,
            object properties2)
        {
            ICollection preopertyCollection1 = properties1 as ICollection;
            ICollection preopertyCollection2 = properties2 as ICollection;
            if (preopertyCollection1.Count != preopertyCollection2.Count)
            {

                SendCompareMessage("Property Collections have different counts: " + preopertyCollection1.Count.ToString() + " vs. " + preopertyCollection2.Count.ToString() + ".");
                Break();
                return false;
            }
            object[] propertyArray1 = new object[preopertyCollection1.Count];
            object[] propertyArray2 = new object[preopertyCollection2.Count];

            for (int i = 0; i < preopertyCollection1.Count; i++)
            {
                if (!CompareLogicalTree(propertyArray1[i], propertyArray2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion Compare properties

        #region SendCompareMessage

        /// <summary>
        /// Logging
        /// </summary>
        private static void SendCompareMessage(string message)
        {
            // Log to console temporarily
            GlobalLog.LogStatus(message);
        }


        #endregion SendCompareMessage

        private static Dictionary<string, PropertyToIgnore> _skipProperties = null;
        private static Dictionary<string, PropertyToIgnore> _skipPropertiesDefault = ReadSkipProperties();
        private static List<int>[] _objectsInTree = new List<int>[2];
    }

    /// <summary>
    /// 
    /// </summary>
    public class PropertyToIgnore
    {
        /// <summary>
        /// 
        /// </summary>
        public PropertyToIgnore()
        {
            _owner = null;
        }
        /// <summary>
        /// 
        /// </summary>
        public string Owner
        {
            set
            {
                _owner = value;
            }
            get
            {
                return _owner;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public IgnoreProperty WhatToIgnore
        {
            set
            {
                _whatToIgnore = value;
            }
            get
            {
                return _whatToIgnore;
            }
        }

        private IgnoreProperty _whatToIgnore;
        private string _owner;
    }

    /// <summary>
    /// 
    /// </summary>
    public enum IgnoreProperty
    {
        /// <summary>
        /// 
        /// </summary>
        IgnoreValueOnly,
        /// <summary>
        /// 
        /// </summary>
        IgnoreNameAndValue
    };

    #region TreeCompareResult struct
    /// <summary>
    /// Struct containing result.
    /// </summary>
    public struct TreeCompareResult
    {
        /// <summary>
        /// Enum TreeCompareResult representing the compared result
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
    #endregion TreeCompareResult struct
}
