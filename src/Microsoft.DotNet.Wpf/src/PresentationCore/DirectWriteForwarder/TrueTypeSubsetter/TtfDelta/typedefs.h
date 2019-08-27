// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * TypeDefs.h: stolen from FSCDEFS.H in FScaler project
  *
  *
  *
  */

#ifndef TYPEDEFS_DOT_H_DEFINED
#define TYPEDEFS_DOT_H_DEFINED

#include <windows.h>
#include <strsafe.h>
#include <stddef.h>
#include <limits.h>

#define true 1
#define false 0
#ifndef TRUE
#define TRUE    true
#endif

#ifndef FALSE
#define FALSE   false
#endif

#define ONEFIX      ( 1L << 16 )
#define ONEFRAC     ( 1L << 30 )
#define ONEHALFFIX  0x8000L
#define ONEVECSHIFT 16
#define HALFVECDIV  (1L << (ONEVECSHIFT-1))

#define NULL_GLYPH  0

typedef signed char int8;
typedef unsigned char uint8;
typedef short int16;
typedef unsigned short uint16;
typedef long int32;
typedef unsigned long uint32;

typedef short FUnit;
typedef unsigned short uFUnit;

typedef short ShortFract;                       /* 2.14 */

#ifndef F26Dot6
#define F26Dot6 long
#endif

#ifndef CONST
#define CONST const
#endif

#ifndef FAR
#define FAR
#endif

#ifndef NEAR
#define NEAR
#endif

/* Private Data Types */
typedef struct {
    int16 xMin;
    int16 yMin;
    int16 xMax;
    int16 yMax;
} BBOX;

typedef struct {
    F26Dot6 x;
    F26Dot6 y;
} point;

typedef int32 ErrorCode;

#define ALIGN(object, p) p =    (p + ((uint32)sizeof(object) - 1)) & ~((uint32)sizeof(object) - 1);

#define ROWBYTESLONG(x)     (((x + 31) >> 5) << 2)

#ifndef SHORTMUL
#define SHORTMUL(a,b)   (int32)((int32)(a) * (b))
#endif

#ifndef SHORTDIV
#define SHORTDIV(a,b)   (int32)((int32)(a) / (b))
#endif

/* Portable code to extract a short or a long from a 2- or 4-byte buffer */
/* which was encoded using Motorola 68000 (TrueType "native") byte order. */
#define FS_2BYTE(p)  ( ((unsigned short)((p)[0]) << 8) |  (p)[1])
#define FS_4BYTE(p)  ( FS_2BYTE((p)+2) | ( (FS_2BYTE(p)+0L) << 16) )

#define SWAPW(a)        ((int16) FS_2BYTE( (unsigned char *)(&a) ))
#define SWAPL(a)        ((int32) FS_4BYTE( (unsigned char *)(&a) ))
#define SWAPWINC(a)     SWAPW(*(a)); a++    /* Do NOT parenthesize! */

#define ASSERT(expression, message)

#ifndef Assert
#define Assert(a)
#endif

#ifndef INTEL    /* should be in stdio, but isn't on MAC */
#define max(a,b)    (((a) > (b)) ? (a) : (b))
#define min(a,b)    (((a) < (b)) ? (a) : (b))
#endif

/* for use by TTFF.h, from TTTypes.h */
typedef char CHAR;     /* lcp 3/97 take away signed so as not to conflict with winnt.h */
typedef unsigned short USHORT;
typedef short SHORT;
typedef long LONG;
typedef unsigned long ULONG;
typedef short FWord;
typedef unsigned short uFWord;
typedef short F2Dot14;
typedef unsigned char UCHAR;
typedef unsigned char BYTE;
typedef int BOOL;
/* #ifdef INTEL    lcp 5/1/97 well, this IS needed after all */
typedef long Fixed;
/* #endif */

#ifdef ICECAP
#define PRIVATE     /* don't define so Icecap will print function names */
#else
#define PRIVATE         static
#endif

#define MAXBUFFERLEN 256 /* convenient for temp buffers */

#if 0          /* for mac and other compilers without stricmp */
#include "mystring.h"
#define _stricmp my_stricmp
#define _strnicmp my_strnicmp
#endif

// on IA64 when we need to access data with an unaligned pointer, we need to use __unaligned
// this is typically when reading of writing data that has a specific alignement corresponding
// to a specific file format, ie Reading/Writing TrueType data

// (from ntdef.h)
#ifndef     UNALIGNED
#if defined(_M_MRX000) || defined(_M_AMD64) || defined(_M_PPC) || defined(_M_IA64)
#define UNALIGNED __unaligned
#else
#define UNALIGNED
#endif
#endif

#endif  /* TYPEDEFS_DOT_H_DEFINED */

