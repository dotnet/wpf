// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
All rights reserved.

--*/

namespace MS.Internal.Printing.Configuration.Interop
{
    using System.Printing;

    public partial class GdiPrintTicketProvider
    {
        bool TryGetPaperSize(PageMediaSizeName pageMediaSizeName, out short value)
        {
            bool result = false;
            value = unchecked((short)0xFFFFFFFF);

            switch (pageMediaSizeName)
            {
                case PageMediaSizeName.BusinessCard: break;
                case PageMediaSizeName.CreditCard: break;
                case PageMediaSizeName.ISOA0: break;
                case PageMediaSizeName.ISOA1: break;
                case PageMediaSizeName.ISOA10: break;
                case PageMediaSizeName.ISOA2: break;
                case PageMediaSizeName.ISOA3: { result = true; value = DevModePaperSizes.DMPAPER_A3; break; }
                case PageMediaSizeName.ISOA3Extra: break;
                case PageMediaSizeName.ISOA3Rotated: { result = true; value = DevModePaperSizes.DMPAPER_A3_ROTATED; break; }
                case PageMediaSizeName.ISOA4: { result = true; value = DevModePaperSizes.DMPAPER_A4; break; }
                case PageMediaSizeName.ISOA4Extra: break;
                case PageMediaSizeName.ISOA4Rotated: { result = true; value = DevModePaperSizes.DMPAPER_A4_ROTATED; break; }
                case PageMediaSizeName.ISOA5: { result = true; value = DevModePaperSizes.DMPAPER_A5; break; }
                case PageMediaSizeName.ISOA5Extra: break;
                case PageMediaSizeName.ISOA5Rotated: { result = true; value = DevModePaperSizes.DMPAPER_A5_ROTATED; break; }
                case PageMediaSizeName.ISOA6: { result = true; value = DevModePaperSizes.DMPAPER_A6; break; }
                case PageMediaSizeName.ISOA6Rotated: { result = true; value = DevModePaperSizes.DMPAPER_A6_ROTATED; break; }
                case PageMediaSizeName.ISOA7: break;
                case PageMediaSizeName.ISOA8: break;
                case PageMediaSizeName.ISOA9: break;
                case PageMediaSizeName.ISOB0: break;
                case PageMediaSizeName.ISOB1: break;
                case PageMediaSizeName.ISOB10: break;
                case PageMediaSizeName.ISOB2: break;
                case PageMediaSizeName.ISOB3: break;
                case PageMediaSizeName.ISOB4: { result = true; value = DevModePaperSizes.DMPAPER_B4; break; } // verify may be JISB4
                case PageMediaSizeName.ISOB4Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_B4; break; } //verify may be JIS
                case PageMediaSizeName.ISOB5Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_B5; break; } //verify may be JIS
                case PageMediaSizeName.ISOB5Extra: break;
                case PageMediaSizeName.ISOB7: break;
                case PageMediaSizeName.ISOB8: break;
                case PageMediaSizeName.ISOB9: break;
                case PageMediaSizeName.ISOC0: break;
                case PageMediaSizeName.ISOC1: break;
                case PageMediaSizeName.ISOC10: break;
                case PageMediaSizeName.ISOC2: break;
                case PageMediaSizeName.ISOC3: break;
                case PageMediaSizeName.ISOC3Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_C3; break; }
                case PageMediaSizeName.ISOC4: break;
                case PageMediaSizeName.ISOC4Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_C4; break; }
                case PageMediaSizeName.ISOC5: break;
                case PageMediaSizeName.ISOC5Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_C5; break; }
                case PageMediaSizeName.ISOC6: break;
                case PageMediaSizeName.ISOC6C5Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_C65; break; }
                case PageMediaSizeName.ISOC6Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_C6; break; }
                case PageMediaSizeName.ISOC7: break;
                case PageMediaSizeName.ISOC8: break;
                case PageMediaSizeName.ISOC9: break;
                case PageMediaSizeName.ISODLEnvelope: break;
                case PageMediaSizeName.ISODLEnvelopeRotated: break;
                case PageMediaSizeName.ISOSRA3: break;
                case PageMediaSizeName.Japan2LPhoto: break;
                case PageMediaSizeName.JapanChou3Envelope: { result = true; value = DevModePaperSizes.DMPAPER_JENV_CHOU3; break; }
                case PageMediaSizeName.JapanChou3EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_JENV_CHOU3_ROTATED; break; }
                case PageMediaSizeName.JapanChou4Envelope: { result = true; value = DevModePaperSizes.DMPAPER_JENV_CHOU4; break; }
                case PageMediaSizeName.JapanChou4EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_JENV_CHOU4_ROTATED; break; }
                case PageMediaSizeName.JapanDoubleHagakiPostcard: { result = true; value = DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD; break; }
                case PageMediaSizeName.JapanDoubleHagakiPostcardRotated: { result = true; value = DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED; break; }
                case PageMediaSizeName.JapanHagakiPostcard: break;
                case PageMediaSizeName.JapanHagakiPostcardRotated: { result = true; value = DevModePaperSizes.DMPAPER_JAPANESE_POSTCARD_ROTATED; break; }
                case PageMediaSizeName.JapanKaku2Envelope: { result = true; value = DevModePaperSizes.DMPAPER_JENV_KAKU2; break; }
                case PageMediaSizeName.JapanKaku2EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_JENV_KAKU2_ROTATED; break; }
                case PageMediaSizeName.JapanKaku3Envelope: { result = true; value = DevModePaperSizes.DMPAPER_JENV_KAKU3; break; }
                case PageMediaSizeName.JapanKaku3EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_JENV_KAKU3_ROTATED; break; }
                case PageMediaSizeName.JapanLPhoto: break;
                case PageMediaSizeName.JapanQuadrupleHagakiPostcard: break;
                case PageMediaSizeName.JapanYou1Envelope: break;
                case PageMediaSizeName.JapanYou2Envelope: break;
                case PageMediaSizeName.JapanYou3Envelope: break;
                case PageMediaSizeName.JapanYou4Envelope: { result = true; value = DevModePaperSizes.DMPAPER_JENV_YOU4; break; }
                case PageMediaSizeName.JapanYou4EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_JENV_YOU4_ROTATED; break; }
                case PageMediaSizeName.JapanYou6Envelope: break;
                case PageMediaSizeName.JapanYou6EnvelopeRotated: break;
                case PageMediaSizeName.JISB0: break;
                case PageMediaSizeName.JISB1: break;
                case PageMediaSizeName.JISB10: break;
                case PageMediaSizeName.JISB2: break;
                case PageMediaSizeName.JISB3: break;
                case PageMediaSizeName.JISB4: { result = true; value = DevModePaperSizes.DMPAPER_B4; break; } // verify may be ISO
                case PageMediaSizeName.JISB4Rotated: { result = true; value = DevModePaperSizes.DMPAPER_B4_JIS_ROTATED; break; }
                case PageMediaSizeName.JISB5: { result = true; value = DevModePaperSizes.DMPAPER_B5; break; } // verify may be ISO
                case PageMediaSizeName.JISB5Rotated: { result = true; value = DevModePaperSizes.DMPAPER_B5_JIS_ROTATED; break; }
                case PageMediaSizeName.JISB6: { result = true; value = DevModePaperSizes.DMPAPER_B6_JIS; break; }
                case PageMediaSizeName.JISB6Rotated: { result = true; value = DevModePaperSizes.DMPAPER_B6_JIS_ROTATED; break; }
                case PageMediaSizeName.JISB7: break;
                case PageMediaSizeName.JISB8: break;
                case PageMediaSizeName.JISB9: break;
                case PageMediaSizeName.NorthAmerica10x11: break;
                case PageMediaSizeName.NorthAmerica10x12: break;
                case PageMediaSizeName.NorthAmerica10x14: { result = true; value = DevModePaperSizes.DMPAPER_10X14; break; }
                case PageMediaSizeName.NorthAmerica11x17: { result = true; value = DevModePaperSizes.DMPAPER_11X17; break; }
                case PageMediaSizeName.NorthAmerica14x17: break;
                case PageMediaSizeName.NorthAmerica4x6: break;
                case PageMediaSizeName.NorthAmerica4x8: break;
                case PageMediaSizeName.NorthAmerica5x7: break;
                case PageMediaSizeName.NorthAmerica8x10: break;
                case PageMediaSizeName.NorthAmerica9x11: break;
                case PageMediaSizeName.NorthAmericaArchitectureASheet: break;
                case PageMediaSizeName.NorthAmericaArchitectureBSheet: break;
                case PageMediaSizeName.NorthAmericaArchitectureCSheet: break;
                case PageMediaSizeName.NorthAmericaArchitectureDSheet: break;
                case PageMediaSizeName.NorthAmericaArchitectureESheet: break;
                case PageMediaSizeName.NorthAmericaCSheet: { result = true; value = DevModePaperSizes.DMPAPER_CSHEET; break; }
                case PageMediaSizeName.NorthAmericaDSheet: { result = true; value = DevModePaperSizes.DMPAPER_DSHEET; break; }
                case PageMediaSizeName.NorthAmericaESheet: { result = true; value = DevModePaperSizes.DMPAPER_ESHEET; break; }
                case PageMediaSizeName.NorthAmericaExecutive: { result = true; value = DevModePaperSizes.DMPAPER_EXECUTIVE; break; }
                case PageMediaSizeName.NorthAmericaGermanLegalFanfold: { result = true; value = DevModePaperSizes.DMPAPER_FANFOLD_LGL_GERMAN; break; }
                case PageMediaSizeName.NorthAmericaGermanStandardFanfold: { result = true; value = DevModePaperSizes.DMPAPER_FANFOLD_STD_GERMAN; break; }
                case PageMediaSizeName.NorthAmericaLegal: break;
                case PageMediaSizeName.NorthAmericaLegalExtra: break;
                case PageMediaSizeName.NorthAmericaLetter: { result = true; value = DevModePaperSizes.DMPAPER_LETTER; break; }
                case PageMediaSizeName.NorthAmericaLetterExtra: break;
                case PageMediaSizeName.NorthAmericaLetterPlus: break;
                case PageMediaSizeName.NorthAmericaLetterRotated: { result = true; value = DevModePaperSizes.DMPAPER_LETTER_ROTATED; break; }
                case PageMediaSizeName.NorthAmericaMonarchEnvelope: break;
                case PageMediaSizeName.NorthAmericaNote: { result = true; value = DevModePaperSizes.DMPAPER_NOTE; break; }
                case PageMediaSizeName.NorthAmericaNumber10Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_10; break; }
                case PageMediaSizeName.NorthAmericaNumber10EnvelopeRotated: break;
                case PageMediaSizeName.NorthAmericaNumber11Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_11; break; }
                case PageMediaSizeName.NorthAmericaNumber12Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_12; break; }
                case PageMediaSizeName.NorthAmericaNumber14Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_14; break; }
                case PageMediaSizeName.NorthAmericaNumber9Envelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_9; break; }
                case PageMediaSizeName.NorthAmericaPersonalEnvelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_PERSONAL; break; };
                case PageMediaSizeName.NorthAmericaQuarto: { result = true; value = DevModePaperSizes.DMPAPER_QUARTO; break; }
                case PageMediaSizeName.NorthAmericaStatement: { result = true; value = DevModePaperSizes.DMPAPER_STATEMENT; break; }
                case PageMediaSizeName.NorthAmericaSuperA: break;
                case PageMediaSizeName.NorthAmericaSuperB: break;
                case PageMediaSizeName.NorthAmericaTabloid: { result = true; value = DevModePaperSizes.DMPAPER_TABLOID; break; }
                case PageMediaSizeName.NorthAmericaTabloidExtra: break;
                case PageMediaSizeName.OtherMetricA3Plus: break;
                case PageMediaSizeName.OtherMetricA4Plus: break;
                case PageMediaSizeName.OtherMetricFolio: { result = true; value = DevModePaperSizes.DMPAPER_FOLIO; break; }
                case PageMediaSizeName.OtherMetricInviteEnvelope: break;
                case PageMediaSizeName.OtherMetricItalianEnvelope: { result = true; value = DevModePaperSizes.DMPAPER_ENV_ITALY; break; }
                case PageMediaSizeName.PRC10Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_10; break; }
                case PageMediaSizeName.PRC10EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_10_ROTATED; break; }
                case PageMediaSizeName.PRC16K: { result = true; value = DevModePaperSizes.DMPAPER_P16K; break; }
                case PageMediaSizeName.PRC16KRotated: { result = true; value = DevModePaperSizes.DMPAPER_P16K_ROTATED; break; }
                case PageMediaSizeName.PRC1Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_1; break; }
                case PageMediaSizeName.PRC1EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_1_ROTATED; break; }
                case PageMediaSizeName.PRC2Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_2; break; }
                case PageMediaSizeName.PRC2EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_2_ROTATED; break; }
                case PageMediaSizeName.PRC32K: { result = true; value = DevModePaperSizes.DMPAPER_P32K; break; }
                case PageMediaSizeName.PRC32KBig: { result = true; value = DevModePaperSizes.DMPAPER_P32KBIG; break; }
                case PageMediaSizeName.PRC32KRotated: { result = true; value = DevModePaperSizes.DMPAPER_P32KBIG_ROTATED; break; }
                case PageMediaSizeName.PRC3Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_3; break; }
                case PageMediaSizeName.PRC3EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_3_ROTATED; break; }
                case PageMediaSizeName.PRC4Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_4; break; }
                case PageMediaSizeName.PRC4EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_4_ROTATED; break; }
                case PageMediaSizeName.PRC5Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_5; break; }
                case PageMediaSizeName.PRC5EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_5_ROTATED; break; }
                case PageMediaSizeName.PRC6Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_6; break; }
                case PageMediaSizeName.PRC6EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_6_ROTATED; break; }
                case PageMediaSizeName.PRC7Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_7; break; }
                case PageMediaSizeName.PRC7EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_7_ROTATED; break; }
                case PageMediaSizeName.PRC8Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_8; break; }
                case PageMediaSizeName.PRC8EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_8_ROTATED; break; }
                case PageMediaSizeName.PRC9Envelope: { result = true; value = DevModePaperSizes.DMPAPER_PENV_9; break; }
                case PageMediaSizeName.PRC9EnvelopeRotated: { result = true; value = DevModePaperSizes.DMPAPER_PENV_9_ROTATED; break; }
                case PageMediaSizeName.Roll04Inch: break;
                case PageMediaSizeName.Roll06Inch: break;
                case PageMediaSizeName.Roll08Inch: break;
                case PageMediaSizeName.Roll12Inch: break;
                case PageMediaSizeName.Roll15Inch: break;
                case PageMediaSizeName.Roll18Inch: break;
                case PageMediaSizeName.Roll22Inch: break;
                case PageMediaSizeName.Roll24Inch: break;
                case PageMediaSizeName.Roll30Inch: break;
                case PageMediaSizeName.Roll36Inch: break;
                case PageMediaSizeName.Roll54Inch: break;
            }

            return result;
        }

        private bool TryGetPaperSize(short devModePaperSizeCode, out PageMediaSize pageMediaSize)
        {
            bool result = false;

            pageMediaSize = null;

            switch (devModePaperSizeCode)
            {
                case DevModePaperSizes.DMPAPER_LETTER: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetter); break; }
                case DevModePaperSizes.DMPAPER_LEGAL: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLegal); break; }
                case DevModePaperSizes.DMPAPER_10X14: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmerica10x14); break; }
                case DevModePaperSizes.DMPAPER_11X17: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmerica11x17); break; }
                case DevModePaperSizes.DMPAPER_12X11: break;
                case DevModePaperSizes.DMPAPER_A3: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA3); break; }
                case DevModePaperSizes.DMPAPER_A3_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA3Rotated); break; }
                case DevModePaperSizes.DMPAPER_A4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4); break; }
                case DevModePaperSizes.DMPAPER_A4_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4Rotated); break; }
                case DevModePaperSizes.DMPAPER_A4SMALL: break;
                case DevModePaperSizes.DMPAPER_A5: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5); break; }
                case DevModePaperSizes.DMPAPER_A5_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5Rotated); break; }
                case DevModePaperSizes.DMPAPER_A6: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA6); break; }
                case DevModePaperSizes.DMPAPER_A6_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA6Rotated); break; }
                case DevModePaperSizes.DMPAPER_B4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB4); break; } //  verify may be ISO B4
                case DevModePaperSizes.DMPAPER_B4_JIS_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB4Rotated); break; }
                case DevModePaperSizes.DMPAPER_B5: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB5); break; } //  verify may be ISO B5
                case DevModePaperSizes.DMPAPER_B5_JIS_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB5Rotated); break; }
                case DevModePaperSizes.DMPAPER_B6_JIS: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB6); break; }
                case DevModePaperSizes.DMPAPER_B6_JIS_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JISB6Rotated); break; }
                case DevModePaperSizes.DMPAPER_CSHEET: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaCSheet); break; }
                case DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanDoubleHagakiPostcard); break; }
                case DevModePaperSizes.DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanDoubleHagakiPostcardRotated); break; }
                case DevModePaperSizes.DMPAPER_DSHEET: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaDSheet); break; }
                case DevModePaperSizes.DMPAPER_ENV_9: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNumber9Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_10: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNumber10Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_11: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNumber11Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_12: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNumber12Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_14: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNumber14Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_C5: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOC5Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_C3: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOC3Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_C4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOC4Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_C6: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOC6Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_C65: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOC6C5Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_B4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOB4Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_B5: { pageMediaSize = new PageMediaSize(PageMediaSizeName.ISOB5Envelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_B6: break;
                case DevModePaperSizes.DMPAPER_ENV_DL: break;
                case DevModePaperSizes.DMPAPER_ENV_ITALY: { pageMediaSize = new PageMediaSize(PageMediaSizeName.OtherMetricItalianEnvelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_MONARCH: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaMonarchEnvelope); break; }
                case DevModePaperSizes.DMPAPER_ENV_PERSONAL: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaPersonalEnvelope); break; }
                case DevModePaperSizes.DMPAPER_ESHEET: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaESheet); break; }
                case DevModePaperSizes.DMPAPER_EXECUTIVE: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaExecutive); break; }
                case DevModePaperSizes.DMPAPER_FANFOLD_US: break;
                case DevModePaperSizes.DMPAPER_FANFOLD_STD_GERMAN: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaGermanStandardFanfold); break; }
                case DevModePaperSizes.DMPAPER_FANFOLD_LGL_GERMAN: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaGermanLegalFanfold); break; }
                case DevModePaperSizes.DMPAPER_FOLIO: { pageMediaSize = new PageMediaSize(PageMediaSizeName.OtherMetricFolio); break; }
                case DevModePaperSizes.DMPAPER_JAPANESE_POSTCARD_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanHagakiPostcardRotated); break; }
                case DevModePaperSizes.DMPAPER_JENV_CHOU3: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanChou3Envelope); break; }
                case DevModePaperSizes.DMPAPER_JENV_CHOU3_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanChou3EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_JENV_CHOU4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanChou4Envelope); break; }
                case DevModePaperSizes.DMPAPER_JENV_CHOU4_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanChou4EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_JENV_KAKU2: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanKaku2Envelope); break; }
                case DevModePaperSizes.DMPAPER_JENV_KAKU2_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanKaku2EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_JENV_KAKU3: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanKaku3Envelope); break; }
                case DevModePaperSizes.DMPAPER_JENV_KAKU3_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanKaku3EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_JENV_YOU4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanYou4Envelope); break; }
                case DevModePaperSizes.DMPAPER_JENV_YOU4_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.JapanYou4EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_LEDGER: break;
                case DevModePaperSizes.DMPAPER_LETTER_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetterRotated); break; }
                case DevModePaperSizes.DMPAPER_LETTERSMALL: break;
                case DevModePaperSizes.DMPAPER_NOTE: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaNote); break; }
                case DevModePaperSizes.DMPAPER_P16K: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC16K); break; }
                case DevModePaperSizes.DMPAPER_P16K_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC16KRotated); break; }
                case DevModePaperSizes.DMPAPER_P32K: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC32K); break; }
                case DevModePaperSizes.DMPAPER_P32K_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC32KRotated); break; }
                case DevModePaperSizes.DMPAPER_P32KBIG: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC32KBig); break; }
                case DevModePaperSizes.DMPAPER_P32KBIG_ROTATED: break;
                case DevModePaperSizes.DMPAPER_PENV_1: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC1Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_1_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC1EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_2: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC2Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_2_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC2EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_3: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC3Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_3_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC3EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_4: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC4Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_4_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC4EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_5: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC5Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_5_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC5EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_6: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC6Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_6_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC6EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_7: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC7Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_7_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC7EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_8: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC8Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_8_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC8EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_9: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC9Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_9_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC9EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_PENV_10: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC10Envelope); break; }
                case DevModePaperSizes.DMPAPER_PENV_10_ROTATED: { pageMediaSize = new PageMediaSize(PageMediaSizeName.PRC10EnvelopeRotated); break; }
                case DevModePaperSizes.DMPAPER_QUARTO: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaQuarto); break; }
                case DevModePaperSizes.DMPAPER_STATEMENT: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaStatement); break; }
                case DevModePaperSizes.DMPAPER_TABLOID: { pageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaTabloid); break; }
            }

            return pageMediaSize != null;
        }
    }
}