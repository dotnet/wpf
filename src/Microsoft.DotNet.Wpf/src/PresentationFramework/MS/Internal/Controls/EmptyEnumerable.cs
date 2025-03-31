// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Empty enumerable
//

using System.Collections;

namespace MS.Internal.Controls
{
    /// <summary>
    /// Returns an Enumerable that is empty.
    /// </summary>
    internal class EmptyEnumerable : IEnumerable
    {
        // singleton class, private ctor
        private EmptyEnumerable()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return EmptyEnumerator.Instance;
        }

        /// <summary>
        /// Read-Only instance of an Empty Enumerable.
        /// </summary>
        public static IEnumerable Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EmptyEnumerable();
                }
                return _instance;
            }
        }

        private static IEnumerable _instance;
    }
}
