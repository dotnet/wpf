// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Debug-only helper class, dumps TextContainer state to debugger.
//

using System;
using System.Diagnostics;

namespace System.Windows.Documents
{
#if DEBUG
    // The TextTreeDumper is a helper class, used to dump TextTrees
    // or TextTreeNodes to the debugger.
    //
    // Use it like:
    //
    //  TextTreeDumper.DumpTree(texttree);
    //  TextTreeDumper.DumpNode(someNode);
    //
    internal class TextTreeDumper
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

    #region Constructors
 
        // Static class.
        private TextTreeDumper()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Dumps a TextContainer.
        internal static void Dump(TextContainer tree)
        {
            if (tree.RootNode == null)
            {
                Debug.WriteLine("<" + tree + "/>");
            }
            else
            {
                DumpNodeRecursive(tree.RootNode, 0);
            }
        }

        // Dumps a TextContainer, xaml style.
        internal static void DumpFlat(TextContainer tree)
        {
            if (tree.RootNode == null)
            {
                Debug.WriteLine("<" + tree + "/>");
            }
            else
            {
                DumpFlat(tree.RootNode);
            }
        }

        // Dumps a node and all its children.
        internal static void Dump(TextTreeNode node)
        {
            DumpNodeRecursive(node, 0);
        }

        // Dumps a node and all contained nodes "flat" -- in notation similar to xaml.
        internal static void DumpFlat(TextTreeNode node)
        {
            Debug.Write("<" + GetFlatPrefix(node) + node.DebugId);

            if (node.ContainedNode != null)
            {
                Debug.Write(">");
                DumpNodeFlatRecursive(node.GetFirstContainedNode());
                Debug.WriteLine("</" + GetFlatPrefix(node) + node.DebugId + ">");
            }
            else
            {
                Debug.WriteLine("/>");
            }
        }

        // Dumps a TextPointer or TextNavigator, including symbol offset.
        internal static void Dump(TextPointer position)
        {
            Debug.WriteLine("Offset: " + position.GetSymbolOffset() + " " + position.ToString());
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Recursive worker to DumpNode.
        internal static void DumpNodeRecursive(SplayTreeNode node, int depth)
        {
            SplayTreeNode containedNode;
            string indent;

            indent = new string(' ', depth*2);

            Debug.Write("<");
            Debug.Write(node);

            if (node.ContainedNode == null)
            {
                Debug.WriteLine("/>");
            }
            else
            {
                Debug.WriteLine(">");
            }

            containedNode = node.ContainedNode;
            if (containedNode != null)
            {
                Debug.Write(indent + "C ");
                DumpNodeRecursive(containedNode, depth + 1);
            }

            if (node.LeftChildNode != null)
            {
                Debug.Write(indent + "L ");
                DumpNodeRecursive(node.LeftChildNode, depth + 1);
            }

            if (node.RightChildNode != null)
            {
                Debug.Write(indent + "R ");
                DumpNodeRecursive(node.RightChildNode, depth + 1);
            }

            if (containedNode != null)
            {
                if (node is TextTreeRootNode)
                {
                    Debug.WriteLine(indent + "</RootNode>");
                }
                else if (node is TextTreeTextElementNode)
                {
                    Debug.WriteLine(indent + "</TextElementNode>");
                }
                else
                {
                    Debug.WriteLine(indent + "</UnknownNode>");
                }
            }
        }

        // Dumps a node and all following nodes "flat" -- in notation similar to xaml.
        internal static void DumpNodeFlatRecursive(SplayTreeNode node)
        {
            for (; node != null; node = node.GetNextNode())
            {
                Debug.Write("<" + GetFlatPrefix(node) + node.DebugId);

                if (node.ContainedNode != null)
                {
                    Debug.Write(">");
                    DumpNodeFlatRecursive(node.GetFirstContainedNode());
                    Debug.Write("</" + GetFlatPrefix(node) + node.DebugId + ">");
                }
                else
                {
                    Debug.Write("/>");
                }
            }

        }

        // Returns a string identifying the type of a given node.
        private static string GetFlatPrefix(SplayTreeNode node)
        {
            string prefix;

            if (node is TextTreeTextNode)
            {
                prefix = "t";
            }
            else if (node is TextTreeObjectNode)
            {
                prefix = "o";
            }
            else if (node is TextTreeTextElementNode)
            {
                prefix = "e";
            }
            else if (node is TextTreeRootNode)
            {
                prefix = "r";
            }
            else
            {
                prefix = "?";
            }

            return prefix;
        }

        #endregion Private Methods
    }
#endif // DEBUG
}
