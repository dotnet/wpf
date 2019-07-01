// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.ComponentModel;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    [TargetTypeAttribute(typeof(GlyphTypeface))]
    internal class GlyphTypefaceFactory : DiscoverableFactory<GlyphTypeface>
    {
        #region Public Members

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public string FontUriString { get; set; }

        public StyleSimulations StyleSimulations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use default constructor or not.
        /// </summary>
        public bool UseDefaultConstructor { get; set; }

        #endregion

        #region Override Memebers

        public override GlyphTypeface Create(DeterministicRandom random)
        {
            GlyphTypeface glyphTypeface;

            if (UseDefaultConstructor)
            {
                glyphTypeface = new GlyphTypeface();
                ((ISupportInitialize)glyphTypeface).BeginInit();
                glyphTypeface.FontUri = new Uri(FontUriString);
                glyphTypeface.StyleSimulations = StyleSimulations;
                ((ISupportInitialize)glyphTypeface).EndInit();
            }
            else
            {
                glyphTypeface = new GlyphTypeface(new Uri(FontUriString), StyleSimulations);
            }

            return glyphTypeface;
        }

        #endregion
    }
}
