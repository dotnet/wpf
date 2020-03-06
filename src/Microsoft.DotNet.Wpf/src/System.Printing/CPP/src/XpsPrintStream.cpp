// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#include "win32inc.hpp"
#include "Shlwapi.h"

#include <XpsPrintStream.hpp>
#include <InternalPrintSystemException.hpp>
#include <PrintSystemUtil.hpp>

using namespace System;
using namespace System::IO;
using namespace System::Printing;
using namespace MS::Internal::PrintWin32Thunk;
using namespace System::Runtime::InteropServices;

XpsPrintStream::
XpsPrintStream(
    void /* IStream */ *IPrintStream,
    bool canRead,
    bool canWrite
) :
    _innerStream((IStream*)IPrintStream),
    _canRead(canRead),
    _canWrite(canWrite)
{
    if (_innerStream == NULL)
    {
        throw gcnew ArgumentNullException("printStream");
    }
}

XpsPrintStream::
~XpsPrintStream(
) 
{
    this->!XpsPrintStream();
}

XpsPrintStream::
!XpsPrintStream(
) 
{
    if (_innerStream != NULL)
    {
        _innerStream->Release();
        _innerStream = NULL;
    }
}

Boolean
XpsPrintStream::CanRead::
get(
    void
)
{
    return _canRead;
}

Boolean
XpsPrintStream::CanWrite::
get(
    void
)
{
    return _canWrite;
}

Boolean
XpsPrintStream::CanSeek::
get(
    void
)
{
    return true;
}

Boolean
XpsPrintStream::CanTimeout::
get(
    void
)
{
    return false;
}

Int64
XpsPrintStream::Length::
get(
    void
)
{
    return _position;
}

Int64
XpsPrintStream::Position::
get(
    void
)
{
    return _position;
}

void
XpsPrintStream::Position::
set(
    Int64 value
)
{
    throw gcnew NotSupportedException();
}

void 
XpsPrintStream::
Flush (
    void
) 
{
}

Int32
XpsPrintStream::
Read (
    array<Byte> ^ buffer,
    Int32 offset,
    Int32 count
)
{
    if(buffer == nullptr)
    {
        throw gcnew ArgumentNullException("buffer");
    }

    if(offset < 0 || offset >= buffer->Length)
    {
        throw gcnew ArgumentNullException("offset");
    }

    if(count < 0 || (count + offset) > buffer->Length)
    {
        throw gcnew ArgumentNullException("count");
    }
    
    ULONG bytesRead = 0;
    
    pin_ptr<byte> pinnedBuffer = &buffer[offset];
    _innerStream->Read(pinnedBuffer, count, &bytesRead);
    
    _position += bytesRead;

    return bytesRead;
}

void
XpsPrintStream::
Write (
    array<Byte> ^ buffer,
    Int32 offset,
    Int32 count
)
{
    if(buffer == nullptr)
    {
        throw gcnew ArgumentNullException("buffer");
    }

    if(offset < 0 || offset >= buffer->Length)
    {
        throw gcnew ArgumentNullException("offset");
    }

    if(count < 0 || (count + offset) > buffer->Length)
    {
        throw gcnew ArgumentNullException("count");
    }
    
    pin_ptr<byte> pinnedBuffer = &buffer[offset];
    
    ULONG totalBytesWritten = 0;    
    ULONG uCount = static_cast<ULONG>(count);
    
    while(totalBytesWritten < uCount)
    {
        ULONG bytesToWrite = uCount - totalBytesWritten;
        ULONG bytesWritten = 0;

        _innerStream->Write(pinnedBuffer, bytesToWrite, &bytesWritten);
        
        assert(bytesWritten <= bytesToWrite);

        ULONG nextTotalBytesWritten = totalBytesWritten + bytesWritten;
        pinnedBuffer += bytesWritten;
        totalBytesWritten = nextTotalBytesWritten;
    }

    _position += totalBytesWritten;
}

ComTypes::IStream^
XpsPrintStream::
GetManagedIStream()
{
    return (ComTypes::IStream^) Marshal::GetTypedObjectForIUnknown(IntPtr(_innerStream), ComTypes::IStream::typeid);
}

Int64
XpsPrintStream::
Seek (
    Int64 offset,
    SeekOrigin origin
)
{
    LARGE_INTEGER pos;
    ULARGE_INTEGER newPos;

    pos.QuadPart = offset;

    _innerStream->Seek(pos, (int)origin, &newPos);

    _position = newPos.QuadPart;
    return _position;
}

void
XpsPrintStream::
SetLength (
    Int64 value
) 
{
    throw gcnew NotSupportedException();
}

XpsPrintStream^
XpsPrintStream::
CreateXpsPrintStream()
{
    IStream * innerStream = NULL;
    XpsPrintStream^ StreamWrapper = nullptr;
    LARGE_INTEGER pos = { 0 };
    
    CreateStreamOnHGlobal(NULL, TRUE, &innerStream);
    innerStream->Seek(pos, STREAM_SEEK_SET, NULL);
    StreamWrapper = gcnew XpsPrintStream(innerStream, false, true);
    
    return StreamWrapper;
}

