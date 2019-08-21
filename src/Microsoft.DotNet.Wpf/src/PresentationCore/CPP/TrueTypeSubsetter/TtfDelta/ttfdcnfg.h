// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: ttfdcnfg.h
 *
 *
 * Provides some platform configuration for ttfdelta.
 *
 **************************************************************************/
#ifndef TTFDCNFG_DOT_H_DEFINED
#define TTFDCNFG_DOT_H_DEFINED  

// Using Microsoft C, callback functions for qsort and bsearch must be __cdecl
// Under C++/CLI, they are declared with __clrcall, which is the only option.
#ifndef _M_CEE
#define CRTCB __cdecl 
#else
#define CRTCB
#endif

#ifndef NO_CRT_ASSERT

#include <assert.h>

#endif

#endif  TTFDCNFG_DOT_H_DEFINED    