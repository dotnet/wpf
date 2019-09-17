// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Windows;

namespace System.Windows.Controls
{
    public class DataGridRowDetailsEventArgs : EventArgs
    {
        public DataGridRowDetailsEventArgs(DataGridRow row, FrameworkElement detailsElement)
        {
            Row = row;
            DetailsElement = detailsElement;
        }

        public FrameworkElement DetailsElement 
        { 
            get; private set; 
        }

        public DataGridRow Row 
        { 
            get; private set; 
        }
    }
}
