// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary/>
    [TargetTypeAttribute(typeof(GlyphRun))]
    internal class GlyphRunFactory : DiscoverableFactory<GlyphRun>
    {
        #region Public Members

        public bool IsSideways { get; set; }

        public Point BaselineOrign { get; set; }

        public int BidiLevel { get; set; }

        public double ScaleFontRenderingEmSize { get; set; }

        [InputAttribute(ContentInputSource.CreateFromFactory, IsEssentialContent = true)]
        public GlyphTypeface GlyphTypeface { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public XmlLanguage Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use default constructor or not.
        /// </summary>
        public bool UseDefaultConstructor { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// GlyphRun Charaters and DeviceFontName properties ignored.
        /// </summary>
        /// <param name="random"/>
        /// <returns>Return a new GlyphRun.</returns>
        public override GlyphRun Create(DeterministicRandom random)
        {
            int count = random.Next(20) + 1;
            List<bool> caretStops = new List<bool>(count);
            List<double> scaleAdvanceWidths = new List<double>(count);
            List<UInt16> clusterMap = new List<UInt16>(count);
            List<UInt16> glyphIndices = new List<UInt16>(count);
            List<Point> glyphOffsets = new List<Point>(count);
            List<char> characters = new List<char>(count);

            //The count need to observe: GlyphIndices=AdvanceWidths=GlyphOffsets
            //                           Characters=CaretStops-1=ClusterMap     
            //HACK: GlyphIndices.Count and Characters.Count can be unequal!            
            for (int n = 0; n < count; n++)
            {
                caretStops.Add(random.NextBool());
                scaleAdvanceWidths.Add(random.NextDouble());
                glyphIndices.Add((UInt16)random.Next(GlyphTypeface.GlyphCount));
                glyphOffsets.Add(new Point(random.NextDouble() * 100, random.NextDouble() * 100));
                clusterMap.Add((UInt16)random.Next(count));
                characters.Add((char)random.Next(256));
            }

            caretStops.Add(random.NextBool());
            //The first ClusterMap need to be zero and all the elements need be sorted.
            clusterMap[0] = 0;
            clusterMap.Sort();

            //Change value of [0-1.0) to (-100,100).
            ExtendDoubleListRange(scaleAdvanceWidths);
            //Change value to (0,100];
            ScaleFontRenderingEmSize = (1 - ScaleFontRenderingEmSize) * 100;

            //Workaround bug 805087 WPFStress: System.OverflowException @ System.Windows.Media.GlyphRun.CreateOnChannel
            //TODO: After fix bug 805087, below code need to be deleted.
            BidiLevel = random.Next(62);

            //Right to left text is not supported when BidiLevel is odd
            if (BidiLevel % 2 == 1)
            {
                IsSideways = false;
            }

            GlyphRun glyphRun;

            if (UseDefaultConstructor)
            {
                glyphRun = new GlyphRun();
                ((ISupportInitialize)glyphRun).BeginInit();
                glyphRun.AdvanceWidths = scaleAdvanceWidths;
                glyphRun.BaselineOrigin = BaselineOrign;
                glyphRun.BidiLevel = BidiLevel;
                glyphRun.CaretStops = caretStops;
                glyphRun.ClusterMap = clusterMap;
                glyphRun.FontRenderingEmSize = ScaleFontRenderingEmSize;
                glyphRun.GlyphIndices = glyphIndices;
                glyphRun.GlyphOffsets = glyphOffsets;
                glyphRun.GlyphTypeface = GlyphTypeface;
                glyphRun.IsSideways = IsSideways;
                glyphRun.Language = Language;
                glyphRun.Characters = characters;
                ((ISupportInitialize)glyphRun).EndInit();
            }
            else
            {
                glyphRun = new GlyphRun(GlyphTypeface, BidiLevel, IsSideways, ScaleFontRenderingEmSize, glyphIndices, BaselineOrign, scaleAdvanceWidths, glyphOffsets, characters, null, clusterMap, caretStops, Language);
            }

            return glyphRun;
        }

        #endregion

        #region Private Members

        //Enlarge the list of value of [0,1.0) to (-100,100).
        private void ExtendDoubleListRange(List<double> rateList)
        {
            for (int i = 0; i < rateList.Count; i++)
            {
                rateList[i] = ExtendDoubleRange(rateList[i]);
            }
        }

        //Enlarge value of [0,1.0) to (-100,100).
        private double ExtendDoubleRange(double rate)
        {
            return 2 * (rate - 0.5) * 100;
        }

        #endregion
    }
}
