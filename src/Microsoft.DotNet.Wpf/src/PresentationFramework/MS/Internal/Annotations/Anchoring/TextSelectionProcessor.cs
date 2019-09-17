// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     TextSelectionProcessor uses TextAnchors to represent portions 
//     of text that are anchors.  It produces locator parts that 
//     represent these TextAnchors and can generate TextAnchors from
//     the locator parts.
//     Spec: Anchoring Namespace Spec.doc
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using MS.Utility;

using MS.Internal.Documents;

namespace MS.Internal.Annotations.Anchoring
{
    /// <summary>
    ///     TextSelectionProcessor uses TextAnchors to represent portions 
    ///     of text that are anchors.  It produces locator parts that 
    ///     represent these TextAnchors and can generate TextAnchors from
    ///     the locator parts.
    /// </summary>  
    internal sealed class TextSelectionProcessor : SelectionProcessor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     Creates an instance of TextSelectionProcessor.
        /// </summary>
        public TextSelectionProcessor()
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
        ///     Merges the two anchors into one, if possible.
        /// </summary>
        /// <param name="anchor1">anchor to merge </param>
        /// <param name="anchor2">other anchor to merge </param>
        /// <param name="newAnchor">new anchor that contains the data from both 
        /// anchor1 and anchor2</param>
        /// <returns>true if the anchors were merged, false otherwise 
        /// </returns>
        /// <exception cref="ArgumentNullException">anchor1 or anchor2 are 
        /// null</exception>
        public override bool MergeSelections(Object anchor1, Object anchor2, out Object newAnchor)
        {
            return TextSelectionHelper.MergeSelections(anchor1, anchor2, out newAnchor);
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
            return TextSelectionHelper.GetSelectedNodes(selection);
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
            return TextSelectionHelper.GetParent(selection);
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
            return TextSelectionHelper.GetAnchorPoint(selection);
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

            if (selection == null)
                throw new ArgumentNullException("selection");

            ITextPointer start;
            ITextPointer end;
            IList<TextSegment> textSegments = null;

            TextSelectionHelper.CheckSelection(selection, out start, out end, out textSegments);
            if (!(start is TextPointer))
                throw new ArgumentException(SR.Get(SRID.WrongSelectionType), "selection");

            ITextPointer elementStart;
            ITextPointer elementEnd;

            // If we can't get the start/end of the node then we can't generate a locator part
            if (!GetNodesStartAndEnd(startNode, out elementStart, out elementEnd))
                return null;

            if (elementStart.CompareTo(end) > 0)
                throw new ArgumentException(SR.Get(SRID.InvalidStartNodeForTextSelection), "startNode");

            if (elementEnd.CompareTo(start) < 0)
                throw new ArgumentException(SR.Get(SRID.InvalidStartNodeForTextSelection), "startNode");

            ContentLocatorPart part = new ContentLocatorPart(CharacterRangeElementName);

            int startOffset = 0;
            int endOffset = 0;
            for (int i = 0; i < textSegments.Count; i++)
            {
                GetTextSegmentValues(textSegments[i], elementStart, elementEnd, out startOffset, out endOffset);

                part.NameValuePairs.Add(SegmentAttribute + i.ToString(NumberFormatInfo.InvariantInfo), startOffset.ToString(NumberFormatInfo.InvariantInfo) + TextSelectionProcessor.Separator[0] + endOffset.ToString(NumberFormatInfo.InvariantInfo));
            }

            part.NameValuePairs.Add(CountAttribute, textSegments.Count.ToString(NumberFormatInfo.InvariantInfo));

            List<ContentLocatorPart> res = new List<ContentLocatorPart>(1);

            res.Add(part);

            return res;
        }

        /// <summary>
        ///     Creates a selection object spanning the portion of 'startNode' 
        ///     specified by 'locatorPart'.
        /// </summary>
        /// <param name="locatorPart">locator part specifying data to be spanned</param>
        /// <param name="startNode">the node to be spanned by the created 
        /// selection</param>
        /// <param name="attachmentLevel">set to AttachmentLevel.Full if the entire range of text
        /// was resolved, otherwise set to StartPortion, MiddlePortion, or EndPortion based on
        /// which part of the range was resolved</param>
        /// <returns>a selection spanning the portion of 'startNode' specified by     
        /// 'locatorPart', null if selection described by locator part could not be
        /// recreated</returns>
        /// <exception cref="ArgumentNullException">locatorPart or startNode are 
        /// null</exception>
        /// <exception cref="ArgumentException">locatorPart is of the incorrect type</exception>
        public override Object ResolveLocatorPart(ContentLocatorPart locatorPart, DependencyObject startNode, out AttachmentLevel attachmentLevel)
        {
            if (startNode == null)
                throw new ArgumentNullException("startNode");

            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            if (CharacterRangeElementName != locatorPart.PartType)
                throw new ArgumentException(SR.Get(SRID.IncorrectLocatorPartType, locatorPart.PartType.Namespace + ":" + locatorPart.PartType.Name), "locatorPart");

            // First we extract the offset and length of the 
            // text range from the locator part.
            int startOffset = 0;
            int endOffset = 0;

            string stringCount = locatorPart.NameValuePairs[CountAttribute];
            if (stringCount == null)
                throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, TextSelectionProcessor.CountAttribute));
            int count = Int32.Parse(stringCount, NumberFormatInfo.InvariantInfo);

            TextAnchor anchor = new TextAnchor();

            attachmentLevel = AttachmentLevel.Unresolved;

            for (int i = 0; i < count; i++)
            {
                GetLocatorPartSegmentValues(locatorPart, i, out startOffset, out endOffset);

                // Now we grab the TextRange so we can create a selection.  
                // TextBox doesn't expose its internal TextRange so we use
                // its API for creating and getting the selection.
                ITextPointer elementStart;
                ITextPointer elementEnd;
                // If we can't get the start/end of the node then we can't resolve the locator part
                if (!GetNodesStartAndEnd(startNode, out elementStart, out elementEnd))
                    return null;

                // If the offset is not withing the element's text range we return null
                int textRangeLength = elementStart.GetOffsetToPosition(elementEnd);
                if (startOffset > textRangeLength)
                    return null;

                ITextPointer start = elementStart.CreatePointer(startOffset);// new TextPointer((TextPointer)elementStart, startOffset);

                ITextPointer end = (textRangeLength <= endOffset) ?
                    elementEnd.CreatePointer() : //new TextPointer((TextPointer)elementEnd) :
                    elementStart.CreatePointer(endOffset);// new TextPointer((TextPointer)elementStart, endOffset);

                //we do not process 0 length selection
                if (start.CompareTo(end) >= 0)
                    return null;

                anchor.AddTextSegment(start, end);
            }

            //we do not support 0 or negative length selection
            if (anchor.IsEmpty)
            {
                throw new ArgumentException(SR.Get(SRID.IncorrectAnchorLength), "locatorPart");
            }

            attachmentLevel = AttachmentLevel.Full;

            if (_clamping)
            {
                ITextPointer currentStart = anchor.Start;
                ITextPointer currentEnd = anchor.End;
                IServiceProvider serviceProvider = null;
                ITextView textView = null;

                if (_targetPage != null)
                {
                    serviceProvider = _targetPage as IServiceProvider;
                }
                else
                {
                    FlowDocument content = currentStart.TextContainer.Parent as FlowDocument;
                    serviceProvider = PathNode.GetParent(content as DependencyObject) as IServiceProvider;
                }

                Invariant.Assert(serviceProvider != null, "No ServiceProvider found to get TextView from.");
                textView = serviceProvider.GetService(typeof(ITextView)) as ITextView;
                Invariant.Assert(textView != null, "Null TextView provided by ServiceProvider.");

                anchor = TextAnchor.TrimToIntersectionWith(anchor, textView.TextSegments);

                if (anchor == null)
                {
                    attachmentLevel = AttachmentLevel.Unresolved;
                }
                else
                {
                    if (anchor.Start.CompareTo(currentStart) != 0)
                    {
                        attachmentLevel &= ~AttachmentLevel.StartPortion;
                    }
                    if (anchor.End.CompareTo(currentEnd) != 0)
                    {
                        attachmentLevel &= ~AttachmentLevel.EndPortion;
                    }
                }
            }

            return anchor;
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
        //  Internal Properties
        //
        //------------------------------------------------------        
        #region Internal Properties

        /// <summary>
        /// Controls whether or not resolving should clamp the text
        /// anchors to the visible portion of a text container.  Default
        /// value is true.
        /// </summary>
        internal bool Clamping
        {
            set
            {
                _clamping = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------        

        #region Internal Methods

        /// <summary>
        /// Gets the smallest offset and the largest offset from all the segments defined in the locator part.
        /// </summary>
        internal static void GetMaxMinLocatorPartValues(ContentLocatorPart locatorPart, out int startOffset, out int endOffset)
        {
            if (locatorPart == null)
                throw new ArgumentNullException("locatorPart");

            string stringCount = locatorPart.NameValuePairs[CountAttribute];
            if (stringCount == null)
                throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, TextSelectionProcessor.CountAttribute));
            int count = Int32.Parse(stringCount, NumberFormatInfo.InvariantInfo);

            startOffset = Int32.MaxValue;
            endOffset = 0;

            int segStart;
            int segEnd;
            for (int i = 0; i < count; i++)
            {
                GetLocatorPartSegmentValues(locatorPart, i, out segStart, out segEnd);

                if (segStart < startOffset)
                    startOffset = segStart;

                if (segEnd > endOffset)
                    endOffset = segEnd;
            }
        }

        /// <summary>
        ///     Set the DocumentPageView this selection processor should use
        ///     when clamping text anchors. If this target is not set then
        ///     the set of DPVs held by the viewer are used.
        /// </summary>
        internal void SetTargetDocumentPageView(DocumentPageView target)
        {
            Debug.Assert(target != null);
            _targetPage = target;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        // Name of segment attribute
        internal const String SegmentAttribute = "Segment";

        // Name of segment attribute
        internal const String CountAttribute = "Count";

        // Name added to a LocatorPart with value "true" to mean
        // the LocatorPart matches for any overlapping LocatorPart
        internal const String IncludeOverlaps = "IncludeOverlaps";

        // Potential separators for values in segment name/value pairs
        internal static readonly Char[] Separator = new Char[] { ',' };

        // Name of locator part element
        internal static readonly XmlQualifiedName CharacterRangeElementName = new XmlQualifiedName("CharacterRange", AnnotationXmlConstants.Namespaces.BaseSchemaNamespace);

        #endregion Internal Fields

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Extracts the values of attributes from a locator part.
        /// </summary>
        /// <param name="locatorPart">the locator part to extract values from</param>
        /// <param name="segmentNumber">the number of the segment to extract values for</param>
        /// <param name="startOffset">value of offset attribute</param>
        /// <param name="endOffset">value of length attribute</param>
        private static void GetLocatorPartSegmentValues(ContentLocatorPart locatorPart, int segmentNumber, out int startOffset, out int endOffset)
        {
            if (segmentNumber < 0)
                throw new ArgumentException("segmentNumber");

            string segmentString = locatorPart.NameValuePairs[SegmentAttribute + segmentNumber.ToString(NumberFormatInfo.InvariantInfo)];

            string[] values = segmentString.Split(Separator);
            if (values.Length != 2)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidLocatorPart, SegmentAttribute + segmentNumber.ToString(NumberFormatInfo.InvariantInfo)));
            }

            startOffset = Int32.Parse(values[0], NumberFormatInfo.InvariantInfo);
            endOffset = Int32.Parse(values[1], NumberFormatInfo.InvariantInfo);
        }

        /// <summary>
        ///     Returns the TextContainer for a node that contains text.  If the node
        ///     doesn't have a TextContainer this method returns null.
        /// </summary>
        private ITextContainer GetTextContainer(DependencyObject startNode)
        {
            Debug.Assert(startNode != null);

            ITextContainer textContainer = null;
            IServiceProvider serviceProvider = startNode as IServiceProvider;
            if (serviceProvider != null)
            {
                textContainer = serviceProvider.GetService(typeof(ITextContainer)) as ITextContainer;
            }

            if (textContainer == null)
            {
                // Special case for TextBox which doesn't implement IServiceProvider
                TextBoxBase textBox = startNode as TextBoxBase;
                if (textBox != null)
                {
                    textContainer = textBox.TextContainer;
                }
            }

            return textContainer;
        }

        /// <summary>
        ///     Returns ITextPointers positioned at the start and end of an element
        ///     that contains text.  
        /// </summary>
        private bool GetNodesStartAndEnd(DependencyObject startNode, out ITextPointer start, out ITextPointer end)
        {
            start = null;
            end = null;

            ITextContainer textContainer = GetTextContainer(startNode);
            if (textContainer != null)
            {
                start = textContainer.Start;
                end = textContainer.End;
            }
            else
            {
                // Special case for TextElement which doesn't expose its TextContainer
                TextElement textElement = startNode as TextElement;
                if (textElement != null)
                {
                    start = textElement.ContentStart;
                    end = textElement.ContentEnd;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Gets start and end offset for a text segment but clamps those values to the start and end 
        /// of a given element.  This way if a large text range is being resolved on a node that only contains
        /// a portion of the text range (such as a paragraph) the result only includes the content in that node.
        /// </summary>
        private void GetTextSegmentValues(TextSegment segment, ITextPointer elementStart, ITextPointer elementEnd, out int startOffset, out int endOffset)
        {
            startOffset = 0;
            endOffset = 0;

            if (elementStart.CompareTo(segment.Start) >= 0)
            {
                // segment starts before the start of the element
                startOffset = 0;
            }
            else
            {
                startOffset = elementStart.GetOffsetToPosition(segment.Start);
            }

            if (elementEnd.CompareTo(segment.End) >= 0)
            {
                endOffset = elementStart.GetOffsetToPosition(segment.End);
            }
            else
            {
                // segment ends after the end of the element
                endOffset = elementStart.GetOffsetToPosition(elementEnd);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // ContentLocatorPart types understood by this processor
        private static readonly XmlQualifiedName[] LocatorPartTypeNames =
                new XmlQualifiedName[]
                {
                    CharacterRangeElementName
                };

        // Optional DPV - used in printing case when there is no viewer available
        private DocumentPageView _targetPage = null;

        // Controls whether or not resolving clamps text anchors to
        // the visible portion of a TextContainer
        private bool _clamping = true;

        #endregion Private Fields
    }
}
