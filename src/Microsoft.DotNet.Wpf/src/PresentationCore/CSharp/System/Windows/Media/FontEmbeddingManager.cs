// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: The FontEmbeddingManager class handles physical and composite font embedding.
//
//              See spec at http://avalon/text/DesignDocsAndSpecs/Font%20embedding%20APIs.htm
// 
//
//

using System;
using System.Text;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.Shaping;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

// Allow suppression of presharp warnings
#pragma warning disable 1634, 1691

namespace System.Windows.Media
{
    /// <summary>
    /// The FontEmbeddingManager class handles physical and composite font embedding.
    /// </summary>
    public class FontEmbeddingManager
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a new instance of font usage manager.
        /// </summary>
        public FontEmbeddingManager()
        {
            _collectedGlyphTypefaces = new Dictionary<Uri, Dictionary<ushort, bool>>(_uriComparer);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Collects information about glyph typeface and index used by a glyph run.
        /// </summary>
        /// <param name="glyphRun">Glyph run to obtain typeface and index information from.</param>
        public void RecordUsage(GlyphRun glyphRun)
        {
            if (glyphRun == null)
                throw new ArgumentNullException("glyphRun");

            // Suppress PRESharp parameter validation warning about glyphRun.GlyphTypeface because
            // GlyphRun.GlyphTypeface property cannot be null.
#pragma warning suppress 56506
            Uri glyphTypeface = glyphRun.GlyphTypeface.FontUri;

            Dictionary<ushort, bool> glyphSet;
            
            if (_collectedGlyphTypefaces.ContainsKey(glyphTypeface))
                glyphSet = _collectedGlyphTypefaces[glyphTypeface];
            else
                glyphSet = _collectedGlyphTypefaces[glyphTypeface] = new Dictionary<ushort, bool>();

            foreach(ushort glyphIndex in glyphRun.GlyphIndices)
            {             
                glyphSet[glyphIndex] = true;
            }
        }

        /// <summary>
        /// Returns the collection of glyph typefaces used by the previously added glyph runs.
        /// </summary>
        /// <returns>The collection of glyph typefaces used by the previously added glyph runs.</returns>
        [CLSCompliant(false)]
        public ICollection<Uri> GlyphTypefaceUris
        {
            get
            {
                return _collectedGlyphTypefaces.Keys;
            }
        }

        /// <summary>
        /// Obtain the list of glyphs used by the glyph typeface specified by a Uri.
        /// </summary>
        /// <param name="glyphTypeface">Specifies the Uri of a glyph typeface to obtain usage data for.</param>
        /// <returns>A collection of glyph indices recorded previously.</returns>
        /// <exception cref="System.ArgumentException">
        ///     Glyph typeface Uri does not point to a previously recorded glyph typeface.
        /// </exception>
        [CLSCompliant(false)]
        public ICollection<ushort> GetUsedGlyphs(Uri glyphTypeface)
        {
            Dictionary<ushort, bool> glyphsUsed = _collectedGlyphTypefaces[glyphTypeface];
            if (glyphsUsed == null)
            {
                throw new ArgumentException(SR.Get(SRID.GlyphTypefaceNotRecorded), "glyphTypeface");
            }
            return glyphsUsed.Keys;
        }

        #endregion Public Methods

        private class UriComparer : IEqualityComparer<Uri>
        {
            #region IEqualityComparer<Uri> Members

            public bool Equals(Uri x, Uri y)
            {
                // We don't use Uri.Equals because it doesn't compare Fragment parts,
                // and we use Fragment part to store font face index.
                return String.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Uri obj)
            {
                return obj.GetHashCode();
            }

            #endregion
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// bool values in the dictionary don't matter,
        /// we'll switch to Set class when it becomes available.
        /// </summary>
        private Dictionary<Uri, Dictionary<ushort, bool>>   _collectedGlyphTypefaces;

        private static UriComparer _uriComparer = new UriComparer();

        #endregion Private Fields
    }
}
