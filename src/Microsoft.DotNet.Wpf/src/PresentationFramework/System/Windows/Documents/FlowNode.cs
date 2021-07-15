// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FlowNode represents a structural node for a flow document. They
//      are inserted into a content flow backing store content flow their 
//      relative orders signal the content flow order. 
//

namespace System.Windows.Documents
{
    using System;
    using System.Diagnostics;
    
    //=====================================================================
    /// <summary>
    /// FlowNode represents a structural node in the Flow Order of a fixed document. 
    /// They are inserted into a content flow backing store (the Flow Order) to 
    /// represent a flow order view of the fixed document.  Their relative position 
    /// in the Flow Order indicates their relative reading order. 
    /// 
    /// A FlowNode is identified by its ScopeId and is compared by its relative position
    /// in the Flow Order (Fp). 
    ///
    /// Content structure and scope can be deduced from the Flow Order. 
    /// For instance
    ///     S1 S2 R2 E2 E1
    /// would indicate element 1 (S1-E1) is the parent of element 2 (S1-E2) 
    /// </summary>
    internal sealed class FlowNode : IComparable
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FlowNode(int scopeId, FlowNodeType type, object cookie)
        {
            //
            // If cookie is FixedElement, you have to set it later, otherwise,
            // please set it here.
            // In that case, cookie will be set as a FixedElement later.
            // It cann't be set here because of the circular reference.
            //

            //
            // We don't allow to create a glyph node with zero run length.
            // it will lots of problem in textOM due to some assumption there.
            //
            Debug.Assert( (type != FlowNodeType.Run)  ||  ((int)cookie != 0));

            _scopeId = scopeId;
            _type       = type;
            _cookie     = cookie;
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
          
        #region Public Methods

        // force object comparision
        public static bool IsNull(FlowNode flow)
        {
            return (object)flow == null;
        }


        /// <summary>
        /// </summary>
        /// <returns>int - hash code</returns>
        public override int GetHashCode()
        {
            return _scopeId.GetHashCode()^_fp.GetHashCode();
        }


        /// <summary>
        /// Compares this FlowNode with the passed in object
        /// </summary>
        /// <param name="o">The object to compare to "this"</param>
        /// <returns>bool - true if the FlowNodes are equal, false otherwise</returns>
        public override bool Equals(object o)
        {
            if (o == null || this.GetType() != o.GetType())
            {
                return false;
            }

            FlowNode fn = (FlowNode)o;
            Debug.Assert(_fp != fn._fp || (_fp == fn._fp) && (_scopeId == fn._scopeId));

            return (this._fp == fn._fp);
        }


        /// <summary>
        /// Compare the flow order of this FlowNode to another FlowNode
        /// </summary>
        /// <param name="o">FlowNode to compare to</param>
        /// <returns>-1, 0, 1</returns>
        public int CompareTo(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            FlowNode fp = o as FlowNode;
            if (fp == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, o.GetType(), typeof(FlowNode)), "o");
            }

            if (Object.ReferenceEquals(this, fp))
            {
                return 0;
            }

            int fd = this._fp - fp._fp;
            if (fd == 0)
            {
                return 0;
            }
            else if (fd < 0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            int page = -1;
            
            switch (_type)
            {
                case FlowNodeType.Boundary:
                case FlowNodeType.Start:
                case FlowNodeType.End:
                case FlowNodeType.Object:
                {
                    FixedElement element = _cookie as FixedElement;
                    if (element != null)
                    {
                        page = element.PageIndex;
                    }
                    break;
                }
                case FlowNodeType.Virtual:
                case FlowNodeType.Noop:
                {
                    page = (int) _cookie;
                    break;
                }
                case FlowNodeType.Run:
                {
                    if (this.FixedSOMElements != null && this.FixedSOMElements.Length > 0)
                    {
                        page = this.FixedSOMElements[0].FixedNode.Page;
                    }
                    break;
                }
                default:
                    break;
            }
            
            
            return String.Format("Pg{0}-nCp{1}-Id{2}-Tp{3}", page, _fp, _scopeId, System.Enum.GetName(typeof(FlowNodeType), _type));
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

        //
        // Note: Make these explicit method so that it won't be used mistakenly
        // Only FixedTextBuilder can modify Fp
        //

        internal void SetFp(int fp)
        {
            _fp = fp;
        }

        internal void IncreaseFp()
        {
            _fp++;
        }

        internal void DecreaseFp()
        {
            _fp--;
        }
        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        // Get/Set position within flow order 
        internal int Fp
        {
            get
            {
                return _fp;
            }
        }

        // Get the scope to which this node belongs
        internal int ScopeId
        {
            get
            {
                return _scopeId;
            }
        }

        // Get the Flag associated with this node
        internal FlowNodeType Type
        {
            get
            {
                return _type;
            }
        }


        // Get/Set the cookie associated with this position. 
        // Higher level protocol decides what to put inside
        // the cookie
        internal object Cookie
        {
            get
            {
                return _cookie;
            }
        }

        internal FixedSOMElement[] FixedSOMElements
        {
            get
            {
                return _elements;
            }
            set
            {
                _elements = value;
            }
        }

        internal void AttachElement(FixedElement fixedElement)
        {
            _cookie = fixedElement;
        }

        #endregion Internal Properties

 
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly int _scopeId;          // Node Identifier
        private readonly FlowNodeType _type;    // type of the node
        private int _fp;                        // Position in flow
        private object _cookie;                 // Associated object
        private FixedSOMElement[] _elements;    // Used for mapping between fixed and flow representations
        #endregion Private Fields
    }
}
