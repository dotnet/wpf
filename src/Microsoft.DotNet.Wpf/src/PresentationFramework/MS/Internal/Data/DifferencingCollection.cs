// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: ObservableCollection that updates by differencing against
//          an IEnumerable
//

/***************************************************************************\
    This class maps an IEnumerable into an ObservableCollection.  The idea is
    to initialize the collection with the items in the enumerable.  When the
    user of the class has reason to believe the enumerable has changed, he
    should call Update.  This re-enumerates the enumerable and brings the
    collection up to sync, and also raises appropriate collection-change
    notifications.  A differencing algorithm detects simple changes, such
    as adding/removing a single item, moving a single item, or replacing an
    item, and raises the corresponding Add, Remove, Move or Replace event.
    All other changes result in a Reset event.

    The constructor and the Update method have an IEnumerable parameter.  It's not
    required that these be the same - you can Update against a different IEnumerable
    than the constructor used.  This makes sense if they enumerate the same
    items (up to simple changes).

    This class is used to support XLinq's Elements and Descendants properties, both
    of which return IEnumerables that don't raise collection-change events.  XLinq
    returns different IEnumerables every time you fetch the property value, so the
    flexibility of the previous paragraph comes into play.
\***************************************************************************/

using System;
using System.Collections;               // IEnumerable
using System.Collections.Generic;       // IList<T>
using System.Collections.ObjectModel;   // ObservableCollection
using System.Collections.Specialized;   // INotifyCollectionChanged
using System.ComponentModel;            // PropertyChangedEventArgs
using MS.Internal;                      // Invariant.Assert

namespace MS.Internal.Data
{
    internal sealed class DifferencingCollection : ObservableCollection<object>
    {
        internal DifferencingCollection(IEnumerable enumerable)
        {
            LoadItems(enumerable);
        }

        internal void Update(IEnumerable enumerable)
        {
            IList<object> list = Items;
            int index1 = -1, index2 = -1;
            int n = list.Count;
            Change change = Change.None;
            int index = 0;
            object target = Unset;

            // determine what kind of change occurred

            // first match each item in the enumerator against the cached list
            foreach (object o in enumerable)
            {
                // skip over matching items
                if (index < n && System.Windows.Controls.ItemsControl.EqualsEx(o, list[index]))
                {
                    ++index;
                    continue;
                }

                // mismatch - what happens next depends on previous history
                switch (change)
                {
                    case Change.None:   // this is the first mismatch
                        if (index + 1 < n && System.Windows.Controls.ItemsControl.EqualsEx(o, list[index + 1]))
                        {
                            // enumerator matches the next list item,
                            // provisionally mark this as Remove (might be Move)
                            change = Change.Remove;
                            index1 = index;
                            target = list[index];
                            index = index + 2;
                        }
                        else
                        {
                            // enumerator doesn't match next list item,
                            // provisionally mark this as Add (might be Move or Replace)
                            change = Change.Add;
                            index1 = index;
                            target = o;
                        }
                        break;

                    case Change.Add:    // previous mismatch was provisionally Add
                        if (index + 1 < n && System.Windows.Controls.ItemsControl.EqualsEx(o, list[index + 1]))
                        {
                            // enumerator matches next list item;  check current
                            // list item

                            if (System.Windows.Controls.ItemsControl.EqualsEx(target, list[index]))
                            {
                                // current matches "added" element from enumerator,
                                // change this to Move
                                change = Change.Move;
                                index2 = index1;
                                index1 = index;
                            }
                            else if (index < n && index == index1)
                            {
                                // current item was replaced
                                change = Change.Replace;
                            }
                            else
                            {
                                // two mismatches, not part of a known pattern
                                change = Change.Reset;
                            }

                            index = index + 2;
                        }
                        else
                        {
                            // enumerator doesn't match next list item;  no pattern
                            change = Change.Reset;
                        }
                        break;

                    case Change.Remove: // previous mismatch was provisionally Remove
                        if (System.Windows.Controls.ItemsControl.EqualsEx(o, target))
                        {
                            // enumerator matches "removed" item from list;
                            // change this to Move
                            change = Change.Move;
                            index2 = index - 1;
                        }
                        else
                        {
                            // enumerator does not match "removed" item;  no pattern
                            change = Change.Reset;
                        }
                        break;

                    default:    // any other previous mismatch implies no pattern
                        change = Change.Reset;
                        break;
                }

                // once we eliminate known patterns, no reason to keep looking
                if (change == Change.Reset)
                    break;
            }

            // Next, account for any leftover items in the list (if any)
            if (index == n - 1)
            {
                // exactly one leftover item - possibly part of a simple pattern
                switch (change)
                {
                    case Change.None:       // no previous change - last item was removed
                        change = Change.Remove;
                        index1 = index;
                        break;

                    case Change.Add:        // provisional Add, might be Move or Replace
                        if (System.Windows.Controls.ItemsControl.EqualsEx(target, list[index]))
                        {
                            // a single extra item matches the "added" item;  change this to Move
                            change = Change.Move;
                            index2 = index1;
                            index1 = index;
                        }
                        else if (index1 == n - 1)
                        {
                            // a single extra item mismatches the last item;  change this to Replace
                            change = Change.Replace;
                        }
                        else
                        {
                            // anything else means no pattern
                            change = Change.Reset;
                        }
                        break;

                    default:                // anything else means no pattern
                        change = Change.Reset;
                        break;
                }
            }
            else if (index != n)
            {
                // two or more leftover items - no pattern
                change = Change.Reset;
            }

            // Finally, make the appropriate change to the list
            switch (change)
            {
                case Change.None:
                    break;

                case Change.Add:
                    Invariant.Assert(target != Unset);
                    Insert(index1, target);
                    break;

                case Change.Remove:
                    RemoveAt(index1);
                    break;

                case Change.Move:
                    Move(index1, index2);
                    break;

                case Change.Replace:
                    Invariant.Assert(target != Unset);
                    this[index1] = target;
                    break;

                case Change.Reset:
                    Reload(enumerable);
                    break;
            }
        }

        void LoadItems(IEnumerable enumerable)
        {
            foreach (object o in enumerable)
            {
                Items.Add(o);
            }
        }

        // reload the list from the given enumerable, raising required events
        void Reload(IEnumerable enumerable)
        {
            Items.Clear();
            LoadItems(enumerable);

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs(System.Windows.Data.Binding.IndexerName));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        enum Change { None, Add, Remove, Move, Replace, Reset }

        static object Unset = new Object();
    }
}
