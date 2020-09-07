// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                              
    Abstract:
    
        This object is instantiated against an ISerializeReach object. It is a public object to be used
        to serialize Print Subsystem objeects.
--*/

namespace System
{
namespace Printing
{
    [Obsolete("This class is obsolete and will not be in the next version.")]
    public ref class PrintableArchivableDocument
    {
    public:
        /// <summary>
        /// Instantiates a <c>PrintableArchivableDocument</c> against an object implementing <c>ISerializeReach</c>.
        /// </summary>        
        /// <param name="serializeReach"><c>ISerializeReach</c> object that will serialize and write the document objects.</param>
        PrintableArchivableDocument(
            ISerializeReach^        serializeReach
            );

        /// <summary>
        /// Writes an <c>DocumentPaginator</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        void
        Write(
            DocumentPaginator^      documentPaginator
            );

        /// <summary>
        /// Writes an <c>Visual</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        void
        Write(
            Visual^                 visual
            );

        /// <summary>
        /// Writes an <c>DocumentSequence</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        void
        Write(
            FixedDocumentSequence^       fixedDocumentSequence
            );

        /// <summary>
        /// Writes an <c>FixedDocument</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        void
        Write(
            FixedDocument^          fixedDocument
            );

        /// <summary>
        /// Writes an <c>FixedPage</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        void
        Write(
            FixedPage^              fixedPage
            );

        /// <summary>
        /// Begin Asynchronous Write of <c>DocumentPaginator</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        IAsyncResult^
        BeginWrite(
            DocumentPaginator^      documentPaginator,
            AsyncCallback^          callback,
            Object^                 state
            );

        /// <summary>
        /// Begin Asynchronous Write of <c>Visual</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        IAsyncResult^
        BeginWrite(
            Visual^                 visual,
            AsyncCallback^          callback,
            Object^                 state
            );

        /// <summary>
        /// Begin Asynchronous Write of <c>DocumentSequence</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        IAsyncResult^
        BeginWrite(
            FixedDocumentSequence^       fixedDocumentSequence,
            AsyncCallback^          callback,
            Object^                 state
            );

        /// <summary>
        /// Begin Asynchronous Write of <c>FixedDocument</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        IAsyncResult^
        BeginWrite(
            FixedDocument^          fixedDocument,
            AsyncCallback^          callback,
            Object^                 state
            );

        /// <summary>
        /// Begin Asynchronous Write of <c>FixedPage</c> to the <c>ISerializeReach</c> object. 
        /// </summary>        
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        IAsyncResult^
        BeginWrite(
            FixedPage^              fixedPage,
            AsyncCallback^          callback,
            Object^                 state
            );

        /// <summary>
        /// Checks to see if pending write has completed. 
        /// </summary>        
        /// <param name="asyncResult"><c>IAsyncResult</c> The reference to the pending asynchronous write request to wait for.</param>
        /// <remarks>
        /// <c>IAsyncResult</c> has to be validly obtained from a BeginWrite call.
        /// </remarks>
        void
        EndWrite(
            IAsyncResult^           asyncResult
            );

    private:
        XpsDocumentWriter^          serializationDestination;
    };
}
}
