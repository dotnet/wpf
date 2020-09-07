// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper services for TextContainer.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation.Peers;            // AutomationPeer
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MS.Internal.Text;

namespace MS.Internal.Documents
{
    internal sealed class TextContentRange
    {
        internal TextContentRange()
        {
        }
        internal TextContentRange(int cpFirst, int cpLast, ITextContainer textContainer)
        {
            Invariant.Assert(cpFirst <= cpLast);
            Invariant.Assert(cpFirst >= 0);
            Invariant.Assert(textContainer != null);
            Invariant.Assert(cpLast <= textContainer.SymbolCount);
            _cpFirst = cpFirst;
            _cpLast = cpLast;
            _size = 0;
            _ranges = null;
            _textContainer = textContainer;
        }
        internal void Merge(TextContentRange other)
        {
            Invariant.Assert(other != null);

            // Skip merge operation if we're merging an empty text content range.
            if (other._textContainer == null)
            {
                return;
            }

            if (_textContainer == null)
            {
                _cpFirst = other._cpFirst;
                _cpLast = other._cpLast;
                _textContainer = other._textContainer;
                _size = other._size;
                if (_size != 0)
                {
                    Invariant.Assert(other._ranges != null);
                    Invariant.Assert(other._ranges.Length >= (other._size * 2));
                    _ranges = new int[_size * 2];
                    for (int i = 0; i < _ranges.Length; i++)
                    {
                        _ranges[i] = other._ranges[i];
                    }
                }
            }
            else
            {
                Invariant.Assert(_textContainer == other._textContainer);
                if (other.IsSimple)
                {
                    Merge(other._cpFirst, other._cpLast);
                }
                else
                {
                    for (int i = 0; i < other._size; i++)
                    {
                        Merge(other._ranges[i * 2], other._ranges[i * 2 + 1]);
                    }
                }
            }
            Normalize();
        }
        internal ReadOnlyCollection<TextSegment> GetTextSegments()
        {
            List<TextSegment> segments;
            if (_textContainer == null)
            {
                segments = new List<TextSegment>();
            }
            else
            {
                if (IsSimple)
                {
                    segments = new List<TextSegment>(1);
                    segments.Add(new TextSegment(
                        _textContainer.CreatePointerAtOffset(_cpFirst, LogicalDirection.Forward),
                        _textContainer.CreatePointerAtOffset(_cpLast, LogicalDirection.Backward),
                        true));
                }
                else
                {
                    segments = new List<TextSegment>(_size);
                    for (int i = 0; i < _size; i++)
                    {
                        segments.Add(new TextSegment(
                            _textContainer.CreatePointerAtOffset(_ranges[i * 2], LogicalDirection.Forward),
                            _textContainer.CreatePointerAtOffset(_ranges[i * 2 + 1], LogicalDirection.Backward),
                            true));
                    }
                }
            }
            return new ReadOnlyCollection<TextSegment>(segments);
        }
        internal bool Contains(ITextPointer position, bool strict)
        {
            bool contains = false;
            int cpPos = position.Offset;
            if (IsSimple)
            {
                if (cpPos >= _cpFirst && cpPos <= _cpLast)
                {
                    contains = true;
                    if (strict && (_cpFirst != _cpLast))
                    {
                        if (cpPos == _cpFirst && position.LogicalDirection == LogicalDirection.Backward ||
                            cpPos == _cpLast && position.LogicalDirection == LogicalDirection.Forward)
                        {
                            contains = false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < _size; i++)
                {
                    if (cpPos >= _ranges[i * 2] && cpPos <= _ranges[i * 2 + 1])
                    {
                        contains = true;
                        if (strict)
                        {
                            if (cpPos == _ranges[i * 2] && position.LogicalDirection == LogicalDirection.Backward ||
                                cpPos == _ranges[i * 2 + 1] && position.LogicalDirection == LogicalDirection.Forward)
                            {
                                contains = false;
                            }
                        }
                        break;
                    }
                }
            }
            return contains;
        }
        internal ITextPointer StartPosition
        {
            get
            {
                ITextPointer startPosition = null;
                if (_textContainer != null)
                {
                    startPosition = _textContainer.CreatePointerAtOffset(IsSimple ? _cpFirst : _ranges[0], LogicalDirection.Forward);
                }
                return startPosition;
            }
        }
        internal ITextPointer EndPosition
        {
            get
            {
                ITextPointer endPosition = null;
                if (_textContainer != null)
                {
                    endPosition = _textContainer.CreatePointerAtOffset(IsSimple ? _cpLast : _ranges[(_size - 1) * 2 + 1], LogicalDirection.Backward);
                }
                return endPosition;
            }
        }
        private void Merge(int cpFirst, int cpLast)
        {
            if (IsSimple)
            {
                if (cpFirst > _cpLast || cpLast < _cpFirst)
                {
                    _size = 2;
                    _ranges = new int[8]; // 4 entries
                    if (cpFirst > _cpLast)
                    {
                        _ranges[0] = _cpFirst;
                        _ranges[1] = _cpLast;
                        _ranges[2] = cpFirst;
                        _ranges[3] = cpLast;
                    }
                    else
                    {
                        _ranges[0] = cpFirst;
                        _ranges[1] = cpLast;
                        _ranges[2] = _cpFirst;
                        _ranges[3] = _cpLast;
                    }
                }
                else
                {
                    _cpFirst = Math.Min(_cpFirst, cpFirst);
                    _cpLast = Math.Max(_cpLast, cpLast);
                }
            }
            else
            {
                int i = 0;
                while (i < _size)
                {
                    if (cpLast < _ranges[i * 2])
                    {
                        // Insert before the current position
                        EnsureSize();
                        for (int j = _size * 2 - 1; j >= i * 2; j--)
                        {
                            _ranges[j + 2] = _ranges[j];
                        }
                        _ranges[i * 2] = cpFirst;
                        _ranges[i * 2 + 1] = cpLast;
                        ++_size;
                        break;
                    }
                    else if (cpFirst <= _ranges[i * 2 + 1])
                    {
                        // Merge with the current position
                        _ranges[i * 2] = Math.Min(_ranges[i * 2], cpFirst);
                        _ranges[i * 2 + 1] = Math.Max(_ranges[i * 2 + 1], cpLast);
                        while (MergeWithNext(i)) { }
                        break;
                    }
                    ++i;
                }
                if (i >= _size)
                {
                    // Insert at the last position
                    EnsureSize();
                    _ranges[_size * 2] = cpFirst;
                    _ranges[_size * 2 + 1] = cpLast;
                    ++_size;
                }
            }
        }
        private bool MergeWithNext(int pos)
        {
            if (pos < _size - 1)
            {
                if (_ranges[pos * 2 + 1] >= _ranges[(pos + 1) * 2])
                {
                    _ranges[pos * 2 + 1] = Math.Max(_ranges[pos * 2 + 1], _ranges[(pos + 1) * 2 + 1]);
                    for (int i = (pos + 1) * 2; i < (_size - 1) * 2; i++)
                    {
                        _ranges[i] = _ranges[i + 2];
                    }
                    --_size;
                    return true;
                }
            }
            return false;
        }
        private void EnsureSize()
        {
            Invariant.Assert(_size > 0);
            Invariant.Assert(_ranges != null);
            if (_ranges.Length < (_size + 1) * 2)
            {
                int[] ranges = new int[_ranges.Length * 2];
                for (int i = 0; i < _size * 2; i++)
                {
                    ranges[i] = _ranges[i];
                }
                _ranges = ranges;
            }
        }
        private void Normalize()
        {
            if (_size == 1)
            {
                _cpFirst = _ranges[0];
                _cpLast = _ranges[1];
                _size = 0;
                _ranges = null;
            }
        }
        private bool IsSimple { get { return (_size == 0); } }
        private int _cpFirst;
        private int _cpLast;
        private int _size;
        private int[] _ranges;
        private ITextContainer _textContainer;
    }

    /// <summary>
    /// Helper services for TextContainer.
    /// </summary>
    internal static class TextContainerHelper
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves a collection of AutomationPeers that fall within the range.
        /// Children that overlap with the range but are not entirely enclosed by 
        /// it will also be included in the collection.
        /// </summary>
        internal static List<AutomationPeer> GetAutomationPeersFromRange(ITextPointer start, ITextPointer end, ITextPointer ownerContentStart)
        {
            bool positionMoved;
            AutomationPeer peer = null;
            object element;
            List<AutomationPeer> peers = new List<AutomationPeer>();
            start = start.CreatePointer();

            while (start.CompareTo(end) < 0)
            {
                // Indicate that 'start' position is not moved yet.
                positionMoved = false;

                if (start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
                {
                    // Get adjacent element and try to retrive AutomationPeer for it.
                    element = start.GetAdjacentElement(LogicalDirection.Forward);
                    if (element is ContentElement)
                    {
                        peer = ContentElementAutomationPeer.CreatePeerForElement((ContentElement)element);
                        // If AutomationPeer has been retrieved, add it to the collection.
                        // And skip entire element.
                        if (peer != null)
                        {
                            if (ownerContentStart == null || IsImmediateAutomationChild(start, ownerContentStart))
                            {
                                peers.Add(peer);
                            }
                            start.MoveToNextContextPosition(LogicalDirection.Forward);
                            start.MoveToElementEdge(ElementEdge.AfterEnd);
                            positionMoved = true;
                        }
                    }
                }
                else if (start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.EmbeddedElement)
                {
                    // Get adjacent element and try to retrive AutomationPeer for it.
                    element = start.GetAdjacentElement(LogicalDirection.Forward);
                    if (element is UIElement)
                    {
                        if (ownerContentStart == null || IsImmediateAutomationChild(start, ownerContentStart))
                        {
                            peer = UIElementAutomationPeer.CreatePeerForElement((UIElement)element);
                            // If AutomationPeer has been retrieved, add it to the collection.
                            if (peer != null)
                            {
                                peers.Add(peer);
                            }
                            else
                            {
                                iterate((Visual)element, peers);
                            }
                        }
                    }
                    else if (element is ContentElement)
                    {
                        peer = ContentElementAutomationPeer.CreatePeerForElement((ContentElement)element);
                        // If AutomationPeer has been retrieved, add it to the collection.
                        if (peer != null)
                        {
                            if (ownerContentStart == null || IsImmediateAutomationChild(start, ownerContentStart))
                            {
                                peers.Add(peer);
                            }
                        }
                    }
                }
                // Move to the next content position, if position has not been moved already.
                if (!positionMoved)
                {
                    if (!start.MoveToNextContextPosition(LogicalDirection.Forward))
                    {
                        break;
                    }
                }
            }

            return peers;
        }

        /// <summary>
        /// Is AutometionPeer represented by 'elementStart' is immediate child of AutomationPeer 
        /// represented by 'ownerContentStart'.
        /// </summary>
        internal static bool IsImmediateAutomationChild(ITextPointer elementStart, ITextPointer ownerContentStart)
        {
            Invariant.Assert(elementStart.CompareTo(ownerContentStart) >= 0);
            bool immediateChild = true;
            // Walk element tree up looking for AutomationPeers.
            ITextPointer position = elementStart.CreatePointer();
            while (typeof(TextElement).IsAssignableFrom(position.ParentType))
            {
                position.MoveToElementEdge(ElementEdge.BeforeStart);
                if (position.CompareTo(ownerContentStart) <= 0)
                {
                    break;
                }
                AutomationPeer peer = null;
                object element = position.GetAdjacentElement(LogicalDirection.Forward);
                if (element is UIElement)
                {
                    peer = UIElementAutomationPeer.CreatePeerForElement((UIElement)element);
                }
                else if (element is ContentElement)
                {
                    peer = ContentElementAutomationPeer.CreatePeerForElement((ContentElement)element);
                }
                if (peer != null)
                {
                    immediateChild = false;
                    break;
                }
            }
            return immediateChild;
        }

        /// <summary>
        /// Returns the common ancestor of two positions. This ancestor needs to have
        /// AutomationPeer associated with it.
        /// </summary>
        internal static AutomationPeer GetEnclosingAutomationPeer(ITextPointer start, ITextPointer end, out ITextPointer elementStart, out ITextPointer elementEnd)
        {
            object element;
            AutomationPeer peer;
            ITextPointer position;
            List<object> ancestorsStart, ancestorsEnd;
            List<ITextPointer> positionsStart, positionsEnd;

            // Get instances of parent objects for start position. The only valid type for
            // scoping elements inside ITextContainer is TextElement.
            ancestorsStart = new List<object>();
            positionsStart = new List<ITextPointer>();
            position = start.CreatePointer();
            while (typeof(TextElement).IsAssignableFrom(position.ParentType))
            {
                position.MoveToElementEdge(ElementEdge.BeforeStart);
                element = position.GetAdjacentElement(LogicalDirection.Forward);
                if (element != null)
                {
                    ancestorsStart.Insert(0, element);
                    positionsStart.Insert(0, position.CreatePointer(LogicalDirection.Forward));
                }
            }

            // Get instances of parent objects for end position. The only valid type for
            // scoping elements inside ITextContainer is TextElement.
            ancestorsEnd = new List<object>();
            positionsEnd = new List<ITextPointer>();
            position = end.CreatePointer();
            while (typeof(TextElement).IsAssignableFrom(position.ParentType))
            {
                position.MoveToElementEdge(ElementEdge.AfterEnd);
                element = position.GetAdjacentElement(LogicalDirection.Backward);
                if (element != null)
                {
                    ancestorsEnd.Insert(0, element);
                    positionsEnd.Insert(0, position.CreatePointer(LogicalDirection.Backward));
                }
            }

            // Find common ancestor. If any of ancestors collection is empty, there
            // is no common ancestor.
            peer = null;
            elementStart = elementEnd = null;
            int depth = Math.Min(ancestorsStart.Count, ancestorsEnd.Count);
            while (depth > 0)
            {
                if (ancestorsStart[depth - 1] == ancestorsEnd[depth - 1])
                {
                    element = ancestorsStart[depth - 1];
                    if (element is UIElement)
                    {
                        peer = UIElementAutomationPeer.CreatePeerForElement((UIElement)element);
                    }
                    else if (element is ContentElement)
                    {
                        peer = ContentElementAutomationPeer.CreatePeerForElement((ContentElement)element);
                    }
                    if (peer != null)
                    {
                        elementStart = positionsStart[depth - 1];
                        elementEnd = positionsEnd[depth - 1];
                        break;
                    }
                }
                depth--;
            }

            return peer;
        }

        /// <summary>
        /// Gets TextContentRange for a TextElement (including element's edges).
        /// </summary>
        internal static TextContentRange GetTextContentRangeForTextElement(TextElement textElement)
        {
            ITextContainer textContainer = textElement.TextContainer;
            int cpFirst = textElement.ElementStartOffset;
            int cpLast = textElement.ElementEndOffset;
            return new TextContentRange(cpFirst, cpLast, textContainer);
        }

        /// <summary>
        /// Gets TextContentRange for a TextElement's edge.
        /// </summary>
        internal static TextContentRange GetTextContentRangeForTextElementEdge(TextElement textElement, ElementEdge edge)
        {
            Invariant.Assert(edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterEnd);

            ITextContainer textContainer = textElement.TextContainer;
            int cpFirst, cpLast;
            if (edge == ElementEdge.AfterEnd)
            {
                cpFirst = textElement.ContentEndOffset;
                cpLast = textElement.ElementEndOffset;
            }
            else
            {
                cpFirst = textElement.ElementStartOffset;
                cpLast = textElement.ContentStartOffset;
            }
            return new TextContentRange(cpFirst, cpLast, textContainer);
        }

        /// <summary>
        /// Retrieves ITextPointer representing content start of given element.
        /// </summary>
        internal static ITextPointer GetContentStart(ITextContainer textContainer, DependencyObject element)
        {
            ITextPointer textPointer;
            // If the element is a TextElement, return the beginning of its content.
            // Otherwise assume that element is the host of text content and return
            // the beginning of TextContainer.
            if (element is TextElement)
            {
                textPointer = ((TextElement)element).ContentStart;
            }
            else
            {
                Invariant.Assert(element is TextBlock || element is FlowDocument || element is TextBox,
                    "Cannot retrive ContentStart position for EmbeddedObject.");
                textPointer = textContainer.CreatePointerAtOffset(0, LogicalDirection.Forward); // Start
            }
            return textPointer;
        }

        /// <summary>
        /// Retrieves the length (in CPs) of given element including edges.
        /// </summary>
        internal static int GetElementLength(ITextContainer textContainer, DependencyObject element)
        {
            int length;
            // For TextElement return its length including edges.
            // Otherwise assume that this is host of TextContainer and return the
            // length of TextContainer.
            if (element is TextElement)
            {
                length = ((TextElement)element).SymbolCount;
            }
            else
            {
                Invariant.Assert(element is TextBlock || element is FlowDocument || element is TextBox,
                    "Cannot retrive length for EmbeddedObject.");
                length = textContainer.SymbolCount;
            }
            return length;
        }

        /// <summary>
        /// Length (in CPs) of embedded object.
        /// </summary>
        internal static int EmbeddedObjectLength { get { return 1; } }

        /// <summary>
        /// Gets dynamic TextPointer form character position.
        /// </summary>
        internal static ITextPointer GetTextPointerFromCP(ITextContainer textContainer, int cp, LogicalDirection direction)
        {
            return textContainer.CreatePointerAtOffset(cp, direction);
        }

        /// <summary>
        /// Gets static TextPointer form character position.
        /// </summary>
        internal static StaticTextPointer GetStaticTextPointerFromCP(ITextContainer textContainer, int cp)
        {
            return textContainer.CreateStaticPointerAtOffset(cp);
        }

        /// <summary>
        /// Retrieves TextPointer representing embedded object.
        /// </summary>
        internal static ITextPointer GetTextPointerForEmbeddedObject(FrameworkElement embeddedObject)
        {
            // Likely the embedded element is hosted by some TextElement, like InlineUIContainer or BlockUIContainer
            ITextPointer position;
            TextElement uiContainer = embeddedObject.Parent as TextElement;
            if (uiContainer != null)
            {
                position = uiContainer.ContentStart;
            }
            else
            {
                Invariant.Assert(false, "Embedded object needs to have InlineUIContainer or BlockUIContainer as parent.");
                position = null;
            }
            return position;
        }

        /// <summary>
        /// Gets CP (character position) representing DependencyObject within given TextContainer.
        /// If object does not belong to the TextContainer, this method returns
        /// distance to the end of TextContainer.
        /// Only TextElement or host of TextContainer can be passed here.
        /// </summary>
        internal static int GetCPFromElement(ITextContainer textContainer, DependencyObject element, ElementEdge edge)
        {
            int cp;
            TextElement textElement;

            textElement = element as TextElement;
            if (textElement != null)
            {
                if (!textElement.IsInTree || textElement.TextContainer != textContainer)
                {
                    // Element is not in the tree. Use TextContainer.End instead.
                    // This situation may happen, if element got removed, but StructuralCache
                    // still have a reference to it and it is trying to do incremental update.
                    cp = textContainer.SymbolCount;
                }
                else
                {
                    // Get TextPointer from appropriate edge.
                    switch (edge)
                    {
                        case ElementEdge.BeforeStart:
                            cp = textElement.ElementStartOffset;
                            break;
                        case ElementEdge.AfterStart:
                            cp = textElement.ContentStartOffset;
                            break;
                        case ElementEdge.BeforeEnd:
                            cp = textElement.ContentEndOffset;
                            break;
                        case ElementEdge.AfterEnd:
                            cp = textElement.ElementEndOffset;
                            break;
                        default:
                            Invariant.Assert(false, "Unknown ElementEdge.");
                            cp = 0;
                            break;
                    }
                }
            }
            else
            {
                // The element is the owner of TextContainer,
                // return the beginning or the end of the content.
                Invariant.Assert(element is TextBlock || element is FlowDocument || element is TextBox,
                    "Cannot retrive length for EmbeddedObject.");
                cp = (edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterStart) ? 0 : textContainer.SymbolCount;
            }
            return cp;
        }

        /// <summary>
        /// Returns the number of TextContainer symbols covered by a DependencyObject.
        /// This includes the element edges if element is a TextElement.
        /// </summary>
        internal static int GetCchFromElement(ITextContainer textContainer, DependencyObject element)
        {
            int cch;
            TextElement textElement;

            textElement = element as TextElement;
            if (textElement != null)
            {
                cch = textElement.SymbolCount;
            }
            else
            {
                cch = textContainer.SymbolCount;
            }

            return cch;
        }

        /// <summary>
        /// Gets CP (character position) representing EmbeddedObject within TextContainer.
        /// If object does not belong to the TextContainer, this method returns -1.
        /// </summary>
        internal static int GetCPFromEmbeddedObject(UIElement embeddedObject, ElementEdge edge)
        {
            Invariant.Assert(edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterEnd, "Cannot retrieve CP from the content of embedded object.");
            int cp = -1;
            if (embeddedObject is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)embeddedObject;
                //likely the embedded element is hosted by some TextElement, like InlineUIContainer or BlockUIContainer
                if (fe.Parent is TextElement)
                {
                    TextElement uiContainer = (TextElement)fe.Parent;
                    cp = (edge == ElementEdge.BeforeStart) ? uiContainer.ContentStartOffset : uiContainer.ContentEndOffset;
                }
            }
            return cp;
        }

        /// <summary>
        /// Element edge character length
        /// </summary>
        internal static int ElementEdgeCharacterLength = 1;

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        private static void iterate(Visual parent, List<AutomationPeer> peers)
        {
            AutomationPeer peer = null;
            int count = parent.InternalVisualChildrenCount;
            for (int i = 0; i < count; i++)
            {
                Visual child = parent.InternalGetVisualChild(i);
                if (child != null
                    && child.CheckFlagsAnd(VisualFlags.IsUIElement)
                    && (peer = UIElementAutomationPeer.CreatePeerForElement((UIElement)child)) != null)
                {
                    peers.Add(peer);
                }
                else
                {
                    iterate(child, peers);
                }
            }
        }

        #endregion Private Methods
    }
}
