// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Internal class that build the baml tree for localization


using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;
using System.Text;

using System.Windows;
using System.Windows.Markup;

namespace MS.Internal.Globalization
{
    /// <summary>
    /// BamlResourceDeserializer. This class strictly operates on Baml format, and doesn't
    /// know any localization specific details
    /// </summary>
    internal sealed class BamlResourceDeserializer
    {
        //----------------------------
        // Internal static
        //----------------------------
        internal static BamlTree LoadBaml(Stream bamlStream)
        {
            // thread safe implementation
            return (new BamlResourceDeserializer()).LoadBamlImp(bamlStream);
        }

        //------------------------------
        // Private constructor
        //------------------------------
        private BamlResourceDeserializer()
        {
        }

        //-----------------------------
        // private  methods
        //-----------------------------
        /// <summary>
        /// build the baml element tree as well as the localizability inheritance tree
        /// </summary>
        /// <param name="bamlSteam">input baml stream.</param>
        /// <returns>the tree constructed.</returns>
        private BamlTree LoadBamlImp(Stream bamlSteam)
        {
            _reader = new BamlReader(bamlSteam);
            _reader.Read();

            if (_reader.NodeType != BamlNodeType.StartDocument)
            {
                throw new XamlParseException(SR.Get(SRID.InvalidStartOfBaml));
            }

            // create root element.
            _root = new BamlStartDocumentNode();
            PushNodeToStack(_root);

            // A hash table used to handle duplicate properties within an element
            Hashtable propertyOccurrences = new Hashtable(8);

            // create the tree by depth first traversal.
            while (_bamlTreeStack.Count > 0 && _reader.Read())
            {
                switch (_reader.NodeType)
                {
                    case BamlNodeType.StartElement:
                        {
                            BamlTreeNode bamlNode = new BamlStartElementNode(
                                _reader.AssemblyName,
                                _reader.Name,
                                _reader.IsInjected,
                                _reader.CreateUsingTypeConverter
                                );
                            PushNodeToStack(bamlNode);
                            break;
                        }
                    case BamlNodeType.EndElement:
                        {
                            BamlTreeNode bamlNode = new BamlEndElementNode();
                            AddChildToCurrentParent(bamlNode);
                            PopStack();
                            break;
                        }
                    case BamlNodeType.StartComplexProperty:
                        {
                            BamlStartComplexPropertyNode bamlNode = new BamlStartComplexPropertyNode(
                                _reader.AssemblyName,
                                _reader.Name.Substring(0, _reader.Name.LastIndexOf('.')),
                                _reader.LocalName
                                );

                            bamlNode.LocalizabilityAncestor = PeekPropertyStack(bamlNode.PropertyName);
                            PushPropertyToStack(bamlNode.PropertyName, (ILocalizabilityInheritable)bamlNode);
                            PushNodeToStack(bamlNode);
                            break;
                        }
                    case BamlNodeType.EndComplexProperty:
                        {
                            BamlTreeNode bamlNode = new BamlEndComplexPropertyNode();
                            AddChildToCurrentParent(bamlNode);
                            PopStack();
                            break;
                        }
                    case BamlNodeType.Event:
                        {
                            BamlTreeNode bamlNode = new BamlEventNode(_reader.Name, _reader.Value);
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.RoutedEvent:
                        {
                            BamlTreeNode bamlNode = new BamlRoutedEventNode(
                                _reader.AssemblyName,
                                _reader.Name.Substring(0, _reader.Name.LastIndexOf('.')),
                                _reader.LocalName,
                                _reader.Value
                                );
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.PIMapping:
                        {
                            BamlTreeNode bamlNode = new BamlPIMappingNode(
                                _reader.XmlNamespace,
                                _reader.ClrNamespace,
                                _reader.AssemblyName
                                );
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.LiteralContent:
                        {
                            BamlTreeNode bamlNode = new BamlLiteralContentNode(_reader.Value);
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.Text:
                        {
                            BamlTreeNode bamlNode = new BamlTextNode(
                                _reader.Value,
                                _reader.TypeConverterAssemblyName,
                                _reader.TypeConverterName
                                );

                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.StartConstructor:
                        {
                            BamlTreeNode bamlNode = new BamlStartConstructorNode();
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.EndConstructor:
                        {
                            BamlTreeNode bamlNode = new BamlEndConstructorNode();
                            AddChildToCurrentParent(bamlNode);
                            break;
                        }
                    case BamlNodeType.EndDocument:
                        {
                            BamlTreeNode bamlNode = new BamlEndDocumentNode();
                            AddChildToCurrentParent(bamlNode);
                            PopStack();
                            break;
                        }
                    default:
                        {
                            throw new XamlParseException(SR.Get(SRID.UnRecognizedBamlNodeType, _reader.NodeType));
                        }
                }

                // read properties if it has any.
                if (_reader.HasProperties)
                {
                    // The Hashtable is used to auto-number repeated properties. The usage is as the following:
                    // When encountering the 1st occurrence of the property, it stores a reference to the property's node (BamlTreeNode).
                    // When encountering the property the 2nd time, we start auto-numbering the property including the 1st occurrence
                    // and store the index count (int) in the slot from that point onwards.                                         
                    propertyOccurrences.Clear();

                    _reader.MoveToFirstProperty();
                    do
                    {
                        switch (_reader.NodeType)
                        {
                            case BamlNodeType.ConnectionId:
                                {
                                    BamlTreeNode bamlNode = new BamlConnectionIdNode(_reader.ConnectionId);
                                    AddChildToCurrentParent(bamlNode);
                                    break;
                                }
                            case BamlNodeType.Property:
                                {
                                    BamlPropertyNode bamlNode = new BamlPropertyNode(
                                        _reader.AssemblyName,
                                        _reader.Name.Substring(0, _reader.Name.LastIndexOf('.')),
                                        _reader.LocalName,
                                        _reader.Value,
                                        _reader.AttributeUsage
                                        );

                                    bamlNode.LocalizabilityAncestor = PeekPropertyStack(bamlNode.PropertyName);
                                    PushPropertyToStack(bamlNode.PropertyName, (ILocalizabilityInheritable)bamlNode);

                                    AddChildToCurrentParent(bamlNode);

                                    if (propertyOccurrences.Contains(_reader.Name))
                                    {
                                        // we autonumber properties that have occurrences larger than 1
                                        object occurrence = propertyOccurrences[_reader.Name];
                                        int index = 2;
                                        if (occurrence is BamlPropertyNode)
                                        {
                                            // start numbering this property as the 2nd occurrence is encountered
                                            // the value stores the 1st occurrence of the property at this point
                                            ((BamlPropertyNode)occurrence).Index = 1;
                                        }
                                        else
                                        {
                                            // For the 3rd or more occurrences, the value stores the next index 
                                            // to assign to the property
                                            index = (int)occurrence;
                                        }

                                        // auto-number the current property node
                                        ((BamlPropertyNode)bamlNode).Index = index;
                                        propertyOccurrences[_reader.Name] = ++index;
                                    }
                                    else
                                    {
                                        // store the first occurrence of the property
                                        propertyOccurrences[_reader.Name] = bamlNode;
                                    }

                                    break;
                                }
                            case BamlNodeType.DefAttribute:
                                {
                                    if (_reader.Name == XamlReaderHelper.DefinitionUid)
                                    {
                                        // set the Uid proeprty when we see it.
                                        ((BamlStartElementNode)_currentParent).Uid = _reader.Value;
                                    }

                                    BamlTreeNode bamlNode = new BamlDefAttributeNode(
                                        _reader.Name,
                                        _reader.Value
                                        );
                                    AddChildToCurrentParent(bamlNode);
                                    break;
                                }
                            case BamlNodeType.XmlnsProperty:
                                {
                                    BamlTreeNode bamlNode = new BamlXmlnsPropertyNode(
                                        _reader.LocalName,
                                        _reader.Value
                                        );
                                    AddChildToCurrentParent(bamlNode);
                                    break;
                                }
                            case BamlNodeType.ContentProperty:
                                {
                                    BamlTreeNode bamlNode = new BamlContentPropertyNode(
                                        _reader.AssemblyName,
                                        _reader.Name.Substring(0, _reader.Name.LastIndexOf('.')),
                                        _reader.LocalName
                                        );

                                    AddChildToCurrentParent(bamlNode);
                                    break;
                                }
                            case BamlNodeType.PresentationOptionsAttribute:
                                {
                                    BamlTreeNode bamlNode = new BamlPresentationOptionsAttributeNode(
                                        _reader.Name,
                                        _reader.Value
                                        );
                                    AddChildToCurrentParent(bamlNode);
                                    break;
                                }
                            default:
                                {
                                    throw new XamlParseException(SR.Get(SRID.UnRecognizedBamlNodeType, _reader.NodeType));
                                }
                        }
                    } while (_reader.MoveToNextProperty());
                }
            }

            // At this point, the baml tree stack should be completely unwinded and also nothing more to read.
            if (_reader.Read() || _bamlTreeStack.Count > 0)
            {
                throw new XamlParseException(SR.Get(SRID.InvalidEndOfBaml));
            }

            return new BamlTree(_root, _nodeCount);
            //notice that we don't close the input stream because we don't own it.
        }

        private void PushNodeToStack(BamlTreeNode node)
        {
            if (_currentParent != null)
                _currentParent.AddChild(node);

            _bamlTreeStack.Push(node);
            _currentParent = node;
            _nodeCount++;
        }

        private void AddChildToCurrentParent(BamlTreeNode node)
        {
            if (_currentParent == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.NullParentNode));
            }

            _currentParent.AddChild(node);
            _nodeCount++;
        }

        private void PopStack()
        {
            BamlTreeNode node = _bamlTreeStack.Pop();
            if (node.Children != null)
            {
                // pop properties from property inheritance stack as well
                foreach (BamlTreeNode child in node.Children)
                {
                    BamlStartComplexPropertyNode propertyNode = child as BamlStartComplexPropertyNode;
                    if (propertyNode != null)
                    {
                        PopPropertyFromStack(propertyNode.PropertyName);
                    }
                }
            }

            if (_bamlTreeStack.Count > 0)
            {
                _currentParent = _bamlTreeStack.Peek();
            }
            else
            {
                // stack is empty. Set CurrentParent to null
                _currentParent = null;
            }
        }

        private void PushPropertyToStack(string propertyName, ILocalizabilityInheritable node)
        {
            Stack<ILocalizabilityInheritable> stackForProperty;
            if (_propertyInheritanceTreeStack.ContainsKey(propertyName))
            {
                // get the stack
                stackForProperty = _propertyInheritanceTreeStack[propertyName];
            }
            else
            {
                stackForProperty = new Stack<ILocalizabilityInheritable>();
                _propertyInheritanceTreeStack.Add(propertyName, stackForProperty);
            }

            // push the node
            stackForProperty.Push(node);
        }

        private void PopPropertyFromStack(string propertyName)
        {
            Stack<ILocalizabilityInheritable> stackForProperty = _propertyInheritanceTreeStack[propertyName];
            stackForProperty.Pop();
        }

        // Get the Top of the stack for a certain property
        private ILocalizabilityInheritable PeekPropertyStack(string propertyName)
        {
            if (_propertyInheritanceTreeStack.ContainsKey(propertyName))
            {
                Stack<ILocalizabilityInheritable> stackForProperty = _propertyInheritanceTreeStack[propertyName];
                if (stackForProperty.Count > 0)
                {
                    return stackForProperty.Peek();
                }
            }

            // return root of the document if there is inheritable property on stack
            return _root;
        }

        //-----------------------------
        // private members.
        //-----------------------------

        // stack for building baml trees
        private Stack<BamlTreeNode> _bamlTreeStack = new Stack<BamlTreeNode>();

        // stacks for building property's localizability inheritance tree.
        private Dictionary<string, Stack<ILocalizabilityInheritable>> _propertyInheritanceTreeStack
            = new Dictionary<string, Stack<ILocalizabilityInheritable>>(8);

        private BamlTreeNode _currentParent;
        private BamlStartDocumentNode _root;
        private BamlReader _reader;
        private int _nodeCount;         // count the total number of nodes in the tree.(for future use by status bar)
    }
}
