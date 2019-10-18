// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    win_mil_graphics_media
//      $Keywords:
//
//  $Description:
//      Maintains all of the global variables
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#if (defined(DECLARE_GLOBALS) || !defined(_COMPOUND_GLOBALS_H_))
#define _COMMON_GLOBALS_H_

#ifdef DECLARE_GLOBALS

#define EXTERN
#define EQ(x) = x

#else

#define EXTERN extern
#define EQ(x)

#endif

EXTERN const ULONG      gc_ticksPerSecond       EQ(10000 * 1000);
EXTERN const DWORD      gc_dwordAllFlags        EQ(DWORD_MAX);
EXTERN const LONGLONG   gc_invalidTimerTime     EQ(MAXLONGLONG);
EXTERN const WCHAR      gc_milcoreName[]        EQ(L"milcore.dll");
EXTERN const long       gc_defaultAvalonVolume  EQ(50);

#undef EXTERN
#undef EQ

#endif



