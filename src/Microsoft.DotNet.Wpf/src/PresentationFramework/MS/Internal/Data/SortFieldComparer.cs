// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: IComparer class to sort by class property value (using reflection).
//
// See spec at IDataCollection.mht
//

using System;
using System.ComponentModel;

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using MS.Utility;
using System.Windows;

namespace MS.Internal.Data
{
    /// <summary>
    /// IComparer class to sort by class property value (using reflection).
    /// </summary>
    internal class SortFieldComparer : IComparer
    {
        /// <summary>
        /// Create a comparer, using the SortDescription and a Type;
        /// tries to find a reflection PropertyInfo for each property name
        /// </summary>
        /// <param name="sortFields">list of property names and direction to sort by</param>
        /// <param name="culture">culture to use for comparisons</param>
        internal SortFieldComparer(SortDescriptionCollection sortFields, CultureInfo culture)
        {
            _sortFields = sortFields;
            _fields = CreatePropertyInfo(_sortFields);

            // create the comparer
            _comparer = (culture == null || culture == CultureInfo.InvariantCulture) ? Comparer.DefaultInvariant
                        : (culture == CultureInfo.CurrentCulture) ? Comparer.Default
                        : new Comparer(culture);
        }

        internal IComparer BaseComparer { get { return _comparer; } }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to or greater than the other.
        /// </summary>
        /// <param name="o1">first item to compare</param>
        /// <param name="o2">second item to compare</param>
        /// <returns>; &lt;0: o1 &lt; o2; =0: o1 == o2; &gt; 0: o1 &gt; o2</returns>
        /// <remarks>
        /// Compares the 2 items using the list of property names and directions.
        /// </remarks>
        public int Compare(object o1, object o2)
        {
            int result = 0;

            // compare both objects by each of the properties until property values don't match
            for (int k = 0; k < _fields.Length; ++k)
            {
                object v1 = _fields[k].GetValue(o1);
                object v2 = _fields[k].GetValue(o2);

                result = _comparer.Compare(v1, v2);
                if (_fields[k].descending)
                    result = -result;

                if (result != 0)
                    break;
            }

            return result;
        }

        // Helper method for sorting an ArrayList.  If the comparer is a SortFieldComparer,
        // use the cached-value approach to avoid excessive reflection.  For other
        // comparers, sort the usual way
        internal static void SortHelper(ArrayList al, IComparer comparer)
        {
            SortFieldComparer sfc = comparer as SortFieldComparer;
            if (sfc == null)
            {
                // sort the usual way
                al.Sort(comparer);
            }
            else
            {
                // Sort with cached values.
                // Step 1.  Copy the items into a list augmented with slots for
                // the cached values.
                int n = al.Count;
                int nFields = sfc._fields.Length;
                CachedValueItem[] list = new CachedValueItem[n];
                for (int i = 0; i < n; ++i)
                {
                    list[i].Initialize(al[i], nFields);
                }

                // Step 2. Sort the augmented list.  The SortFieldComparer will
                // fill in the slots as necessary to perform its comparisons.
                Array.Sort(list, sfc);

                // Step 3. Copy the items back into the original list, now in
                // sorted order
                for (int i = 0; i < n; ++i)
                {
                    al[i] = list[i].OriginalItem;
                }
            }
        }

        // Private Methods
        private SortPropertyInfo[] CreatePropertyInfo(SortDescriptionCollection sortFields)
        {
            SortPropertyInfo[] fields = new SortPropertyInfo[sortFields.Count];
            for (int k = 0; k < sortFields.Count; ++k)
            {
                PropertyPath pp;
                if (String.IsNullOrEmpty(sortFields[k].PropertyName))
                {
                    // sort by the object itself (as opposed to a property)
                    pp = null;
                }
                else
                {
                    // sort by the value of a property path, to be applied to
                    // the items in the list
                    pp = new PropertyPath(sortFields[k].PropertyName);
                }

                // remember PropertyPath and direction, used when actually sorting
                fields[k].index = k;
                fields[k].info = pp;
                fields[k].descending = (sortFields[k].Direction == ListSortDirection.Descending);
            }
            return fields;
        }

        // private types
        struct SortPropertyInfo
        {
            internal int index;
            internal PropertyPath info;
            internal bool descending;

            internal object GetValue(object o)
            {
                if (o is CachedValueItem)
                {
                    return GetValueFromCVI((CachedValueItem)o);
                }
                else
                {
                    return GetValueCore(o);
                }
            }

            object GetValueFromCVI(CachedValueItem cvi)
            {
                object value = cvi[index];

                if (value == DependencyProperty.UnsetValue)
                {
                    // first query for this value.  Compute it and cache it.
                    value = cvi[index] = GetValueCore(cvi.OriginalItem);
                }

                return value;
            }

            object GetValueCore(object o)
            {
                object value;
                if (info == null)
                {
                    value = o;
                }
                else
                {
                    using (info.SetContext(o))
                    {
                        value = info.GetValue();
                    }
                }

                // comparers can deal with null, but not with UnsetValue or null-variants
                if (value == DependencyProperty.UnsetValue ||
                    System.Windows.Data.BindingExpressionBase.IsNullValue(value))
                {
                    value = null;
                }

                return value;
            }
        }

        struct CachedValueItem
        {
            public object OriginalItem
            {
                get { return _item; }
            }

            public void Initialize(object item, int nFields)
            {
                _item = item;
                _values = new object[nFields];
                _values[0] = DependencyProperty.UnsetValue; // sentinel - unknown value
            }

            public object this[int index]
            {
                get { return _values[index]; }
                set
                {
                    _values[index] = value;

                    // set a sentinel into the next field, so we compute it on demand
                    if (++index < _values.Length)
                    {
                        _values[index] = DependencyProperty.UnsetValue;
                    }
                }
            }

            private object _item;      // the underlying item
            private object[] _values;   // the cached values for each sort field
        }

        // Private Fields
        SortPropertyInfo[] _fields;
        SortDescriptionCollection _sortFields;
        Comparer _comparer;
    }
}

