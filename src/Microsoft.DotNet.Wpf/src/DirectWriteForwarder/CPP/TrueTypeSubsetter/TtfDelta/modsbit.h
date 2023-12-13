// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * ModSBIT.h: Interface file for ModSBIT.c - Written by Louise Pathe
  *
  *
  * 
  */
  
#ifndef MODSBIT_DOT_H_DEFINED
#define MODSBIT_DOT_H_DEFINED        

int16 ModSbit( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo,
              TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
              CONST uint8 *puchKeepGlyphList, 
              CONST uint16 usGlyphListCount,  
              uint32 *pulNewOutOffset);

#endif /* MODSBIT_DOT_H_DEFINED */
