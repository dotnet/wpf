// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
#ifndef __SERIALIZEREACHINTERFACE_HPP__
#define __SERIALIZEREACHINTERFACE_HPP__

namespace System
{
namespace Printing
{
    public interface class ISerializeReach
    {
        /// <summary>
        /// Serialize and Write an <c>DocumentPaginator</c>. 
        /// </summary>        
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::DocumentPaginator^  documentPaginator
            );

        /// <summary>
        /// Serialize and Write a <c>Visual</c>. 
        /// </summary>        
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Media::Visual^                 visual
            );

        /// <summary>
        /// Serialize and Write a <c>DocumentSequence</c>. 
        /// </summary>        
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocumentSequence^   fixedDocumentSequence
            );

        /// <summary>
        /// Serialize and Write a <c>FixedDocument</c>. 
        /// </summary>        
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedDocument^      fixedDocument
            );

        /// <summary>
        /// Serialize and Write a <c>FixedPage</c>. 
        /// </summary>        
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        virtual
        void
        Write(
            System::Windows::Documents::FixedPage^          fixedPage
            );

        /// <summary>
        /// Begin Asynchronous Serialize and Write of <c>DocumentPaginator</c>. 
        /// </summary>        
        /// <param name="documentPaginator"><c>DocumentPaginator</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        virtual
        IAsyncResult^
        BeginWrite(
            System::Windows::Documents::DocumentPaginator^  documentPaginator,
            AsyncCallback^                                  asyncCallback,
            Object^                                         state
            );

        /// <summary>
        /// Begin Asynchronous Serialize and Write of <c>Visual</c>. 
        /// </summary>        
        /// <param name="visual"><c>Visual</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        virtual
        IAsyncResult^
        BeginWrite(
            System::Windows::Media::Visual^                 visual,
            AsyncCallback^                                  asyncCallback,
            Object^                                         state
            );

        /// <summary>
        /// Begin Asynchronous Serialize and Write of <c>DocumentSequence</c>. 
        /// </summary>        
        /// <param name="documentSequence"><c>DocumentSequence</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        virtual
        IAsyncResult^
        BeginWrite(
            System::Windows::Documents::FixedDocumentSequence^   fixedDocumentSequence,
            AsyncCallback^                                       asyncCallback,
            Object^                                              state
            );

        /// <summary>
        /// Begin Asynchronous Serialize and Write of <c>FixedDocument</c>. 
        /// </summary>        
        /// <param name="fixedDocument"><c>FixedDocument</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        virtual
        IAsyncResult^
        BeginWrite(
            System::Windows::Documents::FixedDocument^      fixedDocument,
            AsyncCallback^                                  asyncCallback,
            Object^                                         state
            );

        /// <summary>
        /// Begin Asynchronous Serialize and Write of <c>FixedPage</c>. 
        /// </summary>        
        /// <param name="fixedPage"><c>FixedPage</c> object to be written.</param>
        /// <param name="asyncCallback"><c>AsyncCallback</c> object that is called when write is complete.</param>
        /// <param name="state">User supplied object.</param>
        virtual
        IAsyncResult^
        BeginWrite(
            System::Windows::Documents::FixedPage^          fixedPage,
            AsyncCallback^                                  asyncCallback,
            Object^                                         state
            );

        /// <summary>
        /// Checks to see if pending write has completed. 
        /// </summary>        
        /// <param name="asyncResult"><c>IAsyncResult</c> The reference to the pending asynchronous write request to wait for.</param>
        /// <remarks>
        /// <c>IAsyncResult</c> has to be validly obtained from a BeginWrite call.
        /// </remarks>
        virtual
        void
        EndWrite(
            IAsyncResult^                                   asyncResult
            );
    };
}
}

#endif // __SERIALIZEREACHINTERFACE_HPP__
