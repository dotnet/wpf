// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: 
//    SignatureStatus enum for status of applied signatures.
using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Signatures status for the document.
    /// </summary>
    // SignatureResourceHelper.GetDrawingBrushFromStatus relies on these values.  It
    // assumes this to be of type int (default) and 0-indexed (default).
    // Any changes to the type or indexing of this enum will require updates to that code.
    internal enum SignatureStatus
    {
        /// <summary>
        /// Signature status is unknown, this represents the uninitialized value.
        /// </summary>
        Unknown,
        /// <summary>
        /// Signature status is undetermined.
        /// </summary>
        Undetermined,
        /// <summary>
        /// Signature status is invalid.
        /// </summary>
        Invalid,
        /// <summary>
        /// Signature is unable to be evaluated because the document does not meet
        /// the signing criteria.
        /// </summary>
        Unverifiable,
        /// <summary>
        /// Signature status is valid.
        /// </summary>
        Valid,
        /// <summary>
        /// Signatures are not applied.
        /// </summary>
        NotSigned
    }
}
