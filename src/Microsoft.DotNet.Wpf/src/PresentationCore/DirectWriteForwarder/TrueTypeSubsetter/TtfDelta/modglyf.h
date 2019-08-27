// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * ModGlyf.h: Interface file for ModGlyf.c - Written by Louise Pathe
  *
  *
  * 
  */
  
#ifndef MODGLYF_DOT_H_DEFINED
#define MODGLYF_DOT_H_DEFINED        

int16 ModGlyfLocaAndHead( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                     TTFACC_FILEBUFFERINFO * pOutBufferInfo,
                     uint8 *puchKeepGlyphList, 
                     uint16 usGlyphListCount, 
                     uint32 *pCheckSumAdjustment,
                     uint32 *pulNewOutOffset);

#endif /* MODGLYF_DOT_H_DEFINED */
