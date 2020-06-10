// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Class that implements BamlTreeMap.

using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Markup.Localizer;
using System.Diagnostics;
using System.Text;
using System.Windows;

using MS.Utility;


// Disabling 1634 and 1691: 
// In order to avoid generating warnings about unknown message numbers and 
// unknown pragmas when compiling C# source code with the C# compiler, 
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

namespace MS.Internal.Globalization
{
    /// <summary>
    /// creates mappings for the baml tree node
    /// this class in charges of creating mappings to the
    /// loclaizable resources and tree nodes.
    /// </summary>
    internal class BamlTreeMap
    {
        //----------------------------------
        // internal Constructor
        //----------------------------------

        /// <summary>
        /// BamlTreeMap.
        /// </summary>
        internal BamlTreeMap(
            BamlLocalizer localizer,
            BamlTree tree,
            BamlLocalizabilityResolver resolver,
            TextReader comments
            )
        {
            Debug.Assert(tree != null, "Baml Tree is empty");
            Debug.Assert(localizer != null, "BamlLocalizer is null");

            _tree = tree;

            // creates an internal resolver which willd delegate calls to client's resolver intelligently.
            _resolver = new InternalBamlLocalizabilityResolver(localizer, resolver, comments);

            // create a LocalizableResourceBuilder to build localizable resources
            _localizableResourceBuilder = new LocalizableResourceBuilder(_resolver);
        }

        //----------------------------------
        // internal properties
        //----------------------------------
        internal BamlLocalizationDictionary LocalizationDictionary
        {
            get
            {
                EnsureMap();
                return _localizableResources;
            }
        }

        internal InternalBamlLocalizabilityResolver Resolver
        {
            get
            {
                return _resolver;
            }
        }

        /// <summary>
        /// Maps a key to a baml tree node in the given tree
        /// </summary>        
        internal BamlTreeNode MapKeyToBamlTreeNode(BamlLocalizableResourceKey key, BamlTree tree)
        {
            if (_keyToBamlNodeIndexMap.Contains(key))
            {
                return tree[(int)_keyToBamlNodeIndexMap[key]];
            }

            return null;
        }

        /// <summary>
        /// Maps a uid to a baml tree node in the given tree
        /// </summary>        
        internal BamlStartElementNode MapUidToBamlTreeElementNode(string uid, BamlTree tree)
        {
            if (_uidToBamlNodeIndexMap.Contains(uid))
            {
                return tree[(int)_uidToBamlNodeIndexMap[uid]] as BamlStartElementNode;
            }

            return null;
        }

        //--------------------------------------
        // Private methods
        //--------------------------------------
        // construct the maps for enumeration
        internal void EnsureMap()
        {
            if (_localizableResources != null)
                return; // map is already created.

            // create the table based on the treesize passed in
            // the hashtable is for look-up during update
            _resolver.InitLocalizabilityCache();
            _keyToBamlNodeIndexMap = new Hashtable(_tree.Size);
            _uidToBamlNodeIndexMap = new Hashtable(_tree.Size / 2);
            _localizableResources = new BamlLocalizationDictionary();

            for (int i = 0; i < _tree.Size; i++)
            {
                BamlTreeNode currentNode = _tree[i];

                // a node may be marked as unidentifiable if it or its parent has a duplicate uid. 
                if (currentNode.Unidentifiable) continue; // skip unidentifiable nodes

                if (currentNode.NodeType == BamlNodeType.StartElement)
                {
                    // remember classes encountered in this baml
                    BamlStartElementNode elementNode = (BamlStartElementNode)currentNode;
                    _resolver.AddClassAndAssembly(elementNode.TypeFullName, elementNode.AssemblyName);
                }

                // find the Uid of the current node
                BamlLocalizableResourceKey key = GetKey(currentNode);

                if (key != null)
                {
                    if (currentNode.NodeType == BamlNodeType.StartElement)
                    {
                        // store uid mapping to the corresponding element node
                        if (_uidToBamlNodeIndexMap.ContainsKey(key.Uid))
                        {
                            _resolver.RaiseErrorNotifyEvent(
                                new BamlLocalizerErrorNotifyEventArgs(
                                    key,
                                    BamlLocalizerError.DuplicateUid
                                    )
                            );

                            // Mark this element and its properties unidentifiable. 
                            currentNode.Unidentifiable = true;
                            if (currentNode.Children != null)
                            {
                                foreach (BamlTreeNode child in currentNode.Children)
                                {
                                    if (child.NodeType != BamlNodeType.StartElement)
                                        child.Unidentifiable = true;
                                }
                            }

                            continue; // skip the duplicate node                                
                        }
                        else
                        {
                            _uidToBamlNodeIndexMap.Add(key.Uid, i);
                        }
                    }

                    _keyToBamlNodeIndexMap.Add(key, i);

                    if (_localizableResources.RootElementKey == null
                     && currentNode.NodeType == BamlNodeType.StartElement
                     && currentNode.Parent != null
                     && currentNode.Parent.NodeType == BamlNodeType.StartDocument)
                    {
                        // remember the key to the root element so that 
                        // users can further add modifications to the root that would have a global impact.
                        // such as FlowDirection or CultureInfo
                        _localizableResources.SetRootElementKey(key);
                    }

                    // create the resource and add to the dictionary
                    BamlLocalizableResource resource = _localizableResourceBuilder.BuildFromNode(key, currentNode);

                    if (resource != null)
                    {
                        _localizableResources.Add(key, resource);
                    }
                }
            }

            _resolver.ReleaseLocalizabilityCache();
        }


        //-------------------------------------------------
        // Internal static
        //-------------------------------------------------

        /// <summary>
        /// Return the localizable resource key for this baml tree node. 
        /// If this node shouldn't be localized, the key returned will be null.
        /// </summary>
        internal static BamlLocalizableResourceKey GetKey(BamlTreeNode node)
        {
            BamlLocalizableResourceKey key = null;

            switch (node.NodeType)
            {
                case BamlNodeType.StartElement:
                    {
                        BamlStartElementNode elementNode = (BamlStartElementNode)node;
                        if (elementNode.Uid != null)
                        {
                            key = new BamlLocalizableResourceKey(
                                elementNode.Uid,
                                elementNode.TypeFullName,
                                BamlConst.ContentSuffix,
                                elementNode.AssemblyName
                                );
                        }
                        break;
                    }
                case BamlNodeType.Property:
                    {
                        BamlPropertyNode propertyNode = (BamlPropertyNode)node;
                        BamlStartElementNode parent = (BamlStartElementNode)propertyNode.Parent;

                        if (parent.Uid != null)
                        {
                            string uid;
                            if (propertyNode.Index <= 0)
                            {
                                uid = parent.Uid;
                            }
                            else
                            {
                                // This node is auto-numbered. This has to do with the fact that 
                                // the compiler may compile duplicated properties into Baml under the same element. 
                                uid = string.Format(
                                    TypeConverterHelper.InvariantEnglishUS,
                                    "{0}.{1}_{2}",
                                    parent.Uid,
                                    propertyNode.PropertyName,
                                    propertyNode.Index
                                    );
                            }

                            key = new BamlLocalizableResourceKey(
                                uid,
                                propertyNode.OwnerTypeFullName,
                                propertyNode.PropertyName,
                                propertyNode.AssemblyName
                                );
                        }
                        break;
                    }
                case BamlNodeType.LiteralContent:
                    {
                        BamlLiteralContentNode literalNode = (BamlLiteralContentNode)node;
                        BamlStartElementNode parent = (BamlStartElementNode)node.Parent;

                        if (parent.Uid != null)
                        {
                            key = new BamlLocalizableResourceKey(
                                parent.Uid,
                                parent.TypeFullName,
                                BamlConst.LiteralContentSuffix,
                                parent.AssemblyName
                                );
                        }
                        break;
                    }
            }

            return key;
        }

        //---------------------------------
        // private members
        //---------------------------------
        private Hashtable _keyToBamlNodeIndexMap;       // _key to baml node. Key is integer. Not using Generic on value type for perf reason
        private Hashtable _uidToBamlNodeIndexMap;
        private LocalizableResourceBuilder _localizableResourceBuilder;

        private BamlLocalizationDictionary _localizableResources;
        private BamlTree _tree;
        private InternalBamlLocalizabilityResolver _resolver;
    }


    internal class InternalBamlLocalizabilityResolver : BamlLocalizabilityResolver
    {
        BamlLocalizabilityResolver _externalResolver;

        // a list of assemblies encounter in the baml
        FrugalObjectList<string> _assemblyNames;

        // class name mapped to assembly index in the Frugal list
        Hashtable _classNameToAssemblyIndex;

        // cached localizablity values
        private Dictionary<string, ElementLocalizability> _classAttributeTable;
        private Dictionary<string, LocalizabilityAttribute> _propertyAttributeTable;

        //
        // cached localization comments 
        // Normally, we only need to use comments of a single element at one time.
        // In case of formatting inline tags we might be grabing comments of a few more elements.
        // A small cached would be enough
        //
        private ElementComments[] _comments;
        private int _commentsIndex;

        // Localization comment document
        private XmlDocument _commentsDocument;

        private BamlLocalizer _localizer;
        private TextReader _commentingText;

        internal InternalBamlLocalizabilityResolver(
            BamlLocalizer localizer,
            BamlLocalizabilityResolver externalResolver,
            TextReader comments
            )
        {
            _localizer = localizer;
            _externalResolver = externalResolver;
            _commentingText = comments;
        }


        internal void AddClassAndAssembly(string className, string assemblyName)
        {
            if (assemblyName == null || _classNameToAssemblyIndex.Contains(className))
                return;

            int index = _assemblyNames.IndexOf(assemblyName);

            if (index < 0)
            {
                // add the new assembly
                _assemblyNames.Add(assemblyName);
                index = _assemblyNames.Count - 1;
            }

            _classNameToAssemblyIndex.Add(className, index);
        }

        internal void InitLocalizabilityCache()
        {
            _assemblyNames = new FrugalObjectList<string>();
            _classNameToAssemblyIndex = new Hashtable(8);

            _classAttributeTable = new Dictionary<string, ElementLocalizability>(8);
            _propertyAttributeTable = new Dictionary<string, LocalizabilityAttribute>(8);

            // 8 cached values for comments. Slots are reused in round-robin fashion
            _comments = new ElementComments[8];
            _commentsIndex = 0;

            XmlDocument doc = null;
            if (_commentingText != null)
            {
                doc = new XmlDocument();

                try
                {
                    doc.Load(_commentingText);
                }
                catch (XmlException)
                {
                    RaiseErrorNotifyEvent(
                        new BamlLocalizerErrorNotifyEventArgs(
                            new BamlLocalizableResourceKey(string.Empty, string.Empty, string.Empty),
                            BamlLocalizerError.InvalidCommentingXml
                            )
                        );

                    doc = null;
                }
            }
            _commentsDocument = doc;
        }

        internal void ReleaseLocalizabilityCache()
        {
            // release cached that is not needed for baml genearation
            _propertyAttributeTable = null;
            _comments = null;
            _commentsIndex = 0;
            _commentsDocument = null;
        }

        // Grab the comments on a BamlTreeNode for the value. 
        internal LocalizabilityGroup GetLocalizabilityComment(
            BamlStartElementNode node,
            string localName
            )
        {
            // get all the comments declares on this node
            ElementComments comment = LookupCommentForElement(node);

            for (int i = 0; i < comment.LocalizationAttributes.Length; i++)
            {
                if (comment.LocalizationAttributes[i].PropertyName == localName)
                {
                    return (LocalizabilityGroup)comment.LocalizationAttributes[i].Value;
                }
            }

            return null;
        }

        internal string GetStringComment(
            BamlStartElementNode node,
            string localName
            )
        {
            // get all the comments declares on this node            
            ElementComments comment = LookupCommentForElement(node);
            for (int i = 0; i < comment.LocalizationComments.Length; i++)
            {
                if (comment.LocalizationComments[i].PropertyName == localName)
                {
                    return (string)comment.LocalizationComments[i].Value;
                }
            }

            return null;
        }

        internal void RaiseErrorNotifyEvent(BamlLocalizerErrorNotifyEventArgs e)
        {
            _localizer.RaiseErrorNotifyEvent(e);
        }

        //--------------------------------------
        // BamlLocalizabilityResolver interface
        //--------------------------------------   
        public override ElementLocalizability GetElementLocalizability(string assembly, string className)
        {
            if (_externalResolver == null
              || assembly == null || assembly.Length == 0
              || className == null || className.Length == 0)
            {
                // return the default value 
                return new ElementLocalizability(
                    null,
                    DefaultAttribute
                    );
            }


            if (_classAttributeTable.ContainsKey(className))
            {
                // return cached value
                return _classAttributeTable[className];
            }
            else
            {
                ElementLocalizability loc = _externalResolver.GetElementLocalizability(assembly, className);
                if (loc == null || loc.Attribute == null)
                {
                    loc = new ElementLocalizability(
                        null,
                        DefaultAttribute
                        );
                }
                _classAttributeTable[className] = loc;
                return loc;
            }
        }

        public override LocalizabilityAttribute GetPropertyLocalizability(string assembly, string className, string property)
        {
            if (_externalResolver == null
               || assembly == null || assembly.Length == 0
               || className == null || className.Length == 0
               || property == null || property.Length == 0)
            {
                return DefaultAttribute;
            }

            string fullName = className + ":" + property;
            if (_propertyAttributeTable.ContainsKey(fullName))
            {
                // return cached value
                return _propertyAttributeTable[fullName];
            }
            else
            {
                LocalizabilityAttribute loc = _externalResolver.GetPropertyLocalizability(assembly, className, property);
                if (loc == null)
                {
                    loc = DefaultAttribute;
                }

                _propertyAttributeTable[fullName] = loc;
                return loc;
            }
        }

        public override string ResolveFormattingTagToClass(string formattingTag)
        {
            // go through the cache to find the mapping
            foreach (KeyValuePair<string, ElementLocalizability> pair in _classAttributeTable)
            {
                if (pair.Value.FormattingTag == formattingTag)
                    return pair.Key;
            }

            string className = null;
            if (_externalResolver != null)
            {
                // it is a formatting tag not resolved before. need to ask for client's help
                className = _externalResolver.ResolveFormattingTagToClass(formattingTag);
                if (!string.IsNullOrEmpty(className))
                {
                    // cache the result
                    if (_classAttributeTable.ContainsKey(className))
                    {
                        _classAttributeTable[className].FormattingTag = formattingTag;
                    }
                    else
                    {
                        _classAttributeTable[className] = new ElementLocalizability(formattingTag, null);
                    }
                }
            }

            return className;
        }

        public override string ResolveAssemblyFromClass(string className)
        {
            if (className == null || className.Length == 0)
            {
                return string.Empty;
            }

            // go through class encountered in this baml first
            // if we saw this class before, we return the corresponding assembly
            if (_classNameToAssemblyIndex.Contains(className))
            {
                return _assemblyNames[(int)_classNameToAssemblyIndex[className]];
            }

            string assemblyName = null;
            if (_externalResolver != null)
            {
                // it is a class not resolved before. need to ask for client's help
                assemblyName = _externalResolver.ResolveAssemblyFromClass(className);
                AddClassAndAssembly(className, assemblyName);
            }

            return assemblyName;
        }

        private LocalizabilityAttribute DefaultAttribute
        {
            get
            {
                // if the value has no localizability attribute set, we default to all inherit.
                LocalizabilityAttribute attribute = new LocalizabilityAttribute(LocalizationCategory.Inherit);
                attribute.Modifiability = Modifiability.Inherit;
                attribute.Readability = Readability.Inherit;
                return attribute;
            }
        }

        private ElementComments LookupCommentForElement(BamlStartElementNode node)
        {
            Debug.Assert(node.NodeType == BamlNodeType.StartElement);

            if (node.Uid == null)
            {
                return new ElementComments(); // return empty comments for null Uid
            }

            for (int i = 0; i < _comments.Length; i++)
            {
                if (_comments[i] != null && _comments[i].ElementId == node.Uid)
                {
                    return _comments[i];
                }
            }

            ElementComments comment = new ElementComments();
            comment.ElementId = node.Uid;

            if (_commentsDocument != null)
            {
                // select the xmlNode containing the comments
                XmlElement element = FindElementByID(_commentsDocument, node.Uid);

                if (element != null)
                {
                    // parse the comments
                    string locAttribute = element.GetAttribute(LocComments.LocLocalizabilityAttribute);
                    SetLocalizationAttributes(node, comment, locAttribute);

                    locAttribute = element.GetAttribute(LocComments.LocCommentsAttribute);
                    SetLocalizationComments(node, comment, locAttribute);
                }
            }

            if (node.Children != null)
            {
                //
                // The baml itself might contain comments too
                // Grab the missing comments from Baml if there is any.
                //    

                for (int i = 0;
                     i < node.Children.Count && (comment.LocalizationComments.Length == 0 || comment.LocalizationAttributes.Length == 0);
                     i++)
                {
                    BamlTreeNode child = (BamlTreeNode)node.Children[i];
                    if (child.NodeType == BamlNodeType.Property)
                    {
                        BamlPropertyNode propertyNode = (BamlPropertyNode)child;

                        if (LocComments.IsLocCommentsProperty(propertyNode.OwnerTypeFullName, propertyNode.PropertyName)
                         && comment.LocalizationComments.Length == 0)
                        {
                            // grab comments from Baml
                            SetLocalizationComments(node, comment, propertyNode.Value);
                        }
                        else if (LocComments.IsLocLocalizabilityProperty(propertyNode.OwnerTypeFullName, propertyNode.PropertyName)
                            && comment.LocalizationAttributes.Length == 0)
                        {
                            // grab comments from Baml                            
                            SetLocalizationAttributes(node, comment, propertyNode.Value);
                        }
                    }
                }
            }

            // cached it 
            _comments[_commentsIndex] = comment;
            _commentsIndex = (_commentsIndex + 1) % _comments.Length;

            return comment;
        }

        private static XmlElement FindElementByID(XmlDocument doc, string uid)
        {
            // Have considered using XPATH. However, XPATH doesn't have a way to escape single quote within 
            // single quotes, here we iterate through the document by ourselves            
            if (doc != null && doc.DocumentElement != null)
            {
                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Element)
                    {
                        XmlElement element = (XmlElement)node;
                        if (element.Name == LocComments.LocCommentsElement
                         && element.GetAttribute(LocComments.LocCommentIDAttribute) == uid)
                        {
                            return element;
                        }
                    }
                }
            }

            return null;
        }

        private void SetLocalizationAttributes(
            BamlStartElementNode node,
            ElementComments comments,
            string attributes
            )
        {
            if (!string.IsNullOrEmpty(attributes))
            {
                try
                {
                    comments.LocalizationAttributes = LocComments.ParsePropertyLocalizabilityAttributes(attributes);
                }
                catch (FormatException)
                {
                    RaiseErrorNotifyEvent(
                        new BamlLocalizerErrorNotifyEventArgs(
                            BamlTreeMap.GetKey(node),
                            BamlLocalizerError.InvalidLocalizationAttributes
                            )
                        );
                }
            }
        }

        private void SetLocalizationComments(
            BamlStartElementNode node,
            ElementComments comments,
            string stringComment
            )
        {
            if (!string.IsNullOrEmpty(stringComment))
            {
                try
                {
                    comments.LocalizationComments = LocComments.ParsePropertyComments(stringComment);
                }
                catch (FormatException)
                {
                    RaiseErrorNotifyEvent(
                        new BamlLocalizerErrorNotifyEventArgs(
                            BamlTreeMap.GetKey(node),
                            BamlLocalizerError.InvalidLocalizationComments
                            )
                        );
                }
            }
        }

        /// <summary>
        /// Data structure for all the comments declared on a particular element 
        /// </summary>
        private class ElementComments
        {
            internal string ElementId;        // element's uid
            internal PropertyComment[] LocalizationAttributes; // Localization.Attributes
            internal PropertyComment[] LocalizationComments;   // Localization.Comments 

            internal ElementComments()
            {
                ElementId = null;
                LocalizationAttributes = Array.Empty<PropertyComment>();
                LocalizationComments = Array.Empty<PropertyComment>();
            }
        }
    }
}

