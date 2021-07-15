// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//     A simple subclass of DescendentsWalker which introduces a second callback
//     which is called after a node's children have been visited.
//


using System;
using System.Diagnostics;
using System.Windows;
using MS.Utility;

namespace MS.Internal
{
    /// <summary>
    ///     A simple subclass of DescendentsWalker which introduces a second callback
    ///     which is called after a node's children have been visited.
    /// </summary>
    internal class PrePostDescendentsWalker<T> : DescendentsWalker<T>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instances of PrePostDescendentsWalker.
        /// </summary>
        /// <param name="priority">specifies which tree should be visited first</param>
        /// <param name="preCallback">the callback to be called before a node's children are visited</param>
        /// <param name="postCallback">the callback to be called after a node's children are visited</param>
        /// <param name="data">the data passed to each callback</param>
        public PrePostDescendentsWalker(TreeWalkPriority priority, VisitedCallback<T> preCallback, VisitedCallback<T> postCallback, T data) : 
            base(priority, preCallback, data)
        {
            _postCallback = postCallback;
        }

        #endregion Constructors 

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Starts the walking process for the given node.
        /// </summary>
        /// <param name="startNode">the node to start the walk on</param>
        /// <param name="skipStartNode">whether or not the first node should have the callbacks called on it</param>
        public override void StartWalk(DependencyObject startNode, bool skipStartNode)
        {
            try
            {
                base.StartWalk(startNode, skipStartNode);
            }
            finally
            {
                if (!skipStartNode)
                {
                    if (_postCallback != null)
                    {
                        // This type checking is done in DescendentsWalker.  Doing it here
                        // keeps us consistent.
                        if (FrameworkElement.DType.IsInstanceOfType(startNode) || FrameworkContentElement.DType.IsInstanceOfType(startNode))
                        {
                            _postCallback(startNode, this.Data, _priority == TreeWalkPriority.VisualTree);
                        }
                    }
                }
            }
        }

        #endregion Public Methods
        
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        ///     This method is called for every node touched during a walking of 
        ///     the tree.  Some nodes may not have this called if the preCallback
        ///     returns false - thereby preventing its subtree from being visited.
        /// </summary>
        /// <param name="d">the node to visit</param>
        protected override void _VisitNode(DependencyObject d, bool visitedViaVisualTree)
        {
            try
            {
                base._VisitNode(d, visitedViaVisualTree);
            }
            finally
            {
                if (_postCallback != null)
                {
                    _postCallback(d, this.Data, visitedViaVisualTree);
                }
            }
        }

        #endregion Protected Methods      
        
        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------        

        #region Private Properties

        private VisitedCallback<T> _postCallback;

        #endregion Private Properties
    }
}
