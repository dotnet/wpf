// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        Provides a managed stream that allows writing to the Spl file consumed by the Print
        Spooler process.
--*/

#ifndef __PREMIUMPRINTSTREAM_HPP__
#define __PREMIUMPRINTSTREAM_HPP__

namespace System
{
namespace Printing
{
    public ref class PrintQueueStream :
    public Stream
    {
        public:

        PrintQueueStream(
            PrintQueue^     printQueue,
            String^         printJobName,
            Boolean         commitDataOnClose,
            PrintTicket^    printTicket
            );

        PrintQueueStream(
            PrintQueue^     printQueue,
            String^         printJobName,
            Boolean         commitDataOnClose
            );

        PrintQueueStream(
            PrintQueue^     printQueue,
            String^         printJobName
            );

        ~PrintQueueStream(
            );

        property
        Boolean
        CanRead
        {
            virtual Boolean get() override;
        }

        property
        Boolean
        CanWrite
        {
            virtual Boolean get() override;
        }

        property
        Boolean
        CanSeek
        {
            virtual Boolean get() override;
        }

        property
        virtual
        Int64
        Length
        {
            virtual Int64 get() override;
        }

        property
        Int64
        virtual
        Position
        {
            virtual Int64 get() override;

            virtual void set(Int64     value) override;
        }

        property
        Int32
        JobIdentifier
        {
            Int32 get();
        }

        virtual
        IAsyncResult^
        BeginWrite(
            array<Byte>^    buffer,
            Int32           offset,
            Int32           count,
            AsyncCallback^  callback,
            Object^         state
            ) override;

        virtual
        void
        EndWrite(
            IAsyncResult^   asyncResult
            ) override;

        virtual
        int
        Read(
            array<Byte>^    buffer,
            int             offset,
            int             count
            ) override;

        virtual
        void
        Write(
            array<Byte>^  buffer,
            int           offset,
            int           count
            ) override;

        virtual
        void
        Flush(
            ) override;

        virtual
        Int64
        Seek(
            Int64           offset,
            SeekOrigin      origin
            ) override;

        virtual
        void
        Close(
            void
            ) override;

        virtual
        void
        SetLength(
            Int64   value
            ) override;


        void
        HandlePackagingProgressEvent(
            Object^                                                       sender,
            System::Windows::Xps::Packaging::PackagingProgressEventArgs^  e
            );

        internal:
        void
         Abort(
            void
            );

        PrintQueueStream(
            PrintQueue^     printQueue,
            String^         printJobName,
            Boolean         commitDataOnClose,
            PrintTicket^    printTicket,
            Boolean         fastCopy
            );

        protected:

        !PrintQueueStream(
            );

        private:

        void
        InitializePrintStream(
            PrintTicket^ printTicket
            );

        void
        InitializePrintStream(
            PrintTicket^ printTicket,
            Boolean      fastCopy
            );

        void
        CommitDataToPrinter(
            void
            );

        void
        PrintQueueStream::
        AbortOrCancel(
            bool abort
            );

        static
        Exception^
        PrintQueueStream::CreatePrintingCanceledException(
            int hresult,
            String^ messageId
            );

        PrintQueue^     printQueue;

        Int32           jobIdentifier;

        /// <summary>
        /// Number of bytes which need to be commited to Print Spooler.
        /// These are the sum of bytes that are being written to the stream for a single page
        /// </summary>
        Int64           bytesToCommit;

        /// <summary>
        /// Keeps track of number of bytes which were commited to Print Spooler.
        /// Represent the position in the stream up to which the data was commited to Spooler.
        /// </summary>
        Int64           bytesPreviouslyCommited;
        /// <summary>
        /// Controls the way the data is commited to Spooler. This is hardcoded so tha data
        /// is always commited on a per page base. WE are keeping this for further extensibility.
        /// </summary>
        Boolean         commitStreamDataOnClose;
        /// <summary>
        /// The name of the print job for which this stream was created.
        /// </summary>
        String^         printJobName;

        /// <summary>
        /// Object closed
        /// </summary>
        Boolean         streamClosed;

        /// <summary>
        /// Stream aborted
        /// </summary>
        Boolean			streamAborted;

        MS::Internal::PrintWin32Thunk::PrinterThunkHandlerBase^    printerThunkHandler;

        PrintSystemDispatcherObject^    accessVerifier;

        Boolean                             isFinalizer;
    };

    private ref class WritePrinterAsyncResult :
    public IAsyncResult
    {
        public:

        WritePrinterAsyncResult(
            Stream^             stream,
            array<Byte>^        array,
            Int32               offset,
            Int32               numBytes,
            AsyncCallback^      userCallBack,
            Object^             stateObject
            );

        property
        Object^
        AsyncState
        {
            virtual Object^ get();
        }

        property
        System::Threading::WaitHandle^
        AsyncWaitHandle
        {
            virtual System::Threading::WaitHandle^ get();
        }

        property
        AsyncCallback^
        AsyncCallBack
        {
            AsyncCallback^ get();
        }

        property
        bool
        CompletedSynchronously
        {
            virtual bool get();
        }

        property
        bool
        IsCompleted
        {
            virtual bool get();
            void set(bool);
        }

        internal:

        void
        AsyncWrite(
            void
            );

        private:

        Stream^                             printStream;
        Boolean                             isCompleted;
        System::Threading::AutoResetEvent^  writeCompletedEvent;
        AsyncCallback^                      userCallBack;
        Object^                             userObject;
        array<Byte>^                        dataArray;
        Int32                               dataOffset;
        Int32                               numberOfBytes;
    };


}
}

#endif // __PREMIUMPRINTSTREAM_HPP__

