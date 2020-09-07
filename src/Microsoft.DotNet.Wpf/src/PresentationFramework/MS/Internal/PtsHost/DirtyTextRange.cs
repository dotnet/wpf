// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Dirty text range describes change in the TextContainer. 
//

using System;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Dirty text range describes change in the TextContainer.
    // ----------------------------------------------------------------------
    internal struct DirtyTextRange
    {
        // ------------------------------------------------------------------
        // Constructor
        //
        //      startIndex - Index of the starting position of the change.
        //      positionsAdded - Number of characters added.
        //      positionsRemoved - Number of characters removed.
        // ------------------------------------------------------------------
        internal DirtyTextRange(int startIndex, int positionsAdded, int positionsRemoved, bool fromHighlightLayer = false)
        {
            StartIndex = startIndex;
            PositionsAdded = positionsAdded;
            PositionsRemoved = positionsRemoved;
            FromHighlightLayer = fromHighlightLayer;
    }

        // ------------------------------------------------------------------
        // Constructor
        //
        //      change - TextContainer change data.
        // ------------------------------------------------------------------
        internal DirtyTextRange(TextContainerChangeEventArgs change)
        {
            StartIndex = change.ITextPosition.Offset;

            PositionsAdded = 0;
            PositionsRemoved = 0;
            FromHighlightLayer = false;

            switch (change.TextChange)
            {
                case TextChangeType.ContentAdded:
                    PositionsAdded = change.Count;
                    break;

                case TextChangeType.ContentRemoved:
                    PositionsRemoved = change.Count;
                    break;

                case TextChangeType.PropertyModified:
                    PositionsAdded = change.Count;
                    PositionsRemoved = change.Count;
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Index of the starting position of the change.
        // ------------------------------------------------------------------
        internal int StartIndex { get; set; }

        // ------------------------------------------------------------------
        // Number of characters added.
        // ------------------------------------------------------------------
        internal int PositionsAdded { get; set; }

        // ------------------------------------------------------------------
        // Number of characters removed.
        // ------------------------------------------------------------------
        internal int PositionsRemoved { get; set; }

        /// <summary>
        ///
        /// If this dirty text range is caused by a highlight layer change.
        /// </summary>
        internal bool FromHighlightLayer { get; set; }
    }
}
