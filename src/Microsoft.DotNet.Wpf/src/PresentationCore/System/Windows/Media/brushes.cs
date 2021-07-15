// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: A static class which contains well-known SolidColorBrushes.
//
//

using System.Windows.Media;
using MS.Internal;

using System;

namespace System.Windows.Media 
{
    /// <summary>
    /// Brushes - A collection of well-known SolidColorBrushes
    /// </summary>
    public sealed class Brushes
    {
        #region Constructors
        
        /// <summary>
        /// Private constructor - prevents instantiation.
        /// </summary>
        private Brushes() {}

        #endregion Constructors

        #region static Known SolidColorBrushes

        /// <summary>
        /// Well-known SolidColorBrush: AliceBlue
        /// </summary>
        public static SolidColorBrush AliceBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.AliceBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: AntiqueWhite
        /// </summary>
        public static SolidColorBrush AntiqueWhite
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.AntiqueWhite);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Aqua
        /// </summary>
        public static SolidColorBrush Aqua
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Aqua);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Aquamarine
        /// </summary>
        public static SolidColorBrush Aquamarine
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Aquamarine);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Azure
        /// </summary>
        public static SolidColorBrush Azure
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Azure);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Beige
        /// </summary>
        public static SolidColorBrush Beige
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Beige);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Bisque
        /// </summary>
        public static SolidColorBrush Bisque
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Bisque);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Black
        /// </summary>
        public static SolidColorBrush Black
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Black);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: BlanchedAlmond
        /// </summary>
        public static SolidColorBrush BlanchedAlmond
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.BlanchedAlmond);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Blue
        /// </summary>
        public static SolidColorBrush Blue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Blue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: BlueViolet
        /// </summary>
        public static SolidColorBrush BlueViolet
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.BlueViolet);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Brown
        /// </summary>
        public static SolidColorBrush Brown
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Brown);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: BurlyWood
        /// </summary>
        public static SolidColorBrush BurlyWood
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.BurlyWood);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: CadetBlue
        /// </summary>
        public static SolidColorBrush CadetBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.CadetBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Chartreuse
        /// </summary>
        public static SolidColorBrush Chartreuse
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Chartreuse);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Chocolate
        /// </summary>
        public static SolidColorBrush Chocolate
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Chocolate);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Coral
        /// </summary>
        public static SolidColorBrush Coral
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Coral);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: CornflowerBlue
        /// </summary>
        public static SolidColorBrush CornflowerBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.CornflowerBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Cornsilk
        /// </summary>
        public static SolidColorBrush Cornsilk
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Cornsilk);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Crimson
        /// </summary>
        public static SolidColorBrush Crimson
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Crimson);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Cyan
        /// </summary>
        public static SolidColorBrush Cyan
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Cyan);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkBlue
        /// </summary>
        public static SolidColorBrush DarkBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkCyan
        /// </summary>
        public static SolidColorBrush DarkCyan
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkCyan);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkGoldenrod
        /// </summary>
        public static SolidColorBrush DarkGoldenrod
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkGoldenrod);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkGray
        /// </summary>
        public static SolidColorBrush DarkGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkGreen
        /// </summary>
        public static SolidColorBrush DarkGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkKhaki
        /// </summary>
        public static SolidColorBrush DarkKhaki
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkKhaki);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkMagenta
        /// </summary>
        public static SolidColorBrush DarkMagenta
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkMagenta);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkOliveGreen
        /// </summary>
        public static SolidColorBrush DarkOliveGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkOliveGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkOrange
        /// </summary>
        public static SolidColorBrush DarkOrange
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkOrange);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkOrchid
        /// </summary>
        public static SolidColorBrush DarkOrchid
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkOrchid);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkRed
        /// </summary>
        public static SolidColorBrush DarkRed
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkRed);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkSalmon
        /// </summary>
        public static SolidColorBrush DarkSalmon
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkSalmon);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkSeaGreen
        /// </summary>
        public static SolidColorBrush DarkSeaGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkSeaGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkSlateBlue
        /// </summary>
        public static SolidColorBrush DarkSlateBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkSlateBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkSlateGray
        /// </summary>
        public static SolidColorBrush DarkSlateGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkSlateGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkTurquoise
        /// </summary>
        public static SolidColorBrush DarkTurquoise
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkTurquoise);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DarkViolet
        /// </summary>
        public static SolidColorBrush DarkViolet
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DarkViolet);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DeepPink
        /// </summary>
        public static SolidColorBrush DeepPink
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DeepPink);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DeepSkyBlue
        /// </summary>
        public static SolidColorBrush DeepSkyBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DeepSkyBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DimGray
        /// </summary>
        public static SolidColorBrush DimGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DimGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: DodgerBlue
        /// </summary>
        public static SolidColorBrush DodgerBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.DodgerBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Firebrick
        /// </summary>
        public static SolidColorBrush Firebrick
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Firebrick);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: FloralWhite
        /// </summary>
        public static SolidColorBrush FloralWhite
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.FloralWhite);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: ForestGreen
        /// </summary>
        public static SolidColorBrush ForestGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.ForestGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Fuchsia
        /// </summary>
        public static SolidColorBrush Fuchsia
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Fuchsia);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Gainsboro
        /// </summary>
        public static SolidColorBrush Gainsboro
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Gainsboro);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: GhostWhite
        /// </summary>
        public static SolidColorBrush GhostWhite
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.GhostWhite);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Gold
        /// </summary>
        public static SolidColorBrush Gold
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Gold);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Goldenrod
        /// </summary>
        public static SolidColorBrush Goldenrod
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Goldenrod);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Gray
        /// </summary>
        public static SolidColorBrush Gray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Gray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Green
        /// </summary>
        public static SolidColorBrush Green
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Green);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: GreenYellow
        /// </summary>
        public static SolidColorBrush GreenYellow
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.GreenYellow);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Honeydew
        /// </summary>
        public static SolidColorBrush Honeydew
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Honeydew);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: HotPink
        /// </summary>
        public static SolidColorBrush HotPink
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.HotPink);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: IndianRed
        /// </summary>
        public static SolidColorBrush IndianRed
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.IndianRed);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Indigo
        /// </summary>
        public static SolidColorBrush Indigo
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Indigo);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Ivory
        /// </summary>
        public static SolidColorBrush Ivory
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Ivory);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Khaki
        /// </summary>
        public static SolidColorBrush Khaki
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Khaki);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Lavender
        /// </summary>
        public static SolidColorBrush Lavender
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Lavender);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LavenderBlush
        /// </summary>
        public static SolidColorBrush LavenderBlush
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LavenderBlush);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LawnGreen
        /// </summary>
        public static SolidColorBrush LawnGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LawnGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LemonChiffon
        /// </summary>
        public static SolidColorBrush LemonChiffon
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LemonChiffon);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightBlue
        /// </summary>
        public static SolidColorBrush LightBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightCoral
        /// </summary>
        public static SolidColorBrush LightCoral
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightCoral);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightCyan
        /// </summary>
        public static SolidColorBrush LightCyan
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightCyan);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightGoldenrodYellow
        /// </summary>
        public static SolidColorBrush LightGoldenrodYellow
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightGoldenrodYellow);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightGray
        /// </summary>
        public static SolidColorBrush LightGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightGreen
        /// </summary>
        public static SolidColorBrush LightGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightPink
        /// </summary>
        public static SolidColorBrush LightPink
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightPink);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightSalmon
        /// </summary>
        public static SolidColorBrush LightSalmon
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightSalmon);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightSeaGreen
        /// </summary>
        public static SolidColorBrush LightSeaGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightSeaGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightSkyBlue
        /// </summary>
        public static SolidColorBrush LightSkyBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightSkyBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightSlateGray
        /// </summary>
        public static SolidColorBrush LightSlateGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightSlateGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightSteelBlue
        /// </summary>
        public static SolidColorBrush LightSteelBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightSteelBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LightYellow
        /// </summary>
        public static SolidColorBrush LightYellow
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LightYellow);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Lime
        /// </summary>
        public static SolidColorBrush Lime
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Lime);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: LimeGreen
        /// </summary>
        public static SolidColorBrush LimeGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.LimeGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Linen
        /// </summary>
        public static SolidColorBrush Linen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Linen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Magenta
        /// </summary>
        public static SolidColorBrush Magenta
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Magenta);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Maroon
        /// </summary>
        public static SolidColorBrush Maroon
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Maroon);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumAquamarine
        /// </summary>
        public static SolidColorBrush MediumAquamarine
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumAquamarine);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumBlue
        /// </summary>
        public static SolidColorBrush MediumBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumOrchid
        /// </summary>
        public static SolidColorBrush MediumOrchid
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumOrchid);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumPurple
        /// </summary>
        public static SolidColorBrush MediumPurple
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumPurple);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumSeaGreen
        /// </summary>
        public static SolidColorBrush MediumSeaGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumSeaGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumSlateBlue
        /// </summary>
        public static SolidColorBrush MediumSlateBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumSlateBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumSpringGreen
        /// </summary>
        public static SolidColorBrush MediumSpringGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumSpringGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumTurquoise
        /// </summary>
        public static SolidColorBrush MediumTurquoise
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumTurquoise);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MediumVioletRed
        /// </summary>
        public static SolidColorBrush MediumVioletRed
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MediumVioletRed);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MidnightBlue
        /// </summary>
        public static SolidColorBrush MidnightBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MidnightBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MintCream
        /// </summary>
        public static SolidColorBrush MintCream
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MintCream);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: MistyRose
        /// </summary>
        public static SolidColorBrush MistyRose
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.MistyRose);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Moccasin
        /// </summary>
        public static SolidColorBrush Moccasin
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Moccasin);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: NavajoWhite
        /// </summary>
        public static SolidColorBrush NavajoWhite
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.NavajoWhite);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Navy
        /// </summary>
        public static SolidColorBrush Navy
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Navy);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: OldLace
        /// </summary>
        public static SolidColorBrush OldLace
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.OldLace);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Olive
        /// </summary>
        public static SolidColorBrush Olive
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Olive);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: OliveDrab
        /// </summary>
        public static SolidColorBrush OliveDrab
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.OliveDrab);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Orange
        /// </summary>
        public static SolidColorBrush Orange
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Orange);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: OrangeRed
        /// </summary>
        public static SolidColorBrush OrangeRed
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.OrangeRed);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Orchid
        /// </summary>
        public static SolidColorBrush Orchid
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Orchid);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PaleGoldenrod
        /// </summary>
        public static SolidColorBrush PaleGoldenrod
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PaleGoldenrod);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PaleGreen
        /// </summary>
        public static SolidColorBrush PaleGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PaleGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PaleTurquoise
        /// </summary>
        public static SolidColorBrush PaleTurquoise
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PaleTurquoise);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PaleVioletRed
        /// </summary>
        public static SolidColorBrush PaleVioletRed
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PaleVioletRed);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PapayaWhip
        /// </summary>
        public static SolidColorBrush PapayaWhip
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PapayaWhip);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PeachPuff
        /// </summary>
        public static SolidColorBrush PeachPuff
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PeachPuff);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Peru
        /// </summary>
        public static SolidColorBrush Peru
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Peru);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Pink
        /// </summary>
        public static SolidColorBrush Pink
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Pink);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Plum
        /// </summary>
        public static SolidColorBrush Plum
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Plum);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: PowderBlue
        /// </summary>
        public static SolidColorBrush PowderBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.PowderBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Purple
        /// </summary>
        public static SolidColorBrush Purple
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Purple);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Red
        /// </summary>
        public static SolidColorBrush Red
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Red);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: RosyBrown
        /// </summary>
        public static SolidColorBrush RosyBrown
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.RosyBrown);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: RoyalBlue
        /// </summary>
        public static SolidColorBrush RoyalBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.RoyalBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SaddleBrown
        /// </summary>
        public static SolidColorBrush SaddleBrown
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SaddleBrown);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Salmon
        /// </summary>
        public static SolidColorBrush Salmon
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Salmon);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SandyBrown
        /// </summary>
        public static SolidColorBrush SandyBrown
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SandyBrown);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SeaGreen
        /// </summary>
        public static SolidColorBrush SeaGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SeaGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SeaShell
        /// </summary>
        public static SolidColorBrush SeaShell
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SeaShell);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Sienna
        /// </summary>
        public static SolidColorBrush Sienna
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Sienna);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Silver
        /// </summary>
        public static SolidColorBrush Silver
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Silver);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SkyBlue
        /// </summary>
        public static SolidColorBrush SkyBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SkyBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SlateBlue
        /// </summary>
        public static SolidColorBrush SlateBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SlateBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SlateGray
        /// </summary>
        public static SolidColorBrush SlateGray
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SlateGray);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Snow
        /// </summary>
        public static SolidColorBrush Snow
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Snow);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SpringGreen
        /// </summary>
        public static SolidColorBrush SpringGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SpringGreen);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: SteelBlue
        /// </summary>
        public static SolidColorBrush SteelBlue
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.SteelBlue);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Tan
        /// </summary>
        public static SolidColorBrush Tan
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Tan);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Teal
        /// </summary>
        public static SolidColorBrush Teal
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Teal);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Thistle
        /// </summary>
        public static SolidColorBrush Thistle
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Thistle);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Tomato
        /// </summary>
        public static SolidColorBrush Tomato
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Tomato);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Transparent
        /// </summary>
        public static SolidColorBrush Transparent
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Transparent);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Turquoise
        /// </summary>
        public static SolidColorBrush Turquoise
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Turquoise);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Violet
        /// </summary>
        public static SolidColorBrush Violet
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Violet);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Wheat
        /// </summary>
        public static SolidColorBrush Wheat
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Wheat);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: White
        /// </summary>
        public static SolidColorBrush White
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.White);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: WhiteSmoke
        /// </summary>
        public static SolidColorBrush WhiteSmoke
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.WhiteSmoke);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: Yellow
        /// </summary>
        public static SolidColorBrush Yellow
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.Yellow);
            }
        }

        /// <summary>
        /// Well-known SolidColorBrush: YellowGreen
        /// </summary>
        public static SolidColorBrush YellowGreen
        {
            get
            {
                return KnownColors.SolidColorBrushFromUint((uint)KnownColor.YellowGreen);
            }
        }

        #endregion static Known SolidColorBrushes
    }
}
