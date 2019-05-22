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
using System.Security.Permissions;
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

        /// <SecurityNote>
        /// Critical: This holds reference to a pointer which is not ok to expose
        /// </SecurityNote>
        [SecurityCritical]
        private unsafe FamilyCollection.CachedFamily * _family;

        /// <SecurityNote>
        /// Critical: Determines value of CheckedPointer.Size, which is used for bounds checking.
        /// </SecurityNote>
        [SecurityCritical]
        private int _sizeInBytes;

        /// <SecurityNote>
        /// Critical: This stores a pointer and the class is unsafe because it manipulates the pointer; the sizeInBytes
        ///           parameter is critical because it is used for bounds checking (via CheckedPointer)
        /// </SecurityNote>
        [SecurityCritical]
        public unsafe CachedFontFamily(FamilyCollection familyCollection, FamilyCollection.CachedFamily*  family, int sizeInBytes)
        {
            _familyCollection = familyCollection;
            _family = family;
            _sizeInBytes = sizeInBytes;
        }
        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public bool IsNull
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family == null;
                }
            }
        }
        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public bool IsPhysical
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family->familyType == FamilyCollection.FamilyType.Physical;
                }
            }
        }
        
        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public bool IsComposite
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family->familyType == FamilyCollection.FamilyType.Composite;
                }
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public string OrdinalName
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _familyCollection.GetFamilyName(_family);
                }
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer and returns a pointer to a structure
        /// </SecurityNote>
        public unsafe FamilyCollection.CachedPhysicalFamily* PhysicalFamily
        {
            [SecurityCritical]
            get
            {
                Invariant.Assert(IsPhysical);
                return (FamilyCollection.CachedPhysicalFamily *)_family;
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer and returns a pointer to a structure
        /// </SecurityNote>
        public unsafe FamilyCollection.CachedCompositeFamily* CompositeFamily
        {
            [SecurityCritical]
            get
            {
                Invariant.Assert(IsComposite);
                return (FamilyCollection.CachedCompositeFamily *)_family;
            }
        }

        /// <SecurityNote>
        /// Critical:     Accesses critical fields and constructs a CheckedPointer which is a critical operation.
        /// TreatAsSafe:  The fields used to construct the CheckedPointer are marked critical and CheckedPointer
        ///               itself is safe to expose.
        /// </SecurityNote>
        public CheckedPointer CheckedPointer
        {
            [SecurityCritical,SecurityTreatAsSafe]
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

        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public int NumberOfFaces
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family->numberOfTypefaces;
                }
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public double Baseline
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family->baseline;
                }
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public double LineSpacing
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _family->lineSpacing;
                }
            }
        }

        /// <SecurityNote>
        /// Critical: This acccesses a pointer
        /// TreatAsSafe: This information is ok to expose
        /// </SecurityNote>
        public IDictionary<XmlLanguage, string> Names
        {
            [SecurityCritical,SecurityTreatAsSafe]
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

            /// <SecurityNote>
            /// Critical: This acccesses a pointer
            /// TreatAsSafe: This funtions moves to the next 
            /// </SecurityNote>
            [SecurityCritical,SecurityTreatAsSafe]
            public bool MoveNext()
            {
                ++_currentFace;
                if (0 <= _currentFace && _currentFace < _family.NumberOfFaces)
                    return true;
                _currentFace = _family.NumberOfFaces;
                return false;
            }

            /// <SecurityNote>
            /// Critical: This acccesses a pointer
            /// TreatAsSafe: This information is ok to expose
            /// </SecurityNote>
            CachedFontFace IEnumerator<CachedFontFace>.Current
            {
            [SecurityCritical,SecurityTreatAsSafe]
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

