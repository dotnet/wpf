// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: IDigSigProvider is a facade to the XPS Document signature implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace MS.Internal.Documents
{
/// <summary>
/// IDigSigProvider is a facade to the XPS Document signature implementation.
/// </summary>
/// <remarks>
/// The responsiblity of the facade in this case is to simplify implementation
/// and encapsulate changes.
/// </remarks>
internal interface IDigitalSignatureProvider
{
    #region Methods
    //--------------------------------------------------------------------------
    // Methods
    //--------------------------------------------------------------------------

    /// <summary>
    /// Add the Digital Signature Request (Spot)
    /// </summary>
    Guid AddRequestSignature(DigitalSignature digitalSignature);

    /// <summary>
    /// Returns a list of all certificates used to sign the document.
    /// </summary>
    /// <returns>All the certificates in the signatures in the document</returns>
    IList<X509Certificate2> GetAllCertificates();

    /// <summary>
    /// Determines and returns the status of all the certificates used to sign
    /// the document.
    /// </summary>
    /// <param name="certificates">A list of certificates</param>
    /// <returns>A dictionary of certificates and status</returns>
    IDictionary<X509Certificate2, CertificatePriorityStatus> GetCertificateStatus(IList<X509Certificate2> certificates);

    /// <summary>
    /// Remove the Digital Signature Request (Spot)
    /// </summary>
    void RemoveRequestSignature(Guid spotId);

    /// <summary>
    /// Sign the XPS Document (Signature)
    /// </summary>
    void SignDocument(DigitalSignature digitalSignature);

    /// <summary>
    /// Unsign the XPS Document (Signature)
    /// </summary>
    void UnsignDocument(Guid id);

    /// <summary>
    /// Verifies all the XPS digital signatures in the package.
    /// </summary>
    void VerifySignatures();

    #endregion Methods

    #region Properties
    //--------------------------------------------------------------------------
    // Properties
    //--------------------------------------------------------------------------

    /// <summary>
    /// Does this document have signature requests?
    /// </summary>
    /// <remarks>
    /// If the request has been signed then it is no longer a request.
    /// </remarks>
    /// <value>True if signature requests exist.</value>
    bool HasRequests { get; }

    /// <summary>
    /// Does this document have digital signatures?
    /// </summary>
    /// <remarks>
    /// This does not evaluate the signatures; they may be invalid if this
    /// returns true.
    /// </remarks>
    /// <value>True if signatures exist.</value>
    bool IsSigned { get; }

    /// <summary>
    /// Does the document meet the policy for signing?
    /// </summary>
    /// <remarks>
    /// This will return the value of XpsDocument.IsSignable,
    /// which is a time-consuming (blocking) operation.
    /// </remarks>
    /// <value>True the document meets the signing policy.</value>
    bool IsSignable { get; }

    /// <summary>
    /// A list of signatures associated to the XPS Document (Signatures & Spots)
    /// </summary>
    /// <remarks>
    /// Given this list the user should be able to determine what parts have
    /// been signed and whether the signatures are valid.
    /// </remarks>
    /// <value>List of DigitalSignatures</value>
    ReadOnlyCollection<DigitalSignature> Signatures { get; }
    #endregion Properties
}
}
