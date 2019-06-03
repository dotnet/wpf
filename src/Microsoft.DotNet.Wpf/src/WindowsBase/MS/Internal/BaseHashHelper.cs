// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: Static class to help work around hashing-related bugs.
//

using System;
using System.Reflection;                // Assembly
using System.Collections.Specialized;   // HybridDictionary
using MS.Internal.WindowsBase;          // [FriendAccessAllowed]

namespace MS.Internal
{
    [FriendAccessAllowed]   // defined in Base, used in Core and Framework
    internal static class BaseHashHelper
    {
        static BaseHashHelper()
        {
            // register bad types from WindowsBase
            MS.Internal.Hashing.WindowsBase.HashHelper.Initialize();
        }

        [FriendAccessAllowed]   // defined in Base, used in Core and Framework
        internal static void RegisterTypes(Assembly assembly, Type[] types)
        {
            HybridDictionary dictionary = DictionaryFromList(types);

            lock(_table)
            {
                _table[assembly] = dictionary;
            }
        }

        // Some types don't have reliable hash codes - the hashcode can change
        // during the lifetime of an object of that type.  Such an object cannot
        // be used as the key of a hashtable or dictionary.  This is where we
        // detect such objects, so the caller can find some other way to cope.
        [FriendAccessAllowed]   // defined in Base, used in Core and Framework
        internal static bool HasReliableHashCode(object item)
        {
            // null doesn't actually have a hashcode at all.  This method can be
            // called with a representative item from a collection - if the
            // representative is null, we'll be pessimistic and assume the
            // items in the collection should not be hashed.
            if (item == null)
                return false;

            Type type = item.GetType();
            Assembly assembly = type.Assembly;
            HybridDictionary dictionary;

            lock(_table)
            {
                dictionary = (HybridDictionary)_table[assembly];
            }

            if (dictionary == null)
            {
                dictionary = new HybridDictionary();

                lock(_table)
                {
                    _table[assembly] = dictionary;
                }
            }

            return !dictionary.Contains(type);
        }

        // populate a dictionary from the given list
        private static HybridDictionary DictionaryFromList(Type[] types)
        {
            HybridDictionary dictionary = new HybridDictionary(types.Length);
            for (int i=0; i<types.Length; ++i)
            {
                dictionary.Add(types[i], null);
            }

            return dictionary;
        }

        static HybridDictionary _table = new HybridDictionary(3);
    }
}

