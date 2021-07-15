// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Implements Fixed/Flow structural mapping
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    //--------------------------------------------------------------------
    //
    // Internal Enums
    //
    //---------------------------------------------------------------------
    // Indicate the node type
    internal enum FlowNodeType : byte
    {
        Boundary = 0x0,   // container boundary, cookie = FixedElement
        Start = 0x01,   // this signals start of a scope, cookie = FixedElement
        Run = 0x02,   // this is a run within a scope, cookie = int (run length)
        End = 0x04,   // this signals end of a scope, cookie = FixedElement
        Object = 0x08,   // this is a non-scoped object, cookie = FixedElement
        Virtual = 0x10,   // this node is virtualized, cookie = int (page index)
        Noop = 0x20,   // this is a run within a scope, cookie = int (page index)
    }

    //=====================================================================
    /// <summary>
    /// FixedFlowMap maintains mapping between Flow Order and Fixed Order. 
    /// Both Fixed Order and Flow Order represent an address space the covers
    /// the entire fixed document.  They simply provides different way of 
    /// looking at the same fixed document.  
    /// 
    /// Flow Order allows the document to be viewed as if the document is 
    /// consists of a linear stream of symbols. Some symbols represents element
    /// boundaries while others represent embedded object or individual 
    /// Unicode character.  One can traverse the entire fixed document
    /// by scanning through the entire Flow Order from begin to end. 
    /// 
    /// Flow Order is virtualization aware and support random access. 
    /// 
    /// Fixed Order allows the doument to be viewed as if it is consists of
    /// a linear stream of Glyphs and embedded objects (such as Image and Hyperlink). 
    /// One can traverse the entire fixed document by scanning through the entire 
    /// Fixed Order from begin to end. 
    /// 
    /// Depending on user scenario. some operations require scanning through the 
    /// Flow Order while others require scanning through the Fixed Order. 
    /// 
    /// 
    /// ---------------------------------------------------------------------
    /// A typical Flow Order with Page Boundary and Virtualization):
    /// 
    /// |
    /// |    Page 0      Page 1           Page 2           Page 3      
    /// |--  -------   -------------   -----------------   --------  --
    /// |B   PS V PE   PS S R E R PE   PS R S O O R E PE   PS V PE    B 
    /// |
    /// 
    /// Legend:
    ///     NodeStart       - S
    ///     NodeEnd         - E
    ///     NodeRun         - R
    ///     NodeObject      - O
    ///     NodePageStart   - PS
    ///     NodePageEnd     - PE
    ///     NodeVirtual     - V
    ///     NodeBoundary    - B
    /// 
    /// The above Flow Order indicates that Page 0 and Page 3 are not 
    /// loaded yet (still Virtual);while Page 1 and Page 2 are already 
    /// devirtualized.
    /// 
    /// Basic assumption about mapping: there is 1 : 0-N relationship between FlowNode and FixedSOMElement 
    /// and N : 1 relationship between FixedSOMElement and FixedNode.  FlowNodes and FixedSOMElements have pointers
    /// to eachother, and FixedSOMElements contain their FixedNodes, but the FixedNode->FixedSOMElement(s) mapping
    /// is handled by a Hashtable in FixedFlowMap.  FixedFlowMap also keeps track of the flow order.
    /// </summary>
    internal sealed class FixedFlowMap 
    {
        //--------------------------------------------------------------------
        //
        // Const
        //
        //---------------------------------------------------------------------
        #region Consts
        // special values for fixed order
        internal const int FixedOrderStartPage      = int.MinValue;
        internal const int FixedOrderEndPage        = int.MaxValue;
        internal const int FixedOrderStartVisual    = int.MinValue;
        internal const int FixedOrderEndVisual      = int.MaxValue;

        // special vaules for flow order
        internal const int FlowOrderBoundaryScopeId = int.MinValue;
        internal const int FlowOrderVirtualScopeId  = -1;
        internal const int FlowOrderScopeIdStart    = 0;
        #endregion Consts


        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Ctor
        /// </summary>
        internal FixedFlowMap()
        {
            _Init();
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        // Indexer to quickly get to flow position via flow fp
        internal FlowNode this[int fp]
        {
            get
            {
                Debug.Assert(fp >= 0 && fp < _flowOrder.Count);
                return _flowOrder[fp];
            }
        }
        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        //-------------------------------------------------------
        // Mapping
        //-------------------------------------------------------
        
        // Replace an existing FlowNode with a new set of FlowNode
        // Each has a mapped FixedNode set. 
        internal void MappingReplace(FlowNode flowOld, List<FlowNode> flowNew)
        {
            Debug.Assert(flowOld.Type == FlowNodeType.Virtual || flowNew != null);

            // Insert a new entry into Flow Order for each new FlowNode
            int index = flowOld.Fp;
            _flowOrder.RemoveAt(index);
            _flowOrder.InsertRange(index, flowNew);
            for (int i = index; i < _flowOrder.Count; i++)
            {
                _flowOrder[i].SetFp(i);
            }
        }

        internal FixedSOMElement MappingGetFixedSOMElement(FixedNode fixedp, int offset)
        {
            List<FixedSOMElement> entry = _GetEntry(fixedp);

            if (entry != null)
            {
                foreach (FixedSOMElement element in entry)
                {
                    if (offset >= element.StartIndex && offset <= element.EndIndex)
                    {
                        return element;
                    }
                }
            }
            return null;
        }


        //-------------------------------------------------------
        // Fixed Order
        //-------------------------------------------------------

#if DEBUG
        // This is only used in our debug fixed node rendering code
        // Get a range of fixednode in fixed order between two fixed nodes, inclusive
        internal FixedNode[] FixedOrderGetRangeNodes(FixedNode start, FixedNode end)
        {
            //Debug.Assert(start <= end);
            if (start == end)
            {
                return new FixedNode[1] { start };
            }

            ArrayList range = new ArrayList();

            // will this function be passed boundary nodes??
            FlowNode flowNode = ((List<FixedSOMElement>) _GetEntry(start))[0].FlowNode;

            bool foundEnd = false;

            while (!foundEnd)
            {
				if (flowNode.FixedSOMElements != null)
				{
					foreach (FixedSOMElement element in flowNode.FixedSOMElements)
					{
						if (range.Count == 0)
						{
							if (element.FixedNode == start)
							{
								range.Add(start);
							}
						}
						else
						{
							if (!element.FixedNode.Equals(range[range.Count - 1]))
							{
								range.Add(element.FixedNode);
							}
						}

						if (element.FixedNode == end)
						{
							foundEnd = true;
							break;
						}
					}
				}
                flowNode = _flowOrder[flowNode.Fp + 1];
            }
            
            return (FixedNode[])range.ToArray(typeof(FixedNode));
        }
#endif

        //-------------------------------------------------------
        // Flow Order
        //-------------------------------------------------------

        // Insert a FlowNode before a given FlowNode
        // NOTE: FlowNode's Fp will be modified
        internal FlowNode FlowOrderInsertBefore(FlowNode nextFlow, FlowNode newFlow)
        {
            _FlowOrderInsertBefore(nextFlow, newFlow);
            return newFlow;
        }

        internal void AddFixedElement(FixedSOMElement element)
        {
            _AddEntry(element);
        }
        #endregion Internal Methods


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        // Get the FixedNode start object
        internal FixedNode FixedStartEdge
        {
            get
            {
                return s_FixedStart;
            }
        }


        // Get the FlowNode start object
        internal FlowNode FlowStartEdge
        {
            get
            {
                return _flowStart;
            }
        }


        // Get the FlowNode end object
        internal FlowNode FlowEndEdge
        {
            get
            {
                return _flowEnd;
            }
        }

        // Get count of flow positions
        internal int FlowCount
        {
            get
            {
                return _flowOrder.Count;
            }
        }

#if DEBUG        
        internal List<FlowNode> FlowNodes
        {
            get
            {
                return _flowOrder;
            }
        }
#endif
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        // initialize the maps and edge nodes
        private void _Init()
        {
            // mutable boundary flow nodes
            _flowStart  = new FlowNode(FlowOrderBoundaryScopeId, FlowNodeType.Boundary, null);
            _flowEnd    = new FlowNode(FlowOrderBoundaryScopeId, FlowNodeType.Boundary, null);

            //_fixedOrder   = new List<FixedOrderEntry>();
            _flowOrder    = new List<FlowNode>();

            _flowOrder.Add(_flowStart);
            _flowStart.SetFp(0);
            _flowOrder.Add(_flowEnd);
            _flowEnd.SetFp(1);

            _mapping = new Hashtable();
        }

         //-------------------------------------------------------
        // Flow Order Helper Functions
        //-------------------------------------------------------

        // Insert a flow position before a given flow position
        // NOTE: flow fp will be modified
        internal void _FlowOrderInsertBefore(FlowNode nextFlow, FlowNode newFlow)
        {
            newFlow.SetFp(nextFlow.Fp);
            _flowOrder.Insert(newFlow.Fp, newFlow);

            // update all subsequent fps
            for (int i = newFlow.Fp + 1, n = _flowOrder.Count; i < n; i++)
            {
                _flowOrder[i].IncreaseFp();
            }
        }

        private List<FixedSOMElement> _GetEntry(FixedNode node)
        {
            if (_cachedEntry == null || node != _cachedFixedNode)
            {
                _cachedEntry = (List<FixedSOMElement>)_mapping[node]; 
                _cachedFixedNode = node;
            }
            return _cachedEntry;
        }

        private void _AddEntry(FixedSOMElement element)
        {
            FixedNode fn = element.FixedNode;
            List<FixedSOMElement> entry;
            if (_mapping.ContainsKey(fn))
            {
                entry = (List<FixedSOMElement>)_mapping[fn];
            }
            else
            {
                entry = new List<FixedSOMElement>();
                _mapping.Add(fn, entry);
            }

            entry.Add(element);
        }
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private List<FlowNode> _flowOrder;    // Flow Order represents a linear stream of symbols view on the fixed document
        private FlowNode  _flowStart;           // Start FlowNode for the flow document. It is mutable type even though it never flows. 
        private FlowNode  _flowEnd;             // End FlowNode for the flow document.  It flows as new FlowNode gets inserted

        // immutable fixed nodes
        private readonly static FixedNode s_FixedStart = FixedNode.Create(FixedOrderStartPage, 1, FixedOrderStartVisual, -1, null);
        private readonly static FixedNode s_FixedEnd   = FixedNode.Create(FixedOrderEndPage, 1, FixedOrderEndVisual, -1,  null);
        private Hashtable _mapping;
        private FixedNode _cachedFixedNode;
        private List<FixedSOMElement> _cachedEntry;
        #endregion Private Fields
    }
}

