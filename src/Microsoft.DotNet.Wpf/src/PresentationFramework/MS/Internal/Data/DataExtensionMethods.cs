// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Extension methods used by Data Services code
//

using System;
using System.Collections;
using System.Diagnostics;

using System.Windows.Data;

namespace MS.Internal.Data
{
    internal static class DataExtensionMethods
    {
        // Search for value in the slice of the list starting at index with length count,
        // using the given comparer.  The list is assumed to be sorted w.r.t. the
        // comparer.  Return the index if found, or the bit-complement
        // of the index where it would belong.
        internal static int Search(this IList list, int index, int count, object value, IComparer comparer)
        {
            ArrayList al;
            LiveShapingList lsList;

            if ((al = list as ArrayList) != null)
            {
                return al.BinarySearch(index, count, value, comparer);
            }
            else if ((lsList = list as LiveShapingList) != null)
            {
                return lsList.Search(index, count, value);
            }

            // we should never get here, but the compiler doesn't know that
            Debug.Assert(false, "Unsupported list passed to Search");
            return 0;
        }

        // convenience method for search
        internal static int Search(this IList list, object value, IComparer comparer)
        {
            return list.Search(0, list.Count, value, comparer);
        }


        // Move an item from one position to another
        internal static void Move(this IList list, int oldIndex, int newIndex)
        {
            ArrayList al;
            LiveShapingList lsList;

            if ((al = list as ArrayList) != null)
            {
                object item = al[oldIndex];
                al.RemoveAt(oldIndex);
                al.Insert(newIndex, item);
            }
            else if ((lsList = list as LiveShapingList) != null)
            {
                lsList.Move(oldIndex, newIndex);
            }
        }


        // Sort the list, according to the comparer
        internal static void Sort(this IList list, IComparer comparer)
        {
            ArrayList al;
            LiveShapingList lsList;

            if ((al = list as ArrayList) != null)
            {
                SortFieldComparer.SortHelper(al, comparer);
            }
            else if ((lsList = list as LiveShapingList) != null)
            {
                lsList.Sort();
            }
        }
    }
}
