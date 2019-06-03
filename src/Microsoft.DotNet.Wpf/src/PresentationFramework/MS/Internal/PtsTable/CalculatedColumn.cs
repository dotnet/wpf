// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Implementation of calculated column class.
//              Calculated columns are created as a result of table
//              width calculations. Calculated columns are used internally
//              to hold information about table's horizontal geometry
//


using MS.Internal.PtsHost;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsTable
{
    /// <summary>
    /// Calculated column implementation.
    /// </summary>
    internal struct CalculatedColumn
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// ValidateAuto
        /// </summary>
        /// <param name="durMinWidth">New min width value</param>
        /// <param name="durMaxWidth">New max width value</param>
        internal void ValidateAuto(double durMinWidth, double durMaxWidth)
        {
            Debug.Assert(0 <= durMinWidth && durMinWidth <= durMaxWidth);
            _durMinWidth = durMinWidth;
            _durMaxWidth = durMaxWidth;
            SetFlags(true, Flags.ValidAutofit);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns understood by PTS state of cell width dirtiness.
        /// </summary>
        internal int PtsWidthChanged { get { return (PTS.FromBoolean(!CheckFlags(Flags.ValidWidth))); } }

        /// <summary>
        /// DurMinWidth
        /// </summary>
        internal double DurMinWidth { get { return (_durMinWidth); } }

        /// <summary>
        /// DurMaxWidth
        /// </summary>
        internal double DurMaxWidth { get { return (_durMaxWidth); } }

        /// <summary>
        /// UserWidth
        /// </summary>
        internal GridLength UserWidth
        {
            get
            {
                return (_userWidth);
            }
            set
            {
                if (_userWidth != value)
                {
                    SetFlags(false, Flags.ValidAutofit);
                }

                _userWidth = value;
            }
        }

        /// <summary>
        /// DurWidth
        /// </summary>
        internal double DurWidth
        {
            get
            {
                return (_durWidth);
            }
            set
            {
                if (!DoubleUtil.AreClose(_durWidth, value))
                {
                    SetFlags(false, Flags.ValidWidth);
                }

                _durWidth = value;
            }
        }

        /// <summary>
        /// UrOffset
        /// </summary>
        internal double UrOffset
        {
            get { return (_urOffset); }
            set { _urOffset = value; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// SetFlags is used to set or unset one or multiple flags on the cell.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlags returns true if all passed flags in the bitmask are set.
        /// </summary>
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private GridLength _userWidth;      //  user specified width for the column
        private double _durWidth;       //  calculated widht for the column
        private double _durMinWidth;    //  calculated minimum width for the column
        private double _durMaxWidth;    //  calculated maximum width for the column
        private double _urOffset;       //  column's offset
        private Flags _flags;           //  state
        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        [System.Flags]
        private enum Flags
        {
            ValidWidth      =   0x1,    //  resulting width unchanged
            ValidAutofit    =   0x2,    //  auto width unchanged
        }

        #endregion Private Structures Classes
    }
}
