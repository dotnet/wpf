// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * Automap.h: Interface file for Automap.c - Written by Louise Pathe
  *
  *
  * 
  */
  
#ifndef AUTOMAP_DOT_H_DEFINED
#define AUTOMAP_DOT_H_DEFINED

int16 TTOAutoMap( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag);
int16 MortAutoMap( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag);
int16 AppleAutoMap(TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag);

#endif /* AUTOMAP_DOT_H_DEFINED */
