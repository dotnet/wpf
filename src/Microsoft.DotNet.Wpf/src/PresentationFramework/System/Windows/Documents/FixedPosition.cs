// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedPosition represents a hit-testable position in a fixed document's tree.
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    

    //=====================================================================
    /// <summary>
    ///      FixedPosition represents a hit-testable position in a fixed document's tree.
    /// </summary>
    internal struct FixedPosition 
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FixedPosition(FixedNode fixedNode, int offset)
        {
            _fixedNode  = fixedNode;
            _offset     = offset;
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------

#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "FN[{0}]-Offset[{1}]", _fixedNode.ToString(), _offset);
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
        internal int Page
        {
            get
            {
                return _fixedNode.Page;
            }
        }

        //
        internal FixedNode Node
        {
            get
            {
                return _fixedNode;
            }
        }

        internal int Offset
        {
            get
            {
                return _offset;
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
        private readonly FixedNode _fixedNode;
        private readonly int       _offset;      // offset into the fixed node
        #endregion Private Fields
    }
}
