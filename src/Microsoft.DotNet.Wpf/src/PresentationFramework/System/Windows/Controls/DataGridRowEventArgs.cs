// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Controls
{
    public class DataGridRowEventArgs : EventArgs
    {
        public DataGridRowEventArgs(DataGridRow row)
        {
            Row = row;
        }

        public DataGridRow Row 
        { 
            get; private set; 
        }
    }
}
