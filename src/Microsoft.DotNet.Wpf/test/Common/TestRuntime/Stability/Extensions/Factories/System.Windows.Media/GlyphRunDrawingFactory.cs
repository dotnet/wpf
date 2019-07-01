// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    internal class GlyphRunDrawingFactory : DiscoverableFactory<GlyphRunDrawing>
    {
        #region Public Members

        public Brush ForegroundBrush { get; set; }

        public GlyphRun GlyphRun { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use default constructor or not.
        /// </summary>
        public bool UseDefaultConstructor { get; set; }

        #endregion

        #region Override Members

        public override GlyphRunDrawing Create(DeterministicRandom random)
        {
            GlyphRunDrawing glyphRunDrawing;
            if (UseDefaultConstructor)
            {
                glyphRunDrawing = new GlyphRunDrawing();
                glyphRunDrawing.ForegroundBrush = ForegroundBrush;
                glyphRunDrawing.GlyphRun = GlyphRun;
            }
            else
            {
                glyphRunDrawing = new GlyphRunDrawing(ForegroundBrush, GlyphRun);
            }

            return glyphRunDrawing;
        }

        #endregion
    }
}
