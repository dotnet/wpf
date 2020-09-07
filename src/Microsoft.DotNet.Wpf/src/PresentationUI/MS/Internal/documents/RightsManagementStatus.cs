// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    RMStatus enum for overall status of document RM.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Rights Management status for a document.
    /// </summary>
    // RightsManagementResourceHelper.GetDrawingBrushFromStatus relies on these values.  It
    // assumes this to be of type int (default) and 0-indexed (default).
    // Any changes to the type or indexing of this enum will require updates to that code.
    internal enum RightsManagementStatus
    {
        /// <summary>
        /// Document RM status is unknown, this represents the uninitialized value.
        /// </summary>
        Unknown,

        /// <summary>
        /// Document is not RM-protected.
        /// </summary>
        Unprotected,

        /// <summary>
        /// Document is RM-protected.
        /// </summary>
        Protected,
    }
}
