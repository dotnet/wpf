// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Contains information about current state of upate process 
//              in the current container paragraph. 
//


using System;
using System.Diagnostics;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // Contains information about current state of upate process in the 
    // current container paragraph.
    // ----------------------------------------------------------------------
    internal sealed class UpdateRecord
    {
        // ------------------------------------------------------------------
        // Constructor
        // ------------------------------------------------------------------
        internal UpdateRecord()
        {
            Dtr = new DirtyTextRange(0,0,0);
            FirstPara = SyncPara = null;
            ChangeType = PTS.FSKCHANGE.fskchNone;
            Next = null;
            InProcessing = false;
        }

        // ------------------------------------------------------------------
        // Merge with next update record.
        // ------------------------------------------------------------------
        internal void MergeWithNext()
        {
            Debug.Assert(Next != null); // This is the last UR, cannot merge with next.

            // Merge DTRs
            int delta = Next.Dtr.StartIndex - Dtr.StartIndex;

            // Dtr.StartIndex is not changing
            Dtr.PositionsAdded   += delta + Next.Dtr.PositionsAdded;
            Dtr.PositionsRemoved += delta + Next.Dtr.PositionsRemoved;

            // Reasign sync point and next UpdateRecord
            SyncPara = Next.SyncPara;
            Next     = Next.Next;
        }

        // ------------------------------------------------------------------
        // Dirty text range.
        // ------------------------------------------------------------------
        internal DirtyTextRange Dtr;

        // ------------------------------------------------------------------
        // The first paragraph affected by the change.
        // ------------------------------------------------------------------
        internal BaseParagraph FirstPara;

        // ------------------------------------------------------------------
        // The first paragraph not affected by DTR, synchronization point for 
        // update process.
        // ------------------------------------------------------------------
        internal BaseParagraph SyncPara;

        // ------------------------------------------------------------------
        // Type of the change (none, new, inside).
        // ------------------------------------------------------------------
        internal PTS.FSKCHANGE ChangeType;

        // ------------------------------------------------------------------
        // Next UpdateRecord.
        // ------------------------------------------------------------------
        internal UpdateRecord Next;

        // ------------------------------------------------------------------
        // Update record is in processing mode?
        // ------------------------------------------------------------------
        internal bool InProcessing;
    }
}
