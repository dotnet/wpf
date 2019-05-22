// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 1634, 1691
//
//
// Description:
//     PathNode represents a node in a path (a subset of the element tree).
//     PathNodes can have other PathNodes as children.  Each refers to a
//     single element in the element tree.
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Media;
using System.Windows.Markup;
using MS.Utility;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     PathNode represents a node in a path (a subset of the element tree).
    ///     PathNodes can have other PathNodes as children.  Each refers to a
    ///     single element in the element tree.
    /// </summary>
    internal sealed class PathNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of PathNode that refers to the specified tree node.    
        /// </summary>
        /// <param name="node">the tree node represented by this instance </param>
        /// <exception cref="ArgumentNullException">node is null</exception>
        internal PathNode(DependencyObject node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            _node = node;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Determines if obj is a PathNode and refers to the same tree node
        ///     as this instance.   
        /// </summary>
        /// <param name="obj">the Object to test for equality </param>
        /// <returns>true if obj refers to the same tree node as this instance </returns>
        public override bool Equals(Object obj)
        {
            PathNode otherNode = obj as PathNode;
            if (otherNode == null)
                return false;

            return _node.Equals(otherNode.Node);
        }

        /// <summary>
        ///     Generates a hash value for this PathNode based on the tree node 
        ///     it refers to. 
        /// </summary>
        /// <returns>a hash value for this instance based on its tree node</returns>
        public override int GetHashCode()
        {
            if (_node == null)
                return base.GetHashCode();

            return _node.GetHashCode();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        ///     Returns the tree node referred to by this instance of PathNode.
        /// </summary>
        /// <returns>the tree node referred to by this instance</returns>
        public DependencyObject Node
        {
            get { return _node; }
        }

        /// <summary>
        ///     Returns a list of PathNodes that are children of this instance.  
        ///     This set of children is a subset of the children of the tree node 
        ///     referred to by this PathNode.
        /// </summary>
        /// <returns>list of PathNodes that are children of this instance</returns>
        public IList Children
        {
            get
            {
                return _children;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///     Builds a path of PathNodes representing the nodes between all of 
        ///     the nodes and the root of the tree.
        /// </summary>
        /// <returns>the instance referring to the root of the tree; its 
        /// children/descendants only include the nodes between the root and 
        /// all of the nodes</returns>
        /// <exception cref="ArgumentNullException">nodes is null</exception>
        internal static PathNode BuildPathForElements(ICollection nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException("nodes");

            PathNode firstPathNode = null;
            foreach (DependencyObject node in nodes)
            {
                PathNode branch = BuildPathForElement(node);
                if (firstPathNode == null)
                    firstPathNode = branch;
                else
                    AddBranchToPath(firstPathNode, branch);
            }

            // make all the children readonly so we do not need to 
            // lock the PathNode when getting the children
            if (firstPathNode != null)
                firstPathNode.FreezeChildren();

            return firstPathNode;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Property used to point content tree root's to their 'parent'.  
        /// For instance, the root of a PageViewer's content tree would point
        /// to the DocumentPaginator that is holding on to the tree.
        /// </summary>
#pragma warning suppress 7009
        internal static readonly DependencyProperty HiddenParentProperty = DependencyProperty.RegisterAttached("HiddenParent", typeof(DependencyObject), typeof(PathNode));

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Get the parent of the passed in object.
        /// </summary>
        /// <param name="node">the node whose parent is requested</param>
        /// <returns>the parent of this node where parent is defined to be the
        /// first FrameworkElement/FrameworkContentElemnt found walking up the
        /// node's parent chain, with a preference for the visual tree vs the
        /// logical tree</returns>
        internal static DependencyObject GetParent(DependencyObject node)
        {
            Debug.Assert(node != null, "node can not be null");

            DependencyObject current = node;
            DependencyObject parent = null;

            while (true)
            {
                // Try for hidden parent first above all others
                parent = (DependencyObject)current.GetValue(PathNode.HiddenParentProperty);

                if (parent == null)
                {
                    // Try for Visual parent
                    Visual visual = current as Visual;

                    if (visual != null)
                    {
                        // This is a Visual node, get parent
                        parent = VisualTreeHelper.GetParent(visual);
                    }
                }

                if (parent == null)
                {
                    // Try for Model parent
                    parent = LogicalTreeHelper.GetParent(current);
                }

                // Check if located a parent, if so, check if it's the correct type
                if ((parent == null) ||
                    FrameworkElement.DType.IsInstanceOfType(parent) ||
                    FrameworkContentElement.DType.IsInstanceOfType(parent))
                {
                    break;
                }

                // Parent found but not of correct type, continue
                current = parent;
                parent = null;
            }

            return parent;
        }

        /// <summary>
        ///      Builds a path from an element to the root of its tree.  Every
        ///      element in between the element and the root is added to the 
        ///      path.
        /// </summary>
        /// <param name="node">the element to build a path for</param>
        /// <returns>the PathNode instance referring to the root of the tree; its 
        /// children/descendants only include the nodes between the root and 
        /// node</returns>
        private static PathNode BuildPathForElement(DependencyObject node)
        {
            Debug.Assert(node != null, "node can not be null");

            PathNode childNode = null;
            while (node != null)
            {
                PathNode pathNode = new PathNode(node);

                if (childNode != null)
                    pathNode.AddChild(childNode);

                childNode = pathNode;

                // If we find a node that has the service set on it, we should stop 
                // after processing it.  For cases without a service like unit tests,
                // this node won't be found and we'll continue to the root.
                if (node.ReadLocalValue(AnnotationService.ServiceProperty) != DependencyProperty.UnsetValue)
                    break;

                node = PathNode.GetParent(node);
            }

            return childNode;
        }

        /// <summary>
        ///     Adds a branch to an existing path, removing any duplicate
        ///     nodes as necessary.  Assumes that both paths are full paths 
        ///     up to the root of the same tree.  If the paths are not full, 
        ///     the method will give incorrect results.  If the paths are 
        ///     full but belong to different trees (and therefore have 
        ///     different roots) the method will throw.
        /// </summary>
        /// <param name="path">path to add branch to</param>
        /// <param name="branch">branch to be added; should be a linear path 
        /// (no more than one child for any node)</param>
        /// <returns>the path with branch having been added in and duplicate 
        /// nodes pruned</returns>
        private static PathNode AddBranchToPath(PathNode path, PathNode branch)
        {
            Debug.Assert(path != null, "path can not be null");
            Debug.Assert(branch != null, "branch can not be null");

            // The paths must be in the same tree and therefore have the
            // same root.  
            Debug.Assert(path.Node.Equals(branch.Node), "path.Node is not equal to branch.Node");

            PathNode fp = path;
            PathNode sp = branch;

            // Continue down
            while (fp.Node.Equals(sp.Node) && sp._children.Count > 0)
            {
                // if the firstpath component equals the second path component
                // then we try to find the second path child component
                // inside the first path children
                bool found = false;
                PathNode branchNode = (PathNode)sp._children[0];

                foreach (PathNode fpn in fp._children)
                {
                    if (fpn.Equals(branchNode))
                    {
                        // if we found one we keep moving along both the first path and the second path
                        found = true;
                        sp = branchNode;
                        fp = fpn;
                        break;
                    }
                }

                if (found)
                    continue;

                // if we can not find the second path child in the first
                // path child, we then just add the second path child
                // to the set of first path children
                fp.AddChild(branchNode);
                break;
            }

            return path;
        }

        private void AddChild(object child)
        {
            _children.Add(child);
        }

        /// <summary>
        /// Once the node has been constructed via BuildPathForElements
        /// we can not modify any more the childeren. We make the 
        /// children trough the entire PathNode tree readonly
        /// </summary>
        private void FreezeChildren()
        {
            foreach (PathNode node in _children)
            {
                node.FreezeChildren();
            }
            _children = ArrayList.ReadOnly(_children);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The element pointed to by this PathNode
        private DependencyObject _node;

        // The array of children of this PathNode
        private ArrayList _children = new ArrayList(1);

        #endregion Private Fields
    }
}

