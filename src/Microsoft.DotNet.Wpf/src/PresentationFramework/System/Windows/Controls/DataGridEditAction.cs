// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;

namespace System.Windows.Controls
{
    /// <summary>
    ///     Used to specify action to take out of edit mode.
    /// </summary>
    public enum DataGridEditAction
    {
        /// <summary>
        ///     Cancel the changes.
        /// </summary>
        Cancel,

        /// <summary>
        ///     Commit edited value.
        /// </summary>
        Commit
    }
}