// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Internal static class representing a group of methods
//              for list editing
//

namespace System.Windows.Documents
{
    using System;
    using MS.Internal;
    using System.Windows.Controls;

    /// <summary>
    /// The TextRange class represents a pair of TextPositions, with many
    /// rich text editing operations exposed.
    /// </summary>
    internal static class TextRangeEditLists
    {
        // --------------------------------------------------------------------
        //
        // Internal Methods
        //
        // --------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Merges two paragraphs followinng one another.
        /// The content of a second paragraph is moved into the end
        /// of the first one.
        /// </summary>
        /// <param name="firstParagraphOrBlockUIContainer">
        /// First of two merged paragraphs or BlockUIContainer.
        /// </param>
        /// <param name="secondParagraphOrBlockUIContainer">
        /// Second of two mered paragraphs or BlockUIContainer.
        /// </param>
        /// <returns>
        /// true if paragraphs have been merged; false if no actions where made.
        /// </returns>
        internal static bool MergeParagraphs(Block firstParagraphOrBlockUIContainer, Block secondParagraphOrBlockUIContainer)
        {
            if (!ParagraphsAreMergeable(firstParagraphOrBlockUIContainer, secondParagraphOrBlockUIContainer))
            {
                return false; // Cannot mearge these paragraphs.
            }

            // Store parent list item of a second paragraph -
            // to correct its structure after the merge
            ListItem secondListItem = secondParagraphOrBlockUIContainer.PreviousBlock == null ? secondParagraphOrBlockUIContainer.Parent as ListItem : null;

            if (secondListItem != null && secondListItem.PreviousListItem == null && secondParagraphOrBlockUIContainer.NextBlock is List)
            {
                // The second paragraph is a first list item in some list.
                // It has a sublists in it, so this sublist must be unindented
                // to avoid double bulleted line.
                List sublistOfSecondParagraph = (List)secondParagraphOrBlockUIContainer.NextBlock;
                if (sublistOfSecondParagraph.ElementEnd.CompareTo(secondListItem.ContentEnd) == 0)
                {
                    secondListItem.Reposition(null, null);
                }
                else
                {
                    secondListItem.Reposition(sublistOfSecondParagraph.ElementEnd, secondListItem.ContentEnd);
                }
                // At this point the schema is temporaty broken: the secondParagraph and the sublistOfSecondParagraph have List as a parent
                sublistOfSecondParagraph.Reposition(null, null);
                // The schema is repared as to sublistOfSecondParagraph concern, but still broken for secondParagraph - must be corrected in the following code
            }

            // Move the second paragraph out of its wrappers separating from the first paragraph (if any).
            // We can not use RepositionWithContent because it would destroy
            // all pointers and ranges within a moved paragraph.
            // Instead we reposition elements around the two paragraphs.
            while (secondParagraphOrBlockUIContainer.ElementStart.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
            {
                TextElement parentBlock = (TextElement)secondParagraphOrBlockUIContainer.Parent; 
                Invariant.Assert(parentBlock != null);
                Invariant.Assert(TextSchema.AllowsParagraphMerging(parentBlock.GetType()));

                if (secondParagraphOrBlockUIContainer.ElementEnd.CompareTo(parentBlock.ContentEnd) == 0)
                {
                    // Remove ancestor block if it becomes empty
                    parentBlock.Reposition(null, null);
                }
                else
                {
                    // Move ancestor's Start after the end of our paragraph
                    parentBlock.Reposition(secondParagraphOrBlockUIContainer.ElementEnd, parentBlock.ContentEnd);
                }
            }

            // Store a position after the second paragraph where list merging may be needed
            TextPointer positionAfterSecondParagraph = secondParagraphOrBlockUIContainer.ElementEnd.GetFrozenPointer(LogicalDirection.Forward);

            // Move the second paragraph to become an immediate following sibling of the first paragraph
            while (true)
            {
                TextElement previousBlock = secondParagraphOrBlockUIContainer.ElementStart.GetAdjacentElement(LogicalDirection.Backward) as TextElement;
                // Note: We cannot use Block.NextSibling property, because the structure is invalid during this process

                Invariant.Assert(previousBlock != null);

                if (previousBlock is Paragraph || previousBlock is BlockUIContainer)
                {
                    break;
                }

                Invariant.Assert(TextSchema.AllowsParagraphMerging(previousBlock.GetType()));
                previousBlock.Reposition(previousBlock.ContentStart, secondParagraphOrBlockUIContainer.ElementEnd);
            }

            // Now that paragraphs are next to each other merge them.

            // If one of paragraphs is empty we will apply special logic - to preserve a formatting from a non-empty one
            if (secondParagraphOrBlockUIContainer.TextRange.IsEmpty)
            {
                secondParagraphOrBlockUIContainer.RepositionWithContent(null);
            }
            else if (firstParagraphOrBlockUIContainer.TextRange.IsEmpty)
            {
                firstParagraphOrBlockUIContainer.RepositionWithContent(null);
            }
            else if (firstParagraphOrBlockUIContainer is Paragraph && secondParagraphOrBlockUIContainer is Paragraph)
            {
                // Do reposition magic for merging paragraph content
                // without destroying any pointers positioned in them.
                // Pull the second paragraph into the first one
                Invariant.Assert(firstParagraphOrBlockUIContainer.ElementEnd.CompareTo(secondParagraphOrBlockUIContainer.ElementStart) == 0);
                firstParagraphOrBlockUIContainer.Reposition(firstParagraphOrBlockUIContainer.ContentStart, secondParagraphOrBlockUIContainer.ElementEnd);

                // Store inline merging position
                TextPointer inlineMergingPosition = secondParagraphOrBlockUIContainer.ElementStart;

                // Now we can delete the second paragraph
                secondParagraphOrBlockUIContainer.Reposition(null, null);

                // Merge formatting elements at the point of paragraphs merging
                TextRangeEdit.MergeFormattingInlines(inlineMergingPosition);
            }

            // Merge ListItems wrapping first and second paragraphs.
            ListItem followingListItem = positionAfterSecondParagraph.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart 
                ? positionAfterSecondParagraph.GetAdjacentElement(LogicalDirection.Forward) as ListItem : null;
            if (followingListItem != null && followingListItem == secondListItem)
            {
                ListItem precedingListItem = positionAfterSecondParagraph.GetAdjacentElement(LogicalDirection.Backward) as ListItem;
                if (precedingListItem != null)
                {
                    // Merge the second list item with the preceding one
                    Invariant.Assert(positionAfterSecondParagraph.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart);
                    Invariant.Assert(positionAfterSecondParagraph.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementEnd);
                    precedingListItem.Reposition(precedingListItem.ContentStart, followingListItem.ElementEnd);
                    followingListItem.Reposition(null, null);
                }
            }

            // Merge lists at merge position
            MergeLists(positionAfterSecondParagraph);

            return true;
        }

        /// <summary>
        /// Like MergeLists, but will search over formatting elements when
        /// looking for Lists to merge.
        /// </summary>
        internal static bool MergeListsAroundNormalizedPosition(TextPointer mergePosition)
        {
            // Search forward for a List to merge with.
            TextPointer navigator = mergePosition.CreatePointer();

            while (navigator.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                navigator.MoveToNextContextPosition(LogicalDirection.Forward);
            }

            bool merged = MergeLists(navigator);

            // Search backward for a List to merge with.
            if (!merged)
            {
                navigator.MoveToPosition(mergePosition);

                while (navigator.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart)
                {
                    navigator.MoveToNextContextPosition(LogicalDirection.Backward);
                }

                merged = MergeLists(navigator);
            }

            return merged;
        }

        /// <summary>
        /// Merges two naighboring lists ending and starting at position mergePosition
        /// </summary>
        /// <param name="mergePosition">
        /// Position at with two List elements are expected to appear next to each other
        /// </param>
        /// <returns>
        /// true if there were two mergeable List elements and merge happened.
        /// false if there is no pair of List elements at the mergePosition.
        /// </returns>
        internal static bool MergeLists(TextPointer mergePosition)
        {
            if (mergePosition.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.ElementEnd ||
                mergePosition.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.ElementStart)
            {
                return false;
            }

            List precedingList = mergePosition.GetAdjacentElement(LogicalDirection.Backward) as List;
            List followingList = mergePosition.GetAdjacentElement(LogicalDirection.Forward) as List;

            if (precedingList == null || followingList == null)
            {
                return false;
            }

            precedingList.Reposition(precedingList.ContentStart, followingList.ElementEnd);
            followingList.Reposition(null, null);

            // We need to set appropriate FlowDirection property on the new List and its paragraph children. 
            // We take the FlowDirection value from the preceding list.
            TextRangeEdit.SetParagraphProperty(precedingList.ElementStart, precedingList.ElementEnd,
                Paragraph.FlowDirectionProperty, precedingList.GetValue(Paragraph.FlowDirectionProperty));

            return true;
        }

        // Returns true if all paragraphs in a range belong to the same block,
        // so they can be easily grouped and ungrouped.
        internal static bool IsListOperationApplicable(TextRange range)
        {
            // First check, if range start/end are parented by ListItems within the same parent list.
            if (IsRangeWithinSingleList(range))
            {
                return true;
            }

            // Adjust range end so that it does not affect a following paragraph.
            TextPointer end = (TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End);

            // Now try plain Paragraphs
            Block firstBlock = range.Start.ParagraphOrBlockUIContainer;
            Block lastBlock = end.ParagraphOrBlockUIContainer;

            if (firstBlock != null && lastBlock != null && firstBlock.Parent == lastBlock.Parent)
            {
                return true;
            }

            // Allow list editing at potential paragraph positions, this includes
            // positions in initial RichTextBox, empty TableCell, empty ListItem where paragraphs were not yet created.
            if (range.IsEmpty && TextPointerBase.IsAtPotentialParagraphPosition(range.Start))
            {
                return true;
            }

            return false;
        }

        internal static bool ConvertParagraphsToListItems(TextRange range, TextMarkerStyle markerStyle)
        {
            if (range.IsEmpty && TextPointerBase.IsAtPotentialParagraphPosition(range.Start))
            {
                TextPointer insertionPosition = TextRangeEditTables.EnsureInsertionPosition(range.Start);
                ((ITextRange)range).Select(insertionPosition, insertionPosition);
            }

            Block firstBlock = range.Start.ParagraphOrBlockUIContainer;

            TextPointer end = (TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End);
            Block lastBlock = end.ParagraphOrBlockUIContainer;

            // We assume that a range contains a sequence of one-level paragraphs.
            // Otherwise the operation is disabled.
            if (firstBlock == null || lastBlock == null || firstBlock.Parent != lastBlock.Parent ||
                firstBlock.Parent is ListItem && firstBlock.PreviousBlock == null)
            {
                // Either the paragraphs belong to different scopes or first of them has a bullet already.
                // We cannot convert them into bulleted lists.
                return false;
            }

            // Check that all top-level elements of selection are Paragraphs.
            // We do not apply the command to Tables or Sections.
            for (Block block = firstBlock; block != lastBlock && block != null; block = block.NextBlock)
            {
                if (block is Table || block is Section)
                {
                    return false;
                }
            }

            ListItem parentListItem = firstBlock.Parent as ListItem;
            if (parentListItem != null)
            {
                // Paragraphs are inside of ListItem already.

                // Split a current ListItem before each of selected blocks
                Block block = firstBlock;
                while (block != null)
                {
                    Block nextBlock = block == lastBlock ? null : block.ElementEnd.GetAdjacentElement(LogicalDirection.Forward) as Block;

                    Invariant.Assert(block.Parent is ListItem);
                    TextRangeEdit.SplitElement(block.ElementStart);

                    block = nextBlock;
                }
            }
            else
            {
                // Create a list around all paragraphs
                List list = new List();
                list.MarkerStyle = markerStyle;
                list.Apply(firstBlock, lastBlock);

                // Merge with neighboring lists
                //MergeLists(list.ElementEnd);  // start with End to not loose an instance of "list" during merging with the following list
                //MergeLists(list.ElementStart);
            }

            return true;
        }

        // Assumes that a range contains a sequence of same-level ListItems.
        // Converts all these ListItems into Paragraphs and
        // either adds them to preceding ListItem (as non-bulleted continuation)
        // or pulls them out of a List if they start in the beginning of a List
        internal static void ConvertListItemsToParagraphs(TextRange range)
        {
            ListItem firstListItem = TextPointerBase.GetListItem(range.Start);
            ListItem lastListItem = TextPointerBase.GetListItem((TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End));

            // The range must be in a sequence of ListItems belonging to one List wrapper
            if (firstListItem == null || lastListItem == null || firstListItem.Parent != lastListItem.Parent || !(firstListItem.Parent is List))
            {
                return;
            }

            List listToRemove = null;

            ListItem leadingListItem = firstListItem.PreviousListItem;
            if (leadingListItem != null)
            {
                // We have a leading ListItem, so pull selected items into it
                leadingListItem.Reposition(leadingListItem.ContentStart, lastListItem.ElementEnd);
            }
            else
            {
                // We do not have a leading ListItem. So pull selected items out of a list

                // Cut wrapping list after endListItem
                if (lastListItem.NextListItem != null)
                {
                    TextRangeEdit.SplitElement(lastListItem.ElementEnd);
                }

                // Set list to remove
                listToRemove = firstListItem.List;              
            }

            // Remove ListItems from all selected blocks
            ListItem listItem = firstListItem;
            while (listItem != null)
            {
                ListItem nextListItem = listItem.ElementEnd.GetAdjacentElement(LogicalDirection.Forward) as ListItem;

                // If this is an empty <ListItem></ListItem>, insert an explicit paragraph in it before deleting the list item.
                if (listItem.ContentStart.CompareTo(listItem.ContentEnd) == 0)
                {
                    TextRangeEditTables.EnsureInsertionPosition(listItem.ContentStart);
                }

                listItem.Reposition(null, null);
                listItem = listItem == lastListItem ? null : nextListItem;
            }

            // If we have a list to remove, remove it and set its FlowDirection to its children
            if (listToRemove != null)
            {
                FlowDirection flowDirection = (FlowDirection)listToRemove.GetValue(Paragraph.FlowDirectionProperty);                
                listToRemove.Reposition(null, null);
                TextRangeEdit.SetParagraphProperty(range.Start, range.End, Paragraph.FlowDirectionProperty, flowDirection);
            }
        }

        internal static void IndentListItems(TextRange range)
        {
            ListItem firstListItem = TextPointerBase.GetImmediateListItem(range.Start);
            ListItem lastListItem = TextPointerBase.GetImmediateListItem((TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End));

            // The range must be in a sequence of ListItems belonging to one List wrapper
            if (firstListItem == null || lastListItem == null || 
                firstListItem.Parent != lastListItem.Parent || 
                !(firstListItem.Parent is List))
            {
                return;
            }

            // Identify a ListItem which will become a leading item for this potential sublist
            ListItem leadingListItem = firstListItem.PreviousListItem;
            if (leadingListItem == null)
            {
                // There is no leading list item for this group. Indentation is impossible
                return;
            }

            // Get current List
            List list = (List)firstListItem.Parent;

            // Wrap these items into a List - inheriting all properties from our current list
            List indentedList = (List)TextRangeEdit.InsertElementClone(firstListItem.ElementStart, lastListItem.ElementEnd, list);

            // Wrap the leading ListItem to include the sublist
            leadingListItem.Reposition(leadingListItem.ContentStart, indentedList.ElementEnd);

            // Unwrap sublist from the last selected list item (to keep it on its level)
            Paragraph leadingParagraphOfLastItem = lastListItem.Blocks.FirstBlock as Paragraph;
            if (leadingParagraphOfLastItem != null)
            {
                // Unindenting all items of a sublist - if it is the only following element of a list
                List nestedListOfLastItem = leadingParagraphOfLastItem.NextBlock as List;
                if (nestedListOfLastItem != null && nestedListOfLastItem.NextBlock == null)
                {
                    lastListItem.Reposition(lastListItem.ContentStart, nestedListOfLastItem.ElementStart);
                    nestedListOfLastItem.Reposition(null, null);
                }
            }

            // Merge with neighboring lists
            MergeLists(indentedList.ElementStart);
            // No need in merging at nestedList.ElementEnd as ListItem ends there.
        }

        internal static bool UnindentListItems(TextRange range)
        {
            // If listitems in this range cross a list boundary, we cannot unindent them.
            if (!IsRangeWithinSingleList(range))
            {
                return false;
            }

            ListItem firstListItem = TextPointerBase.GetListItem(range.Start);
            ListItem lastListItem = TextPointerBase.GetListItem((TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End));

            // At this point it is possible that lastListItem is a child of
            // firstListItem.
            //
            // This is due to a special case in IsRangeWithinSingleList
            // which allows the input TextRange to cross List boundaries only
            // in the case where the TextRange ends adjacent to an insertion
            // position at the same level as the start, e.g.:
            //
            //    - parent item
            //      - start item (range starts here)
            //          - child item (range ends here)
            //      <start item must not have any siblings>
            //
            // Here we check for that special case and ensure that
            // lastListItem is at the same level as firstListItem.

            TextElement parent = (TextElement)lastListItem.Parent;

            while (parent != firstListItem.Parent)
            {
                lastListItem = parent as ListItem;
                parent = (TextElement)parent.Parent;
            }
            if (lastListItem == null)
            {
                // This can happen if the input is a fragment, a collection
                // of ListItems not parented by an outer List.
                return false;
            }

            // Cut wrapping list before startListItem
            if (firstListItem.PreviousListItem != null)
            {
                TextRangeEdit.SplitElement(firstListItem.ElementStart);
            }

            // Cut wrapping list after endListItem
            if (lastListItem.NextListItem != null)
            {
                TextRangeEdit.SplitElement(lastListItem.ElementEnd);
            }

            // Remove List wrapper from selected items
            List unindentedList = (List)firstListItem.Parent;

            // Check whether we have outer ListItem
            ListItem outerListItem = unindentedList.Parent as ListItem;
            if (outerListItem != null)
            {
                // Selected items belong to a nested list.
                // So we need to pull them to the level of enclosing ListItem, i.e. cut this ListItem.
                // In this case we also need to include trailing list into the last of selected items

                // Remove a wrapping List from selected items
                unindentedList.Reposition(null, null);

                // Remember end position of outerListItem to pull any trailing list or other blocks into the last of selected listitems
                TextPointer outerListItemEnd = outerListItem.ContentEnd;

                if (outerListItem.ContentStart.CompareTo(firstListItem.ElementStart) == 0)
                {
                    // There is nothing before first list item; so outer list item would be empty - just delete it
                    outerListItem.Reposition(null, null);
                }
                else
                {
                    // Wrap all stuff preceding firstListItem in outerListItem
                    outerListItem.Reposition(outerListItem.ContentStart, firstListItem.ElementStart);
                }

                if (outerListItemEnd.CompareTo(lastListItem.ElementEnd) == 0)
                {
                    // There are no following siblings to pull into last selected item; do nothing.
                }
                else
                {
                    // Pull trailing items (following siblings to the selected ones) into the last selected item

                    // Remember a position to merge any trailing list after lastListItem
                    TextPointer mergePosition = lastListItem.ContentEnd;
                    
                    // Reposition last selectd ListItem so that it includes trailing list (or other block) as its children
                    lastListItem.Reposition(lastListItem.ContentStart, outerListItemEnd);

                    // Merge any trailing list with a sublist outdented with our listitem
                    MergeLists(mergePosition);
                }
            }
            else
            {
                // Selected items are not in nested list.
                // We need to simply unwrap them and convert to paragraphs

                TextPointer start = unindentedList.ElementStart;
                TextPointer end = unindentedList.ElementEnd;

                // Save the list's FlowDirection value, to apply later to its children.
                object listFlowDirectionValue = unindentedList.GetValue(Paragraph.FlowDirectionProperty);

                // Remove a wrapping List from selected items
                unindentedList.Reposition(null, null);

                // Remove ListItems from all selected items
                ListItem listItem = firstListItem;
                while (listItem != null)
                {
                    ListItem nextListItem = listItem.ElementEnd.GetAdjacentElement(LogicalDirection.Forward) as ListItem;

                    // If this is an empty <ListItem></ListItem>, insert an explicit paragraph in it before deleting the list item.
                    if (listItem.ContentStart.CompareTo(listItem.ContentEnd) == 0)
                    {
                        TextRangeEditTables.EnsureInsertionPosition(listItem.ContentStart);
                    }

                    listItem.Reposition(null, null);
                    listItem = listItem == lastListItem ? null : nextListItem;
                }

                // Apply FlowDirection of the list just deleted to all its children.
                TextRangeEdit.SetParagraphProperty(start, end, Paragraph.FlowDirectionProperty, listFlowDirectionValue);                

                // Merge lists on boundaries
                MergeLists(start);
                MergeLists(end);
            }

            return true;
        }

        // Predicate which returns true if list items in this range are within the scope of the same parent list.
        private static bool IsRangeWithinSingleList(TextRange range)
        {
            ListItem startListItem = TextPointerBase.GetListItem(range.Start);

            // Adjust range end so that it does not affect a following paragraph.
            TextPointer end = (TextPointer)TextRangeEdit.GetAdjustedRangeEnd(range.Start, range.End);
            ListItem endListItem = TextPointerBase.GetListItem(end);

            // Check if the ListItems belong to one List wrapper.
            if (startListItem != null && endListItem != null && startListItem.Parent == endListItem.Parent)
            {
                return true;
            }

            // In case of nested lists, it may be the case that start and end list item do not belong to one list wrapper,
            // yet no visual list boundary is crossed.
            // e.g.
            //  * aa
            //      * bb
            //      * cc
            // Special case so that list operations are applicable in this scenario.
            if (startListItem != null && endListItem != null)
            {
                while (end.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    if (end.Parent == startListItem.Parent)
                    {
                        return true;
                    }
                    end = end.GetNextContextPosition(LogicalDirection.Forward);
                }
            }

            return false;
        }

        // Checks whether two paragraphs are meargeable.
        // To be meargeable they need to be separated by s sequence of closing, then opening
        // tags only.
        // And all tags must be Sections/Lists/ListItems only.
        internal static bool ParagraphsAreMergeable(Block firstParagraphOrBlockUIContainer, Block secondParagraphOrBlockUIContainer)
        {
            if (firstParagraphOrBlockUIContainer == null || secondParagraphOrBlockUIContainer == null || 
                firstParagraphOrBlockUIContainer == secondParagraphOrBlockUIContainer)
            {
                return false; // nothing to merge
            }

            TextPointer position = firstParagraphOrBlockUIContainer.ElementEnd;
            TextPointer startOfSecondParagraph = secondParagraphOrBlockUIContainer.ElementStart;

            // Skip and check all closing tags (if any)
            while (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                if (!TextSchema.AllowsParagraphMerging(position.Parent.GetType()))
                {
                    return false; // Crossing hard-structured element. Paragraphs are not meargeable.
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            // Skip and check all opening tags (if any)
            while (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
            {
                if (position.CompareTo(startOfSecondParagraph) == 0)
                {
                    // Successfully skipped all tags, and reached the seconfParagraph.
                    // The paragraphs are meargeable.
                    return true;
                }

                position = position.GetNextContextPosition(LogicalDirection.Forward);

                if (!TextSchema.AllowsParagraphMerging(position.Parent.GetType()))
                {
                    return false; // Crossing hardd-structured element. Paragraphs are not meargeable.
                }
            }

            // Non-tag run found. Paragraphs are not meargeable.
            return false;
        }

        // Checks if start and end positions are parented by a List.
        // If so, unindents list items between (start - start's list end) or (end's list start - end)  
        // until they are parented by a top level list.
        // Then, if needed, splits the list(s) at start and/or end positions.
        // Returns false if splitting is not successful due to a failing unindent operation on any nested lists.
        internal static bool SplitListsForFlowDirectionChange(TextPointer start, TextPointer end, object newFlowDirectionValue)
        {
            ListItem startListItem = start.GetListAncestor();

            // Unindent startListItem's list to prepare for a split, if the List's FlowDirection value is different.
            if (startListItem != null && 
                startListItem.List != null && // Check for unparented list items
                !TextSchema.ValuesAreEqual(/*newValue*/newFlowDirectionValue, /*currentValue*/startListItem.List.GetValue(Paragraph.FlowDirectionProperty)))
            {
                while (startListItem != null &&
                    startListItem.List != null &&
                    startListItem.List.Parent is ListItem)
                {
                    // startListItem is within a nested List.
                    if (!UnindentListItems(new TextRange(start, GetPositionAfterList(startListItem.List))))
                    {
                        return false;
                    }
                    startListItem = start.GetListAncestor();
                }
            }
            
            ListItem endListItem = end.GetListAncestor();

            // Unindent endListItem's list to prepare for a split, if the List's FlowDirection value is different.
            if (endListItem != null &&
                endListItem.List != null && 
                !TextSchema.ValuesAreEqual(/*newValue*/newFlowDirectionValue, /*currentValue*/endListItem.List.GetValue(Paragraph.FlowDirectionProperty)))
            {
                if (startListItem != null && startListItem.List != null &&
                    endListItem.List.ElementEnd.CompareTo(startListItem.List.ElementEnd) < 0)
                {
                    // endListItem's List is contained within startListItem's List. 
                    // No need to unindent endListItem.
                }
                else
                {
                    while (endListItem != null &&
                        endListItem.List !=  null && 
                        endListItem.List.Parent is ListItem)
                    {
                        // endListItem is within a nested List.
                        if (!UnindentListItems(new TextRange(endListItem.List.ContentStart, GetPositionAfterList(endListItem.List))))
                        {
                            return false;
                        }
                        endListItem = end.GetListAncestor();
                    }
                }
            }

            // Split list(s) at boundary position(s) if 
            //  1. startListItem is not the first list item within its list (or endListItem is not the last one)
            //  and
            //  2. start/end's parent List's flow direction value is different than the new value being set

            if ((startListItem = start.GetListAncestor()) != null && startListItem.PreviousListItem != null &&
                startListItem.List != null && // Check for unparented list items
                (!TextSchema.ValuesAreEqual(/*newValue*/newFlowDirectionValue, /*currentValue*/startListItem.List.GetValue(Paragraph.FlowDirectionProperty))))
            {
                Invariant.Assert(!(startListItem.List.Parent is ListItem), "startListItem's list must not be nested!");
                TextRangeEdit.SplitElement(startListItem.ElementStart);
            }

            if ((endListItem = end.GetListAncestor()) != null &&
                endListItem.List != null && // Check for unparented list items
                (!TextSchema.ValuesAreEqual(/*newValue*/newFlowDirectionValue, /*currentValue*/endListItem.List.GetValue(Paragraph.FlowDirectionProperty))))
            {
                // Walk up from endListItem to find the topmost listitem that contains it.
                if (endListItem.List.Parent is ListItem)
                {
                    while (endListItem.List != null && endListItem.List.Parent is ListItem)
                    {
                        endListItem = (ListItem)endListItem.List.Parent;
                    }
                }
                if (endListItem.List != null && endListItem.NextListItem != null)
                {
                    Invariant.Assert(!(endListItem.List.Parent is ListItem), "endListItem's list must not be nested!");
                    TextRangeEdit.SplitElement(endListItem.ElementEnd);
                }
            }

            return true;
        }

        // Finds an insertion position after the list
        private static TextPointer GetPositionAfterList(List list)
        {
            Invariant.Assert(list != null, "list cannot be null");

            TextPointer adjustedEnd = list.ElementEnd.GetInsertionPosition(LogicalDirection.Backward);
            if (adjustedEnd != null)
            {
                adjustedEnd = adjustedEnd.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            if (adjustedEnd == null)
            {
                adjustedEnd = list.ElementEnd.TextContainer.End;
            }

            if (TextRangeEditTables.IsTableStructureCrossed(list.ElementEnd, adjustedEnd))
            {
                adjustedEnd = list.ContentEnd;
            }

            return adjustedEnd;
        }

        #endregion Internal Methods
    }
}
