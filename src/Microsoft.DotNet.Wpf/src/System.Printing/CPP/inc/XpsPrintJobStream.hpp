// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#pragma once

#ifndef __XPSPRINTJOBSTREAM_HPP__
#define __XPSPRINTJOBSTREAM_HPP__
/*++
                                                                              

        Managed wrapper for IXpsPrintJobStream interface.

--*/

namespace MS
{
namespace Internal
{
namespace PrintWin32Thunk
{
    using namespace System;
    using namespace System::IO;
    using namespace System::Security;
    
    private ref class XpsPrintJobStream : public Stream
    {
        public:
            
        XpsPrintJobStream(
            void /* IXpsPrintJobStream */ *printJobStream, // ilasm does not recognise IXpsPrintJobStream
            System::Threading::ManualResetEvent^ hCompletedEvent,
            Boolean canRead,
            Boolean canWrite
        );
        
        ~XpsPrintJobStream();
        
        !XpsPrintJobStream();
        
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
        Flush (
            void
        ) override ;
        
        virtual
        Int32
        Read (
            array<Byte> ^ buffer,
            Int32 offset,
            Int32 count
        ) override;
        
        virtual
        void
        Write (
            array<Byte> ^ buffer,
            Int32 offset,
            Int32 count
        ) override;
        
        virtual
        Int64
        Seek (
            Int64 offset,
            SeekOrigin origin
        ) override;
        
        virtual
        void
        SetLength (
            Int64 value
        ) override;
        
        private:

        /// <Summary>
        /// Wrapper around WaitForSingleObjectEx that hides away its various return codes
        /// </Summary>
        BOOL 
        WaitForJobCompletion (
            DWORD waitTimeout
        );

        DWORD
        GetCommitTimeoutMilliseconds (
            void
        );

        IXpsPrintJobStream* inner;

        System::Threading::ManualResetEvent^ hCompletedEvent;
		
        Boolean canRead;
        Boolean canWrite;
        Int64 position;
    };
}
}
}

#endif
