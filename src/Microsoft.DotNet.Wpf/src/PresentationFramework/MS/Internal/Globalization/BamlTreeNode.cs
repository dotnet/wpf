// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: BamlTreeNode structures 
//

using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows;
using System.Windows.Markup;

namespace MS.Internal.Globalization
{
    #region BamlTree
    /// <summary>
    /// This reprenents a BamlTree.
    /// It contains two views of the tree. 
    /// 1) the tree itself through BamlTreeNode. It maintains the Baml
    /// file structure for serialization
    /// 2) flat baml through in depth first order
    /// it will be used when creating flat baml resources
    /// </summary>
    internal sealed class BamlTree
    {
        //----------------------------------
        // Constructor
        //----------------------------------

        /// <summary>
        /// create an emtpy baml tree
        /// </summary>
        internal BamlTree() { }

        /// <summary>
        /// Create a baml tree on a root node.
        /// </summary>
        internal BamlTree(BamlTreeNode root, int size)
        {
            Debug.Assert(root != null, "Baml tree root is null!");
            Debug.Assert(size > 0, "Baml tree size is less than 1");

            _root = root;
            _nodeList = new List<BamlTreeNode>(size);
            CreateInternalIndex(ref _root, ref _nodeList, false);
        }

        internal BamlTreeNode Root
        {
            get { return _root; }
        }

        internal int Size
        {
            get { return _nodeList.Count; }
        }

        // index into the flattened baml tree
        internal BamlTreeNode this[int i]
        {
            get
            {
                return _nodeList[i];
            }
        }

        // create a deep copy of the tree
        internal BamlTree Copy()
        {
            // create new root and new list
            BamlTreeNode newTreeRoot = _root;
            List<BamlTreeNode> newNodeList = new List<BamlTreeNode>(Size);

            // create a new copy of the tree.
            CreateInternalIndex(ref newTreeRoot, ref newNodeList, true);

            BamlTree newTree = new BamlTree();
            newTree._root = newTreeRoot;
            newTree._nodeList = newNodeList;

            return newTree;
        }

        // adds a node into the flattened tree node list.
        internal void AddTreeNode(BamlTreeNode node)
        {
            _nodeList.Add(node);
        }

        // travese the tree and store the node in the arraylist in the same order as travesals.
        // it can also create a a new tree by give true to "toCopy"
        private void CreateInternalIndex(ref BamlTreeNode parent, ref List<BamlTreeNode> nodeList, bool toCopy)
        {
            // gets the old children
            List<BamlTreeNode> children = parent.Children;

            if (toCopy)
            {
                // creates a copy of the current node
                parent = parent.Copy();
                if (children != null)
                {
                    // create an new list if there are children.
                    parent.Children = new List<BamlTreeNode>(children.Count);
                }
            }

            // add the node to the flattened list
            nodeList.Add(parent);

            // return if this is the leaf
            if (children == null)
                return;

            // for each child
            for (int i = 0; i < children.Count; i++)
            {
                // get to the child
                BamlTreeNode child = children[i];

                // recursively create index
                CreateInternalIndex(ref child, ref nodeList, toCopy);

                if (toCopy)
                {
                    // if toCopy, we link the new child and new parent.
                    child.Parent = parent;
                    parent.Children.Add(child);
                }
            }
        }

        private BamlTreeNode _root;        // the root of the tree
        private List<BamlTreeNode> _nodeList;    // stores flattened baml tree in depth first order
    }
    #endregion

    #region ILocalizabilityInheritable inteface

    /// <summary>
    /// Nodes in the Baml tree can inherit localizability from parent nodes. Nodes that 
    /// implements this interface will be part of the localizabilty inheritance tree. 
    /// </summary>
    internal interface ILocalizabilityInheritable
    {
        /// <summary>
        /// The ancestor node to inherit localizability from
        /// </summary>
        ILocalizabilityInheritable LocalizabilityAncestor { get; }

        /// <summary>
        /// The inheritable attributs to child nodes. 
        /// </summary>
        LocalizabilityAttribute InheritableAttribute
        {
            get; set;
        }

        bool IsIgnored
        {
            get; set;
        }
    }
    #endregion

    #region BamlTreeNode and sub-classes
    /// <summary>
    /// The node for the internal baml tree
    /// </summary>
    internal abstract class BamlTreeNode
    {
        //---------------------------
        // Constructor
        //---------------------------
        internal BamlTreeNode(BamlNodeType type)
        {
            NodeType = type;
        }

        //----------------------------
        // internal methods
        //----------------------------          

        /// <summary>
        /// Add a child to this node.
        /// </summary>
        /// <param name="child">child node to add</param>
        internal void AddChild(BamlTreeNode child)
        {
            if (_children == null)
            {
                _children = new List<BamlTreeNode>();
            }

            _children.Add(child);   // Add the children
            child.Parent = this;    // Link to its parent
        }

        /// <summary>
        /// Create a copy of the BamlTreeNode
        /// </summary>
        internal abstract BamlTreeNode Copy();

        /// <summary>
        /// Serialize the node through BamlWriter
        /// </summary>
        internal abstract void Serialize(BamlWriter writer);

        //----------------------------
        // internal  properties.
        //----------------------------

        /// <summary>
        /// NodeType
        /// </summary>
        /// <value>BamlNodeType</value>
        internal BamlNodeType NodeType
        {
            get
            {
                return _nodeType;
            }
            set
            {
                _nodeType = value;
            }
        }

        /// <summary>
        /// List of children nodes.
        /// </summary>
        internal List<BamlTreeNode> Children
        {
            get
            {
                return _children;
            }
            set
            {
                _children = value;
            }
        }


        /// <summary>
        /// Parent node.
        /// </summary>
        /// <value>BamlTreeNode</value>
        internal BamlTreeNode Parent
        {
            get
            {
                return _parent;
            }
            set
            {
                _parent = value;
            }
        }


        /// <summary>
        /// Whether the content of this node has been formatted to be part of 
        /// parent node's content.
        /// </summary>
        internal bool Formatted
        {
            get
            {
                return (_state & BamlTreeNodeState.ContentFormatted) != 0;
            }
            set
            {
                if (value)
                {
                    _state |= BamlTreeNodeState.ContentFormatted;
                }
                else
                {
                    _state &= (~BamlTreeNodeState.ContentFormatted);
                }
            }
        }


        /// <summary>
        /// Whether the node has been visited. Used to detect loop in the tree.
        /// </summary>
        internal bool Visited
        {
            get
            {
                return (_state & BamlTreeNodeState.NodeVisited) != 0;
            }
            set
            {
                if (value)
                {
                    _state |= BamlTreeNodeState.NodeVisited;
                }
                else
                {
                    _state &= (~BamlTreeNodeState.NodeVisited);
                }
            }
        }

        /// <summary>
        /// Indicate if the node is not uniquely identifiable in the tree. Its value 
        /// is true when the node or its parent doesn't have unique Uid.
        /// </summary>
        internal bool Unidentifiable
        {
            get
            {
                return (_state & BamlTreeNodeState.Unidentifiable) != 0;
            }
            set
            {
                if (value)
                {
                    _state |= BamlTreeNodeState.Unidentifiable;
                }
                else
                {
                    _state &= (~BamlTreeNodeState.Unidentifiable);
                }
            }
        }

        //----------------------------
        // protected members
        //----------------------------
        protected BamlNodeType _nodeType; // node type
        protected List<BamlTreeNode> _children; // the children list.      
        protected BamlTreeNode _parent;  // the tree parent of this node 

        private BamlTreeNodeState _state;    // the state of this tree node

        [Flags]
        private enum BamlTreeNodeState : byte
        {
            /// <summary>
            /// Default state
            /// </summary>            
            None = 0,

            /// <summary>
            /// Indicate that the content of this node has been formatted inline. We don't need to 
            /// produce an individual localizable resource for it. 
            /// </summary>
            ContentFormatted = 1,

            /// <summary>
            /// Indicate that this node has already been visited once in the tree traversal (such as serialization). 
            /// It is used to prevent the Baml tree contains multiple references to one node. 
            /// </summary>            
            NodeVisited = 2,

            /// <summary>
            /// Indicate that this node cannot be uniquely identify in the Baml tree. It happens when 
            /// it or its parent element has a duplicate Uid. 
            /// </summary>            
            Unidentifiable = 4,
        }
    }


    /// <summary>
    /// Baml StartDocument node
    /// </summary>
    internal sealed class BamlStartDocumentNode : BamlTreeNode, ILocalizabilityInheritable
    {
        internal BamlStartDocumentNode() : base(BamlNodeType.StartDocument) { }
        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteStartDocument();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlStartDocumentNode();
        }

        public ILocalizabilityInheritable LocalizabilityAncestor
        {
            // return null as StartDocument is the root of the inheritance tree
            get { return null; }
        }

        public LocalizabilityAttribute InheritableAttribute
        {
            get
            {
                // return the default attribute as it is at the root of the inheritance
                LocalizabilityAttribute defaultAttribute = new LocalizabilityAttribute(LocalizationCategory.None);
                defaultAttribute.Readability = Readability.Readable;
                defaultAttribute.Modifiability = Modifiability.Modifiable;
                return defaultAttribute;
            }
            set { }
        }

        public bool IsIgnored
        {
            get { return false; }
            set { }
        }
    }

    /// <summary>
    /// Baml EndDocument node
    /// </summary>
    internal sealed class BamlEndDocumentNode : BamlTreeNode
    {
        internal BamlEndDocumentNode() : base(BamlNodeType.EndDocument) { }
        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteEndDocument();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlEndDocumentNode();
        }
    }

    /// <summary>
    /// Baml ConnectionId node
    /// </summary>
    internal sealed class BamlConnectionIdNode : BamlTreeNode
    {
        internal BamlConnectionIdNode(Int32 connectionId) : base(BamlNodeType.ConnectionId)
        {
            _connectionId = connectionId;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteConnectionId(_connectionId);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlConnectionIdNode(_connectionId);
        }

        private Int32 _connectionId;
    }

    /// <summary>
    /// Baml StartElement node, it can also inherit localizability attributes from parent nodes.
    /// </summary>
    internal sealed class BamlStartElementNode : BamlTreeNode, ILocalizabilityInheritable
    {
        internal BamlStartElementNode(
            string assemblyName,
            string typeFullName,
            bool isInjected,
            bool useTypeConverter
            ) : base(BamlNodeType.StartElement)
        {
            _assemblyName = assemblyName;
            _typeFullName = typeFullName;
            _isInjected = isInjected;
            _useTypeConverter = useTypeConverter;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteStartElement(_assemblyName, _typeFullName, _isInjected, _useTypeConverter);
        }

        internal override BamlTreeNode Copy()
        {
            BamlStartElementNode node = new BamlStartElementNode(_assemblyName, _typeFullName, _isInjected, _useTypeConverter);
            node._content = _content;
            node._uid = _uid;
            node._inheritableAttribute = _inheritableAttribute;
            return node;
        }

        /// <summary>
        /// insert property node
        /// </summary>
        /// <remarks> 
        /// Property node needs to be group in front of 
        /// child elements. This method is called when creating a 
        /// new property node. 
        /// </remarks> 
        internal void InsertProperty(BamlTreeNode child)
        {
            if (_children == null)
            {
                AddChild(child);
            }
            else
            {
                int lastProperty = 0;
                for (int i = 0; i < _children.Count; i++)
                {
                    if (_children[i].NodeType == BamlNodeType.Property)
                    {
                        lastProperty = i;
                    }
                }

                _children.Insert(lastProperty, child);
                child.Parent = this;
            }
        }

        //-------------------------------
        // Internal properties
        //-------------------------------

        internal string AssemblyName
        {
            get { return _assemblyName; }
        }

        internal string TypeFullName
        {
            get { return _typeFullName; }
        }

        internal string Content
        {
            get { return _content; }
            set { _content = value; }
        }

        internal string Uid
        {
            get { return _uid; }
            set { _uid = value; }
        }

        public ILocalizabilityInheritable LocalizabilityAncestor
        {
            // Baml element node inherit from parent element
            get
            {
                if (_localizabilityAncestor == null)
                {
                    // walk up the tree to find a parent node that is ILocalizabilityInheritable
                    for (BamlTreeNode parentNode = Parent;
                         _localizabilityAncestor == null && parentNode != null;
                         parentNode = parentNode.Parent)
                    {
                        _localizabilityAncestor = (parentNode as ILocalizabilityInheritable);
                    }
                }

                return _localizabilityAncestor;
            }
        }

        public LocalizabilityAttribute InheritableAttribute
        {
            get { return _inheritableAttribute; }
            set
            {
                Debug.Assert(value.Category != LocalizationCategory.Ignore && value.Category != LocalizationCategory.Inherit);
                _inheritableAttribute = value;
            }
        }

        public bool IsIgnored
        {
            get { return _isIgnored; }
            set { _isIgnored = value; }
        }

        //----------------------
        // Private members
        //----------------------
        private string _assemblyName;
        private string _typeFullName;
        private string _content;
        private string _uid;
        private LocalizabilityAttribute _inheritableAttribute;
        private ILocalizabilityInheritable _localizabilityAncestor;
        private bool _isIgnored;
        private bool _isInjected;
        private bool _useTypeConverter;
    }

    /// <summary>
    /// Baml EndElement node
    /// </summary>
    internal sealed class BamlEndElementNode : BamlTreeNode
    {
        internal BamlEndElementNode() : base(BamlNodeType.EndElement)
        {
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteEndElement();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlEndElementNode();
        }
    }


    /// <summary>
    /// Baml XmlnsProperty node
    /// </summary>
    internal sealed class BamlXmlnsPropertyNode : BamlTreeNode
    {
        internal BamlXmlnsPropertyNode(
            string prefix,
            string xmlns
            ) : base(BamlNodeType.XmlnsProperty)
        {
            _prefix = prefix;
            _xmlns = xmlns;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteXmlnsProperty(_prefix, _xmlns);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlXmlnsPropertyNode(_prefix, _xmlns);
        }

        private string _prefix;
        private string _xmlns;
    }

    /// <summary>
    /// StartComplexProperty node
    /// </summary>
    internal class BamlStartComplexPropertyNode : BamlTreeNode, ILocalizabilityInheritable
    {
        internal BamlStartComplexPropertyNode(
            string assemblyName,
            string ownerTypeFullName,
            string propertyName
            ) : base(BamlNodeType.StartComplexProperty)
        {
            _assemblyName = assemblyName;
            _ownerTypeFullName = ownerTypeFullName;
            _propertyName = propertyName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteStartComplexProperty(
                _assemblyName,
                _ownerTypeFullName,
                _propertyName
                );
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlStartComplexPropertyNode(
                _assemblyName,
                _ownerTypeFullName,
                _propertyName
                );
        }

        internal string AssemblyName
        {
            get { return _assemblyName; }
        }

        internal string PropertyName
        {
            get { return _propertyName; }
        }

        internal string OwnerTypeFullName
        {
            get { return _ownerTypeFullName; }
        }

        public ILocalizabilityInheritable LocalizabilityAncestor
        {
            get { return _localizabilityAncestor; }
            set { _localizabilityAncestor = value; }
        }

        public LocalizabilityAttribute InheritableAttribute
        {
            get { return _inheritableAttribute; }
            set
            {
                Debug.Assert(value.Category != LocalizationCategory.Ignore && value.Category != LocalizationCategory.Inherit);
                _inheritableAttribute = value;
            }
        }

        public bool IsIgnored
        {
            get { return _isIgnored; }
            set { _isIgnored = value; }
        }

        protected string _assemblyName;
        protected string _ownerTypeFullName;
        protected string _propertyName;

        private ILocalizabilityInheritable _localizabilityAncestor;
        private LocalizabilityAttribute _inheritableAttribute;
        private bool _isIgnored;
    }

    /// <summary>
    /// EndComplexProperty node
    /// </summary>
    internal sealed class BamlEndComplexPropertyNode : BamlTreeNode
    {
        internal BamlEndComplexPropertyNode() : base(BamlNodeType.EndComplexProperty)
        {
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteEndComplexProperty();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlEndComplexPropertyNode();
        }
    }

    /// <summary>
    /// Baml Property node, it can inherit localizable attributes from parent nodes.
    /// </summary>
    internal sealed class BamlPropertyNode : BamlStartComplexPropertyNode
    {
        internal BamlPropertyNode(
            string assemblyName,
            string ownerTypeFullName,
            string propertyName,
            string value,
            BamlAttributeUsage usage
            ) : base(assemblyName, ownerTypeFullName, propertyName)
        {
            _value = value;
            _attributeUsage = usage;
            _nodeType = BamlNodeType.Property;
        }

        internal override void Serialize(BamlWriter writer)
        {
            // skip seralizing Localization.Comments and Localization.Attributes properties
            if (!LocComments.IsLocCommentsProperty(_ownerTypeFullName, _propertyName)
             && !LocComments.IsLocLocalizabilityProperty(_ownerTypeFullName, _propertyName))
            {
                writer.WriteProperty(
                 _assemblyName,
                 _ownerTypeFullName,
                 _propertyName,
                 _value,
                 _attributeUsage
                 );
            }
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlPropertyNode(
                _assemblyName,
                _ownerTypeFullName,
                _propertyName,
                _value,
                _attributeUsage
                );
        }

        internal string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        internal int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        private string _value;
        private BamlAttributeUsage _attributeUsage;
        private int _index = 0; // used for auto-numbering repeated properties within an element
    }


    /// <summary>
    /// LiteralContent node
    /// </summary>
    internal sealed class BamlLiteralContentNode : BamlTreeNode
    {
        internal BamlLiteralContentNode(string literalContent) : base(BamlNodeType.LiteralContent)
        {
            _literalContent = literalContent;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteLiteralContent(_literalContent);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlLiteralContentNode(_literalContent);
        }

        internal string Content
        {
            get { return _literalContent; }
            set { _literalContent = value; }
        }

        private string _literalContent;
    }

    /// <summary>
    /// Text node
    /// </summary>    
    internal sealed class BamlTextNode : BamlTreeNode
    {
        internal BamlTextNode(string text) : this(text, null, null)
        {
        }

        internal BamlTextNode(
            string text,
            string typeConverterAssemblyName,
            string typeConverterName
            ) : base(BamlNodeType.Text)
        {
            _content = text;
            _typeConverterAssemblyName = typeConverterAssemblyName;
            _typeConverterName = typeConverterName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteText(_content, _typeConverterAssemblyName, _typeConverterName);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlTextNode(_content, _typeConverterAssemblyName, _typeConverterName);
        }

        internal string Content
        {
            get { return _content; }
        }

        private string _content;
        private string _typeConverterAssemblyName;
        private string _typeConverterName;
    }

    /// <summary>
    /// Routed event node
    /// </summary>
    internal sealed class BamlRoutedEventNode : BamlTreeNode
    {
        internal BamlRoutedEventNode(
            string assemblyName,
            string ownerTypeFullName,
            string eventIdName,
            string handlerName
            ) : base(BamlNodeType.RoutedEvent)
        {
            _assemblyName = assemblyName;
            _ownerTypeFullName = ownerTypeFullName;
            _eventIdName = eventIdName;
            _handlerName = handlerName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteRoutedEvent(
                _assemblyName,
                _ownerTypeFullName,
                _eventIdName,
                _handlerName
                );
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlRoutedEventNode(
                _assemblyName,
                _ownerTypeFullName,
                _eventIdName,
                _handlerName
                );
        }

        private string _assemblyName;
        private string _ownerTypeFullName;
        private string _eventIdName;
        private string _handlerName;
    }

    /// <summary>
    /// event node
    /// </summary>
    internal sealed class BamlEventNode : BamlTreeNode
    {
        internal BamlEventNode(
            string eventName,
            string handlerName
            ) : base(BamlNodeType.Event)
        {
            _eventName = eventName;
            _handlerName = handlerName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteEvent(
                _eventName,
                _handlerName
                );
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlEventNode(_eventName, _handlerName);
        }

        private string _eventName;
        private string _handlerName;
    }


    /// <summary>
    /// DefAttribute node
    /// </summary>
    internal sealed class BamlDefAttributeNode : BamlTreeNode
    {
        internal BamlDefAttributeNode(string name, string value) : base(BamlNodeType.DefAttribute)
        {
            _name = name;
            _value = value;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteDefAttribute(_name, _value);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlDefAttributeNode(_name, _value);
        }

        private string _name;
        private string _value;
    }

    /// <summary>
    /// PIMapping node
    /// </summary>
    internal sealed class BamlPIMappingNode : BamlTreeNode
    {
        internal BamlPIMappingNode(
            string xmlNamespace,
            string clrNamespace,
            string assemblyName
            ) : base(BamlNodeType.PIMapping)
        {
            _xmlNamespace = xmlNamespace;
            _clrNamespace = clrNamespace;
            _assemblyName = assemblyName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WritePIMapping(
                _xmlNamespace,
                _clrNamespace,
                _assemblyName
                );
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlPIMappingNode(
                _xmlNamespace,
                _clrNamespace,
                _assemblyName
                );
        }

        private string _xmlNamespace;
        private string _clrNamespace;
        private string _assemblyName;
    }

    /// <summary>
    /// StartConstructor node
    /// </summary>
    internal sealed class BamlStartConstructorNode : BamlTreeNode
    {
        internal BamlStartConstructorNode() : base(BamlNodeType.StartConstructor)
        {
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteStartConstructor();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlStartConstructorNode();
        }
    }

    /// <summary>
    /// EndConstructor node
    /// </summary>
    internal sealed class BamlEndConstructorNode : BamlTreeNode
    {
        internal BamlEndConstructorNode() : base(BamlNodeType.EndConstructor)
        {
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteEndConstructor();
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlEndConstructorNode();
        }
    }

    ///// <summary>
    ///// ContentProperty node
    ///// </summary>
    internal sealed class BamlContentPropertyNode : BamlTreeNode
    {
        internal BamlContentPropertyNode(
           string assemblyName,
           string typeFullName,
           string propertyName
           ) : base(BamlNodeType.ContentProperty)
        {
            _assemblyName = assemblyName;
            _typeFullName = typeFullName;
            _propertyName = propertyName;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WriteContentProperty(
                    _assemblyName,
                    _typeFullName,
                    _propertyName
                    );
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlContentPropertyNode(
                    _assemblyName,
                    _typeFullName,
                    _propertyName
            );
        }

        private string _assemblyName;
        private string _typeFullName;
        private string _propertyName;
    }

    internal sealed class BamlPresentationOptionsAttributeNode : BamlTreeNode
    {
        internal BamlPresentationOptionsAttributeNode(string name, string value)
            : base(BamlNodeType.PresentationOptionsAttribute)
        {
            _name = name;
            _value = value;
        }

        internal override void Serialize(BamlWriter writer)
        {
            writer.WritePresentationOptionsAttribute(_name, _value);
        }

        internal override BamlTreeNode Copy()
        {
            return new BamlPresentationOptionsAttributeNode(_name, _value);
        }

        private string _name;
        private string _value;
    }
    #endregion
}

