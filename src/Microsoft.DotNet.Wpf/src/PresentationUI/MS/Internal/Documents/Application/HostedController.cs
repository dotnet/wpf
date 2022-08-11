// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description:
// Responsible for the lifecycle of the Document when hosted in a browser.

using System;
using System.Security;
using System.Windows.TrustUI;
using System.Windows.Interop;
using MS.Internal;
using MS.Internal.PresentationUI;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// Responsible for the lifecycle of the Document when hosted in a browser.
/// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
/// </summary>

[FriendAccessAllowed]
internal class HostedController : IDocumentController
{
    #region IDocumentController Members
    //--------------------------------------------------------------------------
    // IDocumentController Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.EnableEdit(Document document)
    {
        return false;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Open(Document document)
    {
        return false;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.Rebind(Document document)
    {
        bool handled = false;

        if (document.IsReloadNeeded)
        {
            Trace.SafeWrite(
                Trace.File,
                "Navigation requested for Rebind.");

            NavigationHelper.NavigateToDocument(document);
            
            document.IsReloadNeeded = false;
            handled = true;
        } 

        return handled;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveAsPreperation(Document document)
    {
        return false;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SaveCommit(Document document)
    {
        return false;
    }

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IDocumentController"/>
    /// </summary>
    bool IDocumentController.SavePreperation(Document document)
    {
        return false;
    }

    #endregion IDocumentController Members

    #region IChainOfResponsibiltyNode<Document> Members
    //--------------------------------------------------------------------------
    // IChainOfResponsibiltyNode<Document> Members
    //--------------------------------------------------------------------------

    /// <summary>
    /// <see cref="MS.Internal.Documents.Application.IChainOfResponsibiltyNode<T>"/>
    /// </summary>
    bool IChainOfResponsibiltyNode<Document>.IsResponsible(Document subject)
    {
        return NavigationHelper.Navigate != null;
    }

    #endregion IChainOfResponsibiltyNode<Document> Members
}
}
