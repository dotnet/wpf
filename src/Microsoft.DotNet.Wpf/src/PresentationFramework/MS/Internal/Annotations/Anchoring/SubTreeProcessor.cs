// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     SubTreeProcessor is an abstract class defining the API for 
//     processing an element tree (walking the tree and loading 
//     annotations), generating locators, and generating query
//     fragments for locator parts.  
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Media;
using MS.Utility;
using System.Xml;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     SubTreeProcessor is an abstract class defining the API for 
    ///     processing an element tree (walking the tree and loading 
    ///     annotations), generating locators, and generating query
    ///     fragments for locator parts.  
    /// </summary>
    internal abstract class SubTreeProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of SubTreeProcessor.  Subclasses should
        ///     pass the manager that created and owns them.
        /// </summary>
        /// <param name="manager">the manager that owns this processor</param>
        /// <exception cref="ArgumentNullException">manager is null</exception>
        protected SubTreeProcessor(LocatorManager manager)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            _manager = manager;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Gives the processor a chance to process annotations for this node before its
        ///     children are processed.  If calledProcessAnnotations is set to true then the
        ///     children will not be individually processed.  
        /// </summary>
        /// <param name="node">node to process</param>
        /// <param name="calledProcessAnnotations">indicates the callback was called by
        /// this processor</param>
        /// <returns>
        ///     a list of AttachedAnnotations loaded during the processing of
        ///     this node; can be null if no annotations were loaded
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public abstract IList<IAttachedAnnotation> PreProcessNode(DependencyObject node, out bool calledProcessAnnotations);

        /// <summary>
        ///   This method is always called after PreProcessSubTree and after the node's children have 
        ///   been processed (unless that step was skipped because PreProcessSubtree returned true for
        ///   calledProcessAnnotations).
        /// </summary>
        /// <param name="node">the node that is being processed</param>
        /// <param name="childrenCalledProcessAnnotations">the combined value (||'d) of calledProcessAnnotations 
        /// on this node's subtree;  in the case when the children were not individually processed, false is
        /// passed in</param>
        /// <param name="calledProcessAnnotations">indicates the callback was called by this processor</param>
        /// <returns>list of AttachedAnnotations loaded during the post processing of this node;  can
        /// be null if no annotations were loaded</returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public virtual IList<IAttachedAnnotation> PostProcessNode(DependencyObject node, bool childrenCalledProcessAnnotations, out bool calledProcessAnnotations)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            calledProcessAnnotations = false;

            // do nothing here
            return null;
        }

        /// <summary>
        ///     Generates a locator part list identifying node.  
        /// </summary>
        /// <remarks>
        ///     Most subclasses will simply return a ContentLocator with one locator
        ///     part in it.  In some cases, more than one locator part may be
        ///     required (e.g., a node which represents path to some data may
        ///     need a separate locator part for each portion of the path).
        /// </remarks>
        /// <param name="node">the node to generate a locator for</param>
        /// <param name="continueGenerating">specifies whether or not generating should 
        /// continue for the rest of the path; a SubTreeProcessor could return false if
        /// it processed the rest of the path itself </param>
        /// <returns>
        ///     a locator identifying 'node'; in most cases this locator will 
        ///     only contain one locator part; can return null if no 
        ///     locator part can be generated for the given node
        /// </returns>
        /// <exception cref="ArgumentNullException">node is null</exception>
        public abstract ContentLocator GenerateLocator(PathNode node, out bool continueGenerating);

        /// <summary>
        ///     Searches the logical tree for a node matching the values of 
        ///     locatorPart.  The search begins with startNode.  
        /// </summary>
        /// <remarks>
        ///     Subclasses can choose to only examine startNode or traverse
        ///     the logical tree looking for a match.  The algorithms for some
        ///     locator part types may require a search of the tree instead of
        ///     a simple examination of one node.
        /// </remarks>
        /// <param name="locatorPart">locator part to be matched, must be of the type 
        /// handled by this processor</param>
        /// <param name="startNode">logical tree node to start search at</param>
        /// <param name="continueResolving">return flag indicating whether the search 
        /// should continue (presumably because the search was not exhaustive)</param>
        /// <returns>returns a node that matches the locator part; null if no such 
        /// node is found</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect 
        /// type</exception>
        public abstract DependencyObject ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out bool continueResolving);

        /// <summary>
        ///     Returns a list of XmlQualifiedNames representing the
        ///     the locator parts this processor can resolve/generate.
        /// </summary>
        public abstract XmlQualifiedName[] GetLocatorPartTypes();

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

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        /// <summary>
        ///     The manager that created and owns this processor.
        /// </summary>
        protected LocatorManager Manager
        {
            get { return _manager; }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // LocatorProcessingManager that created this subtree processor
        private LocatorManager _manager;

        #endregion Private Fields
    }
}
