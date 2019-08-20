// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: CachedTypeface
//
//

using System;
using System.Windows;
using System.Windows.Media;
using MS.Internal.FontFace;

namespace MS.Internal.FontCache
{
    /// <summary>
    /// CachedTypeface stores the canonical values and font data of a Typeface. It is looked up or constructed 
    /// when client does shaping or query metrics from Typeface objects. Caching this object allows
    /// many equal typeface objects to share the same piece of canonicalized data.
    /// </summary>
    internal class CachedTypeface
    {
        private FontStyle           _canonicalStyle;
        private FontWeight          _canonicalWeight;
        private FontStretch         _canonicalStretch;
        private IFontFamily         _firstFontFamily;
        private ITypefaceMetrics    _typefaceMetrics;
        private bool                _nullFont;

        internal CachedTypeface(
            FontStyle        canonicalStyle,
            FontWeight       canonicalWeight,
            FontStretch      canonicalStretch,
            IFontFamily      firstFontFamily,
            ITypefaceMetrics typefaceMetrics,
            bool             nullFont
            )
        {
            _canonicalStyle   = canonicalStyle;
            _canonicalWeight  = canonicalWeight;
            _canonicalStretch = canonicalStretch;

            Invariant.Assert(firstFontFamily != null && typefaceMetrics != null);
            
            _firstFontFamily  = firstFontFamily;
            _typefaceMetrics  = typefaceMetrics;
            _nullFont         = nullFont;            
        }        

        internal FontStyle CanonicalStyle
        {
            get { return _canonicalStyle; }
        }

        internal FontWeight CanonicalWeight
        {
            get { return _canonicalWeight; }
        }

        internal FontStretch CanonicalStretch
        {
            get { return _canonicalStretch; }
        }

        internal IFontFamily FirstFontFamily
        {
            get { return _firstFontFamily; }
        }

        internal ITypefaceMetrics TypefaceMetrics
        {
            get { return _typefaceMetrics; }
        }

        internal bool NullFont
        {
            get { return _nullFont; }
        }        
    }    
}
  
