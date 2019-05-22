// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;

namespace System.Windows.Controls
{
    /// <summary>
    /// Event args used for Column reordering event raised after column header drag-drop
    /// </summary>
    public class DataGridColumnReorderingEventArgs : DataGridColumnEventArgs
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataGridColumn"></param>
        public DataGridColumnReorderingEventArgs(DataGridColumn dataGridColumn)
            : base(dataGridColumn)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Property to specify whether the Reordering operation should be cancelled
        /// </summary>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }

            set
            {
                _cancel = value;
            }
        }

        /// <summary>
        /// The control which would be used as an indicator for drop location during column header drag drop
        /// </summary>
        public Control DropLocationIndicator
        {
            get
            {
                return _dropLocationIndicator;
            }

            set
            {
                _dropLocationIndicator = value;
            }
        }

        /// <summary>
        /// The control which would be used as a drag indicator during column header drag drop.
        /// </summary>
        public Control DragIndicator
        {
            get
            {
                return _dragIndicator;
            }

            set
            {
                _dragIndicator = value;
            }
        }

        #endregion

        #region Data

        private bool _cancel;
        private Control _dropLocationIndicator;
        private Control _dragIndicator;

        #endregion
    }
}