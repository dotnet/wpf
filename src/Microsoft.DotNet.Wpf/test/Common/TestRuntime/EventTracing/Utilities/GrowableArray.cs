// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// A cheap version of List(T). The idea is to make it as cheap as if you did it 'by hand' using an array and
    /// a int which represents the logical charCount. It is a struct to avoid an extra pointer dereference, so this
    /// is really meant to be embeded in other structures.
    /// 
    /// Also made the Binary search is actually useful (by allowing the key to be something besides the element
    /// itself).
    /// </summary>
    public struct GrowableArray<T>
    {
        public GrowableArray(int initialSize)
        {
            array = new T[initialSize];
            arrayLength = 0;
        }
        public T this[int index]
        {
            get
            {
                Debug.Assert((uint)index < (uint)arrayLength);
                return array[index];
            }
            set
            {
                Debug.Assert((uint)index < (uint)arrayLength);
                array[index] = value;
            }
        }
        public int Count
        {
            get
            {
                return arrayLength;
            }
            set
            {
                if (value > arrayLength)
                {
                    if (value <= array.Length)
                    {
                        // Null out the entries.  
                        for (int i = arrayLength; i < value; i++)
                            array[i] = default(T);
                    }
                    else
                    {
                        T[] newArray = new T[value];
                        Array.Copy(array, newArray, array.Length);
                        array = newArray;
                    }
                }
                arrayLength = value;
            }
        }
        /// <summary>
        /// Add an item at the end of the array, growing as necessary. 
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (array == null || arrayLength >= array.Length)
                Realloc();
            array[arrayLength++] = item;
        }
        /// <summary>
        /// Insert 'item' directly at 'index', shifting all items >= index up.  'index' can be code:Count in
        /// which case the item is appended to the end.  Larger indexes are not allowed. 
        /// </summary>
        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)arrayLength)
                throw new IndexOutOfRangeException();
            if (array == null || arrayLength >= array.Length)
                Realloc();

            // Shift everything up to make room. 
            for (int idx = arrayLength; index < idx; --idx)
                array[idx] = array[idx - 1];

            // insert the element
            array[index] = item;
            arrayLength++;
        }
        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;
            if (count < 0)
                throw new ArgumentException("count can't be negative");

            if ((uint)index >= (uint)arrayLength)
                throw new IndexOutOfRangeException();
            Debug.Assert(index + count <= arrayLength);     // If you violate this it does not hurt

            // Shift everything down. 
            for (int endIndex = index + count; endIndex < arrayLength; endIndex++)
                array[index++] = array[endIndex];

            arrayLength = index;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("GrowableArray(Count=").Append(Count).Append(", [").AppendLine();
            for (int i = 0; i < Count; i++)
                sb.Append("  ").Append(this[i].ToString()).AppendLine();
            sb.Append("  ])");
            return sb.ToString();
        }
        public delegate int Comparison<Key>(Key x, T elem);
        /// <summary>
        /// Sets 'index' to the the smallest index such that all elements with index > 'idx' are > key.  If
        /// index does not match any elements a new element should always be placed AFTER index.  Note that this
        /// means that index may be -1 if the new element belongs in the first position.  
        /// 
        /// return true if the return index matched exactly (success)
        /// </summary>
        public bool BinarySearch<Key>(Key key, out int index, Comparison<Key> comparison)
        {
            // binary search 
            int low = 0;
            int high = arrayLength;
            int lastLowCompare = -1;                // If this number == 0 we had a match. 

            if (high > 0)
            {
                // The invarient in this loop is that 
                //     [0..low) <= key < [high..Count)
                for (; ; )
                {
                    int mid = (low + high) / 2;
                    int compareResult = comparison(key, array[mid]);
                    if (compareResult >= 0)             // key >= array[mid], move low up
                    {
                        lastLowCompare = compareResult; // remember this result, as it indicates a sucessful match. 
                        if (mid == low)
                            break;
                        low = mid;
                    }
                    else                                // key < array[mid], move high down 
                    {
                        high = mid;
                        if (mid == low)
                            break;
                    }

                    // Note that if compareResults == 0, we don't return the match eagerly because there could be
                    // multiple elements that match. We want the match with the largest possible index, so we need
                    // to continue the search until the valid range drops to 0
                }
            }

            if (lastLowCompare < 0)            // key < array[low], subtract 1 to indicate that new element goes BEFORE low. 
            {
                Debug.Assert(low == 0);         // can only happen if it is the first element
                --low;
            }
            index = low;

            Debug.Assert(index == -1 || comparison(key, array[index]) >= 0);                 // element smaller or equal to key            
            Debug.Assert(index + 1 >= Count || comparison(key, array[index + 1]) < 0);       // The next element is strictly bigger.
            Debug.Assert((lastLowCompare != 0) || (comparison(key, array[index]) == 0));     // If we say there is a match, there is. 
            return (lastLowCompare == 0);
        }
        public void Sort(int index, int length, System.Comparison<T> comparison)
        {
            Debug.Assert(index + length <= arrayLength);
            Array.Sort<T>(array, index, length, new FunctorComparer<T>(comparison));
        }
        public void Sort(System.Comparison<T> comparison)
        {
            Array.Sort<T>(array, 0, arrayLength, new FunctorComparer<T>(comparison));
        }

        /// <summary>
        /// Perform a linear search starting at 'startIndex'.  If found return true and the index in 'index'.
        /// It is legal that 'startIndex' is greater than the charCount, in which case, the search returns false
        /// immediately.   This allows a nice loop to find all items matching a pattern. 
        /// </summary>
        public bool Search<Key>(Key key, int startIndex, Comparison<Key> compare, ref int index)
        {
            for (int i = startIndex; i < arrayLength; i++)
            {
                if (compare(key, array[i]) == 0)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }
        #region private
        private void Realloc()
        {
            if (array == null)
            {
                array = new T[16];
            }
            else
            {
                int newLength = array.Length * 3 / 2 + 8;
                T[] newArray = new T[newLength];
                Array.Copy(array, newArray, array.Length);
                array = newArray;
            }
        }

        T[] array;
        int arrayLength;
        #endregion

        #region TESTING
        // Unit testing.  It is reasonable coverage, but concentrates on BinarySearch as that is the one that is
        // easy to get wrong.  
#if TESTING
   public static void TestGrowableArray()
    {
        GrowableArray<float> testArray = new GrowableArray<float>();
        for (float i = 1.1F; i < 10; i += 2)
        {
            int successes = TestBinarySearch(testArray);
            Debug.Assert(successes == ((int)i) / 2);
            testArray.Add(i);
        }

        for (float i = 0.1F; i < 11; i += 2)
        {
            int index;
            bool result = testArray.BinarySearch(i, out index, delegate(float key, float elem) { return (int)key - (int)elem; });
            Debug.Assert(!result);
            testArray.InsertAt(index + 1, i);
        }

        int lastSuccesses = TestBinarySearch(testArray);
        Debug.Assert(lastSuccesses == 11);

        for (float i = 0; i < 11; i += 1)
        {
            int index;
            bool result = testArray.BinarySearch(i, out index, delegate(float key, float elem) { return (int)key - (int)elem; });
            Debug.Assert(result);
            testArray.InsertAt(index + 1, i);
        }

        lastSuccesses = TestBinarySearch(testArray);
        Debug.Assert(lastSuccesses == 11);

        // We always get the last one when the equality comparision allows multiple items to match.  
        for (float i = 0; i < 11; i += 1)
        {
            int index;
            bool result = testArray.BinarySearch(i, out index, delegate(float key, float elem) { return (int)key - (int)elem; });
            Debug.Assert(result);
            Debug.Assert(i == testArray[index]);
        }
        Console.WriteLine("Done");
    }
    private static int TestBinarySearch(GrowableArray<float> testArray)
    {
        int successes = 0;
        for (int i = 0; i < 30; i++)
        {
            int index;
            if (testArray.BinarySearch(i, out index, delegate(float key, float elem) { return (int)key - (int)elem; }))
            {
                successes++;
                Debug.Assert((int)testArray[index] == i);
            }
            else
                Debug.Assert(index + 1 <= testArray.Count);
        }
        return successes;
}
#endif
        #endregion

        // This allows 'foreach' to work.  
        public GrowableArrayEnumerator GetEnumerator() { return new GrowableArrayEnumerator(this); }
        public struct GrowableArrayEnumerator
        {
            public T Current
            {
                get { return array[cur]; }
            }
            public bool MoveNext()
            {
                cur++;
                return cur < end;
            }

            #region private
            internal GrowableArrayEnumerator(GrowableArray<T> growableArray)
            {
                cur = -1;
                end = growableArray.arrayLength;
                array = growableArray.array;
            }
            int cur;
            int end;
            T[] array;
            #endregion
        }
    }

    internal class FunctorComparer<T> : IComparer<T>
    {
        public FunctorComparer(Comparison<T> comparison) { this.comparison = comparison; }
        public int Compare(T x, T y) { return comparison(x, y); }

        private Comparison<T> comparison;
    };
}
