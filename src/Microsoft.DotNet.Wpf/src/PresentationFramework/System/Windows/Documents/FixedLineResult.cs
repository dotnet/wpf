﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

//
// Description:
//      FixedLineResult represents a per-line layout info for a fixe page
//

namespace System.Windows.Documents
{
    //=====================================================================
    /// <summary>
    ///     FixedLineResult represents a per-line layout info for a fixe page
    /// </summary>
    internal sealed class FixedLineResult : IComparable
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FixedLineResult(FixedNode[] nodes, Rect layoutBox)
        {
            _nodes = nodes;
            _layoutBox = layoutBox;
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

        // IComparable Override
        public int CompareTo(object o)
        {
            ArgumentNullException.ThrowIfNull(o);

            if (o.GetType() != typeof(FixedLineResult))
            {
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, o.GetType(), typeof(FixedLineResult)), nameof(o));
            }

            FixedLineResult lineResult = (FixedLineResult)o;
            return this.BaseLine.CompareTo(lineResult.BaseLine);
        }


#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"FLR[{Start}:{End}][{BaseLine}][{_layoutBox}]");
        }
#endif

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


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        //
        internal FixedNode Start
        {
            get
            {
                return _nodes[0];
            }
        }

        internal FixedNode End
        {
            get
            {
                return _nodes[_nodes.Length - 1];
            }
        }

        internal FixedNode[] Nodes
        {
            get
            {
                return _nodes;
            }
        }

        internal double BaseLine
        {
            get
            {
                return _layoutBox.Bottom;
            }
        }

        internal Rect LayoutBox
        {
            get
            {
                return _layoutBox;
            }
        }
        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Properties
        #endregion Private Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        #region Private Fields
        private readonly FixedNode[]  _nodes;
        private readonly Rect       _layoutBox;  // relative to page
        #endregion Private Fields
    }
}
