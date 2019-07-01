// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.Test.ObjectComparison
{
    /// <summary>
    /// Represents one node in the object graph.
    /// The root of the graph is a graph node.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class GraphNode
    {
        #region Public and Protected Members

        /// <summary>
        /// A name to identify this node. When comparing,
        /// the left and right nodes are matched by name.
        /// </summary>
        public string Name 
        { 
            get
            {
                return this.name;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Please provide a valid name", "value");
                }
                this.name = value;
            }
        }

        /// <summary>
        /// Gets the collection of child nodes to this
        /// node. The child nodes represent properties or
        /// fields on an object.
        /// </summary>
        public Collection<GraphNode> Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new Collection<GraphNode>();
                }

                return children;
            }
        }

        /// <summary>
        /// Represents the immediate parent to this node.
        /// The parent node of the root of the graph is null. 
        /// </summary>
        public GraphNode Parent { get; set; }

        /// <summary>
        /// Contains the value of the object represented by
        /// this node.
        /// </summary>
        public object ObjectValue { get; set; }

        /// <summary>
        /// Provides the System.Type of the object represented by 
        /// this node. Returns null if the object value is null.
        /// </summary>
        public Type ObjectType 
        {
            get
            {
                Type objectType = null;
                if (this.ObjectValue != null)
                {
                    objectType = this.ObjectValue.GetType();
                }

                return objectType;
            }
        }

        /// <summary>
        /// Gets the depth of this node from the root. If the depth of
        /// the root node is 0, root.child is 1.
        /// </summary>
        public int Depth
        {
            get
            {
                GraphNode node = this;
                int depth = 0;
                while (node.Parent != null)
                {
                    depth++;
                    node = node.Parent;
                }
                return depth;
            }
        }

        /// <summary>
        /// Gets the fully qualified name of this node
        /// If the root node has a child that is named child1 and the child
        /// has another child that is named child12, qualified name of child12
        /// would be root.child1.child12.
        /// </summary>
        public string QualifiedName
        {
            get
            {
                GraphNode node = this;
                string qualifiedName = this.Name;
                while (node.Parent != null)
                {
                    qualifiedName = node.Parent.Name + "." + qualifiedName;
                    node = node.Parent;
                }
                return qualifiedName;
            }
        }

        /// <summary>
        /// Performs a depth-first traversal of the graph with 
        /// this node as root, and provides the nodes visited, in that
        /// order.
        /// </summary>
        /// <returns>Nodes visited in depth-first order.</returns>
        public IEnumerable<GraphNode> GetNodesInDepthFirstOrder()
        {
            Stack<GraphNode> pendingNodes = new Stack<GraphNode>();
            pendingNodes.Push(this);
            HashSet<GraphNode> visitedNodes = new HashSet<GraphNode>();
            while (pendingNodes.Count != 0)
            {
                GraphNode currentNode = pendingNodes.Pop();
                if (!visitedNodes.Contains(currentNode))
                {
                    foreach (GraphNode node in currentNode.Children)
                    {
                        pendingNodes.Push(node);
                    }
                    visitedNodes.Add(currentNode);
                    yield return currentNode;
                }
            }
        }

        #endregion

        #region Private Data

        private Collection<GraphNode> children;
        private string name;

        #endregion
    }
}