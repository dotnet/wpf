// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//-----------------------------------------------------------------------------

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef _USE_MATH_DEFINES
#define _USE_MATH_DEFINES
#endif

#include <wpfsdl.h>
#include <windows.h>
#include <math.h>
#include <float.h>
#include <stdio.h>
#include <stddef.h>
#include <fcntl.h>
#include <assert.h>

#include <d3d9.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <guiddef.h>

#include <wingdi.h>
#include <ddraw.h>

#define STRSAFE_NO_DEPRECATE
#include <strsafe.h>

#include <intsafe.h>

#include "..\DbgXHelper\minnt.h"
#include "..\DbgXHelper\DbgXHelper.h"

//
// UNCONDITIONAL_EXPR suppresses warning 4127
//   C4127: conditional expression is constant
// Use when you know that the test may be unconditional but that is okay as in
// templates with conditional behavior.  For templates do consider __if_exists
// with a trait map as an alternative.
//
// Note: PREfast doesn't like comma operator when left expression doesn't have
//       a side-effect - see defects 319 and 5430.  So, for PREfast just use
//       the expression as is.
//
#if !defined(_PREFAST_)
    #define UNCONDITIONAL_EXPR(Exp) (0,Exp)
#else
    #define UNCONDITIONAL_EXPR(Exp) (Exp)
#endif

#include "argparse.hxx"
#include "mildbglib.hxx"
#include "..\DbgXHelper\dbghelpers.hxx"
#include "helpers.hxx"
#include "stackcapture.hxx"

extern ModuleParameters Milcore_Module;
extern ModuleParameters Type_Module;

// "redefine" isspace to always work from an unsigned char before promoting to
// int -- see PREfast warning 328.
#define isspace(c) isspace(static_cast<unsigned char>(c))

/////////////////////////////////////////////
//
//  .cxx
//
/////////////////////////////////////////////

extern BOOL gbVerbose;

#undef IFC
#define IFC(expr) {hr = (expr); if (FAILED(hr)) goto Cleanup;}
#define IGNORE_HR(x) ((void)(x))

#define UNUSED_PARAMETER(x) x

#define ARRAY_COMMA_ELEM_COUNT(A) ((A)), ARRAYSIZE((A))
#define ARRAY_SIZE(a) (ARRAYSIZE(a))

#define ReleaseInterface(p) if (p) { (p)->Release(); (p) = NULL; }



