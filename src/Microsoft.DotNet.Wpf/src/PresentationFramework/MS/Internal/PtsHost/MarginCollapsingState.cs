// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Provides paragraph level margin collapsing support. 
//


using System;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // MarginCollapsingState class provides paragraph level margin collapsing
    // support.
    //
    // Adjoining vertical margins of two paragraphs collapse. The resulting 
    // margin width is the maximum of the adjoining margin widths. In the case 
    // of negative margins, the absolute maximum of the negative adjoining 
    // margins is deducted from the maximum of the positive adjoining margins. 
    // If there are no positive margins, the absolute maximum of the negative 
    // adjoining margins is deducted from zero.
    // ----------------------------------------------------------------------
    internal sealed class MarginCollapsingState : UnmanagedHandle
    {
        // ------------------------------------------------------------------
        // Create new margin collapsing state and collapse margins if necessary.
        // If no collapsing happens, retrieve margin from old collapsing state.
        // This margin value should be used to advance pen.
        // ------------------------------------------------------------------
        internal static void CollapseTopMargin(
            PtsContext ptsContext,              // Current PTS Context.
            MbpInfo mbp,                        // MBP information for element entering the scope
            MarginCollapsingState mcsCurrent,   // current margin collapsing state (adjacent to the new one).
            out MarginCollapsingState mcsNew,   // margin collapsing state for element entering the scope
            out int margin)                     // collapsed margin value
        {
            margin = 0;
            mcsNew = null;

            // Create new margin collapsing info
            mcsNew = new MarginCollapsingState(ptsContext, mbp.MarginTop);
            // collapse margins, if current margin collapsing exists
            if (mcsCurrent != null)
            {
                mcsNew.Collapse(mcsCurrent);
            }

            // If border or paddind is specified:
            // (1) get collapsed margin value
            // (2) set new mcs to null, because we don't have one anymore
            if (mbp.BPTop != 0)
            {
                margin = mcsNew.Margin;
                mcsNew.Dispose();
                mcsNew = null;
            }
            else if (mcsCurrent == null && DoubleUtil.IsZero(mbp.Margin.Top))
            {
                // No need to create new margin collapsing info
                mcsNew.Dispose();
                mcsNew = null;
            }
        }

        // ------------------------------------------------------------------
        // Update current margin collapsing state and collapse margins if 
        // necessary. If no collapsing happens, retrieve margin from previous 
        // collapsing state. This margin value should be used to advance pen.
        // ------------------------------------------------------------------
        internal static void CollapseBottomMargin(
            PtsContext ptsContext,              // Current PTS Context.
            MbpInfo mbp,                        // MBP information for element leaving the scope
            MarginCollapsingState mcsCurrent,   // current margin collapsing state (adjacent to the new one).
            out MarginCollapsingState mcsNew,   // margin collapsing state for element leaving the scope
            out int margin)                     // collapsed margin value
        {
            margin = 0;
            mcsNew = null;

            // Create new margin collapsing state, if necessary
            if (!DoubleUtil.IsZero(mbp.Margin.Bottom))
            {
                mcsNew = new MarginCollapsingState(ptsContext, mbp.MarginBottom);
            }

            // If the current margin collapsing state does not exist, we are done.
            // Otherwise, get current border and padding and decide if to collapse margin.
            if (mcsCurrent != null)
            {
                if (mbp.BPBottom != 0)
                {
                    // No collapsing happens, get margin value
                    margin = mcsCurrent.Margin;
                }
                else
                {
                    // Collapse margins
                    if (mcsNew == null)
                    {
                        mcsNew = new MarginCollapsingState(ptsContext, 0);
                    }
                    mcsNew.Collapse(mcsCurrent);
                }
            }
        }

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      ptsContext - Current PTS context.
        //      margin - margin value
        // ------------------------------------------------------------------
        internal MarginCollapsingState(PtsContext ptsContext, int margin) : base(ptsContext)
        {
            _maxPositive = (margin >= 0) ? margin : 0;
            _minNegative = (margin <  0) ? margin : 0;
        }

        // ------------------------------------------------------------------
        // Constructor. Make identical copy of the margin collapsing state.
        //
        //      mcs - margin collapsing state to copy
        // ------------------------------------------------------------------
        private MarginCollapsingState(MarginCollapsingState mcs) : base(mcs.PtsContext)
        {
            _maxPositive = mcs._maxPositive;
            _minNegative = mcs._minNegative;
        }

        // ------------------------------------------------------------------
        // Make identical copy of the margin collapsing state.
        //
        // Returns: identical copy of margin collapsing state.
        // ------------------------------------------------------------------
        internal MarginCollapsingState Clone()
        {
            return new MarginCollapsingState(this);
        }

        // ------------------------------------------------------------------
        // Compare margin collapsing state with another one.
        //
        //      mcs - another margin collapsing state
        //
        // Returns: 'true' if both are the same.
        // ------------------------------------------------------------------
        internal bool IsEqual(MarginCollapsingState mcs)
        {
            return (_maxPositive == mcs._maxPositive && _minNegative == mcs._minNegative);
        }

        // ------------------------------------------------------------------
        // The resulting margin width is the maximum of the adjoining margin 
        // widths. In the case of negative margins, the absolute maximum of 
        // the negative adjoining margins is deducted from the maximum of 
        // the positive adjoining margins. If there are no positive margins, 
        // the absolute maximum of the negative adjoining margins is deducted 
        // from zero.
        //
        //      mcs - margin collapsing state to collapse with
        // ------------------------------------------------------------------
        internal void Collapse(MarginCollapsingState mcs)
        {
            _maxPositive = Math.Max(_maxPositive, mcs._maxPositive);
            _minNegative = Math.Min(_minNegative, mcs._minNegative);
        }

        // ------------------------------------------------------------------
        // The resulting margin width is the maximum of the adjoining margin 
        // widths. In the case of negative margins, the absolute maximum of 
        // the negative adjoining margins is deducted from the maximum of 
        // the positive adjoining margins. If there are no positive margins, 
        // the absolute maximum of the negative adjoining margins is deducted 
        // from zero.
        //
        //      margin - margin value to collapse with
        // ------------------------------------------------------------------
        //internal void Collapse(int margin)
        //{
        //    _maxPositive = Math.Max(_maxPositive, margin);
        //    _minNegative = Math.Min(_minNegative, margin);
        //}

        // ------------------------------------------------------------------
        // Actual margin value.
        // ------------------------------------------------------------------
        internal int Margin { get { return _maxPositive + _minNegative; } }

        // ------------------------------------------------------------------
        // Maximum positive and minimum negative value for margin.
        // ------------------------------------------------------------------
        private int _maxPositive;
        private int _minNegative;
    }
}
