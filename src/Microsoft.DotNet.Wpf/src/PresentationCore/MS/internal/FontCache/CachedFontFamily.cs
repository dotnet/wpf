// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
// 
//
// Description: The CachedFontFamily class
//
//  
//
//
//---------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;

using MS.Win32;
using MS.Utility;
using MS.Internal;
using MS.Internal.FontFace;

namespace MS.Internal.FontCache
{
    /// <summary>
    /// This structure exists because we need a common wrapper for enumeration, but we can't use original cache structures:
    /// 1. C# doesn't allow IEnumerable/IEnumerator on pointer.
    /// 2. The cache structures don't inherit from base class.
    /// </summary>
    internal struct CachedFontFamily : IEnumerable<CachedFontFace>
    {
        private FamilyCollection                _familyCollection;

        private unsafe FamilyCollection.CachedFamily * _family;

        private int _sizeInBytes;

        public unsafe CachedFontFamily(FamilyCollection familyCollection, FamilyCollection.CachedFamily*  family, int sizeInBytes)
        {
            _familyCollection = familyCollection;
            _family = family;
            _sizeInBytes = sizeInBytes;
        }
        public bool IsNull
        {
            get
            {
                unsafe
                {
                    return _family == null;
                }
            }
        }
        public bool IsPhysical
        {
            get
            {
                unsafe
                {
                    return _family->familyType == FamilyCollection.FamilyType.Physical;
                }
            }
        }
        
        public bool IsComposite
        {
            get
            {
                unsafe
                {
                    return _family->familyType == FamilyCollection.FamilyType.Composite;
                }
            }
        }

        public string OrdinalName
        {
            get
            {
                unsafe
                {
                    return _familyCollection.GetFamilyName(_family);
                }
            }
        }

        public unsafe FamilyCollection.CachedPhysicalFamily* PhysicalFamily
        {
            get
            {
                Invariant.Assert(IsPhysical);
                return (FamilyCollection.CachedPhysicalFamily *)_family;
            }
        }

        public unsafe FamilyCollection.CachedCompositeFamily* CompositeFamily
        {
            get
            {
                Invariant.Assert(IsComposite);
                return (FamilyCollection.CachedCompositeFamily *)_family;
            }
        }

        public CheckedPointer CheckedPointer
        {
            get
            {
                unsafe
                {
                    return new CheckedPointer(_family, _sizeInBytes);
                }
            }
        }

        public FamilyCollection FamilyCollection
        {
            get
            {
                return _familyCollection;
            }
        }

        public int NumberOfFaces
        {
            get
            {
                unsafe
                {
                    return _family->numberOfTypefaces;
                }
            }
        }

        public double Baseline
        {
            get
            {
                unsafe
                {
                    return _family->baseline;
                }
            }
        }

        public double LineSpacing
        {
            get
            {
                unsafe
                {
                    return _family->lineSpacing;
                }
            }
        }

        public IDictionary<XmlLanguage, string> Names
        {
            get
            {
                unsafe
                {
                    return _familyCollection.GetLocalizedNameDictionary(_family);
                }
            }
        }
        #region IEnumerable<CachedFontFace> Members

        IEnumerator<CachedFontFace> IEnumerable<CachedFontFace>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        private unsafe struct Enumerator : IEnumerator<CachedFontFace>
        {
            private int                 _currentFace;
            private CachedFontFamily    _family;

            public Enumerator(CachedFontFamily family)
            {
                _family = family;
                _currentFace = -1;
            }

            #region IEnumerator<CachedFontFace> Members

            public bool MoveNext()
            {
                ++_currentFace;
                if (0 <= _currentFace && _currentFace < _family.NumberOfFaces)
                    return true;
                _currentFace = _family.NumberOfFaces;
                return false;
            }

            CachedFontFace IEnumerator<CachedFontFace>.Current
            {
                get
                {
                    return _family.FamilyCollection.GetCachedFace(_family, _currentFace);
                }
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get
                {
                    return ((IEnumerator<CachedFontFace>)this).Current;
                }
            }

            void IEnumerator.Reset()
            {
                _currentFace = -1;
            }

            #endregion

            #region IDisposable Members

            public void Dispose() {}

            #endregion
        }
    }
}

