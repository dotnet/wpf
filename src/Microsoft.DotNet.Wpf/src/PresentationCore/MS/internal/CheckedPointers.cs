// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: Checked pointers for various types
//
//

using System;
using System.Security;
using MS.Internal.Shaping;
using MS.Internal.FontCache;

//
// The file contains wrapper structs for various pointer types. 
// This is to allow us passing these pointers safely in layout code and provides 
// some bound checking. Only construction and probing into these pointers are security critical.
//

namespace MS.Internal
{
    /// <summary>
    /// Checked pointer for (Char*)
    /// </summary>
    internal struct CheckedCharPointer    
    {
        internal unsafe CheckedCharPointer(char * pointer, int length)
        {
            _checkedPointer = new CheckedPointer(pointer, length * sizeof(char));
        }

        internal unsafe char * Probe(int offset, int length)
        {
            return (char*) _checkedPointer.Probe(offset * sizeof(char), length * sizeof(char));
        }
        
        private CheckedPointer _checkedPointer;
    }

    /// <summary>
    /// Checked pointer for (int*)
    /// </summary>
    internal struct CheckedIntPointer
    {
        internal unsafe CheckedIntPointer(int * pointer, int length)
        {
            _checkedPointer = new CheckedPointer(pointer, length * sizeof(int));
        }
        
        internal unsafe int * Probe(int offset, int length)
        {
            return (int *) _checkedPointer.Probe(offset * sizeof(int), length * sizeof(int));
        }
        
        private CheckedPointer _checkedPointer;
    }

    /// <summary>
    /// Checked pointer for (ushort*)
    /// </summary>
    internal struct CheckedUShortPointer
    {
        internal unsafe CheckedUShortPointer(ushort * pointer, int length)
        {
            _checkedPointer = new CheckedPointer(pointer, length * sizeof(ushort));
        }

        internal unsafe ushort * Probe(int offset, int length)
        {
            return (ushort *) _checkedPointer.Probe(offset * sizeof(ushort), length * sizeof(ushort));
        }

        private CheckedPointer _checkedPointer;
    }
}
