// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose: Callback at parse time for node processing
*
\***************************************************************************/

using System.ComponentModel;

using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System;
using System.Xml;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// Describes the action the parser is to take after it 
    /// has called back to the ParserHooks
    /// </summary>
    internal enum ParserAction
    {
        /// <summary>
        /// parser should do normal processing
        /// </summary>
        Normal,  

        /// <summary>
        /// Parser should not process this node.
        ///   If the current node is an Element, skip the current node and all of its children
        ///   If the current node is an attribute,skip to the next attribute
        /// </summary>
        Skip
    }

    /// <summary>
    /// The base class for the parse time callbacks.
    /// </summary>
    /// <remarks>
    /// The localization team will use this under two scenarios
    /// 1. The Uid generation tool wants to know the different xaml nodes and their positions in a xaml file
    /// 2. Used to strip out the localization attributes during compilation to Baml
    /// </remarks>
    internal abstract class ParserHooks  
    {
        /// <summary>
        /// Called by parser after it determines what node type for
        /// the XML Node and has tokenized the xml node content. 
        /// </summary>
        /// <remarks>
        /// Node types are Resources, Code: Element Object, properties, events etc.
        /// The return value is a ParserAction value which indicates if the parser
        /// should: continue normal processing; skip this node and any children
        /// </remarks>
        internal virtual ParserAction LoadNode(XamlNode  tokenNode)
        {
            return ParserAction.Normal;
        }
    }
}

