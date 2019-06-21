// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Set of static methods implementing text range serialization
//

namespace System.Windows.Documents
{
    using MS.Internal;
    using System.Text;
    using System.Xml;
    using System.IO;
    using System.Windows.Markup; // TypeConvertContext, ParserContext
    using System.Windows.Controls;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Security;

    /// <summary>
    ///     TextRangeSerialization is a static class containing
    ///     an implementation for TextRange serialization functionality.
    ///     It is only used from TextRange.GetXml/AppendXml methods.
    /// </summary>
    internal static class TextRangeSerialization
    {
        // -------------------------------------------------------------
        //
        // Internal Methods
        //
        // -------------------------------------------------------------

        #region Internal Methods

        internal static void WriteXaml(XmlWriter xmlWriter, ITextRange range, bool useFlowDocumentAsRoot, WpfPayload wpfPayload)
        {
            WriteXaml(xmlWriter, range, useFlowDocumentAsRoot, wpfPayload, false);
        }

        /// <summary>
        /// Writes a content of current range in form of valid xml.
        /// Places an artificial element xaml:FlowDocument as a root of output xml.
        /// </summary>
        /// <param name="xmlWriter">
        /// XmlWriter to which the range will be serialized
        /// </param>
        /// <param name="range">
        /// TextRange whose content is copied into XmlWriter xmlWriter.
        /// </param>
        /// <param name="useFlowDocumentAsRoot">
        /// true means that we need to serialize the whole FlowDocument - used in FileSave scenario;
        /// false means that we are in copy-paste scenario and will use Section or Span as a root - depending on context.
        /// </param>
        /// <param name="wpfPayload">
        /// When this parameter is not null, images are serialized.  When null, images are stripped out.
        /// </param>
        /// <param name="preserveTextElements">
        /// When TRUE, TextElements are serialized as-is.  When FALSE, they're upcast to their base type.
        /// </param>
        internal static void WriteXaml(XmlWriter xmlWriter, ITextRange range, bool useFlowDocumentAsRoot, WpfPayload wpfPayload, bool preserveTextElements)
        {
            // Set unindented formatting to avoid inserting insignificant whitespaces as significant ones
            Formatting saveWriterFormatting = Formatting.None;
            if (xmlWriter is XmlTextWriter)
            {
                saveWriterFormatting = ((XmlTextWriter)xmlWriter).Formatting;
                ((XmlTextWriter)xmlWriter).Formatting = Formatting.None;
            }

            // Get the default xamlTypeMapper.
            XamlTypeMapper xamlTypeMapper = XmlParserDefaults.DefaultMapper;

            // Identify structural scope of selection - nearest common ancestor
            ITextPointer commonAncestor = FindSerializationCommonAncestor(range);

            // Decide whether we need last paragraph merging or not
            bool lastParagraphMustBeMerged =
                !TextPointerBase.IsAfterLastParagraph(range.End) &&
                range.End.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementStart;

            // Write wrapping element with contextual properties
            WriteRootFlowDocument(range, commonAncestor, xmlWriter, xamlTypeMapper, lastParagraphMustBeMerged, useFlowDocumentAsRoot);

            // The ignoreWriteHyperlinkEnd flag will be set after call WriteOpeningTags.
            // If ignoreWriteHyperlinkEnd is true, WriteXamlTextSegment won't write Hyperlink end element
            // since Hyperlink writing opening tag is ignored by selecting the partial of Hyperlink.
            bool ignoreWriteHyperlinkEnd;
            List<int> ignoreList = new List<int>();

            // Start counting tags needed to be closed.
            // EmptyDocumentDepth==1 - counts FlowDocument opened in WriteRootFlowDocument above.
            int elementLevel = EmptyDocumentDepth + WriteOpeningTags(range, range.Start, commonAncestor, xmlWriter, xamlTypeMapper, /*reduceElement:*/wpfPayload == null, out ignoreWriteHyperlinkEnd, ref ignoreList, preserveTextElements);

            if (range.IsTableCellRange)
            {
                WriteXamlTableCellRange(xmlWriter, range, xamlTypeMapper, ref elementLevel, wpfPayload, preserveTextElements);
            }
            else
            {
                WriteXamlTextSegment(xmlWriter, range.Start, range.End, xamlTypeMapper, ref elementLevel, wpfPayload, ignoreWriteHyperlinkEnd, ignoreList, preserveTextElements);
            }

            // Close all remaining tags - scoping its End position
            Invariant.Assert(elementLevel >= 0, "elementLevel cannot be negative");
            while (elementLevel-- > 0)
            {
                xmlWriter.WriteFullEndElement();
            }

            // Restore xmlWriter's Formatting property
            if (xmlWriter is XmlTextWriter)
            {
                ((XmlTextWriter)xmlWriter).Formatting = saveWriterFormatting;
            }
        }

        /// <summary>
        /// Reads a well-formed xml representing a serialized text range.
        /// It expects a root element xaml:FlowDocument and two range markers
        /// The result of reading is pasting this text into End position
        /// of text range.
        /// </summary>
        /// <param name="range">
        /// TextRange designating the target position for pasting.
        /// The existing content of a range will be deleted and new content
        /// will be inserted at the end.
        /// Resulting locations of Start/End positions depend on their gravities.
        /// Normally (when gravity=Backward/Forward respectively) the resulting
        /// range will embrace the inserted content.
        /// </param>
        /// <param name="fragment">
        /// Represents a portion of xml to insert into the range.
        /// </param>
        /// <remarks>
        /// We are expecting to be called with xmlReader on opening tag
        /// of root text range element - xaml:FlowDocument.
        /// Some insignificant stuff may occur before the root though.
        /// Otherwise exception will be thrown.
        /// </remarks>
        internal static void PasteXml(TextRange range, TextElement fragment)
        {
            Invariant.Assert(fragment != null);

            // Check a special case for pasing a single embedded element
            if (PasteSingleEmbeddedElement(range, fragment))
            {
                // All done. Return successfully.
                return;
            }

            // Set default value for an indicator of whether we need to merge last paragraph or not.
            // It depends on a state of a range, so do it before emptying the range.
            AdjustFragmentForTargetRange(fragment, range);

            // Delete current content of a range
            if (!range.IsEmpty)
            {
                range.Text = String.Empty;
            }
            Invariant.Assert(range.IsEmpty, "range must be empty in the beginning of pasting");

            // Chek special case of empty pasted fragment
            if (((ITextPointer)fragment.ContentStart).CompareTo(fragment.ContentEnd) == 0)
            {
                // Pasted fragment is empty. Nothing to insert.
                return;
            }

            // Transfer the content from reader to writer and merge elements on both ends
            PasteTextFragment(fragment, range);
        }

        #endregion Internal Methods

        // -------------------------------------------------------------
        //
        // Private Methods
        //
        // -------------------------------------------------------------

        #region Private Methods

        // .............................................................
        //
        // Serialization
        //
        // .............................................................

        /// <summary>
        /// This function serializes text segment formed by rangeStart and rangeEnd to valid xml using xmlWriter.
        /// </summary>
        private static void WriteXamlTextSegment(XmlWriter xmlWriter, ITextPointer rangeStart, ITextPointer rangeEnd, XamlTypeMapper xamlTypeMapper, ref int elementLevel, WpfPayload wpfPayload, bool ignoreWriteHyperlinkEnd, List<int> ignoreList, bool preserveTextElements)
        {
            // Special case for pure text selection - we need a Run wrapper for it.
            if (elementLevel == EmptyDocumentDepth && typeof(Run).IsAssignableFrom(rangeStart.ParentType))
            {
                elementLevel++;
                xmlWriter.WriteStartElement(typeof(Run).Name);
            }

            // Create text navigator for reading the range's content
            ITextPointer textReader = rangeStart.CreatePointer();

            // Exclude last opening tag from serialization - we don't need to create extra element
            // is cases when we have whole paragraphs/cells selected.
            // NOTE: We do this slightly differently than in TextRangeEdit.AdjustRangeEnd, where we use normalization for adjusted position.
            // In this case normalized position does not work, because we need to keep information about crossed paragraph boundary.
            while (rangeEnd.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                rangeEnd = rangeEnd.GetNextContextPosition(LogicalDirection.Backward);
            }

            // Write the range internal contents
            while (textReader.CompareTo(rangeEnd) < 0)
            {
                TextPointerContext runType = textReader.GetPointerContext(LogicalDirection.Forward);

                switch (runType)
                {
                    case TextPointerContext.ElementStart:
                        TextElement nextElement = (TextElement)textReader.GetAdjacentElement(LogicalDirection.Forward);
                        if (nextElement is Hyperlink)
                        {
                            // Don't write Hyperlink start element if Hyperlink is invalid
                            // in case of having a UiElement except Image or stated the range end
                            // position before the end position of the Hyperlink.
                            if (IsHyperlinkInvalid(textReader, rangeEnd))
                            {
                                ignoreWriteHyperlinkEnd = true;
                                textReader.MoveToNextContextPosition(LogicalDirection.Forward);

                                continue;
                            }
                        }
                        else if (nextElement != null)
                        {
                            // This code is a generic version of the more specific Hyperlink code
                            // directly above.  That code should be folded into this.

                            TextElementEditingBehaviorAttribute att = (TextElementEditingBehaviorAttribute)Attribute.GetCustomAttribute(nextElement.GetType(), typeof(TextElementEditingBehaviorAttribute));
                            if (att != null && !att.IsTypographicOnly)
                            {
                                if (IsPartialNonTypographic(textReader, rangeEnd))
                                {
                                    // Add pointer to ignore list
                                    ITextPointer ptr = textReader.CreatePointer();
                                    ptr.MoveToElementEdge(ElementEdge.BeforeEnd);
                                    ignoreList.Add(ptr.Offset);

                                    textReader.MoveToNextContextPosition(LogicalDirection.Forward);

                                    continue;
                                }
                            }
                        }

                        elementLevel++;
                        textReader.MoveToNextContextPosition(LogicalDirection.Forward);
                        WriteStartXamlElement(/*range:*/null, textReader, xmlWriter, xamlTypeMapper, /*reduceElement:*/wpfPayload == null, preserveTextElements);

                        break;

                    case TextPointerContext.ElementEnd:
                        // Don't write Hyperlink end element if Hyperlink include the invalid
                        // in case of having a UiElement except Image or stated the range end
                        // before the end position of the Hyperlink or Hyperlink opening tag is
                        // skipped from WriteOpeningTags by selecting of the partial of Hyperlink.
                        if (ignoreWriteHyperlinkEnd && (textReader.GetAdjacentElement(LogicalDirection.Forward) is Hyperlink))
                        {
                            // Reset the flag to keep walk up the next Hyperlink tag
                            ignoreWriteHyperlinkEnd = false;
                            textReader.MoveToNextContextPosition(LogicalDirection.Forward);

                            continue;
                        }

                        // Check the ignore list
                        if (ignoreList.Count > 0)
                        {
                            ITextPointer endPointer = textReader.CreatePointer();
                            endPointer.MoveToElementEdge(ElementEdge.BeforeEnd);  // Is this necessary?
                            if (ignoreList.Contains(endPointer.Offset))
                            {
                                ignoreList.Remove(endPointer.Offset);
                                textReader.MoveToNextContextPosition(LogicalDirection.Forward);

                                continue;
                            }
                        }

                        elementLevel--;
                        if (TextSchema.IsBreak(textReader.ParentType))
                        {
                            // For LineBreak, etc. use empty element syntax
                            xmlWriter.WriteEndElement();
                        }
                        else
                        {   // No need in enforcing full element notation. Remove this branch.
                            // For all other textelements use explicit closing tag.
                            xmlWriter.WriteFullEndElement();
                        }
                        textReader.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    case TextPointerContext.Text:
                        int textLength = textReader.GetTextRunLength(LogicalDirection.Forward);
                        char[] text = new Char[textLength];

                        textLength = TextPointerBase.GetTextWithLimit(textReader, LogicalDirection.Forward, text, 0, textLength, rangeEnd);

                        // XmlWriter will throw an ArgumentException if text contains
                        // any invalid surrogates, so strip them out now.
                        textLength = StripInvalidSurrogateChars(text, textLength);

                        xmlWriter.WriteChars(text, 0, textLength);
                        textReader.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;

                    case TextPointerContext.EmbeddedElement:
                        object embeddedObject = textReader.GetAdjacentElement(LogicalDirection.Forward);
                        textReader.MoveToNextContextPosition(LogicalDirection.Forward);

                        WriteEmbeddedObject(embeddedObject, xmlWriter, wpfPayload);
                        break;

                    default:
                        Invariant.Assert(false, "unexpected value of runType");
                        textReader.MoveToNextContextPosition(LogicalDirection.Forward);
                        break;
                }
            }
        }

        /// <summary>
        /// Serializes a rectagular table range
        /// </summary>
        private static void WriteXamlTableCellRange(XmlWriter xmlWriter, ITextRange range, XamlTypeMapper xamlTypeMapper, ref int elementLevel, WpfPayload wpfPayload, bool preserveTextElements)
        {
            Invariant.Assert(range.IsTableCellRange, "range is expected to be in IsTableCellRange state");

            List<TextSegment> textSegments = range.TextSegments;

            int checkElementLevel = -1; // negative value as an indicator that it is not yet initialized

            // Set ignoreWriteHyperlinkEnd as false initially
            bool ignoreWriteHyperlinkEnd = false;
            List<int> ignoreList = new List<int>();

            for (int i = 0; i < textSegments.Count; i++)
            {
                TextSegment textSegment = textSegments[i];

                // Open a row for this segment (except for the very first one, for which we opened a row in a WriteOpeningTags method)
                if (i > 0)
                {
                    ITextPointer pointer = textSegment.Start.CreatePointer();
                    while (!typeof(TableRow).IsAssignableFrom(pointer.ParentType))
                    {
                        Invariant.Assert(typeof(TextElement).IsAssignableFrom(pointer.ParentType), "pointer must be still in a scope of TextElement");
                        pointer.MoveToElementEdge(ElementEdge.BeforeStart);
                    }
                    Invariant.Assert(typeof(TableRow).IsAssignableFrom(pointer.ParentType), "pointer must be in a scope of TableRow");
                    pointer.MoveToElementEdge(ElementEdge.BeforeStart);

                    ITextRange textRange = new TextRange(textSegment.Start, textSegment.End);

                    elementLevel += WriteOpeningTags(textRange, textSegment.Start, pointer, xmlWriter, xamlTypeMapper, /*reduceElement:*/wpfPayload == null, out ignoreWriteHyperlinkEnd, ref ignoreList, preserveTextElements);
                }

                // Output the cell segment for one row
                WriteXamlTextSegment(xmlWriter, textSegment.Start, textSegment.End, xamlTypeMapper, ref elementLevel, wpfPayload, ignoreWriteHyperlinkEnd, ignoreList, preserveTextElements);

                Invariant.Assert(elementLevel >= 4, "At the minimun we expected to stay within four elements: Section(wrapper),Table,TableRowGroup,TableRow");
                if (checkElementLevel < 0) checkElementLevel = elementLevel; // initialize level checking variable
                Invariant.Assert(checkElementLevel == elementLevel, "elementLevel is supposed to be unchanged between segments of table cell range");

                // Assuming that the element is TableRow - close it.
                // NOTE: Such assumption is valid because WriteXamlTextSegment moves end pointer out of all opening tags,
                // so it ends serialization immediately after the last cell's closing tag.
                // This means that we only need to close one level - for TableRow.
                // To make the code more reliable we need to add explicit control for closing elements.
                elementLevel--;
                xmlWriter.WriteFullEndElement();
            }
        }

        /// <summary>
        /// Walks the tree up from current position and writes all scoping tags
        /// in their natural order - from root to leafs.
        /// </summary>
        /// <param name="range">
        /// Range identifying the whole selection.
        /// Needed for
        /// - table cell range case: proper column processing: to output only columns related to the selection
        /// - text segement case: hyperlink serialization heuristics
        /// </param>
        /// <param name="thisElement">
        /// ITextPointer identifying an element.
        /// </param>
        /// <param name="scope">
        /// A position identifying the scope which should be used for serialization.
        /// All tags outside of this scope will be ignored.
        /// </param>
        /// <param name="xmlWriter">
        /// XmlWriter to write element tags.
        /// </param>
        /// <param name="xamlTypeMapper"></param>
        /// <param name="reduceElement">
        /// <see cref="WriteStartXamlElement"/>
        /// </param>
        /// <param name="ignoreWriteHyperlinkEnd"></param>
        /// <param name="ignoreList"></param>
        /// <param name="preserveTextElements"></param>
        /// /// <returns>
        /// Number of opening tags written into XmlWriter.
        /// This number should be used afterwards to close all opened tags.
        /// </returns>
        private static int WriteOpeningTags(ITextRange range, ITextPointer thisElement, ITextPointer scope, XmlWriter xmlWriter, XamlTypeMapper xamlTypeMapper, bool reduceElement, out bool ignoreWriteHyperlinkEnd, ref List<int> ignoreList, bool preserveTextElements)
        {
            ignoreWriteHyperlinkEnd = false;

            // Recursion ends when we reach the scope level. We will write tags on returing path from the recursion
            if (thisElement.HasEqualScope(scope))
            {
                return 0; // no elements have opened at this level. Return elementCount==0.
            }

            Invariant.Assert(typeof(TextElement).IsAssignableFrom(thisElement.ParentType), "thisElement is expected to be a TextElement");

            ITextPointer previousLevel = thisElement.CreatePointer();
            previousLevel.MoveToElementEdge(ElementEdge.BeforeStart);

            // Recurse into the parent element
            int elementLevel = WriteOpeningTags(range, previousLevel, scope, xmlWriter, xamlTypeMapper, reduceElement, out ignoreWriteHyperlinkEnd, ref ignoreList, preserveTextElements);

            // After returning from the recursion - when all parent tags have been written,
            // write the opening tag for this element

            // Hyperlink open tag will be skipped since the range selection of Hyperlink is the partial
            // of Hyperlink range or Hyperlink include invalid UIElement except Image.
            bool ignoreHyperlink = false;
            bool isPartialNonTypographic = false;

            if (thisElement.ParentType == typeof(Hyperlink))
            {
                if (TextPointerBase.IsAtNonMergeableInlineStart(range.Start))
                {
                    ITextPointer position = thisElement.CreatePointer();
                    position.MoveToElementEdge(ElementEdge.BeforeStart);

                    ignoreHyperlink = IsHyperlinkInvalid(position, range.End);
                }
                else
                {
                    ignoreHyperlink = true;
                }
            }
            else
            {
                // This code is a generic version of the more specific Hyperlink code
                // directly above.  That code should be folded into this.
                TextElementEditingBehaviorAttribute att = (TextElementEditingBehaviorAttribute)Attribute.GetCustomAttribute(thisElement.ParentType, typeof(TextElementEditingBehaviorAttribute));
                if (att != null && !att.IsTypographicOnly)
                {
                    if (TextPointerBase.IsAtNonMergeableInlineStart(range.Start))
                    {
                        ITextPointer position = thisElement.CreatePointer();
                        position.MoveToElementEdge(ElementEdge.BeforeStart);

                        isPartialNonTypographic = IsPartialNonTypographic(position, range.End);
                    }
                    else
                    {
                        isPartialNonTypographic = true;
                    }
                }
            }
            int count;

            if (ignoreHyperlink)
            {
                // Ignore writing Hyperlink opening tag
                ignoreWriteHyperlinkEnd = true;

                // Set elementLevel without adding it
                count = elementLevel;
            }
            else if (isPartialNonTypographic)
            {
                // Add the end pointer to the list
                ITextPointer position = thisElement.CreatePointer();
                position.MoveToElementEdge(ElementEdge.BeforeEnd);
                ignoreList.Add(position.Offset);

                // Set elementLevel without adding to it
                count = elementLevel;
            }
            else
            {
                // Write the opening tag
                WriteStartXamlElement(range, thisElement, xmlWriter, xamlTypeMapper, reduceElement, preserveTextElements);

                // Each opening tag adds one to the level count
                count = elementLevel + 1;
            }

            // Return the opening tag count
            return count;
        }

        /// <summary>
        /// Writes an opening tag of an element together with all attributes
        /// representing Avalon properties.
        /// </summary>
        /// <param name="range">
        /// Parameter used for top-level Table element - to decide what columns to output.
        /// For all other elements it's ignored.
        /// </param>
        /// <param name="textReader">
        /// TextPointer positioned in the scope of element whose
        /// start tag is going to be written.
        /// </param>
        /// <param name="xmlWriter">
        /// XmlWriter to output element opening tag.
        /// </param>
        /// <param name="xamlTypeMapper"></param>
        /// <param name="reduceElement">
        /// True value of this parameter indicates that
        /// serialization goes into XamlPackage, so all elements
        /// can be preserved as is; otherwise some of them must be
        /// reduced into simpler representations (such as InlineUIContainer -> Run
        /// and BlockUIContainer -> Paragraph).
        /// </param>
        /// <param name="preserveTextElements">
        /// If TRUE, TextElements are serialized as-is.  If FALSE, they're upcast
        /// to their base types.
        /// </param>
        private static void WriteStartXamlElement(ITextRange range, ITextPointer textReader, XmlWriter xmlWriter, XamlTypeMapper xamlTypeMapper, bool reduceElement, bool preserveTextElements)
        {
            Type elementType = textReader.ParentType;

            Type elementTypeStandardized = TextSchema.GetStandardElementType(elementType, reduceElement);

            // Get rid f UIContainers when their child is not an image
            if (elementTypeStandardized == typeof(InlineUIContainer) || elementTypeStandardized == typeof(BlockUIContainer))
            {
                Invariant.Assert(!reduceElement);

                InlineUIContainer inlineUIContainer = textReader.GetAdjacentElement(LogicalDirection.Backward) as InlineUIContainer;
                BlockUIContainer blockUIContainer = textReader.GetAdjacentElement(LogicalDirection.Backward) as BlockUIContainer;

                if ((inlineUIContainer == null || !(inlineUIContainer.Child is Image)) &&
                    (blockUIContainer == null || !(blockUIContainer.Child is Image)))
                {
                    // Even when we serialize for DataFormats.XamlPackage we strip out UIElement
                    // different from Images.
                    // Note that this condition is consistent with the one in WriteEmbeddedObject -
                    // so that when we reduce the element type fromm UIContainer to Run/Paragraph
                    // we also output just a space instead of the embedded object conntained in it.
                    elementTypeStandardized = TextSchema.GetStandardElementType(elementType, /*reduceElement:*/true);
                }
            }
            else if (preserveTextElements)
            {
                elementTypeStandardized = elementType;
            }

            bool customTextElement = preserveTextElements && !TextSchema.IsKnownType(elementType);
            if (customTextElement)
            {
                // If the element is not from PresentationFramework, we'll need to serialize a namespace
                // Will module name always have a '.'?  If so, can remove conditional in assembly assignment below
                int index = elementTypeStandardized.Module.Name.LastIndexOf('.');
                string assembly = (index == -1 ? elementTypeStandardized.Module.Name : elementTypeStandardized.Module.Name.Substring(0, index));
                string nameSpace = "clr-namespace:" + elementTypeStandardized.Namespace + ";" + "assembly=" + assembly;
                string prefix = elementTypeStandardized.Namespace;
                xmlWriter.WriteStartElement(prefix, elementTypeStandardized.Name, nameSpace);
            }
            else
            {
                xmlWriter.WriteStartElement(elementTypeStandardized.Name);
            }

            // Write properties
            DependencyObject complexProperties = new DependencyObject();
            WriteInheritableProperties(elementTypeStandardized, textReader, xmlWriter, /*onlyAffected:*/true, complexProperties);
            WriteNoninheritableProperties(elementTypeStandardized, textReader, xmlWriter, /*onlyAffected:*/true, complexProperties);
            if (customTextElement)
            {
                WriteLocallySetProperties(elementTypeStandardized, textReader, xmlWriter, complexProperties);
            }
            WriteComplexProperties(xmlWriter, complexProperties, elementTypeStandardized);

            // Special case for Table element serialization
            if (elementTypeStandardized == typeof(Table) && textReader is TextPointer)
            {
                // Write the columns text.
                WriteTableColumnsInformation(range, (Table)((TextPointer)textReader).Parent, xmlWriter, xamlTypeMapper);
            }
        }

        // Write columns related to the given table cell range.
        private static void WriteTableColumnsInformation(ITextRange range, Table table, XmlWriter xmlWriter, XamlTypeMapper xamlTypeMapper)
        {
            TableColumnCollection columns = table.Columns;
            int startColumn;
            int endColumn;

            if (!TextRangeEditTables.GetColumnRange(range, table, out startColumn, out endColumn))
            {
                startColumn = 0;
                endColumn = columns.Count - 1;
            }

            Invariant.Assert(startColumn >= 0, "startColumn index is supposed to be non-negative");

            if(columns.Count > 0)
            {
                // Build an appropriate name for the complex property
                string complexPropertyName = table.GetType().Name + ".Columns";

                // Write the start element for the complex property.
                xmlWriter.WriteStartElement(complexPropertyName);

                for (int i = startColumn; i <= endColumn && i < columns.Count; i++)
                {
                    WriteXamlAtomicElement(columns[i], xmlWriter, /*reduceElement:*/false);
                }

                // Close the element for the complex property
                xmlWriter.WriteEndElement();
            }
        }

        /// <summary>
        /// Creates a FlowDocument element wrapping copied content and storing its contextual properties.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="context"></param>
        /// <param name="xmlWriter"></param>
        /// <param name="xamlTypeMapper"></param>
        /// <param name="lastParagraphMustBeMerged"></param>
        /// <param name="useFlowDocumentAsRoot">
        /// true means that we need to serialize the whole FlowDocument - used in FileSave scenario;
        /// false means that we are in copy-paste scenario and will use Section or Span as a root - depending on context.
        /// </param>
        private static void WriteRootFlowDocument(ITextRange range, ITextPointer context, XmlWriter xmlWriter, XamlTypeMapper xamlTypeMapper, bool lastParagraphMustBeMerged, bool useFlowDocumentAsRoot)
        {
            Type rootType;
            const string xmlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            const string xmlns = "xmlns";

            // Decide what root element to use
            if (useFlowDocumentAsRoot)
            {
                rootType = typeof(FlowDocument);
            }
            else
            {
                Type contextType = context.ParentType;
                if (contextType == null ||
                    typeof(Paragraph).IsAssignableFrom(contextType) ||
                    typeof(Inline).IsAssignableFrom(contextType) && !typeof(AnchoredBlock).IsAssignableFrom(contextType))
                {
                    rootType = typeof(Span);
                }
                else
                {
                    rootType = typeof(Section);
                }
            }

            // Create a root element FlowDocument
            xmlWriter.WriteStartElement(rootType.Name, xmlNamespace);

            // Define default namespace as Avalon namespace
            xmlWriter.WriteAttributeString(xmlns, xmlNamespace);

            // Set the value of xml:space to "preserve" to consider all spaces as significant
            // Note that Xml treats whitespaces as significant if they belong to some nonempty line
            // (neighbored by non-whitespace characters at least from one side)
            // That's why we only loose whitespaces if they occupy the whole textrun in xml.
            // So alternative solution for whitespace preservation could be setting xml:space="preserve"
            // attribute to only empty runs - this would make our whitespace preservation more
            // narrowed...
            
            // Investigate if this is really worth doing.
            // Currently we wildly preserve all whitespaces in TextRange, which can produce
            // a problem if somebody manually provides this "innocent" xaml (notice newline between paragraphs):
            // <Paragraph>SomeText</Paragraph>
            // <Paragraph>OtherText</Paragraph>
            // In pasting this with all-whitespace-preservation we will add extra new line between paragraphs.
            // Formally it will be correct, but people may get confused with using our AppendXaml API.
            xmlWriter.WriteAttributeString("xml:space", "preserve");

            // Write all contextual properties as attributes of root fragment
            DependencyObject complexProperties = new DependencyObject();
            if (useFlowDocumentAsRoot)
            {
                WriteInheritablePropertiesForFlowDocument((DependencyObject)((TextPointer)context).Parent, xmlWriter, complexProperties);
            }
            else
            {
                WriteInheritableProperties(rootType, context, xmlWriter, /*onlyAffected:*/false, complexProperties);
            }

            if (rootType == typeof(Span))
            {
                // Root element is not real element to paste. It is just a property bag for contextual properties.
                // So we collect non-inheritable properties only for inline content; not needing it for block one.
                WriteNoninheritableProperties(typeof(Span), context, xmlWriter, /*onlyAffected:*/false, complexProperties);
            }

            // Write an indicator that last paragraph must be merged on paste
            if (rootType == typeof(Section) && lastParagraphMustBeMerged)
            {
                xmlWriter.WriteAttributeString(Section.HasTrailingParagraphBreakOnPastePropertyName, "False");
            }

            // Note that we are skipping background property, because we only want to transfer it for the whole document.
            // This heuristic for the "whole-document" background must be implemented.

            WriteComplexProperties(xmlWriter, complexProperties, rootType);
        }

        private static void WriteInheritablePropertiesForFlowDocument(DependencyObject context, XmlWriter xmlWriter, DependencyObject complexProperties)
        {
            DependencyProperty[] inheritableProperties = TextSchema.GetInheritableProperties(typeof(FlowDocument));

            for (int i = 0; i < inheritableProperties.Length; i++)
            {
                DependencyProperty property = inheritableProperties[i];
                object value = context.ReadLocalValue(property);

                if (value != DependencyProperty.UnsetValue)
                {
                    string stringValue = DPTypeDescriptorContext.GetStringValue(property, value);
                    if (stringValue != null)
                    {
                        stringValue = FilterNaNStringValueForDoublePropertyType(stringValue, property.PropertyType);

                        string propertyName;
                        if (property == FrameworkContentElement.LanguageProperty)
                        {
                            // Special case for CultureInfo property that must be represented in xaml as xml:lang attribute
                            propertyName = "xml:lang";
                        }
                        else
                        {
                            // Regular case using own property name
                            propertyName = property.OwnerType == typeof(Typography) ? "Typography." + property.Name : property.Name;
                        }
                        xmlWriter.WriteAttributeString(propertyName, stringValue);
                    }
                    else
                    {
                        complexProperties.SetValue(property, value);
                    }
                }
            }
        }

        // Writes a collection of attributes representing inheritable properties
        // whose values has been affected by this element.
        // Parameter onlyAffected=true means that we serialize only properties affected by
        // the current element; otherwise we output all known inheritable properties.
        private static void WriteInheritableProperties(Type elementTypeStandardized, ITextPointer context, XmlWriter xmlWriter, bool onlyAffected, DependencyObject complexProperties)
        {
            // Create a pointer positioned immediately outside the element
            ITextPointer outerContext = null;
            if (onlyAffected)
            {
                outerContext = context.CreatePointer();
                outerContext.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            DependencyProperty[] inheritableProperties = TextSchema.GetInheritableProperties(elementTypeStandardized);

            for (int i = 0; i < inheritableProperties.Length; i++)
            {
                DependencyProperty property = inheritableProperties[i];

                object innerValue = context.GetValue(property);
                if (innerValue == null)
                {
                    // Some properties like Foreground may have null as default value.
                    // Skip them.
                    continue;
                }

                object outerValue = null;
                if (onlyAffected)
                {
                    outerValue = outerContext.GetValue(property);
                }

                // The property must appear in markup if the element
                if (!onlyAffected ||  // all properties requested for saving context on root
                    !TextSchema.ValuesAreEqual(innerValue, outerValue)) // or the element really affects the property
                {
                    string stringValue = DPTypeDescriptorContext.GetStringValue(property, innerValue);

                    if (stringValue != null)
                    {
                        stringValue = FilterNaNStringValueForDoublePropertyType(stringValue, property.PropertyType);

                        string propertyName;
                        if (property == FrameworkContentElement.LanguageProperty)
                        {
                            // Special case for CultureInfo property that must be represented in xaml as xml:lang attribute
                            propertyName = "xml:lang";
                        }
                        else
                        {
                            // Regular case: serialize a property with its own name
                            propertyName = GetPropertyNameForElement(property, elementTypeStandardized, /*forceComplexName:*/false);
                        }
                        xmlWriter.WriteAttributeString(propertyName, stringValue);
                    }
                    else
                    {
                        complexProperties.SetValue(property, innerValue);
                    }
                }
            }
        }


        // Writes a collection of attributes representing non-inheritable properties
        // whose values are set inline on the given element instance.
        // When we read properties fromContext we want all values including defaults; from text elements we only want only affected
        private static void WriteNoninheritableProperties(Type elementTypeStandardized, ITextPointer context, XmlWriter xmlWriter, bool onlyAffected, DependencyObject complexProperties)
        {
            DependencyProperty[] elementProperties = TextSchema.GetNoninheritableProperties(elementTypeStandardized);

            // We'll need a pointer to walk the tree up when onlyAffected=false
            ITextPointer parentContext = onlyAffected ? null : context.CreatePointer();

            for (int i = 0; i < elementProperties.Length; i++)
            {
                DependencyProperty property = elementProperties[i];
                Type propertyOwnerType = context.ParentType;

                object propertyValue;
                if (onlyAffected)
                {
                    // This way of getting properties works only for elements whose style defaults are equal to known types' style defaults
                    // If user defines some element (say, Heading1) with different default properties we will replace them unintentionally to Paragraph's defaults.
                    propertyValue = context.GetValue(property);
                }
                else
                {
                    // This is request for contextual properties - use "manual" inheritance to collect values
                    Invariant.Assert(elementTypeStandardized == typeof(Span), "Request for contextual properties is expected for Span wrapper only");

                    // Get property value from this element or from one of its ancestors (the latter in case of !onlyAffeted)
                    propertyValue = context.GetValue(property);

                    // Get property value from its ancestors if the property is not set.
                    // TextDecorationCollection is special-cased as its default is empty collection,
                    // and its value source cannot be distinguished from ITextPointer.
                    if (propertyValue == null || TextDecorationCollection.Empty.ValueEquals(propertyValue as TextDecorationCollection))
                    {
                        if (property == Inline.BaselineAlignmentProperty || property == TextElement.TextEffectsProperty)
                        {
                            // These properties do not make sense as contextual; do not include them into context.
                            continue;
                        }

                        parentContext.MoveToPosition(context);
                        while ((propertyValue == null || TextDecorationCollection.Empty.ValueEquals(propertyValue as TextDecorationCollection))
                            && typeof(Inline).IsAssignableFrom(parentContext.ParentType))
                        {
                            parentContext.MoveToElementEdge(ElementEdge.BeforeStart);
                            propertyValue = parentContext.GetValue(property);
                            propertyOwnerType = parentContext.ParentType;
                        }
                    }
                }

                // Paragraph has Margin="Auto" in default style. List has Margin as well as Padding="Auto" in the style.
                // This "Auto" value coming from style is different than the default value
                // in the property metadata (which is zero thickness).
                // So we have the following hard-coded check to avoid serializing them.
                // The intention here is to not bloat the size of serialized xaml for frequently used elements.
                // Note that, in doing so we are at risk of breaking our typographically equivalent serialization philosophy,
                // when the target of paste operation has a different style setting.

                if ((property == Block.MarginProperty && (typeof(Paragraph).IsAssignableFrom(propertyOwnerType) || typeof(List).IsAssignableFrom(propertyOwnerType)))
                    ||
                    (property == Block.PaddingProperty) && typeof(List).IsAssignableFrom(propertyOwnerType))
                {
                    Thickness thickness = (Thickness)propertyValue;
                    if (Paragraph.IsMarginAuto(thickness))
                    {
                        continue;
                    }
                }

                // Write the property as attribute string or add it to a list of complex properties.
                WriteNoninheritableProperty(xmlWriter, property, propertyValue, propertyOwnerType, onlyAffected, complexProperties, context.ReadLocalValue(property));
            }
        }

        // Writes a value of an individual non-inheritable property in form of attribute string.
        // If the value cannot be serialized as a string, adds the property to a collection of complexProperties.
        // To minimize the amount of xaml produced, the property is skipped if its value is equal to its default value
        // for the given element type - the propertyOwnerType.
        // The flag onlyAffected=false means that we want to output all properties independently on
        // if they are equal to their default values or not.
        private static void WriteNoninheritableProperty(XmlWriter xmlWriter, DependencyProperty property, object propertyValue, Type propertyOwnerType, bool onlyAffected, DependencyObject complexProperties, object localValue)
        {
            bool write = false;
            if (propertyValue != null &&
                propertyValue != DependencyProperty.UnsetValue)
            {
                if (!onlyAffected)
                {
                    write = true;
                }
                else
                {
                    PropertyMetadata metadata = property.GetMetadata(propertyOwnerType);

                    write = (metadata == null) || !(TextSchema.ValuesAreEqual(propertyValue, /*defaultValue*/metadata.DefaultValue) && localValue == DependencyProperty.UnsetValue);
                }
            }

            if (write)
            {
                string stringValue = DPTypeDescriptorContext.GetStringValue(property, propertyValue);

                if (stringValue != null)
                {
                    stringValue = FilterNaNStringValueForDoublePropertyType(stringValue, property.PropertyType);

                    // For the property name in this case we safe to use simple name only;
                    // as noninheritable property would never require TypeName.PropertyName notation
                    // for attribute syntax.
                    xmlWriter.WriteAttributeString(property.Name, stringValue);
                }
                else
                {
                    complexProperties.SetValue(property, propertyValue);
                }
            }
        }

        // Writes a collection of attributes representing properties with local values set on them.
        // If the value cannot be serialized as a string, adds the property to a collection of complexProperties.
        private static void WriteLocallySetProperties(Type elementTypeStandardized, ITextPointer context, XmlWriter xmlWriter, DependencyObject complexProperties)
        {
            TextPointer textPointer = context as TextPointer;
            if (textPointer == null)
            {
                // We can't have custom properties if we're not a TextPointer
                return;
            }

            LocalValueEnumerator locallySetProperties = context.GetLocalValueEnumerator();
            DependencyProperty[] inheritableProperties = TextSchema.GetInheritableProperties(elementTypeStandardized);
            DependencyProperty[] nonInheritableProperties = TextSchema.GetNoninheritableProperties(elementTypeStandardized);

            while (locallySetProperties.MoveNext())
            {
                DependencyProperty locallySetProperty = (DependencyProperty)locallySetProperties.Current.Property;

                // Don't serialize read-only properties, or any properties registered or owned by a
                // a class in the framework (we only want to serialize custom properties), to be
                // consistent with our behavior for non-custom inlines.
                // Does the IsKnownType call make the IsPropertyKnown call unnecessary?
                if (!locallySetProperty.ReadOnly &&
                    !IsPropertyKnown(locallySetProperty, inheritableProperties, nonInheritableProperties) &&
                    !TextSchema.IsKnownType(locallySetProperty.OwnerType))
                {
                    object propertyValue = context.ReadLocalValue(locallySetProperty);
                    string stringValue = DPTypeDescriptorContext.GetStringValue(locallySetProperty, propertyValue);

                    if (stringValue != null)
                    {
                        stringValue = FilterNaNStringValueForDoublePropertyType(stringValue, locallySetProperty.PropertyType);

                        string propertyName = GetPropertyNameForElement(locallySetProperty, elementTypeStandardized, /*forceComplexName:*/false);
                        xmlWriter.WriteAttributeString(propertyName, stringValue);
                    }
                    else
                    {
                        complexProperties.SetValue(locallySetProperty, propertyValue);
                    }
                }
            }

// *** WE NEED TO BETTER UNDERSTAND THE IMPLICATIONS OF SERIALIZING NON-DP CLR PROPERTIES, SO THE REST OF
// *** THIS METHOD IS DISABLED UNTIL WE DECIDE THE BEST WAY TO HANDLE THEM.
// *** CLRTypeDescriptorContext is essentially the same as DPTypeDescriptorContext.
#if false
            // Check all CLR properties
            // Note that this is partially redundant.  TypeDescriptor.GetProperties, when called on a
            // DependencyObject, will return all properties that are set-- including all those already
            // serialized as Inheritable, NonInheritable, or LocallySet properties.  A potential
            // optimization, therefore, is to remove those serialization methods and simply use this one
            // for everything when we've opted into custom element serialization.
            PropertyDescriptorCollection descriptorCollection = TypeDescriptor.GetProperties(textPointer.Parent);
            IEnumerator descriptors = descriptorCollection.GetEnumerator();
            while (descriptors.MoveNext())
            {
                PropertyDescriptor current = (PropertyDescriptor)descriptors.Current;
                // ShouldSerializeValue() will return true for readonly properties that have explicitly
                // been told to serialize, such as Span.Inlines.  If we serialize a read-only property,
                // however, the parser will throw an exception when we try to deserialize.  So we
                // explicitly skip all read-only properties, and all DPs.
                if (!current.ShouldSerializeValue(textPointer.Parent) || current.IsReadOnly || current is MS.Internal.ComponentModel.DependencyObjectPropertyDescriptor)
                {
                    continue;
                }
                // Serialize the property
                object propertyValue = current.GetValue(textPointer.Parent);
                if (propertyValue != null)
                {
                    string stringValue = CLRTypeDescriptorContext.GetStringValue(current, propertyValue);

                    if (stringValue != null)
                    {
                        stringValue = FilterNaNStringValueForDoublePropertyType(stringValue, current.PropertyType);

                        xmlWriter.WriteAttributeString(current.Name, stringValue);
                    }
                    else
                    {
                        // Need to Support complex properties
                    }
                }
            }
#endif
        }

        private static bool IsPropertyKnown(DependencyProperty propertyToTest, DependencyProperty[] inheritableProperties, DependencyProperty[] nonInheritableProperties)
        {
            for (int i = 0; i < inheritableProperties.Length; i++)
            {
                DependencyProperty property = inheritableProperties[i];
                if (property == propertyToTest)
                {
                    return true;
                }
            }
            for (int i = 0; i < nonInheritableProperties.Length; i++)
            {
                DependencyProperty property = nonInheritableProperties[i];
                if (property == propertyToTest)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Writes complex properties in form of child elements with compound names
        /// </summary>
        private static void WriteComplexProperties(XmlWriter xmlWriter, DependencyObject complexProperties, Type elementType)
        {
            LocalValueEnumerator properties = complexProperties.GetLocalValueEnumerator();

            properties.Reset();
            while (properties.MoveNext())
            {
                LocalValueEntry propertyEntry = properties.Current;

                // Build an appropriate name for the complex property
                string complexPropertyName = GetPropertyNameForElement(propertyEntry.Property, elementType, /*forceComplexName:*/true);

                // Write the start element for the complex property.
                xmlWriter.WriteStartElement(complexPropertyName);

                // Serialize the complex property value from SaveAsXml().
                string complexPropertyXml = System.Windows.Markup.XamlWriter.Save(propertyEntry.Value);

                // Write the serialized complext property value as Xml.
                xmlWriter.WriteRaw(complexPropertyXml);

                // Close the element for the complex property
                xmlWriter.WriteEndElement();
            }
        }

        // Creates a name for the property which is consistent with xaml parser logic
        // When forceComplexName=true produces the TypeName.PropertyName notation unconditionally,
        // otherwise such complex name is produced only when the TypeName is different from elementType.Name.
        private static string GetPropertyNameForElement(DependencyProperty property, Type elementType, bool forceComplexName)
        {
            string propertyName;
            if (DependencyProperty.FromName(property.Name, elementType) == property)
            {
                // The elementType is an owner of this property, so we can use its name
                if (forceComplexName)
                {
                    propertyName = elementType.Name + "." + property.Name;
                }
                else
                {
                    propertyName = property.Name;
                }
            }
            else
            {
                // The elementType does not own this property, so we use the property's registered owner type name.
                propertyName = property.OwnerType.Name + "." + property.Name;
            }

            return propertyName;
        }

        // Serializes an element assuming that it does not have any children. Used for TableColumn
        // Need to unify this method with WriteEmbeddedObject. They both do the same job now.
        private static void WriteXamlAtomicElement(DependencyObject element, XmlWriter xmlWriter, bool reduceElement)
        {
            Type elementTypeStandardized = TextSchema.GetStandardElementType(element.GetType(), reduceElement);
            DependencyProperty[] elementProperties = TextSchema.GetNoninheritableProperties(elementTypeStandardized);

            xmlWriter.WriteStartElement(elementTypeStandardized.Name);

            for (int i = 0; i < elementProperties.Length; i++)
            {
                DependencyProperty property = elementProperties[i];
                object propertyValue = element.ReadLocalValue(property);
                if (propertyValue != null && propertyValue != DependencyProperty.UnsetValue)
                {
                    System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(property.PropertyType);
                    Invariant.Assert(typeConverter != null, "typeConverter==null: is not expected for atomic elements");
                    Invariant.Assert(typeConverter.CanConvertTo(typeof(string)), "type is expected to be convertable into string type");
                    string stringValue = (string)typeConverter.ConvertTo(/*ITypeDescriptorContext:*/null, CultureInfo.InvariantCulture, propertyValue, typeof(string));
                    Invariant.Assert(stringValue != null, "expecting non-null stringValue");
                    xmlWriter.WriteAttributeString(property.Name, stringValue);
                }
            }

            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Writes embeded object tag.
        /// </summary>
        /// <param name="embeddedObject">
        /// </param>
        /// <param name="xmlWriter">
        /// XmlWriter to output element opening tag.
        /// </param>
        /// <param name="wpfPayload">
        /// </param>
        private static void WriteEmbeddedObject(object embeddedObject, XmlWriter xmlWriter, WpfPayload wpfPayload)
        {
            if (wpfPayload != null && embeddedObject is Image)
            {
                // Writing in WPF mode: need to create an image with a Source referring into a package
                Image image = (Image)embeddedObject;

                if (image.Source != null && !string.IsNullOrEmpty(image.Source.ToString()))
                {
                    // Add the image to the Image collection in the package
                    // and define the reference to image into the package
                    string imageSource = wpfPayload.AddImage(image);
                    if (imageSource != null)
                    {
                        Type elementTypeStandardized = typeof(Image);

                        // Write opening tag for the element
                        xmlWriter.WriteStartElement(elementTypeStandardized.Name);

                        // Write all properties except for Source
                        DependencyProperty[] imageProperties = TextSchema.ImageProperties;

                        DependencyObject complexProperties = new DependencyObject();

                        for (int i = 0; i < imageProperties.Length; i++)
                        {
                            DependencyProperty property = imageProperties[i];
                            if (property != Image.SourceProperty)
                            {
                                object value = image.GetValue(property);

                                // Write the property as attribute string or add it to a list of complex properties.
                                WriteNoninheritableProperty(xmlWriter, property, value, elementTypeStandardized, /*onlyAffected:*/true, complexProperties, image.ReadLocalValue(property));
                            }
                        }

                        // Write Source property - as a local reference into the package container
                        // Write Source property as the complex property to specify the BitmapImage
                        // cache option as "OnLoad" instead of the default "OnDeman". Otherwise,
                        // we couldn't load the image by disposing WpfPayload package.
                        xmlWriter.WriteStartElement(typeof(Image).Name + "." + Image.SourceProperty.Name);
                        xmlWriter.WriteStartElement(typeof(System.Windows.Media.Imaging.BitmapImage).Name);
                        xmlWriter.WriteAttributeString(System.Windows.Media.Imaging.BitmapImage.UriSourceProperty.Name, imageSource);
                        xmlWriter.WriteAttributeString(System.Windows.Media.Imaging.BitmapImage.CacheOptionProperty.Name, "OnLoad");
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndElement();

                        // Write remaining complex properties
                        WriteComplexProperties(xmlWriter, complexProperties, elementTypeStandardized);

                        // Close the element
                        xmlWriter.WriteEndElement();
                    }
                }
            }
            else
            {
                // In non-package mode we ignore all UIElements.
                // Output a space replacing this embedded element.
                // Note that in this mode (DataFormats.Xaml) InlineUIContainer was
                // replaced by Run and BlockUIContainer - by Paragraph,
                // so the space output here will be significant.
                xmlWriter.WriteString(" ");
            }
        }

        // .............................................................
        //
        // Pasting
        //
        // .............................................................

        // Handles a special case for pasting a single embedded element -
        // needs to choose between BlockUIContainer and InlineUIContainer.
        private static bool PasteSingleEmbeddedElement(TextRange range, TextElement fragment)
        {
            if (fragment.ContentStart.GetOffsetToPosition(fragment.ContentEnd) == 3)
            {
                TextElement uiContainer = fragment.ContentStart.GetAdjacentElement(LogicalDirection.Forward) as TextElement;
                FrameworkElement embeddedElement = null;
                if (uiContainer is BlockUIContainer)
                {
                    embeddedElement = ((BlockUIContainer)uiContainer).Child as FrameworkElement;
                    if (embeddedElement != null)
                    {
                        ((BlockUIContainer)uiContainer).Child = null;
                    }
                }
                else if (uiContainer is InlineUIContainer)
                {
                    embeddedElement = ((InlineUIContainer)uiContainer).Child as FrameworkElement;
                    if (embeddedElement != null)
                    {
                        ((InlineUIContainer)uiContainer).Child = null;
                    }
                }

                if (embeddedElement != null)
                {
                    range.InsertEmbeddedUIElement(embeddedElement);
                    return true;
                }
            }

            return false;
        }

        private static void PasteTextFragment(TextElement fragment, TextRange range)
        {
            Invariant.Assert(range.IsEmpty, "range must be empty at this point - emptied by a caller");
            Invariant.Assert(fragment is Section || fragment is Span, "The wrapper element must be a Section or Span");

            // Define insertion position.
            TextPointer insertionPosition = TextRangeEditTables.EnsureInsertionPosition(range.End);

            // Check if our insertion position has a non-splittable Inline ancestor such as Hyperlink element.
            // Since we cannot split such Inline, we must switch to Text mode for pasting.
            // Note that this also has the side effect of converting paragraph breaks to space characters.
            if (insertionPosition.HasNonMergeableInlineAncestor)
            {
                PasteNonMergeableTextFragment(fragment, range);
            }
            else
            {
                PasteMergeableTextFragment(fragment, range, insertionPosition);
            }
        }

        // Helper for PasteTextFragment
        private static void PasteNonMergeableTextFragment(TextElement fragment, TextRange range)
        {
            // We cannot split Hyperlink or other non-splittable inline ancestor.
            // Paste text content of fragment in such case.

            // Get text content to be pasted.
            string fragmentText = TextRangeBase.GetTextInternal(fragment.ElementStart, fragment.ElementEnd);

            // Paste text into our empty target range.
            // Replace this with SetTextInternal when it is implemented.
            range.Text = fragmentText;

            // Select pasted content
            range.Select(range.Start, range.End);
        }

        // Helper for PasteTextFragment
        private static void PasteMergeableTextFragment(TextElement fragment, TextRange range, TextPointer insertionPosition)
        {
            TextPointer fragmentStart;
            TextPointer fragmentEnd;

            if (fragment is Span)
            {
                // Split structure at insertion point in target
                insertionPosition = TextRangeEdit.SplitFormattingElements(insertionPosition, /*keepEmptyFormatting:*/false);
                Invariant.Assert(insertionPosition.Parent is Paragraph, "insertionPosition must be in a scope of a Paragraph after splitting formatting elements");

                // Move the whole Span into the insertion point
                fragment.RepositionWithContent(insertionPosition);

                // Store edge positions of inserted content
                fragmentStart = fragment.ElementStart;
                fragmentEnd = fragment.ElementEnd;

                // Remove wrapper from a tree
                fragment.Reposition(null, null);
                ValidateMergingPositions(typeof(Inline), fragmentStart, fragmentEnd);

                // Transfer inheritable contextual properties
                ApplyContextualProperties(fragmentStart, fragmentEnd, fragment);
            }
            else
            {
                // Correct leading nested List elements in the fragment
                CorrectLeadingNestedLists((Section)fragment);

                // Split a paragraph at insertion position
                bool needFirstParagraphMerging = SplitParagraphForPasting(ref insertionPosition);

                // Move the whole Section into the insertion point
                fragment.RepositionWithContent(insertionPosition);

                // Store edge positions of inserted content
                fragmentStart = fragment.ElementStart;
                fragmentEnd = fragment.ElementEnd.GetPositionAtOffset(0, LogicalDirection.Forward); // need forward orientation to stick with the following content during merge at fragmentStart position

                // And unwrap the root Section
                fragment.Reposition(null, null);
                ValidateMergingPositions(typeof(Block), fragmentStart, fragmentEnd);

                // Transfer inheritable contextual properties
                ApplyContextualProperties(fragmentStart, fragmentEnd, fragment);

                // Merge paragraphs on fragment boundaries
                if (needFirstParagraphMerging)
                {
                    MergeParagraphsAtPosition(fragmentStart, /*mergingOnFragmentStart:*/true);
                }

                // Get an indication that we need to merge last paragraph
                if (!((Section)fragment).HasTrailingParagraphBreakOnPaste)
                {
                    MergeParagraphsAtPosition(fragmentEnd, /*mergingOnFragmentStart:*/false);
                }
            }

            // Whole-Document Heuristic: When initial content was empty we need to transfer flowDocument's properties: PageSize, FlowDirection, etc.

            // For paragraph pasting move range end to the following paragraph, because
            // it must include an ending paragraph break (in case of no-merging)
            if (fragment is Section && ((Section)fragment).HasTrailingParagraphBreakOnPaste)
            {
                fragmentEnd = fragmentEnd.GetInsertionPosition(LogicalDirection.Forward);
            }

            // Select pasted content
            range.Select(fragmentStart, fragmentEnd);
        }

        // Removes nested ListItems in the beginning of a fragment
        // to avoid multiple bulleting.
        // Better to avoid producing such nested ListItems on copy.
        private static void CorrectLeadingNestedLists(Section fragment)
        {
            List list = fragment.Blocks.FirstBlock as List;
            while (list != null)
            {
                ListItem listItem = list.ListItems.FirstListItem;
                if (listItem == null)
                {
                    return;
                }

                if (listItem.NextListItem != null)
                {
                    return;
                }

                List nestedList = listItem.Blocks.FirstBlock as List;
                if (nestedList == null)
                {
                    return;
                }

                // So we have nested list in the very beginning of the outer single-item list:
                // remove that outer list
                listItem.Reposition(null, null);
                list.Reposition(null, null);

                list = nestedList;
            }
        }

        // Decides whether we need to split a paragraph before pasting a fragment or not.
        // Splits the paragraph if needed, or simply moves the insertionPosition before its start.
        // Returns true if splitting happened and consequently merging is required after pasting.
        private static bool SplitParagraphForPasting(ref TextPointer insertionPosition)
        {
            bool needFirstParagraphMerging = true; // we need splitting unless the position os at the bery beginniong of a paragraph

            // When the insertion position is at the beginning of a paragraph we can avoid
            // splitting and then merging paragraphs at fragment start position.
            // This is not a pref consideration. We do not want an empty paragraph
            // would kill a formatting of a first pasted paragraphs (say, ListItem of a pasted List).
            TextPointer positionBeforeParagraph = insertionPosition;
            // Skip formatting tags
            while (positionBeforeParagraph.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.IsFormattingType(positionBeforeParagraph.Parent.GetType()))
            {
                positionBeforeParagraph = positionBeforeParagraph.GetNextContextPosition(LogicalDirection.Backward);
            }
            while (positionBeforeParagraph.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                TextSchema.AllowsParagraphMerging(positionBeforeParagraph.Parent.GetType()))
            {
                needFirstParagraphMerging = false;
                positionBeforeParagraph = positionBeforeParagraph.GetNextContextPosition(LogicalDirection.Backward);
            }
            if (!needFirstParagraphMerging)
            {
                // Insertion position was in the beginning of a paragraph.
                // No need in splitting/merging at fragment start
                insertionPosition = positionBeforeParagraph;
            }
            else
            {
                // split paragraph to create an insertion positionn at block level
                insertionPosition = TextRangeEdit.InsertParagraphBreak(insertionPosition, /*moveIntoSecondParagraph:*/false);
            }

            // When insertionPosition is inside a ListItem, then InsertParagraphBreak will
            // split not only a parent Paragraph but also a ListItem and return a position
            // between ListItems. This position is not good for inserting Block elements,
            // so we also need to split parent List element.
            // In a case when insertionPosition was at the beginning of a paragraph,
            // we still can end up being between ListItems, so again need to split a parent List.
            if (insertionPosition.Parent is List)
            {
                insertionPosition = TextRangeEdit.SplitElement(insertionPosition);
            }

            return needFirstParagraphMerging;
        }

        // Merges two paragraphs preceding and following the given position
        private static void MergeParagraphsAtPosition(TextPointer position, bool mergingOnFragmentStart)
        {
            TextPointer navigator = position;
            while (navigator != null && !(navigator.Parent is Paragraph))
            {
                if (navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd)
                {
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Backward);
                }
                else
                {
                    navigator = null;
                }
            }

            if (navigator != null)
            {
                Invariant.Assert(navigator.Parent is Paragraph, "We suppose have a first paragraph found");
                Paragraph firstParagraph = (Paragraph)navigator.Parent;

                navigator = position;
                while (navigator != null && !(navigator.Parent is Paragraph))
                {
                    if (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                    {
                        navigator = navigator.GetNextContextPosition(LogicalDirection.Forward);
                    }
                    else
                    {
                        navigator = null;
                    }
                }

                if (navigator != null)
                {
                    Invariant.Assert(navigator.Parent is Paragraph, "We suppose a second paragraph found");
                    Paragraph secondParagraph = (Paragraph)navigator.Parent;

                    if (TextRangeEditLists.ParagraphsAreMergeable(firstParagraph, secondParagraph))
                    {
                        TextRangeEditLists.MergeParagraphs(firstParagraph, secondParagraph);
                    }
                    else if (mergingOnFragmentStart && firstParagraph.TextRange.IsEmpty)
                    {
                        firstParagraph.RepositionWithContent(null);
                    }
                    else if (!mergingOnFragmentStart && secondParagraph.TextRange.IsEmpty)
                    {
                        secondParagraph.RepositionWithContent(null);
                    }
                }
            }
        }

        // Validates that the sibling element at this position belong to expected itemType (Inline, Block, ListItem)
        private static void ValidateMergingPositions(Type itemType, TextPointer start, TextPointer end)
        {
            if (start.CompareTo(end) < 0)
            {
                // Verify inner part
                TextPointerContext forwardFromStart = start.GetPointerContext(LogicalDirection.Forward);
                TextPointerContext backwardFromEnd = end.GetPointerContext(LogicalDirection.Backward);
                Invariant.Assert(forwardFromStart == TextPointerContext.ElementStart, "Expecting first opening tag of pasted fragment");
                Invariant.Assert(backwardFromEnd == TextPointerContext.ElementEnd, "Expecting last closing tag of pasted fragment");
                Invariant.Assert(itemType.IsAssignableFrom(start.GetAdjacentElement(LogicalDirection.Forward).GetType()), "The first pasted fragment item is expected to be a " + itemType.Name);
                Invariant.Assert(itemType.IsAssignableFrom(end.GetAdjacentElement(LogicalDirection.Backward).GetType()), "The last pasted fragment item is expected to be a " + itemType.Name);

                // Veryfy outer part
                TextPointerContext backwardFromStart = start.GetPointerContext(LogicalDirection.Backward);
                TextPointerContext forwardFromEnd = end.GetPointerContext(LogicalDirection.Forward);
                Invariant.Assert(backwardFromStart == TextPointerContext.ElementStart || backwardFromStart == TextPointerContext.ElementEnd || backwardFromStart == TextPointerContext.None, "Bad context preceding a pasted fragment");
                Invariant.Assert(!(backwardFromStart == TextPointerContext.ElementEnd) || itemType.IsAssignableFrom(start.GetAdjacentElement(LogicalDirection.Backward).GetType()), "An element preceding a pasted fragment is expected to be a " + itemType.Name);
                Invariant.Assert(forwardFromEnd == TextPointerContext.ElementStart || forwardFromEnd == TextPointerContext.ElementEnd || forwardFromEnd == TextPointerContext.None, "Bad context following a pasted fragment");
                Invariant.Assert(!(forwardFromEnd == TextPointerContext.ElementStart) || itemType.IsAssignableFrom(end.GetAdjacentElement(LogicalDirection.Forward).GetType()), "An element following a pasted fragment is expected to be a " + itemType.Name);
            }
        }

        // Helper function used to set default value for an indicator requesting to merge last paragraph.
        private static void AdjustFragmentForTargetRange(TextElement fragment, TextRange range)
        {
            if (fragment is Section && ((Section)fragment).HasTrailingParagraphBreakOnPaste)
            {
                // Explicit indicator is missing, we need to set it by default.
                // In a case of TextRange.Xml property assignment we assume that
                // user expects to insert as many paragraphs new paragraphs as her pasted xaml contains.
                // The expection must be done to the case when the target range is
                // extended beyond the last paragraph - then we must merge last paragraph
                // to avoid extra paragraph creation at the end (one additional paragraph
                // will be created in this case by Pasting code before pasting).
                // The other case for exception is when target TextContainer is empty -
                // in this case we as well want to merge last paragraph with the following
                // one (which will be created as part of paragraph enforcement in pasting operation).
                // The both desired conditions - IsAfterLastParagraph and "in empty container"
                // can be identified by the following simple test - range.End is not at end-of-doc.
                ((Section)fragment).HasTrailingParagraphBreakOnPaste = range.End.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.None;
            }
        }

        // Applies a whole property bag to a range from start to end to simulate inheritance of this property from source conntext
        private static void ApplyContextualProperties(TextPointer start, TextPointer end, TextElement propertyBag)
        {
            Invariant.Assert(propertyBag.IsEmpty && propertyBag.Parent == null, "propertyBag is supposed to be an empty element outside any tree");

            LocalValueEnumerator contextualProperties = propertyBag.GetLocalValueEnumerator();

            while (start.CompareTo(end) < 0 && contextualProperties.MoveNext())
            {
                // Note: we repeatedly check for IsEmpty because the selection
                // may become empty as a result of normalization after formatting
                // (thai character sequence).
                LocalValueEntry propertyEntry = contextualProperties.Current;
                DependencyProperty property = propertyEntry.Property;
                if (TextSchema.IsCharacterProperty(property) &&
                    TextSchema.IsParagraphProperty(property))
                {
                    // In case a property is both an Inline and Paragraph property,
                    // propertyBag element type (section or span) decides how it should be applied.
                    if (TextSchema.IsBlock(propertyBag.GetType()))
                    {
                        ApplyContextualProperty(typeof(Block), start, end, property, propertyEntry.Value);
                    }
                    else
                    {
                        ApplyContextualProperty(typeof(Inline), start, end, property, propertyEntry.Value);
                    }
                }
                else if (TextSchema.IsCharacterProperty(property))
                {
                    ApplyContextualProperty(typeof(Inline), start, end, property, propertyEntry.Value);
                }
                else if (TextSchema.IsParagraphProperty(property))
                {
                    ApplyContextualProperty(typeof(Block), start, end, property, propertyEntry.Value);
                }
            }

            // Merge formatting elements at end position
            TextRangeEdit.MergeFormattingInlines(start);
            TextRangeEdit.MergeFormattingInlines(end);
        }

        // Applies one property to a range from start to end to simulate inheritance of this property from source conntext
        private static void ApplyContextualProperty(Type targetType, TextPointer start, TextPointer end, DependencyProperty property, object value)
        {
            if (TextSchema.ValuesAreEqual(start.Parent.GetValue(property), value))
            {
                return; // The property at insertion position is the same as it was in source context. Nothing to do.
            }

            // Advance start pointer to enter pasted fragment
            start = start.GetNextContextPosition(LogicalDirection.Forward);

            while (start != null && start.CompareTo(end) < 0)
            {
                TextPointerContext passedContext = start.GetPointerContext(LogicalDirection.Backward);
                if (passedContext == TextPointerContext.ElementStart)
                {
                    TextElement element = (TextElement)start.Parent;

                    // Check if this element affects the property in question
                    if (element.ReadLocalValue(property) != DependencyProperty.UnsetValue ||
                        !TextSchema.ValuesAreEqual(element.GetValue(property), element.Parent.GetValue(property)))
                    {
                        // The element affects this property, so we can skip it
                        start = element.ElementEnd;
                    }
                    else if (targetType.IsAssignableFrom(element.GetType()))
                    {
                        start = element.ElementEnd;

                        if (targetType == typeof(Block) && start.CompareTo(end) > 0)
                        {
                            // Contextual properties should not apply to the last paragraph
                            // when it is merged with the following content -
                            // to avoid affecting the folowing visible content formatting.
                            break;
                        }

                        // This is topmost-level inline element which inherits this property.
                        // Set the value explicitly
                        if (!TextSchema.ValuesAreEqual(value, element.GetValue(property)))
                        {
                            element.ClearValue(property);
                            if (!TextSchema.ValuesAreEqual(value, element.GetValue(property)))
                            {
                                element.SetValue(property, value);
                            }

                            TextRangeEdit.MergeFormattingInlines(element.ElementStart);
                        }
                    }
                    else
                    {
                        // Traverse down into a structured (non-innline) element
                        start = start.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }
                else
                {
                    // Traverse up from any element
                    Invariant.Assert(passedContext != TextPointerContext.None, "TextPointerContext.None is not expected");
                    start = start.GetNextContextPosition(LogicalDirection.Forward);
                }
            }
        }

        // Returns a navigator scoped in the common ancestor for the start and end positions
        // The navigator is positioned in the beginning of the ancestor's content.
        // Modifies the common ancestor for hyperlink serialization heuristic - in case when the range is positioned at hyperlink boundaries.
        // Since we need to write a hyperlink wrapper in this case, navigator is positioned before hyperlink element start.
        private static ITextPointer FindSerializationCommonAncestor(ITextRange range)
        {
            // Create navigators for tree traversing looking for commonAncestor
            ITextPointer commonAncestor = range.Start.CreatePointer();
            ITextPointer runningEnd = range.End.CreatePointer();

            // Find nearest common ancestor
            while (!commonAncestor.HasEqualScope(runningEnd))
            {
                // Run all way from end up the tree to check if start is ancestor
                runningEnd.MoveToPosition(range.End);
                while (typeof(TextElement).IsAssignableFrom(runningEnd.ParentType) && !runningEnd.HasEqualScope(commonAncestor))
                {
                    runningEnd.MoveToElementEdge(ElementEdge.AfterEnd);
                }

                if (runningEnd.HasEqualScope(commonAncestor))
                {
                    break;
                }

                // Move start one level up
                commonAncestor.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            while (!IsAcceptableAncestor(commonAncestor, range))
            {
                commonAncestor.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            if (typeof(TextElement).IsAssignableFrom(commonAncestor.ParentType))
            {
                commonAncestor.MoveToElementEdge(ElementEdge.AfterStart);

                // Check for special case, when range start and end are at Hyperlink boundaries.
                // Need to expand commonAncestor to Hyperlink element start, so that Hyperlink wrapper is written.
                ITextPointer hyperlinkStart = GetHyperlinkStart(range);
                if (hyperlinkStart != null)
                {
                    commonAncestor = hyperlinkStart;
                }
            }
            else
            {
                commonAncestor.MoveToPosition(commonAncestor.TextContainer.Start);
            }

            return commonAncestor;
        }

        // Verify that a pointer is an acceptable ancestor.  Some types can't be ancestors at all, while
        // non-typographic-only elements are unacceptable if the range being serialized does not include the
        // element's start and end (because we don't want to serialize properties on such an element).
        private static bool IsAcceptableAncestor(ITextPointer commonAncestor, ITextRange range)
        {
            if (typeof(TableRow).IsAssignableFrom(commonAncestor.ParentType) ||
                typeof(TableRowGroup).IsAssignableFrom(commonAncestor.ParentType) ||
                typeof(Table).IsAssignableFrom(commonAncestor.ParentType) ||
                typeof(BlockUIContainer).IsAssignableFrom(commonAncestor.ParentType) ||
                typeof(List).IsAssignableFrom(commonAncestor.ParentType) ||
                typeof(Inline).IsAssignableFrom(commonAncestor.ParentType) && TextSchema.HasTextDecorations(commonAncestor.GetValue(Inline.TextDecorationsProperty)))
            {
                return false;
            }

            // We don't want to use any formatting from within a non-typographic-only element unless the entire
            // element is selected (in which case, the ancestor candidate will already be outside that element.
            // If there is such an element ANYWHERE in the ancestry, the only acceptable
            // ancestor is outside the outermost such element.
            ITextPointer navigator = commonAncestor.CreatePointer();
            while (typeof(TextElement).IsAssignableFrom(navigator.ParentType))
            {
                TextElementEditingBehaviorAttribute behaviorAttribute = (TextElementEditingBehaviorAttribute)Attribute.GetCustomAttribute(navigator.ParentType, typeof(TextElementEditingBehaviorAttribute));
                if (behaviorAttribute != null && !behaviorAttribute.IsTypographicOnly)
                {
                    return false;
                }
                navigator.MoveToElementEdge(ElementEdge.BeforeStart);
            }

            return true;
        }

        // Removes (inplace) any invalid surrogate chars from an array.
        // Returns the new array length, text.Length minus any stripped
        // chars.
        //
        // Unicode surrogates are 32 bit references to abstract chars.
        // A valid surrogate pair consists of a 16 bit code point (the
        // high surrogate) in the range u+d800 - u+dbff, followed by a
        // second 16 bit code point (the low surrogate) in the range
        // u+dc00 - u+dfff.
        //
        // An invalid surrogate is a high surrogate not followed by a
        // low surrogate, or a low surrogate not preceeded by a high
        // surrogate.
        //
        // Length specifies the number of characters in text to examine,
        // characters past length are ignored (as if text.Length == length).
        //
        // Also removes nul characters
        private static int StripInvalidSurrogateChars(char[] text, int length)
        {
            int count;

            Invariant.Assert(text.Length >= length, "Asserting that text.Length >= length");

            int i;
            for (i = 0; i < length; i++)
            {
                char testChar = text[i];
                if (Char.IsHighSurrogate(testChar) || Char.IsLowSurrogate(testChar) || IsBadCode(testChar))
                {
                    break;
                }
            }

            if (i == length)
            {
                // No surrogates, early out.
                count = length;
            }
            else
            {
                count = i;

                for (; i < length; i++)
                {
                    if (Char.IsHighSurrogate(text[i]))
                    {
                        if (i + 1 < length && Char.IsLowSurrogate(text[i + 1]))
                        {
                            // Valid surrogate encountered.
                            text[count] = text[i];
                            text[count + 1] = text[i + 1];
                            count += 2;
                            i++; // Skip over low surrogate.
                        }
                        else
                        {
                            // Bogus high surrogte encountered -- remove it.
                            // Simply don't update destinationIndex or count.
                        }
                    }
                    else if (Char.IsLowSurrogate(text[i]))
                    {
                        // Bogus low surrogate encountered -- remove it.
                        // Simply don't update destinationIndex or count.
                    }
                    else if (IsBadCode(text[i]))
                    {
                        // nul character enountered - remove it.
                        // Simply don't update destinationIndex or count.
                        // What about other control codes? Shouldn't they be also stripped out?
                    }
                    else
                    {
                        // Non-surrogate encountered.
                        text[count] = text[i];
                        count += 1;
                    }
                }
            }

            return count;
        }

        private static bool IsBadCode(char code)
        {
            return (code < ' ' && code != '\x0009' && code != '\x000A' && code != '\x000D');
        }

        /// <summary>
        /// Return true if rangeEnd is not at the end of an element.
        ///
        /// textReader must already be at the start of the element.
        /// </summary>
        private static bool IsPartialNonTypographic(ITextPointer textReader, ITextPointer rangeEnd)
        {
            bool isPartial = false;

            Invariant.Assert(textReader.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart);

            ITextPointer elementNavigation = textReader.CreatePointer();
            ITextPointer elementEnd = textReader.CreatePointer();

            elementEnd.MoveToNextContextPosition(LogicalDirection.Forward);

            // Find the end position
            elementEnd.MoveToElementEdge(ElementEdge.AfterEnd);

            if (elementEnd.CompareTo(rangeEnd) > 0)
            {
                isPartial = true;
            }

            return isPartial;
        }

        /// <summary>
        /// Return true if Hyperlink range is invalid.
        /// Hyperlink is invalid if it include a UiElement except Image or the range end position
        /// is stated before the end position of hyperlink.
        /// This must be called before Hyperlink start element position.
        /// </summary>
        private static bool IsHyperlinkInvalid(ITextPointer textReader, ITextPointer rangeEnd)
        {
            // TextRead must be on the position before the element start position of Hyperlink
            Invariant.Assert(textReader.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart);
            Invariant.Assert(typeof(Hyperlink).IsAssignableFrom(textReader.GetElementType(LogicalDirection.Forward)));

            bool hyperlinkInvalid = false;

            // Get the forward adjacent element and cast Hyperlink hardly since it must be Hyperlink
            Hyperlink hyperlink = (Hyperlink)textReader.GetAdjacentElement(LogicalDirection.Forward);

            ITextPointer hyperlinkNavigation = textReader.CreatePointer();
            ITextPointer hyperlinkEnd = textReader.CreatePointer();

            hyperlinkEnd.MoveToNextContextPosition(LogicalDirection.Forward);

            // Find the hyperlink end position
            hyperlinkEnd.MoveToElementEdge(ElementEdge.AfterEnd);

            // Hyperlink end position is stated after the range end position.
            if (hyperlinkEnd.CompareTo(rangeEnd) > 0)
            {
                hyperlinkInvalid = true;
            }
            else
            {
                // Check whether the hyperlink having a UiElement except Image until hyperlink end position
                while (hyperlinkNavigation.CompareTo(hyperlinkEnd) < 0)
                {
                    InlineUIContainer inlineUIContainer = hyperlinkNavigation.GetAdjacentElement(LogicalDirection.Forward) as InlineUIContainer;
                    if (inlineUIContainer != null && !(inlineUIContainer.Child is Image))
                    {
                        hyperlinkInvalid = true;
                        break;
                    }

                    hyperlinkNavigation.MoveToNextContextPosition(LogicalDirection.Forward);
                }
            }

            return hyperlinkInvalid;
        }

        // Returns a position before hyperlink element start if passed range start and end are at hyperlink boundaries. Otherwise, null.
        private static ITextPointer GetHyperlinkStart(ITextRange range)
        {
            ITextPointer hyperlinkStart = null;

            if (TextPointerBase.IsAtNonMergeableInlineStart(range.Start) && TextPointerBase.IsAtNonMergeableInlineEnd(range.End))
            {
                // Find a position at hyperlink start.
                hyperlinkStart = range.Start.CreatePointer(LogicalDirection.Forward);
                while (hyperlinkStart.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart &&
                    !typeof(Hyperlink).IsAssignableFrom(hyperlinkStart.ParentType))
                {
                    hyperlinkStart.MoveToElementEdge(ElementEdge.BeforeStart);
                }
                hyperlinkStart.MoveToElementEdge(ElementEdge.BeforeStart);
                hyperlinkStart.Freeze();
            }

            return hyperlinkStart;
        }

        private static string FilterNaNStringValueForDoublePropertyType(string stringValue, Type propertyType)
        {
            if (propertyType == typeof(double) &&
                String.Compare(stringValue, "NaN", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return "Auto"; // convert NaN to Auto, to keep parser happy
            }

            return stringValue;
        }

        #endregion Private Methods

        // -------------------------------------------------------------
        //
        // Private Constants
        //
        // -------------------------------------------------------------

        #region Private Constants

        // A structural depth of a empty FlowDocument fragment
        private const int EmptyDocumentDepth = 1;

        #endregion Private Constants
    }
}
