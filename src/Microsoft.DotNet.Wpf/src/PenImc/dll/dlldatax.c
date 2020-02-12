// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// wrapper for dlldata.c

#ifdef _MERGE_PROXYSTUB // merge proxy stub DLL

#define REGISTER_PROXY_DLL //DllRegisterServer, etc.

#define USE_STUBLESS_PROXY	//defined only with MIDL switch /Oicf

// 'rpcndr' is a deprecated lib; link to rpcns4.lib and rpcrt4.lib instead.
#pragma comment(lib, "rpcns4.lib")
#pragma comment(lib, "rpcrt4.lib")

#define ENTRY_PREFIX	Prx

#include "dlldata.c"
#include <Penimc_p.c>

#endif //_MERGE_PROXYSTUB

