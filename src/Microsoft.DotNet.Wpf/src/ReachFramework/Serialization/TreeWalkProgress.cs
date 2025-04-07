// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

    Abstract:
        This file implements the TreeWalkProgress
        used by the Xps Serialization APIs for tracking cycles in a visual tree.
--*/
using System.Windows.Media;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class is used by the Xps Serialization APIs for tracking cycles in a visual tree.
    /// </summary>
    internal sealed class TreeWalkProgress
    {
        private readonly HashSet<ICyclicBrush> _cyclicBrushes = new();

        public bool EnterTreeWalk(ICyclicBrush brush)
        {
            return _cyclicBrushes.Add(brush);
        }

        public void ExitTreeWalk(ICyclicBrush brush)
        {
            _cyclicBrushes.Remove(brush);
        }

        public bool IsTreeWalkInProgress(ICyclicBrush brush)
        {
            return _cyclicBrushes.Contains(brush);
        }
    }
}
