// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __PRINTERENUMARATION_H__
#define __PRINTERENUMARATION_H__

#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Collections;

namespace DirectInterop
{
public __gc class  PrintersEnumeration : public IDisposable
{
public:

    PrintersEnumeration(
        IntPtr  win32Buffer,
        UInt32  count
        );

    ~PrintersEnumeration(
        void
        );

    virtual
    void
    Dispose(
        void
        );

    IEnumerator*
    GetEnumerator(
        void
        );

private:

    void
    Dispose(
        bool disposing
        );
                    
    IPrinterInfo*	printerInfoTwoArray __gc [];
    IntPtr          win32EnumPrintersBuffer;    
    bool            isDisposed;
    UInt32          printersCount;
};
}
#endif
