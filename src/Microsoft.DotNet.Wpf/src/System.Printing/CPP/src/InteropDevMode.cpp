// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#include <PrintingSwitches.h>



DeviceMode::
DeviceMode(
    array<Byte>^   devMode
    )
{
    data = (array<Byte>^)devMode;
}

//
// SECURITY: see header comment.
//
// Validates the attacker-controlled DEVMODE length fields against the supplied
// maxBytes upper bound before copying. This prevents a hostile remote print
// server from disclosing up to ~128 KB of adjacent client-process heap memory
// (CWE-125, WPF-V2-006). The historical implementation trusted
// devmode->dmSize + devmode->dmDriverExtra (each a WORD, sum up to 0x1FFFE)
// and unconditionally Marshal::Copy'd that many bytes from the spooler-allocated
// buffer.
//
DeviceMode::
DeviceMode(
    void* devModeUnmanaged,
    Int32 maxBytes
    ) : data(nullptr)
{
    if (devModeUnmanaged == nullptr)
    {
        return;
    }

    // When the kill switch is set, revert to the legacy behavior that trusts
    // the DEVMODE's self-reported length fields unconditionally.
    if (PrintingSwitches::IsDevModeValidationDisabled())
    {
        DEVMODEW* devmode = reinterpret_cast<DEVMODEW*>(devModeUnmanaged);
        size = devmode->dmSize + devmode->dmDriverExtra;
        data = gcnew array<Byte>(size);
        Marshal::Copy((IntPtr)devmode, data, 0 , size);
        return;
    }

    if (maxBytes <= 0)
    {
        return;
    }

    // We must be able to read dmSize and dmDriverExtra before we can trust them.
    const Int32 minHeaderBytes =
        static_cast<Int32>(offsetof(DEVMODEW, dmDriverExtra) + sizeof(WORD));

    if (maxBytes < minHeaderBytes)
    {
        return;
    }

    DEVMODEW* devmode = reinterpret_cast<DEVMODEW*>(devModeUnmanaged);

    UInt32 dmSize = devmode->dmSize;
    UInt32 dmDriverExtra = devmode->dmDriverExtra;

    // dmSize must at least cover the fixed portion through dmFields, otherwise
    // the blob is structurally malformed.
    if (dmSize < static_cast<UInt32>(offsetof(DEVMODEW, dmFields)))
    {
        return;
    }

    // Clamp against the trusted upper bound supplied by the caller. The sum is
    // computed in 64-bit to defend against any future WORD-overflow changes.
    UInt64 declared = static_cast<UInt64>(dmSize) + static_cast<UInt64>(dmDriverExtra);

    if (declared == 0 || declared > static_cast<UInt64>(maxBytes))
    {
        return;
    }

    size = static_cast<UInt32>(declared);
    data = gcnew array<Byte>(static_cast<Int32>(size));
    Marshal::Copy(static_cast<IntPtr>(devmode), data, 0, static_cast<Int32>(size));
}

Int32
DeviceMode::ComputeBytesAvailable(
    SafeMemoryHandle^   buffer,
    void*               subPtr
    )
{
    if (buffer == nullptr || buffer->IsInvalid || subPtr == nullptr)
    {
        return 0;
    }

    Int32 bufferSize = buffer->Size;
    if (bufferSize <= 0)
    {
        // Wrap()-ed handle: we don't know the underlying allocation length, so
        // refuse to compute a bound. The DeviceMode ctor will treat 0 as "skip".
        return 0;
    }

    Boolean mustRelease = false;
    buffer->DangerousAddRef(mustRelease);
    try
    {
        IntPtr bufferBase = buffer->DangerousGetHandle();
        if (bufferBase == IntPtr::Zero)
        {
            return 0;
        }

        unsigned char* base = reinterpret_cast<unsigned char*>(bufferBase.ToPointer());
        unsigned char* endExcl = base + bufferSize;
        unsigned char* sub = reinterpret_cast<unsigned char*>(subPtr);

        if (sub < base || sub >= endExcl)
        {
            // Sub-pointer lies outside the trusted buffer (could happen if a
            // hostile server emits a pDevMode that doesn't reference its own
            // returned buffer).
            return 0;
        }

        return static_cast<Int32>(endExcl - sub);
    }
    finally
    {
        if (mustRelease)
        {
            buffer->DangerousRelease();
        }
    }
}

array<Byte>^
DeviceMode::Data::
get(
    void
    )
{
    return data;
}
