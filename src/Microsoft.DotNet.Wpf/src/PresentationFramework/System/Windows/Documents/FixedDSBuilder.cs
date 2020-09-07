// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedTextBuilder contains heuristics to map fixed document elements
//      into stream of flow text
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System.Windows.Controls;     
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Markup;
    using System.Windows.Shapes;
    using System.Windows.Documents.DocumentStructures;
    using ds=System.Windows.Documents.DocumentStructures;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    //=====================================================================
    /// <summary>
    /// FixedTextBuilder contains heuristics to map fixed document elements
    /// into stream of flow text.
    /// </summary>
    internal sealed class FixedDSBuilder
    {
        class NameHashFixedNode
        {
            internal NameHashFixedNode(UIElement e, int i)
            {
                uiElement = e; index = i;
            }
            internal UIElement uiElement;
            internal int index;
        }


        public FixedDSBuilder(FixedPage fp, StoryFragments sf)
        {
            _nameHashTable = new Dictionary<String, NameHashFixedNode>();
            _fixedPage = fp;
            _storyFragments = sf;
        }

        public void BuildNameHashTable(String Name, UIElement e, int indexToFixedNodes)
        {
            if (!_nameHashTable.ContainsKey(Name))
            {
                _nameHashTable.Add(Name,
                    new NameHashFixedNode(e,indexToFixedNodes));
            }
        }

        public StoryFragments StoryFragments
        {
            get
            {
                return _storyFragments;
            }
        }

        public void ConstructFlowNodes(FixedTextBuilder.FlowModelBuilder flowBuilder, List<FixedNode> fixedNodes)
        {
            //
            // Initialize some variables inside this class.
            //
            _fixedNodes = fixedNodes;
            _visitedArray = new BitArray(fixedNodes.Count);
            _flowBuilder = flowBuilder;

            List<StoryFragment> StoryFragmentList = StoryFragments.StoryFragmentList;
            foreach (StoryFragment storyFragme in StoryFragmentList)
            {
                List<BlockElement> blockElementList = storyFragme.BlockElementList;
                foreach (BlockElement be in blockElementList)
                {
                    _CreateFlowNodes(be);
                }
            }
            //
            // After all the document structure referenced elements, we will go over
            // the FixedNodes again to take out all the un-referenced elements and put 
            // them in the end of the story. 
            //
            // Add the start node in the flow array.
            //
            _flowBuilder.AddStartNode(FixedElement.ElementType.Paragraph);

            for (int i = 0; i< _visitedArray.Count; i++ )
            {
                if (_visitedArray[i] == false)
                {
                    AddFixedNodeInFlow(i, null);
                }
            }

            _flowBuilder.AddEndNode();

            //Add any undiscovered hyperlinks at the end of the page
            _flowBuilder.AddLeftoverHyperlinks();
        }

        private void AddFixedNodeInFlow(int index, UIElement e)
        {
            if (_visitedArray[index])
            {
                // this has already been added to the document structure
                // Debug.Assert(false, "An element is referenced in the document structure multiple times");
                return; // ignore this reference
            }
            FixedNode fn = (FixedNode)_fixedNodes[index];

            if (e == null)
            {
                e = _fixedPage.GetElement(fn) as UIElement;
            }

            _visitedArray[index] = true;

            FixedSOMElement somElement = FixedSOMElement.CreateFixedSOMElement(_fixedPage, e, fn, -1, -1);
            if (somElement != null)
            {
                _flowBuilder.AddElement(somElement);
            }
        }

        /// <summary>
        /// This function will create the flow node corresponding to this BlockElement.
        /// This function will call itself recursively.
        /// </summary>
        /// <param name="be"></param>
        private void _CreateFlowNodes(BlockElement be)
        {
            //
            // Break, NamedElement and SemanticBasicElement all derived from BlockElement.
            // Break element is ignored for now.
            //
            NamedElement ne = be as NamedElement;
            if (ne != null)
            {
                //
                // That is the NamedElement, it might use namedReference or HierachyReference, 
                // we need to construct FixedSOMElement list from this named element.
                //
                ConstructSomElement(ne);
            }
            else
            {
                SemanticBasicElement sbe = be as SemanticBasicElement;
                if (sbe != null)
                {
                    //
                    // Add the start node in the flow array.
                    //
                    _flowBuilder.AddStartNode(be.ElementType);

                    //Set the culture info on this node
                    XmlLanguage language = (XmlLanguage) _fixedPage.GetValue(FrameworkElement.LanguageProperty);
                    _flowBuilder.FixedElement.SetValue(FixedElement.LanguageProperty, language);

                    SpecialProcessing(sbe);
                    //
                    // Enumerate all the childeren under this element.
                    //
                    List<BlockElement> blockElementList = sbe.BlockElementList;
                    foreach (BlockElement bElement in blockElementList)
                    {
                        _CreateFlowNodes(bElement);
                    }

                    //
                    // Add the end node in the flow array.
                    //
                    _flowBuilder.AddEndNode();
                }
            }
        }

        private void AddChildofFixedNodeinFlow(int[] childIndex, NamedElement ne)
        {
            // Create a fake FixedNode to help binary search.
            FixedNode fn = FixedNode.Create(((FixedNode)_fixedNodes[0]).Page, childIndex);
            // Launch the binary search to find the matching FixedNode 
            int index = _fixedNodes.BinarySearch(fn);

            if (index >= 0)
            {
                int startIndex;
                // Search backward for the first Node in this scope
                for (startIndex = index - 1; startIndex >= 0; startIndex--)
                {
                    fn = (FixedNode)_fixedNodes[startIndex];
                    if (fn.ComparetoIndex(childIndex) != 0)
                    {
                        break;
                    }
                }

                // Search forward to add all the nodes in order.
                for (int i = startIndex+1; i < _fixedNodes.Count; i++)
                {
                    fn = (FixedNode)_fixedNodes[i];
                    if (fn.ComparetoIndex(childIndex) == 0)
                    {
                        AddFixedNodeInFlow(i, null);
                    }
                    else break;
                }
            }
        }

        private void SpecialProcessing(SemanticBasicElement sbe)
        {
            ds.ListItemStructure listItem = sbe as ds.ListItemStructure;
            if (listItem != null && listItem.Marker != null)
            {
                NameHashFixedNode fen;
                if (_nameHashTable.TryGetValue(listItem.Marker, out fen) == true)
                {
                    _visitedArray[fen.index] = true;
                }
            }
        }

        private void ConstructSomElement(NamedElement ne)
        {
            NameHashFixedNode fen;
            if (_nameHashTable.TryGetValue(ne.NameReference, out fen) == true)
            {
                if (fen.uiElement is Glyphs || fen.uiElement is Path ||
                    fen.uiElement is Image)
                {
                    // Elements that can't have childrent
                    AddFixedNodeInFlow(fen.index, fen.uiElement);
                }
                else
                {
                    if (fen.uiElement is Canvas)
                    {
                        // We need to find all the fixed nodes inside the scope of 
                        // this grouping element, add all of them in the arraylist.
                        int[] childIndex = _fixedPage._CreateChildIndex(fen.uiElement);

                        AddChildofFixedNodeinFlow(childIndex, ne);
                    }
                }
            }
        }

        private StoryFragments _storyFragments;
        private FixedPage _fixedPage;  
        private List<FixedNode> _fixedNodes;
        private BitArray _visitedArray;
        private Dictionary<String, NameHashFixedNode> _nameHashTable;
        private FixedTextBuilder.FlowModelBuilder _flowBuilder;
   }
}

