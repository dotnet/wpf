// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Empty enumerator
//

using System.Collections;

namespace MS.Internal.Controls
{
    /// <summary>
    /// Returns an Enumerator that enumerates over nothing.
    /// </summary>
    internal class EmptyEnumerator : IEnumerator
    {
        // singleton class, private ctor
        private EmptyEnumerator()
        {
        }

        /// <summary>
        /// Read-Only instance of an Empty Enumerator.
        /// </summary>
        public static IEnumerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EmptyEnumerator();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Reset() { }

        /// <summary>
        /// Returns false.
        /// </summary>
        /// <returns>false</returns>
        public bool MoveNext() { return false; }

        /// <summary>
        /// Returns null.
        /// </summary>
        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        private static IEnumerator _instance;
    }
}
