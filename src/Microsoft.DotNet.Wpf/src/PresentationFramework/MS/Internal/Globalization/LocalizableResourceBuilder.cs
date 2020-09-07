// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Globalization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Markup.Localizer;
using System.Diagnostics;
using System.Text;

namespace MS.Internal.Globalization
{
    ///<summary>
    /// this class is builds the BamlLocalizableResources
    /// it handles all the localizability attribute reading, and inheritance resolution in the tree. 
    ///</summary>     
    internal sealed class LocalizableResourceBuilder
    {
        internal LocalizableResourceBuilder(InternalBamlLocalizabilityResolver resolver)
        {
            _resolver = resolver;
        }

        /// <summary>
        /// build a localizable resource from a baml tree node
        /// </summary>        
        internal BamlLocalizableResource BuildFromNode(BamlLocalizableResourceKey key, BamlTreeNode node)
        {
            if (node.Formatted)
            {
                // the content of the node has been formatted to be part of 
                // parents' content, so no need to create a seperate entry for the
                // element
                return null;
            }

            BamlLocalizableResource resource = null;
            LocalizabilityAttribute localizability = null;
            string formattingTag;

            // 
            // variable controling what comments gets applied
            //
            BamlStartElementNode commentNode = null;  // node containing comment
            string commentTargetName = null;  // the target of the comment, e.g. $Content, FontSize, etc.            

            //
            // step 1: Get the localizability attribute from the source files
            //        
            switch (node.NodeType)
            {
                case BamlNodeType.StartElement:
                    {
                        // For element
                        commentNode = (BamlStartElementNode)node;
                        GetLocalizabilityForElementNode(commentNode, out localizability, out formattingTag);
                        commentTargetName = BamlConst.ContentSuffix;
                        break;
                    }
                case BamlNodeType.LiteralContent:
                    {
                        // For literal content, get the attribute from parent element
                        GetLocalizabilityForElementNode((BamlStartElementNode)node.Parent, out localizability, out formattingTag);

                        commentNode = (BamlStartElementNode)node.Parent;
                        commentTargetName = BamlConst.ContentSuffix;
                        break;
                    }

                case BamlNodeType.Property:
                    {
                        BamlStartComplexPropertyNode propertyNode = (BamlStartComplexPropertyNode)node;
                        if (LocComments.IsLocCommentsProperty(propertyNode.OwnerTypeFullName, propertyNode.PropertyName)
                           || LocComments.IsLocLocalizabilityProperty(propertyNode.OwnerTypeFullName, propertyNode.PropertyName)
                           )
                        {
                            // skip Localization.Comments and Localization.Attributes properties. They aren't localizable
                            return null;
                        }

                        // For property                    
                        GetLocalizabilityForPropertyNode(propertyNode, out localizability);

                        commentTargetName = propertyNode.PropertyName;
                        commentNode = (BamlStartElementNode)node.Parent;
                        break;
                    }
                default:
                    {
                        Invariant.Assert(false); // no localizable resource for such node
                        break;
                    }
            }

            //
            // Step 2: Find out the inheritance value
            //

            // The node participates in localizability inheritance
            // let's fill things in
            localizability = CombineAndPropagateInheritanceValues(
                node as ILocalizabilityInheritable,
                localizability
                );

            //
            // Step 3: We finalized the localizability values. We now apply.
            //            
            string content = null;

            if (localizability.Category != LocalizationCategory.NeverLocalize
             && localizability.Category != LocalizationCategory.Ignore
             && TryGetContent(key, node, out content))
            {
                // we only create one if it is localizable
                resource = new BamlLocalizableResource();
                resource.Readable = (localizability.Readability == Readability.Readable);
                resource.Modifiable = (localizability.Modifiability == Modifiability.Modifiable);
                resource.Category = localizability.Category;
                // continue to fill in content.
                resource.Content = content;
                resource.Comments = _resolver.GetStringComment(commentNode, commentTargetName);
            }

            // return the resource
            return resource;
        }

        /// <summary>
        /// This builds the localizable string from the baml tree node
        /// </summary>
        /// <return>
        /// return true when the node has valid localizable content, false otherwise. 
        /// </return>
        internal bool TryGetContent(BamlLocalizableResourceKey key, BamlTreeNode currentNode, out string content)
        {
            content = string.Empty;

            switch (currentNode.NodeType)
            {
                case BamlNodeType.Property:
                    {
                        bool isValidContent = true;
                        BamlPropertyNode propertyNode = (BamlPropertyNode)currentNode;
                        content = BamlResourceContentUtil.EscapeString(propertyNode.Value);

                        //
                        // Markup extensions are not localizable values, e.g. {x:Type SolidColorBrush}. 
                        // So if the string can be parsed as Markup extensions, we will exclude it unless
                        // the user sets localization comments explicitly to localize this value.
                        //
                        string typeName, args;
                        string tempContent = content;
                        if (MarkupExtensionParser.GetMarkupExtensionTypeAndArgs(ref tempContent, out typeName, out args))
                        {
                            // See if this value has been marked as localizable explicitly in comments
                            LocalizabilityGroup localizability = _resolver.GetLocalizabilityComment(
                                propertyNode.Parent as BamlStartElementNode,
                                propertyNode.PropertyName
                                );

                            isValidContent = (localizability != null && localizability.Readability == Readability.Readable);
                        }
                        return isValidContent;
                    }
                case BamlNodeType.LiteralContent:
                    {
                        content = BamlResourceContentUtil.EscapeString(
                            ((BamlLiteralContentNode)currentNode).Content
                            );
                        return true; // succeed                       
                    }
                case BamlNodeType.StartElement:
                    {
                        BamlStartElementNode elementNode = (BamlStartElementNode)currentNode;
                        if (elementNode.Content == null)
                        {
                            StringBuilder contentBuilder = new StringBuilder();
                            foreach (BamlTreeNode child in elementNode.Children)
                            {
                                // we only format element and text inline
                                // other nodes like property node we don't put them into the content of the element
                                switch (child.NodeType)
                                {
                                    case BamlNodeType.StartElement:
                                        {
                                            string childContent;
                                            if (TryFormatElementContent(key, (BamlStartElementNode)child, out childContent))
                                            {
                                                contentBuilder.Append(childContent);
                                            }
                                            else
                                            {
                                                return false; // failed to get content for children element
                                            }

                                            break;
                                        }
                                    case BamlNodeType.Text:
                                        {
                                            contentBuilder.Append(BamlResourceContentUtil.EscapeString(
                                                ((BamlTextNode)child).Content)
                                                );
                                            break;
                                        }
                                }
                            }

                            elementNode.Content = contentBuilder.ToString();
                        }

                        content = elementNode.Content;
                        return true;
                    }
                default:
                    return true;
            }
        }

        private bool TryFormatElementContent(
            BamlLocalizableResourceKey key,
            BamlStartElementNode node,
            out string content
            )
        {
            content = string.Empty;

            string formattingTag;
            LocalizabilityAttribute attribute;
            GetLocalizabilityForElementNode(node, out attribute, out formattingTag);
            attribute = CombineAndPropagateInheritanceValues(node, attribute);

            if (formattingTag != null
              && attribute.Category != LocalizationCategory.NeverLocalize
              && attribute.Category != LocalizationCategory.Ignore
              && attribute.Modifiability == Modifiability.Modifiable
              && attribute.Readability == Readability.Readable
              )
            {
                // this node should be formatted inline
                StringBuilder contentBuilder = new StringBuilder();

                // write opening tag
                if (node.Uid != null)
                {
                    contentBuilder.AppendFormat(
                        TypeConverterHelper.InvariantEnglishUS,
                        "<{0} {1}=\"{2}\">",
                        formattingTag,
                        XamlReaderHelper.DefinitionUid,
                        BamlResourceContentUtil.EscapeString(node.Uid)
                        );
                }
                else
                {
                    contentBuilder.AppendFormat(TypeConverterHelper.InvariantEnglishUS, "<{0}>", formattingTag);
                }

                // recurisively call down to format the content
                string childContent;
                bool succeed = TryGetContent(key, node, out childContent);

                if (succeed)
                {
                    contentBuilder.Append(childContent);

                    // write closing tag
                    contentBuilder.AppendFormat(TypeConverterHelper.InvariantEnglishUS, "</{0}>", formattingTag);

                    // remeber that we format this element so that we don't format the value again.
                    // e.g. <Text x:Uid="t"> <Bold x:Uid="b"> ... </Bold> </Text>
                    // if <Bold> is already inlined in Text element's contennt, we don't need to 
                    // have a seperate entry for <Bold> anymore
                    node.Formatted = true;
                    content = contentBuilder.ToString();
                }

                return succeed;
            }
            else
            {
                // this node should be represented by place holder.
                bool succeed = true;

                if (node.Uid != null)
                {
                    content = string.Format(
                        TypeConverterHelper.InvariantEnglishUS,
                        "{0}{1}{2}",
                        BamlConst.ChildStart,
                        BamlResourceContentUtil.EscapeString(node.Uid),
                        BamlConst.ChildEnd
                        );
                }
                else
                {
                    // we want to enforce the rule that all children element
                    // must have UID defined.
                    _resolver.RaiseErrorNotifyEvent(
                        new BamlLocalizerErrorNotifyEventArgs(
                            key,
                            BamlLocalizerError.UidMissingOnChildElement
                            )
                    );
                    succeed = false; // failed
                }

                return succeed;
            }
        }


        private void GetLocalizabilityForElementNode(
            BamlStartElementNode node,
            out LocalizabilityAttribute localizability,
            out string formattingTag
            )
        {
            localizability = null;
            formattingTag = null;

            // get the names we need
            string assemblyName = node.AssemblyName;
            string className = node.TypeFullName;

            // query the resolver
            ElementLocalizability result = _resolver.GetElementLocalizability(
                 assemblyName,
                 className
                 );

            LocalizabilityGroup comment = null;
            comment = _resolver.GetLocalizabilityComment(node, BamlConst.ContentSuffix);

            if (comment != null)
            {
                localizability = comment.Override(result.Attribute);
            }
            else
            {
                localizability = result.Attribute;
            }

            formattingTag = result.FormattingTag;
        }


        private void GetLocalizabilityForPropertyNode(
            BamlStartComplexPropertyNode node,
            out LocalizabilityAttribute localizability
            )
        {
            localizability = null;

            string assemblyName = node.AssemblyName;
            string className = node.OwnerTypeFullName;
            string propertyLocalName = node.PropertyName;

            if (className == null || className.Length == 0)
            {
                // class name can be empty or null. For example, <Set PropertyPath="...">
                // We will use the parent node's value.  
                string formattingTag;
                GetLocalizabilityForElementNode((BamlStartElementNode)node.Parent, out localizability, out formattingTag);
                return;
            }

            LocalizabilityGroup comment = _resolver.GetLocalizabilityComment(
                    (BamlStartElementNode)node.Parent,
                    node.PropertyName
                    );

            localizability = _resolver.GetPropertyLocalizability(
                    assemblyName,
                    className,
                    propertyLocalName
                    );

            if (comment != null)
            {
                localizability = comment.Override(localizability);
            }
        }

        /// <summary>
        /// Combine inheritable attributes, and propegate it down the tree.        
        /// </summary>
        /// <param name="node">current node</param>
        /// <param name="localizabilityFromSource">localizability defined in source code</param>
        /// <returns>
        /// The LocalizabilityAttribute to used for this node. It is not the same as the 
        /// inheritable attributes of the node when the node is set to Ignore.    
        /// </returns>
        /// <remarks>We always walk the baml tree in depth-first order</remarks>
        private LocalizabilityAttribute CombineAndPropagateInheritanceValues(
            ILocalizabilityInheritable node,
            LocalizabilityAttribute localizabilityFromSource
            )
        {
            if (node == null)
            {
                return localizabilityFromSource;
            }

            // If this node's inheritable localizability has been constructed, we can skip it
            // This can happen when recursively format the content
            if (node.InheritableAttribute != null)
            {
                return (!node.IsIgnored) ? node.InheritableAttribute : LocalizabilityIgnore;
            }

            // To test wether the current node needs to inherit values from parents. 
            // It inherits values if: 
            // o This node is set to Ignore, in which case it propagates parent values. 
            // o Some of its attributes set to Inherit.
            if (localizabilityFromSource.Category != LocalizationCategory.Ignore
              && localizabilityFromSource.Category != LocalizationCategory.Inherit
              && localizabilityFromSource.Readability != Readability.Inherit
              && localizabilityFromSource.Modifiability != Modifiability.Inherit)
            {
                // just return the same one because no value is inherited
                node.InheritableAttribute = localizabilityFromSource;
                return node.InheritableAttribute;
            }

            // find the ancestor to inherit values now.
            ILocalizabilityInheritable ancestor = node.LocalizabilityAncestor;

            // find out the attribute that is inheritable from above
            LocalizabilityAttribute inheritableAttribute = ancestor.InheritableAttribute;

            if (inheritableAttribute == null)
            {
                // if ancestor's inheritable value isn't resolved yet, we recursively 
                // resolve it here.
                BamlStartElementNode elementNode = ancestor as BamlStartElementNode;
                if (elementNode != null)
                {
                    string formattingTag;
                    GetLocalizabilityForElementNode(elementNode, out inheritableAttribute, out formattingTag);
                }
                else
                {
                    BamlStartComplexPropertyNode propertyNode = ancestor as BamlStartComplexPropertyNode;
                    GetLocalizabilityForPropertyNode(propertyNode, out inheritableAttribute);
                }

                CombineAndPropagateInheritanceValues(ancestor, inheritableAttribute);

                inheritableAttribute = ancestor.InheritableAttribute;
                Debug.Assert(inheritableAttribute != null);
            }

            // if this item is set to ignore
            if (localizabilityFromSource.Category == LocalizationCategory.Ignore)
            {
                // It propagates ancestor's inheritable localizability, but it will use
                // its own value declared in source.
                // We also mark this node as being "Ignored" in the inheritance tree to signal that
                // this node is not using the inheritance value.
                node.InheritableAttribute = inheritableAttribute;
                node.IsIgnored = true;
                return LocalizabilityIgnore;
            }

            // the item is not set to ignore, so we process the inheritable values
            BamlTreeNode treeNode = (BamlTreeNode)node;
            switch (treeNode.NodeType)
            {
                case BamlNodeType.StartElement:
                case BamlNodeType.LiteralContent:
                    {
                        // if everything set to inherit, we just return the inheritable localizability
                        if (localizabilityFromSource.Category == LocalizationCategory.Inherit
                          && localizabilityFromSource.Readability == Readability.Inherit
                          && localizabilityFromSource.Modifiability == Modifiability.Inherit)
                        {
                            // just propagate the ancestor's localizability.
                            node.InheritableAttribute = inheritableAttribute;
                        }
                        else
                        {
                            // set new inherited values
                            node.InheritableAttribute = CreateInheritedLocalizability(
                                localizabilityFromSource,
                                inheritableAttribute
                                );
                        }
                        break;
                    }
                case BamlNodeType.Property:
                case BamlNodeType.StartComplexProperty:
                    {
                        ILocalizabilityInheritable parent = (ILocalizabilityInheritable)treeNode.Parent;

                        // Find the mininum localizability of the containing class and 
                        // parent property. Parent property means the proeprty from parent node that 
                        // has the same name.
                        LocalizabilityAttribute inheritedAttribute = CombineMinimumLocalizability(
                            inheritableAttribute,
                            parent.InheritableAttribute
                            );

                        node.InheritableAttribute = CreateInheritedLocalizability(
                            localizabilityFromSource,
                            inheritedAttribute
                            );

                        if (parent.IsIgnored && localizabilityFromSource.Category == LocalizationCategory.Inherit)
                        {
                            // If the parent node is Ignore and this property is set to inherit, then 
                            // this property node is to be ignore as well. We set the the "Ignore" flag so that
                            // the node will always be ignored without looking at the source localizability again. 
                            node.IsIgnored = true;
                            return LocalizabilityIgnore;
                        }
                        break;
                    }
                default:
                    {
                        Debug.Assert(false, "Can't process localizability attribute on nodes other than Element, Property and LiteralContent.");
                        break;
                    }
            }

            return node.InheritableAttribute;
        }

        /// <summary>
        /// Create the inherited localizability attribute 
        /// </summary>
        /// <param name="source">localizability attribute defined in source</param>
        /// <param name="inheritable">localizability attribute inheritable from above</param>
        /// <returns>LocalizabilityAttribute</returns>
        private LocalizabilityAttribute CreateInheritedLocalizability(
            LocalizabilityAttribute source,
            LocalizabilityAttribute inheritable
            )
        {
            LocalizationCategory category =
                (source.Category == LocalizationCategory.Inherit) ?
                inheritable.Category :
                source.Category;

            Readability readability =
                (source.Readability == Readability.Inherit) ?
                inheritable.Readability :
                source.Readability;

            Modifiability modifiability =
                (source.Modifiability == Modifiability.Inherit) ?
                inheritable.Modifiability :
                source.Modifiability;

            LocalizabilityAttribute attribute = new LocalizabilityAttribute(category);
            attribute.Readability = readability;
            attribute.Modifiability = modifiability;
            return attribute;
        }


        /// <summary>
        /// It combines the min values of two localizability attributes. 
        /// </summary>
        /// <param name="first">first </param>
        /// <param name="second">second</param>
        /// <returns>LocalizabilityAttribute</returns>
        private LocalizabilityAttribute CombineMinimumLocalizability(
            LocalizabilityAttribute first,
            LocalizabilityAttribute second
            )
        {
            if (first == null || second == null)
            {
                return (first == null) ? second : first;
            }

            // min of two readability enum. The less the more restrictive.
            Readability readability = (Readability)Math.Min(
                (int)first.Readability,
                (int)second.Readability
                );

            // min of two Modifiability enum. The less the more restrictive.
            Modifiability modifiability = (Modifiability)Math.Min(
                (int)first.Modifiability,
                (int)second.Modifiability
                );

            // for category, NeverLocalize < Ignore < { all others } < None
            // If both categories belong to { all others }, first.Category wins
            LocalizationCategory category = LocalizationCategory.None;

            if (first.Category == LocalizationCategory.NeverLocalize
              || second.Category == LocalizationCategory.NeverLocalize)
            {
                category = LocalizationCategory.NeverLocalize;
            }
            else if (first.Category == LocalizationCategory.Ignore
                   || second.Category == LocalizationCategory.Ignore)
            {
                category = LocalizationCategory.Ignore;
            }
            else
            {
                category = (first.Category != LocalizationCategory.None) ?
                    first.Category :
                    second.Category;
            }

            LocalizabilityAttribute result = new LocalizabilityAttribute(category);
            result.Readability = readability;
            result.Modifiability = modifiability;

            return result;
        }

        //--------------------------------
        // private members
        //--------------------------------
        private InternalBamlLocalizabilityResolver _resolver;
        private readonly LocalizabilityAttribute LocalizabilityIgnore = new LocalizabilityAttribute(LocalizationCategory.Ignore);
    }
}
