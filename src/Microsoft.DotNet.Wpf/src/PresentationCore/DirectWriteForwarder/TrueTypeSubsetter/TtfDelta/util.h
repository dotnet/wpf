// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * util.h: Interface file for util.c - Written by Louise Pathe
  *
  * 
  */
  
#ifndef UTIL_DOT_H_DEFINED
#define UTIL_DOT_H_DEFINED        


uint16 log2( uint16 arg );
int16 ValueOKForShort(uint32 ulValue);
void DebugMsg( LPCSTR, LPCSTR, uint16 CONST);
#endif /* UTIL_DOT_H_DEFINED */
