// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Collection of typefaces
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using MS.Internal.FontCache;
using System.Globalization;
using System.Security;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.FontFace
{
    internal unsafe struct TypefaceCollection : ICollection<Typeface>
    {
        private FontFamily                    _fontFamily;

        // setting _family and _familyTypefaceCollection are mutually exclusive.
        private Text.TextInterface.FontFamily _family;
        private FamilyTypefaceCollection      _familyTypefaceCollection;

        public TypefaceCollection(FontFamily fontFamily, Text.TextInterface.FontFamily family)
        {
            _fontFamily = fontFamily;
            _family = family;
            _familyTypefaceCollection = null;
        }

        public TypefaceCollection(FontFamily fontFamily, FamilyTypefaceCollection familyTypefaceCollection)
        {
            _fontFamily = fontFamily;
            _familyTypefaceCollection = familyTypefaceCollection;
            _family = null;
        }

        #region ICollection<Typeface> Members

        public void Add(Typeface item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(Typeface item)
        {
            foreach (Typeface t in this)
            {
                if (t.Equals(item))
                    return true;
            }
            return false;
        }

        public void CopyTo(Typeface[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
            }

            // The extra "arrayIndex >= array.Length" check in because even if _collection.Count
            // is 0 the index is not allowed to be equal or greater than the length
            // (from the MSDN ICollection docs)
            if (arrayIndex < 0 || arrayIndex >= array.Length || (arrayIndex + Count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            foreach (Typeface t in this)
            {
                array[arrayIndex++] = t;
            }
        }

        public int Count
        {
            get
            {
                Debug.Assert((_family != null && _familyTypefaceCollection == null)|| (_familyTypefaceCollection != null && _family == null));
                if (_family != null)
                {
                    return checked((int)_family.Count);
                }
                else
                {
                    return _familyTypefaceCollection.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool Remove(Typeface item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<Typeface> Members

        public IEnumerator<Typeface> GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        private struct Enumerator : IEnumerator<Typeface>
        {
            public Enumerator(TypefaceCollection typefaceCollection)
            {
                _typefaceCollection = typefaceCollection;

                Debug.Assert((typefaceCollection._family != null && typefaceCollection._familyTypefaceCollection == null)
                    || (typefaceCollection._familyTypefaceCollection != null && typefaceCollection._family == null));
                if (typefaceCollection._family != null)
                {
                    _familyEnumerator = ((IEnumerable<Text.TextInterface.Font>)typefaceCollection._family).GetEnumerator();
                    _familyTypefaceEnumerator = null;
                }
                else
                {
                    _familyTypefaceEnumerator = ((IEnumerable<FamilyTypeface>)typefaceCollection._familyTypefaceCollection).GetEnumerator();
                    _familyEnumerator = null;
                }
            }

            #region IEnumerator<Typeface> Members

            public Typeface Current
            {
                get
                {
                    if (_typefaceCollection._family != null)
                    {
                        Text.TextInterface.Font face = _familyEnumerator.Current;
                        return new Typeface(_typefaceCollection._fontFamily, new FontStyle((int)face.Style), new FontWeight((int)face.Weight), new FontStretch((int)face.Stretch));
                    }
                    else
                    {
                        FamilyTypeface familyTypeface = _familyTypefaceEnumerator.Current;
                        return new Typeface(_typefaceCollection._fontFamily, familyTypeface.Style, familyTypeface.Weight, familyTypeface.Stretch);
                    }
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose() {}

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return ((IEnumerator<Typeface>)this).Current;
                }
            }

            public bool MoveNext()
            {
                if (_familyEnumerator != null)
                {
                    return _familyEnumerator.MoveNext();
                }
                else
                {
                    return _familyTypefaceEnumerator.MoveNext();
                }
            }

            public void Reset()
            {
                if (_typefaceCollection._family != null)
                {
                    _familyEnumerator = ((IEnumerable<Text.TextInterface.Font>)_typefaceCollection._family).GetEnumerator();
                    _familyTypefaceEnumerator = null;
                }
                else
                {
                    _familyTypefaceEnumerator = ((IEnumerable<FamilyTypeface>)_typefaceCollection._familyTypefaceCollection).GetEnumerator();
                    _familyEnumerator = null;
                }
            }

            #endregion

            // setting _familyEnumerator and _familyTypefaceEnumerator are mutually exclusive.
            private IEnumerator<Text.TextInterface.Font> _familyEnumerator;
            private IEnumerator<FamilyTypeface>          _familyTypefaceEnumerator;
            private TypefaceCollection                   _typefaceCollection;
        }
    }
}
