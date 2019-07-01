// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Test
{
    /// <summary>
    /// Specifies the category of a test
    /// </summary>
    // Both suppressions are due to poor names, but changing would be a breaking change.
    public enum TestType
    {
        /// <summary/>
        None,
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1709")]
        DRT,
        /// <summary/>
        Functional,
        /// <summary/>
        [SuppressMessage("Microsoft.Naming", "CA1704")]
        Perf,
        /// <summary/>
        Stress,
        /// <summary/>
        Leak
    }
}