// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
// Description: This interface is implemented by elements that support 
//              paginated layout of their content. IDocumentPaginatorSource 
//              contains only one member, DocumentPaginator, the object which 
//              performs the actual pagination of content.
//
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// This interface is implemented by elements that support paginated 
    /// layout of their content. IDocumentPaginatorSource contains only 
    /// one member, DocumentPaginator, the object which performs the actual 
    /// pagination of content.
    /// </summary>
    public interface IDocumentPaginatorSource
    {
        /// <summary>
        /// An object which paginates content.
        /// </summary>
        DocumentPaginator DocumentPaginator { get; }
    }
}
