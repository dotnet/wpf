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
        private unsafe FamilyCollection.CachedFace *   _face;

        private unsafe int _sizeInBytes;

        public unsafe CachedFontFace(FamilyCollection familyCollection, FamilyCollection.CachedFace* face, int sizeInBytes)
        {
            _familyCollection = familyCollection;
            _face = face;
            _sizeInBytes = sizeInBytes;
        }
        public bool IsNull
        {
            get
            {
                unsafe
                {
                    return _face == null;
                }
            }
        }
        public static CachedFontFace Null
        {
            get
            {
                unsafe
                {
                    return new CachedFontFace(null, null, 0);
                }
            }
        }

        public unsafe FamilyCollection.CachedPhysicalFace* CachedPhysicalFace
        {
            get
            {
                return (FamilyCollection.CachedPhysicalFace *)_face;
            }
        }

        public unsafe FamilyCollection.CachedCompositeFace* CompositeFace
        {
            get
            {
                return (FamilyCollection.CachedCompositeFace *)_face;
            }
        }

        public CheckedPointer CheckedPointer
        {
            get
            {
                unsafe
                {
                    return new CheckedPointer(_face, _sizeInBytes);
                }
            }
        }

        public FontStyle Style
        {
            get
            {
                unsafe
                {
                    return _face->style;
                }
            }
        }

        public FontWeight Weight
        {
            get
            {
                unsafe
                {
                    return _face->weight;
                }
            }
        }

        public FontStretch Stretch
        {
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

