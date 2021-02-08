// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Rtf reader to convert the rtf content into Xaml content.
//

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.IO;
using System.Windows.Media; // Color
using Microsoft.Win32; // Registry for font substitutes
using MS.Internal; // Invariant
using MS.Internal.Text;


namespace System.Windows.Documents
{
    /// <summary>
    /// Horizontal alignment
    /// </summary>
    internal enum HAlign
    {
        AlignLeft,
        AlignRight,
        AlignCenter,
        AlignJustify,
        AlignDefault
    };

    /// <summary>
    /// Horizontal alignment
    /// </summary>
    internal enum VAlign
    {
        AlignTop,
        AlignCenter,
        AlignBottom,
    };

    /// <summary>
    /// Flow direction
    /// </summary>
    internal enum DirState
    {
        DirDefault,
        DirLTR,
        DirRTL
    };

    internal enum FontSlot
    {
        LOCH = 0,
        DBCH = 1,
        HICH = 2
    };

    /// <summary>
    /// Underline state
    /// </summary>
    internal enum ULState
    {
        ULNone,
        ULNormal,
        ULDot,
        ULDash,
        ULDashDot,
        ULDashDotDot,
        ULDouble,
        ULHeavyWave,
        ULLongDash,
        ULThick,
        ULThickDot,
        ULThickDash,
        ULThickDashDot,
        ULThickDashDotDot,
        ULThickLongDash,
        ULDoubleWave,
        ULWord,
        ULWave
    };

    /// <summary>
    /// Strikethrough state
    /// </summary>
    internal enum StrikeState
    {
        StrikeNone,
        StrikeNormal,
        StrikeDouble
    };

    /// <summary>
    /// Document node type
    /// WARNING: Keep in sync with array used by GetTagName
    /// </summary>
    internal enum DocumentNodeType
    {
        dnUnknown = 0,
        dnText,
        dnInline,
        dnLineBreak,
        dnHyperlink,
        dnParagraph,
        dnInlineUIContainer,
        dnBlockUIContainer,
        dnImage,
        dnList,
        dnListItem,
        dnTable,
        dnTableBody,
        dnRow,
        dnCell,
        dnSection,
        dnFigure,
        dnFloater,
        dnFieldBegin,
        dnFieldEnd,
        dnShape,
        dnListText
    };

    /// <summary>
    /// MarkerStyle
    /// </summary>
    internal enum MarkerStyle
    {
        MarkerNone = -1,
        MarkerArabic = 0,
        MarkerUpperRoman = 1,
        MarkerLowerRoman = 2,
        MarkerUpperAlpha = 3,
        MarkerLowerAlpha = 4,
        MarkerOrdinal = 5,
        MarkerCardinal = 6,
        MarkerBullet = 23,
        MarkerHidden = 255          // pseudo-value for no list text
    };

    /// <summary>
    /// Converters
    /// </summary>
    internal static class Converters
    {
        internal static double HalfPointToPositivePx(double halfPoint)
        {
            // Twip is 1/10 of half-point
            // So convert it to Twip by multiplying halftPoint with 10
            return TwipToPositivePx(halfPoint * 10);
        }

        internal static double TwipToPx(double twip)
        {
            return (twip / 1440f) * 96f;
        }

        internal static double TwipToPositivePx(double twip)
        {
            double px = (twip / 1440f) * 96f;
            if (px < 0)
            {
                px = 0;
            }
            return px;
        }

        internal static double TwipToPositiveVisiblePx(double twip)
        {
            double px = (twip / 1440f) * 96f;
            if (px < 0)
            {
                px = 0;
            }
            if (twip > 0.0 && px < 1.0)
            {
                px = 1.0;
            }
            return px;
        }

        internal static string TwipToPxString(double twip)
        {
            double px = TwipToPx(twip);
            return px.ToString("f2", CultureInfo.InvariantCulture);
        }

        internal static string TwipToPositivePxString(double twip)
        {
            double px = TwipToPositivePx(twip);
            return px.ToString("f2", CultureInfo.InvariantCulture);
        }

        internal static string TwipToPositiveVisiblePxString(double twip)
        {
            double px = TwipToPositiveVisiblePx(twip);
            return px.ToString("f2", CultureInfo.InvariantCulture);
        }

        internal static double PxToPt(double px)
        {
            return (px / 96) * 72f;
        }

        internal static long PxToTwipRounded(double px)
        {
            double twip = (px / 96f) * 1440f;
            if (twip < 0)
            {
                return (long)(twip - 0.5);
            }
            else
            {
                return (long)(twip + 0.5);
            }
        }

        internal static long PxToHalfPointRounded(double px)
        {
            // Twip is 1/10 of half-point
            double twip = (px / 96f) * 1440f;
            double halfPoint = twip / 10;
            if (halfPoint < 0)
            {
                return (long)(halfPoint - 0.5);
            }
            else
            {
                return (long)(halfPoint + 0.5);
            }
        }

        internal static bool StringToDouble(string s, ref double d)
        {
            bool ret = true;

            d = 0.0;
            try
            {
                d = System.Convert.ToDouble(s, CultureInfo.InvariantCulture);
            }
            catch (System.OverflowException)
            {
                ret = false;
            }
            catch (System.FormatException)
            {
                ret = false;
            }

            return ret;
        }

        internal static bool StringToInt(string s, ref int i)
        {
            bool ret = true;

            i = 0;
            try
            {
                i = System.Convert.ToInt32(s, CultureInfo.InvariantCulture);
            }
            catch (System.OverflowException)
            {
                ret = false;
            }
            catch (System.FormatException)
            {
                ret = false;
            }
            return ret;
        }

        internal static string StringToXMLAttribute(string s)
        {
            if (s.IndexOf('"') == -1)
            {
                return s;
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '"')
                {
                    sb.Append("&quot;");
                }
                else
                {
                    sb.Append(s[i]);
                }
            }

            return sb.ToString();
        }

        internal static bool HexStringToInt(string s, ref int i)
        {
            bool ret = true;

            i = 0;
            try
            {
                i = System.Int32.Parse(s, System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            }
            catch (System.OverflowException)
            {
                ret = false;
            }
            catch (System.FormatException)
            {
                ret = false;
            }
            return ret;
        }

        internal static string MarkerStyleToString(MarkerStyle ms)
        {
            switch (ms)
            {
                case MarkerStyle.MarkerArabic:
                    return "Decimal";
                case MarkerStyle.MarkerUpperRoman:
                    return "UpperRoman";
                case MarkerStyle.MarkerLowerRoman:
                    return "LowerRoman";
                case MarkerStyle.MarkerUpperAlpha:
                    return "UpperLatin";
                case MarkerStyle.MarkerLowerAlpha:
                    return "LowerLatin";
                case MarkerStyle.MarkerOrdinal:
                    return "Decimal";               // Note no XAML support
                case MarkerStyle.MarkerCardinal:
                    return "Decimal";               // Note no XAML support
                case MarkerStyle.MarkerHidden:
                    return "None";
                case MarkerStyle.MarkerBullet:
                    return "Disc";
                default:
                    return "Decimal";
            }
        }

        internal static string MarkerStyleToOldRTFString(MarkerStyle ms)
        {
            switch (ms)
            {
                case MarkerStyle.MarkerBullet:
                    // return "\\pnbidia";
                    // return "\\pnbidib";
                    return "\\pnlvlblt";
                default:
                case MarkerStyle.MarkerArabic:
                    // return "\\pndec";
                    return "\\pnlvlbody\\pndec";
                case MarkerStyle.MarkerCardinal:
                    return "\\pnlvlbody\\pncard";
                case MarkerStyle.MarkerUpperAlpha:
                    return "\\pnlvlbody\\pnucltr";
                case MarkerStyle.MarkerUpperRoman:
                    return "\\pnlvlbody\\pnucrm";
                case MarkerStyle.MarkerLowerAlpha:
                    return "\\pnlvlbody\\pnlcltr";
                case MarkerStyle.MarkerLowerRoman:
                    return "\\pnlvlbody\\pnlcrm";
                case MarkerStyle.MarkerOrdinal:
                    //return "\\pnlvlbody\\pnordt";
                    return "\\pnlvlbody\\pnord";
            }
        }

        // Convert FG, BG and shading to produce color to fill with.
        // Shading is in 100'ths of a percent (ie, 10000 is 100%)
        // A value of zero for shading means use all CB.
        // A value of 10000 for shading means use all CF.
        // Intermediate values mean some combination of
        internal static bool ColorToUse(ConverterState converterState, long cb, long cf, long shade, ref Color c)
        {
            ColorTableEntry entryCB = cb >= 0 ? converterState.ColorTable.EntryAt((int)cb) : null;
            ColorTableEntry entryCF = cf >= 0 ? converterState.ColorTable.EntryAt((int)cf) : null;

            // No shading
            if (shade < 0)
            {
                if (entryCB == null)
                {
                    return false;
                }
                else
                {
                    c = entryCB.Color;
                    return true;
                }
            }

            // Shading
            else
            {
                Color cCB = entryCB != null ? entryCB.Color : Color.FromArgb(0xFF, 0, 0, 0);
                Color cCF = entryCF != null ? entryCF.Color : Color.FromArgb(0xFF, 255, 255, 255);

                // No color specifies means shading is treated as a grey intensity.
                if (entryCF == null && entryCB == null)
                {
                    c = Color.FromArgb(0xff,
                                      (byte)(255 - (255 * shade / 10000)),
                                      (byte)(255 - (255 * shade / 10000)),
                                      (byte)(255 - (255 * shade / 10000)));
                    return true;
                }

                // Only CF means CF fades as shading goes from 10,000 to 0
                else if (entryCB == null)
                {
                    c = Color.FromArgb(0xff,
                                        (byte)(cCF.R + ((255 - cCF.R) * (10000 - shade) / 10000)),
                                        (byte)(cCF.G + ((255 - cCF.G) * (10000 - shade) / 10000)),
                                        (byte)(cCF.B + ((255 - cCF.B) * (10000 - shade) / 10000)));
                    return true;
                }

                // Only CB means CB gets larger impact (from black ) as shading goes from 10000 to 0
                else if (entryCF == null)
                {
                    c = Color.FromArgb(0xff,
                                        (byte)(cCB.R - (cCB.R * shade / 10000)),
                                        (byte)(cCB.G - (cCB.G * shade / 10000)),
                                        (byte)(cCB.B - (cCB.B * shade / 10000)));
                    return true;
                }

                // Both - need to mix colors
                else
                {
                    c = Color.FromArgb(0xff,
                                      (byte)((cCB.R * (10000 - shade) / 10000) +
                                              (cCF.R * shade / 10000)),
                                      (byte)((cCB.G * (10000 - shade) / 10000) +
                                              (cCF.G * shade / 10000)),
                                      (byte)((cCB.B * (10000 - shade) / 10000) +
                                              (cCF.B * shade / 10000)));
                    return true;
                }
            }
        }

        internal static string AlignmentToString(HAlign a, DirState ds)
        {
            switch (a)
            {
                case HAlign.AlignLeft:
                    return (ds != DirState.DirRTL) ? "Left" : "Right";
                case HAlign.AlignRight:
                    return (ds != DirState.DirRTL) ? "Right" : "Left";
                case HAlign.AlignCenter:
                    return "Center";
                case HAlign.AlignJustify:
                    return "Justify";
                case HAlign.AlignDefault:
                default:
                    return "";
            }
        }

        internal static string MarkerCountToString(MarkerStyle ms, long nCount)
        {
            StringBuilder sb = new StringBuilder();

            if (nCount < 0)
            {
                nCount = 0;
            }

            switch (ms)
            {
                case MarkerStyle.MarkerUpperRoman:
                case MarkerStyle.MarkerLowerRoman:
                    {
                        return MarkerRomanCountToString(sb, ms, nCount);
                    }

                case MarkerStyle.MarkerLowerAlpha:
                case MarkerStyle.MarkerUpperAlpha:
                    {
                        return MarkerAlphaCountToString(sb, ms, nCount);
                    }

                case MarkerStyle.MarkerArabic:
                case MarkerStyle.MarkerOrdinal:
                case MarkerStyle.MarkerCardinal:
                    return nCount.ToString(CultureInfo.InvariantCulture);

                case MarkerStyle.MarkerHidden:
                case MarkerStyle.MarkerNone:
                    return "";

                case MarkerStyle.MarkerBullet:
                default:
                    return "\\'B7";
            }
        }

        private static string MarkerRomanCountToString(StringBuilder sb, MarkerStyle ms, long nCount)
        {
            while (nCount >= 1000)
            {
                sb.Append("M");
                nCount -= 1000;
            }

            // 100's
            switch (nCount / 100)
            {
                case 9:
                    sb.Append("CM"); break;
                case 8:
                    sb.Append("DCCC"); break;
                case 7:
                    sb.Append("DCC"); break;
                case 6:
                    sb.Append("DC"); break;
                case 5:
                    sb.Append("D"); break;
                case 4:
                    sb.Append("CD"); break;
                case 3:
                    sb.Append("CCC"); break;
                case 2:
                    sb.Append("CC"); break;
                case 1:
                    sb.Append("C"); break;
                case 0:
                    break;
            }
            nCount = nCount % 100;

            // 10's
            switch (nCount / 10)
            {
                case 9:
                    sb.Append("XC"); break;
                case 8:
                    sb.Append("LXXX"); break;
                case 7:
                    sb.Append("LXX"); break;
                case 6:
                    sb.Append("LX"); break;
                case 5:
                    sb.Append("L"); break;
                case 4:
                    sb.Append("XL"); break;
                case 3:
                    sb.Append("XXX"); break;
                case 2:
                    sb.Append("XX"); break;
                case 1:
                    sb.Append("X"); break;
                case 0:
                    break;
            }
            nCount = nCount % 10;

            // 1's
            switch (nCount)
            {
                case 9:
                    sb.Append("IX"); break;
                case 8:
                    sb.Append("VIII"); break;
                case 7:
                    sb.Append("VII"); break;
                case 6:
                    sb.Append("VI"); break;
                case 5:
                    sb.Append("V"); break;
                case 4:
                    sb.Append("IV"); break;
                case 3:
                    sb.Append("III"); break;
                case 2:
                    sb.Append("II"); break;
                case 1:
                    sb.Append("I"); break;
                case 0:
                    break;
            }

            if (ms == MarkerStyle.MarkerUpperRoman)
            {
                return sb.ToString();
            }
            else
            {
                return sb.ToString().ToLower(CultureInfo.InvariantCulture);
            }
        }

        private static string MarkerAlphaCountToString(StringBuilder sb, MarkerStyle ms, long nCount)
        {
            int toThe1 = 26;
            int toThe2 = 676;
            int toThe3 = 17576;
            int toThe4 = 456976;

            char[] ca = new char[1];
            int temp;

            temp = 0;
            while (nCount > toThe4 + toThe3 + toThe2 + toThe1)
            {
                temp++;
                nCount -= toThe4;
            }
            if (temp > 0)
            {
                if (temp > 26) temp = 26;
                ca[0] = (char)('A' + (temp - 1));
                sb.Append(ca);
            }

            temp = 0;
            while (nCount > toThe3 + toThe2 + toThe1)
            {
                temp++;
                nCount -= toThe3;
            }
            if (temp > 0)
            {
                ca[0] = (char)('A' + (temp - 1));
                sb.Append(ca);
            }

            temp = 0;
            while (nCount > toThe2 + toThe1)
            {
                temp++;
                nCount -= toThe2;
            }
            if (temp > 0)
            {
                ca[0] = (char)('A' + (temp - 1));
                sb.Append(ca);
            }

            temp = 0;
            while (nCount > toThe1)
            {
                temp++;
                nCount -= toThe1;
            }
            if (temp > 0)
            {
                ca[0] = (char)('A' + (temp - 1));
                sb.Append(ca);
            }

            ca[0] = (char)('A' + (nCount - 1));
            sb.Append(ca);

            if (ms == MarkerStyle.MarkerUpperAlpha)
            {
                return sb.ToString();
            }
            else
            {
                return sb.ToString().ToLower(CultureInfo.InvariantCulture);
            }
        }

        // Convert byte to the hex data(0x3a ==> 0x33 and 0x61)
        internal static void ByteToHex(byte byteData, out byte firstHexByte, out byte secondHexByte)
        {
            firstHexByte = (byte)((byteData >> 4) & 0x0f);
            secondHexByte = (byte)(byteData & 0x0f);

            // First hex digit
            if (firstHexByte >= 0x00 && firstHexByte <= 0x09)
            {
                firstHexByte += 0x30;
            }
            else if (firstHexByte >= 0xa && firstHexByte <= 0xf)
            {
                firstHexByte += 'a' - 0xa;
            }

            // Second hex digit
            if (secondHexByte >= 0x00 && secondHexByte <= 0x09)
            {
                secondHexByte += 0x30;
            }
            else if (secondHexByte >= 0xa && secondHexByte <= 0xf)
            {
                secondHexByte += 'a' - 0xa;
            }
        }
    };

    internal static class Validators
    {
        internal static bool IsValidFontSize(long fs)
        {
            return fs >= 0 && fs <= 0x7FFF;
        }

        internal static bool IsValidWidthType(long wt)
        {
            return wt >= 0 && wt <= 3;
        }

        internal static long MakeValidShading(long s)
        {
            if (s > 10000) s = 10000;
            return s;
        }

        internal static long MakeValidBorderWidth(long w)
        {
            // Word's UI only supports values from 0 to 120.  But it will actually render larger
            // values, so let's maintain them through the converter (but still have some kind of limit).
            if (w < 0)
            {
                w = 0;
            }
            if (w > 1440)
            {
                w = 1440;
            }
            return w;
        }
    };

    /// <summary>
    /// FormatState
    /// </summary>
    internal class FormatState
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FormatState()
        {
            // Other settings
            _dest = RtfDestination.DestNormal;
            _stateSkip = 1;

            // Font settings
            SetCharDefaults();

            // Para settings
            SetParaDefaults();

            // Row settings
            SetRowDefaults();
        }

        internal FormatState(FormatState formatState)
        {
            // Font settings
            Bold = formatState.Bold;
            Italic = formatState.Italic;
            Engrave = formatState.Engrave;
            Shadow = formatState.Shadow;
            SCaps = formatState.SCaps;
            Outline = formatState.Outline;
            Super = formatState.Super;
            Sub = formatState.Sub;
            SuperOffset = formatState.SuperOffset;
            FontSize = formatState.FontSize;
            Font = formatState.Font;
            CodePage = formatState.CodePage;
            CF = formatState.CF;
            CB = formatState.CB;
            DirChar = formatState.DirChar;
            UL = formatState.UL;
            Strike = formatState.Strike;
            Expand = formatState.Expand;
            Lang = formatState.Lang;
            LangFE = formatState.LangFE;
            LangCur = formatState.LangCur;
            FontSlot = formatState.FontSlot;

            // Para settings
            SB = formatState.SB;
            SA = formatState.SA;
            FI = formatState.FI;
            RI = formatState.RI;
            LI = formatState.LI;
            SL = formatState.SL;
            SLMult = formatState.SLMult;
            HAlign = formatState.HAlign;
            ILVL = formatState.ILVL;
            ITAP = formatState.ITAP;
            ILS = formatState.ILS;
            DirPara = formatState.DirPara;
            CFPara = formatState.CFPara;
            CBPara = formatState.CBPara;
            ParaShading = formatState.ParaShading;
            Marker = formatState.Marker;
            IsContinue = formatState.IsContinue;
            StartIndex = formatState.StartIndex;
            StartIndexDefault = formatState.StartIndexDefault;
            IsInTable = formatState.IsInTable;
            _pb = formatState.HasParaBorder ? new ParaBorder(formatState.ParaBorder) : null;

            // Row settings
            // For performance reasons, we don't make a full copy of the Row format information.  The implication
            // of this is that changes to row format data will propagate back up from nested scopes, which is not
            // according to the strict semantics.  But in practice, all new rows are explicitly cleared with the
            // \trowd keyword, which clears this, so this should be fine.
            RowFormat = formatState._rowFormat;

            // Other
            RtfDestination = formatState.RtfDestination;
            IsHidden = formatState.IsHidden;

            _stateSkip = formatState.UnicodeSkip;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void SetCharDefaults()
        {
            _fBold = false;
            _fItalic = false;
            _fEngrave = false;
            _fShadow = false;
            _fScaps = false;
            _fOutline = false;
            _fSub = false;
            _fSuper = false;
            _superOffset = 0;
            _fs = 24;           // Default for RTF
            _font = -1;
            _codePage = -1;
            _cf = -1;
            _cb = -1;
            _dirChar = DirState.DirLTR;
            _ul = ULState.ULNone;
            _strike = StrikeState.StrikeNone;
            _expand = 0;
            _fHidden = false;
            _lang = -1;
            _langFE = -1;
            _langCur = -1;
            _fontSlot = FontSlot.LOCH;
        }

        internal void SetParaDefaults()
        {
            _sb = 0;
            _sa = 0;
            _fi = 0;
            _ri = 0;
            _li = 0;
            _align = HAlign.AlignDefault;
            _ilvl = 0;
            _pnlvl = 0;
            _itap = 0;
            _ils = -1;
            _dirPara = DirState.DirLTR;
            _cbPara = -1;
            _nParaShading = -1;
            _cfPara = -1;
            _marker = MarkerStyle.MarkerNone;
            _fContinue = false;
            _nStartIndex = -1;
            _nStartIndexDefault = -1;
            _sl = 0;
            _slMult = false;
            _pb = null;

            _fInTable = false;
        }

        internal void SetRowDefaults()
        {
            // Just toss old info
            RowFormat = null;
        }

        internal bool IsEqual(FormatState formatState)
        {
            return
                // Font Settings
                   Bold == formatState.Bold
                && Italic == formatState.Italic
                && Engrave == formatState.Engrave
                && Shadow == formatState.Shadow
                && SCaps == formatState.SCaps
                && Outline == formatState.Outline
                && Super == formatState.Super
                && Sub == formatState.Sub
                && SuperOffset == formatState.SuperOffset
                && FontSize == formatState.FontSize
                && Font == formatState.Font
                && CodePage == formatState.CodePage
                && CF == formatState.CF
                && CB == formatState.CB
                && DirChar == formatState.DirChar
                && UL == formatState.UL
                && Strike == formatState.Strike
                && Expand == formatState.Expand
                && Lang == formatState.Lang
                && LangFE == formatState.LangFE
                && LangCur == formatState.LangCur
                && FontSlot == formatState.FontSlot

                // Para settings
                && SB == formatState.SB
                && SA == formatState.SA
                && FI == formatState.FI
                && RI == formatState.RI
                && LI == formatState.LI
                && HAlign == formatState.HAlign
                && ILVL == formatState.ILVL
                && ITAP == formatState.ITAP
                && ILS == formatState.ILS
                && DirPara == formatState.DirPara
                && CFPara == formatState.CFPara
                && CBPara == formatState.CBPara
                && ParaShading == formatState.ParaShading
                && Marker == formatState.Marker
                && IsContinue == formatState.IsContinue
                && StartIndex == formatState.StartIndex
                && StartIndexDefault == formatState.StartIndexDefault
                && SL == formatState.SL
                && SLMult == formatState.SLMult
                && IsInTable == formatState.IsInTable

                // Don't include para borders in this test

                // Row Settings
                // Don't include row settings in this test.

                // Other
                && RtfDestination == formatState.RtfDestination
                && IsHidden == formatState.IsHidden
                && UnicodeSkip == formatState.UnicodeSkip;
        }

        static internal FormatState EmptyFormatState
        {
            get
            {
                if (_fsEmptyState == null)
                {
                    _fsEmptyState = new FormatState();
                    _fsEmptyState.FontSize = -1;
                }

                return _fsEmptyState;
            }
        }

        internal string GetBorderAttributeString(ConverterState converterState)
        {
            if (HasParaBorder)
            {
                return ParaBorder.GetBorderAttributeString(converterState);
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // Random Stacked Properties
        internal RtfDestination RtfDestination
        {
            get
            {
                return _dest;
            }
            set
            {
                _dest = value;
            }
        }

        internal bool IsHidden
        {
            get
            {
                return _fHidden;
            }
            set
            {
                _fHidden = value;
            }
        }

        internal bool IsContentDestination
        {
            get
            {
                return _dest == RtfDestination.DestNormal
                    || _dest == RtfDestination.DestFieldResult
                    || _dest == RtfDestination.DestShapeResult
                    || _dest == RtfDestination.DestShape
                    || _dest == RtfDestination.DestListText;
            }
        }

        // Character Style
        internal bool Bold
        {
            get
            {
                return _fBold;
            }
            set
            {
                _fBold = value;
            }
        }

        internal bool Italic
        {
            get
            {
                return _fItalic;
            }
            set
            {
                _fItalic = value;
            }
        }

        internal bool Engrave
        {
            get
            {
                return _fEngrave;
            }
            set
            {
                _fEngrave = value;
            }
        }

        internal bool Shadow
        {
            get
            {
                return _fShadow;
            }
            set
            {
                _fShadow = value;
            }
        }

        internal bool SCaps
        {
            get
            {
                return _fScaps;
            }
            set
            {
                _fScaps = value;
            }
        }

        internal bool Outline
        {
            get
            {
                return _fOutline;
            }
            set
            {
                _fOutline = value;
            }
        }

        internal bool Sub
        {
            get
            {
                return _fSub;
            }
            set
            {
                _fSub = value;
            }
        }

        internal bool Super
        {
            get
            {
                return _fSuper;
            }
            set
            {
                _fSuper = value;
            }
        }

        internal long SuperOffset
        {
            get
            {
                return _superOffset;
            }
            set
            {
                _superOffset = value;
            }
        }

        internal long FontSize
        {
            get
            {
                return _fs;
            }
            set
            {
                _fs = value;
            }
        }

        internal long Font
        {
            get
            {
                return _font;
            }
            set
            {
                _font = value;
            }
        }

        internal int CodePage
        {
            get
            {
                return _codePage;
            }
            set
            {
                _codePage = value;
            }
        }

        internal long CF
        {
            get
            {
                return _cf;
            }
            set
            {
                _cf = value;
            }
        }

        internal long CB
        {
            get
            {
                return _cb;
            }
            set
            {
                _cb = value;
            }
        }

        internal DirState DirChar
        {
            get
            {
                return _dirChar;
            }
            set
            {
                _dirChar = value;
            }
        }

        internal ULState UL
        {
            get
            {
                return _ul;
            }
            set
            {
                _ul = value;
            }
        }

        internal StrikeState Strike
        {
            get
            {
                return _strike;
            }
            set
            {
                _strike = value;
            }
        }

        internal long Expand
        {
            get
            {
                return _expand;
            }
            set
            {
                _expand = value;
            }
        }

        internal long Lang
        {
            get
            {
                return _lang;
            }
            set
            {
                _lang = value;
            }
        }

        internal long LangFE
        {
            get
            {
                return _langFE;
            }
            set
            {
                _langFE = value;
            }
        }

        internal long LangCur
        {
            get
            {
                return _langCur;
            }
            set
            {
                _langCur = value;
            }
        }

        internal FontSlot FontSlot
        {
            get
            {
                return _fontSlot;
            }
            set
            {
                _fontSlot = value;
            }
        }

        // Paragraph Style
        internal long SB
        {
            get
            {
                return _sb;
            }
            set
            {
                _sb = value;
            }
        }

        internal long SA
        {
            get
            {
                return _sa;
            }
            set
            {
                _sa = value;
            }
        }

        internal long FI
        {
            get
            {
                return _fi;
            }
            set
            {
                _fi = value;
            }
        }

        internal long RI
        {
            get
            {
                return _ri;
            }
            set
            {
                _ri = value;
            }
        }

        internal long LI
        {
            get
            {
                return _li;
            }
            set
            {
                _li = value;
            }
        }

        internal HAlign HAlign
        {
            get
            {
                return _align;
            }
            set
            {
                _align = value;
            }
        }

        internal long ILVL
        {
            get
            {
                return _ilvl;
            }
            set
            {
                if (value >= 0 && value <= MAX_LIST_DEPTH)
                {
                    _ilvl = value;
                }
            }
        }

        internal long PNLVL
        {
            get
            {
                return _pnlvl;
            }
            set
            {
                _pnlvl = value;
            }
        }

        internal long ITAP
        {
            get
            {
                return _itap;
            }
            set
            {
                if (value >= 0 && value <= MAX_TABLE_DEPTH)
                {
                    _itap = value;
                }
            }
        }

        internal long ILS
        {
            get
            {
                return _ils;
            }
            set
            {
                _ils = value;
            }
        }

        internal DirState DirPara
        {
            get
            {
                return _dirPara;
            }
            set
            {
                _dirPara = value;
            }
        }

        internal long CFPara
        {
            get
            {
                return _cfPara;
            }
            set
            {
                _cfPara = value;
            }
        }

        internal long CBPara
        {
            get
            {
                return _cbPara;
            }
            set
            {
                _cbPara = value;
            }
        }

        internal long ParaShading
        {
            get
            {
                return _nParaShading;
            }
            set
            {
                _nParaShading = Validators.MakeValidShading(value);
            }
        }

        internal MarkerStyle Marker
        {
            get
            {
                return _marker;
            }
            set
            {
                _marker = value;
            }
        }

        internal bool IsContinue
        {
            get
            {
                return _fContinue;
            }
            set
            {
                _fContinue = value;
            }
        }

        internal long StartIndex
        {
            get
            {
                return _nStartIndex;
            }
            set
            {
                _nStartIndex = value;
            }
        }

        internal long StartIndexDefault
        {
            get
            {
                return _nStartIndexDefault;
            }
            set
            {
                _nStartIndexDefault = value;
            }
        }

        internal long SL
        {
            get
            {
                return _sl;
            }
            set
            {
                _sl = value;
            }
        }

        internal bool SLMult
        {
            get
            {
                return _slMult;
            }
            set
            {
                _slMult = value;
            }
        }

        internal bool IsInTable
        {
            get
            {
                return _fInTable;
            }
            set
            {
                _fInTable = value;
            }
        }

        internal long TableLevel
        {
            get
            {
                if (_fInTable || _itap > 0)
                {
                    return _itap > 0 ? _itap : 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal long ListLevel
        {
            get
            {
                if (_ils >= 0 || _ilvl > 0)
                {
                    return _ilvl > 0 ? _ilvl + 1 : 1;
                }
                else if (PNLVL > 0)
                {
                    return PNLVL;
                }
                else if (_marker != MarkerStyle.MarkerNone)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal int UnicodeSkip
        {
            get
            {
                return _stateSkip;
            }
            set
            {
                if (value >= 0 && value < 0xffff)
                {
                    _stateSkip = value;
                }
            }
        }

        internal RowFormat RowFormat
        {
            get
            {
                // Allocate on access.
                if (_rowFormat == null)
                {
                    _rowFormat = new RowFormat();
                }

                return _rowFormat;
            }
            set
            {
                _rowFormat = value;
            }
        }

        internal bool HasRowFormat
        {
            get
            {
                return _rowFormat != null;
            }
        }

        internal ParaBorder ParaBorder
        {
            get
            {
                if (_pb == null)
                {
                    _pb = new ParaBorder();
                }
                return _pb;
            }
        }

        internal bool HasParaBorder
        {
            get
            {
                return _pb != null && !_pb.IsNone;
            }
        }

        // Image type property
        internal RtfImageFormat ImageFormat
        {
            get
            {
                return _imageFormat;
            }
            set
            {
                _imageFormat = value;
            }
        }

        // Image source name property
        internal string ImageSource
        {
            get
            {
                return _imageSource;
            }
            set
            {
                _imageSource = value;
            }
        }

        // Image width property
        internal double ImageWidth
        {
            get
            {
                return _imageWidth;
            }
            set
            {
                _imageWidth = value;
            }
        }

        // Image height property
        internal double ImageHeight
        {
            get
            {
                return _imageHeight;
            }
            set
            {
                _imageHeight = value;
            }
        }

        internal double ImageBaselineOffset
        {
            get
            {
                return _imageBaselineOffset;
            }
            set
            {
                _imageBaselineOffset = value;
            }
        }

        internal bool IncludeImageBaselineOffset
        {
            get
            {
                return _isIncludeImageBaselineOffset;
            }
            set
            {
                _isIncludeImageBaselineOffset = value;
            }
        }

        // Image width property
        internal double ImageScaleWidth
        {
            get
            {
                return _imageScaleWidth;
            }
            set
            {
                _imageScaleWidth = value;
            }
        }

        // Image height property
        internal double ImageScaleHeight
        {
            get
            {
                return _imageScaleHeight;
            }
            set
            {
                _imageScaleHeight = value;
            }
        }

        // IsImageDataBinary: The default is false that is hex data for image
        internal bool IsImageDataBinary
        {
            get
            {
                return _isImageDataBinary;
            }
            set
            {
                _isImageDataBinary = value;
            }
        }

        // Image stretch property to apply scale factor
        internal string ImageStretch
        {
            get
            {
                return _imageStretch;
            }
            set
            {
                _imageStretch = value;
            }
        }

        // Image stretch direction property to apply scale factor
        internal string ImageStretchDirection
        {
            get
            {
                return _imageStretchDirection;
            }
            set
            {
                _imageStretchDirection = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private RtfDestination _dest;
        private bool _fBold;
        private bool _fItalic;
        private bool _fSuper;
        private bool _fSub;
        private bool _fOutline;
        private bool _fEngrave;
        private bool _fShadow;
        private bool _fScaps;

        private long _fs;                // Font size in half points
        private long _font;              // Index into font table
        private int _codePage;           // Cache of code page from font
        private long _superOffset;       // Sub/super offset
        private long _cf;                // Foreground color index
        private long _cb;                // Background color index
        private DirState _dirChar;       // Character level direction
        private ULState _ul;
        private StrikeState _strike;
        private long _expand;
        private long _lang;
        private long _langFE;
        private long _langCur;
        private FontSlot _fontSlot;

        // Para style flags and values
        private long _sa;                // Space After
        private long _sb;                // Space Before
        private long _li;                // Left Indent
        private long _ri;               // Right Indent
        private long _fi;                // First Indent
        private HAlign _align;            // Paragraph alignment
        private long _ils;                // List override index
        private long _ilvl;                // 0-based List level
        private long _pnlvl;                // 1-based old style List level
        private long _itap;                // Table level
        private DirState _dirPara;        // Paragraph level direction
        private long _cfPara;            // Paragraph fill color
        private long _cbPara;            // Paragraph pattern background color
        private long _nParaShading;     // Paragraph shading in 100's of a percent
        private MarkerStyle _marker;    // Type of bullet for old-style RTF
        private bool _fContinue;        // Continue list numbering? (without number)
        private long _nStartIndex;      // List start index
        private long _nStartIndexDefault;      // List start index default value
        private long _sl;
        private bool _slMult;
        private ParaBorder _pb;

        private bool _fInTable;         // Paragraph is in table
        private bool _fHidden;          // Hidden text

        private int _stateSkip;

        private RowFormat _rowFormat;

        private static FormatState _fsEmptyState = null;

        // Image property fields
        private RtfImageFormat _imageFormat;
        private string _imageSource;
        private double _imageWidth;
        private double _imageHeight;
        private double _imageBaselineOffset;
        private bool _isIncludeImageBaselineOffset = false;
        private double _imageScaleWidth;
        private double _imageScaleHeight;
        private bool _isImageDataBinary;
        private string _imageStretch;
        private string _imageStretchDirection;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Const
        //
        //------------------------------------------------------

        #region Private Const

        private const int MAX_LIST_DEPTH = 32;
        private const int MAX_TABLE_DEPTH = 32;

        #endregion Private Const
    }

    internal enum BorderType
    {
        BorderNone,
        BorderSingle,
        BorderDouble
        // ... lots more
    }

    internal class BorderFormat
    {
        internal BorderFormat()
        {
            SetDefaults();
        }

        internal BorderFormat(BorderFormat cb)
        {
            CF = cb.CF;
            Width = cb.Width;
            Type = cb.Type;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal long CF
        {
            get
            {
                return _cf;
            }
            set
            {
                _cf = value;
            }
        }

        internal long Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = Validators.MakeValidBorderWidth(value);
            }
        }

        internal long EffectiveWidth
        {
            get
            {
                switch (Type)
                {
                    case BorderType.BorderNone: return 0;
                    case BorderType.BorderDouble: return Width * 2;
                    default:
                    case BorderType.BorderSingle: return Width;
                }
            }
        }

        internal BorderType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        internal bool IsNone
        {
            get
            {
                return EffectiveWidth <= 0 || Type == BorderType.BorderNone;
            }
        }

        internal string RTFEncoding
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (IsNone)
                {
                    sb.Append("\\brdrnone");
                }
                else
                {
                    sb.Append("\\brdrs\\brdrw");
                    sb.Append(EffectiveWidth.ToString(CultureInfo.InvariantCulture));
                    if (CF >= 0)
                    {
                        sb.Append("\\brdrcf");
                        sb.Append(CF.ToString(CultureInfo.InvariantCulture));
                    }
                }

                return sb.ToString();
            }
        }

        static internal BorderFormat EmptyBorderFormat
        {
            get
            {
                if (_emptyBorderFormat == null)
                {
                    _emptyBorderFormat = new BorderFormat();
                }
                return _emptyBorderFormat;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void SetDefaults()
        {
            _cf = -1;
            _width = 0;
            _type = BorderType.BorderNone;
        }

        #endregion Internal Methods

        private long _cf;
        private long _width;
        private BorderType _type;
        static private BorderFormat _emptyBorderFormat = null;
    }

    internal class ParaBorder
    {
        internal ParaBorder()
        {
            BorderLeft = new BorderFormat();
            BorderTop = new BorderFormat();
            BorderRight = new BorderFormat();
            BorderBottom = new BorderFormat();
            BorderAll = new BorderFormat();
            Spacing = 0;
        }

        internal ParaBorder(ParaBorder pb)
        {
            BorderLeft = new BorderFormat(pb.BorderLeft);
            BorderTop = new BorderFormat(pb.BorderTop);
            BorderRight = new BorderFormat(pb.BorderRight);
            BorderBottom = new BorderFormat(pb.BorderBottom);
            BorderAll = new BorderFormat(pb.BorderAll);
            Spacing = pb.Spacing;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal BorderFormat BorderLeft
        {
            get
            {
                return _bfLeft;
            }
            set
            {
                _bfLeft = value;
            }
        }

        internal BorderFormat BorderTop
        {
            get
            {
                return _bfTop;
            }
            set
            {
                _bfTop = value;
            }
        }

        internal BorderFormat BorderRight
        {
            get
            {
                return _bfRight;
            }
            set
            {
                _bfRight = value;
            }
        }

        internal BorderFormat BorderBottom
        {
            get
            {
                return _bfBottom;
            }
            set
            {
                _bfBottom = value;
            }
        }

        internal BorderFormat BorderAll
        {
            get
            {
                return _bfAll;
            }
            set
            {
                _bfAll = value;
            }
        }

        internal long Spacing
        {
            get
            {
                return _nSpacing;
            }
            set
            {
                _nSpacing = value;
            }
        }

        internal long CF
        {
            get
            {
                return BorderLeft.CF;
            }
            set
            {
                BorderLeft.CF = value;
                BorderTop.CF = value;
                BorderRight.CF = value;
                BorderBottom.CF = value;
                BorderAll.CF = value;
            }
        }

        internal bool IsNone
        {
            get
            {
                return BorderLeft.IsNone && BorderTop.IsNone
                    && BorderRight.IsNone && BorderBottom.IsNone
                    && BorderAll.IsNone;
            }
        }

        internal string GetBorderAttributeString(ConverterState converterState)
        {
            if (IsNone)
            {
                return string.Empty;
            }

            // Build the border attribute string based on border values
            StringBuilder sb = new StringBuilder();

            // Left,Top,Right,Bottom
            sb.Append(" BorderThickness=\"");
            if (!BorderAll.IsNone)
            {
                sb.Append(Converters.TwipToPositiveVisiblePxString(BorderAll.EffectiveWidth));
            }
            else
            {
                sb.Append(Converters.TwipToPositiveVisiblePxString(BorderLeft.EffectiveWidth));
                sb.Append(",");
                sb.Append(Converters.TwipToPositiveVisiblePxString(BorderTop.EffectiveWidth));
                sb.Append(",");
                sb.Append(Converters.TwipToPositiveVisiblePxString(BorderRight.EffectiveWidth));
                sb.Append(",");
                sb.Append(Converters.TwipToPositiveVisiblePxString(BorderBottom.EffectiveWidth));
            }
            sb.Append("\"");

            ColorTableEntry entry = null;
            if (CF >= 0)
            {
                entry = converterState.ColorTable.EntryAt((int)CF);
            }

            if (entry != null)
            {
                sb.Append(" BorderBrush=\"");
                sb.Append(entry.Color.ToString());
                sb.Append("\"");
            }
            else
            {
                sb.Append(" BorderBrush=\"#FF000000\"");
            }

            if (Spacing != 0)
            {
                sb.Append(" Padding=\"");
                sb.Append(Converters.TwipToPositivePxString(Spacing));
                sb.Append("\"");
            }

            return sb.ToString();
        }

        internal string RTFEncoding
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (IsNone)
                {
                    sb.Append("\\brdrnil");
                }
                else
                {
                    sb.Append("\\brdrl");
                    sb.Append(BorderLeft.RTFEncoding);
                    if (BorderLeft.CF >= 0)
                    {
                        sb.Append("\\brdrcf");
                        sb.Append(BorderLeft.CF.ToString(CultureInfo.InvariantCulture));
                    }
                    sb.Append("\\brdrt");
                    sb.Append(BorderTop.RTFEncoding);
                    if (BorderTop.CF >= 0)
                    {
                        sb.Append("\\brdrcf");
                        sb.Append(BorderTop.CF.ToString(CultureInfo.InvariantCulture));
                    }
                    sb.Append("\\brdrr");
                    sb.Append(BorderRight.RTFEncoding);
                    if (BorderRight.CF >= 0)
                    {
                        sb.Append("\\brdrcf");
                        sb.Append(BorderRight.CF.ToString(CultureInfo.InvariantCulture));
                    }
                    sb.Append("\\brdrb");
                    sb.Append(BorderBottom.RTFEncoding);
                    if (BorderBottom.CF >= 0)
                    {
                        sb.Append("\\brdrcf");
                        sb.Append(BorderBottom.CF.ToString(CultureInfo.InvariantCulture));
                    }
                    sb.Append("\\brsp");
                    sb.Append(Spacing.ToString(CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private BorderFormat _bfLeft;
        private BorderFormat _bfTop;
        private BorderFormat _bfRight;
        private BorderFormat _bfBottom;
        private BorderFormat _bfAll;
        private long _nSpacing;

        #endregion Private Fields
    }

    internal enum WidthType
    {
        WidthIgnore = 0,
        WidthAuto = 1,
        WidthPercent = 2,   // Actually 50ths
        WidthTwips = 3
    }

    internal class CellWidth
    {
        internal CellWidth()
        {
            Type = WidthType.WidthAuto;
            Value = 0;
        }

        internal CellWidth(CellWidth cw)
        {
            Type = cw.Type;
            Value = cw.Value;
        }

        internal WidthType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        internal long Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        internal void SetDefaults()
        {
            Type = WidthType.WidthAuto;
            Value = 0;
        }

        private WidthType _type;
        private long _value;
    }

    internal class CellFormat
    {
        internal CellFormat()
        {
            BorderLeft = new BorderFormat();
            BorderRight = new BorderFormat();
            BorderBottom = new BorderFormat();
            BorderTop = new BorderFormat();

            Width = new CellWidth();

            SetDefaults();

            IsPending = true;
        }

        internal CellFormat(CellFormat cf)
        {
            CellX = cf.CellX;
            IsCellXSet = cf.IsCellXSet;
            Width = new CellWidth(cf.Width);
            CB = cf.CB;
            CF = cf.CF;
            Shading = cf.Shading;
            PaddingTop = cf.PaddingTop;
            PaddingBottom = cf.PaddingBottom;
            PaddingRight = cf.PaddingRight;
            PaddingLeft = cf.PaddingLeft;
            BorderLeft = new BorderFormat(cf.BorderLeft);
            BorderRight = new BorderFormat(cf.BorderRight);
            BorderBottom = new BorderFormat(cf.BorderBottom);
            BorderTop = new BorderFormat(cf.BorderTop);
            SpacingTop = cf.SpacingTop;
            SpacingBottom = cf.SpacingBottom;
            SpacingRight = cf.SpacingRight;
            SpacingLeft = cf.SpacingLeft;
            VAlign = VAlign.AlignTop;
            IsPending = true;
            IsHMerge = cf.IsHMerge;
            IsHMergeFirst = cf.IsHMergeFirst;
            IsVMerge = cf.IsVMerge;
            IsVMergeFirst = cf.IsVMergeFirst;
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal long CB
        {
            get
            {
                return _cb;
            }
            set
            {
                _cb = value;
            }
        }

        internal long CF
        {
            get
            {
                return _cf;
            }
            set
            {
                _cf = value;
            }
        }

        internal long Shading
        {
            get
            {
                return _nShading;
            }
            set
            {
                _nShading = Validators.MakeValidShading(value);
            }
        }

        internal long PaddingLeft
        {
            get
            {
                return _padL;
            }
            set
            {
                _padL = value;
            }
        }

        internal long PaddingRight
        {
            get
            {
                return _padR;
            }
            set
            {
                _padR = value;
            }
        }

        internal long PaddingTop
        {
            get
            {
                return _padT;
            }
            set
            {
                _padT = value;
            }
        }

        internal long PaddingBottom
        {
            get
            {
                return _padB;
            }
            set
            {
                _padB = value;
            }
        }

        internal BorderFormat BorderTop
        {
            get
            {
                return _brdT;
            }
            set
            {
                _brdT = value;
            }
        }

        internal BorderFormat BorderBottom
        {
            get
            {
                return _brdB;
            }
            set
            {
                _brdB = value;
            }
        }

        internal BorderFormat BorderLeft
        {
            get
            {
                return _brdL;
            }
            set
            {
                _brdL = value;
            }
        }

        internal BorderFormat BorderRight
        {
            get
            {
                return _brdR;
            }
            set
            {
                _brdR = value;
            }
        }

        internal CellWidth Width
        {
            get
            {
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        internal long CellX
        {
            get
            {
                return _nCellX;
            }
            set
            {
                _nCellX = value;
                _fCellXSet = true;
            }
        }

        internal bool IsCellXSet
        {
            get
            {
                return _fCellXSet;
            }
            set
            {
                _fCellXSet = value;
            }
        }

        internal VAlign VAlign
        {
            set
            {
                _valign = value;
            }
        }

        internal long SpacingTop
        {
            get
            {
                return _spaceT;
            }
            set
            {
                _spaceT = value;
            }
        }

        internal long SpacingLeft
        {
            get
            {
                return _spaceL;
            }
            set
            {
                _spaceL = value;
            }
        }

        internal long SpacingBottom
        {
            get
            {
                return _spaceB;
            }
            set
            {
                _spaceB = value;
            }
        }

        internal long SpacingRight
        {
            get
            {
                return _spaceR;
            }
            set
            {
                _spaceR = value;
            }
        }

        internal bool IsPending
        {
            get
            {
                return _fPending;
            }
            set
            {
                _fPending = value;
            }
        }

        internal bool IsHMerge
        {
            get
            {
                return _fHMerge;
            }
            set
            {
                _fHMerge = value;
            }
        }

        internal bool IsHMergeFirst
        {
            get
            {
                return _fHMergeFirst;
            }
            set
            {
                _fHMergeFirst = value;
            }
        }

        internal bool IsVMerge
        {
            get
            {
                return _fVMerge;
            }
            set
            {
                _fVMerge = value;
            }
        }

        internal bool IsVMergeFirst
        {
            get
            {
                return _fVMergeFirst;
            }
            set
            {
                _fVMergeFirst = value;
            }
        }

        internal bool HasBorder
        {
            get
            {
                return BorderLeft.EffectiveWidth > 0
                        || BorderRight.EffectiveWidth > 0
                        || BorderTop.EffectiveWidth > 0
                        || BorderBottom.EffectiveWidth > 0;
            }
        }

        internal string RTFEncodingForWidth
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("\\clftsWidth");
                int t = (int)Width.Type;
                sb.Append(t.ToString(CultureInfo.InvariantCulture));
                sb.Append("\\clwWidth");
                sb.Append(Width.Value.ToString(CultureInfo.InvariantCulture));
                sb.Append("\\cellx");
                sb.Append(CellX.ToString(CultureInfo.InvariantCulture));

                return sb.ToString();
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void SetDefaults()
        {
            CellX = -1;
            IsCellXSet = false;
            Width.SetDefaults();
            CB = -1;
            CF = -1;
            Shading = -1;
            PaddingTop = 0;
            PaddingBottom = 0;
            PaddingRight = 0;
            PaddingLeft = 0;
            BorderLeft.SetDefaults();
            BorderRight.SetDefaults();
            BorderBottom.SetDefaults();
            BorderTop.SetDefaults();
            SpacingTop = 0;
            SpacingBottom = 0;
            SpacingRight = 0;
            SpacingLeft = 0;
            VAlign = VAlign.AlignTop;
            IsHMerge = false;
            IsHMergeFirst = false;
            IsVMerge = false;
            IsVMergeFirst = false;
        }

        internal string GetBorderAttributeString(ConverterState converterState)
        {
            Debug.Assert(HasBorder);

            // Build the border attribute string based on border values
            StringBuilder sb = new StringBuilder();

            // Left,Top,Right,Bottom
            sb.Append(" BorderThickness=\"");
            sb.Append(Converters.TwipToPositiveVisiblePxString(BorderLeft.EffectiveWidth));
            sb.Append(",");
            sb.Append(Converters.TwipToPositiveVisiblePxString(BorderTop.EffectiveWidth));
            sb.Append(",");
            sb.Append(Converters.TwipToPositiveVisiblePxString(BorderRight.EffectiveWidth));
            sb.Append(",");
            sb.Append(Converters.TwipToPositiveVisiblePxString(BorderBottom.EffectiveWidth));
            sb.Append("\"");

            // Only grab one color
            ColorTableEntry entry = null;
            if (BorderLeft.CF >= 0)
            {
                entry = converterState.ColorTable.EntryAt((int)BorderLeft.CF);
            }

            if (entry != null)
            {
                sb.Append(" BorderBrush=\"");
                sb.Append(entry.Color.ToString(CultureInfo.InvariantCulture));
                sb.Append("\"");
            }
            else
            {
                sb.Append(" BorderBrush=\"#FF000000\"");
            }

            return sb.ToString();
        }

        internal string GetPaddingAttributeString()
        {
            // Build the padding attribute string based on padding values
            StringBuilder sb = new StringBuilder();

            // Left,Top,Right,Bottom
            sb.Append(" Padding=\"");
            sb.Append(Converters.TwipToPositivePxString(PaddingLeft));
            sb.Append(",");
            sb.Append(Converters.TwipToPositivePxString(PaddingTop));
            sb.Append(",");
            sb.Append(Converters.TwipToPositivePxString(PaddingRight));
            sb.Append(",");
            sb.Append(Converters.TwipToPositivePxString(PaddingBottom));
            sb.Append("\"");

            return sb.ToString();
        }

        #endregion Internal Methods

        private long _cb;
        private long _cf;
        private long _nShading;
        private long _padT;
        private long _padB;
        private long _padR;
        private long _padL;
        private long _spaceT;
        private long _spaceB;
        private long _spaceR;
        private long _spaceL;
        private long _nCellX;
        private CellWidth _width;
        private VAlign _valign;
        private BorderFormat _brdL;
        private BorderFormat _brdR;
        private BorderFormat _brdT;
        private BorderFormat _brdB;
        private bool _fPending;
        private bool _fHMerge;
        private bool _fHMergeFirst;
        private bool _fVMerge;
        private bool _fVMergeFirst;
        private bool _fCellXSet;
    }


    internal class RowFormat
    {
        internal RowFormat()
        {
            _rowCellFormat = new CellFormat();
            _widthA = new CellWidth();
            _widthB = new CellWidth();
            _widthRow = new CellWidth();
            _cellFormats = new ArrayList();
            _dir = DirState.DirLTR;
            _nTrgaph = -1;
            _nTrleft = 0;
        }

        internal RowFormat(RowFormat ri)
        {
            _rowCellFormat = new CellFormat(ri.RowCellFormat);
            _cellFormats = new ArrayList();
            _widthA = new CellWidth(ri.WidthA);
            _widthB = new CellWidth(ri.WidthB);
            _widthRow = new CellWidth(ri.WidthRow);
            _nTrgaph = ri.Trgaph;
            _dir = ri.Dir;
            _nTrleft = ri._nTrleft;

            for (int i = 0; i < ri.CellCount; i++)
            {
                _cellFormats.Add(new CellFormat(ri.NthCellFormat(i)));
            }
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal CellFormat RowCellFormat
        {
            get
            {
                return _rowCellFormat;
            }
        }

        internal int CellCount
        {
            get
            {
                return _cellFormats.Count;
            }
        }

        internal CellFormat TopCellFormat
        {
            get
            {
                return CellCount > 0 ? NthCellFormat(CellCount - 1) : null;
            }
        }

        internal CellWidth WidthA
        {
            get
            {
                return _widthA;
            }
        }

        internal CellWidth WidthB
        {
            get
            {
                return _widthB;
            }
        }

        internal CellWidth WidthRow
        {
            get
            {
                return _widthRow;
            }
        }

        internal long Trgaph
        {
            get
            {
                return _nTrgaph;
            }
            set
            {
                _nTrgaph = value;
            }
        }

        internal long Trleft
        {
            get
            {
                return _nTrleft;
            }
            set
            {
                _nTrleft = value;
            }
        }

        internal DirState Dir
        {
            get
            {
                return _dir;
            }
            set
            {
                _dir = value;
            }
        }

        internal bool IsVMerge
        {
            get
            {
                for (int i = 0; i < CellCount; i++)
                {
                    if (NthCellFormat(i).IsVMerge)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal CellFormat NthCellFormat(int n)
        {
            // If asked for a cell format beyond the bounds specified, just use row defaults.
            // This probably indicates malformed content, but makes reading more robust.
            if (n < 0 || n >= CellCount)
            {
                return RowCellFormat;
            }

            return (CellFormat)_cellFormats[n];
        }

        internal CellFormat NextCellFormat()
        {
            _cellFormats.Add(new CellFormat(RowCellFormat));
            return TopCellFormat;
        }

        internal CellFormat CurrentCellFormat()
        {
            if (CellCount == 0 || !TopCellFormat.IsPending)
            {
                return NextCellFormat();
            }
            else
            {
                return TopCellFormat;
            }
        }

        internal void CanonicalizeWidthsFromRTF()
        {
            if (CellCount == 0)
            {
                return;
            }

            CellFormat cfPrev = null;
            long cellx = Trleft;

            // Make sure widths and cellx are set up.  For merged cells, the first cell in the merged set has
            // the real width while the last cell in the merged set has the real CellX.  I put the real CellX
            // in the first merged cell so I can just use that one.
            for (int i = 0; i < CellCount; i++)
            {
                CellFormat cf = NthCellFormat(i);

                // Ignore HMerge Cells
                if (cf.IsHMerge)
                {
                    continue;
                }

                // Grab CellX from last cell in range of merged cells
                if (cf.IsHMergeFirst)
                {
                    for (int k = i + 1; k < CellCount; k++)
                    {
                        CellFormat cf1 = NthCellFormat(k);

                        if (cf1.IsHMerge)
                        {
                            cf.CellX = cf1.CellX;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (cf.Width.Value == 0 && cf.IsCellXSet)
                {
                    cf.Width.Type = WidthType.WidthTwips;
                    cf.Width.Value = (cfPrev == null) ? cf.CellX - Trleft : cf.CellX - cfPrev.CellX;
                }
                else if (cf.Width.Value > 0 && !cf.IsCellXSet)
                {
                    cellx += cf.Width.Value;
                    cf.CellX = cellx;
                }
                cfPrev = cf;
            }

            // It's also important that CellX be monotonic.
            cellx = NthCellFormat(0).CellX;
            for (int i = 1; i < CellCount; i++)
            {
                CellFormat cf = NthCellFormat(i);

                if (cf.CellX < cellx)
                {
                    cf.CellX = cellx + 1;
                }
                cellx = cf.CellX;
            }
        }

        internal void CanonicalizeWidthsFromXaml()
        {
            long nCellX = Trleft;

            for (int i = 0; i < CellCount; i++)
            {
                CellFormat cf = NthCellFormat(i);

                if (cf.Width.Type == WidthType.WidthTwips)
                {
                    nCellX += cf.Width.Value;
                }
                else
                {
                    nCellX += 1440; // arbitrary - one inch.
                }
                cf.CellX = nCellX;
            }
        }

        #endregion Internal Methods

        private CellFormat _rowCellFormat;
        private CellWidth _widthA;
        private CellWidth _widthB;
        private CellWidth _widthRow;
        private ArrayList _cellFormats;
        private long _nTrgaph;
        private long _nTrleft;
        private DirState _dir;
    }


    internal class MarkerListEntry
    {
        internal MarkerListEntry()
        {
            _marker = MarkerStyle.MarkerBullet;
            _nILS = -1;
            _nStartIndexOverride = -1;
            _nStartIndexDefault = -1;
            _nVirtualListLevel = -1;
        }

        internal MarkerStyle Marker
        {
            get
            {
                return _marker;
            }
            set
            {
                _marker = value;
            }
        }

        internal long StartIndexOverride
        {
            get
            {
                return _nStartIndexOverride;
            }
            set
            {
                _nStartIndexOverride = value;
            }
        }

        internal long StartIndexDefault
        {
            get
            {
                return _nStartIndexDefault;
            }
            set
            {
                _nStartIndexDefault = value;
            }
        }

        internal long VirtualListLevel
        {
            get
            {
                return _nVirtualListLevel;
            }
            set
            {
                _nVirtualListLevel = value;
            }
        }

        internal long StartIndexToUse
        {
            get
            {
                return _nStartIndexOverride > 0 ? _nStartIndexOverride : _nStartIndexDefault;
            }
        }

        internal long ILS
        {
            get
            {
                return _nILS;
            }
            set
            {
                _nILS = value;
            }
        }

        private MarkerStyle _marker;
        private long _nStartIndexOverride;
        private long _nStartIndexDefault;
        private long _nVirtualListLevel;
        private long _nILS;
    }


    internal class MarkerList : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal MarkerList()
            : base(5)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal MarkerListEntry EntryAt(int index)
        {
            return (MarkerListEntry)this[index];
        }

        internal void AddEntry(MarkerStyle m, long nILS, long nStartIndexOverride, long nStartIndexDefault, long nLevel)
        {
            MarkerListEntry entry = new MarkerListEntry();

            entry.Marker = m;
            entry.StartIndexOverride = nStartIndexOverride;
            entry.StartIndexDefault = nStartIndexDefault;
            entry.VirtualListLevel = nLevel;
            entry.ILS = nILS;
            Add(entry);
        }

        #endregion Internal Methods
    }


    /// <summary>
    /// FontTableEntry
    /// </summary>
    internal class FontTableEntry
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FontTableEntry()
        {
            _index = -1;
            _codePage = -1;
            _charSet = 0;
            _bNameSealed = false;
            _bPending = true;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal int Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        internal string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        internal bool IsNameSealed
        {
            get
            {
                return _bNameSealed;
            }
            set
            {
                _bNameSealed = value;
            }
        }

        internal bool IsPending
        {
            get
            {
                return _bPending;
            }
            set
            {
                _bPending = value;
            }
        }

        internal int CodePage
        {
            get
            {
                return _codePage;
            }
            set
            {
                _codePage = value;
            }
        }

        internal int CodePageFromCharSet
        {
            set
            {
                int cp = CharSetToCodePage(value);
                if (cp != 0)
                {
                    CodePage = cp;
                }
            }
        }

        internal int CharSet
        {
            get
            {
                return _charSet;
            }
            set
            {
                _charSet = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal static int CharSetToCodePage(int cs)
        {
            switch (cs)
            {
                case 0: // ANSI
                    return 1252;    // ANSI means use 1252
                case 1: // DEFAULT
                    return -1;    // -1 means use default code page
                case 2: // Symbol
                    return 1252;  // Symbol isn't a charset
                case 3: // Invalid
                    return -1;    // -1 means use ansicpg
                case 77: // Mac
                    return 10000;
                case 78:  // Shift JIS - bug in MacWord98J
                case 128: // Shift JIS
                    return 932;
                case 129: // Hangul
                    return 949;
                case 130: // Johab
                    return 1361;
                case 134: // GB2312
                    return 936;
                case 136: // Big5
                    return 950;
                case 161: // Greek
                    return 1253;
                case 162: // Turkish
                    return 1254;
                case 163: // Vietnamese
                    return 1258;
                case 177: // Hebrew
                    return 1255;
                case 178: // Arabic
                    return 1256;
                case 179: // Arabic Traditional
                    return 1256;
                case 180: // Arabic user
                    return 1256;
                case 181: // Hebrew user
                    return 1255;
                case 186: // Baltic
                    return 1257;
                case 204: // Russian
                    return 1251;
                case 222: // Thai
                    return 874;
                case 238: // Eastern European
                    return 1250;
                case 254: // PC 437
                    return 437;
                case 255: // OEM
                    return 850;
                default:
                    return 0;
            }
        }

        internal void ComputePreferredCodePage()
        {
            int[] CodePageList = {
                1252, 932, 949, 1361, 936, 950, 1253, 1254, 1258, 1255, 1256, 1257, 1251, 874, 1250, 437, 850 };

            CodePage = 1252;
            CharSet = 0;

            if (Name != null && Name.Length > 0)
            {
                byte[] rgBytes = new byte[Name.Length * 6];
                char[] rgChars = new char[Name.Length * 6];

                for (int i = 0; i < CodePageList.Length; i++)
                {
                    Encoding e = InternalEncoding.GetEncoding(CodePageList[i]);

                    int cb = e.GetBytes(Name, 0, Name.Length, rgBytes, 0);
                    int cch = e.GetChars(rgBytes, 0, cb, rgChars, 0);

                    if (cch == Name.Length)
                    {
                        int k = 0;
                        for (k = 0; k < cch; k++)
                        {
                            if (rgChars[k] != Name[k])
                            {
                                break;
                            }
                        }

                        // This code page can encode this font name.
                        if (k == cch)
                        {
                            CodePage = CodePageList[i];
                            CharSet = CodePageToCharSet(CodePage);
                            break;
                        }
                    }
                }

                // Set the symbol charset for symbol font
                if (IsSymbolFont(Name))
                {
                    CharSet = 2 /* Symbol Charset */;
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static int CodePageToCharSet(int cp)
        {
            switch (cp)
            {
                case 1252: // ANSI
                    return 0;    // ANSI means use 1252
                case 10000: // Mac
                    return 77;
                case 932: // Shift JIS
                    return 128;
                case 949: // Hangul
                    return 129;
                case 1361: // Johab
                    return 130;
                case 936: // GB2312
                    return 134;
                case 950: // Big5
                    return 136;
                case 1253: // Greek
                    return 161;
                case 1254: // Turkish
                    return 162;
                case 1258: // Vietnamese
                    return 163;
                case 1255: // Hebrew
                    return 177;
                case 1256: // Arabic
                    return 178;
                case 1257: // Baltic
                    return 186;
                case 1251: // Russian
                    return 204;
                case 874: // Thai
                    return 222;
                case 1250: // Eastern European
                    return 238;
                case 437: // PC 437
                    return 254;
                case 850: // OEM
                    return 255;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Return true if the specified typeface name is the symbol font.
        /// </summary>
        private static bool IsSymbolFont(string typefaceName)
        {
            bool isSymbolFont = false;

            Typeface typeface = new Typeface(typefaceName);

            if (typeface != null)
            {
                GlyphTypeface glyphTypeface = typeface.TryGetGlyphTypeface();

                if (glyphTypeface != null && glyphTypeface.Symbol)
                {
                    isSymbolFont = true;
                }
            }

            return isSymbolFont;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string _name;
        private int _index;
        private int _codePage;
        private int _charSet;
        private bool _bNameSealed;
        private bool _bPending;

        #endregion Private Fields
    }

    /// <summary>
    /// FontTable that includes the font table.
    /// </summary>
    internal class FontTable : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal FontTable()
            : base(20)
        {
            _fontMappings = null;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal FontTableEntry DefineEntry(int index)
        {
            // Might happen with bad input
            FontTableEntry entry = FindEntryByIndex(index);

            if (entry != null)
            {
                // Re-open it
                entry.IsPending = true;
                entry.Name = null;

                return entry;
            }

            entry = new FontTableEntry();

            entry.Index = index;

            Add(entry);

            return entry;
        }

        internal FontTableEntry FindEntryByIndex(int index)
        {
            for (int i = 0; i < Count; i++)
            {
                FontTableEntry entry = EntryAt(i);

                if (entry.Index == index)
                {
                    return entry;
                }
            }

            return null;
        }

        internal FontTableEntry FindEntryByName(string name)
        {
            for (int i = 0; i < Count; i++)
            {
                FontTableEntry entry = EntryAt(i);

                if (name.Equals(entry.Name))
                {
                    return entry;
                }
            }

            return null;
        }

        internal FontTableEntry EntryAt(int index)
        {
            return (FontTableEntry)this[index];
        }

        internal int DefineEntryByName(string name)
        {
            int maxIndex = -1;
            for (int i = 0; i < Count; i++)
            {
                FontTableEntry entry = EntryAt(i);

                if (name.Equals(entry.Name))
                {
                    return entry.Index;
                }
                if (entry.Index > maxIndex)
                {
                    maxIndex = entry.Index;
                }
            }

            // Not there - define one.
            FontTableEntry newEntry = new FontTableEntry();
            newEntry.Index = maxIndex + 1;
            Add(newEntry);
            newEntry.Name = name;
            return maxIndex + 1;
        }

        internal void MapFonts()
        {
            Hashtable map = FontMappings;

            for (int i = 0; i < Count; i++)
            {
                FontTableEntry entry = EntryAt(i);

                if (entry.Name != null)
                {
                    string mappedName = (string)map[entry.Name.ToLower(CultureInfo.InvariantCulture)];
                    if (mappedName != null)
                    {
                        entry.Name = mappedName;
                    }
                    else
                    {
                        int iCP = entry.Name.IndexOf('(');
                        if (iCP >= 0)
                        {
                            while (iCP > 0 && entry.Name[iCP - 1] == ' ')
                                iCP--;
                            entry.Name = entry.Name.Substring(0, iCP);
                        }
                    }
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal FontTableEntry CurrentEntry
        {
            get
            {
                if (Count == 0)
                {
                    return null;
                }

                // Find Pending Entry
                for (int i = Count - 1; i >= 0; i--)
                {
                    FontTableEntry entry = EntryAt(i);

                    if (entry.IsPending)
                    {
                        return entry;
                    }
                }

                return EntryAt(Count - 1);
            }
        }

        internal Hashtable FontMappings
        {
            get
            {
                if (_fontMappings == null)
                {
                    _fontMappings = new Hashtable();
                    RegistryKey rk = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\FontSubstitutes");
                    if (rk != null)
                    {
                        string[] names = rk.GetValueNames();
                        foreach (string name in names)
                        {
                            string value = (string)rk.GetValue(name);

                            if (name.Length > 0 && value.Length > 0)
                            {
                                string lhs_name = name;
                                string lhs_tag = string.Empty;
                                string rhs_name = value;
                                string rhs_tag = string.Empty;

                                int i;
                                i = name.IndexOf(',');
                                if (i >= 0)
                                {
                                    lhs_name = name.Substring(0, i);
                                    lhs_tag = name.Substring(i + 1, name.Length - i - 1);
                                }
                                i = value.IndexOf(',');
                                if (i >= 0)
                                {
                                    rhs_name = value.Substring(0, i);
                                    rhs_tag = value.Substring(i + 1, value.Length - i - 1);
                                }
                                if (lhs_name.Length > 0 && rhs_name.Length > 0)
                                {
                                    bool bAdd = false;
                                    // If both entries specify charset, they must match.
                                    if (lhs_tag.Length > 0 && rhs_tag.Length > 0)
                                    {
                                        if (string.Compare(lhs_tag, rhs_tag, StringComparison.OrdinalIgnoreCase) == 0)
                                        {
                                            bAdd = true;
                                        }
                                    }

                                    // If neither specifies a charset, the tagged (left) entry must be a substring.
                                    else if (lhs_tag.Length == 0 && rhs_tag.Length == 0)
                                    {
                                        if (lhs_name.Length > rhs_name.Length)
                                        {
                                            string s = lhs_name.Substring(0, rhs_name.Length);
                                            if (string.Compare(s, rhs_name, StringComparison.OrdinalIgnoreCase) == 0)
                                            {
                                                bAdd = true;
                                            }
                                        }
                                    }

                                    // If just the name specifies the charset, use it.
                                    else if (lhs_tag.Length > 0 && rhs_tag.Length == 0)
                                    {
                                        bAdd = true;
                                    }

                                    // OK, actually add the mapping.
                                    if (bAdd)
                                    {
                                        // Don't add a new mapping if one exists
                                        string keyname = lhs_name.ToLower(CultureInfo.InvariantCulture);
                                        if (_fontMappings[keyname] == null)
                                        {
                                            _fontMappings.Add(keyname, rhs_name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return _fontMappings;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        Hashtable _fontMappings;

        #endregion Private Fields
    }

    /// <summary>
    /// ColorTableEntry that includes color.
    /// </summary>
    internal class ColorTableEntry
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ColorTableEntry()
        {
            _color = Color.FromArgb(0xff, 0, 0, 0);
            _bAuto = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
            }
        }

        internal bool IsAuto
        {
            get
            {
                return _bAuto;
            }
            set
            {
                _bAuto = value;
            }
        }

        internal byte Red
        {
            set
            {
                _color = Color.FromArgb(0xff, value, _color.G, _color.B);
            }
        }

        internal byte Green
        {
            set
            {
                _color = Color.FromArgb(0xff, _color.R, value, _color.B);
            }
        }

        internal byte Blue
        {
            set
            {
                _color = Color.FromArgb(0xff, _color.R, _color.G, value);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private Color _color;
        private bool _bAuto;

        #endregion Private Fields
    }

    /// <summary>
    /// ColorTableEntry that includes color table.
    /// </summary>
    internal class ColorTable : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ColorTable()
            : base(20)
        {
            _inProgress = false;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal Color ColorAt(int index)
        {
            if (index >= 0 && index < Count)
            {
                return EntryAt(index).Color;
            }
            else
            {
                return Color.FromArgb(0xff, 0, 0, 0);
            }
        }

        internal void FinishColor()
        {
            if (_inProgress)
            {
                _inProgress = false;
            }
            else
            {
                int i = AddColor(Color.FromArgb(0xff, 0, 0, 0));

                // Initial unspecified color value is treated as "auto".
                EntryAt(i).IsAuto = true;
            }
        }

        internal int AddColor(Color color)
        {
            // First return existing one
            for (int i = 0; i < Count; i++)
            {
                if (ColorAt(i) == color)
                {
                    return i;
                }
            }

            // OK, need to add one
            ColorTableEntry entry = new ColorTableEntry();
            entry.Color = color;
            Add(entry);
            return Count - 1;
        }

        internal ColorTableEntry EntryAt(int index)
        {
            if (index >= 0 && index < Count)
            {
                return (ColorTableEntry)this[index];
            }
            else
            {
                return null;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal byte NewRed
        {
            set
            {
                ColorTableEntry entry = GetInProgressEntry();
                if (entry != null)
                {
                    entry.Red = value;
                }
            }
        }

        internal byte NewGreen
        {
            set
            {
                ColorTableEntry entry = GetInProgressEntry();
                if (entry != null)
                {
                    entry.Green = value;
                }
            }
        }

        internal byte NewBlue
        {
            set
            {
                ColorTableEntry entry = GetInProgressEntry();
                if (entry != null)
                {
                    entry.Blue = value;
                }
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private ColorTableEntry GetInProgressEntry()
        {
            if (_inProgress)
            {
                return EntryAt(Count - 1);
            }
            else
            {
                _inProgress = true;

                ColorTableEntry entry = new ColorTableEntry();

                Add(entry);

                return entry;
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _inProgress;

        #endregion Private Fields
    }

    /// <summary>
    /// ListLevel
    /// </summary>
    internal class ListLevel
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListLevel()
        {
            _nStartIndex = 1;
            _numberType = MarkerStyle.MarkerArabic;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal long StartIndex
        {
            get
            {
                return _nStartIndex;
            }
            set
            {
                _nStartIndex = value;
            }
        }

        internal MarkerStyle Marker
        {
            get
            {
                return _numberType;
            }
            set
            {
                _numberType = value;
            }
        }

        internal FormatState FormatState
        {
            set
            {
                _formatState = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private long _nStartIndex;
        private MarkerStyle _numberType;
        private FormatState _formatState;

        #endregion Private Fields
    }

    /// <summary>
    /// ListLevelTable
    /// </summary>
    internal class ListLevelTable : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListLevelTable()
            : base(1)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal ListLevel EntryAt(int index)
        {
            // Note - we silently handle out of range index values here since the lookup
            // might have been based on the structure of the file or the content of some
            // keyword parameter.
            if (index > Count)
            {
                index = Count - 1;
            }
            return (ListLevel)(Count > index && index >= 0 ? this[index] : null);
        }

        internal ListLevel AddEntry()
        {
            ListLevel entry = new ListLevel();

            Add(entry);

            return entry;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ListLevel CurrentEntry
        {
            get
            {
                return Count > 0 ? EntryAt(Count - 1) : null;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// ListTableEntry
    /// </summary>
    internal class ListTableEntry
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListTableEntry()
        {
            _id = 0;
            _templateID = 0;

            _levels = new ListLevelTable();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal long ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        internal long TemplateID
        {
            set
            {
                _templateID = value;
            }
        }

        internal bool Simple
        {
            set
            {
                _simple = value;
            }
        }

        internal ListLevelTable Levels
        {
            get
            {
                return _levels;
            }
        }


        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private long _id;
        private long _templateID;

        private bool _simple;

        private ListLevelTable _levels;

        #endregion Private Fields
    }

    /// <summary>
    /// ListTable
    /// </summary>
    internal class ListTable : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListTable()
            : base(20)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal ListTableEntry EntryAt(int index)
        {
            return (ListTableEntry)this[index];
        }

        internal ListTableEntry FindEntry(long id)
        {
            for (int i = 0; i < Count; i++)
            {
                ListTableEntry entry = EntryAt(i);

                if (entry.ID == id)
                {
                    return entry;
                }
            }

            return null;
        }

        internal ListTableEntry AddEntry()
        {
            ListTableEntry entry = new ListTableEntry();

            Add(entry);

            return entry;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ListTableEntry CurrentEntry
        {
            get
            {
                return Count > 0 ? EntryAt(Count - 1) : null;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// ListOverride
    /// </summary>
    internal class ListOverride
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListOverride()
        {
            _id = 0;
            _index = 0;
            _levels = null;
            _nStartIndex = -1;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal long ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        internal long Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        internal ListLevelTable Levels
        {
            get
            {
                return _levels;
            }
            set
            {
                _levels = value;
            }
        }

        internal long StartIndex
        {
            get
            {
                return _nStartIndex;
            }
            set
            {
                _nStartIndex = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private long _id;
        private long _index;
        private long _nStartIndex;
        private ListLevelTable _levels;

        #endregion Private Fields
    }

    /// <summary>
    /// ListOverrideTable
    /// </summary>
    internal class ListOverrideTable : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal ListOverrideTable()
            : base(20)
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal ListOverride EntryAt(int index)
        {
            return (ListOverride)this[index];
        }

        internal ListOverride FindEntry(int index)
        {
            for (int i = 0; i < Count; i++)
            {
                ListOverride entry = EntryAt(i);

                if (entry.Index == index)
                {
                    return entry;
                }
            }

            return null;
        }

        internal ListOverride AddEntry()
        {
            ListOverride entry = new ListOverride();

            Add(entry);

            return entry;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal ListOverride CurrentEntry
        {
            get
            {
                return Count > 0 ? EntryAt(Count - 1) : null;
            }
        }

        #endregion Internal Properties
    }

    /// <summary>
    /// DocumentNode
    /// </summary>
    internal class DocumentNode
    {
        //------------------------------------------------------
        //
        //  Consts
        //
        //------------------------------------------------------

        #region Consts

        internal static string[] HtmlNames = new string[]
        {
            "",
            "",
            "span",
            "br",
            "a",
            "p",
            "ul",
            "li",
            "table",
            "tbody",
            "tr",
            "td"
        };

        internal static int[] HtmlLengths = new int[]
        {
            0,    // unknown
            0,    // text
            4,    // span
            2,    // br
            1,    // a
            1,    // p
            2,    // ul
            2,    // li
            5,    // table
            6,    // tbody
            2,    // tr
            2     // td
        };

        internal static string[] XamlNames = new string[]
        {
            "",
            "",
            "Span",
            "LineBreak",
            "Hyperlink",
            "Paragraph",
            "InlineUIContainer",
            "BlockUIContainer",
            "Image",
            "List",
            "ListItem",
            "Table",
            "TableRowGroup",
            "TableRow",
            "TableCell",
            "Section",
            "Figure",
            "Floater",
            "Field",
            "ListText"
        };

        #endregion Consts

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DocumentNode(DocumentNodeType documentNodeType)
        {
            _type = documentNodeType;
            _bPending = true;
            _childCount = 0;
            _index = -1;
            _dna = null;
            _parent = null;
            _bTerminated = false;
            _bMatched = false;
            _bHasMarkerContent = false;
            _sCustom = null;
            _nRowSpan = 1;
            _nColSpan = 1;
            _nVirtualListLevel = -1;
            _csa = null;

            _formatState = new FormatState();
            _contentBuilder = new StringBuilder();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void InheritFormatState(FormatState formatState)
        {
            _formatState = new FormatState(formatState);

            // Reset non-inherited properties
            _formatState.LI = 0;
            _formatState.RI = 0;
            _formatState.SB = 0;
            _formatState.SA = 0;
            _formatState.FI = 0;
            _formatState.Marker = MarkerStyle.MarkerNone;
            _formatState.CBPara = -1;
        }

        internal string GetTagName()
        {
            return XamlNames[(int)Type];
        }

        internal DocumentNode GetParentOfType(DocumentNodeType parentType)
        {
            DocumentNode dn = Parent;

            while (dn != null && dn.Type != parentType)
            {
                dn = dn.Parent;
            }

            return dn;
        }

        internal int GetTableDepth()
        {
            DocumentNode dn = Parent;
            int nDepth = 0;

            while (dn != null)
            {
                if (dn.Type == DocumentNodeType.dnTable)
                {
                    nDepth++;
                }

                dn = dn.Parent;
            }

            return nDepth;
        }

        internal int GetListDepth()
        {
            DocumentNode dn = Parent;
            int nDepth = 0;

            while (dn != null)
            {
                if (dn.Type == DocumentNodeType.dnList)
                {
                    nDepth++;
                }
                else if (dn.Type == DocumentNodeType.dnCell)
                {
                    break;
                }
                dn = dn.Parent;
            }

            return nDepth;
        }

        internal void Terminate(ConverterState converterState)
        {
            if (!IsTerminated)
            {
                string plaintext = StripInvalidChars(Xaml);
                AppendXamlPrefix(converterState);
                StringBuilder xamlBuilder = new StringBuilder(Xaml);
                xamlBuilder.Append(plaintext);
                Xaml = xamlBuilder.ToString();
                AppendXamlPostfix(converterState);
                IsTerminated = true;
            }
        }

        internal void ConstrainFontPropagation(FormatState fsOrig)
        {
            // We only output certain font properties at the paragraph level.  Ensure the paragraph's formatstate
            // only records those properties that are actually written there, so that inline nodes properly
            // generate the result.
            FormatState.SetCharDefaults();
            FormatState.Font = fsOrig.Font;
            FormatState.FontSize = fsOrig.FontSize;
            FormatState.Bold = fsOrig.Bold;
            FormatState.Italic = fsOrig.Italic;

            // No, lang can't be written at paragraph level since I have no way of turning it "off", e.g. if
            // \lang specified for \par but not for inline text.
            // FormatState.LangCur = fsOrig.LangCur;

            // No, font color can't be written at paragraph level since I have no way of turning it "off" once
            // I've turned it on, so "automatic" color values can't be encoded.  I'll just have to skip it here.
            // FormatState.CF = fsOrig.CF;

            // No, text decorations can't be written at paragraph level since they don't propagate.
            // FormatState.UL = fsOrig.UL;
            // FormatState.Strike = fsOrig.Strike;
        }

        internal bool RequiresXamlFontProperties()
        {
            FormatState fsThis = FormatState;
            FormatState fsParent = ParentFormatStateForFont;

            return      (fsThis.Strike != fsParent.Strike)
                    ||  (fsThis.UL != fsParent.UL)
                    ||  (fsThis.Font != fsParent.Font && fsThis.Font >= 0)
                    ||  (fsThis.FontSize != fsParent.FontSize && fsThis.FontSize >= 0)
                    ||  (fsThis.CF != fsParent.CF)
                    ||  (fsThis.Bold != fsParent.Bold)
                    ||  (fsThis.Italic != fsParent.Italic)
                    ||  (fsThis.LangCur != fsParent.LangCur);
        }

        internal void AppendXamlFontProperties(ConverterState converterState, StringBuilder sb)
        {
            FormatState fsThis = FormatState;
            FormatState fsParent = ParentFormatStateForFont;

            bool bStrike = fsThis.Strike != fsParent.Strike;
            bool bUL = fsThis.UL != fsParent.UL;
            if (bStrike || bUL)
            {
                sb.Append(" TextDecorations=\"");
                if (bUL)
                {
                    sb.Append("Underline");
                }
                if (bUL && bStrike)
                {
                    sb.Append(", ");
                }
                if (bStrike)
                {
                    sb.Append("Strikethrough");
                }
                sb.Append("\"");
            }

            if (fsThis.Font != fsParent.Font && fsThis.Font >= 0)
            {
                FontTableEntry entry = converterState.FontTable.FindEntryByIndex((int)fsThis.Font);

                if (entry != null && entry.Name != null && !(entry.Name.Equals(string.Empty)))
                {
                    sb.Append(" FontFamily=\"");

                    // FontFamily should be limited with LF_FACESIZE(32) characters,
                    // because GDI doesn't support fonts that have the name with more than
                    // LF_FACESIZE characters.
                    if (entry.Name.Length > 32)
                    {
                        sb.Append(entry.Name, 0, 32);
                    }
                    else
                    {
                        sb.Append(entry.Name);
                    }

                    sb.Append("\"");
                }
            }

            if (fsThis.FontSize != fsParent.FontSize && fsThis.FontSize >= 0)
            {
                sb.Append(" FontSize=\"");
                double fs = (double)fsThis.FontSize;
                if (fs <= 1f)
                {
                    fs = 2f;
                }
                sb.Append((fs / 2).ToString(CultureInfo.InvariantCulture));
                sb.Append("pt\"");
            }

            if (fsThis.Bold != fsParent.Bold)
            {
                if (fsThis.Bold)
                {
                    sb.Append(" FontWeight=\"Bold\"");
                }
                else
                {
                    sb.Append(" FontWeight=\"Normal\"");
                }
            }

            if (fsThis.Italic != fsParent.Italic)
            {
                if (fsThis.Italic)
                {
                    sb.Append(" FontStyle=\"Italic\"");
                }
                else
                {
                    sb.Append(" FontStyle=\"Normal\"");
                }
            }

            if (fsThis.CF != fsParent.CF)
            {
                ColorTableEntry entry = converterState.ColorTable.EntryAt((int)fsThis.CF);

                if (entry != null && !entry.IsAuto)
                {
                    sb.Append(" Foreground=\"");
                    sb.Append(entry.Color.ToString());
                    sb.Append("\"");
                }
            }

            // NB: 0x400 (1024) is reserved value for "lidNoProof" - not a real language code.
            if (fsThis.LangCur != fsParent.LangCur && fsThis.LangCur > 0 && fsThis.LangCur != 0x400)
            {
                try
                {
                    CultureInfo ci = new CultureInfo((int)fsThis.LangCur);
                    sb.Append(" xml:lang=\"");
                    sb.Append(ci.Name);
                    sb.Append("\"");
                }
                catch (System.ArgumentException)
                {
                    // Just omit xml:lang tag if this is not a valid value.
                }
            }
        }

        internal string StripInvalidChars(string text)
        {
            if (text == null || text.Length == 0)
            {
                return text;
            }

            StringBuilder sb = null;
            int i = 0;

            for (; i < text.Length; i++)
            {
                int iStart = i;
                for (; i < text.Length; i++)
                {
                    if ((text[i] & 0xF800) == 0xD800)    // if surrogate
                    {
                        if ((i + 1 == text.Length)             // and no trail char
                            || ((text[i] & 0xFC00) == 0xDC00)   // or low surrogate occurs before high
                            || ((text[i + 1] & 0xFC00) != 0xDC00) // or high not followed by low
                            )
                        {
                            break;  // then cull this
                        }
                        else
                        {
                            i++;    // move past first word of surrogate, then second at top of loop`
                        }
                    }
                }

                if (iStart != 0 || i != text.Length)
                {
                    if (sb == null)
                    {
                        sb = new StringBuilder();
                    }
                    if (i != iStart)
                    {
                        sb.Append(text, iStart, i - iStart);
                    }
                }
            }

            if (sb != null)
            {
                return sb.ToString();
            }
            else
            {
                return text;
            }
        }

        internal void AppendXamlEncoded(string text)
        {
            StringBuilder xamlStringBuilder = new StringBuilder(Xaml);

            int index = 0;
            while (index < text.Length)
            {
                int currentIndex = index;

                while (currentIndex < text.Length)
                {
                    if (text[currentIndex] < 32
                        && text[currentIndex] != '\t')
                    {
                        break;
                    }
                    if (text[currentIndex] == '&'
                        || text[currentIndex] == '>'
                        || text[currentIndex] == '<'
                        || text[currentIndex] == 0)
                    {
                        break;
                    }

                    currentIndex++;
                }
                if (currentIndex != index)
                {
                    string substring = text.Substring(index, currentIndex - index);
                    xamlStringBuilder.Append(substring);
                }
                if (currentIndex < text.Length)
                {
                    if (text[currentIndex] < 32 && text[currentIndex] != '\t')
                    {
                        switch (text[currentIndex])
                        {
                            case '\f':  // formfeed
                                xamlStringBuilder.Append("&#x");
                                int ic = (int)text[currentIndex];
                                xamlStringBuilder.Append(ic.ToString("x", CultureInfo.InvariantCulture));
                                xamlStringBuilder.Append(";");
                                break;

                            default:
                                // don't care about low ANSI values - not supported by XAML
                                break;
                        }
                    }
                    else
                    {
                        switch (text[currentIndex])
                        {
                            case '&': xamlStringBuilder.Append("&amp;"); break;
                            case '<': xamlStringBuilder.Append("&lt;"); break;
                            case '>': xamlStringBuilder.Append("&gt;"); break;
                            case (char)0: break;
                        }
                    }
                }
                index = currentIndex + 1;
            }
            Xaml = xamlStringBuilder.ToString();
        }

        internal void AppendXamlPrefix(ConverterState converterState)
        {
            DocumentNodeArray dna = converterState.DocumentNodeArray;

            if (IsHidden)
            {
                return;
            }

            if (Type == DocumentNodeType.dnImage)
            {
                // Append image xaml prefix
                AppendImageXamlPrefix();
                return;
            }

            if (Type == DocumentNodeType.dnText || Type == DocumentNodeType.dnInline)
            {
                AppendInlineXamlPrefix(converterState);
                return;
            }

            StringBuilder xamlStringBuilder = new StringBuilder();

            // Do I need to wrap a font around this?
            if (IsEmptyNode && RequiresXamlFontProperties())
            {
                xamlStringBuilder.Append("<");
                xamlStringBuilder.Append(XamlNames[(int)DocumentNodeType.dnInline]);
                AppendXamlFontProperties(converterState, xamlStringBuilder);
                xamlStringBuilder.Append(">");
            }

            xamlStringBuilder.Append("<");
            xamlStringBuilder.Append(GetTagName());

            switch (Type)
            {
                case DocumentNodeType.dnTable:
                    // See below for writing out table column information
                    AppendXamlPrefixTableProperties(xamlStringBuilder);
                    break;

                case DocumentNodeType.dnCell:
                    // Row stores cell properties.
                    AppendXamlPrefixCellProperties(xamlStringBuilder, dna, converterState);
                    break;

                case DocumentNodeType.dnParagraph:
                    AppendXamlPrefixParagraphProperties(xamlStringBuilder, converterState);
                    break;

                case DocumentNodeType.dnListItem:
                    // List margins are handled at the paragraph level
                    AppendXamlPrefixListItemProperties(xamlStringBuilder);
                    break;

                case DocumentNodeType.dnList:
                    // List margins are handled at the listitem level
                    AppendXamlPrefixListProperties(xamlStringBuilder);
                    break;

                case DocumentNodeType.dnHyperlink:
                    AppendXamlPrefixHyperlinkProperties(xamlStringBuilder);
                    break;
            }

            if (IsEmptyNode)
            {
                xamlStringBuilder.Append(" /");
            }

            xamlStringBuilder.Append(">");

            // Do I need to wrap a font around this?
            if (IsEmptyNode && RequiresXamlFontProperties())
            {
                xamlStringBuilder.Append("</");
                xamlStringBuilder.Append(XamlNames[(int)DocumentNodeType.dnInline]);
                xamlStringBuilder.Append(">");
            }

            // Anything after the start tag?
            switch (Type)
            {
                case DocumentNodeType.dnTable:
                    AppendXamlTableColumnsAfterStartTag(xamlStringBuilder);
                    break;
            }

            Xaml = xamlStringBuilder.ToString();
        }

        private void AppendXamlPrefixTableProperties(StringBuilder xamlStringBuilder)
        {
            // See below for writing out table column information
            if (FormatState.HasRowFormat)
            {
                if (FormatState.RowFormat.Dir == DirState.DirRTL)
                {
                    xamlStringBuilder.Append(" FlowDirection=\"RightToLeft\"");
                }

                RowFormat rf = FormatState.RowFormat;
                CellFormat cf = rf.RowCellFormat;
                xamlStringBuilder.Append(" CellSpacing=\"");
                xamlStringBuilder.Append(Converters.TwipToPositiveVisiblePxString(cf.SpacingLeft));
                xamlStringBuilder.Append("\"");
                xamlStringBuilder.Append(" Margin=\"");
                xamlStringBuilder.Append(Converters.TwipToPositivePxString(rf.Trleft));
                xamlStringBuilder.Append(",0,0,0\"");
            }
            else
            {
                xamlStringBuilder.Append(" CellSpacing=\"0\" Margin=\"0,0,0,0\"");
            }
        }

        private void AppendXamlPrefixCellProperties(StringBuilder xamlStringBuilder, DocumentNodeArray dna, ConverterState converterState)
        {
            Color cToUse = Color.FromArgb(0xff, 0, 0, 0);

            // Row stores cell properties.
            DocumentNode dnRow = GetParentOfType(DocumentNodeType.dnRow);
            Debug.Assert(dnRow != null);                        // Need row
            Debug.Assert(dnRow != null && !dnRow.IsPending);   // Row props attached when row is closed
            Debug.Assert(dnRow != null && dnRow.FormatState.RowFormat != null);

            if (dnRow != null && dnRow.FormatState.HasRowFormat)
            {
                int nCol = GetCellColumn();
                CellFormat cf = dnRow.FormatState.RowFormat.NthCellFormat(nCol);
                if (Converters.ColorToUse(converterState, cf.CB, cf.CF, cf.Shading, ref cToUse))
                {
                    xamlStringBuilder.Append(" Background=\"");
                    xamlStringBuilder.Append(cToUse.ToString(CultureInfo.InvariantCulture));
                    xamlStringBuilder.Append("\"");
                }

                if (cf.HasBorder)
                {
                    xamlStringBuilder.Append(cf.GetBorderAttributeString(converterState));
                }
                xamlStringBuilder.Append(cf.GetPaddingAttributeString());
            }
            else
                xamlStringBuilder.Append(" BorderBrush=\"#FF000000\" BorderThickness=\"1,1,1,1\"");

            if (ColSpan > 1)
            {
                xamlStringBuilder.Append(" ColumnSpan=\"");
                xamlStringBuilder.Append(ColSpan.ToString(CultureInfo.InvariantCulture));
                xamlStringBuilder.Append("\"");
            }
            if (RowSpan > 1)
            {
                xamlStringBuilder.Append(" RowSpan=\"");
                xamlStringBuilder.Append(RowSpan.ToString(CultureInfo.InvariantCulture));
                xamlStringBuilder.Append("\"");
            }
        }

        private void AppendXamlDir(StringBuilder xamlStringBuilder)
        {
            if (RequiresXamlDir)
            {
                if (XamlDir == DirState.DirLTR)
                {
                    xamlStringBuilder.Append(" FlowDirection=\"LeftToRight\"");
                }
                else
                {
                    xamlStringBuilder.Append(" FlowDirection=\"RightToLeft\"");
                }
            }
        }

        private void AppendXamlPrefixParagraphProperties(StringBuilder xamlStringBuilder, ConverterState converterState)
        {
            Color cToUse = Color.FromArgb(0xff, 0, 0, 0);
            FormatState fsThis = FormatState;

            if (Converters.ColorToUse(converterState, fsThis.CBPara, fsThis.CFPara, fsThis.ParaShading, ref cToUse))
            {
                xamlStringBuilder.Append(" Background=\"");
                xamlStringBuilder.Append(cToUse.ToString(CultureInfo.InvariantCulture));
                xamlStringBuilder.Append("\"");
            }

            // Handle paragraph direction
            AppendXamlDir(xamlStringBuilder);

            // Handle paragraph margins
            xamlStringBuilder.Append(" Margin=\"");
            xamlStringBuilder.Append(Converters.TwipToPositivePxString(NearMargin));
            xamlStringBuilder.Append(",");
            xamlStringBuilder.Append(Converters.TwipToPositivePxString(fsThis.SB));
            xamlStringBuilder.Append(",");
            xamlStringBuilder.Append(Converters.TwipToPositivePxString(FarMargin));
            xamlStringBuilder.Append(",");
            xamlStringBuilder.Append(Converters.TwipToPositivePxString(fsThis.SA));
            xamlStringBuilder.Append("\"");

            // FontFamily, Size, Bold, Italic
            AppendXamlFontProperties(converterState, xamlStringBuilder);

            // Lineheight
            // NB: Avalon only supports "lineheight exact" - we're just not going to output it.
            //if (fsThis.SL != 0)
            //{
            //    double px = (float)fsThis.SL;
            //    if (px < 0) px = -px;

            // Whether SLMult is on or not is really moot.  The value is always defined in twips,
            // the UI is the only thing that then interprets this as "multiple", probably when the
            // paragraph font is reset.

            //    xamlStringBuilder.Append(" LineHeight=\"");
            //    xamlStringBuilder.Append(Converters.TwipToPxString(px));
            //    xamlStringBuilder.Append("\"");
            //}

            // Indent
            if (fsThis.FI != 0)
            {
                xamlStringBuilder.Append(" TextIndent=\"");
                xamlStringBuilder.Append(Converters.TwipToPxString(fsThis.FI));
                xamlStringBuilder.Append("\"");
            }

            // Handle paragraph alignment
            if (fsThis.HAlign != HAlign.AlignDefault)
            {
                xamlStringBuilder.Append(" TextAlignment=\"");
                xamlStringBuilder.Append(Converters.AlignmentToString(fsThis.HAlign, fsThis.DirPara));
                xamlStringBuilder.Append("\"");
            }

            // Handle paragraph borders
            if (fsThis.HasParaBorder)
            {
                xamlStringBuilder.Append(fsThis.GetBorderAttributeString(converterState));
            }
        }

        private void AppendXamlPrefixListItemProperties(StringBuilder xamlStringBuilder)
        {
            // List margins are handled here normally.
            // NB: Avalon doesn't render list markers if margin is zero.  Enforce a minimum indent in order
            // to ensure the marker is visible.
            long lMargin = NearMargin;
            if (lMargin < 360 && this.GetListDepth() == 1)
            {
                DocumentNode dnList = Parent;

                if (dnList != null && dnList.FormatState.Marker != MarkerStyle.MarkerHidden)
                {
                    lMargin = 360;
                }
            }
            xamlStringBuilder.Append(" Margin=\"");
            xamlStringBuilder.Append(Converters.TwipToPositivePxString(lMargin));
            xamlStringBuilder.Append(",0,0,0\"");

            // Handle direction
            AppendXamlDir(xamlStringBuilder);
        }

        private void AppendXamlPrefixListProperties(StringBuilder xamlStringBuilder)
        {
            // List margins are handled at the listitem level
            xamlStringBuilder.Append(" Margin=\"0,0,0,0\"");
            xamlStringBuilder.Append(" Padding=\"0,0,0,0\"");

            // Marker style
            xamlStringBuilder.Append(" MarkerStyle=\"");
            xamlStringBuilder.Append(Converters.MarkerStyleToString(FormatState.Marker));
            xamlStringBuilder.Append("\"");

            // Note that we don't allow a value of zero here, since XAML doesn't support it.
            if (FormatState.StartIndex > 0 && FormatState.StartIndex != 1)
            {
                xamlStringBuilder.Append(" StartIndex=\"");
                xamlStringBuilder.Append(FormatState.StartIndex.ToString(CultureInfo.InvariantCulture));
                xamlStringBuilder.Append("\"");
            }

            // Handle direction
            AppendXamlDir(xamlStringBuilder);
        }

        private void AppendXamlPrefixHyperlinkProperties(StringBuilder xamlStringBuilder)
        {
            if (NavigateUri != null && NavigateUri.Length > 0)
            {
                xamlStringBuilder.Append(" NavigateUri=\"");
                xamlStringBuilder.Append(Converters.StringToXMLAttribute(NavigateUri));
                xamlStringBuilder.Append("\"");
            }
        }

        private void AppendXamlTableColumnsAfterStartTag(StringBuilder xamlStringBuilder)
        {
            if (ColumnStateArray != null && ColumnStateArray.Count > 0)
            {
                xamlStringBuilder.Append("<Table.Columns>");
                long prevX = 0;
                if (FormatState.HasRowFormat)
                {
                    prevX = FormatState.RowFormat.Trleft;
                }
                for (int i = 0; i < ColumnStateArray.Count; i++)
                {
                    ColumnState cs = ColumnStateArray.EntryAt(i);

                    long width = cs.CellX - prevX;

                    if (width <= 0)
                    {
                        width = 1;
                    }
                    prevX = cs.CellX;

                    xamlStringBuilder.Append("<TableColumn Width=\"");
                    xamlStringBuilder.Append(Converters.TwipToPxString(width));
                    xamlStringBuilder.Append("\" />");
                }
                xamlStringBuilder.Append("</Table.Columns>");
            }
        }

        internal void AppendXamlPostfix(ConverterState converterState)
        {
            if (IsHidden)
            {
                return;
            }

            // Empty tag terminated above
            if (IsEmptyNode)
            {
                return;
            }

            if (Type == DocumentNodeType.dnImage)
            {
                // Append image xaml postfix
                AppendImageXamlPostfix();
                return;
            }

            if (Type == DocumentNodeType.dnText || Type == DocumentNodeType.dnInline)
            {
                AppendInlineXamlPostfix(converterState);
                return;
            }

            StringBuilder xamlStringBuilder = new StringBuilder(Xaml);

            xamlStringBuilder.Append("</");
            xamlStringBuilder.Append(GetTagName());
            xamlStringBuilder.Append(">");

            if (IsBlock)
            {
                xamlStringBuilder.Append("\r\n");
            }

            Xaml = xamlStringBuilder.ToString();
        }

        internal void AppendInlineXamlPrefix(ConverterState converterState)
        {
            StringBuilder xamlStringBuilder = new StringBuilder();

            FormatState fsThis = this.FormatState;
            FormatState fsParent = ParentFormatStateForFont;

            // Wrap any text with formatting tags.
            xamlStringBuilder.Append("<Span");

            AppendXamlDir(xamlStringBuilder);

            if (fsThis.CB != fsParent.CB)
            {
                ColorTableEntry entry = converterState.ColorTable.EntryAt((int)fsThis.CB);

                if (entry != null && !entry.IsAuto)
                {
                    xamlStringBuilder.Append(" Background=\"");
                    xamlStringBuilder.Append(entry.Color.ToString());
                    xamlStringBuilder.Append("\"");
                }
            }

            AppendXamlFontProperties(converterState, xamlStringBuilder);

            // NB: Avalon does not support the RTF notion of Expand
            //if (fsThis.Expand != fsParent.Expand)
            //{
            //    if (fsThis.Expand > 0)
            //    {
            //        xamlStringBuilder.Append(" FontStretch=\"Expanded\"");
            //    }
            //    else
            //    {
            //        xamlStringBuilder.Append(" FontStretch=\"Condensed\"");
            //    }
            //}

            if (fsThis.Super != fsParent.Super)
            {
                xamlStringBuilder.Append(" Typography.Variants=\"Superscript\"");
            }

            if (fsThis.Sub != fsParent.Sub)
            {
                xamlStringBuilder.Append(" Typography.Variants=\"Subscript\"");
            }

            xamlStringBuilder.Append(">");

            Xaml = xamlStringBuilder.ToString();
        }

        internal void AppendInlineXamlPostfix(ConverterState converterState)
        {
            StringBuilder xamlStringBuilder = new StringBuilder(Xaml);

            xamlStringBuilder.Append("</Span>");

            Xaml = xamlStringBuilder.ToString();
        }

        internal void AppendImageXamlPrefix()
        {
            StringBuilder xamlStringBuilder = new StringBuilder();
            xamlStringBuilder.Append("<InlineUIContainer>");
            Xaml = xamlStringBuilder.ToString();
        }

        internal void AppendImageXamlPostfix()
        {
            StringBuilder xamlStringBuilder = new StringBuilder(Xaml);
            xamlStringBuilder.Append("</InlineUIContainer>");
            Xaml = xamlStringBuilder.ToString();
        }

        internal bool IsAncestorOf(DocumentNode documentNode)
        {
            int parentIndex = Index;
            int parentLastChild = Index + ChildCount;

            return documentNode.Index > parentIndex && documentNode.Index <= parentLastChild;
        }

        internal bool IsLastParagraphInCell()
        {
            DocumentNodeArray dna = DNA;

            if (Type != DocumentNodeType.dnParagraph)
                return false;

            DocumentNode dnCell = GetParentOfType(DocumentNodeType.dnCell);
            if (dnCell == null)
            {
                return false;
            }

            int nFirst = dnCell.Index + 1;
            int nLast = dnCell.Index + dnCell.ChildCount;

            for (; nFirst <= nLast; nLast--)
            {
                DocumentNode dn = dna.EntryAt(nLast);

                if (dn == this)
                {
                    return true;
                }
                if (dn.IsBlock)
                {
                    return false;
                }
            }

            return false;
        }

        internal DocumentNodeArray GetTableRows()
        {
            DocumentNodeArray dna = DNA;
            DocumentNodeArray retArray = new DocumentNodeArray();

            if (Type == DocumentNodeType.dnTable)
            {
                int nStart = this.Index + 1;
                int nLast = this.Index + this.ChildCount;

                for (; nStart <= nLast; nStart++)
                {
                    DocumentNode dnRow = dna.EntryAt(nStart);

                    if (dnRow.Type == DocumentNodeType.dnRow && this == dnRow.GetParentOfType(DocumentNodeType.dnTable))
                    {
                        retArray.Push(dnRow);
                    }
                }
            }

            return retArray;
        }

        internal DocumentNodeArray GetRowsCells()
        {
            DocumentNodeArray dna = DNA;
            DocumentNodeArray retArray = new DocumentNodeArray();

            if (Type == DocumentNodeType.dnRow)
            {
                int nStart = this.Index + 1;
                int nLast = this.Index + this.ChildCount;

                for (; nStart <= nLast; nStart++)
                {
                    DocumentNode dnCell = dna.EntryAt(nStart);

                    if (dnCell.Type == DocumentNodeType.dnCell && this == dnCell.GetParentOfType(DocumentNodeType.dnRow))
                    {
                        retArray.Push(dnCell);
                    }
                }
            }

            return retArray;
        }

        internal int GetCellColumn()
        {
            DocumentNodeArray dna = DNA;
            int nCol = 0;

            if (Type == DocumentNodeType.dnCell)
            {
                DocumentNode dnRow = this.GetParentOfType(DocumentNodeType.dnRow);

                if (dnRow != null)
                {
                    int nStart = dnRow.Index + 1;
                    int nLast = dnRow.Index + dnRow.ChildCount;

                    for (; nStart <= nLast; nStart++)
                    {
                        DocumentNode dnCell = dna.EntryAt(nStart);

                        if (dnCell == this)
                        {
                            break;
                        }

                        if (dnCell.Type == DocumentNodeType.dnCell && dnCell.GetParentOfType(DocumentNodeType.dnRow) == dnRow)
                        {
                            nCol++;
                        }
                    }
                }
            }

            return nCol;
        }

        internal ColumnStateArray ComputeColumns()
        {
            DocumentNodeArray dna = DNA;
            Debug.Assert(Type == DocumentNodeType.dnTable);

            DocumentNodeArray dnaRows = GetTableRows();
            ColumnStateArray cols = new ColumnStateArray();

            for (int i = 0; i < dnaRows.Count; i++)
            {
                DocumentNode dnRow = dnaRows.EntryAt(i);
                RowFormat rf = dnRow.FormatState.RowFormat;

                long prevCellX = 0;
                for (int j = 0; j < rf.CellCount; j++)
                {
                    CellFormat cf = rf.NthCellFormat(j);
                    bool bHandled = false;
                    long prevColX = 0;

                    // Ignore merged cells
                    if (cf.IsHMerge)
                    {
                        continue;
                    }

                    for (int k = 0; k < cols.Count; k++)
                    {
                        ColumnState cs = (ColumnState)cols[k];

                        if (cs.CellX == cf.CellX)
                        {
                            if (!cs.IsFilled && prevColX == prevCellX)
                            {
                                cs.IsFilled = true;
                            }
                            bHandled = true;
                            break;
                        }
                        else if (cs.CellX > cf.CellX)
                        {
                            // Hmmm, need to insert a new cell here
                            ColumnState csNew = new ColumnState();
                            csNew.Row = dnRow;
                            csNew.CellX = cf.CellX;
                            csNew.IsFilled = (prevColX == prevCellX);
                            cols.Insert(k, csNew);
                            bHandled = true;
                            break;
                        }

                        prevColX = cs.CellX;
                    }

                    // New cell at the end
                    if (!bHandled)
                    {
                        ColumnState csNew = new ColumnState();
                        csNew.Row = dnRow;
                        csNew.CellX = cf.CellX;
                        csNew.IsFilled = (prevColX == prevCellX);
                        cols.Add(csNew);
                    }

                    prevCellX = cf.CellX;
                }
            }

            return cols;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal bool IsInline
        {
            get
            {
                return _type == DocumentNodeType.dnText
                    || _type == DocumentNodeType.dnInline
                    || _type == DocumentNodeType.dnImage
                    || _type == DocumentNodeType.dnLineBreak
                    || _type == DocumentNodeType.dnListText
                    || _type == DocumentNodeType.dnHyperlink;
            }
        }

        internal bool IsBlock
        {
            get
            {
                return _type == DocumentNodeType.dnParagraph
                    || _type == DocumentNodeType.dnList
                    || _type == DocumentNodeType.dnListItem
                    || _type == DocumentNodeType.dnTable
                    || _type == DocumentNodeType.dnTableBody
                    || _type == DocumentNodeType.dnRow
                    || _type == DocumentNodeType.dnCell
                    || _type == DocumentNodeType.dnSection
                    || _type == DocumentNodeType.dnFigure
                    || _type == DocumentNodeType.dnFloater;
            }
        }

        internal bool IsEmptyNode
        {
            get
            {
                return _type == DocumentNodeType.dnLineBreak;
            }
        }

        internal bool IsHidden
        {
            get
            {
                return _type == DocumentNodeType.dnFieldBegin
                    || _type == DocumentNodeType.dnFieldEnd
                    || _type == DocumentNodeType.dnShape
                    || _type == DocumentNodeType.dnListText;
            }
        }

        internal bool IsWhiteSpace
        {
            get
            {
                // Can't compute this on a terminated node, since non-text-data has been appended for properties.
                if (IsTerminated)
                {
                    return false;
                }

                if (_type == DocumentNodeType.dnText)
                {
                    string textdata = Xaml.Trim();
                    return textdata.Length == 0;
                }
                return false;
            }
        }

        internal bool IsPending
        {
            get
            {
                // Can't be pending if no longer in main document array
                return Index >= 0 && _bPending;
            }
            set
            {
                _bPending = value;
            }
        }

        internal bool IsTerminated
        {
            get
            {
                return _bTerminated;
            }
            set
            {
                _bTerminated = value;
            }
        }

        internal bool IsMatched
        {
            get
            {
                // This is only relevant for types that need matching.
                if (Type == DocumentNodeType.dnFieldBegin)
                {
                    return _bMatched;
                }

                // Otherwise, always true.
                return true;
            }
            set
            {
                _bMatched = value;
            }
        }

        internal bool IsTrackedAsOpen
        {
            get
            {
                if (Index < 0)
                {
                    return false;
                }
                if (Type == DocumentNodeType.dnFieldEnd)
                {
                    return false;
                }
                if (IsPending && !IsTerminated)
                {
                    return true;
                }
                if (!IsMatched)
                {
                    return true;
                }
                return false;
            }
        }

        internal bool HasMarkerContent
        {
            get
            {
                return _bHasMarkerContent;
            }
            set
            {
                _bHasMarkerContent = value;
            }
        }

        internal bool IsNonEmpty
        {
            get
            {
                return ChildCount > 0 || Xaml != null;
            }
        }

        internal string ListLabel
        {
            get
            {
                return _sCustom;
            }
            set
            {
                _sCustom = value;
            }
        }

        internal long VirtualListLevel
        {
            get
            {
                return _nVirtualListLevel;
            }
            set
            {
                _nVirtualListLevel = value;
            }
        }

        internal string NavigateUri
        {
            get
            {
                return _sCustom;
            }
            set
            {
                _sCustom = value;
            }
        }

        internal DocumentNodeType Type
        {
            get
            {
                return _type;
            }
        }

        internal FormatState FormatState
        {
            get
            {
                return _formatState;
            }
            set
            {
                _formatState = value;
            }
        }

        internal FormatState ParentFormatStateForFont
        {
            get
            {
                DocumentNode dnPa = Parent;

                // Hyperlink doesn't record relevant font info
                if (dnPa != null && dnPa.Type == DocumentNodeType.dnHyperlink)
                {
                    dnPa = dnPa.Parent;
                }

                if (Type == DocumentNodeType.dnParagraph || dnPa == null)
                {
                    return FormatState.EmptyFormatState;
                }

                return dnPa.FormatState;
            }
        }

        internal int ChildCount
        {
            get
            {
                return _childCount;
            }
            set
            {
                Debug.Assert(value >= 0);
                Debug.Assert(!IsPending);
                if (value >= 0)
                {
                    _childCount = value;
                }
            }
        }

        internal int Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }

        internal DocumentNodeArray DNA
        {
            get
            {
                return _dna;
            }
            set
            {
                _dna = value;
            }
        }

        internal int LastChildIndex
        {
            get
            {
                return Index + ChildCount;
            }
        }

        internal DocumentNode ClosedParent
        {
            get
            {
                return _parent;
            }
        }

        internal DocumentNode Parent
        {
            get
            {
                if (_parent == null && DNA != null)
                {
                    return DNA.GetOpenParentWhileParsing(this);
                }

                return _parent;
            }
            set
            {
                Debug.Assert(value == null || !value.IsPending);
                _parent = value;
            }
        }

        internal string Xaml
        {
            get
            {
                return _xaml;
            }
            set
            {
                _xaml = value;
            }
        }

        internal StringBuilder Content
        {
            get
            {
                return _contentBuilder;
            }
        }

        internal int RowSpan
        {
            get
            {
                return _nRowSpan;
            }
            set
            {
                _nRowSpan = value;
            }
        }

        internal int ColSpan
        {
            get
            {
                return _nColSpan;
            }
            set
            {
                _nColSpan = value;
            }
        }

        internal ColumnStateArray ColumnStateArray
        {
            get
            {
                return _csa;
            }
            set
            {
                _csa = value;
            }
        }

        internal DirState XamlDir
        {
            get
            {
                // Inline's easy
                if (IsInline)
                {
                    return FormatState.DirChar;
                }

                // We only have valid direction on table, list and paragraph.
                if (Type == DocumentNodeType.dnTable)
                {
                    if (FormatState.HasRowFormat)
                    {
                        return FormatState.RowFormat.Dir;
                    }

                    return ParentXamlDir;
                }
                else if (Type == DocumentNodeType.dnList || Type == DocumentNodeType.dnParagraph)
                {
                    return FormatState.DirPara;
                }
                else
                {
                    for (DocumentNode dnPa = Parent; dnPa != null; dnPa = dnPa.Parent)
                    {
                        switch (dnPa.Type)
                        {
                            case DocumentNodeType.dnList:
                            case DocumentNodeType.dnParagraph:
                            case DocumentNodeType.dnTable:
                                return dnPa.XamlDir;
                        }
                    }
                    return DirState.DirLTR;
                }
            }
        }

        internal DirState ParentXamlDir
        {
            get
            {
                return (Parent == null) ? DirState.DirLTR : Parent.XamlDir;
            }
        }

        internal bool RequiresXamlDir
        {
            get
            {
                return XamlDir != ParentXamlDir;
            }
        }

        internal long NearMargin
        {
            get
            {
                return ParentXamlDir == DirState.DirLTR ? FormatState.LI : FormatState.RI;
            }
            set
            {
                if (ParentXamlDir == DirState.DirLTR)
                {
                    FormatState.LI = value;
                }
                else
                {
                    FormatState.RI = value;
                }
            }
        }

        internal long FarMargin
        {
            get
            {
                return ParentXamlDir == DirState.DirLTR ? FormatState.RI : FormatState.LI;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        private bool _bPending;
        private bool _bTerminated;
        private DocumentNodeType _type;
        private FormatState _formatState;
        private string _xaml;
        private StringBuilder _contentBuilder;

        // Used for "tree" semantics
        private int _childCount;
        private int _index;
        private DocumentNode _parent;
        private DocumentNodeArray _dna;

        // Custom fields for specific node types
        // Tables
        private ColumnStateArray _csa;

        // Cells
        private int _nRowSpan;
        private int _nColSpan;

        // Lists
        private string _sCustom;    // Also used for Hyperlink
        private long _nVirtualListLevel;

        // ListText
        private bool _bHasMarkerContent;

        // Fields
        private bool _bMatched;

        #endregion Internal Fields
    }


    internal class ColumnState
    {
        internal ColumnState()
        {
            _nCellX = 0;
            _row = null;
            _fFilled = false;
        }

        internal long CellX
        {
            get
            {
                return _nCellX;
            }
            set
            {
                _nCellX = value;
            }
        }

        internal DocumentNode Row
        {
            get
            {
                return _row;
            }
            set
            {
                _row = value;
            }
        }

        internal bool IsFilled
        {
            get
            {
                return _fFilled;
            }
            set
            {
                _fFilled = value;
            }
        }

        private long _nCellX;
        private DocumentNode _row;
        private bool _fFilled;
    }

    internal class ColumnStateArray : ArrayList
    {
        internal ColumnStateArray()
            : base(20)
        {
        }

        internal ColumnState EntryAt(int i)
        {
            return (ColumnState)this[i];
        }

        internal int GetMinUnfilledRowIndex()
        {
            int nUnfilledRowIndex = -1;
            for (int i = 0; i < Count; i++)
            {
                ColumnState cs = EntryAt(i);

                if (!cs.IsFilled && (nUnfilledRowIndex < 0 || nUnfilledRowIndex > cs.Row.Index))
                {
                    // Don't split at row that is traversed by a row-spanning cell.
                    if (!cs.Row.FormatState.RowFormat.IsVMerge)
                    {
                        nUnfilledRowIndex = cs.Row.Index;
                    }
                }
            }

            Debug.Assert(nUnfilledRowIndex != 0);
            return nUnfilledRowIndex;
        }
    }

    /// <summary>
    /// class DocumentNodeArray:
    ///     This array represents a depth-first walk through the tree of nodes.  Each node records its current
    ///     index in the array (for ease of mapping back to the array) as well as the number of descendants
    ///     (confusingly called ChildCount).  A Parent pointer is also maintained, but this is essentially a
    ///     cache - the real structure is specified by the implicit ordering and the ChildCount value.
    ///     While the array is being constructed, nodes may be marked as "Pending".  The ChildCount of pending
    ///     nodes is not accurate.  ChildCount only becomes valid when a node is "Closed".
    /// </summary>
    internal class DocumentNodeArray : ArrayList
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DocumentNodeArray()
            : base(100)
        {
            _fMain = false;
            _dnaOpen = null;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal DocumentNode EntryAt(int nAt)
        {
            return (DocumentNode)this[nAt];
        }

        internal void Push(DocumentNode documentNode)
        {
            InsertNode(Count, documentNode);
        }

        internal DocumentNode Pop()
        {
            DocumentNode documentNode = Top;

            if (Count > 0)
            {
                Excise(Count - 1, 1);
            }

            return documentNode;
        }

        internal DocumentNode TopPending()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                DocumentNode dn = EntryAt(i);

                if (dn.IsPending)
                {
                    return dn;
                }
            }

            return null;
        }

        internal bool TestTop(DocumentNodeType documentNodeType)
        {
            return ((Count > 0) && (EntryAt(Count - 1).Type == documentNodeType));
        }

        internal void PreCoalesceChildren(ConverterState converterState, int nStart, bool bChild)
        {
            // We process tables twice to handle first colspan, then rowspan
            DocumentNodeArray dnaTables = new DocumentNodeArray();
            bool fVMerged = false;

            // Try to move paragraph margin to containing list items
            DocumentNode dnCoalesce = EntryAt(nStart);
            int nChild = dnCoalesce.ChildCount;
            Debug.Assert(nStart + nChild < Count);
            if (nStart + nChild >= Count)
            {
                nChild = Count - nStart - 1;
            }

            int nEnd = nStart + nChild;

            // If bChild specified, we don't process parent
            if (bChild)
            {
                nStart++;
            }

            // This is vaguely N^2 in the sense that for each list item, I process all containing paragraphs,
            // including ones contained in other list items.  But it's only a problem for very deep, very long
            // lists, so we can live with it.
            for (int nAt = nStart; nAt <= nEnd; nAt++)
            {
                DocumentNode dn = EntryAt(nAt);

                // Inline direction merging
                if (dn.IsInline && dn.RequiresXamlDir && dn.ClosedParent != null)
                {
                    int nnAt = nAt + 1;
                    for (; nnAt <= nEnd; nnAt++)
                    {
                        DocumentNode dnn = EntryAt(nnAt);

                        if (!dnn.IsInline
                            || dnn.Type == DocumentNodeType.dnHyperlink
                            || dnn.FormatState.DirChar != dn.FormatState.DirChar
                            || dnn.ClosedParent != dn.ClosedParent)
                        {
                            break;
                        }
                    }

                    int nChildHere = nnAt - nAt;
                    if (nChildHere > 1)
                    {
                        DocumentNode dnNewDir = new DocumentNode(DocumentNodeType.dnInline);
                        dnNewDir.FormatState = new FormatState(dn.Parent.FormatState);
                        dnNewDir.FormatState.DirChar = dn.FormatState.DirChar;

                        InsertChildAt(dn.ClosedParent, dnNewDir, nAt, nChildHere);

                        // Adjust the loop end to account for the newly inserted element
                        nEnd += 1;
                    }
                }

                else if (dn.Type == DocumentNodeType.dnListItem)
                {
                    PreCoalesceListItem(dn);
                }

                else if (dn.Type == DocumentNodeType.dnList)
                {
                    PreCoalesceList(dn);
                }

                else if (dn.Type == DocumentNodeType.dnTable)
                {
                    dnaTables.Add(dn);
                    nEnd += PreCoalesceTable(dn);
                }

                // Compute colspan
                else if (dn.Type == DocumentNodeType.dnRow)
                {
                    PreCoalesceRow(dn, ref fVMerged);
                }
            }

            // Process tables to examine rowspan
            if (fVMerged)
            {
                ProcessTableRowSpan(dnaTables);
            }
        }

        internal void CoalesceChildren(ConverterState converterState, int nStart)
        {
            Debug.Assert(Count == 0 || (nStart >= 0 && nStart < Count));
            if (nStart >= Count || nStart < 0)
            {
                return;
            }

            // Do some fixups to match semantics for more complicated constructs
            PreCoalesceChildren(converterState, nStart, false);

            DocumentNode dnCoalesce = EntryAt(nStart);
            int nChild = dnCoalesce.ChildCount;
            Debug.Assert(nStart + nChild < Count);
            if (nStart + nChild >= Count)
            {
                nChild = Count - nStart - 1;
            }

            int nEnd = nStart + nChild;

            for (int nAt = nEnd; nAt >= nStart; nAt--)
            {
                DocumentNode dn = EntryAt(nAt);

                if (dn.ChildCount == 0)
                {
                    dn.Terminate(converterState);
                }
                else
                {
                    Debug.Assert(nAt + dn.ChildCount <= nEnd);
                    Debug.Assert(!dn.IsTerminated);

                    dn.AppendXamlPrefix(converterState);
                    StringBuilder xamlBuilder = new StringBuilder(dn.Xaml);
                    int nChildrenHere = dn.ChildCount;
                    int nEndHere = nAt + nChildrenHere;
                    for (int i = nAt + 1; i <= nEndHere; i++)
                    {
                        DocumentNode dnChild = EntryAt(i);
                        Debug.Assert(dnChild.ChildCount == 0 && dnChild.IsTerminated);
                        xamlBuilder.Append(dnChild.Xaml);
                    }
                    dn.Xaml = xamlBuilder.ToString();
                    dn.AppendXamlPostfix(converterState);
                    dn.IsTerminated = true;
                    Excise(nAt + 1, nChildrenHere);
                    nEnd -= nChildrenHere;

                    AssertTreeInvariants();
                }

                // Zero out spanned columns
                if (dn.ColSpan == 0)
                {
                    dn.Xaml = string.Empty;
                }
            }
        }

        internal void CoalesceOnlyChildren(ConverterState converterState, int nStart)
        {
            Debug.Assert(Count == 0 || (nStart >= 0 && nStart < Count));
            if (nStart >= Count || nStart < 0)
            {
                return;
            }

            // Do some fixups to match semantics for more complicated constructs
            PreCoalesceChildren(converterState, nStart, true);

            DocumentNode dnCoalesce = EntryAt(nStart);
            int nChild = dnCoalesce.ChildCount;
            Debug.Assert(nStart + nChild < Count);
            if (nStart + nChild >= Count)
            {
                nChild = Count - nStart - 1;
            }

            int nEnd = nStart + nChild;

            for (int nAt = nEnd; nAt >= nStart; nAt--)
            {
                DocumentNode dn = EntryAt(nAt);

                if (dn.ChildCount == 0 && nAt != nStart)
                {
                    dn.Terminate(converterState);
                }
                else if (dn.ChildCount > 0)
                {
                    Debug.Assert(nAt + dn.ChildCount <= nEnd);
                    Debug.Assert(!dn.IsTerminated);

                    if (nAt != nStart)
                    {
                        dn.AppendXamlPrefix(converterState);
                    }
                    StringBuilder xamlBuilder = new StringBuilder(dn.Xaml);
                    int nChildrenHere = dn.ChildCount;
                    int nEndHere = nAt + nChildrenHere;
                    for (int i = nAt + 1; i <= nEndHere; i++)
                    {
                        DocumentNode dnChild = EntryAt(i);
                        Debug.Assert(dnChild.ChildCount == 0 && dnChild.IsTerminated);
                        xamlBuilder.Append(dnChild.Xaml);
                    }
                    dn.Xaml = xamlBuilder.ToString();
                    if (nAt != nStart)
                    {
                        dn.AppendXamlPostfix(converterState);
                        dn.IsTerminated = true;
                    }
                    Excise(nAt + 1, nChildrenHere);
                    nEnd -= nChildrenHere;
                }
            }
        }

        internal void CoalesceAll(ConverterState converterState)
        {
            for (int nAt = 0; nAt < Count; nAt++)
            {
                CoalesceChildren(converterState, nAt);
            }
        }

        internal void CloseAtHelper(int index, int nChildCount)
        {
            Debug.Assert(Count == 0 || (index >= 0 && index < Count));
            Debug.Assert(index + nChildCount < Count);
            if (index >= Count || index < 0 || index + nChildCount >= Count)
            {
                return;
            }

            DocumentNode dnClose = EntryAt(index);
            if (!dnClose.IsPending)
            {
                return;
            }

            // Mark this as closed
            dnClose.IsPending = false;

            dnClose.ChildCount = nChildCount;

            int nAt = index + 1;
            int nEnd = index + dnClose.ChildCount;
            while (nAt <= nEnd)
            {
                DocumentNode dn = EntryAt(nAt);
                dn.Parent = dnClose;
                nAt += dn.ChildCount + 1;
            }
        }

        internal void CloseAt(int index)
        {
            Debug.Assert(Count == 0 || (index >= 0 && index < Count));
            if (index >= Count || index < 0)
            {
                return;
            }

            DocumentNode dnClose = EntryAt(index);
            if (!dnClose.IsPending)
            {
                return;
            }

            AssertTreeInvariants();
            AssertTreeSemanticInvariants();

            // Make sure everything after its start is closed.
            for (int i = Count - 1; i > index; i--)
            {
                DocumentNode dn = EntryAt(i);
                if (dn.IsPending)
                {
                    CloseAt(i);
                }
            }

            // Set up child/parent relationship
            CloseAtHelper(index, Count - index - 1);

            AssertTreeInvariants();
            AssertTreeSemanticInvariants();
        }

        internal void AssertTreeInvariants()
        {
            if (Invariant.Strict)
            {
                for (int nAt = 0; nAt < Count; nAt++)
                {
                    DocumentNode dn = EntryAt(nAt);

                    for (int i = nAt + 1; i <= dn.LastChildIndex; i++)
                    {
                        Debug.Assert(EntryAt(i).ClosedParent != null);
                    }

                    Debug.Assert(nAt + dn.ChildCount < Count);

                    for (DocumentNode dnPa = dn.Parent; dnPa != null; dnPa = dnPa.Parent)
                    {
                        Debug.Assert(dnPa.IsPending || (nAt > dnPa.Index && nAt <= dnPa.Index + dnPa.ChildCount));
                    }
                }
            }
        }

        internal void AssertTreeSemanticInvariants()
        {
            if (Invariant.Strict)
            {
                for (int nAt = 0; nAt < Count; nAt++)
                {
                    DocumentNode dn = EntryAt(nAt);
                    DocumentNode dnPa = dn.Parent;

                    switch (dn.Type)
                    {
                        case DocumentNodeType.dnTableBody:
                            Debug.Assert(dnPa != null && dnPa.Type == DocumentNodeType.dnTable);
                            break;
                        case DocumentNodeType.dnRow:
                            Debug.Assert(dnPa != null && dnPa.Type == DocumentNodeType.dnTableBody);
                            break;
                        case DocumentNodeType.dnCell:
                            Debug.Assert(dnPa != null && dnPa.Type == DocumentNodeType.dnRow);
                            break;
                        case DocumentNodeType.dnListItem:
                            Debug.Assert(dnPa != null && dnPa.Type == DocumentNodeType.dnList);
                            break;
                    }
                }
            }
        }

        internal void CloseAll()
        {
            for (int nAt = 0; nAt < Count; nAt++)
            {
                if (EntryAt(nAt).IsPending)
                {
                    CloseAt(nAt);
                    break;
                }
            }
        }

        internal int CountOpenNodes(DocumentNodeType documentNodeType)
        {
            int nOpen = 0;

            if (_dnaOpen != null)
            {
                _dnaOpen.CullOpen();
                for (int i = _dnaOpen.Count - 1; i >= 0; i--)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.IsPending)
                    {
                        if (dn.Type == documentNodeType)
                        {
                            nOpen++;
                        }

                        // Shape blocks nesting
                        else if (dn.Type == DocumentNodeType.dnShape)
                        {
                            break;
                        }
                    }
                }
            }

            return nOpen;
        }

        internal int CountOpenCells()
        {
            return CountOpenNodes(DocumentNodeType.dnCell);
        }

        internal DocumentNode GetOpenParentWhileParsing(DocumentNode dn)
        {
            if (_dnaOpen != null)
            {
                _dnaOpen.CullOpen();
                for (int i = _dnaOpen.Count - 1; i >= 0; i--)
                {
                    DocumentNode dnPa = _dnaOpen.EntryAt(i);

                    if (dnPa.IsPending && dnPa.Index < dn.Index)
                    {
                        return dnPa;
                    }
                }
            }

            return null;
        }

        internal DocumentNodeType GetTableScope()
        {
            if (_dnaOpen != null)
            {
                _dnaOpen.CullOpen();
                for (int i = _dnaOpen.Count - 1; i >= 0; i--)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.IsPending)
                    {
                        if (dn.Type == DocumentNodeType.dnTable
                            || dn.Type == DocumentNodeType.dnTableBody
                            || dn.Type == DocumentNodeType.dnRow
                            || dn.Type == DocumentNodeType.dnCell)
                        {
                            return dn.Type;
                        }

                        // Shape blocks table structure
                        else if (dn.Type == DocumentNodeType.dnShape)
                        {
                            return DocumentNodeType.dnParagraph;
                        }
                    }
                }
            }

            return DocumentNodeType.dnParagraph;
        }

        internal MarkerList GetOpenMarkerStyles()
        {
            MarkerList ml = new MarkerList();

            if (_dnaOpen != null)
            {
                _dnaOpen.CullOpen();
                int nShape = 0;
                for (int i = 0; i < _dnaOpen.Count; i++)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.IsPending && dn.Type == DocumentNodeType.dnShape)
                    {
                        nShape = i + 1;
                    }
                }

                for (int i = nShape; i < _dnaOpen.Count; i++)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.IsPending && dn.Type == DocumentNodeType.dnList)
                    {
                        ml.AddEntry(dn.FormatState.Marker, dn.FormatState.ILS,
                                    dn.FormatState.StartIndex,
                                    dn.FormatState.StartIndexDefault, dn.VirtualListLevel);
                    }
                }
            }

            return ml;
        }

        internal MarkerList GetLastMarkerStyles(MarkerList mlHave, MarkerList mlWant)
        {
            MarkerList ml = new MarkerList();
            if (mlHave.Count > 0 || mlWant.Count == 0)
            {
                return ml;
            }

            bool bAllBullet = true;
            for (int i = Count - 1; i >= 0; i--)
            {
                DocumentNode dn = EntryAt(i);

                // Don't reopen a list across a table.
                if (dn.Type == DocumentNodeType.dnCell || dn.Type == DocumentNodeType.dnTable)
                {
                    break;
                }

                if (dn.Type == DocumentNodeType.dnListItem)
                {
                    // Don't open a list item in a closed table.
                    DocumentNode dnCell = dn.GetParentOfType(DocumentNodeType.dnCell);
                    if (dnCell != null && !dnCell.IsPending)
                    {
                        break;
                    }

                    // Ignore list items in shapes - note the continue since these didn't effect list continuation.
                    DocumentNode dnShape = dn.GetParentOfType(DocumentNodeType.dnShape);
                    if (dnShape != null && !dnShape.IsPending)
                    {
                        continue;
                    }

                    // OK, gather up the list structure that I'm potentially reopening.
                    for (DocumentNode dnList = dn.Parent; dnList != null; dnList = dnList.Parent)
                    {
                        // Note that I'm building this list up in the reverse order of GetOpenMarkerStyles
                        if (dnList.Type == DocumentNodeType.dnList)
                        {
                            MarkerListEntry mle = new MarkerListEntry();

                            mle.Marker = dnList.FormatState.Marker;
                            mle.StartIndexOverride = dnList.FormatState.StartIndex;
                            mle.StartIndexDefault = dnList.FormatState.StartIndexDefault;
                            mle.VirtualListLevel = dnList.VirtualListLevel;
                            mle.ILS = dnList.FormatState.ILS;
                            ml.Insert(0, mle);

                            if (mle.Marker != MarkerStyle.MarkerBullet)
                            {
                                bAllBullet = false;
                            }
                        }
                    }

                    break;
                }
            }

            // If all bullets at one level, don't do the continuation thing for simpler content generation.
            if (ml.Count == 1 && bAllBullet)
            {
                ml.RemoveRange(0, 1);
            }

            return ml;
        }

        internal void OpenLastList()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                DocumentNode dn = EntryAt(i);

                if (dn.Type == DocumentNodeType.dnListItem)
                {
                    // Don't do this for lists in shapes.
                    DocumentNode dnShape = dn.GetParentOfType(DocumentNodeType.dnShape);
                    if (dnShape != null && !dnShape.IsPending)
                    {
                        continue;
                    }

                    // Make all this pending.
                    for (DocumentNode dnPa = dn; dnPa != null; dnPa = dnPa.Parent)
                    {
                        if (dnPa.Type == DocumentNodeType.dnList || dnPa.Type == DocumentNodeType.dnListItem)
                        {
                            dnPa.IsPending = true;
                            _dnaOpen.InsertOpenNode(dnPa);
                        }
                    }

                    break;
                }
            }
        }

        internal void OpenLastCell()
        {
            // Be careful about nested cells - I want to open the last cell for the table/body/row that is
            // currently pending, not the last cell in the depth-first walk of the tree.  So first find
            // the pending table scope.
            for (int i = _dnaOpen.Count - 1; i >= 0; i--)
            {
                DocumentNode dn = _dnaOpen.EntryAt(i);

                if (dn.IsPending)
                {
                    if (dn.Type == DocumentNodeType.dnCell)
                    {
                        // Nothing to do!
                        return;
                    }
                    else if (dn.Type == DocumentNodeType.dnTable
                            || dn.Type == DocumentNodeType.dnTableBody
                            || dn.Type == DocumentNodeType.dnRow)
                    {
                        // OK, now find the cell
                        for (int j = Count - 1; j >= 0; j--)
                        {
                            DocumentNode ddn = EntryAt(j);

                            // Yikes, better find a child first.
                            if (ddn == dn)
                            {
                                Debug.Assert(false);
                                return;
                            }

                            if (ddn.Type == DocumentNodeType.dnCell && ddn.GetParentOfType(dn.Type) == dn)
                            {
                                for (DocumentNode dnPa = ddn; dnPa != null && dnPa != dn; dnPa = dnPa.Parent)
                                {
                                    dnPa.IsPending = true;
                                    _dnaOpen.InsertOpenNode(dnPa);
                                }

                                return;
                            }
                        }
                    }
                }
            }
        }

        internal int FindPendingFrom(DocumentNodeType documentNodeType, int nStart, int nLow)
        {
            if (_dnaOpen != null)
            {
                _dnaOpen.CullOpen();
                for (int i = _dnaOpen.Count - 1; i >= 0; i--)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.Index > nStart)
                    {
                        continue;
                    }
                    if (dn.Index <= nLow)
                    {
                        break;
                    }

                    if (dn.IsPending)
                    {
                        if (dn.Type == documentNodeType)
                        {
                            return dn.Index;
                        }

                        // Don't return pending elements across shape boundaries
                        else if (dn.Type == DocumentNodeType.dnShape)
                        {
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        internal int FindPending(DocumentNodeType documentNodeType, int nLow)
        {
            return FindPendingFrom(documentNodeType, Count - 1, nLow);
        }

        internal int FindPending(DocumentNodeType documentNodeType)
        {
            return FindPending(documentNodeType, -1);
        }

        internal int FindUnmatched(DocumentNodeType dnType)
        {
            if (_dnaOpen != null)
            {
                for (int i = _dnaOpen.Count - 1; i >= 0; i--)
                {
                    DocumentNode dn = _dnaOpen.EntryAt(i);

                    if (dn.Type == dnType && !dn.IsMatched)
                    {
                        return dn.Index;
                    }
                }
            }

            return -1;
        }

        internal void EstablishTreeRelationships()
        {
            // Record indices
            int i;
            for (i = 0; i < Count; i++)
            {
                EntryAt(i).Index = i;
            }

            for (i = 1; i < Count; i++)
            {
                DocumentNode dnThis = EntryAt(i);
                DocumentNode dnPrev = EntryAt(i - 1);

                // If prev isn't my parent, walk up its parent chain to find my parent
                if (dnPrev.ChildCount == 0)
                {
                    for (dnPrev = dnPrev.Parent; dnPrev != null; dnPrev = dnPrev.Parent)
                    {
                        if (dnPrev.IsAncestorOf(dnThis))
                        {
                            break;
                        }
                    }
                }

                dnThis.Parent = dnPrev;
            }
        }

        internal void CullOpen()
        {
            int i = Count - 1;
            for (; i >= 0; i--)
            {
                DocumentNode dn = EntryAt(i);

                if (dn.Index >= 0 && dn.IsTrackedAsOpen)
                {
                    break;
                }
            }
            int nCull = Count - (i + 1);
            if (nCull > 0)
            {
                RemoveRange(i + 1, nCull);
            }
        }

        internal void InsertOpenNode(DocumentNode dn)
        {
            CullOpen();

            int i = Count;
            for (; i > 0; i--)
            {
                if (dn.Index > EntryAt(i - 1).Index)
                {
                    break;
                }
            }
            Insert(i, dn);
        }

        internal void InsertNode(int nAt, DocumentNode dn)
        {
            Insert(nAt, dn);

            // Match sure Index values remain up-to-date.
            if (_fMain)
            {
                dn.Index = nAt;
                dn.DNA = this;
                for (nAt++; nAt < Count; nAt++)
                {
                    EntryAt(nAt).Index = nAt;
                }

                // Track open nodes
                if (dn.IsTrackedAsOpen)
                {
                    if (_dnaOpen == null)
                    {
                        _dnaOpen = new DocumentNodeArray();
                    }
                    _dnaOpen.InsertOpenNode(dn);
                }
            }
        }

        internal void InsertChildAt(DocumentNode dnParent, DocumentNode dnNew, int nInsertAt, int nChild)
        {
            Debug.Assert(_fMain);

            InsertNode(nInsertAt, dnNew);
            CloseAtHelper(nInsertAt, nChild);

            // Parent's parent shouldn't be the child document node
            if (dnParent != null && dnParent.Parent == dnNew)
            {
                Invariant.Assert(false, "Parent's Parent node shouldn't be the child node!");
            }

            // Patch the child count of the ancestors
            dnNew.Parent = dnParent;
            for (; dnParent != null; dnParent = dnParent.ClosedParent)
            {
                dnParent.ChildCount += 1;
            }

            AssertTreeInvariants();
        }

        internal void Excise(int nAt, int nExcise)
        {
            DocumentNode dn = EntryAt(nAt);

            // Mark the nodes as deleted from main array
            if (_fMain)
            {
                int nEnd = nAt + nExcise;
                for (int i = nAt; i < nEnd; i++)
                {
                    DocumentNode dn1 = EntryAt(i);
                    dn1.Index = -1;
                    dn1.DNA = null;
                }
            }

            // Remove from the array.
            RemoveRange(nAt, nExcise);

            if (_fMain)
            {
                // Patch the child count of the ancestors
                for (DocumentNode dnPa = dn.Parent; dnPa != null; dnPa = dnPa.Parent)
                {
                    if (!dnPa.IsPending)
                    {
                        Debug.Assert(dnPa.LastChildIndex >= nAt + nExcise - 1);
                        dnPa.ChildCount = dnPa.ChildCount - nExcise;
                    }
                }

                // Patch the Index of trailing nodes
                for (; nAt < Count; nAt++)
                {
                    EntryAt(nAt).Index = nAt;
                }

                AssertTreeInvariants();
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal DocumentNode Top
        {
            get
            {
                return Count > 0 ? EntryAt(Count - 1) : null;
            }
        }

        internal bool IsMain
        {
            set
            {
                _fMain = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // <Summary>
        //      The PreCoalesce process for a ListItem involves seeing if I can migrate the left indent
        //      from contained paragraphs to the ListItem itself.  This results in better bullet placement
        //      in the generated XAML.
        // </Summary>

        private void PreCoalesceListItem(DocumentNode dn)
        {
            int nAt = dn.Index;
            long nMargin = -1;
            int nEndItem = nAt + dn.ChildCount;

            for (int nnAt = nAt + 1; nnAt <= nEndItem; nnAt++)
            {
                DocumentNode ddn = EntryAt(nnAt);

                if (ddn.Type == DocumentNodeType.dnParagraph)
                {
                    if (nMargin == -1)
                    {
                        nMargin = ddn.NearMargin;
                    }
                    else if (ddn.NearMargin < nMargin && ddn.IsNonEmpty)
                    {
                        nMargin = ddn.NearMargin;
                    }
                }
            }
            dn.NearMargin = nMargin;
            for (int nnAt = nAt; nnAt <= nEndItem; nnAt++)
            {
                DocumentNode ddn = EntryAt(nnAt);

                if (ddn.Type == DocumentNodeType.dnParagraph)
                {
                    ddn.NearMargin = ddn.NearMargin - nMargin;
                }
            }
        }

        // <Summary>
        //      The PreCoalesce process for a List involves promoting the flowdirection if at all possible
        //      from contained paragraphs to the list itself.  This ensures that Avalon displays the list
        //      bullets in the proper location.
        // </Summary>

        private void PreCoalesceList(DocumentNode dn)
        {
            int nAt = dn.Index;
            bool bConflict = false;
            DirState ds = DirState.DirDefault;
            int nEndItem = nAt + dn.ChildCount;
            for (int nnAt = nAt + 1; !bConflict && nnAt <= nEndItem; nnAt++)
            {
                DocumentNode ddn = EntryAt(nnAt);

                if (ddn.Type == DocumentNodeType.dnParagraph && ddn.IsNonEmpty)
                {
                    if (ds == DirState.DirDefault)
                    {
                        ds = ddn.FormatState.DirPara;
                    }
                    else if (ds != ddn.FormatState.DirPara)
                    {
                        bConflict = true;
                    }
                }
            }

            // OK, promote if possible.
            if (!bConflict && ds != DirState.DirDefault)
            {
                for (int nnAt = nAt; nnAt <= nEndItem; nnAt++)
                {
                    DocumentNode ddn = EntryAt(nnAt);

                    if (ddn.Type == DocumentNodeType.dnList || ddn.Type == DocumentNodeType.dnListItem)
                    {
                        ddn.FormatState.DirPara = ds;
                    }
                }
            }
        }

        // <Summary>
        //      Table column handling.  RTF tables allow each row to be arbitrarily aligned.  XAML (like HTML)
        //      doesn't allow that.  You can achieve that effect in HTML by inserting extra rows with spurious
        //      cells propped to a specific width, but I'm not going to do that.  Instead, I'm going to split
        //      the rows into separate tables when combining some set of rows into a table would force me
        //      to fabricate a column that doesn't contain any defined cell.
        // </Summary>

        private int PreCoalesceTable(DocumentNode dn)
        {
            int nInserted = 0;
            int nAt = dn.Index;
            ColumnStateArray cols = dn.ComputeColumns();

            // OK, now I have a set of columns and information about which row caused the column to
            // be instantiated.  The naive algorithm is to strip the first N rows from the table until
            // the row that caused an uninstantiated column, break the table there, and then run the
            // algorithm again on the trailing table.
            int nUnfilledRowIndex = cols.GetMinUnfilledRowIndex();

            if (nUnfilledRowIndex > 0)
            {
                // OK, Need to insert a new table and table group around the remaining rows.
                DocumentNode dnNewTable = new DocumentNode(DocumentNodeType.dnTable);
                DocumentNode dnNewTableBody = new DocumentNode(DocumentNodeType.dnTableBody);
                dnNewTable.FormatState = new FormatState(dn.FormatState);
                dnNewTable.FormatState.RowFormat = EntryAt(nUnfilledRowIndex).FormatState.RowFormat;
                int nChildrenOldTable = nUnfilledRowIndex - dn.Index - 1;
                int nChildrenNewTable = dn.ChildCount - nChildrenOldTable;
                dn.ChildCount = nChildrenOldTable;  // Update old table child count
                EntryAt(nAt + 1).ChildCount = nChildrenOldTable - 1;    // Update old TableBody child count
                InsertNode(nUnfilledRowIndex, dnNewTableBody);
                CloseAtHelper(nUnfilledRowIndex, nChildrenNewTable);
                InsertNode(nUnfilledRowIndex, dnNewTable);
                CloseAtHelper(nUnfilledRowIndex, nChildrenNewTable + 1);

                // Adjust parent pointers
                dnNewTableBody.Parent = dnNewTable;
                dnNewTable.Parent = dn.ClosedParent;
                for (DocumentNode dnPa = dnNewTable.ClosedParent; dnPa != null; dnPa = dnPa.ClosedParent)
                {
                    dnPa.ChildCount = dnPa.ChildCount + 2;
                }

                // Adjust the loop end to account for the newly inserted elements
                nInserted = 2;

                // Need to recompute the ColumnStateArray for the newly truncated table.
                dn.ColumnStateArray = dn.ComputeColumns();
            }
            else
            {
                dn.ColumnStateArray = cols;
            }

            return nInserted;
        }

        private void PreCoalesceRow(DocumentNode dn, ref bool fVMerged)
        {
            DocumentNodeArray dnaCells = dn.GetRowsCells();
            RowFormat rf = dn.FormatState.RowFormat;
            DocumentNode dnTable = dn.GetParentOfType(DocumentNodeType.dnTable);
            ColumnStateArray csa = (dnTable != null) ? dnTable.ColumnStateArray : null;

            // Normally number of cells and cell definitions are equal, but be careful.
            int nCount = dnaCells.Count < rf.CellCount ? dnaCells.Count : rf.CellCount;

            // Non-unary colspan can arise both because I have "merged" cells specified
            // as well as because I just have cells that exactly span some other cols.
            // The code in PreCoalesce enforces that the cells line up, so I can just
            // test for that here.

            int nColsSeen = 0;
            int i = 0;
            while (i < nCount)
            {
                DocumentNode dnCell = dnaCells.EntryAt(i);
                CellFormat cf = rf.NthCellFormat(i);
                long cellx = cf.CellX;

                // optimization - record if we encountered a vmerged cell
                if (cf.IsVMerge)
                {
                    fVMerged = true;
                }

                // Determine colspan based on cells we will eliminate through the merge flags
                if (cf.IsHMergeFirst)
                {
                    for (i++; i < nCount; i++)
                    {
                        cf = rf.NthCellFormat(i);
                        if (cf.IsVMerge)
                        {
                            fVMerged = true;
                        }
                        if (cf.IsHMerge)
                        {
                            dnaCells.EntryAt(i).ColSpan = 0;    // zero means omit this cell
                        }
                    }
                }
                else
                {
                    i++;
                }

                // Determine actual colspan based on cellx value
                if (csa != null)
                {
                    int nColStart = nColsSeen;

                    while (nColsSeen < csa.Count)
                    {
                        ColumnState cs = csa.EntryAt(nColsSeen);
                        nColsSeen++;

                        // This is the normal case
                        if (cs.CellX == cellx)
                        {
                            break;
                        }

                        // This is anomalous, but can occur with odd \cellx values (non-monotonically increasing).
                        if (cs.CellX > cellx)
                        {
                            break;
                        }
                    }

                    if (nColsSeen - nColStart > dnCell.ColSpan)
                    {
                        dnCell.ColSpan = nColsSeen - nColStart;
                    }
                }
            }
        }

        private void ProcessTableRowSpan(DocumentNodeArray dnaTables)
        {
            for (int i = 0; i < dnaTables.Count; i++)
            {
                DocumentNode dnTable = dnaTables.EntryAt(i);
                ColumnStateArray csa = dnTable.ColumnStateArray;
                if (csa == null || csa.Count == 0)
                {
                    continue;
                }
                int nDim = csa.Count;

                DocumentNodeArray dnaRows = dnTable.GetTableRows();
                DocumentNodeArray dnaSpanCells = new DocumentNodeArray();
                for (int k = 0; k < nDim; k++)
                {
                    dnaSpanCells.Add(null);
                }

                for (int j = 0; j < dnaRows.Count; j++)
                {
                    DocumentNode dnRow = dnaRows.EntryAt(j);
                    RowFormat rf = dnRow.FormatState.RowFormat;
                    DocumentNodeArray dnaCells = dnRow.GetRowsCells();
                    int nCount = nDim;
                    if (rf.CellCount < nCount)
                    {
                        nCount = rf.CellCount;
                    }
                    if (dnaCells.Count < nCount)
                    {
                        nCount = dnaCells.Count;
                    }

                    // Nominally, the index into dnaSpanCells, dnaCells and RowFormat.NthCellFormat
                    // should all be the same.  But in some cases we have spanning cells that don't
                    // actually have an explicit cell associated with it (the span is implicit in the
                    // cellx/width values).  I can detect this case by finding a colspan > 1 that is
                    // not then followed by a HMerged format.  In this case, I need to apply a correction
                    // to my iteration, since the ColumnStateArray will have an entry for that field.

                    int kCSA = 0;   // this might advance faster
                    for (int k = 0; k < nCount && kCSA < dnaSpanCells.Count; k++)
                    {
                        DocumentNode dnCell = dnaCells.EntryAt(k);
                        CellFormat cf = rf.NthCellFormat(k);
                        if (cf.IsVMerge)
                        {
                            DocumentNode dnSpanningCell = dnaSpanCells.EntryAt(kCSA);
                            if (dnSpanningCell != null)
                            {
                                dnSpanningCell.RowSpan = dnSpanningCell.RowSpan + 1;
                            }
                            kCSA += dnCell.ColSpan;
                            dnCell.ColSpan = 0;
                        }
                        else
                        {
                            if (cf.IsVMergeFirst)
                            {
                                dnCell.RowSpan = 1;
                                dnaSpanCells[kCSA] = dnCell;
                            }
                            else
                            {
                                dnaSpanCells[kCSA] = null;
                            }
                            for (int l = kCSA + 1; l < kCSA + dnCell.ColSpan; l++)
                            {
                                dnaSpanCells[l] = null;
                            }
                            kCSA += dnCell.ColSpan;
                        }
                    }
                }
            }
        }

        #endregion PrivateMethods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private bool _fMain;
        private DocumentNodeArray _dnaOpen;

        #endregion Private Fields
    }

    /// <summary>
    /// ConverterState
    /// </summary>
    internal class ConverterState
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// ConverterState Constructor
        /// </summary>
        internal ConverterState()
        {
            _rtfFormatStack = new RtfFormatStack();
            _documentNodeArray = new DocumentNodeArray();
            _documentNodeArray.IsMain = true;
            _fontTable = new FontTable();
            _colorTable = new ColorTable();
            _listTable = new ListTable();
            _listOverrideTable = new ListOverrideTable();
            _defaultFont = -1;
            _defaultLang = -1;
            _defaultLangFE = -1;
            _bMarkerWhiteSpace = false;
            _bMarkerPresent = false;
            _border = null;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal FormatState PreviousTopFormatState(int fromTop)
        {
            return _rtfFormatStack.PrevTop(fromTop);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal RtfFormatStack RtfFormatStack
        {
            get
            {
                return _rtfFormatStack;
            }
        }

        internal FontTable FontTable
        {
            get
            {
                return _fontTable;
            }
        }

        internal ColorTable ColorTable
        {
            get
            {
                return _colorTable;
            }
        }

        internal ListTable ListTable
        {
            get
            {
                return _listTable;
            }
        }

        internal ListOverrideTable ListOverrideTable
        {
            get
            {
                return _listOverrideTable;
            }
        }

        internal DocumentNodeArray DocumentNodeArray
        {
            get
            {
                return _documentNodeArray;
            }
        }

        internal FormatState TopFormatState
        {
            get
            {
                return _rtfFormatStack.Top();
            }
        }

        internal int CodePage
        {
            get
            {
                return _codePage;
            }
            set
            {
                _codePage = value;
            }
        }

        internal long DefaultFont
        {
            get
            {
                return _defaultFont;
            }
            set
            {
                _defaultFont = value;
            }
        }

        internal long DefaultLang
        {
            get
            {
                return _defaultLang;
            }
            set
            {
                _defaultLang = value;
            }
        }

        internal long DefaultLangFE
        {
            get
            {
                return _defaultLangFE;
            }
            set
            {
                _defaultLangFE = value;
            }
        }

        internal bool IsMarkerWhiteSpace
        {
            get
            {
                return _bMarkerWhiteSpace;
            }
            set
            {
                _bMarkerWhiteSpace = value;
            }
        }

        internal bool IsMarkerPresent
        {
            get
            {
                return _bMarkerPresent;
            }
            set
            {
                _bMarkerPresent = value;
            }
        }

        internal BorderFormat CurrentBorder
        {
            get
            {
                return _border;
            }
            set
            {
                _border = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private RtfFormatStack _rtfFormatStack;
        private DocumentNodeArray _documentNodeArray;
        private FontTable _fontTable;
        private ColorTable _colorTable;
        private ListTable _listTable;
        private ListOverrideTable _listOverrideTable;
        private long _defaultFont;
        private long _defaultLang;
        private long _defaultLangFE;
        private int _codePage;
        private bool _bMarkerWhiteSpace;
        private bool _bMarkerPresent;
        private BorderFormat _border;

        #endregion Private Fields
    }

    /// <summary>
    /// RtfToXamlReader
    /// </summary>
    internal class RtfToXamlReader
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// RtfToXamlReader Constructor
        /// </summary>
        internal RtfToXamlReader(string rtfString)
        {
            _rtfBytes = Encoding.Default.GetBytes(rtfString);
            _bForceParagraph = false;

            Initialize();
        }

        private void Initialize()
        {
            _lexer = new RtfToXamlLexer(_rtfBytes);

            _converterState = new ConverterState();
            _converterState.RtfFormatStack.Push();

            _outerXamlBuilder = new StringBuilder();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// RtfToXamlError process
        /// </summary>
        internal RtfToXamlError Process()
        {
            RtfToXamlError rtfToXamlError = RtfToXamlError.None;

            RtfToken token = new RtfToken();
            bool findUnknownDestinationToken = false;

            int nStartCount = _converterState.RtfFormatStack.Count;

            while (rtfToXamlError == RtfToXamlError.None)
            {
                rtfToXamlError = _lexer.Next(token, _converterState.TopFormatState);

                if (rtfToXamlError != RtfToXamlError.None)
                {
                    break;
                }

                switch (token.Type)
                {
                    case RtfTokenType.TokenGroupStart:
                        _converterState.RtfFormatStack.Push();
                        findUnknownDestinationToken = false;
                        break;

                    case RtfTokenType.TokenGroupEnd:
                        ProcessGroupEnd();
                        findUnknownDestinationToken = false;
                        break;

                    case RtfTokenType.TokenInvalid:
                        rtfToXamlError = RtfToXamlError.InvalidFormat;
                        break;

                    case RtfTokenType.TokenEOF:
                        // Handle any anomalous missing group ends.
                        while (_converterState.RtfFormatStack.Count > 2 && _converterState.RtfFormatStack.Count > nStartCount)
                            ProcessGroupEnd();

                        AppendDocument();

                        return RtfToXamlError.None;

                    case RtfTokenType.TokenDestination:
                        findUnknownDestinationToken = true;
                        break;

                    case RtfTokenType.TokenControl:
                        {
                            RtfControlWordInfo controlWordInfo = token.RtfControlWordInfo;

                            if (controlWordInfo != null && !findUnknownDestinationToken)
                            {
                                if ((controlWordInfo.Flags & RtfControls.RTK_DESTINATION) != 0)
                                {
                                    findUnknownDestinationToken = true;
                                }
                            }

                            if (findUnknownDestinationToken)
                            {
                                // Ignore unknown control on the current field result destination.
                                // Otherwise, the field result content will be ignoreed by the unknown rtf destination.
                                if (controlWordInfo != null &&
                                    controlWordInfo.Control == RtfControlWord.Ctrl_Unknown &&
                                    _converterState.TopFormatState.RtfDestination == RtfDestination.DestFieldResult)
                                {
                                    controlWordInfo = null;
                                }
                                else
                                {
                                    _converterState.TopFormatState.RtfDestination = RtfDestination.DestUnknown;
                                }
                                findUnknownDestinationToken = false;
                            }

                            if (controlWordInfo != null)
                            {
                                HandleControl(token, controlWordInfo);
                            }

                            break;
                        }

                    case RtfTokenType.TokenText:
                        ProcessText(token);
                        break;

                    case RtfTokenType.TokenTextSymbol:
                        ProcessTextSymbol(token);
                        break;

                    case RtfTokenType.TokenNewline:
                    case RtfTokenType.TokenNullChar:
                        // Eaten
                        break;

                    case RtfTokenType.TokenPictureData:
                        ProcessImage(_converterState.TopFormatState);
                        break;
                }
            }

            return rtfToXamlError;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Output Xaml string from converting of Rtf
        /// </summary>
        internal string Output
        {
            get
            {
                return _outerXamlBuilder.ToString();
            }
        }

        internal bool ForceParagraph
        {
            get
            {
                return _bForceParagraph;
            }
            set
            {
                _bForceParagraph = value;
            }
        }

        internal ConverterState ConverterState
        {
            get
            {
                return _converterState;
            }
        }

        // WpfPayload package that containing the image for the specified Xaml
        internal WpfPayload WpfPayload
        {
            set
            {
                _wpfPayload = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal bool TreeContainsBlock()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            for (int i = 0; i < dna.Count; i++)
            {
                DocumentNode documentNode = dna.EntryAt(i);

                if (documentNode.Type == DocumentNodeType.dnParagraph ||
                    documentNode.Type == DocumentNodeType.dnList ||
                    documentNode.Type == DocumentNodeType.dnTable)
                {
                    return true;
                }
            }

            return false;
        }

        internal void AppendDocument()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // Remove any trailing whitespace that wasn't explicitly terminated
            while (dna.Count > 0)
            {
                DocumentNode dnLast = dna.EntryAt(dna.Count - 1);

                if (dnLast.IsInline && dnLast.IsWhiteSpace)
                {
                    dna.Excise(dna.Count - 1, 1);
                }
                else
                {
                    break;
                }
            }

            // If RTF ended with inline content and no \par, might need to force it.
            if (ForceParagraph || TreeContainsBlock())
            {
                if (dna.Count > 0)
                {
                    DocumentNode dnLast = dna.EntryAt(dna.Count - 1);
                    if (dnLast.IsInline)
                    {
                        FormatState formatState = _converterState.TopFormatState;
                        if (formatState != null)
                        {
                            HandlePara(null, formatState);
                        }
                    }
                }
            }

            dna.CloseAll();
            dna.CoalesceAll(_converterState);

            // Search for the paragraph node
            bool bBlock = ForceParagraph || TreeContainsBlock();

            // Add the document header
            if (bBlock)
            {
                _outerXamlBuilder.Append("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\" >\r\n");
            }
            else
            {
                _outerXamlBuilder.Append("<Span xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xml:space=\"preserve\">");
            }

            // Add the document content
            for (int i = 0; i < dna.Count; i++)
            {
                DocumentNode documentNode = dna.EntryAt(i);
                _outerXamlBuilder.Append(documentNode.Xaml);
            }

            // Add the document footer
            if (bBlock)
            {
                _outerXamlBuilder.Append("</Section>");
            }
            else
            {
                _outerXamlBuilder.Append("</Span>");
            }
        }

        // <summary>
        //  ProcessField is called when the \field group is closed.  There should be content in this group with the
        //  field instruction and the field result.  All of these distinct content areas are wrapped by
        //  dnField nodes.  The RtfDestination in the FormatState of the DocumentNode specifies which field
        //  property.
        // </summary>

        internal void ProcessField()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dnFieldBegin = null;
            DocumentNode dnFieldEnd = null;
            DocumentNode dnFieldInstBegin = null;
            DocumentNode dnFieldInstEnd = null;
            DocumentNode dnFieldResultBegin = null;
            DocumentNode dnFieldResultEnd = null;

            for (int i = dna.Count - 1; i >= 0 && dnFieldBegin == null; i--)
            {
                DocumentNode dn = dna.EntryAt(i);

                if (dn.Type == DocumentNodeType.dnFieldBegin)
                {
                    switch (dn.FormatState.RtfDestination)
                    {
                        case RtfDestination.DestFieldInstruction:
                            dnFieldInstBegin = dn;
                            break;
                        case RtfDestination.DestFieldResult:
                            dnFieldResultBegin = dn;
                            break;
                        case RtfDestination.DestField:
                            dnFieldBegin = dn;
                            break;
                    }
                }
                else if (dn.Type == DocumentNodeType.dnFieldEnd)
                {
                    switch (dn.FormatState.RtfDestination)
                    {
                        case RtfDestination.DestFieldInstruction:
                            dnFieldInstEnd = dn;
                            break;
                        case RtfDestination.DestFieldResult:
                            dnFieldResultEnd = dn;
                            break;
                        case RtfDestination.DestField:
                            dnFieldEnd = dn;
                            break;
                    }
                }
            }

            // This is anomalous - bad input?
            if (dnFieldBegin == null || dnFieldEnd == null)
            {
                return;
            }

            // Get the text of the instruction to determine how to handle it.
            DocumentNode dnInstruction = null;
            string pictureUri = null;

            if (dnFieldInstBegin != null && dnFieldInstEnd != null)
            {
                if (dnFieldInstEnd.Index > dnFieldInstBegin.Index + 1)
                {
                    string instructionName = string.Empty;

                    // Get the field instruction from the each text node of the
                    // field instruction child
                    for (int fieldInstruction = dnFieldInstBegin.Index + 1;
                         fieldInstruction < dnFieldInstEnd.Index;
                         fieldInstruction++)
                    {
                        DocumentNode dnChild = dna.EntryAt(fieldInstruction);
                        if (dnChild.Type == DocumentNodeType.dnText)
                        {
                            instructionName += dnChild.Xaml;
                        }
                    }

                    if (instructionName.Length > 0 && instructionName[0] == ' ')
                    {
                        instructionName = instructionName.Substring(1);
                    }

                    // Processs the instruction as hyperlink or symbol
                    if (instructionName.IndexOf("HYPERLINK", StringComparison.Ordinal) == 0)
                    {
                        dnInstruction = ProcessHyperlinkField(instructionName);
                    }
                    else if (instructionName.IndexOf("SYMBOL", StringComparison.Ordinal) == 0)
                    {
                        dnInstruction = ProcessSymbolField(instructionName);
                    }
                    else if (instructionName.IndexOf("INCLUDEPICTURE", StringComparison.Ordinal) == 0)
                    {
                        pictureUri = GetIncludePictureUri(instructionName);
                    }
                }
            }

            // Get rid of everything but field result contents
            if (dnFieldInstBegin != null && dnFieldInstEnd != null)
            {
                int nInst = dnFieldInstEnd.Index - dnFieldInstBegin.Index + 1;
                dna.Excise(dnFieldInstBegin.Index, nInst);
            }
            if (dnFieldResultBegin != null)
                dna.Excise(dnFieldResultBegin.Index, 1);
            if (dnFieldResultEnd != null)
                dna.Excise(dnFieldResultEnd.Index, 1);

            // Record the region that is going to be spanned by the new field node.
            int nInsertAt = dnFieldBegin.Index;
            int nChildCount = dnFieldEnd.Index - dnFieldBegin.Index - 1;
            DocumentNode dnPa = dnFieldBegin.ClosedParent;

            // Get rid of the field nodes themselves.
            dna.Excise(dnFieldBegin.Index, 1);
            dna.Excise(dnFieldEnd.Index, 1);

            if (pictureUri != null && nChildCount != 0)
            {
                DocumentNode dnImage = dna.EntryAt(nInsertAt);

                if (dnImage.Type == DocumentNodeType.dnImage)
                {
                    // Replace the Image UriSource with the picture uri.
                    // IE copying rtf content has the bogus wmetafile so the image quality
                    // is very low. Now we replace image with the included picture uri to get
                    // the image directly from the specified Uri.
                    int uriSourceIndex = dnImage.Xaml.IndexOf("UriSource=", StringComparison.Ordinal);
                    int uriSourceEndIndex = dnImage.Xaml.IndexOf('\"', uriSourceIndex + 11);

                    string imageXaml = dnImage.Xaml.Substring(0, uriSourceIndex);
                    imageXaml += "UriSource=\"" + pictureUri + "\"";
                    imageXaml += dnImage.Xaml.Substring(uriSourceEndIndex + 1);

                    dnImage.Xaml = imageXaml;
                }
            }

            // Now insert the node around the results.
            if (dnInstruction != null)
            {
                // Never insert an inline node around block content.  For example, Word/HTML allows hyperlinks
                // around any content, but in XAML hyperlinks are only inline.
                bool bOK = true;
                if (dnInstruction.IsInline)
                {
                    for (int i = nInsertAt; bOK && i < nInsertAt + nChildCount; i++)
                    {
                        if (dna.EntryAt(i).IsBlock)
                        {
                            bOK = false;
                        }
                    }
                }

                if (bOK)
                {
                    // Insert the instruction node if it is text(symbol) or has the child count for
                    // hyperlink or include picture
                    if (dnInstruction.Type == DocumentNodeType.dnText || nChildCount != 0)
                    {
                        dna.InsertChildAt(dnPa, dnInstruction, nInsertAt, nChildCount);
                        // No, don't coalesce since spanned content may need paragraph parent to determine props.
                        // dna.CoalesceChildren(_converterState, nInsertAt);
                    }
                }

                // If hyperlink, need to wrap each paragraph's contents, since XAML treats hyperlink as inline.
                else if (dnInstruction.Type == DocumentNodeType.dnHyperlink)
                {
                    dnInstruction.AppendXamlPrefix(_converterState);
                    for (int i = nInsertAt; i < nInsertAt + nChildCount; i++)
                    {
                        DocumentNode dn = dna.EntryAt(i);

                        if (dn.Type == DocumentNodeType.dnParagraph && !dn.IsTerminated)
                        {
                            Debug.Assert(dn.ChildCount == 0);
                            StringBuilder sb = new StringBuilder(dnInstruction.Xaml);
                            sb.Append(dn.Xaml);
                            sb.Append("</Hyperlink>");
                            dn.Xaml = sb.ToString();
                        }
                    }

                    // If I have trailing text that is not yet in a para, I need to wrap that as well
                    int nInline = 0;
                    int nInlineAt = nInsertAt + nChildCount - 1;
                    for (; nInlineAt >= nInsertAt; nInlineAt--)
                    {
                        DocumentNode dn = dna.EntryAt(nInlineAt);

                        if (dn.IsInline)
                        {
                            nInline++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (nInline > 0)
                    {
                        WrapInlineInParagraph(nInlineAt + 1, nInline);
                        DocumentNode dn = dna.EntryAt(nInlineAt + 1);
                        if (dn.Type == DocumentNodeType.dnParagraph && !dn.IsTerminated)
                        {
                            Debug.Assert(dn.ChildCount == 0);
                            StringBuilder sb = new StringBuilder(dnInstruction.Xaml);
                            sb.Append(dn.Xaml);
                            sb.Append("</Hyperlink>");
                            dn.Xaml = sb.ToString();
                        }
                    }
                }
            }

            // If I processed a field result with block content, it's possible I left some remaining
            // inline nodes unwrapped in a paragraph before the field.  Let's look for them and clean them up.
            bool bBlock = false;
            for (int i = nInsertAt; !bBlock && i < dna.Count; i++)
            {
                DocumentNode dn = dna.EntryAt(i);

                bBlock = dn.IsBlock;
            }

            if (bBlock)
            {
                int nVisible = 0;
                nChildCount = 0;
                for (int i = nInsertAt - 1; i >= 0; i--)
                {
                    DocumentNode dn = dna.EntryAt(i);

                    if (dn.IsInline || dn.IsHidden)
                    {
                        nChildCount++;
                        if (!dn.IsHidden)
                        {
                            nVisible++;
                        }
                    }
                    if (dn.IsBlock)
                    {
                        nChildCount -= dn.ChildCount;
                        break;
                    }
                }

                if (nVisible > 0)
                {
                    WrapInlineInParagraph(nInsertAt - nChildCount, nChildCount);
                }
            }
        }

        private int HexToInt(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (c - '0');
            }
            else if (c >= 'a' && c <= 'f')
            {
                return (10 + c - 'a');
            }
            else if (c >= 'A' && c <= 'F')
            {
                return (10 + c - 'A');
            }
            else
            {
                return 0;
            }
        }

        private int DecToInt(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (c - '0');
            }
            else
            {
                return 0;
            }
        }

        // Return the included picture Uri
        private string GetIncludePictureUri(string instructionName)
        {
            string pictureUri = null;
            int uriIndex = instructionName.IndexOf("http:", StringComparison.OrdinalIgnoreCase);

            if (uriIndex != -1)
            {
                pictureUri = instructionName.Substring(uriIndex, instructionName.Length - uriIndex - 1);

                int pictureUriEndIndex = pictureUri.IndexOf('\"');
                if (pictureUriEndIndex != -1)
                {
                    pictureUri = pictureUri.Substring(0, pictureUriEndIndex);
                }

                if (!Uri.IsWellFormedUriString(pictureUri, UriKind.Absolute))
                {
                    pictureUri = null;
                }
            }

            return pictureUri;
        }

        internal DocumentNode ProcessHyperlinkField(string instr)
        {
            DocumentNode dn = new DocumentNode(DocumentNodeType.dnHyperlink);
            dn.FormatState = new FormatState(_converterState.PreviousTopFormatState(0));
            string sUri = null;
            string sTarget = null;
            string sBookmark = null;

            bool bTargetNext = false;
            bool bBookmarkNext = false;

            // iterate, starting past " HYPERLINK"
            int i = 10;
            while (i < instr.Length)
            {
                // Skip spaces
                if (instr[i] == ' ')
                {
                    i++;
                }

                // NavigateUri?
                else if (instr[i] == '"')
                {
                    i++;
                    if (i < instr.Length)
                    {
                        int iStart = i;
                        int iEnd = i;

                        for (; iEnd < instr.Length; iEnd++)
                        {
                            if (instr[iEnd] == '"')
                            {
                                break;
                            }
                        }

                        string param = instr.Substring(iStart, iEnd - iStart);
                        if (bTargetNext)
                        {
                            sTarget = param;
                            bTargetNext = false;
                        }
                        else if (bBookmarkNext)
                        {
                            sBookmark = param;
                            bBookmarkNext = false;
                        }
                        else if (sUri == null)
                            sUri = param;
                        // else eat the string

                        i = iEnd + 1;
                    }
                }

                // Instructions
                else if (instr[i] == '\\')
                {
                    i++;
                    if (i < instr.Length)
                    {
                        switch (instr[i])
                        {
                            case 'l':   // bookmark
                                bBookmarkNext = true; bTargetNext = false;
                                break;
                            case 't':   // target
                                bBookmarkNext = false; bTargetNext = true;
                                break;
                        }
                        i++;
                    }
                }

                // Ignore other characters
                else
                    i++;
            }

            StringBuilder sb = new StringBuilder();
            if (sUri != null)
            {
                sb.Append(sUri);
            }
            if (sBookmark != null)
            {
                sb.Append("#");
                sb.Append(sBookmark);
            }

            // Remove the second backslash(which rtf specified) to keep only single backslash
            for (int uriIndex = 0; uriIndex < sb.Length; uriIndex++)
            {
                if (sb[uriIndex] == '\\' && uriIndex + 1 < sb.Length && sb[uriIndex + 1] == '\\')
                {
                    // Remove the sceond backslash
                    sb.Remove(uriIndex + 1, 1);
                }
            }

            dn.NavigateUri = sb.ToString();

            return dn.NavigateUri.Length > 0 ? dn : null;
        }

        internal DocumentNode ProcessSymbolField(string instr)
        {
            DocumentNode dn = new DocumentNode(DocumentNodeType.dnText);
            dn.FormatState = new FormatState(_converterState.PreviousTopFormatState(0));

            int nChar = -1;
            EncodeType encodeType = EncodeType.Ansi;

            // iterate, starting past " SYMBOL"
            int i = 7;
            while (i < instr.Length)
            {
                // Skip spaces
                if (instr[i] == ' ')
                {
                    i++;
                }
                // Is this the character code?
                else if (nChar == -1 && instr[i] >= '0' && instr[i] <= '9')
                {
                    // Hex or decimal?
                    if (instr[i] == '0' && i < instr.Length - 1 && instr[i + 1] == 'x')
                    {
                        i += 2;

                        nChar = 0;
                        for (; i < instr.Length && instr[i] != ' ' && instr[i] != '\\'; i++)
                        {
                            if (nChar < 0xFFFF)
                            {
                                nChar = (nChar * 16) + HexToInt(instr[i]);
                            }
                        }
                    }
                    else
                    {
                        nChar = 0;
                        for (; i < instr.Length && instr[i] != ' ' && instr[i] != '\\'; i++)
                        {
                            if (nChar < 0xFFFF)
                            {
                                nChar = (nChar * 10) + DecToInt(instr[i]);
                            }
                        }
                    }
                }
                // Instructions
                else if (instr[i] == '\\')
                {
                    i++;
                    if (i < instr.Length)
                    {
                        ProcessSymbolFieldInstruction(dn, instr, ref i, ref encodeType);
                    }
                }
                else
                {
                    i++;
                }
            }

            // If no character code was specified, just bail
            if (nChar == -1)
            {
                return null;
            }

            // Otherwise, convert it to text
            ConvertSymbolCharValueToText(dn, nChar, encodeType);

            return (dn.Xaml != null && dn.Xaml.Length > 0) ? dn : null;
        }

        private void ProcessSymbolFieldInstruction(DocumentNode dn, string instr, ref int i, ref EncodeType encodeType)
        {
            int iStart = 0;

            switch (instr[i++])
            {
                case 'a':
                    encodeType = EncodeType.Ansi;
                    break;
                case 'u':
                    encodeType = EncodeType.Unicode;
                    break;
                case 'j':
                    encodeType = EncodeType.ShiftJis;
                    break;
                case 'h':
                    // linespacing instruction: ignore
                    break;
                case 's':
                    if (i < instr.Length && instr[i] == ' ')
                        i++;
                    // font size in points, not half-points
                    iStart = i;
                    for (; i < instr.Length && instr[i] != ' '; i++)
                    {
                        continue;
                    }
                    string ptString = instr.Substring(iStart, i - iStart);

                    // Now convert number part
                    bool ret = true;
                    double d = 0f;

                    try
                    {
                        d = System.Convert.ToDouble(ptString, CultureInfo.InvariantCulture);
                    }
                    catch (System.OverflowException)
                    {
                        ret = false;
                    }
                    catch (System.FormatException)
                    {
                        ret = false;
                    }

                    if (ret)
                    {
                        dn.FormatState.FontSize = (long)((d * 2) + 0.5);
                    }
                    break;
                case 'f':
                    // Font Name
                    if (i < instr.Length && instr[i] == ' ')
                    {
                        i++;
                    }
                    if (i < instr.Length && instr[i] == '"')
                    {
                        i++;
                    }
                    iStart = i;
                    for (; i < instr.Length && instr[i] != '"'; i++)
                    {
                        continue;
                    }
                    string name = instr.Substring(iStart, i - iStart);
                    // Move past trailing double-quote
                    i++;
                    if (name != null && name.Length > 0)
                    {
                        dn.FormatState.Font = _converterState.FontTable.DefineEntryByName(name);
                    }
                    break;
            }
        }

        // Process image that create image node for generating Xaml Image control
        // Disable inlining to avoid loading System.Drawing.Bitmap without need
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ProcessImage(FormatState formatState)
        {
            string contentType;
            string imagePartUriString;

            switch (formatState.ImageFormat)
            {
                case RtfImageFormat.Wmf:
                case RtfImageFormat.Png:
                    contentType = "image/png";
                    break;

                case RtfImageFormat.Jpeg:
                    contentType = "image/jpeg";
                    break;

                default:
                    contentType = string.Empty;
                    break;
            }

            bool skipImage = (formatState.ImageScaleWidth < 0) || (formatState.ImageScaleHeight < 0);

            if (_wpfPayload != null && contentType != string.Empty && !skipImage)
            {
                // Get image part URI string and image binary steam to write Rtf image data
                // into the container of WpfPayload
                Stream imageStream = _wpfPayload.CreateImageStream(_imageCount, contentType, out imagePartUriString);

                using (imageStream)
                {
                    if (formatState.ImageFormat != RtfImageFormat.Wmf)
                    {
                        // Write the image binary data on the container from Rtf image data
                        _lexer.WriteImageData(imageStream, formatState.IsImageDataBinary);
                    }
                    else
                    {
                        // Read the windows metafile from the rtf content and then convert it
                        // to bitmap data then save it as PNG on the container image part
                        MemoryStream metafileStream = new MemoryStream(); ;

                        using (metafileStream)
                        {
                            // Get Windows Metafile from rtf content
                            _lexer.WriteImageData(metafileStream, formatState.IsImageDataBinary);

                            metafileStream.Position = 0;

                            SystemDrawingHelper.SaveMetafileToImageStream(metafileStream, imageStream);
                        }
                    }
                }

                // Increase the image count to generate the image source name
                _imageCount++;

                formatState.ImageSource = imagePartUriString;

                // Create the image document node
                DocumentNode dnImage = new DocumentNode(DocumentNodeType.dnImage);

                dnImage.FormatState = formatState;

                StringBuilder imageStringBuilder = new StringBuilder();

                // Add the xaml image element
                imageStringBuilder.Append("<Image ");

                // Add the xaml image width property
                imageStringBuilder.Append(" Width=\"");
                double width;
                if (formatState.ImageScaleWidth != 0)
                {
                    width = formatState.ImageWidth * (formatState.ImageScaleWidth / 100);
                }
                else
                {
                    width = formatState.ImageWidth;
                }
                imageStringBuilder.Append(width.ToString(CultureInfo.InvariantCulture));
                imageStringBuilder.Append("\"");

                // Add the xaml image height property
                imageStringBuilder.Append(" Height=\"");
                double height = formatState.ImageHeight * (formatState.ImageScaleHeight / 100);
                if (formatState.ImageScaleHeight != 0)
                {
                    height = formatState.ImageHeight * (formatState.ImageScaleHeight / 100);
                }
                else
                {
                    height = formatState.ImageHeight;
                }
                imageStringBuilder.Append(height.ToString(CultureInfo.InvariantCulture));
                imageStringBuilder.Append("\"");

                // Add the xaml image baseline offset property
                if (formatState.IncludeImageBaselineOffset == true)
                {
                    double baselineOffset = height - formatState.ImageBaselineOffset;
                    imageStringBuilder.Append(" TextBlock.BaselineOffset=\"");
                    imageStringBuilder.Append(baselineOffset.ToString(CultureInfo.InvariantCulture));
                    imageStringBuilder.Append("\"");
                }

                // Add the xaml image stretch property
                imageStringBuilder.Append(" Stretch=\"Fill");
                imageStringBuilder.Append("\"");


                // Add the xaml image close tag
                imageStringBuilder.Append(">");

                // Add the image source property as the complex property
                // This is for specifying BitmapImage.CacheOption as OnLoad instead of
                // the default OnDemand option.
                imageStringBuilder.Append("<Image.Source>");
                imageStringBuilder.Append("<BitmapImage ");
                imageStringBuilder.Append("UriSource=\"");
                imageStringBuilder.Append(imagePartUriString);
                imageStringBuilder.Append("\" ");
                imageStringBuilder.Append("CacheOption=\"OnLoad\" ");
                imageStringBuilder.Append("/>");
                imageStringBuilder.Append("</Image.Source>");
                imageStringBuilder.Append("</Image>");

                // Set Xaml for image element
                dnImage.Xaml = imageStringBuilder.ToString();

                // Insert the image document node to the document node array
                DocumentNodeArray dna = _converterState.DocumentNodeArray;
                dna.Push(dnImage);
                dna.CloseAt(dna.Count - 1);
            }
            else
            {
                // Skip the image data if the image type is unknown or WpfPayload is null
                _lexer.AdvanceForImageData();
            }
        }

        private void ConvertSymbolCharValueToText(DocumentNode dn, int nChar, EncodeType encodeType)
        {
            switch (encodeType)
            {
                case EncodeType.Unicode:
                    if (nChar < 0xFFFF)
                    {
                        char[] unicodeChar = new char[1];

                        unicodeChar[0] = (char)nChar;
                        dn.AppendXamlEncoded(new string(unicodeChar));
                    }
                    break;

                case EncodeType.ShiftJis:
                    if (nChar < 0xFFFF)
                    {
                        // NB: How to interpret this numeric value as Shift-JIS?
                        Encoding ec = InternalEncoding.GetEncoding(932);
                        int nChars = nChar > 256 ? 2 : 1;
                        byte[] ba = new byte[2];
                        if (nChars == 1)
                        {
                            ba[0] = (byte)nChar;
                        }
                        else
                        {
                            ba[0] = (byte)((nChar >> 8) & 0xFF);
                            ba[1] = (byte)(nChar & 0xFF);
                        }
                        dn.AppendXamlEncoded(ec.GetString(ba, 0, nChars));
                    }
                    break;

                default:
                    if (nChar < 256)
                    {
                        // Keep the byte char value as the unicode
                        char singleChar = (char) nChar;
                        dn.AppendXamlEncoded(new string(singleChar, 1));
                    }
                    break;
            }
        }

        internal void ProcessListText()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            int ndnListText = dna.FindPending(DocumentNodeType.dnListText);
            if (ndnListText >= 0)
            {
                DocumentNode dnListText = dna.EntryAt(ndnListText);
                dna.CloseAt(ndnListText);

                // If I didn't throw away a picture, need to actually test for content.
                bool bWS = true;
                if (dnListText.HasMarkerContent)
                {
                    bWS = false;
                }
                else
                {
                    int nEnd = ndnListText + dnListText.ChildCount;
                    for (int i = ndnListText + 1; bWS && i <= nEnd; i++)
                    {
                        DocumentNode dnText = dna.EntryAt(i);

                        if (!dnText.IsWhiteSpace)
                        {
                            bWS = false;
                        }
                    }
                }
                dna.CoalesceChildren(_converterState, ndnListText);
                if (bWS)
                {
                    _converterState.IsMarkerWhiteSpace = true;
                }
                dnListText.Xaml = string.Empty;
                _converterState.IsMarkerPresent = true;
            }
        }

        internal void ProcessShapeResult()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            int nAt = dna.FindPending(DocumentNodeType.dnShape);

            if (nAt >= 0)
            {
                // Make sure any pending inlines are wrapped.
                FormatState formatState = _converterState.TopFormatState;
                if (formatState != null)
                {
                    WrapPendingInlineInParagraph(null, formatState);
                }

                // No structures should leak out of a shape result
                dna.CloseAt(nAt);
            }
        }

        private void ProcessRtfDestination(FormatState fsCur)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            int nAt;

            switch (fsCur.RtfDestination)
            {
                case RtfDestination.DestField:
                    nAt = dna.FindUnmatched(DocumentNodeType.dnFieldBegin);
                    if (nAt >= 0)
                    {
                        DocumentNode dnEnd = new DocumentNode(DocumentNodeType.dnFieldEnd);
                        dnEnd.FormatState = new FormatState(fsCur);
                        dnEnd.IsPending = false;
                        dna.Push(dnEnd);
                        dna.EntryAt(nAt).IsMatched = true;
                        ProcessField();
                    }
                    break;
                case RtfDestination.DestFieldInstruction:
                case RtfDestination.DestFieldPrivate:
                case RtfDestination.DestFieldResult:
                    nAt = dna.FindUnmatched(DocumentNodeType.dnFieldBegin);
                    if (nAt >= 0)
                    {
                        DocumentNode dnEnd = new DocumentNode(DocumentNodeType.dnFieldEnd);
                        dnEnd.FormatState = new FormatState(fsCur);
                        dnEnd.IsPending = false;
                        dna.Push(dnEnd);
                        dna.EntryAt(nAt).IsMatched = true;
                    }
                    break;

                // The DestShape destination is only to distinguish the case of leaving a shaperesult destination
                // when shapes were nested.  No processing is actually necessary here.
                case RtfDestination.DestShape:
                    break;

                case RtfDestination.DestShapeResult:
                    ProcessShapeResult();
                    break;

                case RtfDestination.DestListText:
                    ProcessListText();
                    break;

                case RtfDestination.DestListLevel:
                    {
                        ListTableEntry listTableEntry = _converterState.ListTable.CurrentEntry;
                        if (listTableEntry != null)
                        {
                            ListLevel listLevel = listTableEntry.Levels.CurrentEntry;
                            listLevel.FormatState = new FormatState(fsCur);
                        }
                    }
                    break;

                case RtfDestination.DestListOverride:
                    break;

                case RtfDestination.DestList:
                    break;

                case RtfDestination.DestFontName:
                    FontTableEntry entry = _converterState.FontTable.CurrentEntry;
                    if (entry != null)
                    {
                        entry.IsNameSealed = true;
                        entry.IsPending = false;
                    }
                    break;

                case RtfDestination.DestFontTable:
                    _converterState.FontTable.MapFonts();
                    break;
            }
        }

        internal void ProcessGroupEnd()
        {
            // Pop the state for this group.  Don't overpop if we are handed bad input
            if (_converterState.RtfFormatStack.Count > 2)
            {
                DocumentNodeArray dna = _converterState.DocumentNodeArray;
                FormatState fsCur = _converterState.PreviousTopFormatState(0);
                FormatState fsNew = _converterState.PreviousTopFormatState(1);

                // Various interesting actions are taken when the destination changes
                if (fsCur.RtfDestination != fsNew.RtfDestination)
                {
                    ProcessRtfDestination(fsCur);
                }
                else if (fsNew.RtfDestination == RtfDestination.DestFontTable)
                {
                    FontTableEntry entry = _converterState.FontTable.CurrentEntry;
                    if (entry != null)
                    {
                        entry.IsPending = false;
                    }
                }

                _converterState.RtfFormatStack.Pop();
                if (fsNew.CodePage == -1)
                {
                    _lexer.CodePage = _converterState.CodePage;
                }
                else
                {
                    _lexer.CodePage = fsNew.CodePage;
                }

                // After seeing font table, setup default font for current state.
                if (fsCur.RtfDestination == RtfDestination.DestFontTable
                    && _converterState.DefaultFont >= 0)
                {
                    SelectFont(_converterState.DefaultFont);
                }

                // If we ever pop to a state without a default font, re-assert it.
                else if (fsCur.Font < 0 && _converterState.DefaultFont >= 0)
                {
                    SelectFont(_converterState.DefaultFont);
                }
            }
        }

        internal void SelectFont(long nFont)
        {
            FormatState formatState = _converterState.TopFormatState;

            if (formatState == null)
            {
                return;
            }

            formatState.Font = nFont;
            FontTableEntry entry = _converterState.FontTable.FindEntryByIndex((int)formatState.Font);
            if (entry != null)
            {
                if (entry.CodePage == -1)
                {
                    formatState.CodePage = _converterState.CodePage;
                }
                else
                {
                    formatState.CodePage = entry.CodePage;
                }
                _lexer.CodePage = formatState.CodePage;
            }
        }

        internal void HandleControl(RtfToken token, RtfControlWordInfo controlWordInfo)
        {
            FormatState formatState = _converterState.TopFormatState;

            if (formatState == null)
            {
                return;
            }

            switch (controlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_V:
                    formatState.IsHidden = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_DELETED:
                    formatState.IsHidden = true;
                    break;
                case RtfControlWord.Ctrl_B:
                    formatState.Bold = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_I:
                    formatState.Italic = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_IMPR:
                    formatState.Engrave = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_OUTL:
                    formatState.Outline = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_SHAD:
                    formatState.Shadow = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_SCAPS:
                    formatState.SCaps = token.ToggleValue > 0;
                    break;
                case RtfControlWord.Ctrl_FS:
                    if (Validators.IsValidFontSize(token.Parameter))
                        formatState.FontSize = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_EXPND:
                case RtfControlWord.Ctrl_EXPNDTW:
                    formatState.Expand = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_F:
                    if (token.HasParameter)
                        if (formatState.RtfDestination == RtfDestination.DestFontTable)
                        {
                            _converterState.FontTable.DefineEntry((int)token.Parameter);
                        }
                        else
                        {
                            SelectFont(token.Parameter);

                            // Setting font also sets current language in effect
                            if (formatState.FontSlot == FontSlot.DBCH)
                            {
                                if (formatState.LangFE > 0)
                                {
                                    formatState.LangCur = formatState.LangFE;
                                }
                                else if (_converterState.DefaultLangFE > 0)
                                {
                                    formatState.LangCur = _converterState.DefaultLangFE;
                                }
                            }
                            else
                            {
                                if (formatState.Lang > 0)
                                {
                                    formatState.LangCur = formatState.Lang;
                                }
                                else if (_converterState.DefaultLang > 0)
                                {
                                    formatState.LangCur = _converterState.DefaultLang;
                                }
                            }
                        }

                    break;
                case RtfControlWord.Ctrl_DBCH:
                    formatState.FontSlot = FontSlot.DBCH;
                    break;
                case RtfControlWord.Ctrl_LOCH:
                    formatState.FontSlot = FontSlot.LOCH;
                    break;
                case RtfControlWord.Ctrl_HICH:
                    formatState.FontSlot = FontSlot.HICH;
                    break;
                case RtfControlWord.Ctrl_LANG:
                    formatState.Lang = token.Parameter;
                    formatState.LangCur = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_LANGFE:
                    formatState.LangFE = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_DEFLANG:
                    _converterState.DefaultLang = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_DEFLANGFE:
                    _converterState.DefaultLangFE = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_DEFF:
                    _converterState.DefaultFont = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_FNAME:
                    {
                        FormatState previousFormatState = _converterState.PreviousTopFormatState(1);
                        if (previousFormatState.RtfDestination == RtfDestination.DestFontTable)
                        {
                            formatState.RtfDestination = RtfDestination.DestFontName;
                            FontTableEntry entry = _converterState.FontTable.CurrentEntry;
                            if (entry != null)
                            {
                                entry.Name = null;
                            }
                        }
                    }
                    break;
                case RtfControlWord.Ctrl_FCHARSET:
                    if (formatState.RtfDestination == RtfDestination.DestFontTable)
                    {
                        HandleFontTableTokens(token);
                    }
                    break;
                case RtfControlWord.Ctrl_HIGHLIGHT:
                    formatState.CB = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CB:
                    formatState.CB = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CF:
                    formatState.CF = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_SUB:
                    formatState.Sub = token.FlagValue;
                    if (formatState.Sub)
                    {
                        formatState.Super = false;
                    }
                    break;
                case RtfControlWord.Ctrl_SUPER:
                    formatState.Super = token.FlagValue;
                    if (formatState.Super)
                    {
                        formatState.Sub = false;
                    }
                    break;
                case RtfControlWord.Ctrl_NOSUPERSUB:
                    formatState.Sub = false;
                    formatState.Super = false;

                    formatState.SuperOffset = 0;
                    break;
                case RtfControlWord.Ctrl_UP:
                    formatState.SuperOffset = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_ULC:
                    // NB: underline color not implemented
                    break;
                case RtfControlWord.Ctrl_UL:
                case RtfControlWord.Ctrl_ULD:
                case RtfControlWord.Ctrl_ULDASH:
                case RtfControlWord.Ctrl_ULDASHD:
                case RtfControlWord.Ctrl_ULDASHDD:
                case RtfControlWord.Ctrl_ULDB:
                case RtfControlWord.Ctrl_ULHAIR:
                case RtfControlWord.Ctrl_ULHWAVE:
                case RtfControlWord.Ctrl_ULLDASH:
                case RtfControlWord.Ctrl_ULTH:
                case RtfControlWord.Ctrl_ULTHD:
                case RtfControlWord.Ctrl_ULTHDASH:
                case RtfControlWord.Ctrl_ULTHDASHD:
                case RtfControlWord.Ctrl_ULTHDASHDD:
                case RtfControlWord.Ctrl_ULTHLDASH:
                case RtfControlWord.Ctrl_ULULDBWAVE:
                case RtfControlWord.Ctrl_ULWAVE:
                case RtfControlWord.Ctrl_ULW:
                    formatState.UL = (token.ToggleValue > 0 ? ULState.ULNormal : ULState.ULNone);
                    break;
                case RtfControlWord.Ctrl_ULNONE:
                    formatState.UL = ULState.ULNone;
                    break;
                case RtfControlWord.Ctrl_STRIKE:
                    formatState.Strike = token.ToggleValue > 0 ? StrikeState.StrikeNormal : StrikeState.StrikeNone;
                    break;
                case RtfControlWord.Ctrl_STRIKED:
                    formatState.Strike = token.ToggleValue > 0 ? StrikeState.StrikeDouble : StrikeState.StrikeNone;
                    break;
                case RtfControlWord.Ctrl_PLAIN:
                    formatState.SetCharDefaults();
                    if (_converterState.DefaultFont >= 0)
                    {
                        SelectFont(_converterState.DefaultFont);
                    }
                    break;

                // Paragraph settings
                case RtfControlWord.Ctrl_PARD:
                    formatState.SetParaDefaults();
                    break;
                case RtfControlWord.Ctrl_SB:
                    formatState.SB = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_SA:
                    formatState.SA = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_FI:
                    // Word first indent(\fiN) and Avalon TextIndent concept is the different.
                    // So we ignore the negative first indent safely which can make the content invisible.
                    // Word: \fi-200        -200    0   200
                    //                              abc
                    //                                  xyz
                    // Word: \fi200         -200    0   200
                    //                                  abc
                    //                              xyz
                    if (token.Parameter > 0)
                    {
                        formatState.FI = token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_LIN:
                    // We ignore \lin and do RTL fixup on margins outselves.
                    // formatState.LI = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_LI:
                    formatState.LI = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_RIN:
                    // We ignore \rin and do RTL fixup on margins outselves.
                    // formatState.RI = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_RI:
                    formatState.RI = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_QC:
                    formatState.HAlign = HAlign.AlignCenter;
                    break;
                case RtfControlWord.Ctrl_QJ:
                    formatState.HAlign = HAlign.AlignJustify;
                    break;
                case RtfControlWord.Ctrl_QL:
                    formatState.HAlign = HAlign.AlignLeft;
                    break;
                case RtfControlWord.Ctrl_QR:
                    formatState.HAlign = HAlign.AlignRight;
                    break;
                case RtfControlWord.Ctrl_CFPAT:
                    formatState.CFPara = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_SHADING:
                    // Shading of the CFPAT color, in 100's of a percent.
                    formatState.ParaShading = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CBPAT:
                    formatState.CBPara = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_SL:
                    formatState.SL = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_SLMULT:
                    formatState.SLMult = token.ToggleValue > 0;
                    break;

                // Special text
                case RtfControlWord.Ctrl_LINE:
                    ProcessHardLine(token, formatState);
                    break;

                // LTR-RTL stuff
                case RtfControlWord.Ctrl_LTRCH:
                    formatState.DirChar = DirState.DirLTR;
                    break;
                case RtfControlWord.Ctrl_LTRPAR:
                case RtfControlWord.Ctrl_LTRDOC:
                case RtfControlWord.Ctrl_LTRSECT:
                    formatState.DirPara = DirState.DirLTR;
                    break;
                case RtfControlWord.Ctrl_RTLCH:
                    formatState.DirChar = DirState.DirRTL;
                    break;
                case RtfControlWord.Ctrl_RTLPAR:
                case RtfControlWord.Ctrl_RTLDOC:
                case RtfControlWord.Ctrl_RTLSECT:
                    formatState.DirPara = DirState.DirRTL;
                    // RTF defaults paragraph alignment to left for both RTL and LTR paragraphs, but
                    // Avalon switches the default.  Hard-code the alignment here when dealing with RTL
                    // paragraphs so that the distinction doesn't result in different defaults.
                    if (formatState.HAlign == HAlign.AlignDefault)
                    {
                        formatState.HAlign = HAlign.AlignLeft;
                    }
                    break;
                case RtfControlWord.Ctrl_LTRMARK:
                    ProcessText(new string('\x200e', 1));
                    break;
                case RtfControlWord.Ctrl_RTLMARK:
                    ProcessText(new string('\x200f', 1));
                    break;

                // Structure output
                case RtfControlWord.Ctrl_PAR:
                case RtfControlWord.Ctrl_SECT:
                    HandlePara(token, formatState);
                    break;

                case RtfControlWord.Ctrl_PAGE:
                    HandlePage(token, formatState);
                    break;

                // Table Tokens
                case RtfControlWord.Ctrl_CELL:
                case RtfControlWord.Ctrl_NESTCELL:
                case RtfControlWord.Ctrl_ROW:
                case RtfControlWord.Ctrl_TROWD:
                case RtfControlWord.Ctrl_NESTROW:
                case RtfControlWord.Ctrl_NESTTABLEPROPS:
                    HandleTableTokens(token, formatState);
                    break;

                // Table property tokens
                case RtfControlWord.Ctrl_TRGAPH:
                case RtfControlWord.Ctrl_TRLEFT:
                case RtfControlWord.Ctrl_TRQC:
                case RtfControlWord.Ctrl_TRQL:
                case RtfControlWord.Ctrl_TRQR:
                case RtfControlWord.Ctrl_TRPADDL:
                case RtfControlWord.Ctrl_TRPADDR:
                case RtfControlWord.Ctrl_TRPADDB:
                case RtfControlWord.Ctrl_TRPADDT:
                case RtfControlWord.Ctrl_TRPADDFL:
                case RtfControlWord.Ctrl_TRPADDFT:
                case RtfControlWord.Ctrl_TRPADDFB:
                case RtfControlWord.Ctrl_TRPADDFR:
                case RtfControlWord.Ctrl_TRFTSWIDTH:
                case RtfControlWord.Ctrl_TRFTSWIDTHB:
                case RtfControlWord.Ctrl_TRFTSWIDTHA:
                case RtfControlWord.Ctrl_TRWWIDTH:
                case RtfControlWord.Ctrl_TRWWIDTHB:
                case RtfControlWord.Ctrl_TRWWIDTHA:
                case RtfControlWord.Ctrl_TRAUTOFIT:
                case RtfControlWord.Ctrl_CLWWIDTH:
                case RtfControlWord.Ctrl_CLFTSWIDTH:
                case RtfControlWord.Ctrl_TRBRDRT:
                case RtfControlWord.Ctrl_TRBRDRB:
                case RtfControlWord.Ctrl_TRBRDRR:
                case RtfControlWord.Ctrl_TRBRDRL:
                case RtfControlWord.Ctrl_TRBRDRV:
                case RtfControlWord.Ctrl_TRBRDRH:
                case RtfControlWord.Ctrl_CLVERTALT:
                case RtfControlWord.Ctrl_CLVERTALB:
                case RtfControlWord.Ctrl_CLVERTALC:
                case RtfControlWord.Ctrl_CLSHDRAWNIL:
                case RtfControlWord.Ctrl_CLSHDNG:
                case RtfControlWord.Ctrl_CLBRDRB:
                case RtfControlWord.Ctrl_CLBRDRR:
                case RtfControlWord.Ctrl_CLBRDRT:
                case RtfControlWord.Ctrl_CLBRDRL:
                case RtfControlWord.Ctrl_CLCBPAT:
                case RtfControlWord.Ctrl_CLCFPAT:
                case RtfControlWord.Ctrl_CLPADL:
                case RtfControlWord.Ctrl_CLPADFL:
                case RtfControlWord.Ctrl_CLPADR:
                case RtfControlWord.Ctrl_CLPADFR:
                case RtfControlWord.Ctrl_CLPADT:
                case RtfControlWord.Ctrl_CLPADFT:
                case RtfControlWord.Ctrl_CLPADB:
                case RtfControlWord.Ctrl_CLPADFB:
                case RtfControlWord.Ctrl_CELLX:
                case RtfControlWord.Ctrl_BRDRART:
                case RtfControlWord.Ctrl_BRDRB:
                case RtfControlWord.Ctrl_BRDRNIL:
                case RtfControlWord.Ctrl_BRDRNONE:
                case RtfControlWord.Ctrl_BRDRTBL:
                case RtfControlWord.Ctrl_BRDRBAR:
                case RtfControlWord.Ctrl_BRDRBTW:
                case RtfControlWord.Ctrl_BRDRCF:
                case RtfControlWord.Ctrl_BRDRDASH:
                case RtfControlWord.Ctrl_BRDRDASHD:
                case RtfControlWord.Ctrl_BRDRDASHDD:
                case RtfControlWord.Ctrl_BRDRDASHDOTSTR:
                case RtfControlWord.Ctrl_BRDRDASHSM:
                case RtfControlWord.Ctrl_BRDRDB:
                case RtfControlWord.Ctrl_BRDRDOT:
                case RtfControlWord.Ctrl_BRDREMBOSS:
                case RtfControlWord.Ctrl_BRDRENGRAVE:
                case RtfControlWord.Ctrl_BRDRFRAME:
                case RtfControlWord.Ctrl_BRDRHAIR:
                case RtfControlWord.Ctrl_BRDRINSET:
                case RtfControlWord.Ctrl_BRDRL:
                case RtfControlWord.Ctrl_BRDROUTSET:
                case RtfControlWord.Ctrl_BRDRR:
                case RtfControlWord.Ctrl_BRDRS:
                case RtfControlWord.Ctrl_BRDRSH:
                case RtfControlWord.Ctrl_BRDRT:
                case RtfControlWord.Ctrl_BRDRTH:
                case RtfControlWord.Ctrl_BRDRTHTNLG:
                case RtfControlWord.Ctrl_BRDRTHTNMG:
                case RtfControlWord.Ctrl_BRDRTHTNSG:
                case RtfControlWord.Ctrl_BRDRTNTHLG:
                case RtfControlWord.Ctrl_BRDRTNTHMG:
                case RtfControlWord.Ctrl_BRDRTNTHSG:
                case RtfControlWord.Ctrl_BRDRTNTHTNLG:
                case RtfControlWord.Ctrl_BRDRTNTHTNMG:
                case RtfControlWord.Ctrl_BRDRTNTHTNSG:
                case RtfControlWord.Ctrl_BRDRTRIPLE:
                case RtfControlWord.Ctrl_BRDRW:
                case RtfControlWord.Ctrl_BRDRWAVY:
                case RtfControlWord.Ctrl_BRDRWAVYDB:
                case RtfControlWord.Ctrl_BRSP:
                case RtfControlWord.Ctrl_BOX:
                case RtfControlWord.Ctrl_RTLROW:
                case RtfControlWord.Ctrl_LTRROW:
                case RtfControlWord.Ctrl_TRSPDB:
                case RtfControlWord.Ctrl_TRSPDFB:
                case RtfControlWord.Ctrl_TRSPDFL:
                case RtfControlWord.Ctrl_TRSPDFR:
                case RtfControlWord.Ctrl_TRSPDFT:
                case RtfControlWord.Ctrl_TRSPDL:
                case RtfControlWord.Ctrl_TRSPDR:
                case RtfControlWord.Ctrl_TRSPDT:
                case RtfControlWord.Ctrl_CLMGF:
                case RtfControlWord.Ctrl_CLMRG:
                case RtfControlWord.Ctrl_CLVMGF:
                case RtfControlWord.Ctrl_CLVMRG:
                    HandleTableProperties(token, formatState);
                    break;

                // Symbols
                case RtfControlWord.Ctrl_TAB:
                    ProcessText("\t");
                    break;
                case RtfControlWord.Ctrl_EMDASH:
                    ProcessText("\x2014");
                    break;
                case RtfControlWord.Ctrl_ENDASH:
                    ProcessText("\x2013");
                    break;
                case RtfControlWord.Ctrl_EMSPACE:
                    ProcessText("\x2003");
                    break;
                case RtfControlWord.Ctrl_ENSPACE:
                    ProcessText("\x2002");
                    break;
                case RtfControlWord.Ctrl_QMSPACE:
                    ProcessText("\x2005");
                    break;
                case RtfControlWord.Ctrl_BULLET:
                    ProcessText("\x2022");  // Unicode bullet
                    break;
                case RtfControlWord.Ctrl_LQUOTE:
                    ProcessText("\x2018");
                    break;
                case RtfControlWord.Ctrl_RQUOTE:
                    ProcessText("\x2019");
                    break;
                case RtfControlWord.Ctrl_LDBLQUOTE:
                    ProcessText("\x201c");
                    break;
                case RtfControlWord.Ctrl_RDBLQUOTE:
                    ProcessText("\x201d");
                    break;
                case RtfControlWord.Ctrl_ZWJ:  // zero-width-joiner
                    ProcessText("\x200d");
                    break;
                case RtfControlWord.Ctrl_ZWNJ: // zero-width-non-joiner
                    ProcessText("\x200c");
                    break;

                case RtfControlWord.Ctrl_ANSICPG:
                    // This is just a hint for RTF output roundtripping - ignore.
                    break;

                // CodePage Tokens
                case RtfControlWord.Ctrl_ANSI:
                case RtfControlWord.Ctrl_MAC:
                case RtfControlWord.Ctrl_PC:
                case RtfControlWord.Ctrl_PCA:
                case RtfControlWord.Ctrl_UPR:
                case RtfControlWord.Ctrl_UD:
                case RtfControlWord.Ctrl_UC:
                    HandleCodePageTokens(token, formatState);
                    break;
                case RtfControlWord.Ctrl_U:
                    HandleCodePageTokens(token, formatState);
                    _lexer.AdvanceForUnicode(formatState.UnicodeSkip);
                    break;

                // Field (hyperlink) commands
                case RtfControlWord.Ctrl_FIELD:
                case RtfControlWord.Ctrl_FLDRSLT:
                case RtfControlWord.Ctrl_FLDINST:
                case RtfControlWord.Ctrl_FLDPRIV:
                    HandleFieldTokens(token, formatState);
                    break;

                // Structure commands
                case RtfControlWord.Ctrl_ILVL:
                    formatState.ILVL = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_INTBL:
                    formatState.IsInTable = token.FlagValue;
                    break;

                case RtfControlWord.Ctrl_LS:
                    if (formatState.RtfDestination == RtfDestination.DestListOverride)
                    {
                        HandleListTokens(token, formatState);
                    }
                    else
                    {
                        formatState.ILS = token.Parameter;
                    }
                    break;

                case RtfControlWord.Ctrl_ITAP:
                    formatState.ITAP = token.Parameter;
                    break;

                // Other special parsing commands
                case RtfControlWord.Ctrl_BIN:
                    HandleBinControl(token, formatState);
                    break;

                // List Table commands
                case RtfControlWord.Ctrl_LIST:
                case RtfControlWord.Ctrl_LISTTEMPLATEID:
                case RtfControlWord.Ctrl_LISTHYBRID:
                case RtfControlWord.Ctrl_LISTSIMPLE:
                case RtfControlWord.Ctrl_LISTLEVEL:
                case RtfControlWord.Ctrl_LISTTEXT:
                case RtfControlWord.Ctrl_LEVELNFC:
                case RtfControlWord.Ctrl_LEVELNFCN:
                case RtfControlWord.Ctrl_LEVELJC:
                case RtfControlWord.Ctrl_LEVELJCN:
                case RtfControlWord.Ctrl_LEVELFOLLOW:
                case RtfControlWord.Ctrl_LEVELSTARTAT:
                case RtfControlWord.Ctrl_LEVELSPACE:
                case RtfControlWord.Ctrl_LEVELINDENT:
                case RtfControlWord.Ctrl_LEVELTEXT:
                case RtfControlWord.Ctrl_LEVELTEMPLATEID:
                case RtfControlWord.Ctrl_LISTID:
                case RtfControlWord.Ctrl_LEVELNUMBERS:
                case RtfControlWord.Ctrl_LISTOVERRIDE:
                    HandleListTokens(token, formatState);
                    break;

                case RtfControlWord.Ctrl_LISTPICTURE:
                    formatState.RtfDestination = RtfDestination.DestListPicture;
                    break;

                // Old-style list info
                case RtfControlWord.Ctrl_PNLVL:
                case RtfControlWord.Ctrl_PNLVLBLT:
                case RtfControlWord.Ctrl_PNLVLBODY:
                case RtfControlWord.Ctrl_PNLVLCONT:
                case RtfControlWord.Ctrl_PNCARD:
                case RtfControlWord.Ctrl_PNDEC:
                case RtfControlWord.Ctrl_PNUCLTR:
                case RtfControlWord.Ctrl_PNUCRM:
                case RtfControlWord.Ctrl_PNLCLTR:
                case RtfControlWord.Ctrl_PNLCRM:
                case RtfControlWord.Ctrl_PNORD:
                case RtfControlWord.Ctrl_PNORDT:
                case RtfControlWord.Ctrl_PNBIDIA:
                case RtfControlWord.Ctrl_PNBIDIB:
                case RtfControlWord.Ctrl_PN:
                case RtfControlWord.Ctrl_PNTXTA:
                case RtfControlWord.Ctrl_PNTXTB:
                case RtfControlWord.Ctrl_PNTEXT:
                case RtfControlWord.Ctrl_PNSTART:
                    HandleOldListTokens(token, formatState);
                    break;

                // Shapes (we only read down-level info)
                case RtfControlWord.Ctrl_DO:
                case RtfControlWord.Ctrl_SHPRSLT:
                case RtfControlWord.Ctrl_DPTXBXTEXT:
                case RtfControlWord.Ctrl_SHPPICT:
                case RtfControlWord.Ctrl_NONSHPPICT:
                    HandleShapeTokens(token, formatState);
                    break;

                // Document tables
                case RtfControlWord.Ctrl_FONTTBL:
                    formatState.RtfDestination = RtfDestination.DestFontTable;
                    break;

                case RtfControlWord.Ctrl_COLORTBL:
                    formatState.RtfDestination = RtfDestination.DestColorTable;
                    break;

                case RtfControlWord.Ctrl_LISTTABLE:
                    formatState.RtfDestination = RtfDestination.DestListTable;
                    break;

                case RtfControlWord.Ctrl_LISTOVERRIDETABLE:
                    formatState.RtfDestination = RtfDestination.DestListOverrideTable;
                    break;

                case RtfControlWord.Ctrl_RED:
                    if (formatState.RtfDestination == RtfDestination.DestColorTable)
                    {
                        _converterState.ColorTable.NewRed = (byte)token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_GREEN:
                    if (formatState.RtfDestination == RtfDestination.DestColorTable)
                        _converterState.ColorTable.NewGreen = (byte)token.Parameter;
                    break;
                case RtfControlWord.Ctrl_BLUE:
                    if (formatState.RtfDestination == RtfDestination.DestColorTable)
                    {
                        _converterState.ColorTable.NewBlue = (byte)token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_SHPINST:
                    formatState.RtfDestination = RtfDestination.DestShapeInstruction;
                    break;
                case RtfControlWord.Ctrl_PICT:
                    {
                        FormatState previousFormatState = _converterState.PreviousTopFormatState(1);

                        // Filter the redundant picture or list picture
                        if ((previousFormatState.RtfDestination == RtfDestination.DestShapePicture ||
                             previousFormatState.RtfDestination == RtfDestination.DestShapeInstruction) ||
                            (previousFormatState.RtfDestination != RtfDestination.DestNoneShapePicture &&
                             previousFormatState.RtfDestination != RtfDestination.DestShape &&
                             previousFormatState.RtfDestination != RtfDestination.DestListPicture))
                        {
                            formatState.RtfDestination = RtfDestination.DestPicture;
                        }
                    }
                    break;
                case RtfControlWord.Ctrl_PNGBLIP:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageFormat = RtfImageFormat.Png;
                    }
                    break;
                case RtfControlWord.Ctrl_JPEGBLIP:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageFormat = RtfImageFormat.Jpeg;
                    }
                    break;
                case RtfControlWord.Ctrl_WMETAFILE:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageFormat = RtfImageFormat.Wmf;
                    }
                    break;
                case RtfControlWord.Ctrl_DN:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        // DN comes in half points
                        formatState.ImageBaselineOffset = Converters.HalfPointToPositivePx(token.Parameter);
                        formatState.IncludeImageBaselineOffset = true;
                    }
                    break;
                case RtfControlWord.Ctrl_PICHGOAL:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageHeight = Converters.TwipToPositivePx(token.Parameter);
                    }
                    break;
                case RtfControlWord.Ctrl_PICWGOAL:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageWidth = Converters.TwipToPositivePx(token.Parameter);
                    }
                    break;
                case RtfControlWord.Ctrl_PICSCALEX:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageScaleWidth = token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_PICSCALEY:
                    if (formatState.RtfDestination == RtfDestination.DestPicture)
                    {
                        formatState.ImageScaleHeight = token.Parameter;
                    }
                    break;
            }
        }

        internal void ProcessText(RtfToken token)
        {
            FormatState fs = _converterState.TopFormatState;
            if (fs.IsHidden)
            {
                return;
            }

            switch (fs.RtfDestination)
            {
                case RtfDestination.DestFieldResult:
                case RtfDestination.DestShapeResult:
                case RtfDestination.DestNormal:
                case RtfDestination.DestListText:
                    HandleNormalText(token.Text, fs);
                    break;
                case RtfDestination.DestFontTable:
                case RtfDestination.DestFontName:
                    ProcessFontTableText(token);
                    break;
                case RtfDestination.DestColorTable:
                    ProcessColorTableText(token);
                    break;
                case RtfDestination.DestField:
                case RtfDestination.DestFieldInstruction:
                case RtfDestination.DestFieldPrivate:
                    ProcessFieldText(token);
                    break;
            }
        }

        internal void ProcessTextSymbol(RtfToken token)
        {
            // Quoted control character (be generous) - should be limited to "'-*;\_{|}~"

            if (token.Text.Length == 0)
            {
                return;
            }

            // Set token text with the quoted control character
            SetTokenTextWithControlCharacter(token);

            switch (_converterState.TopFormatState.RtfDestination)
            {
                case RtfDestination.DestNormal:
                case RtfDestination.DestFieldResult:
                case RtfDestination.DestShapeResult:
                case RtfDestination.DestListText:
                    HandleNormalText(token.Text, _converterState.TopFormatState);
                    break;
                case RtfDestination.DestFontTable:
                    ProcessFontTableText(token);
                    break;
                case RtfDestination.DestColorTable:
                    ProcessColorTableText(token);
                    break;
                case RtfDestination.DestField:
                case RtfDestination.DestFieldInstruction:
                case RtfDestination.DestFieldPrivate:
                    ProcessFieldText(token);
                    break;
            }
        }

        internal void HandleBinControl(RtfToken token, FormatState formatState)
        {
            if (token.Parameter > 0)
            {
                if (formatState.RtfDestination == RtfDestination.DestPicture)
                {
                    formatState.IsImageDataBinary = true;
                }
                else
                {
                    _lexer.AdvanceForBinary((int)token.Parameter);
                }
            }
        }

        internal void HandlePara(RtfToken token, FormatState formatState)
        {
            // Ignore \par in other destinations
            if (!formatState.IsContentDestination || formatState.IsHidden)
            {
                return;
            }

            // Arrival of paragraph tag allows us to rationalize structure information.
            // Various tags have told us things about this paragraph:
            //        \INTBL:        Paragraph is inside a table cell.
            //        \ITAP{N}:    Nesting level for table.  Note that I may not have yet
            //                    seen any tags that start the nested table.
            //        \LS{N}:        Listtable override index.  Any value indicates this paragraph
            //                    is in a list.
            //        \ILVL:        Nesting level for list.

            // If we're in a throw-away destination, just return.
            HandleParagraphFromText(formatState);

            // Make sure proper number of tables are open to reflect this paragraphs nest level
            HandleTableNesting(formatState);

            // Now handle lists, which always behave as if they are inside tables.
            HandleListNesting(formatState);
        }

        internal void WrapPendingInlineInParagraph(RtfToken token, FormatState formatState)
        {
            // Ignore \par in other destinations
            if (!formatState.IsContentDestination || formatState.IsHidden)
            {
                return;
            }

            // Only treat \page as \par if there is already some inline content.  In normal cases,
            // (e.g. Word output) a page break comes between paragraphs and we don't want to emit
            // anything extra in that case.
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dn;

            // Insert the paragraph before any text or inline nodes at the top of the stack.
            int nNoOpValue = dna.Count;
            int nInsertAt = dna.Count;    // Default insertion location
            for (; nInsertAt > 0; nInsertAt--)
            {
                dn = dna.EntryAt(nInsertAt - 1);
                if (!dn.IsInline || dn.ClosedParent != null || !dn.IsMatched)
                {
                    break;
                }

                // If we only have listtext on the stack, don't force a para.
                else if (dn.Type == DocumentNodeType.dnListText && !dn.IsPending
                         && nInsertAt + dn.ChildCount == dna.Count)
                {
                    nNoOpValue = nInsertAt - 1;
                }
            }

            // If there are no inline nodes, don't generate extra content.
            if (nInsertAt == nNoOpValue)
            {
                return;
            }

            // Otherwise, just treat as paragraph mark
            HandlePara(token, formatState);
        }

        internal void HandlePage(RtfToken token, FormatState formatState)
        {
            WrapPendingInlineInParagraph(token, formatState);
        }

        internal void HandleParagraphFromText(FormatState formatState)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dn;

            // Insert the paragraph before any text or inline nodes at the top of the stack.
            int nInsertAt = dna.Count;    // Default insertion location
            for (; nInsertAt > 0; nInsertAt--)
            {
                dn = dna.EntryAt(nInsertAt - 1);
                if (!dn.IsInline
                    || (dn.ClosedParent != null && !dn.ClosedParent.IsInline)
                    || !dn.IsMatched)
                {
                    break;
                }
            }

            dn = new DocumentNode(DocumentNodeType.dnParagraph);
            dn.FormatState = new FormatState(formatState);
            dn.ConstrainFontPropagation(formatState);
            dna.InsertNode(nInsertAt, dn);

            // Now close immediately.
            dna.CloseAt(nInsertAt);
            dna.CoalesceOnlyChildren(_converterState, nInsertAt);
        }

        internal void WrapInlineInParagraph(int nInsertAt, int nChildren)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            Debug.Assert(nInsertAt >= 0 && nChildren > 0 && nInsertAt + nChildren - 1 < dna.Count);

            DocumentNode dnChild = dna.EntryAt(nInsertAt + nChildren - 1);
            DocumentNode dn = new DocumentNode(DocumentNodeType.dnParagraph);
            dn.FormatState = new FormatState(dnChild.FormatState);
            dn.ConstrainFontPropagation(dn.FormatState);

            DocumentNode dnParent = null;

            // Parent shouldn't be the child of new inserted document node
            // to avoid the infinite loop on InsertChildAt()
            if (dnChild.ClosedParent != null
                && dnChild.ClosedParent.Index < nInsertAt
                && dnChild.ClosedParent.Index > (nInsertAt + nChildren - 1))
            {
                dnParent = dnChild.ClosedParent;
            }

            dna.InsertChildAt(dnParent, dn, nInsertAt, nChildren);
            dna.CoalesceOnlyChildren(_converterState, nInsertAt);
        }

        internal void ProcessPendingTextAtRowEnd()
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // Remove any pending inline content before closing the row.
            int nExcise = 0;
            for (int i = dna.Count - 1; i >= 0; i--)
            {
                DocumentNode dn = dna.EntryAt(i);

                if (dn.IsInline && dn.ClosedParent == null)
                {
                    nExcise++;
                }
                else
                {
                    break;
                }
            }

            if (nExcise > 0)
            {
                dna.Excise(dna.Count - nExcise, nExcise);
            }
        }

        internal void HandleTableTokens(RtfToken token, FormatState formatState)
        {
            FormatState fsCur = _converterState.PreviousTopFormatState(0);
            FormatState fsOld = _converterState.PreviousTopFormatState(1);
            if (fsCur == null || fsOld == null)
            {
                return;
            }

            // Propagate current destination into nested table props destination keyword
            if (token.RtfControlWordInfo.Control == RtfControlWord.Ctrl_NESTTABLEPROPS)
            {
                fsCur.RtfDestination = fsOld.RtfDestination;
            }

            if (!formatState.IsContentDestination)
            {
                return;
            }

            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            int nCellAt;
            bool bOldHide = formatState.IsHidden;

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_CELL:
                    // Force a paragraph
                    // Set intbl and itap values, then use paragraph code.
                    formatState.IsInTable = true;
                    formatState.ITAP = 1;
                    formatState.IsHidden = false;
                    HandlePara(token, formatState);
                    formatState.IsHidden = bOldHide;
                    nCellAt = dna.FindPending(DocumentNodeType.dnCell);
                    if (nCellAt >= 0)
                    {
                        dna.CloseAt(nCellAt);

                        // Don't coalesce cell tag itself, since I might not have yet read the properties
                        // I need for writing out the cell attributes.
                        // Actually, even paragraph tags inside cell needs to have table info.
                        // dna.CoalesceOnlyChildren(_converterState, nCellAt);
                    }
                    break;

                case RtfControlWord.Ctrl_NESTCELL:
                    // Force a paragraph out.
                    formatState.IsHidden = false;
                    HandlePara(token, formatState);
                    formatState.IsHidden = bOldHide;

                    // If we encounter an open row before an open cell, we need to open a new cell.
                    // Or if we only have one level of table currently open.
                    int nOpenCells = dna.CountOpenCells();
                    DocumentNodeType scope = dna.GetTableScope();

                    if (scope != DocumentNodeType.dnCell || nOpenCells < 2)
                    {
                        HandlePara(token, formatState);
                    }

                    // Now close the currently open cell
                    nCellAt = dna.FindPending(DocumentNodeType.dnCell);
                    if (nCellAt >= 0)
                    {
                        dna.CloseAt(nCellAt);

                        // Don't coalesce cell tag itself, since I might not have yet read the properties
                        // I need for writing out the cell attributes.
                        // Actually, even paragraph tags inside cell needs to have table info.
                        // dna.CoalesceOnlyChildren(_converterState, nCellAt);
                    }
                    break;

                case RtfControlWord.Ctrl_TROWD:
                    formatState.IsHidden = false;
                    formatState.SetRowDefaults();
                    formatState.IsHidden = bOldHide;
                    break;

                case RtfControlWord.Ctrl_ROW:
                case RtfControlWord.Ctrl_NESTROW:
                    // Word puts out \row properties both before and after the cell contents.
                    // \nestrow properties are only put out *after* the cell contents.
                    formatState.IsHidden = false;
                    int nRowAt = dna.FindPending(DocumentNodeType.dnRow);
                    if (nRowAt >= 0)
                    {
                        DocumentNode dnRow = dna.EntryAt(nRowAt);
                        if (formatState.RowFormat != null)
                        {
                            dnRow.FormatState.RowFormat = new RowFormat(formatState.RowFormat);
                            dnRow.FormatState.RowFormat.CanonicalizeWidthsFromRTF();

                            // Also cache the row information in the table node
                            int nTable = dna.FindPendingFrom(DocumentNodeType.dnTable, nRowAt - 1, -1);
                            if (nTable >= 0)
                            {
                                DocumentNode dnTable = dna.EntryAt(nTable);
                                if (!dnTable.FormatState.HasRowFormat)
                                {
                                    dnTable.FormatState.RowFormat = dnRow.FormatState.RowFormat;
                                }
                            }
                        }

                        // Anomalous, but possible for illegal content.
                        ProcessPendingTextAtRowEnd();

                        dna.CloseAt(nRowAt);

                        // Don't coalesce - I need to examine all cells before writing things out
                        // dna.CoalesceChildren(_converterState, nRowAt);
                    }
                    formatState.IsHidden = bOldHide;
                    break;

                case RtfControlWord.Ctrl_NESTTABLEPROPS:
                    // Handled above.
                    break;
            }
        }

        internal ListOverride GetControllingListOverride()
        {
            ListTable lt = _converterState.ListTable;
            ListOverrideTable lot = _converterState.ListOverrideTable;
            RtfFormatStack formats = _converterState.RtfFormatStack;

            for (int i = formats.Count - 1; i >= 0; i--)
            {
                FormatState fs = formats.EntryAt(i);

                if (fs.RtfDestination == RtfDestination.DestListOverride)
                {
                    return lot.CurrentEntry;
                }
                else if (fs.RtfDestination == RtfDestination.DestListTable)
                {
                    return null;
                }
            }

            return null;
        }

        internal ListLevelTable GetControllingLevelTable()
        {
            ListTable lt = _converterState.ListTable;
            ListOverrideTable lot = _converterState.ListOverrideTable;
            RtfFormatStack formats = _converterState.RtfFormatStack;

            for (int i = formats.Count - 1; i >= 0; i--)
            {
                FormatState fs = formats.EntryAt(i);

                if (fs.RtfDestination == RtfDestination.DestListOverride)
                {
                    ListOverride lo = lot.CurrentEntry;

                    if (lo.Levels == null)
                    {
                        lo.Levels = new ListLevelTable();
                    }
                    return lo.Levels;
                }
                else if (fs.RtfDestination == RtfDestination.DestListTable)
                {
                    ListTableEntry lte = lt.CurrentEntry;

                    if (lte != null)
                    {
                        return lte.Levels;
                    }
                }
            }

            return null;
        }

        internal void HandleListTokens(RtfToken token, FormatState formatState)
        {
            ListTable listTable = _converterState.ListTable;
            ListOverrideTable listOverrideTable = _converterState.ListOverrideTable;

            FormatState fsCur = _converterState.PreviousTopFormatState(0);
            FormatState fsOld = _converterState.PreviousTopFormatState(1);
            if (fsCur == null || fsOld == null)
            {
                return;
            }

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_LIST:
                    if (formatState.RtfDestination == RtfDestination.DestListTable)
                    {
                        ListTableEntry listTableEntry = listTable.AddEntry();
                    }
                    break;

                case RtfControlWord.Ctrl_LISTTEMPLATEID:
                    {
                        ListTableEntry listTableEntry = listTable.CurrentEntry;
                        if (listTableEntry != null)
                        {
                            listTableEntry.TemplateID = token.Parameter;
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LISTHYBRID:
                case RtfControlWord.Ctrl_LISTSIMPLE:
                    {
                        ListTableEntry listTableEntry = listTable.CurrentEntry;
                        if (listTableEntry != null)
                        {
                            listTableEntry.Simple = token.RtfControlWordInfo.Control == RtfControlWord.Ctrl_LISTSIMPLE;
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LISTLEVEL:
                    {
                        formatState.RtfDestination = RtfDestination.DestListLevel;
                        ListLevelTable levels = GetControllingLevelTable();
                        if (levels != null)
                        {
                            ListLevel listLevel = levels.AddEntry();
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LISTTEXT:
                    if (fsOld.IsContentDestination || formatState.IsHidden)
                    {
                        formatState.RtfDestination = RtfDestination.DestListText;
                        DocumentNodeArray dna = _converterState.DocumentNodeArray;
                        DocumentNode dnl = new DocumentNode(DocumentNodeType.dnListText);

                        dnl.FormatState = new FormatState(formatState);
                        dna.Push(dnl);
                    }
                    break;

                case RtfControlWord.Ctrl_LEVELNFC:
                case RtfControlWord.Ctrl_LEVELNFCN:
                    {
                        ListLevelTable levels = GetControllingLevelTable();
                        if (levels != null)
                        {
                            ListLevel listLevel = levels.CurrentEntry;
                            if (listLevel != null)
                            {
                                listLevel.Marker = (MarkerStyle)token.Parameter;
                            }
                        }
                    }
                    break;
                case RtfControlWord.Ctrl_LEVELJC:
                case RtfControlWord.Ctrl_LEVELJCN:
                    // NB: Marker alignment not supported in XAML.
                    break;

                case RtfControlWord.Ctrl_LEVELFOLLOW:
                    break;

                case RtfControlWord.Ctrl_LEVELSTARTAT:
                    {
                        ListLevelTable levels = GetControllingLevelTable();
                        if (levels != null)
                        {
                            ListLevel listLevel = levels.CurrentEntry;
                            if (listLevel != null)
                            {
                                listLevel.StartIndex = token.Parameter;
                            }
                            else
                            {
                                // This is the case where the list override *only* specifies startat override.
                                ListOverride lo = GetControllingListOverride();

                                if (lo != null)
                                {
                                    lo.StartIndex = token.Parameter;
                                }
                            }
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LEVELSPACE:
                    break;
                case RtfControlWord.Ctrl_LEVELINDENT:
                    break;
                case RtfControlWord.Ctrl_LEVELTEXT:
                    break;
                case RtfControlWord.Ctrl_LEVELTEMPLATEID:
                    break;

                case RtfControlWord.Ctrl_LISTID:
                    {
                        if (formatState.RtfDestination == RtfDestination.DestListOverride)
                        {
                            ListOverride listOverride = listOverrideTable.CurrentEntry;
                            if (listOverride != null)
                            {
                                listOverride.ID = token.Parameter;
                            }
                        }
                        else
                        {
                            ListTableEntry listTableEntry = listTable.CurrentEntry;
                            if (listTableEntry != null)
                            {
                                listTableEntry.ID = token.Parameter;
                            }
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LEVELNUMBERS:
                    break;

                case RtfControlWord.Ctrl_LISTOVERRIDE:
                    {
                        FormatState previousFormatState = _converterState.PreviousTopFormatState(1);

                        if (previousFormatState.RtfDestination == RtfDestination.DestListOverrideTable)
                        {
                            formatState.RtfDestination = RtfDestination.DestListOverride;

                            ListOverride listOverride = listOverrideTable.AddEntry();
                        }
                    }
                    break;

                case RtfControlWord.Ctrl_LS:
                    if (formatState.RtfDestination == RtfDestination.DestListOverride)
                    {
                        ListOverride listOverride = listOverrideTable.CurrentEntry;
                        if (listOverride != null)
                        {
                            listOverride.Index = token.Parameter;
                        }
                    }
                    break;
            }
        }

        internal void HandleShapeTokens(RtfToken token, FormatState formatState)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            FormatState fsCur = _converterState.PreviousTopFormatState(0);
            FormatState fsOld = _converterState.PreviousTopFormatState(1);
            if (fsCur == null || fsOld == null)
            {
                return;
            }

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_DO:
                    // Just propagate destination through this keyword.
                    fsCur.RtfDestination = fsOld.RtfDestination;
                    break;
                case RtfControlWord.Ctrl_SHPRSLT:
                    if (fsOld.IsContentDestination)
                    {
                        fsCur.RtfDestination = RtfDestination.DestShape;
                    }
                    break;
                case RtfControlWord.Ctrl_DPTXBXTEXT:
                    if (fsOld.IsContentDestination)
                    {
                        // Track the destination so I can recognize when we leave this scope.
                        fsCur.RtfDestination = RtfDestination.DestShapeResult;

                        // Wrap any inline content that occurs before this shape anchor in a paragraph,
                        // since the shape content itself will be block level.
                        WrapPendingInlineInParagraph(token, formatState);

                        DocumentNodeType t = dna.GetTableScope();
                        if (t != DocumentNodeType.dnParagraph)
                        {
                            if (t == DocumentNodeType.dnTableBody)
                            {
                                // If row has been closed, close overall table as well.
                                int nAt = dna.FindPending(DocumentNodeType.dnTable);
                                if (nAt >= 0)
                                {
                                    dna.CloseAt(nAt);
                                    dna.CoalesceChildren(_converterState, nAt);
                                }
                            }
                            else
                            {
                                // If I'm inside a table, reopen last cell to insert shape contents.  Otherwise
                                // table gets torqued.
                                dna.OpenLastCell();
                            }
                        }

                        // The shape node generates no output but plays a large role in changing the
                        // behavior of the "FindPending" routines to keep from looking outside this scope.
                        DocumentNode dn = new DocumentNode(DocumentNodeType.dnShape);
                        formatState.SetParaDefaults();
                        formatState.SetCharDefaults();
                        dn.FormatState = new FormatState(formatState);
                        dna.Push(dn);
                    }
                    break;
                case RtfControlWord.Ctrl_SHPPICT:
                    // If this occurs in listtext context, mark listtext as non-empty.
                    int ndnListText = dna.FindPending(DocumentNodeType.dnListText);
                    if (ndnListText >= 0)
                    {
                        DocumentNode dnListText = dna.EntryAt(ndnListText);

                        dnListText.HasMarkerContent = true;
                    }

                    // Keep the rtf destination as the list picture to skip the list picture
                    if (fsOld.RtfDestination == RtfDestination.DestListPicture)
                    {
                        formatState.RtfDestination = RtfDestination.DestListPicture;
                    }
                    else
                    {
                        formatState.RtfDestination = RtfDestination.DestShapePicture;
                    }

                    break;
                case RtfControlWord.Ctrl_NONSHPPICT:
                    formatState.RtfDestination = RtfDestination.DestNoneShapePicture;
                    break;
            }
        }

        internal void HandleOldListTokens(RtfToken token, FormatState formatState)
        {
            FormatState fsCur = _converterState.PreviousTopFormatState(0);
            FormatState fsOld = _converterState.PreviousTopFormatState(1);
            if (fsCur == null || fsOld == null)
            {
                return;
            }

            // If we're in the PN destination, we push marker setting into the previous format state.
            if (formatState.RtfDestination == RtfDestination.DestPN)
            {
                formatState = fsOld;
            }

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_PNLVL:
                    formatState.PNLVL = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_PNLVLBLT:
                    formatState.Marker = MarkerStyle.MarkerBullet;
                    formatState.IsContinue = false;
                    break;
                case RtfControlWord.Ctrl_PNLVLBODY:
                    formatState.Marker = MarkerStyle.MarkerArabic;
                    formatState.IsContinue = false;
                    break;
                case RtfControlWord.Ctrl_PNLVLCONT:
                    formatState.IsContinue = true;
                    break;
                case RtfControlWord.Ctrl_PNCARD:
                    formatState.Marker = MarkerStyle.MarkerCardinal;
                    break;
                case RtfControlWord.Ctrl_PNDEC:
                    formatState.Marker = MarkerStyle.MarkerArabic;
                    break;
                case RtfControlWord.Ctrl_PNUCLTR:
                    formatState.Marker = MarkerStyle.MarkerUpperAlpha;
                    break;
                case RtfControlWord.Ctrl_PNUCRM:
                    formatState.Marker = MarkerStyle.MarkerUpperRoman;
                    break;
                case RtfControlWord.Ctrl_PNLCLTR:
                    formatState.Marker = MarkerStyle.MarkerLowerAlpha;
                    break;
                case RtfControlWord.Ctrl_PNLCRM:
                    formatState.Marker = MarkerStyle.MarkerLowerRoman;
                    break;
                case RtfControlWord.Ctrl_PNORD:
                    formatState.Marker = MarkerStyle.MarkerOrdinal;
                    break;
                case RtfControlWord.Ctrl_PNORDT:
                    formatState.Marker = MarkerStyle.MarkerOrdinal;
                    break;
                case RtfControlWord.Ctrl_PNBIDIA:
                    formatState.Marker = MarkerStyle.MarkerArabic;
                    break;
                case RtfControlWord.Ctrl_PNBIDIB:
                    formatState.Marker = MarkerStyle.MarkerArabic;
                    break;
                case RtfControlWord.Ctrl_PN:
                    formatState.RtfDestination = RtfDestination.DestPN;
                    fsOld.Marker = MarkerStyle.MarkerBullet;
                    break;
                case RtfControlWord.Ctrl_PNTXTA:
                    // Leave with unknown destination so text is tossed.
                    break;
                case RtfControlWord.Ctrl_PNTXTB:
                    // Leave with unknown destination so text is tossed.
                    break;
                case RtfControlWord.Ctrl_PNTEXT:
                    if (fsOld.IsContentDestination || formatState.IsHidden)
                    {
                        fsCur.RtfDestination = RtfDestination.DestListText;
                        DocumentNodeArray dna = _converterState.DocumentNodeArray;
                        DocumentNode dnl = new DocumentNode(DocumentNodeType.dnListText);

                        dnl.FormatState = new FormatState(formatState);
                        dna.Push(dnl);
                    }
                    break;
                case RtfControlWord.Ctrl_PNSTART:
                    formatState.StartIndex = token.Parameter;
                    break;

                default:
                    formatState.Marker = MarkerStyle.MarkerBullet;
                    break;
            }
        }

        internal void HandleTableProperties(RtfToken token, FormatState formatState)
        {
            if (!formatState.IsContentDestination)
            {
                return;
            }

            CellFormat cf = null;

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_TRGAPH:
                    break;
                case RtfControlWord.Ctrl_TRLEFT:
                    formatState.RowFormat.Trleft = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRQC:
                    // Specifies overall alignment of the table row - ignore for now.
                    break;
                case RtfControlWord.Ctrl_TRQL:
                    // Specifies overall alignment of the table row - ignore for now.
                    break;
                case RtfControlWord.Ctrl_TRQR:
                    // Specifies overall alignment of the table row - ignore for now.
                    break;
                case RtfControlWord.Ctrl_TRPADDL:
                    formatState.RowFormat.RowCellFormat.PaddingLeft = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRPADDR:
                    formatState.RowFormat.RowCellFormat.PaddingRight = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRPADDB:
                    formatState.RowFormat.RowCellFormat.PaddingBottom = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRPADDT:
                    formatState.RowFormat.RowCellFormat.PaddingTop = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRPADDFL:
                    // zero value indicates ignore trpaddl, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_TRPADDFT:
                    // zero value indicates ignore trpaddt, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_TRPADDFB:
                    // zero value indicates ignore trpaddb, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_TRPADDFR:
                    // zero value indicates ignore trpaddr, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_TRSPDFB:
                    if (token.Parameter == 0)
                        formatState.RowFormat.RowCellFormat.SpacingBottom = 0;
                    break;
                case RtfControlWord.Ctrl_TRSPDFL:
                    if (token.Parameter == 0)
                        formatState.RowFormat.RowCellFormat.SpacingLeft = 0;
                    break;
                case RtfControlWord.Ctrl_TRSPDFR:
                    if (token.Parameter == 0)
                        formatState.RowFormat.RowCellFormat.SpacingRight = 0;
                    break;
                case RtfControlWord.Ctrl_TRSPDFT:
                    if (token.Parameter == 0)
                        formatState.RowFormat.RowCellFormat.SpacingTop = 0;
                    break;
                case RtfControlWord.Ctrl_TRSPDB:
                    formatState.RowFormat.RowCellFormat.SpacingBottom = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRSPDL:
                    formatState.RowFormat.RowCellFormat.SpacingLeft = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRSPDR:
                    formatState.RowFormat.RowCellFormat.SpacingRight = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRSPDT:
                    formatState.RowFormat.RowCellFormat.SpacingTop = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRWWIDTH:
                    // Row (table) width
                    formatState.RowFormat.WidthRow.Value = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRFTSWIDTH:
                    // Units for WWIDTH (0 - ignore, 1 - auto, 2 - 50ths of a percent, 3 - twips)
                    if (Validators.IsValidWidthType(token.Parameter))
                        formatState.RowFormat.WidthRow.Type = (WidthType)token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRWWIDTHB:
                    // Space before row width
                    break;
                case RtfControlWord.Ctrl_TRFTSWIDTHB:
                    break;
                case RtfControlWord.Ctrl_TRWWIDTHA:
                    // Space after row width
                    formatState.RowFormat.WidthA.Value = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRFTSWIDTHA:
                    if (Validators.IsValidWidthType(token.Parameter))
                        formatState.RowFormat.WidthA.Type = (WidthType)token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRAUTOFIT:
                    if (token.ToggleValue > 0)
                        formatState.RowFormat.WidthRow.SetDefaults();
                    break;
                case RtfControlWord.Ctrl_CLWWIDTH:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.Width.Value = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLFTSWIDTH:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    if (Validators.IsValidWidthType(token.Parameter))
                        cf.Width.Type = (WidthType)token.Parameter;
                    break;
                case RtfControlWord.Ctrl_TRBRDRT:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderTop;
                    break;
                case RtfControlWord.Ctrl_TRBRDRB:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderBottom;
                    break;
                case RtfControlWord.Ctrl_TRBRDRR:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderRight;
                    break;
                case RtfControlWord.Ctrl_TRBRDRL:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderLeft;
                    break;
                case RtfControlWord.Ctrl_TRBRDRV:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderLeft;
                    break;
                case RtfControlWord.Ctrl_TRBRDRH:
                    ConverterState.CurrentBorder = formatState.RowFormat.RowCellFormat.BorderTop;
                    break;
                case RtfControlWord.Ctrl_CLVERTALT:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.VAlign = VAlign.AlignTop;
                    break;
                case RtfControlWord.Ctrl_CLVERTALB:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.VAlign = VAlign.AlignBottom;
                    break;
                case RtfControlWord.Ctrl_CLVERTALC:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.VAlign = VAlign.AlignCenter;
                    break;
                case RtfControlWord.Ctrl_CLSHDNG:
                    // Cell shading in 100's of a percent
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.Shading = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLSHDRAWNIL:
                    // No cell shading - clear color for now
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.Shading = -1;
                    cf.CB = -1;
                    cf.CF = -1;
                    break;
                case RtfControlWord.Ctrl_CLBRDRB:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    ConverterState.CurrentBorder = cf.BorderBottom;
                    break;
                case RtfControlWord.Ctrl_CLBRDRR:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    ConverterState.CurrentBorder = cf.BorderRight;
                    break;
                case RtfControlWord.Ctrl_CLBRDRT:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    ConverterState.CurrentBorder = cf.BorderTop;
                    break;
                case RtfControlWord.Ctrl_CLBRDRL:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    ConverterState.CurrentBorder = cf.BorderLeft;
                    break;
                case RtfControlWord.Ctrl_CLCBPAT:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.CB = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLCFPAT:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.CF = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLPADL:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.PaddingLeft = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLPADR:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.PaddingRight = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLPADB:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.PaddingBottom = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLPADT:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.PaddingTop = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_CLPADFL:
                    // zero value indicates ignore clpadl, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_CLPADFT:
                    // zero value indicates ignore clpadt, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_CLPADFB:
                    // zero value indicates ignore clpadb, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_CLPADFR:
                    // zero value indicates ignore clpadr, three means treat it as twips
                    break;
                case RtfControlWord.Ctrl_CELLX:
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.CellX = token.Parameter;
                    cf.IsPending = false;
                    break;
                case RtfControlWord.Ctrl_RTLROW:
                    formatState.RowFormat.Dir = DirState.DirRTL;
                    break;
                case RtfControlWord.Ctrl_LTRROW:
                    formatState.RowFormat.Dir = DirState.DirLTR;
                    break;

                // Cell merging
                case RtfControlWord.Ctrl_CLMGF:
                    // First cell in range of table cells to be merged.
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.IsHMergeFirst = true;
                    break;
                case RtfControlWord.Ctrl_CLMRG:
                    // Contents of this cell are merged with those of the preceding cell.
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.IsHMerge = true;
                    break;
                case RtfControlWord.Ctrl_CLVMGF:
                    // First cell in range of cells to be vertically merged.
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.IsVMergeFirst = true;
                    break;
                case RtfControlWord.Ctrl_CLVMRG:
                    // Contents of this cell are vertically merged with those of the preceding cell.
                    cf = formatState.RowFormat.CurrentCellFormat();
                    cf.IsVMerge = true;
                    break;

                // General borders, not just tables
                case RtfControlWord.Ctrl_BRDRL:
                    // Left paragraph border
                    ConverterState.CurrentBorder = formatState.ParaBorder.BorderLeft;
                    break;
                case RtfControlWord.Ctrl_BRDRR:
                    // Right paragraph border
                    ConverterState.CurrentBorder = formatState.ParaBorder.BorderRight;
                    break;
                case RtfControlWord.Ctrl_BRDRT:
                    // Top paragraph border
                    ConverterState.CurrentBorder = formatState.ParaBorder.BorderTop;
                    break;
                case RtfControlWord.Ctrl_BRDRB:
                    // Bottom paragraph border
                    ConverterState.CurrentBorder = formatState.ParaBorder.BorderBottom;
                    break;
                case RtfControlWord.Ctrl_BOX:
                    // All four borders
                    ConverterState.CurrentBorder = formatState.ParaBorder.BorderAll;
                    break;
                case RtfControlWord.Ctrl_BRDRNIL:
                    // No borders
                    ConverterState.CurrentBorder = null;
                    break;
                case RtfControlWord.Ctrl_BRSP:
                    // Space in twips between borders and paragraph
                    formatState.ParaBorder.Spacing = token.Parameter;
                    break;
                case RtfControlWord.Ctrl_BRDRTBL:
                    // No cell borders
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderNone;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRART:
                    // Art border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRBAR:
                    // Border on outside edge of page (treat as BRDRL for XAML)
                    break;
                case RtfControlWord.Ctrl_BRDRBTW:
                    break;
                case RtfControlWord.Ctrl_BRDRCF:
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.CF = token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDASH:
                    // Dashed border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDASHD:
                    // Dash dot border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDASHDD:
                    // Dot dot dash border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDASHDOTSTR:
                    // Dash-dot border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDASHSM:
                    // Small dash border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDB:
                    // Double border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderDouble;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRDOT:
                    // Dotted border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDREMBOSS:
                    // Emboss-style border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRENGRAVE:
                    // Engrave-style border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRFRAME:
                    // Frame-style border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRHAIR:
                    // Hairline border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRINSET:
                    // Inset border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDROUTSET:
                    // Outset border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRS:
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRSH:
                    // Shadow border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTH:
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderDouble;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTHTNLG:
                    // Thick-thin (large) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTHTNMG:
                    // Thick-thin (medium) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTHTNSG:
                    // Thick-thin-thin (thin) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHLG:
                    // Thin-thick (large) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHMG:
                    // Thick-thin-thin (medium) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHSG:
                    // Thick-thin-thin (small) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHTNLG:
                    // Thick-thin-thin (large) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHTNMG:
                    // Thin-thick-thin (medium) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTNTHTNSG:
                    // Thick-thin-thin (small) border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRTRIPLE:
                    // Triple border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRW:
                    // Border thickness
                    if (ConverterState.CurrentBorder != null)
                    {
                        // Note that propset does validation
                        ConverterState.CurrentBorder.Width = token.Parameter;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRNONE:
                    // No borders
                    if (ConverterState.CurrentBorder != null)
                    {
                        // Note that propset does validation
                        ConverterState.CurrentBorder.SetDefaults();
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRWAVY:
                    // Wavy border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderSingle;
                    }
                    break;
                case RtfControlWord.Ctrl_BRDRWAVYDB:
                    // Double border
                    if (ConverterState.CurrentBorder != null)
                    {
                        ConverterState.CurrentBorder.Type = BorderType.BorderDouble;
                    }
                    break;
            }
        }

        internal void HandleFieldTokens(RtfToken token, FormatState formatState)
        {
            // Don't start processing fields in non-normal destinatations
            FormatState fsCur = _converterState.PreviousTopFormatState(0);
            FormatState fsOld = _converterState.PreviousTopFormatState(1);
            if (fsCur == null || fsOld == null)
            {
                return;
            }

            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_FIELD:
                    // Process fields in normal content or nested fields
                    if (!fsOld.IsContentDestination || formatState.IsHidden)
                    {
                        return;
                    }
                    formatState.RtfDestination = RtfDestination.DestField;
                    break;
                case RtfControlWord.Ctrl_FLDRSLT:
                    if (fsOld.RtfDestination != RtfDestination.DestField)
                    {
                        return;
                    }
                    formatState.RtfDestination = RtfDestination.DestFieldResult;
                    break;
                case RtfControlWord.Ctrl_FLDPRIV:
                    if (fsOld.RtfDestination != RtfDestination.DestField)
                    {
                        return;
                    }
                    formatState.RtfDestination = RtfDestination.DestFieldPrivate;
                    break;
                case RtfControlWord.Ctrl_FLDINST:
                    if (fsOld.RtfDestination != RtfDestination.DestField)
                    {
                        return;
                    }
                    formatState.RtfDestination = RtfDestination.DestFieldInstruction;
                    break;
                default:
                    return;
            }

            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dnf = new DocumentNode(DocumentNodeType.dnFieldBegin);

            dnf.FormatState = new FormatState(formatState);
            dnf.IsPending = false;  // Field start mark should not impact other tags open/close behavior
            dnf.IsTerminated = true;
            dna.Push(dnf);
        }

        internal void HandleTableNesting(FormatState formatState)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            // If we're in a throw-away destination, just return.
            if (!formatState.IsContentDestination || formatState.IsHidden)
            {
                return;
            }

            // Make sure proper number of tables are open to reflect this paragraphs nest level
            int nTables = dna.CountOpenNodes(DocumentNodeType.dnTable);
            int nLevel = (int)formatState.TableLevel;

            // If we're not in a table, end early
            if (nTables == nLevel && nTables == 0)
            {
                return;
            }

            if (nTables > nLevel)
            {
                DocumentNode dnPara = dna.Pop();

                bool bInField = dna.FindUnmatched(DocumentNodeType.dnFieldBegin) >= 0;
                while (nTables > nLevel)
                {
                    int nOpen = dna.FindPending(DocumentNodeType.dnTable);
                    if (nOpen >= 0)
                    {
                        dna.CloseAt(nOpen);
                        if (!bInField)
                        {
                            dna.CoalesceChildren(_converterState, nOpen);
                        }
                    }
                    nTables--;
                }
                dna.Push(dnPara);
            }
            else
            {
                // Before opening the table, let's close any open lists.  Word (RTF) allows somewhat
                // arbitrary interleaving (because there's no explicit nesting), but when converting to
                // XAML we have to choose an explicit nesting.  We never create a table *inside* a list, so
                // let's just terminate any open lists right now.
                if (nTables < nLevel)
                {
                    int nListAt = dna.FindPending(DocumentNodeType.dnList);
                    if (nListAt >= 0)
                    {
                        // I want the currently pending paragraph to be part of the table, not part of the list
                        // I'm going to close.  So I temporarily pop it off while closing off the list and then
                        // push it back on before inserting the table(s).
                        DocumentNode dnPara = dna.Pop();

                        while (nListAt >= 0)
                        {
                            dna.CloseAt(nListAt);
                            nListAt = dna.FindPending(DocumentNodeType.dnList);
                        }

                        dna.Push(dnPara);
                    }
                }

                // Ensure sufficient tables are open - this may be our first indication
                // Insert the table nodes right before the current paragraph.
                Debug.Assert(dna.Count > 0 && dna.EntryAt(dna.Count - 1).Type == DocumentNodeType.dnParagraph);

                int nInsertAt = dna.Count - 1;

                // Ensure row is open
                int nTable = dna.FindPending(DocumentNodeType.dnTable);
                if (nTable >= 0)
                {
                    int nRow = dna.FindPending(DocumentNodeType.dnRow, nTable);
                    if (nRow == -1)
                    {
                        DocumentNode dnRow = new DocumentNode(DocumentNodeType.dnRow);
                        dna.InsertNode(nInsertAt++, dnRow);
                        nRow = nInsertAt - 1;
                    }

                    int nCell = dna.FindPending(DocumentNodeType.dnCell, nRow);
                    if (nCell == -1)
                    {
                        DocumentNode dnCell = new DocumentNode(DocumentNodeType.dnCell);
                        dna.InsertNode(nInsertAt, dnCell);
                    }
                }

                nInsertAt = dna.Count - 1;
                while (nTables < nLevel)
                {
                    DocumentNode dnTable = new DocumentNode(DocumentNodeType.dnTable);
                    DocumentNode dnTableBody = new DocumentNode(DocumentNodeType.dnTableBody);
                    DocumentNode dnRow = new DocumentNode(DocumentNodeType.dnRow);
                    DocumentNode dnCell = new DocumentNode(DocumentNodeType.dnCell);

                    dna.InsertNode(nInsertAt, dnCell);
                    dna.InsertNode(nInsertAt, dnRow);
                    dna.InsertNode(nInsertAt, dnTableBody);
                    dna.InsertNode(nInsertAt, dnTable);

                    nTables++;
                }
            }

            dna.AssertTreeSemanticInvariants();
        }

        internal MarkerList GetMarkerStylesOfParagraph(MarkerList mlHave, FormatState fs, bool bMarkerPresent)
        {
            MarkerList mlWant = new MarkerList();
            long nVirtualListLevel = fs.ListLevel;
            long nStartIndexOverride = -1;

            // No list?
            if (nVirtualListLevel < 1)
            {
                return mlWant;
            }

            // Use currently open list styles for all levels below requested one
            for (int i = 0; i < mlHave.Count; i++)
                if (mlHave.EntryAt(i).VirtualListLevel < nVirtualListLevel || fs.IsContinue)
                {
                    MarkerListEntry mle = mlHave.EntryAt(i);
                    mlWant.AddEntry(mle.Marker, mle.ILS, -1, mle.StartIndexDefault, mle.VirtualListLevel);
                }
                else
                {
                    break;
                }

            // If I'm a continuation paragraph, I'm done.
            if (fs.IsContinue)
            {
                return mlWant;
            }

            // Now determine the list style for the list level I'm going to add.
            ListOverrideTable lot = _converterState.ListOverrideTable;
            ListOverride lo = lot.FindEntry((int)fs.ILS);
            if (lo != null)
            {
                ListLevelTable levels = lo.Levels;
                if (levels == null || levels.Count == 0)
                {
                    ListTableEntry lte = _converterState.ListTable.FindEntry(lo.ID);
                    if (lte != null)
                    {
                        levels = lte.Levels;
                    }

                    // Did the list override specify a start index?
                    if (lo.StartIndex > 0)
                    {
                        nStartIndexOverride = lo.StartIndex;
                    }
                }

                if (levels != null)
                {
                    ListLevel listLevel = levels.EntryAt((int)nVirtualListLevel - 1);
                    if (listLevel != null)
                    {
                        // If there was a marker present, we ignore the "Hidden" style in the list table.
                        MarkerStyle ms = listLevel.Marker;
                        if (ms == MarkerStyle.MarkerHidden && bMarkerPresent)
                        {
                            ms = MarkerStyle.MarkerBullet;
                        }

                        mlWant.AddEntry(ms, fs.ILS, nStartIndexOverride, listLevel.StartIndex, nVirtualListLevel);
                        return mlWant;
                    }
                }
            }

            // If there wasn't a list definition, use the marker type in the formatstate.
            mlWant.AddEntry(fs.Marker, fs.ILS, nStartIndexOverride, fs.StartIndex, nVirtualListLevel);
            return mlWant;
        }

        internal void HandleListNesting(FormatState formatState)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dnPara = dna.EntryAt(dna.Count - 1);
            bool bMarkerPresent = _converterState.IsMarkerPresent;

            // Test if we encountered list text
            if (_converterState.IsMarkerPresent)
            {
                _converterState.IsMarkerPresent = false;
            }

            // If we're in a throw-away destination, just return.
            if (!formatState.IsContentDestination || formatState.IsHidden)
            {
                return;
            }

            // Treat no marker text present as a continuation paragraph
            if (!bMarkerPresent && formatState.ListLevel > 0)
            {
                // Allocate a new one here so that this change doesn't propagate beyond here.
                formatState = new FormatState(formatState);

                // If the only thing making this look like a list is the ILVL property, just clear it.
                if (formatState.ILVL > 0 && formatState.ILS < 0)
                {
                    formatState.ILVL = 0;
                }
                else // otherwise treat this as a continuation paragraph.
                {
                    formatState.IsContinue = true;
                }
            }

            // Make sure proper number of lists are open to reflect this paragraphs nest level
            MarkerList mlHave = dna.GetOpenMarkerStyles();
            MarkerList mlWant = GetMarkerStylesOfParagraph(mlHave, formatState, bMarkerPresent);

            int nLists = mlHave.Count;
            int nLevel = mlWant.Count;

            // If we're not in a list end early
            if (nLists == nLevel && nLists == 0)
            {
                return;
            }

            // If we're not in a list and marked as a continuation, ignore this - anomaly.
            if (nLists == 0 && nLevel == 1 && formatState.IsContinue)
            {
                return;
            }

            // Propagate noticing that the specified text was empty.
            if (_converterState.IsMarkerWhiteSpace)
            {
                _converterState.IsMarkerWhiteSpace = false;
                if (nLevel > 0)
                {
                    MarkerListEntry entry = mlWant.EntryAt(nLevel - 1);
                    entry.Marker = MarkerStyle.MarkerHidden;
                }
            }

            // The ones we have are only "good" if the styles match what we want.
            int nMatch = GetMatchedMarkList(formatState, mlHave, mlWant);

            // If none match, we might do better by extending some previously open list.
            if (nMatch == 0)
            {
                MarkerList mlCouldHave = dna.GetLastMarkerStyles(mlHave, mlWant);
                MarkerList mlCouldWant = GetMarkerStylesOfParagraph(mlCouldHave, formatState, bMarkerPresent);
                nMatch = GetMatchedMarkList(formatState, mlCouldHave, mlCouldWant);

                // If I would re-open a previous list but close some set of them and then reopen another,
                // this is unlikely to be what the user intended.  Instead, don't extend the previous list.
                // (RtfConverter:Spacing between left most edge and marker is incorrect on last list item (RTF-XAML))
                if (nMatch < mlCouldHave.Count && mlCouldWant.Count > nMatch)
                {
                    nMatch = 0;
                }

                if (nMatch > 0)
                {
                    mlHave = mlCouldHave;
                    mlWant = mlCouldWant;
                    dna.OpenLastList();
                }
            }

            // Ensure list and listitem
            EnsureListAndListItem(formatState, dna, mlHave, mlWant, nMatch);

            // To get better results in XAML output, clear the FI for the first para in a list item.
            if (dna.Count > 1 && dna.EntryAt(dna.Count - 2).Type == DocumentNodeType.dnListItem)
            {
                Debug.Assert(!dnPara.IsTerminated);
                dnPara.FormatState.FI = 0;
            }

            dna.AssertTreeSemanticInvariants();
        }

        internal void HandleCodePageTokens(RtfToken token, FormatState formatState)
        {
            switch (token.RtfControlWordInfo.Control)
            {
                case RtfControlWord.Ctrl_ANSI:
                    // ANSI apparently means specifically 1252, not ACP.  That makes a lot more sense...
                    //_converterState.CodePage = CultureInfo.CurrentCulture.TextInfo.ANSICodePage;
                    _converterState.CodePage = 1252;
                    _lexer.CodePage = _converterState.CodePage;
                    break;
                case RtfControlWord.Ctrl_MAC:
                    _converterState.CodePage = 10000;
                    _lexer.CodePage = _converterState.CodePage;
                    break;
                case RtfControlWord.Ctrl_PC:
                    _converterState.CodePage = 437;
                    _lexer.CodePage = _converterState.CodePage;
                    break;
                case RtfControlWord.Ctrl_PCA:
                    _converterState.CodePage = 850;
                    _lexer.CodePage = _converterState.CodePage;
                    break;
                case RtfControlWord.Ctrl_UPR:
                    // We discard this ansi representation - \ud will then switch back to current.
                    formatState.RtfDestination = RtfDestination.DestUPR;
                    break;
                case RtfControlWord.Ctrl_U:
                    {
                        char[] unicodeChar = new char[1];

                        unicodeChar[0] = (char)token.Parameter;
                        ProcessText(new string(unicodeChar));
                    }
                    break;
                case RtfControlWord.Ctrl_UD:
                    {
                        // We are parsing: {\upr ansi stuff{\*\ud unicode stuff }}
                        // When we encountered the UPR we set state to a throwaway destination (DestUPR).
                        // The nested group pushed a new format state but that now has DestUnknown because of this.
                        // Now that we encountered the \ud destination, lets push back the original destination.
                        FormatState previous = _converterState.PreviousTopFormatState(1);
                        FormatState previousPrevious = _converterState.PreviousTopFormatState(2);
                        if (previous != null && previousPrevious != null)
                        {
                            if (formatState.RtfDestination == RtfDestination.DestUPR && previous.RtfDestination == RtfDestination.DestUnknown)
                            {
                                formatState.RtfDestination = previousPrevious.RtfDestination;
                            }
                        }
                    }
                    break;
                case RtfControlWord.Ctrl_UC:
                    formatState.UnicodeSkip = (int)token.Parameter;
                    break;
            }
        }

        internal void ProcessFieldText(RtfToken token)
        {
            switch (_converterState.TopFormatState.RtfDestination)
            {
                case RtfDestination.DestField:
                    break;
                case RtfDestination.DestFieldInstruction:
                    // Gather up for later processing
                    HandleNormalText(token.Text, _converterState.TopFormatState);
                    break;
                case RtfDestination.DestFieldPrivate:
                    // Discard
                    break;
                case RtfDestination.DestFieldResult:
                    HandleNormalText(token.Text, _converterState.TopFormatState);
                    break;
            }
        }

        internal void ProcessFontTableText(RtfToken token)
        {
            string tokenName = token.Text;

            // Strip line endings
            tokenName = tokenName.Replace("\r\n", "");
            // Strip trailing semi-colon
            tokenName = tokenName.Replace(";", "");

            FontTableEntry entry = _converterState.FontTable.CurrentEntry;

            if (entry != null && tokenName.Length > 0 && !entry.IsNameSealed)
            {
                // If name not yet specified, just set it
                if (entry.Name == null)
                {
                    entry.Name = tokenName;
                }
                else // Otherwise, append it
                {
                    entry.Name += tokenName;
                }
            }
        }

        internal void HandleFontTableTokens(RtfToken token)
        {
            FontTableEntry entry = _converterState.FontTable.CurrentEntry;
            FormatState formatState = _converterState.TopFormatState;

            if (entry != null)
            {
                switch (token.RtfControlWordInfo.Control)
                {
                    case RtfControlWord.Ctrl_FCHARSET:
                        // Set the codepage to the font table entry
                        entry.CodePageFromCharSet = (int)token.Parameter;

                        // Also set lexer code page
                        if (entry.CodePage == -1)
                        {
                            formatState.CodePage = _converterState.CodePage;
                        }
                        else
                        {
                            formatState.CodePage = entry.CodePage;
                        }
                        _lexer.CodePage = formatState.CodePage;
                        break;
                }
            }
        }

        internal void ProcessColorTableText(RtfToken token)
        {
            // This is just a separator for color table entries
            _converterState.ColorTable.FinishColor();
        }

        internal void ProcessText(string text)
        {
            FormatState fs = _converterState.TopFormatState;

            if (fs.IsContentDestination && !fs.IsHidden && text != string.Empty)
            {
                HandleNormalTextRaw(text, fs);
            }
        }

        internal void HandleNormalText(string text, FormatState formatState)
        {
            // Normal CRLF's are eaten by the RTF lexer.  Any ones that have slipped through here
            // were either escaped or hex-encoded and should be treated as a linebreak.
            int nStart = 0;
            while (nStart < text.Length)
            {
                int nEnd = nStart;
                while (nEnd < text.Length)
                {
                    if (text[nEnd] == 0x0d || text[nEnd] == 0x0a)
                    {
                        break;
                    }
                    nEnd++;
                }

                // Handle text before newline
                if (nStart == 0 && nEnd == text.Length)
                {
                    HandleNormalTextRaw(text, formatState);
                }
                else if (nEnd > nStart)
                {
                    string subtext = text.Substring(nStart, nEnd - nStart);
                    HandleNormalTextRaw(subtext, formatState);
                }

                // Handle newlines
                while (nEnd < text.Length && (text[nEnd] == 0x0d || text[nEnd] == 0x0a))
                {
                    ProcessNormalHardLine(formatState);

                    if (nEnd + 1 < text.Length && text[nEnd] == 0x0d && text[nEnd] == 0x0a)
                    {
                        nEnd += 2;
                    }
                    else
                    {
                        nEnd += 1;
                    }
                }

                nStart = nEnd;
            }
        }

        internal void HandleNormalTextRaw(string text, FormatState formatState)
        {
            DocumentNodeArray dna = _converterState.DocumentNodeArray;
            DocumentNode dnTop = dna.Top;

            // See if I can just append the text content if the format is the same.
            if (dnTop != null && (dnTop.Type == DocumentNodeType.dnText))
            {
                // If the format is not equal, close this text element and we'll open a new one.
                if (!dnTop.FormatState.IsEqual(formatState))
                {
                    dna.CloseAt(dna.Count - 1);
                    dnTop = null;
                }
            }

            // OK, create a text node if necessary
            if (dnTop == null || dnTop.Type != DocumentNodeType.dnText)
            {
                dnTop = new DocumentNode(DocumentNodeType.dnText);
                dnTop.FormatState = new FormatState(formatState);
                dna.Push(dnTop);
            }

            Debug.Assert(!dnTop.IsTerminated);
            dnTop.AppendXamlEncoded(text);
            dnTop.IsPending = false;
        }

        internal void ProcessNormalHardLine(FormatState formatState)
        {
            // Close out pending text nodes
            DocumentNodeArray dna = _converterState.DocumentNodeArray;

            if (dna.TestTop(DocumentNodeType.dnText))
            {
                dna.CloseAt(dna.Count - 1);
            }

            DocumentNode documentNode = new DocumentNode(DocumentNodeType.dnLineBreak);
            documentNode.FormatState = new FormatState(formatState);
            dna.Push(documentNode);
            dna.CloseAt(dna.Count - 1);
            dna.CoalesceChildren(_converterState, dna.Count - 1);
        }

        internal void ProcessHardLine(RtfToken token, FormatState formatState)
        {
            switch (_converterState.TopFormatState.RtfDestination)
            {
                case RtfDestination.DestNormal:
                case RtfDestination.DestFieldResult:
                case RtfDestination.DestShapeResult:
                case RtfDestination.DestListText:
                    ProcessNormalHardLine(formatState);
                    break;
                case RtfDestination.DestFontTable:
                case RtfDestination.DestFontName:
                    break;
                case RtfDestination.DestColorTable:
                    break;
                case RtfDestination.DestField:
                    break;
                case RtfDestination.DestFieldInstruction:
                case RtfDestination.DestFieldPrivate:
                    ProcessNormalHardLine(formatState);
                    break;
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void SetTokenTextWithControlCharacter(RtfToken token)
        {
            switch (token.Text[0])
            {
                case '~':
                    // NBSP
                    token.Text = new string('\xA0', 1);    // Unicode NBSP
                    break;
                case '-':
                    // Optional hypen (not really in input)
                    token.Text = string.Empty;
                    break;
                case ':':
                    // Sub-entry in index - leave as literal
                    break;
                case '_':
                    // Non-breaking hypen - convert to real hypen
                    token.Text = new string('\x2011', 1);
                    break;
                case '|':
                    // Formula character - leave as literal (or toss?)
                    break;

                // Escaped lexically special RTF characters - leave as literal text
                case '\\':
                case '{':
                case '}':
                    break;
            }
        }

        private int GetMatchedMarkList(FormatState formatState, MarkerList mlHave, MarkerList mlWant)
        {
            // The ones we have are only "good" if the styles match what we want.
            int nMatch = 0;
            for (; nMatch < mlHave.Count && nMatch < mlWant.Count; nMatch++)
            {
                if (!formatState.IsContinue)
                {
                    MarkerListEntry eHave = mlHave.EntryAt(nMatch);
                    MarkerListEntry eWant = mlWant.EntryAt(nMatch);

                    if (eHave.Marker != eWant.Marker
                        || eHave.ILS != eWant.ILS
                        || eHave.StartIndexDefault != eWant.StartIndexDefault
                        || eWant.StartIndexOverride >= 0)
                    {
                        break;
                    }
                }
            }

            return nMatch;
        }

        private void EnsureListAndListItem(FormatState formatState, DocumentNodeArray dna, MarkerList mlHave, MarkerList mlWant, int nMatch)
        {
            int nInsertAt;

            bool added = false;

            int nLists = mlHave.Count;
            int nLevel = mlWant.Count;

            // Close any open lists that don't match the ones we want.
            bool bInField = dna.FindUnmatched(DocumentNodeType.dnFieldBegin) >= 0;
            if (nLists > nMatch)
            {
                DocumentNode documentNodePara = dna.Pop();
                while (nLists > nMatch)
                {
                    int nOpen = dna.FindPending(DocumentNodeType.dnList);
                    if (nOpen >= 0)
                    {
                        dna.CloseAt(nOpen);

                        // Only coalesce if this is a top-level list.  Otherwise I want to get
                        // the full structure to use for margin fixups so I delay coalescing.
                        // No, don't coalesce since a later list may need to get merged with this one.
                        // if (!bInField && dna.FindPending(DocumentNodeType.dnList) < 0)
                        //    dna.CoalesceChildren(_converterState, nOpen);
                    }
                    nLists--;
                    mlHave.RemoveRange(mlHave.Count - 1, 1);
                }
                dna.Push(documentNodePara);
            }

            if (nLists < nLevel)
            {
                // Multiple immediately nested lists are handled poorly in Avalon and are usually an indication
                // of bad input from Word (or some other word processor).  Clip the number of lists we'll create here.
                if (nLevel != nLists + 1)
                {
                    // I'm going to truncate, but make the list I create here of the specific type at this level.
                    if (nLevel <= mlWant.Count)
                    {
                        mlWant[nLists] = mlWant[mlWant.Count - 1];
                    }
                    nLevel = nLists + 1;
                }

                // Ensure sufficient lists are open - this may be our first indication
                // Insert the list nodes right before the current paragraph
                nInsertAt = dna.Count - 1;

                while (nLists < nLevel)
                {
                    added = true;

                    DocumentNode dnList = new DocumentNode(DocumentNodeType.dnList);
                    DocumentNode dnLI = new DocumentNode(DocumentNodeType.dnListItem);

                    dna.InsertNode(nInsertAt, dnLI);
                    dna.InsertNode(nInsertAt, dnList);

                    // Set the list properties
                    MarkerListEntry mle = mlWant.EntryAt(nLists);
                    dnList.FormatState.Marker = mle.Marker;
                    dnList.FormatState.StartIndex = mle.StartIndexToUse;
                    dnList.FormatState.StartIndexDefault = mle.StartIndexDefault;
                    dnList.VirtualListLevel = mle.VirtualListLevel;
                    dnList.FormatState.ILS = mle.ILS;
                    nLists++;
                }
            }

            // Ensure listitem is open
            nInsertAt = dna.Count - 1;
            int nList = dna.FindPending(DocumentNodeType.dnList);
            if (nList >= 0)
            {
                int nLI = dna.FindPending(DocumentNodeType.dnListItem, nList);

                if (nLI >= 0 && !added && !formatState.IsContinue)
                {
                    DocumentNode documentNodePara = dna.Pop();
                    dna.CloseAt(nLI);
                    // Don't coalesce - I may need to do margin fixup.
                    // dna.CoalesceChildren(_converterState, nLI);
                    dna.Push(documentNodePara);

                    nLI = -1;
                    nInsertAt = dna.Count - 1;
                }
                if (nLI == -1)
                {
                    DocumentNode dnLI = new DocumentNode(DocumentNodeType.dnListItem);

                    dna.InsertNode(nInsertAt, dnLI);
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Enum
        //
        //------------------------------------------------------

        #region Private Enum

        private enum EncodeType
        {
            Ansi,
            Unicode,
            ShiftJis
        }

        #endregion Private Enum

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private byte[] _rtfBytes;
        private StringBuilder _outerXamlBuilder;

        private RtfToXamlLexer _lexer;
        private ConverterState _converterState;

        private bool _bForceParagraph;

        // WpfPayload package that containing the image for the specified Xaml
        private WpfPayload _wpfPayload;

        // Rtf image count that is the unique image id which is on WpfPayload
        private int _imageCount;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Const
        //
        //------------------------------------------------------

        #region Private Const

        private const int MAX_GROUP_DEPTH = 32;

        #endregion Private Const
    }
}
