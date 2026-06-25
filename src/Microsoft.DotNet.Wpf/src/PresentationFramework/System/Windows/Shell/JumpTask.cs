// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



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
    }
}
