// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Defines the interface for participating in the document lifecycle.

using System;
using System.Collections.Generic;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Defines the interface for participating in the document lifecycle.
/// <seealso cref="MS.Internal.Documents.Application.DocumentManager"/>
/// </summary>
internal interface IDocumentController : IChainOfResponsibiltyNode<Document>
{
    /// <summary>
    /// Implementors should prepare Document.Workspace for editing purposes
    /// or return false if Edit is not possible.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool EnableEdit(Document document);

    /// <summary>
    /// Implementors should ensure source data is ready for use in 
    /// Document.Source.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool Open(Document document);

    /// <summary>
    /// Called when the underlying source has changed.  Some providers will
    /// need to re-open the source in this case.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool Rebind(Document document);

    /// <summary>
    /// Implementors should prepare the Document.Destination to be written
    /// to durring SaveCommit.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool SaveAsPreperation(Document document);

    /// <summary>
    /// Implementors should write there state to Document.Destination.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool SaveCommit(Document document);

    /// <summary>
    /// Implementors should prepare the Document.Destination to be written
    /// to durring SaveCommit.
    /// </summary>
    /// <param name="document">A document.</param>
    /// <returns>True if you handled the operation and it should not be 
    /// presented to others in the chain to handle.</returns>
    bool SavePreperation(Document document);
}
}