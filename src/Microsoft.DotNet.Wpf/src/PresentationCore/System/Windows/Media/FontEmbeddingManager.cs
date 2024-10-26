// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The FontEmbeddingManager class handles physical and composite font embedding.
//
//              See spec at http://avalon/text/DesignDocsAndSpecs/Font%20embedding%20APIs.htm
// 

using SR = MS.Internal.PresentationCore.SR;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace System.Windows.Media
{
    /// <summary>
    /// The <see cref="FontEmbeddingManager"/> class handles physical and composite font embedding.
    /// </summary>
    public class FontEmbeddingManager
    {
        /// <summary>
        /// Creates a new instance of font usage manager.
        /// </summary>
        public FontEmbeddingManager()
        {
            _collectedGlyphTypefaces = new Dictionary<Uri, HashSet<ushort>>(s_uriComparer);
        }

        /// <summary>
        /// Collects information about glyph typeface and index used by a glyph run.
        /// </summary>
        /// <param name="glyphRun">Glyph run to obtain typeface and index information from.</param>
        public void RecordUsage(GlyphRun glyphRun)
        {
            ArgumentNullException.ThrowIfNull(glyphRun);

            Uri glyphTypeface = glyphRun.GlyphTypeface.FontUri;

            ref HashSet<ushort> glyphSet = ref CollectionsMarshal.GetValueRefOrAddDefault(_collectedGlyphTypefaces, glyphTypeface, out bool exists);
            if (!exists)
                glyphSet = new HashSet<ushort>(glyphRun.GlyphIndices.Count);

            foreach (ushort glyphIndex in glyphRun.GlyphIndices)
            {
                glyphSet.Add(glyphIndex);
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
        /// <exception cref="ArgumentException">
        ///     Glyph typeface Uri does not point to a previously recorded glyph typeface.
        /// </exception>
        [CLSCompliant(false)]
        public ICollection<ushort> GetUsedGlyphs(Uri glyphTypeface)
        {
            HashSet<ushort> glyphsUsed = _collectedGlyphTypefaces[glyphTypeface];
            if (glyphsUsed == null) // NOTE: This will currently throw KeyNotFoundException instead
            {
                throw new ArgumentException(SR.GlyphTypefaceNotRecorded, nameof(glyphTypeface));
            }
            return glyphsUsed;
        }

        private sealed class UriComparer : IEqualityComparer<Uri>
        {
            public bool Equals(Uri x, Uri y)
            {
                // We don't use Uri.Equals because it doesn't compare Fragment parts,
                // and we use Fragment part to store font face index.
                return string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Uri obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// Contains the FontUri and its GlyphIndicies as values.   
        /// </summary>
        private readonly Dictionary<Uri, HashSet<ushort>> _collectedGlyphTypefaces;

        /// <summary>
        /// Custom comparer for FontUri used in <see cref="_collectedGlyphTypefaces"/>.
        /// </summary>
        private static readonly UriComparer s_uriComparer = new();

    }
}
