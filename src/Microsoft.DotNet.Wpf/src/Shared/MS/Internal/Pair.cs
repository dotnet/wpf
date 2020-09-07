// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

// 
//
// Description: Pair class is useful when one needs to treat a pair of objects as a singly key in a collection.
// 
//
//  
//
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace MS.Internal
{
    /// <summary>
    /// Pair class is useful when one needs to treat a pair of objects as a singly key in a collection.
    /// Apart from providing storage and accessors, the class forwards GetHashCode and Equals to the contained objects.
    /// Both object are allowed to be null.
    /// </summary>
    internal class Pair
    {
        public Pair(object first, object second)
        {
            _first = first;
            _second = second;
        }

        public object First { get { return _first; } }
        public object Second { get { return _second; } }

        public override int GetHashCode()
        {
            return (_first == null ? 0 : _first.GetHashCode()) ^ (_second == null ? 0 : _second.GetHashCode());
        }

        public override bool Equals(object o)
        {
            Pair other = o as Pair;
            return other != null &&
                (_first != null ? _first.Equals(other._first) : other._first == null) &&
                (_second != null ? _second.Equals(other._second) : other._second == null);
        }

        private object _first;
        private object _second;
    }
}

