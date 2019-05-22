// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedNode is an immutable type that represents a fast and 
//      efficient way to orderly locate an element in a fixed document
//      It is inserted into Fixed Order, which provides a linear view
//      of the fixed document. 
//

namespace System.Windows.Documents
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    //=====================================================================
    /// <summary>
    /// FixedNode is an immutable type that represents a fast and 
    /// efficient way to locate an element in a fixed document. All elements
    /// are leaf nodes of a fixed document tree. 
    /// 
    /// It is inserted into Fixed Order to provide a linear view 
    /// of the fixed document. 
    /// 
    /// Usage:
    /// 1. Given a FixedNode, get the element it refers to (Glyphs/Image/etc. GetText etc.)
    /// 2. Given a Glyphs, construct its FixedNode representation (Page Analysis/HitTesting)
    /// 3. Given a FixedNode, find out which FlowNode it maps to. 
    /// 4. Given a FixedNode, compare its relative position with another FixedNode in Fixed Order.
    /// 5. Used to represent artifical boundary (page/document) node. 
    /// 
    /// Design:
    /// 
    /// FixedNode contains a path array that indicates the path from top most container 
    /// to the leaf node. Example:
    /// 
    /// +------------------+
    /// | P# | ChildIndex  |            1st Level Leaf Node                                      
    /// +------------------+
    /// 
    /// +--------------------------------+
    /// | P# | ChildIndex  | ChildIndex  |  2nd Level Leaf Node
    /// +--------------------------------+
    /// 
    /// +---------------------------------------------+
    /// | P# | ChildIndex  | ChildIndex  | ChildIndex |  3rd Level Leaf Node
    /// +---------------------------------------------+
    /// 
    /// etc.
    /// 
    /// Although I have carefully considered alternative designs that does not 
    /// require using a fixed array, that uses clever schema to fully utilize
    /// the entire uint32 address space, The Type Safty feature of CLR makes 
    /// it difficult to efficiently handle overflow situation (deep nesting
    /// level leaf node). 
    /// 
    /// The Array approach has its virtue of simplicity. 
    /// 
    /// The real Page index always starts at 0, ends at n-1
    /// The real Index always starts at 0, ends at n-1
    /// Any index outside of the range is position mark.
    /// </summary>
    internal struct FixedNode : IComparable
    {
        //--------------------------------------------------------------------
        //
        // Static Method
        //
        //---------------------------------------------------------------------

        // Factory method to create a fixed node from its path
        // Avoiding create ArrayList if possible
        // Most common case childLevel is either 1, 2  level deep. 
        // level 0 is always the page
        internal static FixedNode Create(int pageIndex, int childLevels, int level1Index, int level2Index, int[] childPath)
        {
            Debug.Assert(childLevels >= 1);
            switch (childLevels)
            {
                case 1:
                    return new FixedNode(pageIndex, level1Index);

                case 2:
                    return new FixedNode(pageIndex, level1Index, level2Index);

                default:
                    return FixedNode.Create(pageIndex, childPath);
            }
        }

        internal static FixedNode Create(int pageIndex, int[] childPath)
        {
            Debug.Assert(childPath != null);
            int[] completePath = new int[childPath.Length + 1];
            completePath[0] = pageIndex;
            childPath.CopyTo(completePath, 1);

            return new FixedNode(completePath);
        }

        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Ctor for common case of Level 1 leaf node
        /// </summary>
        private FixedNode(int page, int level1Index)
        {
            _path = new int[2];
            _path[0] = page;
            _path[1] = level1Index;
        }


        // Ctor for common case Level 2 leaf node
        private FixedNode(int page, int level1Index, int level2Index)
        {
            _path = new int[3];
            _path[0] = page;
            _path[1] = level1Index;
            _path[2] = level2Index;
        }

        // Ctor for deep nesting case
        private FixedNode(int[] path)
        {
            Debug.Assert(path != null && path.Length >= 2); // At least a page index and an element index
            _path = path;
       }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
          
        #region Public Methods
        // IComparable Override
        public int CompareTo(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException("o");
            }

            if (o.GetType() != typeof(FixedNode))
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, o.GetType(), typeof(FixedNode)), "o");
            }

            FixedNode fixedp = (FixedNode)o;
            return CompareTo(fixedp);
        }


        // Strongly typed compare function to avoid boxing
        // This function wil return 0 if two nodes are parent 
        // and child relationship.
        // Positive means this node is before the input node.
        // Negative means this node is after the input node.
        public int CompareTo(FixedNode fixedNode)
        {
            int comp = this.Page.CompareTo(fixedNode.Page);
            if (comp == 0)
            {
                // compare index when two FixedNodes are in the same page
                int level = 1;
                while (level <= this.ChildLevels && level <= fixedNode.ChildLevels)
                {
                    int thisIndex   = this[level];
                    int fixedNodeIndex = fixedNode[level];
                    if (thisIndex == fixedNodeIndex)
                    {
                        level++;
                        continue;
                    }
                    return thisIndex.CompareTo(fixedNodeIndex);
                }
            }
            return comp;
        }

        /// <summary>
        /// childPath doesn't not include the PageNumberIndex
        /// This function wil return 0 if two nodes are parent 
        /// and child relationship.
        /// Positive means this node is before the input node.
        /// Negative means this node is after the input node.
        /// </summary>
        /// <param name="childPath"></param>
        /// <returns></returns>
        internal int ComparetoIndex(int[] childPath)
        {
            for (int i = 0; i < childPath.Length && i < _path.Length - 1; i++)
            {
                if (childPath[i] == _path[i + 1])
                    continue;

                return childPath[i].CompareTo(_path[i + 1]);
            }
            return 0;
        }
        
        public static bool operator <(FixedNode fp1, FixedNode fp2)
        {
            return fp1.CompareTo(fp2) < 0;
        }

        public static bool operator <=(FixedNode fp1, FixedNode fp2)
        {
            return fp1.CompareTo(fp2) <= 0;
        }

        public static bool operator >(FixedNode fp1, FixedNode fp2)
        {
            return fp1.CompareTo(fp2) > 0;
        }

        public static bool operator >=(FixedNode fp1, FixedNode fp2)
        {
            return fp1.CompareTo(fp2) >= 0;
        }


        /// <summary>
        /// Compares this FixedNode with the passed in object
        /// </summary>
        /// <param name="o">The object to compare to "this"</param>
        /// <returns>bool - true if the FixedNodes are equal, false otherwise</returns>
        public override bool Equals(object o)
        {
            if (o is FixedNode)
            {
                return Equals((FixedNode)o);
            }

            return false;
        }


        // Strongly typed version of Equals to avoid boxing
        public bool Equals(FixedNode fixedp)
        {
            return (this.CompareTo(fixedp) == 0);
        }


        public static bool operator ==(FixedNode fp1, FixedNode fp2)
        {
            return fp1.Equals(fp2);
        }

        public static bool operator !=(FixedNode fp1, FixedNode fp2)
        {
            return !fp1.Equals(fp2);
        }



        /// <summary>
        /// Returns the HashCode for this FixedNode
        /// </summary>
        /// <returns>int the HashCode for this FixedNode</returns>
        public override int GetHashCode()
        {
            //return _path.GetHashCode();
			int hash = 0;
			foreach (int i in _path)
			{
				hash = 43 * hash + i;
			}
			return hash;
        }

#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format(CultureInfo.InvariantCulture, "P{0}-", _path[0]));
            for (int i = 1; i < _path.Length; i++)
            {
                sb.Append(String.Format(CultureInfo.InvariantCulture, "[{0}]", _path[i]));
            }
            return sb.ToString();
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
        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        internal int Page
        {
            get
            {
                return _path[0];
            }
        }

        // Accessor to different level of the path
        // Level 0 is always the page index. 
        // element index inside the page start from Level 1.
        internal int this[int level]
        {
            get
            {
                Debug.Assert(level < _path.Length);
                return _path[level];
            }
        }

        // element path levels 
        // it is always greater than 1.  
        internal int ChildLevels
        {
            get
            {
                Debug.Assert(_path.Length >= 2);
                return _path.Length - 1;
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly int[] _path;   // path (including PageIndex) to the leaf node
        #endregion Private Fields
    }
}
