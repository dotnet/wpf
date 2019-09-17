// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __GLOBAL_INIT_H
#define __GLOBAL_INIT_H

// This class is used to initialize the global arrays below.
private ref class GlobalInit
{
    static Object^ _staticLock = gcnew Object();
    static bool _isInitialized = false;

public:
    static void Init();
};
#endif // __GLOBAL_INIT_H
