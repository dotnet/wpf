// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __XPSPRINTSTREAM_HPP__
#define __XPSPRINTSTREAM_HPP__
/*++

    Abstract:

        Managed wrapper for IStream interface.

--*/

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System;
    using namespace System::IO;
    using namespace System::Printing;
    using namespace System::Runtime::InteropServices;
    using namespace System::Security;
    using namespace System::Runtime::InteropServices;
    
    private ref class XpsPrintStream : public Stream
    {
    
    private:
        XpsPrintStream(
            void /* IStream */ *IPrintStream,
            Boolean canRead,
            Boolean canWrite
            );

    public:
        ~XpsPrintStream();

        !XpsPrintStream();

        property
            Boolean
            CanRead
        {
            Boolean virtual get() override;
        }

        property
            Boolean
            CanWrite
        {
            Boolean virtual get() override;
        }

        property
            Boolean
            CanSeek
        {
            Boolean virtual get() override;
        }

        property
            Boolean
            CanTimeout
        {
            Boolean virtual get() override;
        }

        property
            Int64
            Length
        {
            Int64 virtual get() override;
        }

        property
            Int64
            Position
        {
            Int64 virtual get() override;
            void virtual set(Int64 value) override;
        }

        virtual
            void
            Flush(
            void
            ) override;

        virtual
            Int32
            Read(
            array<Byte> ^ buffer,
            Int32 offset,
            Int32 count
            ) override;

        virtual
            void
            Write(
            array<Byte> ^ buffer,
            Int32 offset,
            Int32 count
            ) override;

        virtual
            Int64
            Seek(
            Int64 offset,
            SeekOrigin origin
            ) override;

        virtual
            void
            SetLength(
            Int64 value
            ) override;

        static
        XpsPrintStream^
        CreateXpsPrintStream();

        ComTypes::IStream^
        GetManagedIStream();

        private:

        IStream* _innerStream;

        Boolean _canRead;
        Boolean _canWrite;
        Int64 _position;
    };
}
}
}

#endif
