// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Definition for DtrLits (Dirty Text Range List) which 
//              contains information about tree changes.
//


using System;
using System.Diagnostics;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // DtrLits (Dirty Text Range List) manages sorted list of accumulated 
    // tree changes.
    // All entries in DtrList are sorted with respect to StartIndex.
    // StartIndex is always representing offset in the tree before any 
    // changes.
    // ----------------------------------------------------------------------
    internal sealed class DtrList
    {
        // ------------------------------------------------------------------
        // Construct a DtrList. The array of DTRs initially can store up to
        // 4 entries. Upon adding elements the capacity increased in multiples 
        // of two as required.
        // ------------------------------------------------------------------
        internal DtrList()
        {
            _dtrs = new DirtyTextRange[_defaultCapacity];
            _count = 0;
        }

        // ------------------------------------------------------------------
        // Merge new DTR with list of exising DTRs:
        // 1) Convert startIndex to index reflecting position before any changes.
        // 2) Merge it with existing list of DTRs.
        //
        //      dtr - New DTR to be merged with exising list of DTRs.
        // ------------------------------------------------------------------
        internal void Merge(DirtyTextRange dtr)
        {
            bool merge = false;
            int i = 0;
            int startIndexOld = dtr.StartIndex;

            // 1) Convert StartIndex to index reflecting position before any changes.
            //    And find out if there is a need to merge DTRs
            if (_count > 0)
            {
                while (i < _count)
                {
                    // a) New DTR starts before the next one. In this case there are
                    //    two possibilities:
                    //    * new DTR does not intersect with the beginning of the next DTR,
                    //      in this case insert new DTR before the next one.
                    //    * new DTR does intersect with the beginning of the next DTR,
                    //      in this case merge these 2 DTRs
                    if (startIndexOld < _dtrs[i].StartIndex)
                    {
                        if (startIndexOld + dtr.PositionsRemoved > _dtrs[i].StartIndex)
                        {
                            merge = true;
                        }
                        // else new dtr has to be inserted at position 'i'
                        break;
                    }
                    // b) New DTR starts in the range of the previous DTR. In this case
                    //    merge these 2 DTRs
                    else if (startIndexOld <= _dtrs[i].StartIndex + _dtrs[i].PositionsAdded)
                    {
                        // merge with existing dtr at position 'i'
                        merge = true;
                        break;
                    }
                    // c) No intersection has been found, go to the next DTR in the list.
                    startIndexOld -= _dtrs[i].PositionsAdded - _dtrs[i].PositionsRemoved;
                    ++i;
                }
                // Update dcp of the new DTR, to reflect position before any tree changes.
                dtr.StartIndex = startIndexOld;
            }

            // 2) Insert new DTR into the list, merge if necessary
            if (i < _count)
            {
                if (merge)
                {
                    // The simplest way to merge these two DTRs is to add together 
                    // cchAdded/cchDeleted form both DTRs, but it will invalidate more
                    // than required. Formula used below is more accurate.

                    // a) New DTR does intersect with the beginning of the next DTR,
                    //    in this case merge these 2 DTRs.
                    // * dcp = dcpN (since it starts before dcpO)
                    // * add = addN + addO - min(addO, delN - (dcpO - dcpN))
                    // * del = delN + delO - min(addO, delN - (dcpO - dcpN))
                    // NOTE: dcpO - dcpN is always <= delN
                    if (dtr.StartIndex < _dtrs[i].StartIndex)
                    {
                        int delta  = _dtrs[i].StartIndex - dtr.StartIndex;
                        int adjust = Math.Min(_dtrs[i].PositionsAdded, dtr.PositionsRemoved - delta);
                        _dtrs[i].StartIndex        = dtr.StartIndex;
                        _dtrs[i].PositionsAdded   += dtr.PositionsAdded   - adjust;
                        _dtrs[i].PositionsRemoved += dtr.PositionsRemoved - adjust;
                    }
                    // b) New DTR starts in the range of the previous DTR. In this case
                    //    merge these 2 DTRs.
                    // * dcp = dcpO (since it starts before dcpN)
                    // * add = addN + addO - min(delN, addO - (dcpN - dcpO))
                    // * del = delN + delO - min(delN, addO - (dcpN - dcpO))
                    // NOTE: dcpN - dcpO is always <= addO
                    else
                    {
                        int delta  = dtr.StartIndex - _dtrs[i].StartIndex;
                        int adjust = Math.Min(dtr.PositionsRemoved, _dtrs[i].PositionsAdded - delta);
                        //_dtrs[i].dcp: no need to change it
                        _dtrs[i].PositionsAdded   += dtr.PositionsAdded   - adjust;
                        _dtrs[i].PositionsRemoved += dtr.PositionsRemoved - adjust;
                    }

                    // Prefer not highlight layer change when merging
                    _dtrs[i].FromHighlightLayer &= dtr.FromHighlightLayer;
                }
                else
                {
                    // The new DTR has to be inserted before DTR at position 'i'.
                    if (_count == _dtrs.Length) { Resize(); }
                    Array.Copy(_dtrs, i, _dtrs, i+1, _count-i);
                    _dtrs[i] = dtr;
                    ++_count;
                }
                MergeWithNext(i);
            }
            else
            {
                // The new DTR has to be appended to the end of the list.
                if (_count == _dtrs.Length) { Resize(); }
                _dtrs[_count] = dtr;
                ++_count;
            }

#if TEXTPANELLAYOUTDEBUG
            System.Text.StringBuilder msg = new System.Text.StringBuilder();
            msg.Append("Merge DTR (" + dtr.StartIndex + "," + dtr.PositionsAdded + "," + dtr.PositionsRemoved + ") ->");
            for (i = 0; i < _count; i++)
            {
                msg.Append(" (" + _dtrs[i].StartIndex + "," + _dtrs[i].PositionsAdded + "," + _dtrs[i].PositionsRemoved + ")");
            }
            TextPanelDebug.Log(msg.ToString(), TextPanelDebug.Category.ContentChange);
#endif
        }

        /// <summary>
        /// Merges the DtrList into a single DirtyTextRange containing all
        /// ranges in the list.
        /// </summary>
        /// <returns>A DirtyTextRange containing all ranges in this list</returns>
        internal DirtyTextRange GetMergedRange()
        {
            if (_count > 0)
            {
                DirtyTextRange range = _dtrs[0];

                int previousOffset = range.StartIndex;
                int positionsAdded = range.PositionsAdded;
                int positionsRemoved = range.PositionsRemoved;
                bool fromHighlightLayer = range.FromHighlightLayer;

                for (int i = 1; i < _count; i++)
                {
                    range = _dtrs[i];

                    int rangeDistance = range.StartIndex - previousOffset;
                    positionsAdded = rangeDistance + range.PositionsAdded;
                    positionsRemoved = rangeDistance + range.PositionsRemoved;

                    // Prefer not from highlight layer when squashing
                    fromHighlightLayer &= range.FromHighlightLayer;

                    previousOffset = range.StartIndex;
                }

                return new DirtyTextRange(_dtrs[0].StartIndex, positionsAdded, positionsRemoved, fromHighlightLayer);
            }

            return new DirtyTextRange(0, 0, 0, false);
        }

        // ------------------------------------------------------------------
        // Retrieve list of dtrs from range.
        //
        //      dcpNew - Distance from the beginning of TextContainer after all
        //              tree changes.
        //      cchOld - Number of characters in the range, but before any 
        //              tree changes.
        //
        // Returns: List of DRTs for specified range.
        // ------------------------------------------------------------------
        internal DtrList DtrsFromRange(int dcpNew, int cchOld)
        {
            DtrList dtrs = null;
            int i = 0;
            int first, last;
            int positionsAdded = 0;

            // Find the first dtr intersecting with the specified range.
            // Since DTRs store dcp before any changes, during iteration
            // accumulate positionsAdded (number of characters added to the tree
            // up to the current point).
            while (i < _count)
            {
                if (dcpNew <= _dtrs[i].StartIndex + positionsAdded + _dtrs[i].PositionsAdded)
                {
                    break;
                }
                positionsAdded += _dtrs[i].PositionsAdded - _dtrs[i].PositionsRemoved;
                ++i;
            }
            first = i;

            // Find the last dtr intersecting with the specified range.
            // dcpNew-positionsAdded points to position before any tree changes, from
            // where we start counting cchOld.
            // Do not add characters (positionsAdded), since start position has been already found.
            while (i < _count)
            {
                if (dcpNew - positionsAdded + cchOld <= _dtrs[i].StartIndex + _dtrs[i].PositionsRemoved)
                {
                    // If there is no intersection with the current DTR, go to the previous one.
                    if (dcpNew - positionsAdded + cchOld < _dtrs[i].StartIndex)
                    {
                        --i;
                    }
                    break;
                }
                ++i;
            }
            last = (i < _count) ? i : _count-1;

            // If there are DTRs in the specified range, create new DtrList object
            if (last >= first)
            {
                dtrs = new DtrList();
                while (last >= first)
                {
                    // Since dcpNew is after tree changes, add positionsAdded to all dtrs
                    // to build dtr list relative to dcpNew position.
                    DirtyTextRange dtr = _dtrs[first];
                    dtr.StartIndex += positionsAdded;
                    dtrs.Append(dtr);
                    ++first;
                }
            }
            return dtrs;
        }

        // ------------------------------------------------------------------
        // Merge DRT at position 'index' with the next one, if possible.
        //
        //      index - Index of the DTR to be merged with next one.
        // ------------------------------------------------------------------
        private void MergeWithNext(int index)
        {
            while (index + 1 < _count)
            {
                DirtyTextRange dtrNext = _dtrs[index+1];

                // DTR starts in the range of the previous DTR. In this case
                // merge these 2 DTRs.
                if (dtrNext.StartIndex <= _dtrs[index].StartIndex + _dtrs[index].PositionsRemoved)
                {
                    //_dtrs[index].dcp: no need to change it
                    _dtrs[index].PositionsAdded += dtrNext.PositionsAdded;
                    _dtrs[index].PositionsRemoved += dtrNext.PositionsRemoved;

                    // Prefer not highlight layer change when merging
                    _dtrs[index].FromHighlightLayer &= dtrNext.FromHighlightLayer;

                    // Remove merged entry
                    for (int i = index + 2; i < _count; i++)
                    {
                        _dtrs[i - 1] = _dtrs[i];
                    }

                    --_count;
                }
                else
                {
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Append new DTR to the list.
        //
        //      dtr - DTR to be appended to the list.
        // ------------------------------------------------------------------
        private void Append(DirtyTextRange dtr)
        {
            if (_count == _dtrs.Length) { Resize(); }
            _dtrs[_count] = dtr;
            ++_count;
        }

        // ------------------------------------------------------------------
        // Increases the capacity of the DTR array.
        // <new size> = <current size>*2.
        // ------------------------------------------------------------------
        private void Resize()
        {
            Debug.Assert(_dtrs.Length > 0);

            // Allocate new array and copy all existing entries into it
            DirtyTextRange [] newdtrs = new DirtyTextRange[_dtrs.Length * 2];
            Array.Copy(_dtrs, newdtrs, _dtrs.Length);
            _dtrs = newdtrs;
        }

        // ------------------------------------------------------------------
        // Array like access to list of DTRs.
        // ------------------------------------------------------------------
        internal int Length 
        { 
            get { return _count; } 
        }
        internal DirtyTextRange this[int index]
        {
            get { return _dtrs[index]; }
        }

        // ------------------------------------------------------------------
        // Array of DTRs. This array stores sorted list of DTRs.
        // ------------------------------------------------------------------
        private DirtyTextRange [] _dtrs;

        // ------------------------------------------------------------------
        // Default capacity of the DTRs array. The array capacity is always
        // increased in multiples of two as required: 4*(2^N).
        // ------------------------------------------------------------------
        private const int _defaultCapacity = 4;
        private int _count;
    }
}
