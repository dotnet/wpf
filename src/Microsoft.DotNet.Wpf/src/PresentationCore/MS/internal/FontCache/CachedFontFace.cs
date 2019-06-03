// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
// 
//
// Description: The CachedFontFace class
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
    internal struct CachedFontFace
    {
        private FamilyCollection                _familyCollection;
        /// <SecurityNote>
        ///    Critical: This is a pointer variable and hence not safe to expose.
        /// </SecurityNote>
        [SecurityCritical]
        private unsafe FamilyCollection.CachedFace *   _face;

        /// <SecurityNote>
        /// Critical: Determines value of CheckedPointer.Size, which is used for bounds checking.
        /// </SecurityNote>
        [SecurityCritical]
        private unsafe int _sizeInBytes;

        /// <SecurityNote>
        ///    Critical: This accesses a pointer and is unsafe; the sizeInBytes is critical because it
        ///              is used for bounds checking (via CheckedPointer)
        /// </SecurityNote>
        [SecurityCritical]
        public unsafe CachedFontFace(FamilyCollection familyCollection, FamilyCollection.CachedFace* face, int sizeInBytes)
        {
            _familyCollection = familyCollection;
            _face = face;
            _sizeInBytes = sizeInBytes;
        }
        /// <SecurityNote>
        ///    Critical: This accesses a pointer and is unsafe
        ///    TreatAsSafe: This information is ok to return
        /// </SecurityNote>
        public bool IsNull
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _face == null;
                }
            }
        }
        /// <SecurityNote>
        ///    Critical: This contructs a null object
        ///    TreatAsSafe: This is ok to execute
        /// </SecurityNote>
        public static CachedFontFace Null
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return new CachedFontFace(null, null, 0);
                }
            }
        }

        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and returns a pointer
        /// </SecurityNote>
        public unsafe FamilyCollection.CachedPhysicalFace* CachedPhysicalFace
        {
            [SecurityCritical]    
            get
            {
                return (FamilyCollection.CachedPhysicalFace *)_face;
            }
        }

        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and returns a pointer
        /// </SecurityNote>
        public unsafe FamilyCollection.CachedCompositeFace* CompositeFace
        {
            [SecurityCritical]
            get
            {
                return (FamilyCollection.CachedCompositeFace *)_face;
            }
        }

        /// <SecurityNote>
        /// Critical:     Accesses critical fields and constructs a CheckedPointer which is a critical operation.
        /// TreatAsSafe:  The fields used to construct the CheckedPointer are marked critical and CheckedPointer
        ///               itself is safe to expose.
        /// </SecurityNote>
        public CheckedPointer CheckedPointer
        {
            [SecurityCritical, SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return new CheckedPointer(_face, _sizeInBytes);
                }
            }
        }

        /// <SecurityNote>
        ///    Critical: This accesses a pointer and is unsafe
        ///    TreatAsSafe: This information is ok to return
        /// </SecurityNote>
        public FontStyle Style
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _face->style;
                }
            }
        }

        /// <SecurityNote>
        ///    Critical: This accesses a pointer and is unsafe
        ///    TreatAsSafe: This information is ok to return
        /// </SecurityNote>
        public FontWeight Weight
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _face->weight;
                }
            }
        }

        /// <SecurityNote>
        ///    Critical: This accesses a pointer and is unsafe
        ///    TreatAsSafe: This information is ok to return
        /// </SecurityNote>
        public FontStretch Stretch
        {
            [SecurityCritical,SecurityTreatAsSafe]
            get
            {
                unsafe
                {
                    return _face->stretch;
                }
            }
        }

        /// <summary>
        /// Matching style
        /// </summary>
        public MatchingStyle MatchingStyle
        {
            get
            {
                return new MatchingStyle(Style, Weight, Stretch);
            }
        }


        /// <SecurityNote>
        /// Critical - as this accesses unsafe pointers and returns GlyphTypeface created from internal constructor
        ///            which exposes windows font information.
        /// Safe - as this doesn't allow you to create a GlyphTypeface object for a specific
        ///        font and thus won't allow you to figure what fonts might be installed on
        ///        the machine.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        public GlyphTypeface CreateGlyphTypeface()
        {
            unsafe
            {
                return new GlyphTypeface(
                    _familyCollection.GetFontUri(CachedPhysicalFace),
                    CachedPhysicalFace->styleSimulations,
                    /* fromPublic = */ false
                );
            }
        }
    }
}

