// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//-----------------------------------------------------------------------------
//

//
//-----------------------------------------------------------------------------

#pragma once


#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <wpfsdl.h>
#include <windows.h>
#include <intsafe.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <strsafe.h>
#include <dbghelp.h>

#include "minnt.h"

#include "DbgXHelper.h"
#include "debug.hxx"

#undef IFC
#define IFC(expr) {hr = (expr); if (FAILED(hr)) goto Cleanup;}

#ifndef ARRAY_SIZE
#define ARRAY_SIZE(a) (ARRAYSIZE(a))
#endif

#ifndef ReleaseInterface
#define ReleaseInterface(p) if (p) { (p)->Release(); (p) = NULL; }
#endif



