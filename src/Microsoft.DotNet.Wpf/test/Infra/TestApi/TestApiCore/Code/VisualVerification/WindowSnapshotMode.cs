// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.VisualVerification
{
    /// <summary>
    /// WindowSnapshotMode determines if window border should be captured as part of Snapshot.
    /// </summary>
    public enum WindowSnapshotMode
    {
        /// <summary>
        /// Capture a snapshot of only the window client area. This mode excludes the window border.
        /// </summary>
        ExcludeWindowBorder = 0,
        /// <summary>
        /// Capture a snapshot of the entire window area. This mode includes the window border. 
        /// </summary>
        IncludeWindowBorder = 1
    }

}