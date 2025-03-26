// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This class is used by the StreamGeometry class to generate an inlined,
// flattened geometry stream.
//

namespace System.Windows.Media
{
    /// <summary>
    ///     CapacityStreamGeometryContext
    /// </summary>
    internal abstract class CapacityStreamGeometryContext : StreamGeometryContext
    {
        internal virtual void SetFigureCount(int figureCount) {}
        internal virtual void SetSegmentCount(int segmentCount) {}
    }
}
