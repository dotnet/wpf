// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#include "win32inc.hpp"

#include <XpsPrintJobStream.hpp>
#include <InternalPrintSystemException.hpp>
#include <PrintSystemUtil.hpp>

using namespace System;
using namespace System::IO;
using namespace System::Printing;
using namespace MS::Internal::PrintWin32Thunk;

XpsPrintJobStream::
XpsPrintJobStream(
    void /* IXpsPrintJobStream */ *printJobStream, // ilasm does not recognise IXpsPrintJobStream
    System::Threading::ManualResetEvent^ hCompletedEvent,
    bool canRead,
    bool canWrite
) :
    inner((IXpsPrintJobStream*)printJobStream),
    hCompletedEvent(hCompletedEvent),
    canRead(canRead),
    canWrite(canWrite)
{
    if(NULL == inner)
    {
        throw gcnew ArgumentNullException("printJobStream");
    }
}

XpsPrintJobStream::
~XpsPrintJobStream (
) 
{
    if(inner != NULL)
    {
        inner->Close();
        DWORD timeout = GetCommitTimeoutMilliseconds();
        if(WaitForJobCompletion(timeout))
        {
            inner->Release();
            inner = NULL;

            delete hCompletedEvent;
            hCompletedEvent = nullptr;
        }
    }
}

XpsPrintJobStream::
!XpsPrintJobStream (
) 
{
    if(inner != NULL)
    {
        inner->Close();
        inner = NULL;
    }
}

Boolean
XpsPrintJobStream::CanRead::
get(
    void
)
{
    return canRead;
}

Boolean
XpsPrintJobStream::CanWrite::
get(
    void
)
{
    return canWrite;
}

Boolean
XpsPrintJobStream::CanSeek::
get(
    void
)
{
    return false;
}

Boolean
XpsPrintJobStream::CanTimeout::
get(
    void
)
{
    return false;
}

Int64
XpsPrintJobStream::Length::
get(
    void
)
{
    return position;
}

Int64
XpsPrintJobStream::Position::
get(
    void
)
{
    return position;
}

void
XpsPrintJobStream::Position::
set(
    Int64 value
)
{
    throw gcnew NotSupportedException();
}

void 
XpsPrintJobStream::
Flush (
    void
) 
{
}

Int32
XpsPrintJobStream::
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
    inner->Read(pinnedBuffer, count, &bytesRead);
    
    position += bytesRead;

    return bytesRead;
}

void
XpsPrintJobStream::
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

        inner->Write(pinnedBuffer, bytesToWrite, &bytesWritten);        
        
        assert(bytesWritten <= bytesToWrite);

        ULONG nextTotalBytesWritten = totalBytesWritten + bytesWritten;
        pinnedBuffer += bytesWritten;
        totalBytesWritten = nextTotalBytesWritten;
    }

    position += totalBytesWritten;
}

Int64
XpsPrintJobStream::
Seek (
    Int64 offset,
    SeekOrigin origin
)
{
    throw gcnew NotSupportedException();
}

void
XpsPrintJobStream::
SetLength (
    Int64 value
) 
{
    throw gcnew NotSupportedException();
}

DWORD
XpsPrintJobStream::
GetCommitTimeoutMilliseconds (
    void
)
{
    DWORD result = ((DWORD)-1); // default to an infinite timeout 

    InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();
    System::Globalization::CultureInfo^ culture = System::Threading::Thread::CurrentThread->CurrentUICulture;
    String^ regKeyBasePath = manager->GetString("RegKeyBasePath", culture);
    String^ commitTimeOutRegValue = manager->GetString("XpsPrintJobStream.CommitTimeout_RegValue", culture);

    Object^ objValue = Microsoft::Win32::Registry::GetValue(regKeyBasePath, commitTimeOutRegValue, result);
    if (objValue != nullptr && dynamic_cast<Int32^>(objValue))
    {
        result = safe_cast<Int32>(objValue);
    }

    return result;
}

BOOL 
XpsPrintJobStream::
WaitForJobCompletion(
    DWORD waitTimeout
)
{
    BOOL result = FALSE;

    if(hCompletedEvent != nullptr)
    {        
        result = hCompletedEvent->WaitOne(waitTimeout);
    }
        
    return result;
}

