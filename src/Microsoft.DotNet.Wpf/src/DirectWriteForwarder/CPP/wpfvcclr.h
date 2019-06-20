// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//  This file is a copy of <vcclr.h>, with modifications necessary for WPF.
//------------------------------------------------------------------------------

#if _MSC_VER > 1000
#pragma once
#endif

#if !defined(_INC_WPFVCCLR)
#define _INC_WPFVCCLR
#ifndef RC_INVOKED

#using <mscorlib.dll>
#include <gcroot.h>

#pragma warning(push)
#pragma warning(disable:4400)

#ifdef __cplusplus_cli
typedef cli::interior_ptr<const System::Char> __const_Char_ptr;
typedef cli::interior_ptr<const System::Byte> __const_Byte_ptr;
typedef cli::interior_ptr<System::Byte> _Byte_ptr;
typedef const System::String^ __const_String_handle;
#define _NULLPTR nullptr
#else
typedef const System::Char* __const_Char_ptr;
typedef const System::Byte* __const_Byte_ptr;
typedef System::Byte* _Byte_ptr;
typedef const System::String* __const_String_handle;
#define _NULLPTR 0
#endif

/// <remarks>
///     The standard PtrToStringChars function in vcclr.h is not annotated as
///     SecurityCritical. A recent change to the compiler detects this and
///     prevents it from being inlined.  For some reason, this now causes
///     JIT-ing the first time this method is called.  This JIT happens in a
///     startup code path that is sensitive to perf.  So we make a copy here
///     and annotate it.
/// </remarks>
inline __const_Char_ptr CriticalPtrToStringChars(__const_String_handle s)
{
    _Byte_ptr bp = const_cast<_Byte_ptr>(reinterpret_cast<__const_Byte_ptr>(s));
    if( bp != _NULLPTR )
    {
        unsigned offset = System::Runtime::CompilerServices::RuntimeHelpers::OffsetToStringData;
        bp += offset;
    }
    return reinterpret_cast<__const_Char_ptr>(bp);
}

#pragma warning(pop)

#undef _NULLPTR

#endif /* RC_INVOKED */
#endif //_INC_WPFVCCLR
