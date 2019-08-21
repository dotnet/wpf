// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __OPENTYPETABLETAG_H
#define __OPENTYPETABLETAG_H

#include "Common.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    private enum class OpenTypeTableTag
    {
        CharToIndexMap      = DWRITE_MAKE_OPENTYPE_TAG('c','m','a','p'),        /* 'cmap' */
        ControlValue        = DWRITE_MAKE_OPENTYPE_TAG('c','v','t',' '),        /* 'cvt ' */
        BitmapData          = DWRITE_MAKE_OPENTYPE_TAG('E','B','D','T'),        /* 'EBDT' */
        BitmapLocation      = DWRITE_MAKE_OPENTYPE_TAG('E','B','L','C'),        /* 'EBLC' */
        BitmapScale         = DWRITE_MAKE_OPENTYPE_TAG('E','B','S','C'),        /* 'EBSC' */
        Editor0             = DWRITE_MAKE_OPENTYPE_TAG('e','d','t','0'),        /* 'edt0' */
        Editor1             = DWRITE_MAKE_OPENTYPE_TAG('e','d','t','1'),        /* 'edt1' */
        Encryption          = DWRITE_MAKE_OPENTYPE_TAG('c','r','y','p'),        /* 'cryp' */
        FontHeader          = DWRITE_MAKE_OPENTYPE_TAG('h','e','a','d'),        /* 'head' */
        FontProgram         = DWRITE_MAKE_OPENTYPE_TAG('f','p','g','m'),        /* 'fpgm' */
        GridfitAndScanProc  = DWRITE_MAKE_OPENTYPE_TAG('g','a','s','p'),        /* 'gasp' */
        GlyphDirectory      = DWRITE_MAKE_OPENTYPE_TAG('g','d','i','r'),        /* 'gdir' */
        GlyphData           = DWRITE_MAKE_OPENTYPE_TAG('g','l','y','f'),        /* 'glyf' */
        HoriDeviceMetrics   = DWRITE_MAKE_OPENTYPE_TAG('h','d','m','x'),        /* 'hdmx' */
        HoriHeader          = DWRITE_MAKE_OPENTYPE_TAG('h','h','e','a'),        /* 'hhea' */
        HorizontalMetrics   = DWRITE_MAKE_OPENTYPE_TAG('h','m','t','x'),        /* 'hmtx' */
        IndexToLoc          = DWRITE_MAKE_OPENTYPE_TAG('l','o','c','a'),        /* 'loca' */
        Kerning             = DWRITE_MAKE_OPENTYPE_TAG('k','e','r','n'),        /* 'kern' */
        LinearThreshold     = DWRITE_MAKE_OPENTYPE_TAG('L','T','S','H'),        /* 'LTSH' */
        MaxProfile          = DWRITE_MAKE_OPENTYPE_TAG('m','a','x','p'),        /* 'maxp' */
        NamingTable         = DWRITE_MAKE_OPENTYPE_TAG('n','a','m','e'),        /* 'name' */
        OS_2                = DWRITE_MAKE_OPENTYPE_TAG('O','S','/','2'),        /* 'OS/2' */
        Postscript          = DWRITE_MAKE_OPENTYPE_TAG('p','o','s','t'),        /* 'post' */
        PreProgram          = DWRITE_MAKE_OPENTYPE_TAG('p','r','e','p'),        /* 'prep' */
        VertDeviceMetrics   = DWRITE_MAKE_OPENTYPE_TAG('V','D','M','X'),        /* 'VDMX' */
        VertHeader          = DWRITE_MAKE_OPENTYPE_TAG('v','h','e','a'),        /* 'vhea' */
        VerticalMetrics     = DWRITE_MAKE_OPENTYPE_TAG('v','m','t','x'),        /* 'vmtx' */
        PCLT                = DWRITE_MAKE_OPENTYPE_TAG('P','C','L','T'),        /* 'PCLT' */
        TTO_GSUB            = DWRITE_MAKE_OPENTYPE_TAG('G','S','U','B'),        /* 'GSUB' */
        TTO_GPOS            = DWRITE_MAKE_OPENTYPE_TAG('G','P','O','S'),        /* 'GPOS' */
        TTO_GDEF            = DWRITE_MAKE_OPENTYPE_TAG('G','D','E','F'),        /* 'GDEF' */
        TTO_BASE            = DWRITE_MAKE_OPENTYPE_TAG('B','A','S','E'),        /* 'BASE' */
        TTO_JSTF            = DWRITE_MAKE_OPENTYPE_TAG('J','S','T','F'),        /* 'JSTF' */
    };

}}}}//MS::Internal::Text::TextInterface

#endif __OPENTYPETABLETAG_H