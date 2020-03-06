// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __XPSDOCUMENTWRITER_HPP__
#define __XPSDOCUMENTWRITER_HPP__
/*++

    Abstract:

        This object is instantiated against an XPSEmitter object. It is a public object to be used
        to serialize Visuals to Print Subsystem objeects.
--*/
using namespace System::Runtime::Serialization;
using namespace System::Windows::Xps::Packaging;
using namespace System::Windows::Xps::Serialization;
using namespace System::IO;
using namespace System::IO::Packaging;
using namespace System::Windows::Documents::Serialization;

namespace System
{
namespace Windows
{
namespace Xps
{

    [AttributeUsage(
        AttributeTargets::Class |
        AttributeTargets::Property |
        AttributeTargets::Method |
        AttributeTargets::Struct |
        AttributeTargets::Enum |
        AttributeTargets::Interface |
        AttributeTargets::Delegate |
        AttributeTargets::Constructor,
        AllowMultiple = false,
        Inherited = true)
    ]

    private ref class FriendAccessAllowedAttribute sealed : Attribute
    {
    };

    ref class VisualsToXpsDocument;

    public enum class XpsDocumentNotificationLevel
    {
        None                        = 0,
        ReceiveNotificationEnabled  = 1,
        ReceiveNotificationDisabled = 2
    };


    public ref class XpsDocumentWriter: public SerializerWriter
    {

    internal:

        /// <summary>
        /// Instantiates a <c>XpsDocumentWriter</c> against an object implementing <c>XPSEmitter</c>.
        /// </summary>
        /// <param name="serializeReach"><c>XPSEmitter</c> object that will serialize and write the document objects.</param>
        [FriendAccessAllowed]
        XpsDocumentWriter(
            PrintQueue^    printQueue
            );

        /// <summary>
        /// Instantiates a <c>XpsDocumentWriter</c> against an object implementing <c>XPSEmitter</c>.
        /// </summary>
        /// <param name="serializeReach"><c>XPSEmitter</c> object that will serialize and write the document objects.</param>
        [FriendAccessAllowed]
        XpsDocumentWriter(
            XpsDocument^    document
            );

    internal:

        /// <summary>
        /// Instantiates a <c>XpsDocumentWriter</c> against an object implementing <c>XPSEmitter</c>.
        /// </summary>
        /// <param name="serializeReach"><c>XPSEmitter</c> object that will serialize and write the document objects.</param>
        /// <param name="bogus"><c>Bogus</c> Bogus param to have a second internal constructor.</param>
        XpsDocumentWriter(
            PrintQueue^     printQueue,
            Object^         bogus
            );

        /// <summary>
        /// Writes a <c>FixedDocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocumentSequence"><c>FixedDocumentSequence</c> object to be written.</param>
        /// <param name="printJobIdentifier">Print Job identifier.</param>
        void
        BeginPrintFixedDocumentSequence(
            System::Windows::Documents::FixedDocumentSequence^      fixedDocumentSequence,
            Int32&                                                  printJobIdentifier
            );

        /// <summary>
        /// Writes a <c>FixedDocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocumentSequence"><c>FixedDocumentSequence</c> object to be written.</param>
        /// <param name="printTicket">Print ticket.</param>
        /// <param name="printJobIdentifier">Print Job identifier.</param>
        void
        BeginPrintFixedDocumentSequence(
            System::Windows::Documents::FixedDocumentSequence^      fixedDocumentSequence,
            PrintTicket^                                            printTicket,
            Int32&                                                  printJobIdentifier
            );

        /// <summary>
        /// Dispose objects and deletes the print job.
        /// </summary>
        void
        EndPrintFixedDocumentSequence(
            void
            );

    public:

        /// <summary>
        /// Writes an <c>XpsDocument</c> to the destination object.
        /// </summary>
        /// <param name="documentPath"><c>XpsDocument</c> object to be written.</param>
        void
        Write(
            String^      documentPath
            );

        /// <summary>
        /// Writes an <c>XpsDocument</c> to the destination object.
        /// </summary>
        /// <param name="documentPath"><c>XpsDocument</c> object to be written.</param>
        /// <param name="notificationLevel"><c>XpsDocumentNotificationLevel</C>
        /// granularity of notification. if ReceiveNotificationEnabled is set, then the document would be re-serialized and extented
        /// XPS content can't be preserved.
        /// </param>
        void
        Write(
            String^                         documentPath,
            XpsDocumentNotificationLevel    notificationLevel
            );

        /// <summary>
        /// Writes an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::DocumentPaginator^      documentPaginator
            ) override;

        /// <aummary>
        /// Writes an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        Write(
            System::Windows::Documents::DocumentPaginator^      documentPaginator,
            PrintTicket^                                        printTicket
            ) override;

        /// <summary>
        /// Writes a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Media::Visual^     visual
            ) override;

        /// <summary>
        /// Writes a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        Write(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket
            ) override;

        /// <summary>
        /// Writes a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocumentSequence^      fixedDocumentSequence
            ) override;

        /// <summary>
        /// Writes a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocumentSequence^      fixedDocumentSequence,
            PrintTicket^                                            printTicket
            ) override;

        /// <summary>
        /// Writes a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocument^      fixedDocument
            ) override;

        /// <summary>
        /// Writes a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocument^      fixedDocument,
            PrintTicket^                                    printTicket
            ) override;

        /// <summary>
        /// Writes a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedPage^          fixedPage
            ) override;

        /// Writes a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedPage^          fixedPage,
            PrintTicket^                                    printTicket
            ) override;


        /// <summary>
        /// Asynchronously Writes an <c>XpsDocument</c> to the destination object.
        /// </summary>
        /// <param name="documentPath"><c>XpsDocument</c> object to be written.</param>
        void
        WriteAsync(
            String^      documentPath
            );

        /// <summary>
        /// Asynchronously Writes an <c>XpsDocument</c> to the destination object.
        /// </summary>
        /// <param name="documentPath"><c>XpsDocument</c> object to be written.</param>
        /// <param name="notificationLevel"><c>XpsDocumentNotificationLevel</C>
        /// granularity of notification. if ReceiveNotificationEnabled is set, then the document would be re-serialized and extented
        /// XPS content can't be preserved.
        /// </param>
        void
        WriteAsync(
            String^                         documentPath,
            XpsDocumentNotificationLevel    notificationLevel
            );

        /// <summary>
        /// Asynchronously write an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::DocumentPaginator^      documentPaginator
            ) override;

        /// <summary>
        /// Asynchronously write an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::DocumentPaginator^      documentPaginator,
            PrintTicket^                                        printTicket
            ) override;

        /// <summary>
        /// Asynchronously write an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::DocumentPaginator^      documentPaginator,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write an <c>DocumentPaginator</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::DocumentPaginator^      documentPaginator,
            PrintTicket^                                        printTicket,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual
            ) override;

        /// <summary>
        /// Asynchronously write a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket
            ) override;

        /// <summary>
        /// Asynchronously write a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            Object^                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>Visual</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket,
            Object^                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocumentSequence^  fixedDocumentSequence
            ) override;

        /// <summary>
        /// Asynchronously write a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocumentSequence^  fixedDocumentSequence,
            PrintTicket^                                        printTicket
            ) override;

        /// <summary>
        /// Asynchronously write a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocumentSequence^  fixedDocumentSequence,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>DocumentSequence</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocumentSequence^  fixedDocumentSequence,
            PrintTicket^                                        printTicket,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocument^          fixedDocument
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocument^          fixedDocument,
            PrintTicket^                                        printTicket
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocument^          fixedDocument,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedDocument</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedDocument^          fixedDocument,
            PrintTicket^                                        printTicket,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedPage^              fixedPage
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedPage^              fixedPage,
            PrintTicket^                                        printTicket
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedPage^              fixedPage,
            Object^                                             userSuppliedState
            ) override;

        /// <summary>
        /// Asynchronously write a <c>FixedPage</c> to the <c>XPSEmitter</c> object.
        /// </summary>
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="printTicket"><c>PrintTicket</c> to apply to object.</param>
        /// <param name="userSuppliedState">User supplied object.</param>
        virtual
        void
        WriteAsync(
            System::Windows::Documents::FixedPage^              fixedPage,
            PrintTicket^                                        printTicket,
            Object^                                             userSuppliedState
            ) override;

        virtual
        void
        CancelAsync(
            ) override;

        /// <summary>
        /// Creates and returns the <c>VisualsToXpsDocument</c> visuals collater for batch writing.
        /// </summary>
        /// <returns><c>VisualsToXpsDocument</c></returns>
        virtual
        SerializerWriterCollator^
        CreateVisualsCollator(
            ) override;
        /*++

            Function Name:
                CreateVisualsCollator

            Description:
                Creates and returns a VisualsToXPSDocument visuals collater for batch writing.

            Parameters:

                documentSequencePrintTicket     -   PrintTicket to use on the FixedDocumentSequence
                documentPrintTicket             -   PrintTicket to use on the FixedDocument

            Return Value:

                VisualsToXpsDocument

        --*/
        virtual
        SerializerWriterCollator^
        XpsDocumentWriter::
        CreateVisualsCollator(
            PrintTicket^    documentSequencePrintTicket OPTIONAL,
            PrintTicket^    documentPrintTicket         OPTIONAL
            ) override;

        event WritingPrintTicketRequiredEventHandler^ WritingPrintTicketRequired
        {
            public:

            virtual
            void
            add(
                WritingPrintTicketRequiredEventHandler^   handler
                ) override
            {
                _WritingPrintTicketRequired+=handler;
            }

            virtual
            void
            remove(
                WritingPrintTicketRequiredEventHandler^    handler
                ) override
            {
                _WritingPrintTicketRequired-=handler;
            }

            virtual
            void
            raise(
                Object^                                sender,
                 WritingPrintTicketRequiredEventArgs^   e
                 )
            {
                _WritingPrintTicketRequired(sender,e);
            }


        }

        event WritingProgressChangedEventHandler^     WritingProgressChanged
         {
            public:

            virtual
            void
            add(
                WritingProgressChangedEventHandler^   handler
                ) override
            {
                _WritingProgressChanged+=handler;
            }

            virtual
            void
            remove(
                WritingProgressChangedEventHandler^    handler
                ) override
            {
                _WritingProgressChanged-=handler;
             }

            virtual
            void
            raise(
                Object^                                sender,
                 WritingProgressChangedEventArgs^   e
                 )
            {
                _WritingProgressChanged(sender,e);
            }
        }

        event WritingCompletedEventHandler^           WritingCompleted
        {
            public:

            virtual
            void
            add(
                WritingCompletedEventHandler^   handler
                ) override
            {
                _WritingCompleted+=handler;
            }

            virtual
            void
            remove(
                WritingCompletedEventHandler^    handler
                ) override
            {
                _WritingCompleted-=handler;
             }

            virtual
            void
            raise(
                Object^                                sender,
                 WritingCompletedEventArgs^   e
                 )
            {
                _WritingCompleted(sender,e);
            }
        }

        event WritingCancelledEventHandler^           WritingCancelled
        {
            public:

            virtual
            void
            add(
                WritingCancelledEventHandler^   handler
                ) override
            {
                _WritingCancelled+=handler;
                _writingCancelledEventHandlersCount++;
            }

            virtual
            void
            remove(
                WritingCancelledEventHandler^    handler
                ) override
            {
                if(_writingCancelledEventHandlersCount>0)
                {
                    _WritingCancelled-=handler;
                    _writingCancelledEventHandlersCount--;
                }
                else
                {
                    throw gcnew InvalidOperationException();
                }
            }


            virtual
            void
            raise(
                Object^                     sender,
                WritingCancelledEventArgs^  args
                )
            {
                _WritingCancelled(sender,args);
            }
        }


    internal:

        event WritingPrintTicketRequiredEventHandler^ _WritingPrintTicketRequired
        {
            internal:

            void
            add(
                WritingPrintTicketRequiredEventHandler^   handler
                )
            {
                m_WritingPrintTicketRequired += handler;
            }

            void
            remove(
                WritingPrintTicketRequiredEventHandler^    handler
                )
            {
                m_WritingPrintTicketRequired -= handler;
            }

            void
            raise(
                Object^                                sender,
                 WritingPrintTicketRequiredEventArgs^   e
                 )
            {
                WritingPrintTicketRequiredEventHandler^ handler = m_WritingPrintTicketRequired;
                if(handler != nullptr)
                {
                    handler(sender,e);
                }
            }
        }

        event WritingProgressChangedEventHandler^ _WritingProgressChanged;
        event WritingCompletedEventHandler^       _WritingCompleted;
        event WritingCancelledEventHandler^ _WritingCancelled;

    internal:
        void
        ForwardUserPrintTicket(
            Object^                                                                               sender,
            System::Windows::Xps::Serialization::XpsSerializationPrintTicketRequiredEventArgs^    args
            );

        void
        ForwardWriteCompletedEvent(
            Object^                                                                   sender,
            System::Windows::Xps::Serialization::XpsSerializationCompletedEventArgs^  args
            );

        void
        ForwardProgressChangedEvent(
            Object^                                                                           sender,
            System::Windows::Xps::Serialization::XpsSerializationProgressChangedEventArgs^    args
            );

        WritingProgressChangeLevel
        TranslateProgressChangeLevel(
            System::
            Windows::
            Xps::Serialization::XpsWritingProgressChangeLevel xpsChangeLevel
            );

        void
        CloneSourcePrintTicket(
            Object^                                                                               sender,
            System::Windows::Xps::Serialization::XpsSerializationPrintTicketRequiredEventArgs^    args
            );

        void
        EndBatchMode(
            void
            );

        void
        SetPrintTicketEventHandler(
            System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
            XpsSerializationPrintTicketRequiredEventHandler^                  eventHandler
            );

        void
        SetCompletionEventHandler(
            System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
            Object^                                                           userState
            );

        void
        SetProgressChangedEventHandler(
            System::Windows::Xps::Serialization::PackageSerializationManager^ manager,
            Object^                                                           userState
            );

        property
        PrintTicket^
            CurrentUserPrintTicket
        {
            void set(PrintTicket^ userPrintTicket);
        }

        property
        System::Windows::Xps::Serialization::PrintTicketLevel
        CurrentWriteLevel
        {
             void set(System::Windows::Xps::Serialization::PrintTicketLevel writeLevel);
        }

        void
        OnWritingPrintTicketRequired(
            Object^                                sender,
            WritingPrintTicketRequiredEventArgs^   args
            );

        bool
        OnWritingCanceled(
            Object^     sender,
            Exception^  exception
            );


        private:

        void
        InitializeSequences(
            void
            );

        bool
        BeginWrite(
            bool                batchMode,
            bool                asyncMode,
            bool                setPrintTicketHandler,
            PrintTicket^        printTicket,
            System::Windows::
            Xps::
            Serialization::
            PrintTicketLevel    printTicketLevel,
            bool                printJobIdentifierSet
            );

        void
        EndWrite(
            bool disposeManager
            );

        void
        EndWrite(
            bool disposeManager,
            bool abort
            );

        void
        SaveAsXaml(
            Object^     serializedObject,
            bool        isSync
            );

        bool
        MxdwConversionRequired(
            PrintQueue^  printQueue
            );

        String^
        MxdwInitializeOptimizationConversion(
            PrintQueue^ printQueue
            );

        void
        CreateXPSDocument(
            String^     documentName
            );

        void
        VerifyAccess(
            void
            );


        private:

        enum class DocumentWriterState
        {
            kRegularMode,
            kBatchMode,
            kDone,
            kCancelled
        };

        PrintQueue^                         destinationPrintQueue;

        XpsDocument^                        destinationDocument;

        DocumentWriterState                 currentState;

        PrintTicket^                        currentUserPrintTicket;

        Object^                             _currentUserState;
        ArrayList^                          _printTicketSequences;
        ArrayList^                          _writingProgressSequences;

        MXDWSerializationManager^           _mxdwManager;

        Package^                            _mxdwPackage;
        Boolean                             _isDocumentCloned;

        XpsDocument^                        _sourceXpsDocument;

        IXpsFixedDocumentSequenceReader^    _sourceXpsFixedDocumentSequenceReader;

        Package^                            _sourcePackage;
        Int32                               _writingCancelledEventHandlersCount;
        PrintSystemDispatcherObject^    accessVerifier;

        System::Windows::Xps::Serialization::PrintTicketLevel              currentWriteLevel;

        System::Windows::Xps::Serialization::PackageSerializationManager^  _manager;

        WritingPrintTicketRequiredEventHandler^ m_WritingPrintTicketRequired;
    };


    public ref class VisualsToXpsDocument:SerializerWriterCollator
    {

    internal:


        VisualsToXpsDocument(
            XpsDocumentWriter^  writer,
            PrintQueue^         printQueue
            );

        VisualsToXpsDocument(
            XpsDocumentWriter^  writer,
            XpsDocument^        document
            );

        VisualsToXpsDocument(
            XpsDocumentWriter^  writer,
            PrintQueue^         printQueue,
            PrintTicket^        documentSequencePrintTicket,
            PrintTicket^        documentPrintTicket
            );

        VisualsToXpsDocument(
            XpsDocumentWriter^  writer,
            XpsDocument^        document,
            PrintTicket^        documentSequencePrintTicket,
            PrintTicket^        documentPrintTicket
            );

    public:

        virtual
        void
        BeginBatchWrite(
            ) override;

        virtual
        void
        EndBatchWrite(
            ) override;

        virtual
        void
        Write(
            System::Windows::Media::Visual^     visual
            ) override;

        virtual
        void
        Write(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket
            ) override;

        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual
            ) override;

        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket
            ) override;

        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            Object^                             userSuppliedState
            ) override;

        virtual
        void
        WriteAsync(
            System::Windows::Media::Visual^     visual,
            PrintTicket^                        printTicket,
            Object^                             userSuppliedState
            ) override;

        virtual
        void
        CancelAsync(
            ) override;

        virtual
        void
        Cancel(
            void
            ) override;

    internal:

    void
    SetPrintTicketEventHandler(
        System::Windows::Xps::Serialization::PackageSerializationManager^ manager
        );

    void
    ForwardUserPrintTicket(
        Object^                                                                               sender,
        System::Windows::Xps::Serialization::XpsSerializationPrintTicketRequiredEventArgs^    args
        );

    private:

        bool
        WriteVisual(
            bool                asyncMode,
            PrintTicket^        printTicket,
            System::Windows::
            Xps::
            Serialization::
            PrintTicketLevel    printTicketLevel,
            System::
            Windows::
            Media::
            Visual^             visual
            );

        bool
        MxdwConversionRequired(
            PrintQueue^  printQueue
            );

        String^
        MxdwInitializeOptimizationConversion(
            PrintQueue^ printQueue
            );

        void
        CreateXPSDocument(
            String^     documentName
            );

        void
        InitializeSequences(
            void
            );

        void
        VerifyAccess(
            void
            );


        enum class VisualsCollaterState
        {
            kUninit,
            kSync,
            kAsync,
            kDone,
            kCancelled
        };

        Object^                     _currentUserState;

        PrintTicket^                _documentSequencePrintTicket;

        PrintTicket^                _documentPrintTicket;

        XpsDocumentWriter^          parentWriter;
        VisualsCollaterState        currentState;
        PrintQueue^                 destinationPrintQueue;

        XpsDocument^                destinationDocument;
        bool                        isPrintTicketEventHandlerSet;
        bool                        isCompletionEventHandlerSet;
        bool                        isProgressChangedEventHandlerSet;

        MXDWSerializationManager^   _mxdwManager;

        Package^                    _mxdwPackage;
        Hashtable^                  _printTicketsTable;
        ArrayList^                  _printTicketSequences;
        Int32                       _numberOfVisualsCollated;
        PrintSystemDispatcherObject^    accessVerifier;

        System::Windows::Xps::Serialization::PackageSerializationManager^  _manager;
    };


    /// <summary>
    /// This class is used to throw exceptions from the XpsDocumentWriter and related classes.
    /// </summary>
    [System::Serializable]
    public ref class XpsWriterException : public Exception
    {
    public:
        /// <summary>
        ///
        /// </summary>
        XpsWriterException(
            );

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        XpsWriterException(
            String^     message
            );

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        XpsWriterException(
            String^     message,
            Exception^  innerException
            );

	protected:
		XpsWriterException(
		    SerializationInfo   ^info,
		    StreamingContext    context
		    );

    internal:

        static void
        ThrowException(
            String^ message
            );
    };
}
}
}

#endif // __XPSDOCUMENTWRITER_HPP__
