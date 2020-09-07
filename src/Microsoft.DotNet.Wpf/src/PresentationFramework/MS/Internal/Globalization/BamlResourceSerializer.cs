// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Class that implements BamlResourceSerializer


using System;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Markup.Localizer;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace MS.Internal.Globalization
{
    /// <summary>
    /// BamlResourceSerializer
    /// </summary>
    internal sealed class BamlResourceSerializer
    {
        //-------------------------------
        // Internal static
        //-------------------------------
        internal static void Serialize(BamlLocalizer localizer, BamlTree tree, Stream output)
        {
            // Thread safe implementation
            (new BamlResourceSerializer()).SerializeImp(localizer, tree, output);
        }


        //----------------------------------
        // constructor.
        //----------------------------------
        /// <summary>
        /// constructor
        /// </summary>
        private BamlResourceSerializer()
        {
        }

        //----------------------------------
        // private method
        //----------------------------------

        /// <summary>
        /// Serialize the tree out to the stream.
        /// </summary>
        private void SerializeImp(
            BamlLocalizer localizer,
            BamlTree tree,
            Stream output
            )
        {
            Debug.Assert(output != null, "The output stream given is null");
            Debug.Assert(tree != null && tree.Root != null, "The tree to be serialized is null.");

            _writer = new BamlWriter(output);
            _bamlTreeStack = new Stack<BamlTreeNode>();

            // intialize the stack.
            _bamlTreeStack.Push(tree.Root);

            while (_bamlTreeStack.Count > 0)
            {
                BamlTreeNode currentNode = _bamlTreeStack.Pop();
                if (!currentNode.Visited)
                {
                    // Mark this node so that it won't be serialized again.
                    currentNode.Visited = true;
                    currentNode.Serialize(_writer);
                    PushChildrenToStack(currentNode.Children);
                }
                else
                {
                    BamlStartElementNode elementNode = currentNode as BamlStartElementNode;
                    Debug.Assert(elementNode != null);

                    if (elementNode != null)
                    {
                        localizer.RaiseErrorNotifyEvent(
                            new BamlLocalizerErrorNotifyEventArgs(
                                BamlTreeMap.GetKey(elementNode),
                                BamlLocalizerError.DuplicateElement
                            )
                        );
                    }
                }
            }

            // do not close stream as we don't own it.            
        }

        private void PushChildrenToStack(List<BamlTreeNode> children)
        {
            if (children == null)
                return;

            for (int i = children.Count - 1; i >= 0; i--)
            {
                _bamlTreeStack.Push(children[i]);
            }
        }

        //---------------------------------
        // private 
        //---------------------------------
        private BamlWriter _writer;
        private Stack<BamlTreeNode> _bamlTreeStack;
    }
}
