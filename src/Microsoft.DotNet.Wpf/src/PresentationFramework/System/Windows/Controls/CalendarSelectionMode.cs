// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies values for the different selection modes of a Calendar. 
    /// </summary>
    public enum CalendarSelectionMode
    {
        /// <summary>
        /// One date can be selected at a time.
        /// </summary>
        SingleDate = 0,
        
        /// <summary>
        /// One range of dates can be selected at a time.
        /// </summary>
        SingleRange = 1,
        
        /// <summary>
        /// Multiple dates or ranges can be selected at a time.
        /// </summary>
        MultipleRange = 2,
        
        /// <summary>
        /// No dates can be selected.
        /// </summary>
        None = 3,
    }
}
