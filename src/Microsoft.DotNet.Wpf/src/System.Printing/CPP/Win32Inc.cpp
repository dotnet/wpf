// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Win32Inc.hpp"

// Required to compile pure assembly : From http://msdn.microsoft.com/en-us/library/ms384352(VS.71).aspx
#ifdef __cplusplus
extern "C" { 
#endif
int _fltused=1; 
#ifdef __cplusplus
}
#endif
