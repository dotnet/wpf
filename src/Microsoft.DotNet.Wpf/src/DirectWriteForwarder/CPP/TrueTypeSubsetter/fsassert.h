// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------------
//  Description:    
//      In files where we use Security Assert, C++ pre-processor is expanding
//      the security assert to the debug assert macro and causing errors.  This
//      file has the macros to get around that issue.  When you include this
//      file you should use FsAssert for debug Asserts.
//
//------------------------------------------------------------------------------

#undef Assert

#if DBG

#define FsAssert System::Diagnostics::Debug::Assert
#define assert System::Diagnostics::Debug::Assert

// disable the forcing value to bool 'true' or 'false' (performance warning)
#pragma warning(disable : 4800)

#else

#define FsAssert(expression)
#define assert(expression)

#endif
