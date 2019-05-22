// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration
{
    /// <remarks>
    /// From http://msdn.microsoft.com/en-us/library/cc244659(PROT.13).aspx
    /// </remarks>
    internal static class DevModePaperSizes
    {
        public static bool IsCustom(short paperSizeCode)
        {
            return paperSizeCode >= DMPAPER_USER;
        }

        ///<summary>Letter, 8 1/2 x 11 inches.</summary>
        public const short DMPAPER_LETTER = 0x0001;

        ///<summary>Legal, 8 1/2 x 14 inches.</summary>
        public const short DMPAPER_LEGAL = 0x0005;

        public const short DMPAPER_LEGAL_EXTRA = 0x0033;

        ///<summary>9 x 11-inch sheet.</summary>
        public const short DMPAPER_9X11 = 0x002C;

        ///<summary>10 x 11-inch sheet.</summary>
        public const short DMPAPER_10X11 = 0x002D;

        ///<summary>10 x 14-inch sheet.</summary>
        public const short DMPAPER_10X14 = 0x0010;

        ///<summary>11 x 17-inch sheet.</summary>
        public const short DMPAPER_11X17 = 0x0011;

        ///<summary>12 x 11-inch sheet.</summary>
        public const short DMPAPER_12X11 = 0x005A;

        public const short DMPAPER_A_PLUS = 0x0039;

        public const short DMPAPER_A2 = 0x0042;

        ///<summary>A3 sheet, 297 x 420 millimeters.</summary>
        public const short DMPAPER_A3 = 0x0008;

        public const short DMPAPER_A3_EXTRA = 0x003F;

        ///<summary>A3 rotated sheet, 420 x 297 millimeters.</summary>
        public const short DMPAPER_A3_ROTATED = 0x004C;

        ///<summary>A4 sheet, 210 x 297 millimeters.</summary>
        public const short DMPAPER_A4 = 0x0009;

        public const short DMPAPER_A4_EXTRA = 0x0035;

        ///<summary>A4 rotated sheet, 297 x 210 millimeters.</summary>
        public const short DMPAPER_A4_ROTATED = 0x004D;

        ///<summary>A4 small sheet, 210 x 297 millimeters.</summary>
        public const short DMPAPER_A4SMALL = 0x000A;

        public const short DMPAPER_A4_PLUS = 0x003C;

        ///<summary>A5 sheet, 148 x 210 millimeters.</summary>
        public const short DMPAPER_A5 = 0x000B;

        public const short DMPAPER_A5_EXTRA = 0x0040;

        ///<summary>A5 rotated sheet, 210 x 148 millimeters.</summary>
        public const short DMPAPER_A5_ROTATED = 0x004E;

        ///<summary>A6 sheet, 105 x 148 millimeters.</summary>
        public const short DMPAPER_A6 = 0x0046;

        ///<summary>A6 rotated sheet, 148 x 105 millimeters.</summary>
        public const short DMPAPER_A6_ROTATED = 0x0053;

        ///<summary>B4 sheet, 250 x 354 millimeters.</summary>
        public const short DMPAPER_B4 = 0x000C;

        public const short DMPAPER_B_PLUS = 0x003A;

        ///<summary>B4 (JIS) rotated sheet, 364 x 257 millimeters.</summary>
        public const short DMPAPER_B4_JIS_ROTATED = 0x004F;

        ///<summary>B5 sheet, 182 x 257-millimeter paper.</summary>
        public const short DMPAPER_B5 = 0x000D;

        ///<summary>B5 (JIS) rotated sheet, 257 x 182 millimeters.</summary>
        public const short DMPAPER_B5_JIS_ROTATED = 0x0050;

        ///<summary>B6 (JIS) sheet, 128 x 182 millimeters.</summary>
        public const short DMPAPER_B6_JIS = 0x0058;

        ///<summary>B6 (JIS) rotated sheet, 182 x 128 millimeters.</summary>
        public const short DMPAPER_B6_JIS_ROTATED = 0x0059;

        ///<summary>C Sheet, 17 x 22 inches.</summary>
        public const short DMPAPER_CSHEET = 0x0018;

        public const short DMPAPER_JAPANESE_POSTCARD = 0x002B;

        ///<summary>Double Japanese Postcard, 200 x 148 millimeters.</summary>
        public const short DMPAPER_DBL_JAPANESE_POSTCARD = 0x0045;

        ///<summary>Double Japanese Postcard Rotated, 148 x 200 millimeters.</summary>
        public const short DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED = 0x0052;

        ///<summary>D Sheet, 22 x 34 inches.</summary>
        public const short DMPAPER_DSHEET = 0x0019;

        ///<summary>#9 Envelope, 3 7/8 x 8 7/8 inches.</summary>
        public const short DMPAPER_ENV_9 = 0x0013;

        ///<summary>#10 Envelope, 4 1/8 x 9 1/2 inches.</summary>
        public const short DMPAPER_ENV_10 = 0x0014;

        ///<summary>#11 Envelope, 4 1/2 x 10 3/8 inches.</summary>
        public const short DMPAPER_ENV_11 = 0x0015;

        ///<summary>#12 Envelope, 4 3/4 x 11 inches.</summary>
        public const short DMPAPER_ENV_12 = 0x0016;

        ///<summary>#14 Envelope, 5 x 11 1/2 inches.</summary>
        public const short DMPAPER_ENV_14 = 0x0017;

        ///<summary>C5 Envelope, 162 x 229 millimeters.</summary>
        public const short DMPAPER_ENV_C5 = 0x001C;

        ///<summary>C3 Envelope, 324 x 458 millimeters.</summary>
        public const short DMPAPER_ENV_C3 = 0x001D;

        ///<summary>C4 Envelope, 229 x 324 millimeters.</summary>
        public const short DMPAPER_ENV_C4 = 0x001E;

        ///<summary>C6 Envelope, 114 x 162 millimeters.</summary>
        public const short DMPAPER_ENV_C6 = 0x001F;

        ///<summary>C65 Envelope, 114 x 229 millimeters.</summary>
        public const short DMPAPER_ENV_C65 = 0x0020;

        ///<summary>B4 Envelope, 250 x 353 millimeters.</summary>
        public const short DMPAPER_ENV_B4 = 0x0021;

        ///<summary>B5 Envelope, 176 x 250 millimeters.</summary>
        public const short DMPAPER_ENV_B5 = 0x0022;

        ///<summary>B6 Envelope, 176 x 125 millimeters.</summary>
        public const short DMPAPER_ENV_B6 = 0x0023;

        ///<summary>DL Envelope, 110 x 220 millimeters.</summary>
        public const short DMPAPER_ENV_DL = 0x001B;

        ///<summary>Italy Envelope, 110 x 230 millimeters.</summary>
        public const short DMPAPER_ENV_ITALY = 0x0024;

        ///<summary>Monarch Envelope, 3 7/8 x 7 1/2 inches.</summary>
        public const short DMPAPER_ENV_MONARCH = 0x0025;

        ///<summary>6 3/4 Envelope, 3 5/8 x 6 1/2 inches.</summary>
        public const short DMPAPER_ENV_PERSONAL = 0x0026;

        ///<summary>E Sheet, 34 x 44 inches.</summary>
        public const short DMPAPER_ESHEET = 0x001A;

        ///<summary>Executive, 7 1/4 x 10 1/2 inches.</summary>
        public const short DMPAPER_EXECUTIVE = 0x0007;

        ///<summary>US Std Fanfold, 14 7/8 x 11 inches.</summary>
        public const short DMPAPER_FANFOLD_US = 0x0027;

        ///<summary>German Std Fanfold, 8 1/2 x 12 inches.</summary>
        public const short DMPAPER_FANFOLD_STD_GERMAN = 0x0028;

        ///<summary>German Legal Fanfold, 8 x 13 inches.</summary>
        public const short DMPAPER_FANFOLD_LGL_GERMAN = 0x0029;

        ///<summary>Folio, 8 1/2 x 13-inch paper.</summary>
        public const short DMPAPER_FOLIO = 0x000E;

        ///<summary>Japanese Postcard Rotated, 148 x 100 millimeters.</summary>
        public const short DMPAPER_JAPANESE_POSTCARD_ROTATED = 0x0051;

        ///<summary>Japanese Envelope Chou #3.</summary>
        public const short DMPAPER_JENV_CHOU3 = 0x0049;

        ///<summary>Japanese Envelope Chou #3 Rotated.</summary>
        public const short DMPAPER_JENV_CHOU3_ROTATED = 0x0056;

        ///<summary>Japanese Envelope Chou #4.</summary>
        public const short DMPAPER_JENV_CHOU4 = 0x004A;

        ///<summary>Japanese Envelope Chou #4 Rotated.</summary>
        public const short DMPAPER_JENV_CHOU4_ROTATED = 0x0057;

        ///<summary>Japanese Envelope Kaku #2.</summary>
        public const short DMPAPER_JENV_KAKU2 = 0x0047;

        ///<summary>Japanese Envelope Kaku #2 Rotated.</summary>
        public const short DMPAPER_JENV_KAKU2_ROTATED = 0x0054;

        ///<summary>Japanese Envelope Kaku #3.</summary>
        public const short DMPAPER_JENV_KAKU3 = 0x0048;

        ///<summary>Japanese Envelope Kaku #3 Rotated.</summary>
        public const short DMPAPER_JENV_KAKU3_ROTATED = 0x0055;

        ///<summary>Japanese Envelope You #4.</summary>
        public const short DMPAPER_JENV_YOU4 = 0x005B;

        ///<summary>Japanese Envelope You #4.</summary>
        public const short DMPAPER_JENV_YOU4_ROTATED = 0x005C;

        ///<summary>Ledger, 17 x 11 inches.</summary>
        public const short DMPAPER_LEDGER = 0x0004;

        public const short DMPAPER_LETTER_EXTRA = 0x0032;

        ///<summary>Letter Rotated, 11 by 8 1/2 inches.</summary>
        public const short DMPAPER_LETTER_ROTATED = 0x004B;

        ///<summary>Letter Small, 8 1/2 x 11 inches.</summary>
        public const short DMPAPER_LETTERSMALL = 0x0002;

        ///<summary>Note, 8 1/2 x 11-inches.</summary>
        public const short DMPAPER_NOTE = 0x0012;

        ///<summary>PRC 16K, 146 x 215 millimeters.</summary>
        public const short DMPAPER_P16K = 0x005D;

        ///<summary>PRC 16K Rotated, 215 x 146 millimeters.</summary>
        public const short DMPAPER_P16K_ROTATED = 0x006A;

        ///<summary>PRC 32K, 97 x 151 millimeters.</summary>
        public const short DMPAPER_P32K = 0x005E;

        ///<summary>PRC 32K Rotated, 151 x 97 millimeters.</summary>
        public const short DMPAPER_P32K_ROTATED = 0x006B;

        ///<summary>PRC 32K(Big) 97 x 151 millimeters.</summary>
        public const short DMPAPER_P32KBIG = 0x005F;

        ///<summary>PRC 32K(Big) Rotated, 151 x 97 millimeters.</summary>
        public const short DMPAPER_P32KBIG_ROTATED = 0x006C;

        ///<summary>PRC Envelope #1, 102 by 165 millimeters.</summary>
        public const short DMPAPER_PENV_1 = 0x0060;

        ///<summary>PRC Envelope #1 Rotated, 165 x 102 millimeters.</summary>
        public const short DMPAPER_PENV_1_ROTATED = 0x006D;

        ///<summary>PRC Envelope #2, 102 x 176 millimeters.</summary>
        public const short DMPAPER_PENV_2 = 0x0061;

        ///<summary>PRC Envelope #2 Rotated, 176 x 102 millimeters.</summary>
        public const short DMPAPER_PENV_2_ROTATED = 0x006E;

        ///<summary>PRC Envelope #3, 125 x 176 millimeters.</summary>
        public const short DMPAPER_PENV_3 = 0x0062;

        ///<summary>PRC Envelope #3 Rotated, 176 x 125 millimeters.</summary>
        public const short DMPAPER_PENV_3_ROTATED = 0x006F;

        ///<summary>PRC Envelope #4, 110 x 208 millimeters.</summary>
        public const short DMPAPER_PENV_4 = 0x0063;

        ///<summary>PRC Envelope #4 Rotated, 208 x 110 millimeters.</summary>
        public const short DMPAPER_PENV_4_ROTATED = 0x0070;

        ///<summary>PRC Envelope #5, 110 x 220 millimeters.</summary>
        public const short DMPAPER_PENV_5 = 0x0064;

        ///<summary>PRC Envelope #5 Rotated, 220 x 110 millimeters.</summary>
        public const short DMPAPER_PENV_5_ROTATED = 0x0071;

        ///<summary>PRC Envelope #6, 120 x 230 millimeters.</summary>
        public const short DMPAPER_PENV_6 = 0x0065;

        ///<summary>PRC Envelope #6 Rotated, 230 x 120 millimeters.</summary>
        public const short DMPAPER_PENV_6_ROTATED = 0x0072;

        ///<summary>PRC Envelope #7, 160 x 230 millimeters.</summary>
        public const short DMPAPER_PENV_7 = 0x0066;

        ///<summary>PRC Envelope #7 Rotated, 230 x 160 millimeters.</summary>
        public const short DMPAPER_PENV_7_ROTATED = 0x0073;

        ///<summary>PRC Envelope #8, 120 x 309 millimeters.</summary>
        public const short DMPAPER_PENV_8 = 0x0067;

        ///<summary>PRC Envelope #8 Rotated, 309 x 120 millimeters.</summary>
        public const short DMPAPER_PENV_8_ROTATED = 0x0074;

        ///<summary>PRC Envelope #9, 229 x 324 millimeters.</summary>
        public const short DMPAPER_PENV_9 = 0x0068;

        ///<summary>PRC Envelope #9 Rotated, 324 x 229 millimeters.</summary>
        public const short DMPAPER_PENV_9_ROTATED = 0x0075;

        ///<summary>PRC Envelope #10, 324 x 458 millimeters.</summary>
        public const short DMPAPER_PENV_10 = 0x0069;

        ///<summary>PRC Envelope #10 Rotated, 458 x 324 millimeters.</summary>
        public const short DMPAPER_PENV_10_ROTATED = 0x0076;

        ///<summary>Quarto, 215 x 275 millimeter paper.</summary>
        public const short DMPAPER_QUARTO = 0x000F;

        ///<summary>Statement, 5 1/2 x 8 1/2 inches.</summary>
        public const short DMPAPER_STATEMENT = 0x0006;

        ///<summary>Tabloid, 11 x 17 inches.</summary>
        public const short DMPAPER_TABLOID = 0x0003;

        public const short DMPAPER_TABLOID_EXTRA = 0x0034;

        private const short DMPAPER_USER = 0x0100;
    }
}