// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows;

namespace MS.Utility
{
    /***************************************************************************\
    *****************************************************************************
    *
    * DTypeMap (DType --> Object)
    *
    * Maps the first N used DependencyObject-derived types via an array
    * (low constant time lookup) for mapping. After which falls back on a
    * hash table.
    *
    * - Fastest gets and sets (normally single array access). 
    * - Large memory footprint.
    *
    * Starting mapping is all map to null
    * 
    *****************************************************************************
    \***************************************************************************/

    using MS.Internal.PresentationCore;

    [FriendAccessAllowed] // Built into Core, also used by Framework.
    internal class DTypeMap
    {
    
        public DTypeMap(int entryCount)
        {
            // Constant Time Lookup entries (array size)
            _entryCount = entryCount;
            _entries = new object[_entryCount];
            _activeDTypes = new ItemStructList<DependencyObjectType>(128);
        }
    
        public object this[DependencyObjectType dType]
        {
            get
            {
                if (dType.Id < _entryCount)
                {
                    return _entries[dType.Id];
                }
                else
                {
                    if (_overFlow != null)
                    {
                        return _overFlow[dType];
                    }

                    return null;
                }
            }

            set
            {
                if (dType.Id < _entryCount)
                {
                    _entries[dType.Id] = value;
                }
                else
                {
                    if (_overFlow == null)
                    {
                        _overFlow = new Hashtable();
                    }

                    _overFlow[dType] = value;
                }

                _activeDTypes.Add(dType);
            }
        }

        // Return list of non-null DType mappings
        public ItemStructList<DependencyObjectType> ActiveDTypes
        {
            get { return _activeDTypes; }
        }

        // Clear the data-structures to be able to start over
        public void Clear()
        {
            for (int i=0; i<_entryCount; i++)
            {
                _entries[i] = null;
            }

            for (int i=0; i<_activeDTypes.Count; i++)
            {
                _activeDTypes.List[i] = null;
            }

            if (_overFlow != null)
            {
                _overFlow.Clear();
            }
        }

        private int _entryCount;
        private object[] _entries;
        private Hashtable _overFlow;
        private ItemStructList<DependencyObjectType> _activeDTypes;
    }
}
