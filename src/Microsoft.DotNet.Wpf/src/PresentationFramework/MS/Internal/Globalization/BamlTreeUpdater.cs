// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using System.Xml;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Markup.Localizer;

namespace MS.Internal.Globalization
{
    internal static class BamlTreeUpdater
    {
        //-----------------------------
        // internal methods
        //-----------------------------

        internal static void UpdateTree(
            BamlTree tree,
            BamlTreeMap treeMap,
            BamlLocalizationDictionary dictionary
            )
        {
            Debug.Assert(tree != null && tree.Root != null, "Empty Tree!");
            Debug.Assert(treeMap != null, "Empty map!");
            Debug.Assert(dictionary != null, "Empty dictionary");

            // no changes to do to the tree.
            if (dictionary.Count <= 0)
                return;

            // create a tree map to be used for update
            BamlTreeUpdateMap updateMap = new BamlTreeUpdateMap(treeMap, tree);

            //
            // a) Create baml tree nodes for missing child place holders and properties.
            //    Translations may require new nodes to be constructed. For example
            //    translation contains new child place holders
            //
            CreateMissingBamlTreeNode(dictionary, updateMap);


            // 
            // b) Look through each translation and make modification to the tree
            //    At this step, new nodes are linked to the tree if applicable.
            //            
            BamlLocalizationDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            ArrayList deferredResources = new ArrayList();
            while (enumerator.MoveNext())
            {
                if (!ApplyChangeToBamlTree(enumerator.Key, enumerator.Value, updateMap))
                {
                    deferredResources.Add(enumerator.Entry);
                }
            }

            //
            // c) Hook up the property nodes that aren't hooked up yet            
            //    Formatting tags inserted in the translation will only be created the 
            //    previous step. Hook up properties to those nodes now if applicable
            //
            for (int i = 0; i < deferredResources.Count; i++)
            {
                DictionaryEntry entry = (DictionaryEntry)deferredResources[i];
                ApplyChangeToBamlTree(
                    (BamlLocalizableResourceKey)entry.Key,
                    (BamlLocalizableResource)entry.Value,
                    updateMap
                    );
            }
        }

        private static void CreateMissingBamlTreeNode(
            BamlLocalizationDictionary dictionary,
            BamlTreeUpdateMap treeMap
            )
        {
            BamlLocalizationDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                BamlLocalizableResourceKey key = enumerator.Key;
                BamlLocalizableResource resource = enumerator.Value;

                // get the baml tree node from the tree
                BamlTreeNode node = treeMap.MapKeyToBamlTreeNode(key);

                if (node == null)
                {
                    if (key.PropertyName == BamlConst.ContentSuffix)
                    {
                        // see if there is already a Baml node with the Uid. If so
                        // ignore this entry
                        node = treeMap.MapUidToBamlTreeElementNode(key.Uid);
                        if (node == null)
                        {
                            // create new Baml element node
                            BamlStartElementNode newNode = new BamlStartElementNode(
                                treeMap.Resolver.ResolveAssemblyFromClass(key.ClassName),
                                key.ClassName,
                                false, /*isInjected*/
                                false /*CreateUsingTypeConverter*/
                                );

                            // create new x:Uid node for this element node
                            newNode.AddChild(
                                new BamlDefAttributeNode(
                                    XamlReaderHelper.DefinitionUid,
                                    key.Uid
                                    )
                                );

                            TryAddContentPropertyToNewElement(treeMap, newNode);

                            // terminate the node with EndElementNode
                            newNode.AddChild(new BamlEndElementNode());

                            // store this new node into the map so that it can be found
                            // when other translations reference it as a childplace holder, or property owner
                            treeMap.AddBamlTreeNode(key.Uid, key, newNode);
                        }
                    }
                    else
                    {
                        BamlTreeNode newNode;
                        if (key.PropertyName == BamlConst.LiteralContentSuffix)
                        {
                            // create a LiterContent node
                            newNode = new BamlLiteralContentNode(resource.Content);
                        }
                        else
                        {
                            newNode = new BamlPropertyNode(
                                treeMap.Resolver.ResolveAssemblyFromClass(key.ClassName),
                                key.ClassName,
                                key.PropertyName,
                                resource.Content,
                                BamlAttributeUsage.Default
                                );
                        }

                        // add to the map
                        treeMap.AddBamlTreeNode(null, key, newNode);
                    }
                }
            }
        }

        private static bool ApplyChangeToBamlTree(
            BamlLocalizableResourceKey key,
            BamlLocalizableResource resource,
            BamlTreeUpdateMap treeMap
            )
        {
            if (resource == null
                || resource.Content == null
                || !resource.Modifiable)
            {
                // Invalid translation or the resource is marked as non-modifiable.
                return true;
            }

            if (!treeMap.LocalizationDictionary.Contains(key)
                && !treeMap.IsNewBamlTreeNode(key))
            {
                // A localizable node is either in the localization dicationary extracted 
                // from the source or it is a new node created by the localizer. 
                // Otherwise, we cannot modify it.
                return true;
            }

            // get the node, at this point, all the missing nodes are created
            BamlTreeNode node = treeMap.MapKeyToBamlTreeNode(key);
            Invariant.Assert(node != null);

            // apply translations
            switch (node.NodeType)
            {
                case BamlNodeType.LiteralContent:
                    {
                        BamlLiteralContentNode literalNode = (BamlLiteralContentNode)node;

                        // set the content to the node.
                        literalNode.Content = BamlResourceContentUtil.UnescapeString(resource.Content);

                        // now try to link this node into the parent.
                        if (literalNode.Parent == null)
                        {
                            BamlTreeNode parent = treeMap.MapUidToBamlTreeElementNode(key.Uid);
                            if (parent != null)
                            {
                                // link it up with the parent
                                parent.AddChild(literalNode);
                            }
                            else
                            {
                                return false; // can't resolve the parent yet
                            }
                        }
                        break;
                    }
                case BamlNodeType.Property:
                    {
                        BamlPropertyNode propertyNode = (BamlPropertyNode)node;

                        // set the translation into the property 
                        propertyNode.Value = BamlResourceContentUtil.UnescapeString(resource.Content);

                        // now try to link this node into the parent
                        if (propertyNode.Parent == null)
                        {
                            BamlStartElementNode parent = (BamlStartElementNode)treeMap.MapUidToBamlTreeElementNode(key.Uid);
                            if (parent != null)
                            {
                                // insert property node to the parent
                                parent.InsertProperty(node);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case BamlNodeType.StartElement:
                    {
                        string source = null;
                        if (treeMap.LocalizationDictionary.Contains(key))
                        {
                            source = ((BamlLocalizableResource)treeMap.LocalizationDictionary[key]).Content;
                        }

                        if (resource.Content != source)
                        {
                            // only rearrange the value if source and update are different
                            ReArrangeChildren(key, node, resource.Content, treeMap);
                        }

                        break;
                    }
                default:
                    break;
            }


            return true;
        }

        private static void ReArrangeChildren(
            BamlLocalizableResourceKey key,
            BamlTreeNode node,
            string translation,
            BamlTreeUpdateMap treeMap
            )
        {
            //
            // Split the translation into a list of BamlNodes.
            //
            IList<BamlTreeNode> nodes = SplitXmlContent(
                key,
                translation,
                treeMap
                );

            // merge the nodes from translation with the source nodes
            MergeChildrenList(key, treeMap, node, nodes);
        }

        private static void MergeChildrenList(
            BamlLocalizableResourceKey key,
            BamlTreeUpdateMap treeMap,
            BamlTreeNode parent,
            IList<BamlTreeNode> newChildren
            )
        {
            if (newChildren == null) return;

            List<BamlTreeNode> oldChildren = parent.Children;

            int nodeIndex = 0;
            StringBuilder textBuffer = new StringBuilder();
            if (oldChildren != null)
            {
                Hashtable uidSubstitutions = new Hashtable(newChildren.Count);
                foreach (BamlTreeNode node in newChildren)
                {
                    if (node.NodeType == BamlNodeType.StartElement)
                    {
                        BamlStartElementNode element = (BamlStartElementNode)node;

                        // element's Uid can be null if it is a formatting tag.
                        if (element.Uid != null)
                        {
                            if (uidSubstitutions.ContainsKey(element.Uid))
                            {
                                treeMap.Resolver.RaiseErrorNotifyEvent(
                                    new BamlLocalizerErrorNotifyEventArgs(
                                        key,
                                        BamlLocalizerError.DuplicateElement
                                        )
                                    );
                                return; // the substitution contains duplicate elements.                                
                            }

                            uidSubstitutions[element.Uid] = null;  // stored in Hashtable
                        }
                    }
                }

                parent.Children = null; // start re-adding child element to parent               

                // The last node is EndStartElement node and must remain to be at the end,
                // so it won't be rearranged.                            
                for (int i = 0; i < oldChildren.Count - 1; i++)
                {
                    BamlTreeNode child = oldChildren[i];
                    switch (child.NodeType)
                    {
                        case BamlNodeType.StartElement:
                            {
                                BamlStartElementNode element = (BamlStartElementNode)child;

                                if (element.Uid != null)
                                {
                                    if (!uidSubstitutions.ContainsKey(element.Uid))
                                    {
                                        // cannot apply uid susbstitution because the susbstituition doesn't
                                        // contain all the existing uids. 
                                        parent.Children = oldChildren; // reset to old children and exit
                                        treeMap.Resolver.RaiseErrorNotifyEvent(
                                            new BamlLocalizerErrorNotifyEventArgs(
                                                key,
                                                BamlLocalizerError.MismatchedElements
                                                )
                                        );

                                        return;
                                    }

                                    // Each Uid can only appear once. 
                                    uidSubstitutions.Remove(element.Uid);
                                }

                                // Append all the contents till the matching element.
                                while (nodeIndex < newChildren.Count)
                                {
                                    BamlTreeNode newNode = newChildren[nodeIndex++];
                                    Invariant.Assert(newNode != null);

                                    if (newNode.NodeType == BamlNodeType.Text)
                                    {
                                        textBuffer.Append(((BamlTextNode)newNode).Content); // Collect all text into the buffer                                   
                                    }
                                    else
                                    {
                                        TryFlushTextToBamlNode(parent, textBuffer);
                                        parent.AddChild(newNode);

                                        if (newNode.NodeType == BamlNodeType.StartElement)
                                            break;
                                    }
                                }

                                break;
                            }
                        case BamlNodeType.Text:
                            {
                                // Skip original text node. New text node will be created from 
                                // text tokens in translation
                                break;
                            }
                        default:
                            {
                                parent.AddChild(child);
                                break;
                            }
                    }
                }
            }

            // finish the rest of the nodes
            for (; nodeIndex < newChildren.Count; nodeIndex++)
            {
                BamlTreeNode newNode = newChildren[nodeIndex];
                Invariant.Assert(newNode != null);

                if (newNode.NodeType == BamlNodeType.Text)
                {
                    textBuffer.Append(((BamlTextNode)newNode).Content); // Collect all text into the buffer                                   
                }
                else
                {
                    TryFlushTextToBamlNode(parent, textBuffer);
                    parent.AddChild(newNode);
                }
            }

            TryFlushTextToBamlNode(parent, textBuffer);

            // Always terminate the list with EndElementNode;
            parent.AddChild(new BamlEndElementNode());
        }

        private static void TryFlushTextToBamlNode(BamlTreeNode parent, StringBuilder textContent)
        {
            if (textContent.Length > 0)
            {
                BamlTreeNode textNode = new BamlTextNode(textContent.ToString());
                parent.AddChild(textNode);
                textContent.Length = 0;
            }
        }

        private static IList<BamlTreeNode> SplitXmlContent(
            BamlLocalizableResourceKey key,
            string content,
            BamlTreeUpdateMap bamlTreeMap
            )
        {
            // process each translation as a piece of xml content because of potential formatting tag inside
            StringBuilder xmlContent = new StringBuilder();
            xmlContent.Append("<ROOT>");
            xmlContent.Append(content);
            xmlContent.Append("</ROOT>");

            IList<BamlTreeNode> list = new List<BamlTreeNode>(4);
            XmlDocument doc = new XmlDocument();

            bool succeed = true;

            try
            {
                doc.LoadXml(xmlContent.ToString());
                XmlElement root = doc.FirstChild as XmlElement;
                if (root != null && root.HasChildNodes)
                {
                    for (int i = 0; i < root.ChildNodes.Count && succeed; i++)
                    {
                        succeed = GetBamlTreeNodeFromXmlNode(
                            key,
                            root.ChildNodes[i],
                            bamlTreeMap,
                            list
                            );
                    }
                }
            }
            catch (XmlException)
            {
                // The content can't be parse as Xml.
                bamlTreeMap.Resolver.RaiseErrorNotifyEvent(
                    new BamlLocalizerErrorNotifyEventArgs(
                        key,
                        BamlLocalizerError.SubstitutionAsPlaintext
                        )
                );

                // Apply the substitution as plain text                                    
                succeed = GetBamlTreeNodeFromText(
                    key,
                    content,
                    bamlTreeMap,
                    list
                    );
            }

            return (succeed ? list : null);
        }

        private static bool GetBamlTreeNodeFromXmlNode(
            BamlLocalizableResourceKey key,
            XmlNode node,                    // xml node to construct BamlTreeNode from
            BamlTreeUpdateMap bamlTreeMap,             // Baml tree update map
            IList<BamlTreeNode> newChildrenList          // list of new children
            )
        {
            if (node.NodeType == XmlNodeType.Text)
            {
                // construct a Text tree node from the xml content
                return GetBamlTreeNodeFromText(
                    key,
                    node.Value,
                    bamlTreeMap,
                    newChildrenList
                    );
            }
            else if (node.NodeType == XmlNodeType.Element)
            {
                XmlElement child = node as XmlElement;
                string className = bamlTreeMap.Resolver.ResolveFormattingTagToClass(child.Name);

                bool invalidResult = string.IsNullOrEmpty(className);

                string assemblyName = null;
                if (!invalidResult)
                {
                    assemblyName = bamlTreeMap.Resolver.ResolveAssemblyFromClass(className);
                    invalidResult = string.IsNullOrEmpty(assemblyName);
                }

                if (invalidResult)
                {
                    bamlTreeMap.Resolver.RaiseErrorNotifyEvent(
                        new BamlLocalizerErrorNotifyEventArgs(
                            key,
                            BamlLocalizerError.UnknownFormattingTag
                            )
                        );
                    return false;
                }

                // get the uid for this formatting tag
                string tagUid = null;
                if (child.HasAttributes)
                {
                    tagUid = child.GetAttribute(XamlReaderHelper.DefinitionUid);

                    if (!string.IsNullOrEmpty(tagUid))
                        tagUid = BamlResourceContentUtil.UnescapeString(tagUid);
                }


                BamlStartElementNode bamlNode = null;
                if (tagUid != null)
                {
                    bamlNode = bamlTreeMap.MapUidToBamlTreeElementNode(tagUid);
                }

                if (bamlNode == null)
                {
                    bamlNode = new BamlStartElementNode(
                        assemblyName,
                        className,
                        false, /*isInjected*/
                        false /*CreateUsingTypeConverter*/
                        );

                    if (tagUid != null)
                    {
                        // store the new node created                        
                        bamlTreeMap.AddBamlTreeNode(
                            tagUid,
                            new BamlLocalizableResourceKey(tagUid, className, BamlConst.ContentSuffix, assemblyName),
                            bamlNode
                            );

                        // Add the x:Uid node to the element
                        bamlNode.AddChild(
                            new BamlDefAttributeNode(
                                XamlReaderHelper.DefinitionUid,
                                tagUid
                                )
                        );
                    }

                    TryAddContentPropertyToNewElement(bamlTreeMap, bamlNode);

                    // terminate the child by a end element node
                    bamlNode.AddChild(new BamlEndElementNode());
                }
                else
                {
                    if (bamlNode.TypeFullName != className)
                    {
                        // This can happen if the localizer adds a new element with an id 
                        // that is also been added to the newer version of source baml
                        bamlTreeMap.Resolver.RaiseErrorNotifyEvent(
                            new BamlLocalizerErrorNotifyEventArgs(
                                key,
                                BamlLocalizerError.DuplicateUid
                                )
                            );
                        return false;
                    }
                }

                newChildrenList.Add(bamlNode);

                bool succeed = true;
                if (child.HasChildNodes)
                {
                    // recursively go down 
                    IList<BamlTreeNode> list = new List<BamlTreeNode>();
                    for (int i = 0; i < child.ChildNodes.Count && succeed; i++)
                    {
                        succeed = GetBamlTreeNodeFromXmlNode(
                            key,
                            child.ChildNodes[i],
                            bamlTreeMap,
                            list
                            );
                    }

                    if (succeed)
                    {
                        // merging the formatting translation with exisiting nodes. 
                        // formatting translation doesn't contain properties.
                        MergeChildrenList(key, bamlTreeMap, bamlNode, list);
                    }
                }

                return succeed;
            }

            return true; // other than text and element nodes
        }

        private static bool GetBamlTreeNodeFromText(
            BamlLocalizableResourceKey key,
            string content,                 // xml node to construct BamlTreeNode from
            BamlTreeUpdateMap bamlTreeMap,
            IList<BamlTreeNode> newChildrenList          // list of new children
            )
        {
            BamlStringToken[] tokens = BamlResourceContentUtil.ParseChildPlaceholder(content);

            if (tokens == null)
            {
                bamlTreeMap.Resolver.RaiseErrorNotifyEvent(
                    new BamlLocalizerErrorNotifyEventArgs(
                        key,
                        BamlLocalizerError.IncompleteElementPlaceholder
                        )
                    );
                return false;
            }

            bool succeed = true;
            for (int i = 0; i < tokens.Length; i++)
            {
                switch (tokens[i].Type)
                {
                    case BamlStringToken.TokenType.Text:
                        {
                            BamlTreeNode node = new BamlTextNode(tokens[i].Value);
                            newChildrenList.Add(node);
                            break;
                        }
                    case BamlStringToken.TokenType.ChildPlaceHolder:
                        {
                            BamlTreeNode node = bamlTreeMap.MapUidToBamlTreeElementNode(tokens[i].Value);

                            // The value will be null if there is no uid-matching node in the tree.                        
                            if (node != null)
                            {
                                newChildrenList.Add(node);
                            }
                            else
                            {
                                bamlTreeMap.Resolver.RaiseErrorNotifyEvent(
                                    new BamlLocalizerErrorNotifyEventArgs(
                                        new BamlLocalizableResourceKey(
                                            tokens[i].Value,
                                            string.Empty,
                                            string.Empty
                                            ),
                                        BamlLocalizerError.InvalidUid
                                        )
                                    );
                                succeed = false;
                            }

                            break;
                        }
                }
            }

            return succeed;
        }

        /// <remarks>
        /// Try to add the matching ContentPropertyNode to the newly constructed element
        /// </remarks>
        private static void TryAddContentPropertyToNewElement(
            BamlTreeUpdateMap bamlTreeMap,
            BamlStartElementNode bamlNode
            )
        {
            string contentProperty = bamlTreeMap.GetContentProperty(bamlNode.AssemblyName, bamlNode.TypeFullName);
            if (!string.IsNullOrEmpty(contentProperty))
            {
                bamlNode.AddChild(
                    new BamlContentPropertyNode(
                        bamlNode.AssemblyName,
                        bamlNode.TypeFullName,
                        contentProperty
                        )
                    );
            }
        }

        private class BamlTreeUpdateMap
        {
            private BamlTreeMap _originalMap;
            private BamlTree _tree;

            // from Uid to new nodes created, it is used when:
            // o Deserializing formatting tags e.g <b Id="bold01"></b>. It looks up the node by "bold01".
            // o Apply properties for new nodes. e.g. "Italic01:System.Windows.TextElement.Foreground" is applied on 
            //   element with "Italic 01".
            private Hashtable _uidToNewBamlNodeIndexMap;

            // from full key name to new nodes created
            private Hashtable _keyToNewBamlNodeIndexMap;

            // cached content property table storing (fulltypeName, content property name) pair. 
            private Dictionary<String, string> _contentPropertyTable;

            internal BamlTreeUpdateMap(BamlTreeMap map, BamlTree tree)
            {
                _uidToNewBamlNodeIndexMap = new Hashtable(8);
                _keyToNewBamlNodeIndexMap = new Hashtable(8);
                _originalMap = map;
                _tree = tree;
            }

            internal BamlTreeNode MapKeyToBamlTreeNode(BamlLocalizableResourceKey key)
            {
                BamlTreeNode node = _originalMap.MapKeyToBamlTreeNode(key, _tree);

                if (node == null)
                {
                    // find it in the new nodes
                    if (_keyToNewBamlNodeIndexMap.Contains(key))
                    {
                        node = _tree[(int)_keyToNewBamlNodeIndexMap[key]];
                    }
                }

                return node;
            }

            internal bool IsNewBamlTreeNode(BamlLocalizableResourceKey key)
            {
                return _keyToNewBamlNodeIndexMap.Contains(key);
            }

            internal BamlStartElementNode MapUidToBamlTreeElementNode(string uid)
            {
                BamlStartElementNode node = _originalMap.MapUidToBamlTreeElementNode(uid, _tree);
                if (node == null)
                {
                    // find it in the new nodes
                    if (_uidToNewBamlNodeIndexMap.Contains(uid))
                    {
                        node = _tree[(int)_uidToNewBamlNodeIndexMap[uid]] as BamlStartElementNode;
                    }
                }

                return node;
            }
            internal void AddBamlTreeNode(
                string uid,
                BamlLocalizableResourceKey key,
                BamlTreeNode node
                )
            {
                // add to node
                _tree.AddTreeNode(node);

                // remember the tree node index
                if (uid != null)
                {
                    _uidToNewBamlNodeIndexMap[uid] = _tree.Size - 1;
                }

                _keyToNewBamlNodeIndexMap[key] = _tree.Size - 1;
            }

            internal BamlLocalizationDictionary LocalizationDictionary
            {
                get { return _originalMap.LocalizationDictionary; }
            }

            internal InternalBamlLocalizabilityResolver Resolver
            {
                get { return _originalMap.Resolver; }
            }

            /// <remarks>
            /// The method retrieves the Content property name for the given type. It first looks into 
            /// the KnownTypes table for the value. If not found, it will do a reflection to grab the 
            /// ContentPropertyAttribute on the type. Custom-control assembly is alrady required to be 
            /// present for BamlWriter to generate baml. 
            /// </remarks>
            internal string GetContentProperty(string assemblyName, string fullTypeName)
            {
                //
                // go to KnownTypes to find the content property first
                //
                string nameSpace = string.Empty;
                string typeName = fullTypeName;
                int lastDot = fullTypeName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    nameSpace = fullTypeName.Substring(0, lastDot);
                    typeName = fullTypeName.Substring(lastDot + 1);
                }

                short id = BamlMapTable.GetKnownTypeIdFromName(assemblyName, nameSpace, typeName);

                if (id != 0)
                {
                    KnownElements knownElement = (KnownElements)(-id);
                    return KnownTypes.GetContentPropertyName(knownElement);
                }

                string contentProperty = null;

                //
                // Look into cached values.                 
                //
                if (_contentPropertyTable != null && _contentPropertyTable.TryGetValue(fullTypeName, out contentProperty))
                {
                    return contentProperty;
                }

                //
                // Need to do reflection for it. 
                //

                // Assembly.Load will throw exception if it fails. 
                Assembly assm = Assembly.Load(assemblyName);
                Type type = assm.GetType(fullTypeName);
                if (type != null)
                {
                    object[] contentPropertyAttributes = type.GetCustomAttributes(
                        typeof(ContentPropertyAttribute),
                        true                           // search for inherited value
                        );

                    if (contentPropertyAttributes.Length > 0)
                    {
                        ContentPropertyAttribute contentPropertyAttribute = contentPropertyAttributes[0] as ContentPropertyAttribute;
                        contentProperty = contentPropertyAttribute.Name;

                        // Cach the value for future use.
                        if (_contentPropertyTable == null)
                        {
                            _contentPropertyTable = new Dictionary<string, string>(8);
                        }
                        _contentPropertyTable.Add(fullTypeName, contentProperty);
                    }
                }

                return contentProperty;
            }
        }
    }
}

