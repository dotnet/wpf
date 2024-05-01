// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// Description:
//              This is a helper class to facilate the storage of data that's Critical for set.
//              The data itself is not information disclosure but the value controls a critical
//              operation.
//
//              For example a filepath variable might control what part of the file system the
//              code gets access to.

using System ;
using System.Security ;

#if SYSTEM_XAML
namespace MS.Internal.Xaml
#else
namespace MS.Internal
#endif
{
    [Serializable]
    internal struct SecurityCriticalDataForSet<T>
    {
        internal SecurityCriticalDataForSet(T value)
        {
            _value = value;
        }

        internal T Value
        {
        #if DEBUG
            [System.Diagnostics.DebuggerStepThrough]
        #endif
            get
            {
                return _value;
            }

        #if DEBUG
            [System.Diagnostics.DebuggerStepThrough]
        #endif
            set
            {
                _value = value;
            }
        }

        private T _value;
    }
}

