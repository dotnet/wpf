// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FlowPosition represents a navigational position in a document's content flow. 
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Windows.Controls;

    

    //=====================================================================
    /// <summary>
    /// FlowPosition represents a navigational position in a document's content flow. 
    /// </summary>
    /// <remarks>
    /// A FlowPosition is represented by a FlowNode in the backing store and offset within 
    /// the flow node (starts with 0, on the left side of the symbol). e.g.
    ///         &lt;P&gt;     H   A   L     &lt;/P&gt;      
    ///        0          1 0   1   2   3  0          1
    /// </remarks>
    internal sealed class FlowPosition : IComparable
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FlowPosition(FixedTextContainer container, FlowNode node, int offset)
        {
            Debug.Assert(!FlowNode.IsNull(node));
            _container  = container;
            _flowNode   = node;
            _offset     = offset;
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
          
        #region Public Methods
        /// <summary>
        /// Create a shallow copy of this objet
        /// </summary>
        /// <returns>A clone of this FlowPosition</returns>
        public object Clone()
        {
            return new FlowPosition(_container, _flowNode, _offset);
        }


        // Compare two FixedTextPointer based on their flow order and offset
        public int CompareTo(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            FlowPosition flow = o as FlowPosition;
            if (flow == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, o.GetType(), typeof(FlowPosition)), "o");
            }

            return _OverlapAwareCompare(flow);
        }


        /// <summary>
        /// Compute hash code. A flow position is predominantly identified
        /// by its flow node and the offset. 
        /// </summary>
        /// <returns>int - hash code</returns>
        public override int GetHashCode()
        {
            return _flowNode.GetHashCode()^_offset.GetHashCode();
        }


#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "FP[{0}+{1}]", _flowNode, _offset);
        }
#endif
        #endregion Public Methods

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

        //--------------------------------------------------------------------
        // Text OM Helper 
        //---------------------------------------------------------------------
        #region Text OM Helper
        // Returns the count of symbols between pos1 and pos2
        internal int GetDistance(FlowPosition flow)
        {
            Debug.Assert(flow != null);
            // if both clings to the same flow node, simply 
            // compare the offset
            if (_flowNode.Equals(flow._flowNode))
            {
                return flow._offset - _offset;
            }

            // otherwise scan to find out distance
            // Make sure scanning from low to high flow order
            int np = _OverlapAwareCompare(flow);
            FlowPosition flowScan, flowEnd;
            if (np == -1) 
            {
                // scan forward
                flowScan = (FlowPosition)this.Clone();
                flowEnd  = flow;
            }
            else
            {
                // scan backward
                flowScan = (FlowPosition)flow.Clone();
                flowEnd  = this;
            }

            // scan from low to high and accumulate counts
            // devirtualize the node as it scans
            int distance = 0;
            while (!flowScan._IsSamePosition(flowEnd))
            {
                if (flowScan._flowNode.Equals(flowEnd._flowNode))
                {
                    distance += (flowEnd._offset - flowScan._offset);
                    break;
                }
                int scan = flowScan._vScan(LogicalDirection.Forward, -1);

                distance += scan;
            } 
            return np * (-1) * distance;
        }



        // Returns TextPointerContext.None if query is Backward at TextContainer.Start or Forward at TetContainer.End
        internal TextPointerContext GetPointerContext(LogicalDirection dir)
        {
            Debug.Assert(dir == LogicalDirection.Forward || dir == LogicalDirection.Backward);
            return _vGetSymbolType(dir);
        }
            

        // returns remaining text length on dir
        internal int GetTextRunLength(LogicalDirection dir)
        {
            Debug.Assert(GetPointerContext(dir) == TextPointerContext.Text);
            FlowPosition flow = GetClingPosition(dir);

            if (dir == LogicalDirection.Forward)
            {
                return flow._NodeLength - flow._offset;
            }
            else
            {
                return flow._offset;
            }
        }


        // Get Text until end-of-run or maxLength/limit is hit. 
        internal int GetTextInRun(LogicalDirection dir, int maxLength, char[] chars, int startIndex)
        {
            Debug.Assert(GetPointerContext(dir) == TextPointerContext.Text);

            // make sure the position is clinged to text run
            FlowPosition flow = GetClingPosition(dir);
            
            int runLength = flow._NodeLength;
            int remainingLength;
            if (dir == LogicalDirection.Forward)
            {
                remainingLength = runLength - flow._offset;
            }
            else
            {
                remainingLength = flow._offset;
            }
            maxLength = Math.Min(maxLength, remainingLength);

            //
            // THIS IS VERY INEFFICIENT! 
            // We need to add a function in FixedTextBuilder that 
            // allows copying segement of the flow node text run directly. 
            //

            string text = _container.FixedTextBuilder.GetFlowText(flow._flowNode);
            if (dir == LogicalDirection.Forward)
            {
                Array.Copy(text.ToCharArray(flow._offset, maxLength), 0, chars, startIndex, maxLength);
            }
            else
            {
                Array.Copy(text.ToCharArray(flow._offset - maxLength, maxLength), 0, chars, startIndex, maxLength);
            }
            return maxLength;
        }


        // Get Embedeed Object instance
        internal object GetAdjacentElement(LogicalDirection dir)
        {
            FlowPosition flow = GetClingPosition(dir);
            FlowNodeType type = flow._flowNode.Type;
            Debug.Assert(type == FlowNodeType.Object || type == FlowNodeType.Noop || type == FlowNodeType.Start || type == FlowNodeType.End);
            
            if (type == FlowNodeType.Noop)
            {
                return String.Empty;
            }
            else
            {
                Object obj = ((FixedElement)flow._flowNode.Cookie).GetObject();
                Image image = obj as Image;
                if (type == FlowNodeType.Object && image != null)
                {
                    //Set width and height properties by looking at corresponding SOMImage
                    FixedSOMElement[] elements = flow._flowNode.FixedSOMElements;
                    if (elements != null && elements.Length > 0)
                    {
                        FixedSOMImage somImage = elements[0] as FixedSOMImage;
                        if (somImage != null)
                        {
                            image.Width = somImage.BoundingRect.Width;
                            image.Height = somImage.BoundingRect.Height;
                        }
                    }
                }
                return obj;
            }
        }

        // return FixedElement on the direction
        internal FixedElement GetElement(LogicalDirection dir)
        {
            FlowPosition flow = GetClingPosition(dir);
            return (FixedElement)flow._flowNode.Cookie;
        }


        // Immediate scoping element. If no element scops the position, 
        // returns the container element. 
        internal FixedElement GetScopingElement()
        {
            FlowPosition flowScan = (FlowPosition)this.Clone();
            int nestedElement = 0;
            TextPointerContext tst;

            while (flowScan.FlowNode.Fp > 0 && !IsVirtual(_FixedFlowMap[flowScan.FlowNode.Fp - 1]) && // do not de-virtualize nodes
                (tst = flowScan.GetPointerContext(LogicalDirection.Backward))!= TextPointerContext.None)
            {
                if (tst == TextPointerContext.ElementStart)
                {
                    if (nestedElement == 0)
                    {
                        FlowPosition flowEnd = flowScan.GetClingPosition(LogicalDirection.Backward);
                        return (FixedElement)flowEnd._flowNode.Cookie;
                    }
                    nestedElement--;
                }
                else if (tst == TextPointerContext.ElementEnd)
                {
                    nestedElement++;
                }

                flowScan.Move(LogicalDirection.Backward);
            } 
            return _container.ContainerElement;
        }


        // return false if distance exceeds the size of the document
        internal bool Move(int distance)
        {
            LogicalDirection dir = (distance >= 0 ? LogicalDirection.Forward : LogicalDirection.Backward);
            distance = Math.Abs(distance);
            FlowNode startNode = _flowNode;  // save state
            int startOffset = _offset;
            // keep moving until we hit distance
            while (distance > 0)
            {
                int scan = _vScan(dir, distance);
                if (scan == 0)
                {
                    //restore state and return false
                    _flowNode = startNode;
                    _offset = startOffset;
                    return false;
                }
                distance -= scan;
            }
            return true;
        }


        // Skip current symbol or run
        internal bool Move(LogicalDirection dir)
        {
            if (_vScan(dir, -1) > 0)
            {
                return true;
            }

            return false;
        }

        // Move to next FlowPosition
        internal void MoveTo(FlowPosition flow)
        {
            _flowNode   = flow._flowNode;
            _offset     = flow._offset;
        }

        #endregion Text OM Helper


        //--------------------------------------------------------------------
        // Internal Methods
        //---------------------------------------------------------------------
        internal void AttachElement(FixedElement e)
        {
            _flowNode.AttachElement(e);
        }

        internal void GetFlowNode(LogicalDirection direction, out FlowNode flowNode, out int offsetStart)
        {
            FlowPosition fp = GetClingPosition(direction);

            offsetStart = fp._offset;
            flowNode = fp._flowNode;
        }

        // return FlowNode within this range
        internal void GetFlowNodes(FlowPosition pEnd, out FlowNode[] flowNodes, out int offsetStart, out int offsetEnd)
        {
            Debug.Assert(this._OverlapAwareCompare(pEnd) < 0);
            flowNodes = null;
            offsetStart = 0;
            offsetEnd = 0;

            FlowPosition flowScan = GetClingPosition(LogicalDirection.Forward);
            offsetStart = flowScan._offset;

            ArrayList ar = new ArrayList();
            int distance = GetDistance(pEnd);
            // keep moving until we hit distance
            while (distance > 0)
            {
                int scan = flowScan._vScan(LogicalDirection.Forward, distance);

                distance -= scan;
                if (flowScan.IsRun || flowScan.IsObject)
                {
                    ar.Add(flowScan._flowNode);
                    offsetEnd = flowScan._offset;
                }
            }
            flowNodes = (FlowNode [])ar.ToArray(typeof(FlowNode));
        }

        // A canonical position is one that clings to a FlowNode
        internal FlowPosition GetClingPosition(LogicalDirection dir)
        {
            FlowPosition flow = (FlowPosition)this.Clone();
            FlowNode fn;

            if (dir == LogicalDirection.Forward)
            {
                if (_offset == _NodeLength)
                {
                    // This position is at right side of a FlowNode
                    // look for next run
                    fn = _xGetNextFlowNode();
                    if (!FlowNode.IsNull(fn))
                    {
                        flow._flowNode = fn;
                        flow._offset = 0;
                    }
#if DEBUG
                    else
                    {
                        DocumentsTrace.FixedTextOM.FlowPosition.Trace("GetClingPosition: Next FlowNode is null");
                    }
#endif
                }
            }
            else
            {
                Debug.Assert(dir == LogicalDirection.Backward);
                if (_offset == 0)
                {
                    // This position is at left side of a FlowNode
                    // look for previous run
                    fn = _xGetPreviousFlowNode();
                    if (!FlowNode.IsNull(fn))
                    {
                        flow._flowNode = fn;
                        flow._offset = flow._NodeLength;
                    }
#if DEBUG
                    else
                    {
                        DocumentsTrace.FixedTextOM.FlowPosition.Trace("GetClingPosition: Next FlowNode is null");
                    }
#endif
                }
            }
            return flow;
        }


        internal bool IsVirtual(FlowNode flowNode)
        {
            return (flowNode.Type == FlowNodeType.Virtual);
        }
        #endregion Internal Methods


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        internal FixedTextContainer TextContainer
        {
            get
            {
                return _container;
            }
        }

        internal bool IsBoundary
        {
            get
            {
                return (_flowNode.Type == FlowNodeType.Boundary);
            }
        }

        internal bool IsStart
        {
            get
            {
                return (_flowNode.Type == FlowNodeType.Start);
            }
        }

        internal bool IsEnd
        {
            get
            {
                return (_flowNode.Type == FlowNodeType.End);
            }
        }

        // If th_Is position _Is symbol 
        internal bool IsSymbol
        {
            get
            {
                FlowNodeType t = _flowNode.Type;
                return (t == FlowNodeType.Start || t == FlowNodeType.End || t == FlowNodeType.Object);
            }
        }


        // If the FlowNode has run length
        internal bool IsRun
        {
            get
            {
                return (_flowNode.Type == FlowNodeType.Run);
            }
        }

        // If the FlowNode has run length
        internal bool IsObject
        {
            get
            {
                return (_flowNode.Type == FlowNodeType.Object);
            }
        }

        internal FlowNode FlowNode
        {
            get
            {
                return this._flowNode;
            }
        }
        
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        //--------------------------------------------------------------------
        //  Helper functions that could result in de-virtualization 
        //---------------------------------------------------------------------

        // scan one node forward, return characters/symbols passed
        // limit < 0 means scan entire node. 
        private int _vScan(LogicalDirection dir, int limit)
        {
            if (limit == 0)
            {
                return 0;
            }

            FlowNode flowNode = _flowNode;
            int scanned = 0;
            if (dir == LogicalDirection.Forward)
            {
                if (_offset == _NodeLength || flowNode.Type == FlowNodeType.Boundary)
                {
                    // This position is at right side of a FlowNode
                    flowNode = _xGetNextFlowNode();
                    if (FlowNode.IsNull(flowNode))
                    {
                        return scanned;
                    }
                    _flowNode = flowNode;
                    scanned = _NodeLength;
                }
                else
                {
                    scanned = _NodeLength - _offset;
                }

                _offset = _NodeLength;
                if (limit > 0 && scanned > limit)
                {
                    int back = scanned - limit;
                    scanned  = limit;
                    _offset -= back;
                }
            }
            else
            {
                Debug.Assert(dir == LogicalDirection.Backward);
                if (_offset == 0 || flowNode.Type == FlowNodeType.Boundary)
                {
                    // This position is at left side of a FlowNode
                    // look for previous run
                    flowNode = _xGetPreviousFlowNode();
                    if (FlowNode.IsNull(flowNode))
                    {
                        return scanned;
                    }

                    _flowNode = flowNode;
                    scanned = _NodeLength;
                }
                else
                {
                    scanned = _offset;
                }

                _offset = 0;
                if (limit > 0 && scanned > limit)
                {
                    int back = scanned - limit;
                    scanned  = limit;
                    _offset += back;
                }
            }
            return scanned;
        }


        private TextPointerContext _vGetSymbolType(LogicalDirection dir)
        {
            FlowNode flowNode = _flowNode;
            if (dir == LogicalDirection.Forward)
            {
                if (_offset == _NodeLength)
                {
                    // This position is at right side of a FlowNode
                    // look for next run
                    flowNode = _xGetNextFlowNode();
                }

                if (!FlowNode.IsNull(flowNode))
                {
                    return _FlowNodeTypeToTextSymbol(flowNode.Type);
                }
            }
            else
            {
                Debug.Assert(dir == LogicalDirection.Backward);
                if (_offset == 0)
                {
                    // This position is at left side of a FlowNode
                    // look for previous run
                    flowNode = _xGetPreviousFlowNode();
                }

                if (!FlowNode.IsNull(flowNode))
                {
                    return _FlowNodeTypeToTextSymbol(flowNode.Type);
                }
            }
            return TextPointerContext.None;
        }


        //--------------------------------------------------------------------
        // Helper functions that have raw access to backing store and provided
        // a non-virtualied view on top of virtualized content. 
        //---------------------------------------------------------------------

        // return null if no previous node
        private FlowNode _xGetPreviousFlowNode()
        {
            if (_flowNode.Fp > 1)
            {
                FlowNode flow = _FixedFlowMap[_flowNode.Fp - 1];

                // devirtualize the backing store
                if (IsVirtual(flow))
                {
                    _FixedTextBuilder.EnsureTextOMForPage((int)flow.Cookie);
                    flow = _FixedFlowMap[_flowNode.Fp - 1];
                }

                // filter out boundary node
                if (flow.Type != FlowNodeType.Boundary)
                {
                    return flow;
                }
            }
            return null;
        }


        // return null if no next node
        private FlowNode _xGetNextFlowNode()
        {
            if (_flowNode.Fp < _FixedFlowMap.FlowCount - 1)
            {
                FlowNode flow = _FixedFlowMap[_flowNode.Fp + 1];

                // devirtualize the backing store
                if (IsVirtual(flow))
                {
                    _FixedTextBuilder.EnsureTextOMForPage((int)flow.Cookie);
                    flow = _FixedFlowMap[_flowNode.Fp + 1];
                }

                // filter out boundary node
                if (flow.Type != FlowNodeType.Boundary)
                {
                    return flow;
                }
            }

            return null;
        }


        //--------------------------------------------------------------------
        // Helper functions 
        //---------------------------------------------------------------------

        // see if the two FlowPosition are indeed same position
        private bool _IsSamePosition(FlowPosition flow)
        {
            if (flow == null)
            {
                return false;
            }

            return (_OverlapAwareCompare(flow) == 0);
        }


        // intelligent compare routine that understands position overlap
        private int _OverlapAwareCompare(FlowPosition flow)
        {
            Debug.Assert(flow != null);
            if ((object)this == (object)flow)
            {
                return 0;
            }

            int comp = this._flowNode.CompareTo(flow._flowNode);
            if (comp < 0)
            {
                // Check overlap positions
                // Positions are the same if they are at end of previous run or begin of next run
                if (this._flowNode.Fp == flow._flowNode.Fp - 1 && this._offset == _NodeLength && flow._offset == 0)
                {
                    return 0;
                }
            }
            else if (comp > 0)
            {
                // Check overlap positions
                // Positions are the same if they are at end of previous run or begin of next run
                if (flow._flowNode.Fp == this._flowNode.Fp - 1 && flow._offset == flow._NodeLength && this._offset == 0)
                {
                    return 0;
                }
            }
            else
            {
                // compare offset only
                Debug.Assert(this._flowNode.Equals(flow._flowNode));
                comp = this._offset.CompareTo(flow._offset);
            }
            return comp;
        }


        // Convert Flow Node Type to TextPointerContext 
        private TextPointerContext _FlowNodeTypeToTextSymbol(FlowNodeType t)
        {
            switch (t)
            {
                case FlowNodeType.Start:
                    return TextPointerContext.ElementStart;

                case FlowNodeType.End:
                    return TextPointerContext.ElementEnd;

                case FlowNodeType.Run:
                    return TextPointerContext.Text;

                case FlowNodeType.Object:
                case FlowNodeType.Noop:
                    return TextPointerContext.EmbeddedElement;

                default:
                    return TextPointerContext.None;
            }
        }


        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------------------------

        #region Private Properties
        // Get length of this node structure
        private int _NodeLength
        {
            get
            {
                if (IsRun)
                {
                    return (int)_flowNode.Cookie;
                }
                else
                {
                    return 1;
                }
            }
        }


        private FixedTextBuilder _FixedTextBuilder
        {
            get
            {
                return _container.FixedTextBuilder;
            }
        }

        private FixedFlowMap _FixedFlowMap
        {
            get
            {
                return _container.FixedTextBuilder.FixedFlowMap;
            }
        }
        #endregion Private Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private FixedTextContainer  _container;         // the container has the backing store for flow nodes
        private FlowNode            _flowNode;          // flow node
        private int                 _offset;            // offset into flow
        #endregion Private Fields
    }
}
