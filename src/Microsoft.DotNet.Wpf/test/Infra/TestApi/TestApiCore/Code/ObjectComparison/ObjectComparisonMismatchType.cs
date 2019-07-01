// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ObjectComparison
{
    /// <summary>
    /// Represents the type of mismatch.
    /// </summary>
    public enum ObjectComparisonMismatchType
    {
        /// <summary>
        /// The node is missing in the right graph.
        /// </summary>
        MissingRightNode = 0,

        /// <summary>
        /// The node is missing in the left graph.
        /// </summary>
        MissingLeftNode = 1,

        /// <summary>
        /// The right node has fewer children than the left node.
        /// </summary>
        RightNodeHasFewerChildren = 2,

        /// <summary>
        /// The left node has fewer children than the right node.
        /// </summary>
        LeftNodeHasFewerChildren = 3,

        /// <summary>
        /// The node types do not match.
        /// </summary>
        ObjectTypesDoNotMatch = 4,

        /// <summary>
        /// The node values do not match.
        /// </summary>
        ObjectValuesDoNotMatch = 5
    }
}
