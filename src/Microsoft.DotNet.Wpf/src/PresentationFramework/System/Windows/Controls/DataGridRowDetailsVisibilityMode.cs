// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace System.Windows.Controls
{
    public enum DataGridRowDetailsVisibilityMode
    {
        Collapsed,              // Show no details by default. Developer must toggle visibility
        Visible,                // Show the details section for all rows
        VisibleWhenSelected     // Show the details section only for the selected row(s)
    }
}
