// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  This class wraps TransactionalPackage, ensuring that only parts with
//  approved content types can be written.

using System;
using System.IO;
using System.IO.Packaging;
using System.Security;
using System.Windows.TrustUI;

using MS.Internal;

namespace MS.Internal.Documents.Application
{
/// <summary>
/// This class wraps TransactionalPackage, ensuring that only approved
/// part content types can be written.
/// </summary>
internal sealed class RestrictedTransactionalPackage : TransactionalPackage
{
    #region Constructors
    //--------------------------------------------------------------------------
    // Constructors
    //-------------------------------------------------------------------------

    /// <summary>
    /// Requires an existing open Package; and returns a package which will
    /// capture changes with out applying them to the original.
    /// See the class description for details.
    /// </summary>
    /// <exception cref="System.ArgumentNullException" />
    /// <example>
    /// Package package = new RestrictedTransactionalPackage(
    ///     Package.Open(source, FileMode.Open, FileAccess.Read));
    /// </example>
    /// <param name="originalPackage">An open package.</param>
    internal RestrictedTransactionalPackage(Stream original)
        : base(original)
    { }
    #endregion Constructors

    /// <exception cref="System.ArgumentNullException" />
    /// <exception cref="System.ArgumentException" />
    internal override void MergeChanges(Stream target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (TempPackage.Value != null)
        {
            foreach (PackagePart part in TempPackage.Value.GetParts())
            {
                // Ensure that all parts being modified are permitted.
                if ((part != null) && (!IsValidContentType(part.ContentType)))
                {
                    throw new NotSupportedException(SR.Get(SRID.PackagePartTypeNotWritable));
                }
            }

            base.MergeChanges(target);
        }
    }

    /// <summary>
    /// Creates a new PackagePart.
    /// </summary>
    /// <remarks>
    /// When creating a new PackagePart we must:
    ///    a) ensure the part does not exist in package
    ///    b) ensure there is a writable package
    ///    c) create a temp part
    ///    d) update active part reference to the temp part
    /// 
    /// What if a PackagePart with the same Uri already exists?
    ///   Package.CreatePart checks for this.
    ///
    /// Do we need to worry about updating relationships and other parts?
    ///   Relationships are a part and are thus intrinsically handled.
    /// </remarks>
    /// <param name="partUri">Uri for the part to create.</param>
    /// <param name="contentType">Content type string.</param>
    /// <param name="compressionOption">Compression options.</param>
    /// <returns>A new PackagePart.</returns>
    protected override PackagePart CreatePartCore(
        Uri partUri, string contentType, CompressionOption compressionOption)
    {
        // Ensure that modifying this contentType is permitted.
        if (!IsValidContentType(contentType))
        {
            throw new ArgumentException(SR.Get(SRID.PackagePartTypeNotWritable), "contentType");
        }
        return base.CreatePartCore(partUri, contentType, compressionOption);
    }

    /// <summary>
    /// Verifies that parts of the the given contentType are allowed to be modified.
    /// </summary>
    /// <param name="contentType">The content type of the part being modified.</param>
    /// <returns>True if modification is allowed, false otherwise.</returns>
    /// Critical:
    ///  1) This code makes the actual security decision as to whether a specific
    ///     content type can be written.
    /// TreatAsSafe:
    ///  1) The list of content types is a hardcoded constant list of strings that
    ///     is only maintained here.  The only data being used is the string content
    ///     type to check.
    private bool IsValidContentType(string contentType)
    {
        // Check that the contentType is a valid string, and of one of the approved
        // contentTypes.
        //
        // The approved content types come from the Package-wide content types list
        // in the XPS Specification and Reference Guide.
        // Internally available at http://metroportal/
        // Externally available at http://www.microsoft.com/xps/
        return ((!string.IsNullOrEmpty(contentType)) &&
                ((contentType.Equals(
                    @"application/xml",
                    StringComparison.OrdinalIgnoreCase)) ||
                 // Core Properties Part
                 (contentType.Equals(
                    @"application/vnd.openxmlformats-package.core-properties+xml",
                    StringComparison.OrdinalIgnoreCase)) ||
                 // Digital Signature Certificate Part
                 (contentType.Equals(
                    @"application/vnd.openxmlformats-package.digital-signature-certificate",
                    StringComparison.OrdinalIgnoreCase)) ||
                 // Digital Signature Origin Part
                 (contentType.Equals(
                    @"application/vnd.openxmlformats-package.digital-signature-origin",
                    StringComparison.OrdinalIgnoreCase)) ||
                 // Digital Signature XML Signature Part
                 (contentType.Equals(
                    @"application/vnd.openxmlformats-package.digital-signature-xmlsignature+xml",
                    StringComparison.OrdinalIgnoreCase)) ||
                 // Relationships Part
                 (contentType.Equals(
                    @"application/vnd.openxmlformats-package.relationships+xml",
                    StringComparison.OrdinalIgnoreCase))
                ));
    }    
}
}
