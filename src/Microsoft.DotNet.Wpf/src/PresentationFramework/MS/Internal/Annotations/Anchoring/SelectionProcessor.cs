// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     SelectionProcessor is an abstract class defining the API for
//     creating and processing selections in the annotation framework.  
//     SelectionProcessors are responsible for creating locator parts representing
//     a selection and for creating selections based on a locator part.
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Media;
using MS.Utility;
using System.Xml;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     SelectionProcessor is an abstract class defining the API for
    ///     creating and processing selections in the annotation framework.  
    ///     SelectionProcessors are responsible for creating locator parts representing
    ///     a selection and for creating selections based on a locator part.
    /// </summary> 
    internal abstract class SelectionProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of SelectionProcessor.  
        /// </summary>
        protected SelectionProcessor()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods     

        /// <summary>
        ///     Merges the two selections into one, if possible.
        /// </summary>
        /// <param name="selection1">selection to merge </param>
        /// <param name="selection2">other selection to merge </param>
        /// <param name="newSelection">new selection that contains the data from both 
        /// selection1 and selection2</param>
        /// <returns>true if the selections were merged, false otherwise 
        /// </returns>
        /// <exception cref="ArgumentNullException">selection1 or selection2 are 
        /// null</exception>
        public abstract bool MergeSelections(Object selection1, Object selection2, out Object newSelection);

        /// <summary>
        ///     Gets the tree elements spanned by the selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>a list of elements spanned by the selection; never returns 
        /// null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public abstract IList<DependencyObject> GetSelectedNodes(Object selection);

        /// <summary>
        ///     Gets the parent element of this selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the parent element of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public abstract UIElement GetParent(Object selection);

        /// <summary>
        ///     Gets the anchor point for the selection
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the anchor point of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public abstract Point GetAnchorPoint(Object selection);

        /// <summary>
        ///     Creates one or more locator parts representing the portion
        ///     of 'startNode' spanned by 'selection'.
        /// </summary>
        /// <param name="selection">the selection that is being processed</param>
        /// <param name="startNode">the node the locator parts should be in the 
        /// context of</param>
        /// <returns>one or more locator parts representing the portion of 'startNode' spanned 
        /// by 'selection'</returns>
        /// <exception cref="ArgumentNullException">startNode or selection is null</exception>
        /// <exception cref="ArgumentException">startNode is not a DependencyObject or 
        /// selection is of the wrong type</exception>
        public abstract IList<ContentLocatorPart> GenerateLocatorParts(Object selection, DependencyObject startNode);

        /// <summary>
        ///     Creates a selection object spanning the portion of 'startNode' 
        ///     specified by 'locatorPart'.
        /// </summary>
        /// <param name="locatorPart">locator part specifying data to be spanned</param>
        /// <param name="startNode">the node to be spanned by the created 
        /// selection</param>
        /// <param name="attachmentLevel">describes the level of resolution reached when resolving the locator part</param>
        /// <returns>a selection spanning the portion of 'startNode' specified by     
        /// 'locatorPart'</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect type</exception>
        public abstract Object ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out AttachmentLevel attachmentLevel);

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
        //  Public Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
    }
}
