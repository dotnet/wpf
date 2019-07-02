// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;

namespace Microsoft.Test.RenderingVerification
{
    /// <summary>
    /// Keep a collection of ony the smallest element added.
    /// </summary>
    [System.ComponentModel.BrowsableAttribute(false)]
    internal class KeepGreaterKey : IEnumerable
    {
        #region Properties
            private IComparable _biggest = null;
            private ArrayList _list = null;
            internal int Count
            {
                get { return _list.Count; }
            }
        #endregion Properties

        #region Constructors
            internal KeepGreaterKey()
            {
                _list = new ArrayList();
            }
        #endregion Constructors

        #region Methods
            internal void Add(IComparable compareKey, object value)
            {
                if (_biggest == null)
                {
                    _biggest = compareKey;
                    _list.Add(value);
                    return;
                }
                if (_biggest.CompareTo(compareKey) <= 0)
                {
                    if (_biggest.CompareTo(compareKey) < 0)
                    {
                        _biggest = compareKey;
                        _list.Clear();
                    }
                    _list.Add(value);
                }
            }
            internal object[] GetBiggestElements()
            {
                return _list.ToArray();
            }
            internal Array ToArray(Type type)
            {
                return _list.ToArray(type);
            }
        #endregion Methods

        #region IEnumerable Members
            public IEnumerator GetEnumerator()
            {
                return _list.GetEnumerator();
            }
        #endregion
    }
}
