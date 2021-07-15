// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

using MS.Internal;

namespace System.Windows.Controls
{
    /// <summary>
    /// The separator used to indicate drop location during column header drag-drop
    /// </summary>
    internal class DataGridColumnDropSeparator : Separator
    {
        #region Constructors

        static DataGridColumnDropSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DataGridColumnDropSeparator), 
                new FrameworkPropertyMetadata(DataGridColumnHeader.ColumnHeaderDropSeparatorStyleKey));

            WidthProperty.OverrideMetadata(
                typeof(DataGridColumnDropSeparator), 
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceWidth)));
            
            HeightProperty.OverrideMetadata(
                typeof(DataGridColumnDropSeparator), 
                new FrameworkPropertyMetadata(null, new CoerceValueCallback(OnCoerceHeight)));
        }

        #endregion

        #region Static Methods

        private static object OnCoerceWidth(DependencyObject d, object baseValue)
        {
            double width = (double)baseValue;
            if (DoubleUtil.IsNaN(width))
            {
                return 2.0;
            }

            return baseValue;
        }

        private static object OnCoerceHeight(DependencyObject d, object baseValue)
        {
            double height = (double)baseValue;
            DataGridColumnDropSeparator separator = (DataGridColumnDropSeparator)d;
            if (separator._referenceHeader != null && DoubleUtil.IsNaN(height))
            {
                return separator._referenceHeader.ActualHeight;
            }

            return baseValue;
        }

        #endregion

        #region Properties

        internal DataGridColumnHeader ReferenceHeader
        {
            get
            {
                return _referenceHeader;
            }

            set
            {
                _referenceHeader = value;
            }
        }

        #endregion

        #region Data

        private DataGridColumnHeader _referenceHeader;

        #endregion
    }
}
