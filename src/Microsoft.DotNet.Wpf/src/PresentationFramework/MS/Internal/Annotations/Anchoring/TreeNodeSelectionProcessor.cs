// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     TreeNodeSelectionProcessor is a selection processor 
//     that can handle Logical Tree Nodes as selections.
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using MS.Utility;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     TreeNodeSelectionProcessor is a selection processor 
    ///     that can handle Logical Tree Nodes as selections.
    /// </summary>  
    internal sealed class TreeNodeSelectionProcessor : SelectionProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of TreeNodeSelectionProcessor.
        /// </summary>
        public TreeNodeSelectionProcessor()
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
        /// <returns>always returns false, Logical Tree Nodes cannot be merged 
        /// </returns>
        /// <exception cref="ArgumentNullException">selection1 or selection2 are 
        /// null</exception>
        public override bool MergeSelections(Object selection1, Object selection2, out Object newSelection)
        {
            if (selection1 == null)
                throw new ArgumentNullException(nameof(selection1));

            if (selection2 == null)
                throw new ArgumentNullException(nameof(selection2));

            newSelection = null;
            return false;
        }

        /// <summary>
        ///     Gets the tree elements spanned by the selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>a list of elements spanned by the selection; never returns 
        /// null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override IList<DependencyObject> GetSelectedNodes(Object selection)
        {
            return new DependencyObject[] { GetParent(selection) };
        }

        /// <summary>
        ///     Gets the parent element of this selection.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the parent element of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override UIElement GetParent(Object selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            UIElement element = selection as UIElement;
            if (element == null)
            {
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), nameof(selection));
            }

            return element;
        }

        /// <summary>
        ///     Gets the anchor point for the selection
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the anchor point of the selection; can be null</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override Point GetAnchorPoint(Object selection)
        {
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            Visual element = selection as Visual;

            if (element == null)
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), nameof(selection));

            // get the Visual's bounding rectangle's let, top and store them in a point
            Rect rect = element.VisualContentBounds;

            return new Point(rect.Left, rect.Top);
        }

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
        public override IList<ContentLocatorPart> GenerateLocatorParts(Object selection, DependencyObject startNode)
        {
            if (startNode == null)
                throw new ArgumentNullException(nameof(startNode));

            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            return new List<ContentLocatorPart>(0);
        }

        /// <summary>
        ///     Creates a selection object spanning the portion of 'startNode' 
        ///     specified by 'locatorPart'.
        /// </summary>
        /// <param name="locatorPart">locator part specifying data to be spanned</param>
        /// <param name="startNode">the node to be spanned by the created 
        /// selection</param>
        /// <param name="attachmentLevel">always set to AttachmentLevel.Full</param>
        /// <returns>a selection spanning the portion of 'startNode' specified by     
        /// 'locatorPart'</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect type</exception>
        public override Object ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out AttachmentLevel attachmentLevel)
        {
            if (startNode == null)
                throw new ArgumentNullException(nameof(startNode));

            if (locatorPart == null)
                throw new ArgumentNullException(nameof(locatorPart));

            attachmentLevel = AttachmentLevel.Full;

            return startNode;
        }

        /// <summary>
        ///     Returns a list of XmlQualifiedNames representing the
        ///     the locator parts this processor can resolve/generate.
        /// </summary>
        public override XmlQualifiedName[] GetLocatorPartTypes()
        {
            return (XmlQualifiedName[])LocatorPartTypeNames.Clone();
        }

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
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // ContentLocatorPart types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames = Array.Empty<XmlQualifiedName>();

        #endregion Private Fields        
    }
}
