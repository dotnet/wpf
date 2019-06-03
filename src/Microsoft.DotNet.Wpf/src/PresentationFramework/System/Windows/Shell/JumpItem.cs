// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace System.Windows.Shell
{
    public abstract class JumpItem
    {
        // This class is just provided to strongly type the JumpList's contents.
        // It's not externally extendable.
        internal JumpItem()
        {
        }

        public string CustomCategory { get; set; }
    }
}
