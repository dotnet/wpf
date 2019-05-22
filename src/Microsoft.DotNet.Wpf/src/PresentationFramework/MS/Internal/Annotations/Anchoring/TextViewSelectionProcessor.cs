// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     A SelectionProcessor subclass which produces locator parts that select
//     all anchors that intersect with the text in an element's TextView.      
//

using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;
using MS.Utility;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using MS.Internal.Documents;
using MS.Internal.PtsHost;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    /// </summary>  
    internal class TextViewSelectionProcessor : SelectionProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of TextViewSelectionProcessor.
        /// </summary>
        public TextViewSelectionProcessor()
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
        ///     Not implemented for this selection processor.
        /// </summary>
        /// <param name="selection1">selection to merge </param>
        /// <param name="selection2">other selection to merge </param>
        /// <param name="newSelection">always set to null because DocumentPageViews cannot be merged</param>
        /// <returns>always returns false because DocumentPageViewss cannot be merged</returns>
        public override bool MergeSelections(Object selection1, Object selection2, out Object newSelection)
        {
            newSelection = null;
            return false;
        }


        /// <summary>
        ///     Simply returns the selection passed in which should be an element.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>a list containing the selection</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override IList<DependencyObject> GetSelectedNodes(Object selection)
        {
            // Verify selection is a service provider that provides ITextView
            VerifySelection(selection);

            return new DependencyObject[] { (DependencyObject)selection };
        }

        /// <summary>
        ///     Simply returns the selection which should be an element.
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <returns>the selection itself</returns>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override UIElement GetParent(Object selection)
        {
            // Verify selection is a service provider that provides ITextView
            VerifySelection(selection);

            return (UIElement)selection;
        }


        /// <summary>
        ///     Gets the anchor point for the selection.  This isn't a runtime selection
        ///     and therefore shouldn't be anchored to.  This processor returns a point of
        ///     (Double.NaN, Double.NaN).
        /// </summary>
        /// <param name="selection">the selection to examine</param>
        /// <exception cref="ArgumentNullException">selection is null</exception>
        /// <exception cref="ArgumentException">selection is of wrong type</exception>
        public override Point GetAnchorPoint(Object selection)
        {
            // Verify selection is a DocumentPageView
            VerifySelection(selection);

            // Returns the point (Nan,Nan) 
            return new Point(double.NaN, double.NaN);
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
        /// <exception cref="ArgumentException">selection is of the wrong type</exception>
        public override IList<ContentLocatorPart>
            GenerateLocatorParts(Object selection, DependencyObject startNode)
        {
            if (startNode == null)
                throw new ArgumentNullException("startNode");

            List<ContentLocatorPart> res = null;

            ITextView textView = VerifySelection(selection);

            res = new List<ContentLocatorPart>(1);

            int startOffset;
            int endOffset;
            if (textView != null && textView.IsValid)
            {
                GetTextViewTextRange(textView, out startOffset, out endOffset);
            }
            else
            {
                // This causes no content to be loaded
                startOffset = -1;
                endOffset = -1;
            }

            ContentLocatorPart part = new ContentLocatorPart(TextSelectionProcessor.CharacterRangeElementName);// DocumentPageViewLocatorPart();
            part.NameValuePairs.Add(TextSelectionProcessor.CountAttribute, 1.ToString(NumberFormatInfo.InvariantInfo));
            part.NameValuePairs.Add(TextSelectionProcessor.SegmentAttribute + 0.ToString(NumberFormatInfo.InvariantInfo), startOffset.ToString(NumberFormatInfo.InvariantInfo) + TextSelectionProcessor.Separator[0] + endOffset.ToString(NumberFormatInfo.InvariantInfo));
            part.NameValuePairs.Add(TextSelectionProcessor.IncludeOverlaps, Boolean.TrueString);

            res.Add(part);

            return res;
        }

        /// <summary>
        ///     This processor doesn't resolve ContentLocatorParts.  It simply returns null.
        /// </summary>
        /// <param name="locatorPart">locator part specifying data to be spanned</param>
        /// <param name="startNode">the node to be spanned by the created 
        /// selection</param>
        /// <param name="attachmentLevel">always set to AttachmentLevel.Unresolved</param>
        /// <returns>always returns null; this processor does not resolve ContentLocatorParts</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are null</exception>
        public override Object ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out AttachmentLevel attachmentLevel)
        {
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            if (startNode == null)
                throw new ArgumentNullException("startNode");

            attachmentLevel = AttachmentLevel.Unresolved;

            // ContentLocator Parts generated by this selection processor cannot be resolved
            return null;
        }

        /// <summary>
        ///     Returns a list of XmlQualifiedNames representing the
        ///     the locator parts this processor can generate.  This processor
        ///     does not resolve these ContentLocatorParts - only generates them.
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
        //  Internal Methods
        //
        //------------------------------------------------------        

        #region Internal Methods

        /// <summary>
        /// Returns the TextRange representing the visible contents of the ITextView.
        /// Also returns the same information in terms of the offsets to the start and end.
        /// </summary>
        /// <param name="textView">ITextView to get visible contents of</param>
        /// <param name="startOffset">offset into the document representing start of visible content</param>
        /// <param name="endOffset">offset into the document representing end of visible content</param>
        /// <returns>TextRange spanning all visible content</returns>
        internal static TextRange GetTextViewTextRange(ITextView textView, out int startOffset, out int endOffset)
        {
            Debug.Assert(textView != null);

            // These are the default in case we don't find any content in the DocumentPageView
            startOffset = int.MinValue;
            endOffset = 0;

            TextRange textRange = null;
            IList<TextSegment> segments = textView.TextSegments;
            if (segments != null && segments.Count > 0)
            {
                ITextPointer start = segments[0].Start;
                ITextPointer end = segments[segments.Count - 1].End;

                startOffset = end.TextContainer.Start.GetOffsetToPosition(start);
                endOffset = end.TextContainer.Start.GetOffsetToPosition(end);

                textRange = new TextRange(start, end);
            }
            return textRange;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------        

        #region Private Methods        

        /// <summary>
        ///   Verify the selection is of the correct type.
        /// </summary>
        /// <param name="selection">selection to verify</param>
        /// <returns>the selection cast to the necessary type</returns>
        private ITextView VerifySelection(object selection)
        {
            if (selection == null)
                throw new ArgumentNullException("selection");

            IServiceProvider provider = selection as IServiceProvider;
            if (provider == null)
                throw new ArgumentException(SR.Get(SRID.SelectionMustBeServiceProvider), "selection");

            ITextView textView = provider.GetService(typeof(ITextView)) as ITextView;

            return textView;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // ContentLocator part types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames = new XmlQualifiedName[0];

        #endregion Private Fields
    }
}
