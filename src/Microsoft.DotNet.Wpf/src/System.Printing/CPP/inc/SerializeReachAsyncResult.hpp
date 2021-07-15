// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __SERIALIZEREACHASYNCRESULT_HPP__
#define __SERIALIZEREACHASYNCRESULT_HPP__
/*++
                                                              
    Abstract:
        
        Helper class for ISerializeReach implementation classes Asynchronous Write operations.
                                                         
--*/

namespace System
{
namespace Printing
{

public ref class SerializeReachAsyncResult :
public IAsyncResult, public IDisposable
{
    public:

    /// <summary>
    /// Instantiates a <c>SerializeReachAsyncResult</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serializes and writes document objects.</param>
    /// <param name="callback"><c>AsyncCallback</c> called when write is complete.</param>
    /// <param name="state"><c>Object</c>User supplied object.</param>
    SerializeReachAsyncResult(
        ISerializeReach^    serializationDestination,
        AsyncCallback^      callback,
        Object^             state
        );

    ~SerializeReachAsyncResult(
        );

    /// <value>
    /// Serialization and Write destination property.
    /// </value> 
    property
    System::Printing::ISerializeReach^
    AsyncWriteDestination
    {
        System::Printing::ISerializeReach^ get();
    }

    /// <value>
    /// User supplied object property.
    /// </value> 
    property
    Object^
    AsyncState
    {
        Object^ get();
    }

    /// <value>
    /// Asynchronous Wait handle property.
    /// </value> 
    property
    System::Threading::WaitHandle^
    AsyncWaitHandle
    {
        System::Threading::WaitHandle^ get();
    }

    /// <value>
    /// User supplied callback property.
    /// </value> 
    property
    AsyncCallback^
    SerializeReachAsyncCallback
    {
        AsyncCallback^ get();
    }

    /// <value>
    /// Has call completed synchronously property.
    /// </value> 
    property
    bool
    CompletedSynchronously
    {
        bool get();
    }

    /// <value>
    /// Has call been completed property.
    /// </value> 
    property
    bool
    IsCompleted
    {
        bool get();
    }

    internal:

    void
    AsyncWrite(
        void
        );

    private:

    System::Printing::ISerializeReach^                  userSerializationDestination;
    Boolean                                             writeCompleted;
    System::Threading::AutoResetEvent^                  writeCompletedEvent;
    AsyncCallback^                                      userCallback;
    Object^                                             userState;
};

public ref class WriteDocumentPaginatorAsyncResult :
public SerializeReachAsyncResult
{
    public:

    /// <summary>
    /// Instantiates a <c>WriteDocumentPaginatorAsyncResult</c>, which is used by an <c>ISerializeReach</c> object to
    /// provide Asynchronous Write of <c>DocumentPaginator</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serialized Document objects can be written to.</param>
    /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
    /// <param name="callback"><c>AsyncCallback</c> object that is called when write is complete.</param>
    /// <param name="state">User supplied object.</param>
    WriteDocumentPaginatorAsyncResult(
        System::Printing::ISerializeReach^                  serializationDestination,
        System::Windows::Documents::DocumentPaginator^      documentPaginator,
        AsyncCallback^                                      asyncCallback,
        Object^                                             state
        );

    /// <summary>
    /// Start Asynchronous Write of the <c>DocumentPaginator</c> to the <c>ISerializeReach</c>.
    /// </summary>        
    void
    AsyncWrite(
        void
        );

    private:

    System::Windows::Documents::DocumentPaginator^ userDocumentPaginator;
};

public ref class WriteVisualAsyncResult :
public SerializeReachAsyncResult
{
    public:

    /// <summary>
    /// Instantiates a <c>WriteVisualAsyncResult</c>, which is used by an <c>ISerializeReach</c> object to
    /// provide Asynchronous Write of <c>Visual</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serialized Document objects can be written to.</param>
    /// <param name="visual"><c>Visual</c> object to be written.</param>
    /// <param name="callback"><c>AsyncCallback</c> object that is called when write is complete.</param>
    /// <param name="state">User supplied object.</param>
    WriteVisualAsyncResult(
        System::Printing::ISerializeReach^                  serializationDestination,
        System::Windows::Media::Visual^                     visual,
        AsyncCallback^                                      asyncCallback,
        Object^                                             state
        );

    /// <summary>
    /// Start Asynchronous Write of the <c>Visual</c> to the <c>ISerializeReach</c>.
    /// </summary>
    void
    AsyncWrite(
        void
        );

    private:

    System::Windows::Media::Visual^                 userVisual;
};

public ref class WriteDocumentSequenceAsyncResult :
public SerializeReachAsyncResult
{
    public:

    /// <summary>
    /// Instantiates a <c>WriteDocumentSequenceAsyncResult</c>, which is used by an <c>ISerializeReach</c> object to
    /// provide Asynchronous Write of <c>DocumentSequence</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serialized Document objects can be written to.</param>
    /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
    /// <param name="callback"><c>AsyncCallback</c> object that is called when write is complete.</param>
    /// <param name="state">User supplied object.</param>
    WriteDocumentSequenceAsyncResult(
        System::Printing::ISerializeReach^                  serializationDestination,
        System::Windows::Documents::FixedDocumentSequence^  fixedDocumentSequence,
        AsyncCallback^                                      asyncCallback,
        Object^                                             state
        );

    /// <summary>
    /// Start Asynchronous Write of the <c>DocumentSequence</c> to the <c>ISerializeReach</c>.
    /// </summary>
    void
    AsyncWrite(
        void
        );

    private:

    System::Windows::Documents::FixedDocumentSequence^   userDocumentSequence;
};

public ref class WriteFixedDocumentAsyncResult :
public SerializeReachAsyncResult
{
    public:

    /// <summary>
    /// Instantiates a <c>WriteFixedDocumentAsyncResult</c>, which is used by an <c>ISerializeReach</c> object to
    /// provide Asynchronous Write of <c>FixedDocument</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serialized Document objects can be written to.</param>
    /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
    /// <param name="callback"><c>AsyncCallback</c> object that is called when write is complete.</param>
    /// <param name="state">User supplied object.</param>
    WriteFixedDocumentAsyncResult(
        System::Printing::ISerializeReach^                  serializationDestination,
        System::Windows::Documents::FixedDocument^          fixedDocument,
        AsyncCallback^                                      asyncCallback,
        Object^                                             state
        );

    /// <summary>
    /// Start Asynchronous Write of the <c>FixedDocument</c> to the <c>ISerializeReach</c>.
    /// </summary>
    void
    AsyncWrite(
        void
        );

    private:

    System::Windows::Documents::FixedDocument^      userFixedDocument;
};

public ref class WriteFixedPageAsyncResult :
public SerializeReachAsyncResult
{
    public:

    /// <summary>
    /// Instantiates a <c>WriteFixedPageAsyncResult</c>, which is used by an <c>ISerializeReach</c> object to
    /// provide Asynchronous Write of <c>FixedPage</c>.
    /// </summary>        
    /// <param name="serializationDestination"><c>ISerializeReach</c> object that serialized Document objects can be written to.</param>
    /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
    /// <param name="callback"><c>AsyncCallback</c> object that is called when write is complete.</param>
    /// <param name="state">User supplied object.</param>
    WriteFixedPageAsyncResult(
        System::Printing::ISerializeReach^                  serializationDestination,
        System::Windows::Documents::FixedPage^              fixedPage,
        AsyncCallback^                                      asyncCallback,
        Object^                                             state
        );

    /// <summary>
    /// Start Asynchronous Write of the <c>FixedPage</c> to the <c>ISerializeReach</c>.
    /// </summary>
    void
    AsyncWrite(
        void
        );

    private:

    System::Windows::Documents::FixedPage^          userFixedPage;
};

}
}
#endif // __SERIALIZEREACHASYNCRESULT_HPP__
