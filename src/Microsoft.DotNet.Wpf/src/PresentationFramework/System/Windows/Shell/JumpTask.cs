// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿

namespace System.Windows.Shell
{
    public class JumpTask : JumpItem
    {
        public JumpTask() : base()
        {}

        public string Title { get; set; }

        public string Description { get; set; }

        public string ApplicationPath { get; set; }

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public string IconResourcePath { get; set; }

        public int IconResourceIndex { get; set; }

        /// <summary>
        /// Shell link data flags to enable. For more details see the SHELL_LINK_DATA_FLAGS documentation
        /// https://docs.microsoft.com/windows/desktop/api/shlobj_core/ne-shlobj_core-shell_link_data_flags 
        /// </summary>
        public UInt32[] FlagsToEnable { get; set; }
    }
}
